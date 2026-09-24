using Farm.Actors;
using UnityEngine;
using UnityEngine.AI;

namespace Farm.UnityAdapters
{
    public sealed class NavMeshAgentNavigation : IActorNavigation
    {
        private readonly NavMeshAgent agent;
        private bool hasDestination;
        private bool failed;

        public NavMeshAgentNavigation(NavMeshAgent agent)
        {
            this.agent = agent;
        }

        public NavigationStatus Status
        {
            get
            {
                if (!hasDestination) return NavigationStatus.Idle;
                if (failed) return NavigationStatus.Unreachable;
                if (!agent.isOnNavMesh) return NavigationStatus.Unreachable;
                if (agent.pathPending) return NavigationStatus.Moving;
                if (agent.pathStatus != NavMeshPathStatus.PathComplete) return NavigationStatus.Unreachable;
                return agent.remainingDistance <= agent.stoppingDistance + 0.05f
                    ? NavigationStatus.Arrived
                    : NavigationStatus.Moving;
            }
        }

        public bool PlaceAt(WorldPoint point)
        {
            var position = new Vector3(point.X, point.Y, point.Z);
            if (!NavMesh.SamplePosition(position, out var hit, 1.5f, agent.areaMask)) return false;
            if (!agent.Warp(hit.position)) return false;
            hasDestination = false;
            failed = false;
            return true;
        }

        public bool MoveTo(WorldPoint point)
        {
            hasDestination = true;
            failed = !agent.isOnNavMesh || !agent.SetDestination(new Vector3(point.X, point.Y, point.Z));
            return !failed;
        }

        public void Stop()
        {
            hasDestination = false;
            failed = false;
            if (agent.isOnNavMesh) agent.ResetPath();
        }
    }

    public static class WorldPointMapping
    {
        public static WorldPoint ToWorldPoint(this Vector3 value) => new WorldPoint(value.x, value.y, value.z);
    }
}
