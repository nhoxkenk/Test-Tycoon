using System;
using Farm.Actors;
using Pathfinding;
using UnityEngine;

namespace Farm.Actors
{
    public sealed class AstarActorNavigation : IActorNavigation, IDisposable
    {
        private const float MaxAnchorOffset = 0.3f;
        private readonly AIPath ai;
        private readonly Seeker seeker;
        private bool requested;
        private bool failed;
        private bool disposed;

        public AstarActorNavigation(AIPath ai)
        {
            this.ai = ai;
            seeker = ai != null ? ai.GetComponent<Seeker>() : null;
            if (seeker != null) seeker.pathCallback += OnPathComplete;
        }

        public NavigationStatus Status
        {
            get
            {
                if (!requested) return NavigationStatus.Idle;
                if (failed) return NavigationStatus.Unreachable;
                if (ai == null || ai.pathPending) return NavigationStatus.Moving;
                if (!ai.hasPath) return NavigationStatus.Unreachable;
                return ai.reachedDestination ? NavigationStatus.Arrived : NavigationStatus.Moving;
            }
        }

        public bool PlaceAt(WorldPoint point)
        {
            if (!TryGetGroundedAnchor(point, out var anchor)) return false;
            ai.Teleport(anchor, true);
            ai.isStopped = true;
            requested = false;
            failed = false;
            return true;
        }

        public bool MoveTo(WorldPoint point)
        {
            if (!TryGetGroundedAnchor(point, out var anchor)) return Fail();
            ai.destination = anchor;
            ai.isStopped = false;
            requested = true;
            failed = false;
            ai.SearchPath();
            return true;
        }

        public void Stop()
        {
            if (ai != null) ai.isStopped = true;
            requested = false;
            failed = false;
        }

        public void Dispose()
        {
            if (disposed) return;
            if (seeker != null) seeker.pathCallback -= OnPathComplete;
            disposed = true;
        }

        private void OnPathComplete(Path path)
        {
            if (requested && path.CompleteState != PathCompleteState.Complete) failed = true;
        }

        private bool TryGetGroundedAnchor(WorldPoint point, out Vector3 anchor)
        {
            anchor = default;
            if (ai == null || AstarPath.active == null) return false;
            var requested = ToVector(point);
            var nearest = AstarPath.active.GetNearest(requested, NNConstraint.Default);
            if (nearest.node == null || !nearest.node.Walkable) return false;
            var onGraph = (Vector3)nearest.position;
            if (Vector2.Distance(new Vector2(requested.x, requested.z), new Vector2(onGraph.x, onGraph.z)) > MaxAnchorOffset)
                return false;
            anchor = new Vector3(requested.x, onGraph.y, requested.z);
            return true;
        }

        private bool Fail() { requested = true; failed = true; return false; }
        private static Vector3 ToVector(WorldPoint point) => new Vector3(point.X, point.Y, point.Z);
    }
}
