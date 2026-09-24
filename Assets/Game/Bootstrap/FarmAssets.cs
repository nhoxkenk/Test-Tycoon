using System;
using Farm.Actors;
using Farm.UnityAdapters;
using UnityEngine;

namespace Farm.Bootstrap
{
    // Loads all Resources assets needed for a farm session. Single point of load.
    public sealed class FarmAssets
    {
        public readonly GameObject ConstructionUpgradePrefab;
        public readonly GameObject UpgradeSectionPrefab;
        public readonly GameObject UpgradeItemPrefab;
        public readonly GameObject PayEffectPrefab;
        public readonly GameObject BuildDoneEffectPrefab;
        public readonly WorkerActor WorkerPrefab;
        public readonly CustomerActor CustomerPrefab;
        public readonly UpgradeConfig[] UpgradeConfigs;

        private FarmAssets(GameObject constructionUpgrade, GameObject upgradeSection, GameObject upgradeItem,
            GameObject payEffect, GameObject buildDoneEffect, WorkerActor worker, CustomerActor customer,
            UpgradeConfig[] upgrades)
        {
            ConstructionUpgradePrefab = constructionUpgrade;
            UpgradeSectionPrefab = upgradeSection;
            UpgradeItemPrefab = upgradeItem;
            PayEffectPrefab = payEffect;
            BuildDoneEffectPrefab = buildDoneEffect;
            WorkerPrefab = worker;
            CustomerPrefab = customer;
            UpgradeConfigs = upgrades;
        }

        // Load all assets for the given navigation backend. Throws on missing assets.
        public static FarmAssets Load(string backendId)
        {
            string workerPath, customerPath;
            if (backendId == "Astar")
            {
                workerPath = FarmResourcePaths.AstarDeliveryActor;
                customerPath = FarmResourcePaths.AstarCustomerActor;
            }
            else
            {
                workerPath = FarmResourcePaths.DeliveryActor;
                customerPath = FarmResourcePaths.CustomerActor;
            }

            return new FarmAssets(
                LoadPrefab(FarmResourcePaths.ConstructionUpgradeView),
                LoadPrefab(FarmResourcePaths.UpgradeView),
                LoadPrefab(FarmResourcePaths.UpgradeItemView),
                LoadPrefab(FarmResourcePaths.EffPay),
                LoadPrefab(FarmResourcePaths.EffBuildDone),
                LoadActor<WorkerActor>(workerPath),
                LoadActor<CustomerActor>(customerPath),
                LoadUpgradeConfigs()
            );
        }

        private static GameObject LoadPrefab(string key)
        {
            var prefab = Resources.Load<GameObject>(key);
            if (prefab == null) throw new InvalidOperationException("Missing Resources prefab: " + key + ".");
            return prefab;
        }

        private static T LoadActor<T>(string key) where T : Component
        {
            var prefab = LoadPrefab(key);
            var actor = prefab.GetComponent<T>();
            if (actor == null) throw new InvalidOperationException("Resources prefab " + key + " is missing " + typeof(T).Name + ".");
            return actor;
        }

        private static UpgradeConfig[] LoadUpgradeConfigs()
        {
            var configs = Resources.LoadAll<UpgradeConfig>(FarmResourcePaths.Upgrades);
            if (configs.Length != 7)
                throw new InvalidOperationException("Resources/Upgrades must contain exactly seven upgrade configs; found " + configs.Length + ".");
            return configs;
        }
    }
}
