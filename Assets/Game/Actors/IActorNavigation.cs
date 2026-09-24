namespace Farm.Actors
{
    public readonly struct WorldPoint
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public WorldPoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public enum NavigationStatus
    {
        Idle,
        Moving,
        Arrived,
        Unreachable
    }

    public interface IActorNavigation
    {
        NavigationStatus Status { get; }
        bool PlaceAt(WorldPoint point);
        bool MoveTo(WorldPoint point);
        void Stop();
    }

    public interface IWorldQuery
    {
        bool TryGetPathLength(WorldPoint origin, WorldPoint destination, out float length);
    }
}
