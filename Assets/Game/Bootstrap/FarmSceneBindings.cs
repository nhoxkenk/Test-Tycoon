using System;
using Farm.Actors;
using Farm.UnityAdapters;
using UnityEngine;
using UnityEngine.UI;

namespace Farm.Bootstrap
{
    // Groups all scene-level references needed by the composition root.
    // Serialized on GameBootstrap; validated once before any side effects.
    [Serializable]
    public sealed class FarmSceneBindings
    {
        [SerializeField] private ActorSceneInstaller actors;
        [SerializeField] private PlotSlotView[] plots;
        [SerializeField] private WalletHudView hud;
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Button upgradeNavButton;

        public ActorSceneInstaller Actors => actors;
        public PlotSlotView[] Plots => plots;
        public WalletHudView Hud => hud;
        public Canvas MainCanvas => mainCanvas;
        public Camera GameCamera => gameCamera;
        public Button UpgradeNavButton => upgradeNavButton;

        public bool Validate(out string error)
        {
            if (actors == null)
            {
                error = "ActorSceneInstaller is not assigned.";
                return false;
            }

            if (plots == null || plots.Length == 0)
            {
                error = "No PlotSlotViews assigned.";
                return false;
            }

            if (mainCanvas == null)
            {
                error = "Main Canvas is not assigned.";
                return false;
            }

            if (gameCamera == null)
            {
                error = "Game Camera is not assigned.";
                return false;
            }

            if (!actors.HasValidMarket)
            {
                error = "Market dock anchors are missing.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
