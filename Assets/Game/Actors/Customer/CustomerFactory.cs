using Farm.Actors;
using UnityEngine;
using UnityEngine.Pool;
using Pathfinding;
using UnityEngine.AI;

namespace Farm.Actors
{
    public sealed class CustomerFactory
    {
        private readonly CustomerActor prefab;
        private readonly Transform parent;
        private readonly ObjectPool<CustomerActor> pool;

        public CustomerFactory(CustomerActor prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
            pool = new ObjectPool<CustomerActor>(Create, OnGet, OnRelease, OnDestroy, true, 2, 32);
        }

        public CustomerActor Spawn(int actorId, int slotId, WorldPoint start, WorldPoint table, WorldPoint exit)
        {
            var actor = pool.Get();
            var navigation = new NavMeshAgentNavigation(actor.GetComponent<NavMeshAgent>());
            if (actor.Initialize(actorId, slotId, start, table, exit, navigation)) return actor;
            pool.Release(actor);
            return null;
        }

        public void Release(CustomerActor actor)
        {
            if (actor != null) pool.Release(actor);
        }
        public void Dispose() => pool.Dispose();

        private CustomerActor Create()
        {
            var actor = Object.Instantiate(prefab, parent);
            actor.gameObject.SetActive(false);
            return actor;
        }

        private static void OnGet(CustomerActor actor) => actor.gameObject.SetActive(true);

        private static void OnRelease(CustomerActor actor)
        {
            if (actor == null) return;
            actor.ResetForPool();
            actor.gameObject.SetActive(false);
        }

        private static void OnDestroy(CustomerActor actor)
        {
            if (actor != null) Object.Destroy(actor.gameObject);
        }
    }
}
