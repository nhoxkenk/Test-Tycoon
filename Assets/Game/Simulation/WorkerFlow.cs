using System;
using Farm.Actors;

namespace Farm.Simulation
{
    public enum WorkerState
    {
        MoveToResource,
        Harvest,
        IdleWithCargo,
        MoveToCashout,
        Cashout,
        ReturnToOrigin,
        ReturnToPool
    }

    public sealed class WorkerContext
    {
        public int ActorId { get; }
        public int ResourceId { get; }
        public WorldPoint Origin { get; }
        public WorldPoint HarvestPoint { get; }
        public IActorNavigation Navigation { get; }
        public bool ResourceOpen { get; private set; } = true;
        public bool HasCargo { get; private set; }
        public bool SaleComplete { get; private set; }
        public int CustomerId { get; private set; } = -1;
        public WorldPoint CashoutPoint { get; private set; }
        public Action<int> HarvestRequested { get; set; }
        public Action<int> ReadyWithCargo { get; set; }
        public Action<int, int> SaleRequested { get; set; }
        public Action<int, int> CustomerTargetLost { get; set; }

        public bool HasCustomer => CustomerId >= 0;

        public WorkerContext(int actorId, int resourceId, WorldPoint origin, WorldPoint harvestPoint, IActorNavigation navigation)
        {
            ActorId = actorId;
            ResourceId = resourceId;
            Origin = origin;
            HarvestPoint = harvestPoint;
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        public void CloseResource() => ResourceOpen = false;

        public void CompleteHarvest()
        {
            if (HasCargo) throw new InvalidOperationException("Worker already has cargo.");
            HasCargo = true;
            SaleComplete = false;
        }

        public void AssignCustomer(int customerId, WorldPoint cashoutPoint)
        {
            if (!HasCargo || HasCustomer) throw new InvalidOperationException("Worker is not available for a customer.");
            CustomerId = customerId;
            CashoutPoint = cashoutPoint;
        }

        public void CancelCustomer()
        {
            if (!HasCustomer) return;
            var lostId = CustomerId;
            CustomerId = -1;
            CustomerTargetLost?.Invoke(ActorId, lostId);
        }

        public void CompleteSale()
        {
            if (!HasCargo || !HasCustomer) throw new InvalidOperationException("No pending sale.");
            HasCargo = false;
            SaleComplete = true;
            CustomerId = -1;
        }
    }

    public sealed class WorkerMoveToResourceState : IActorState<WorkerContext>
    {
        public void Enter(WorkerContext context) => context.Navigation.MoveTo(context.HarvestPoint);
        public void Tick(WorkerContext context, float deltaTime) { }
        public void Exit(WorkerContext context) => context.Navigation.Stop();
    }

    public sealed class WorkerHarvestState : IActorState<WorkerContext>
    {
        public void Enter(WorkerContext context) => context.HarvestRequested?.Invoke(context.ActorId);
        public void Tick(WorkerContext context, float deltaTime) { }
        public void Exit(WorkerContext context) { }
    }

    public sealed class WorkerIdleWithCargoState : IActorState<WorkerContext>
    {
        public void Enter(WorkerContext context)
        {
            context.Navigation.Stop();
            context.ReadyWithCargo?.Invoke(context.ActorId);
        }
        public void Tick(WorkerContext context, float deltaTime) { }
        public void Exit(WorkerContext context) { }
    }

    public sealed class WorkerMoveToCashoutState : IActorState<WorkerContext>
    {
        public void Enter(WorkerContext context) => context.Navigation.MoveTo(context.CashoutPoint);
        public void Tick(WorkerContext context, float deltaTime)
        {
            if (context.Navigation.Status == NavigationStatus.Unreachable)
                context.CancelCustomer();
        }
        public void Exit(WorkerContext context) => context.Navigation.Stop();
    }

    public sealed class WorkerCashoutState : IActorState<WorkerContext>
    {
        public void Enter(WorkerContext context)
        {
            context.Navigation.Stop();
            context.SaleRequested?.Invoke(context.ActorId, context.CustomerId);
        }
        public void Tick(WorkerContext context, float deltaTime) { }
        public void Exit(WorkerContext context) { }
    }

    public sealed class WorkerReturnToOriginState : IActorState<WorkerContext>
    {
        public void Enter(WorkerContext context) => context.Navigation.MoveTo(context.Origin);
        public void Tick(WorkerContext context, float deltaTime) { }
        public void Exit(WorkerContext context) => context.Navigation.Stop();
    }

    public sealed class WorkerReturnToPoolState : IActorState<WorkerContext>
    {
        public void Enter(WorkerContext context) => context.Navigation.Stop();
        public void Tick(WorkerContext context, float deltaTime) { }
        public void Exit(WorkerContext context) { }
    }
}
