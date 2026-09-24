using System;
using Farm.Economy;
using Farm.Farming;

namespace Farm.Simulation
{
    public sealed class MarketSaleService
    {
        private readonly HarvestService harvest;
        private readonly IWalletTransactions wallet;

        public event Action<HarvestBatch> SaleCommitted;

        public MarketSaleService(HarvestService harvest, IWalletTransactions wallet)
        {
            this.harvest = harvest ?? throw new ArgumentNullException(nameof(harvest));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public bool TrySell(int workerId, int customerId, Func<int, int, int, bool> confirmTransfer)
        {
            if (confirmTransfer == null) throw new ArgumentNullException(nameof(confirmTransfer));
            if (!harvest.TryGetBatch(workerId, out var batch)) return false;
            if (!confirmTransfer(workerId, customerId, batch.Quantity)) return false;
            if (!harvest.RemoveBatch(workerId, batch.Id))
                throw new InvalidOperationException("Confirmed batch was already consumed.");
            wallet.Credit(batch.SaleValue);
            SaleCommitted?.Invoke(batch);
            return true;
        }
    }
}
