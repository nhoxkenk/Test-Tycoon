using Farm.Actors;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.AI;

namespace Farm.UnityAdapters
{
    public sealed class WorkerFactory
    {
        private readonly WorkerActor prefab;
        private readonly Transform parent;
        private readonly ObjectPool<WorkerActor> pool;

        public WorkerFactory(WorkerActor prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
            pool = new ObjectPool<WorkerActor>(Create, OnGet, OnRelease, OnDestroy, true, 2, 32);
        }

        public WorkerActor Spawn(int actorId, int resourceId, WorldPoint origin, WorldPoint harvestPoint)
        {
            var actor = pool.Get();
            if (actor.Initialize(actorId, resourceId, origin, harvestPoint,
                    new NavMeshAgentNavigation(actor.GetComponent<NavMeshAgent>()))) return actor;
            pool.Release(actor);
            return null;
        }

        public void Release(WorkerActor actor)
        {
            if (actor != null) pool.Release(actor);
        }
        public void Dispose() => pool.Dispose();

        private WorkerActor Create()
        {
            var actor = Object.Instantiate(prefab, parent);
            actor.gameObject.SetActive(false);
            return actor;
        }

        private static void OnGet(WorkerActor actor) => actor.gameObject.SetActive(true);

        private static void OnRelease(WorkerActor actor)
        {
            if (actor == null) return;
            actor.ResetForPool();
            actor.gameObject.SetActive(false);
        }

        private static void OnDestroy(WorkerActor actor)
        {
            if (actor != null) Object.Destroy(actor.gameObject);
        }
    }
}
