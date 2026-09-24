using UnityEngine;
using UnityEngine.AI;

namespace Farm.Actors
{
    public sealed class NavMeshWorldQuery : IWorldQuery
    {
        private readonly int areaMask;

        public NavMeshWorldQuery(int areaMask = NavMesh.AllAreas)
        {
            this.areaMask = areaMask;
        }

        public bool TryGetPathLength(WorldPoint origin, WorldPoint destination, out float length)
        {
            length = 0f;
            if (!NavMesh.SamplePosition(new Vector3(origin.X, origin.Y, origin.Z), out var start, 1.5f, areaMask) ||
                !NavMesh.SamplePosition(new Vector3(destination.X, destination.Y, destination.Z), out var end, 1.5f, areaMask))
                return false;

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(start.position, end.position, areaMask, path) ||
                path.status != NavMeshPathStatus.PathComplete)
                return false;

            var corners = path.corners;
            for (var i = 1; i < corners.Length; i++)
                length += Vector3.Distance(corners[i - 1], corners[i]);
            return true;
        }
    }
}