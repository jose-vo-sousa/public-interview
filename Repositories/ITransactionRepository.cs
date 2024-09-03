namespace TinyBank2.Repositories
{
    using TinyBank2.Model;

    public interface ITransactionRepository
    {
        void AddTransaction(Transaction transaction);
        IEnumerable<Transaction> GetTransactionsByUserId(Guid userId);
        IEnumerable<Transaction> GetAllTransactions();
    }
}
