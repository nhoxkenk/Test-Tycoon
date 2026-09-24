using Pathfinding;
using UnityEngine;

namespace Farm.Actors
{
    public sealed class AstarNavigation : IActorNavigation
    {
        private readonly AIPath ai;
        private readonly Seeker seeker;
        private bool hasDestination;

        public AstarNavigation(AIPath ai, Seeker seeker)
        {
            this.ai = ai;
            this.seeker = seeker;
        }

        public NavigationStatus Status
        {
            get
            {
                if (!hasDestination) return NavigationStatus.Idle;
                if (!ai.hasPath && !ai.pathPending) return NavigationStatus.Unreachable;
                if (ai.pathPending) return NavigationStatus.Moving;
                if (!ai.reachedDestination) return NavigationStatus.Moving;
                return NavigationStatus.Arrived;
            }
        }

        public bool PlaceAt(WorldPoint point)
        {
            var pos = new Vector3(point.X, point.Y, point.Z);
            ai.Teleport(pos);
            hasDestination = false;
            return true;
        }

        public bool MoveTo(WorldPoint point)
        {
            hasDestination = true;
            ai.destination = new Vector3(point.X, point.Y, point.Z);
            ai.SearchPath();
            return true;
        }

        public void Stop()
        {
            hasDestination = false;
            if (ai != null && ai.hasPath) ai.SetPath(null);
        }
    }
}
