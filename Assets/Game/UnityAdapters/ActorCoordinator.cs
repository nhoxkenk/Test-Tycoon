using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm.UnityAdapters
{
    // Scene-level owner of pooled actors and table reservations.
    public sealed class ActorCoordinator : IDisposable
    {
        private readonly WorkerFactory workers;
        private readonly CustomerFactory customers;
        private readonly MarketLayout market;
        private readonly Dictionary<int, WorkerActor> activeWorkers = new Dictionary<int, WorkerActor>();
        private readonly Dictionary<int, CustomerActor> activeCustomers = new Dictionary<int, CustomerActor>();
        private readonly HashSet<int> assignedResources = new HashSet<int>();
        private readonly HashSet<int> claimedCustomers = new HashSet<int>();
        private readonly int[] slotOwners;
        private int nextActorId = 1;
        private int targetCustomerCount = 1;
        private bool disposed;

        public event Action<int, int> HarvestRequested; // workerId, resourceId
        public event Action<int, int> SaleRequested; // workerId, customerId

        public int ActiveCustomerCount => activeCustomers.Count;
        public int ActiveWorkerCount => activeWorkers.Count;

        public int TargetCustomerCount
        {
            get => targetCustomerCount;
            set
            {
                targetCustomerCount = Math.Max(0, value);
                SpawnMissingCustomers();
            }
        }

        public ActorCoordinator(WorkerFactory workers, CustomerFactory customers, MarketLayout market)
        {
            this.workers = workers;
            this.customers = customers;
            this.market = market;
            slotOwners = new int[market.SlotCount];
        }

        public bool RegisterResource(int resourceId, Transform origin, Transform harvestPoint)
        {
            if (disposed || origin == null || harvestPoint == null || !assignedResources.Add(resourceId)) return false;
            var actor = workers.Spawn(nextActorId++, resourceId, origin.position.ToWorldPoint(), harvestPoint.position.ToWorldPoint());
            if (actor == null)
            {
                assignedResources.Remove(resourceId);
                return false;
            }
            activeWorkers.Add(actor.ActorId, actor);
            actor.HarvestRequested += OnHarvestRequested;
            actor.ReadyWithCargo += OnWorkerReadyWithCargo;
            actor.SaleRequested += OnSaleRequested;
            actor.CustomerTargetLost += OnCustomerTargetLost;
            actor.ReadyToPool += OnWorkerReadyToPool;
            return true;
        }

        public void UnregisterResource(int resourceId)
        {
            foreach (var worker in activeWorkers.Values)
                if (worker.ResourceId == resourceId) worker.CloseResource();
        }

        public bool CompleteHarvest(int workerId)
            => CompleteHarvest(workerId, 3);

        public bool CanHarvest(int workerId, int resourceId) =>
            activeWorkers.TryGetValue(workerId, out var worker) &&
            worker.ResourceId == resourceId && worker.State == Farm.Simulation.WorkerState.Harvest && !worker.HasCargo;

        public bool CompleteHarvest(int workerId, int quantity)
            => CompleteHarvest(workerId, quantity, null);

        public bool CompleteHarvest(int workerId, int quantity, Vector3[] fromPositions)
        {
            if (!activeWorkers.TryGetValue(workerId, out var worker) || worker.State != Farm.Simulation.WorkerState.Harvest)
                return false;
            worker.ShowCargo(quantity, fromPositions);
            worker.CompleteHarvest();
            TryMatch();
            return true;
        }

        public bool ConfirmSale(int workerId, int customerId, int quantity)
        {
            if (!activeWorkers.TryGetValue(workerId, out var worker) ||
                worker.CustomerId != customerId ||
                !activeCustomers.TryGetValue(customerId, out var customer) ||
                worker.State != Farm.Simulation.WorkerState.Cashout || !customer.IsReady)
                return false;

            var fromPositions = worker.CargoPositions;
            worker.ShowCargo(0);
            customer.ShowCargo(quantity, fromPositions);
            worker.ConfirmSale();
            customer.ConfirmPurchase();
            claimedCustomers.Remove(customer.ActorId);
            return true;
        }

        private void SpawnMissingCustomers()
        {
            if (disposed) return;
            while (activeCustomers.Count < targetCustomerCount)
            {
                var slot = Array.FindIndex(slotOwners, ownerId => ownerId == 0);
                if (slot < 0) return;
                var actorId = nextActorId++;
                slotOwners[slot] = actorId; // Reserve before taking an actor from the pool.
                var actor = customers.Spawn(actorId, slot,
                    market.CustomerStart.position.ToWorldPoint(),
                    market.CustomerPoint(slot).position.ToWorldPoint(),
                    market.CustomerEnd.position.ToWorldPoint());
                if (actor == null)
                {
                    slotOwners[slot] = 0;
                    Debug.LogError("Customer spawn point is not on a baked NavMesh.");
                    return;
                }
                activeCustomers.Add(actor.ActorId, actor);
                actor.ReadyAtTable += OnCustomerReady;
                actor.LeftTable += OnCustomerLeftTable;
                actor.ReadyToPool += OnCustomerReadyToPool;
            }
        }

        private void TryMatch()
        {
            foreach (var worker in activeWorkers.Values)
            {
                if (!worker.CanServe) continue;
                foreach (var customer in activeCustomers.Values)
                {
                    if (!customer.IsReady || claimedCustomers.Contains(customer.ActorId)) continue;
                    claimedCustomers.Add(customer.ActorId);
                    worker.AssignCustomer(customer.ActorId, market.WorkerPoint(customer.SlotId).position.ToWorldPoint());
                    break;
                }
            }
        }

        private void OnCustomerReady(CustomerActor actor) => TryMatch();
        private void OnCustomerLeftTable(CustomerActor actor)
        {
            ReleaseSlot(actor);
            SpawnMissingCustomers();
        }
        private void OnWorkerReadyWithCargo(WorkerActor actor) => TryMatch();
        private void OnHarvestRequested(WorkerActor actor) => HarvestRequested?.Invoke(actor.ActorId, actor.ResourceId);
        private void OnSaleRequested(WorkerActor actor, int customerId) => SaleRequested?.Invoke(actor.ActorId, customerId);
        private void OnCustomerTargetLost(WorkerActor actor, int customerId) => claimedCustomers.Remove(customerId);

        private void OnWorkerReadyToPool(WorkerActor actor)
        {
            if (actor.HasCargo)
            {
                Debug.LogError("Cannot pool a worker that still owns cargo.", actor);
                return;
            }
            activeWorkers.Remove(actor.ActorId);
            assignedResources.Remove(actor.ResourceId);
            workers.Release(actor);
        }

        private void OnCustomerReadyToPool(CustomerActor actor)
        {
            foreach (var worker in activeWorkers.Values)
                if (worker.CustomerId == actor.ActorId) worker.CancelCustomer();
            claimedCustomers.Remove(actor.ActorId);
            ReleaseSlot(actor);
            activeCustomers.Remove(actor.ActorId);
            customers.Release(actor);
            SpawnMissingCustomers();
        }

        private void ReleaseSlot(CustomerActor actor)
        {
            if (slotOwners[actor.SlotId] == actor.ActorId)
                slotOwners[actor.SlotId] = 0;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var actor in activeWorkers.Values)
                if (actor != null) workers.Release(actor);
            foreach (var actor in activeCustomers.Values)
                if (actor != null) customers.Release(actor);
            activeWorkers.Clear();
            activeCustomers.Clear();
            workers.Dispose();
            customers.Dispose();
            HarvestRequested = null;
            SaleRequested = null;
        }
    }
}
