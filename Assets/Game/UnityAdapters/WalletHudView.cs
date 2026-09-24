using Farm.Economy;
using TMPro;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class WalletHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text gemText;
        [SerializeField] private TMP_Text coinText;

        private IWalletReader wallet;

        public void Bind(IWalletReader reader)
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            wallet = reader;
            wallet.BalanceChanged += OnBalanceChanged;
            gemText.text = wallet.GetBalance(CurrencyId.Gem).ToString();
            coinText.text = wallet.GetBalance(CurrencyId.Coin).ToString();
        }

        private void OnBalanceChanged(Money balance)
        {
            if (balance.Currency == CurrencyId.Gem) gemText.text = balance.ToString();
            if (balance.Currency == CurrencyId.Coin) coinText.text = balance.ToString();
        }

        public void Unbind()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            wallet = null;
        }

        private void OnDestroy() => Unbind();
    }
}
