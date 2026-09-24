using System;
using Farm.Actors;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class ActorSceneInstaller : MonoBehaviour
    {
        [SerializeField] private WorkerActor workerPrefab;
        [SerializeField] private CustomerActor customerPrefab;
        [SerializeField] private MarketLayout market;
        [SerializeField] private Transform actorParent;
        [SerializeField, Min(0)] private int initialCustomerCount = 1;

        public ActorCoordinator Coordinator { get; private set; }
        public IWorldQuery WorldQuery { get; private set; }
        public event Action<int, int> HarvestRequested;
        public event Action<int, int> SaleRequested;

        public bool Initialize()
        {
            if (Coordinator != null) return true;
            if (workerPrefab == null || customerPrefab == null || market == null || !market.Initialize())
            {
                Debug.LogError("Actor scene references or Market dock anchors are missing.", this);
                return false;
            }

            Coordinator = new ActorCoordinator(
                new WorkerFactory(workerPrefab, actorParent != null ? actorParent : transform),
                new CustomerFactory(customerPrefab, actorParent != null ? actorParent : transform),
                market);
            WorldQuery = new NavMeshWorldQuery();
            Coordinator.HarvestRequested += (workerId, resourceId) => HarvestRequested?.Invoke(workerId, resourceId);
            Coordinator.SaleRequested += (workerId, customerId) => SaleRequested?.Invoke(workerId, customerId);
            Coordinator.TargetCustomerCount = initialCustomerCount;
            return Coordinator.ActiveCustomerCount == initialCustomerCount;
        }

        public bool RegisterResource(int resourceId, Transform workerOrigin, Transform harvestPoint) =>
            Coordinator != null && Coordinator.RegisterResource(resourceId, workerOrigin, harvestPoint);

        public void UnregisterResource(int resourceId) => Coordinator?.UnregisterResource(resourceId);
        public bool CompleteHarvest(int workerId) => Coordinator != null && Coordinator.CompleteHarvest(workerId);
        public bool ConfirmSale(int workerId) => Coordinator != null && Coordinator.ConfirmSale(workerId);
        public void SetTargetCustomerCount(int count)
        {
            if (Coordinator != null) Coordinator.TargetCustomerCount = count;
        }

        public void Dispose()
        {
            Coordinator?.Dispose();
            Coordinator = null;
            WorldQuery = null;
            HarvestRequested = null;
            SaleRequested = null;
        }

        private void OnDestroy() => Dispose();
    }
}
