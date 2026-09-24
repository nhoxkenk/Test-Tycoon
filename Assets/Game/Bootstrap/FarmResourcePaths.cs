namespace Farm.Bootstrap
{
    // Centralized Resources paths for all assets loaded at composition time.
    public static class FarmResourcePaths
    {
        // UI prefabs
        public const string ConstructionUpgradeView = "Farm/UI/ConstructionUpgradeView";
        public const string UpgradeView = "Farm/UI/UpgradeView";
        public const string UpgradeItemView = "Farm/UI/UpgradeItemView";

        // Effects
        public const string EffPay = "Farm/Effects/EffPay";
        public const string EffBuildDone = "Farm/Effects/EffBuildDone";

        // Actors — NavMesh (default)
        public const string DeliveryActor = "Farm/Actors/Delivery";
        public const string CustomerActor = "Farm/Actors/Customer";

        // Actors — A* variants
        public const string AstarDeliveryActor = "Farm/Actors/Astar/Delivery";
        public const string AstarCustomerActor = "Farm/Actors/Astar/Customer";

        // Upgrade configs
        public const string Upgrades = "Upgrades";
    }
}
