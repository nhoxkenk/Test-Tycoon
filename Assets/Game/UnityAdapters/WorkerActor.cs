using System;
using Farm.Actors;
using Farm.Simulation;
using UnityEngine;
using Pathfinding;

namespace Farm.UnityAdapters
{
    [RequireComponent(typeof(AIPath), typeof(Seeker), typeof(Pathfinding.RVO.RVOController))]
    public sealed class WorkerActor : MonoBehaviour
    {
        private static readonly int IsMove = Animator.StringToHash("IsMove");
        private static readonly int IsCarryMove = Animator.StringToHash("IsCarryMove");
        private static readonly int IsEmpty = Animator.StringToHash("IsEmpty");

        private IActorNavigation navigation;
        private Animator animator;
        private ActorCarryView carryView;
        private WorkerContext context;
        private ActorStateMachine<WorkerState, WorkerContext> machine;
        private bool returnNotified;

        public int ActorId => context?.ActorId ?? -1;
        public int ResourceId => context?.ResourceId ?? -1;
        public int CustomerId => context?.CustomerId ?? -1;
        public bool HasCargo => context != null && context.HasCargo;
        public bool CanServe => HasCargo && context.CustomerId < 0 && machine.Current == WorkerState.IdleWithCargo;
        public WorkerState State => machine?.Current ?? WorkerState.ReturnToPool;

        public event Action<WorkerActor> HarvestRequested;
        public event Action<WorkerActor> ReadyWithCargo;
        public event Action<WorkerActor, int> SaleRequested;
        public event Action<WorkerActor, int> CustomerTargetLost;
        public event Action<WorkerActor> ReadyToPool;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            carryView = GetComponent<ActorCarryView>();
        }

        public Vector3[] CargoPositions => carryView != null ? carryView.CarryPositions : null;
        public void ShowCargo(int quantity, Vector3[] fromPositions = null) => carryView?.Show(quantity, fromPositions);

        public bool Initialize(int actorId, int resourceId, WorldPoint origin, WorldPoint harvestPoint, IActorNavigation actorNavigation)
        {
            ResetForPool();
            navigation = actorNavigation ?? throw new ArgumentNullException(nameof(actorNavigation));
            if (!navigation.PlaceAt(origin)) return false;

            context = new WorkerContext(actorId, resourceId, origin, harvestPoint, navigation)
            {
                HarvestRequested = _ => HarvestRequested?.Invoke(this),
                ReadyWithCargo = _ => ReadyWithCargo?.Invoke(this),
                SaleRequested = (_, customerId) => SaleRequested?.Invoke(this, customerId),
                CustomerTargetLost = (_, customerId) => CustomerTargetLost?.Invoke(this, customerId)
            };

            // Each pooled spawn owns fresh state instances and fresh Func guards.
            machine = new ActorStateMachine<WorkerState, WorkerContext>(context);
            machine.AddState(WorkerState.MoveToResource, new WorkerMoveToResourceState());
            machine.AddState(WorkerState.Harvest, new WorkerHarvestState());
            machine.AddState(WorkerState.IdleWithCargo, new WorkerIdleWithCargoState());
            machine.AddState(WorkerState.MoveToCashout, new WorkerMoveToCashoutState());
            machine.AddState(WorkerState.Cashout, new WorkerCashoutState());
            machine.AddState(WorkerState.ReturnToOrigin, new WorkerReturnToOriginState());
            machine.AddState(WorkerState.ReturnToPool, new WorkerReturnToPoolState());
            machine.AddTransition(WorkerState.MoveToResource, WorkerState.ReturnToPool, c => !c.ResourceOpen || c.Navigation.Status == NavigationStatus.Unreachable);
            machine.AddTransition(WorkerState.MoveToResource, WorkerState.Harvest, c => c.Navigation.Status == NavigationStatus.Arrived);
            machine.AddTransition(WorkerState.Harvest, WorkerState.IdleWithCargo, c => c.HasCargo);
            machine.AddTransition(WorkerState.Harvest, WorkerState.ReturnToPool, c => !c.ResourceOpen && !c.HasCargo);
            machine.AddTransition(WorkerState.IdleWithCargo, WorkerState.MoveToCashout, c => c.HasCargo && c.HasCustomer);
            machine.AddTransition(WorkerState.MoveToCashout, WorkerState.IdleWithCargo, c => !c.HasCustomer);
            machine.AddTransition(WorkerState.MoveToCashout, WorkerState.Cashout, c => c.Navigation.Status == NavigationStatus.Arrived && c.HasCustomer);
            machine.AddTransition(WorkerState.Cashout, WorkerState.IdleWithCargo, c => !c.HasCustomer && c.HasCargo);
            machine.AddTransition(WorkerState.Cashout, WorkerState.ReturnToOrigin, c => c.SaleComplete);
            machine.AddTransition(WorkerState.ReturnToOrigin, WorkerState.ReturnToPool,
                c => c.Navigation.Status == NavigationStatus.Unreachable ||
                     (c.Navigation.Status == NavigationStatus.Arrived && !c.ResourceOpen));
            machine.AddTransition(WorkerState.ReturnToOrigin, WorkerState.MoveToResource,
                c => c.Navigation.Status == NavigationStatus.Arrived && c.ResourceOpen);
            machine.Start(WorkerState.MoveToResource);
            returnNotified = false;
            return true;
        }

        public void CompleteHarvest()
        {
            if (State != WorkerState.Harvest) throw new InvalidOperationException("Worker is not harvesting.");
            context.CompleteHarvest();
        }

        public void AssignCustomer(int customerId, WorldPoint cashoutPoint) => context.AssignCustomer(customerId, cashoutPoint);
        public void CancelCustomer() => context?.CancelCustomer();
        public void CloseResource() => context?.CloseResource();

        public void ConfirmSale()
        {
            if (State != WorkerState.Cashout) throw new InvalidOperationException("Worker has not reached the customer.");
            context.CompleteSale();
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (machine == null) return;
            machine.Tick(deltaTime);
            UpdateAnimation();
            if (machine.Current != WorkerState.ReturnToPool || returnNotified) return;
            returnNotified = true;
            ReadyToPool?.Invoke(this);
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;
            var moving = State == WorkerState.MoveToResource || State == WorkerState.MoveToCashout || State == WorkerState.ReturnToOrigin;
            animator.SetBool(IsMove, moving && !HasCargo);
            animator.SetBool(IsCarryMove, moving && HasCargo);
            animator.SetBool(IsEmpty, !HasCargo);
        }

        public void ResetForPool()
        {
            carryView?.Show(0);
            machine?.Stop();
            navigation?.Stop();
            (navigation as IDisposable)?.Dispose();
            machine = null;
            context = null;
            navigation = null;
            returnNotified = false;
            HarvestRequested = null;
            ReadyWithCargo = null;
            SaleRequested = null;
            CustomerTargetLost = null;
            ReadyToPool = null;
            if (animator != null)
            {
                animator.SetBool(IsMove, false);
                animator.SetBool(IsCarryMove, false);
                animator.SetBool(IsEmpty, true);
            }
        }

        private void OnDisable() => ResetForPool();
    }
}
