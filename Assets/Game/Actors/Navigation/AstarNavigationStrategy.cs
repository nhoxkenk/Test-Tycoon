using Pathfinding;
using UnityEngine;

namespace Farm.Actors
{
    public sealed class AstarNavigationStrategy : ActorNavigationStrategy
    {
        [SerializeField] private AstarPath astarPath;

        public override string BackendId => "Astar";

        public override bool Validate(WorkerActor workerPrefab, CustomerActor customerPrefab, out string error)
        {
            error = null;
            if (astarPath == null)
            { error = "AstarPath reference is not assigned on AstarNavigationStrategy."; return false; }
            if (workerPrefab == null || workerPrefab.GetComponent<AIPath>() == null || workerPrefab.GetComponent<Seeker>() == null)
            { error = "Worker prefab is missing AIPath or Seeker for A* backend."; return false; }
            if (customerPrefab == null || customerPrefab.GetComponent<AIPath>() == null || customerPrefab.GetComponent<Seeker>() == null)
            { error = "Customer prefab is missing AIPath or Seeker for A* backend."; return false; }
            return true;
        }

        public override void Prepare()
        {
            if (astarPath != null && (astarPath.graphs == null || astarPath.graphs.Length == 0))
                astarPath.Scan();
        }

        public override IActorNavigation CreateNavigation(Component actor)
        {
            var ai = actor.GetComponent<AIPath>();
            var seeker = actor.GetComponent<Seeker>();
            if (ai == null || seeker == null) return null;
            return new AstarNavigation(ai, seeker);
        }

        public override void Cleanup()
        {
            // A* graph data persists with the scene; no runtime cleanup needed.
        }
    }
}
