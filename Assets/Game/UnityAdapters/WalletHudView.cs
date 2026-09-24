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
            gemText.text = MoneyDisplayFormatter.FormatHud(wallet.GetBalance(CurrencyId.Gem));
            coinText.text = MoneyDisplayFormatter.FormatHud(wallet.GetBalance(CurrencyId.Coin));
        }

        private void OnBalanceChanged(Money balance)
        {
            if (balance.Currency == CurrencyId.Gem) gemText.text = MoneyDisplayFormatter.FormatHud(balance);
            if (balance.Currency == CurrencyId.Coin) coinText.text = MoneyDisplayFormatter.FormatHud(balance);
        }

        public void Unbind()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            wallet = null;
        }

        private void OnDestroy() => Unbind();
    }
}
