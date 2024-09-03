namespace TinyBank2.Services
{
    using TinyBank2.Model;

    /// <summary>
    /// The bank service interface
    /// </summary>
    public interface IBankService
    {
        public User CreateUser(UserCreationRequest userCreationRequest);

        public User? GetUserById(Guid id);

        public User? GetUserByEmail(string email);

        public bool DeactivateUser(Guid id);

        public List<Transaction> GetTransactionHistory(Guid userId);

        public Transaction TransferMoney(Guid fromUserId, TransactionRequest transactionRequest);

        public User AuthenticateUser(string email, string password);

        public Transaction DepositMoney(Guid id, DepositRequest depositRequest);

        public Transaction WithdrawMoney(Guid id, WithdrawRequest withdrawRequest);
    }
}
