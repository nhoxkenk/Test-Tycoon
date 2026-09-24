using System;
using Farm.Actors;
using UnityEngine;
using Pathfinding;

namespace Farm.Actors
{
    //[RequireComponent(typeof(AIPath), typeof(Seeker), typeof(Pathfinding.RVO.RVOController))]
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    public sealed class CustomerActor : MonoBehaviour
    {
        private static readonly int IsMove = Animator.StringToHash("IsMove");
        private static readonly int IsCarryMove = Animator.StringToHash("IsCarryMove");
        private static readonly int IsEmpty = Animator.StringToHash("IsEmpty");

        private IActorNavigation navigation;
        private Animator animator;
        private ActorCarryView carryView;
        private CustomerContext context;
        private ActorStateMachine<CustomerState, CustomerContext> machine;
        private bool returnNotified;

        public int ActorId => context?.ActorId ?? -1;
        public int SlotId => context?.SlotId ?? -1;
        public bool IsReady => machine != null && machine.Current == CustomerState.WaitForWorker && !context.PurchaseConfirmed;
        public CustomerState State => machine?.Current ?? CustomerState.ReturnToPool;

        public event Action<CustomerActor> ReadyAtTable;
        public event Action<CustomerActor> LeftTable;
        public event Action<CustomerActor> ReadyToPool;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            carryView = GetComponent<ActorCarryView>();
        }

        public void ShowCargo(int quantity, Vector3[] fromPositions = null) => carryView?.Show(quantity, fromPositions);

        public bool Initialize(int actorId, int slotId, WorldPoint start, WorldPoint table, WorldPoint exit, IActorNavigation actorNavigation)
        {
            ResetForPool();
            navigation = actorNavigation ?? throw new ArgumentNullException(nameof(actorNavigation));
            if (!navigation.PlaceAt(start)) return false;

            context = new CustomerContext(actorId, slotId, table, exit, navigation)
            {
                ReadyAtTable = _ => ReadyAtTable?.Invoke(this),
                LeavingTable = _ => LeftTable?.Invoke(this)
            };
            machine = new ActorStateMachine<CustomerState, CustomerContext>(context);
            machine.AddState(CustomerState.MoveToTable, new CustomerMoveToTableState());
            machine.AddState(CustomerState.WaitForWorker, new CustomerWaitForWorkerState());
            machine.AddState(CustomerState.MoveToExit, new CustomerMoveToExitState());
            machine.AddState(CustomerState.ReturnToPool, new CustomerReturnToPoolState());
            machine.AddTransition(CustomerState.MoveToTable, CustomerState.ReturnToPool, c => c.Navigation.Status == NavigationStatus.Unreachable);
            machine.AddTransition(CustomerState.MoveToTable, CustomerState.WaitForWorker, c => c.Navigation.Status == NavigationStatus.Arrived);
            machine.AddTransition(CustomerState.WaitForWorker, CustomerState.MoveToExit, c => c.PurchaseConfirmed);
            machine.AddTransition(CustomerState.MoveToExit, CustomerState.ReturnToPool,
                c => c.Navigation.Status == NavigationStatus.Arrived || c.Navigation.Status == NavigationStatus.Unreachable);
            machine.Start(CustomerState.MoveToTable);
            returnNotified = false;
            return true;
        }

        public void ConfirmPurchase()
        {
            if (!IsReady) throw new InvalidOperationException("Customer is not waiting at the table.");
            context.ConfirmPurchase();
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (machine == null) return;
            machine.Tick(deltaTime);
            UpdateAnimation();
            if (machine.Current != CustomerState.ReturnToPool || returnNotified) return;
            returnNotified = true;
            ReadyToPool?.Invoke(this);
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;
            animator.SetBool(IsMove, State == CustomerState.MoveToTable);
            animator.SetBool(IsCarryMove, State == CustomerState.MoveToExit);
            animator.SetBool(IsEmpty, !context.PurchaseConfirmed);
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
            ReadyAtTable = null;
            LeftTable = null;
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
