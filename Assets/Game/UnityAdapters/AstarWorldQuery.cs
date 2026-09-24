using Farm.Actors;
using Pathfinding;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public static class WorldPointExtensions
    {
        public static WorldPoint ToWorldPoint(this Vector3 point) => new WorldPoint(point.x, point.y, point.z);
    }

    public sealed class AstarWorldQuery : IWorldQuery
    {
        public bool TryGetPathLength(WorldPoint origin, WorldPoint destination, out float length)
        {
            length = 0f;
            var astar = AstarPath.active;
            if (astar == null || astar.isScanning || astar.graphs == null || astar.graphs.Length == 0) return false;
            var start = astar.GetNearest(ToVector(origin), new NNConstraint());
            var end = astar.GetNearest(ToVector(destination), new NNConstraint());
            if (start.node == null || end.node == null || !start.node.Walkable || !end.node.Walkable) return false;

            var path = ABPath.Construct((Vector3)start.position, (Vector3)end.position, null);
            AstarPath.StartPath(path);
            AstarPath.BlockUntilCalculated(path);
            if (path.error || path.CompleteState != PathCompleteState.Complete || path.vectorPath == null) return false;
            for (var i = 1; i < path.vectorPath.Count; i++)
                length += Vector3.Distance(path.vectorPath[i - 1], path.vectorPath[i]);
            return true;
        }

        private static Vector3 ToVector(WorldPoint point) => new Vector3(point.X, point.Y, point.Z);
    }
}


