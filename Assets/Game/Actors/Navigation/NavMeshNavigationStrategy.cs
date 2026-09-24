using UnityEngine;
using UnityEngine.AI;

namespace Farm.Actors
{
    public sealed class NavMeshNavigationStrategy : ActorNavigationStrategy
    {
        public override string BackendId => "NavMesh";

        public override bool Validate(WorkerActor workerPrefab, CustomerActor customerPrefab, out string error)
        {
            error = null;
            if (workerPrefab == null || workerPrefab.GetComponent<NavMeshAgent>() == null)
            { error = "Worker prefab is missing NavMeshAgent."; return false; }
            if (customerPrefab == null || customerPrefab.GetComponent<NavMeshAgent>() == null)
            { error = "Customer prefab is missing NavMeshAgent."; return false; }
            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices.Length == 0)
            { error = "NavMesh has not been baked in this scene."; return false; }
            return true;
        }

        public override void Prepare() { /* NavMesh data is baked at edit time; no runtime prep. */ }

        public override IActorNavigation CreateNavigation(Component actor)
        {
            var agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null) return null;
            return new NavMeshAgentNavigation(agent);
        }

        public override void Cleanup() { /* No runtime resources to release. */ }
    }
}
