using System;

namespace Farm.Economy
{
    // Inject this into UI and other consumers that must not change balances.
    public interface IWalletReader
    {
        event Action<Money> BalanceChanged;
        Money GetBalance(CurrencyId currency);
        Money[] GetBalances();
    }

    // Only transaction use cases receive this contract from the composition root.
    public interface IWalletTransactions : IWalletReader
    {
        bool TrySpend(Money cost);
        void Credit(Money amount);
    }
}
