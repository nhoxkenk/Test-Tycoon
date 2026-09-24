using System;
using System.Collections.Generic;

namespace Farm.Actors
{
    public interface IActorState<in TContext>
    {
        void Enter(TContext context);
        void Tick(TContext context, float deltaTime);
        void Exit(TContext context);
    }

    public sealed class ActorStateMachine<TState, TContext> where TState : Enum
    {
        private readonly TContext context;
        private readonly Dictionary<TState, IActorState<TContext>> states = new Dictionary<TState, IActorState<TContext>>();
        private readonly Dictionary<TState, List<Transition>> transitions = new Dictionary<TState, List<Transition>>();
        private bool running;

        public TState Current { get; private set; }
        public bool IsRunning => running;

        public ActorStateMachine(TContext context)
        {
            if (ReferenceEquals(context, null)) throw new ArgumentNullException(nameof(context));
            this.context = context;
        }

        public void AddState(TState id, IActorState<TContext> state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            states.Add(id, state);
        }

        // Earlier transitions have higher priority. Guards must only read context.
        public void AddTransition(TState from, TState to, Func<TContext, bool> guard)
        {
            if (guard == null) throw new ArgumentNullException(nameof(guard));
            if (!transitions.TryGetValue(from, out var list))
                transitions[from] = list = new List<Transition>();
            list.Add(new Transition(to, guard));
        }

        public void Start(TState initial)
        {
            if (running) throw new InvalidOperationException("State machine is already running.");
            if (!states.ContainsKey(initial)) throw new ArgumentException("Initial state is not registered.", nameof(initial));
            Current = initial;
            running = true;
            states[Current].Enter(context);
        }

        public void Tick(float deltaTime)
        {
            if (!running) return;
            states[Current].Tick(context, deltaTime);
            if (!transitions.TryGetValue(Current, out var list)) return;

            foreach (var transition in list)
            {
                if (!transition.Guard(context)) continue;
                if (!states.ContainsKey(transition.To))
                    throw new InvalidOperationException("Transition destination is not registered.");
                states[Current].Exit(context);
                Current = transition.To;
                states[Current].Enter(context);
                return; // At most one transition per tick.
            }
        }

        public void Stop()
        {
            if (!running) return;
            states[Current].Exit(context);
            running = false;
        }

        private readonly struct Transition
        {
            public readonly TState To;
            public readonly Func<TContext, bool> Guard;

            public Transition(TState to, Func<TContext, bool> guard)
            {
                To = to;
                Guard = guard;
            }
        }
    }
}
