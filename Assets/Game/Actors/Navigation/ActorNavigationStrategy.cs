using UnityEngine;

namespace Farm.Actors
{
    // Selects and prepares a navigation backend for actor spawning.
    public abstract class ActorNavigationStrategy : MonoBehaviour
    {
        // Fixed identifier used by composition to pick the right prefab pair.
        public abstract string BackendId { get; }

        // Validate that the scene and prefabs are ready for this backend.
        public abstract bool Validate(WorkerActor workerPrefab, CustomerActor customerPrefab, out string error);

        // One-time backend preparation before any spawns (e.g. scan graph, check bake data).
        public abstract void Prepare();

        // Create an IActorNavigation for an actor instance pulled from a pool.
        public abstract IActorNavigation CreateNavigation(Component actor);

        // Cleanup backend resources owned by this strategy. Safe to call multiple times.
        public abstract void Cleanup();
    }
}
