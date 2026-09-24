using System;
using Farm.Actors;

namespace Farm.Actors
{
    public enum CustomerState
    {
        MoveToTable,
        WaitForWorker,
        MoveToExit,
        ReturnToPool
    }

    public sealed class CustomerContext
    {
        public int ActorId { get; }
        public int SlotId { get; }
        public WorldPoint TablePoint { get; }
        public WorldPoint ExitPoint { get; }
        public IActorNavigation Navigation { get; }
        public bool PurchaseConfirmed { get; private set; }
        public Action<int> ReadyAtTable { get; set; }
        public Action<int> LeavingTable { get; set; }

        public CustomerContext(int actorId, int slotId, WorldPoint tablePoint, WorldPoint exitPoint, IActorNavigation navigation)
        {
            ActorId = actorId;
            SlotId = slotId;
            TablePoint = tablePoint;
            ExitPoint = exitPoint;
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        public void ConfirmPurchase() => PurchaseConfirmed = true;
    }

    public sealed class CustomerMoveToTableState : IActorState<CustomerContext>
    {
        public void Enter(CustomerContext context) => context.Navigation.MoveTo(context.TablePoint);
        public void Tick(CustomerContext context, float deltaTime) { }
        public void Exit(CustomerContext context) => context.Navigation.Stop();
    }

    public sealed class CustomerWaitForWorkerState : IActorState<CustomerContext>
    {
        public void Enter(CustomerContext context)
        {
            context.Navigation.Stop();
            context.ReadyAtTable?.Invoke(context.ActorId);
        }
        public void Tick(CustomerContext context, float deltaTime) { }
        public void Exit(CustomerContext context) { }
    }

    public sealed class CustomerMoveToExitState : IActorState<CustomerContext>
    {
        public void Enter(CustomerContext context)
        {
            context.Navigation.MoveTo(context.ExitPoint);
            context.LeavingTable?.Invoke(context.ActorId);
        }
        public void Tick(CustomerContext context, float deltaTime) { }
        public void Exit(CustomerContext context) => context.Navigation.Stop();
    }

    public sealed class CustomerReturnToPoolState : IActorState<CustomerContext>
    {
        public void Enter(CustomerContext context) => context.Navigation.Stop();
        public void Tick(CustomerContext context, float deltaTime) { }
        public void Exit(CustomerContext context) { }
    }
}
