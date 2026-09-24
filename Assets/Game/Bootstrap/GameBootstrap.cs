using Farm.Economy;
using Farm.UnityAdapters;
using UnityEngine;

namespace Farm.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string initialBalance = "0";
        [SerializeField] private ActorSceneInstaller actors;

        private Wallet wallet;

        private void Awake()
        {
            if (!Money.TryParse(CurrencyId.Coin, initialBalance, out var money))
            {
                Debug.LogError("Initial balance must be a non-negative integer.", this);
                enabled = false;
                return;
            }

            wallet = new Wallet(money);
            if (actors == null || !actors.Initialize())
            {
                Debug.LogError("Actor scene could not be initialized.", this);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            actors?.Dispose();
            wallet = null;
        }
    }
}
