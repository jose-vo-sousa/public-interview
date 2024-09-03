using System.Collections.Concurrent;
using TinyBank2.Model;

namespace TinyBank2.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ConcurrentBag<Transaction> _transactions = new ConcurrentBag<Transaction>();

        public void AddTransaction(Transaction transaction)
        {
            _transactions.Add(transaction);
        }

        public IEnumerable<Transaction> GetTransactionsByUserId(Guid userId) =>
            _transactions.Where(t => t.OriginAccount == userId || t.DestinationAccount == userId);

        public IEnumerable<Transaction> GetAllTransactions() => _transactions;
    }
}
