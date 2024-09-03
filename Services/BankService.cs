namespace TinyBank2.Services
{
    using TinyBank2.Mappers;
    using TinyBank2.Model;
    using TinyBank2.Repositories;

    public class BankService : IBankService
    {
        // Memory data for application to work
        private readonly IUserRepository userRepository;
        private readonly ITransactionRepository transactionRepository;
        private readonly ILogger<BankService> logger;

        public BankService(
            ILogger<BankService> logger,
            IUserRepository userRepository,
            ITransactionRepository transactionRepository)
        {
            this.logger = logger;
            this.userRepository = userRepository;
            this.transactionRepository = transactionRepository;
        }

        public User? GetUserById(Guid id)
        {
            return this.userRepository.GetUserById(id);
        }

        public User? GetUserByEmail(string email)
        {
            return this.userRepository.GetUserByEmail(email);
        }

        public bool DeactivateUser(Guid id)
        {
            var user = this.userRepository.GetUserById(id);
            if (user != null && user.Active)
            {
                user.Active = false;
                this.userRepository.UpdateUser(user);
                return true;
            }

            return false;
        }

        public IEnumerable<Transaction> GetTransactionHistory(Guid userId)
        {
            return this.transactionRepository.GetTransactionsByUserId(userId);
        }

        public User AuthenticateUser(string email, string password)
        {
            var user = this.userRepository.GetUserByEmail(email);
            if (user != null && user.Password == password)
                return user;

            return null;
        }

        public User CreateUser(UserCreationRequest userCreationRequest)
        {
            if (!this.userRepository.UserExists(userCreationRequest.Email, userCreationRequest.PhoneNumber))
            {
                var user = userCreationRequest.ToUser();
                this.userRepository.AddUser(user);
                return user;
            }

            var messagePrefix = $"{nameof(BankService)} > {nameof(CreateUser)} >";
            var accountAlreadyExistsMessage = $"{messagePrefix} Email '{userCreationRequest.Email}' or phone number '{userCreationRequest.PhoneNumber}' already in use.";
            this.LogWarning(accountAlreadyExistsMessage);

            // We don't want to expose PII in the exceptions, so it will leave with generic message
            throw new Exception("Something went wrong creating with the account creation process.");
        }

        public Transaction TransferMoney(Guid fromUserId, TransactionRequest transactionRequest)
        {
            var fromUser = GetAccountByUserId(fromUserId);
            var toUser = GetAccountByUserId(transactionRequest.DestinationAccount);

            ValidateAccountFunds(fromUser, transactionRequest.Amount);
            
            fromUser.AccountBalance -= transactionRequest.Amount;
            toUser.AccountBalance += transactionRequest.Amount;

            this.userRepository.UpdateUser(toUser);
            this.userRepository.UpdateUser(fromUser);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Amount = transactionRequest.Amount,
                Description = string.IsNullOrWhiteSpace(transactionRequest.Description) ? "Transfer" : transactionRequest.Description,
                OriginAccount = fromUserId,
                DestinationAccount = transactionRequest.DestinationAccount
            };

            this.transactionRepository.AddTransaction(transaction);
            return transaction;
        }

        public Transaction DepositMoney(Guid id, DepositRequest depositRequest)
        {
            var userAccount = GetAccountByUserId(id);

            userAccount.AccountBalance += depositRequest.Amount;
            this.userRepository.UpdateUser(userAccount);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Amount = depositRequest.Amount,
                DestinationAccount = id,
                OriginAccount = Guid.Empty,
                Description = "Deposit"
            };

            this.transactionRepository.AddTransaction(transaction);
            return transaction;
        }

        public Transaction WithdrawMoney(Guid id, WithdrawRequest withdrawRequest)
        {
            var userAccount = GetAccountByUserId(id);

            ValidateAccountFunds(userAccount, withdrawRequest.Amount);

            userAccount.AccountBalance -= withdrawRequest.Amount;
            this.userRepository.UpdateUser(userAccount);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Amount = withdrawRequest.Amount,
                DestinationAccount = Guid.Empty,
                OriginAccount = id,
                Description = "Withdraw"
            };

            this.transactionRepository.AddTransaction(transaction);
            return transaction;
        }

        private void LogWarning(string message)
        {
            logger.Log(LogLevel.Warning, message);
        }

        private User GetAccountByUserId(Guid id)
        {
            var messagePrefix = $"{nameof(BankService)} > {nameof(GetAccountByUserId)} >";
            var invalidUserIdMessage = $"{messagePrefix} Invalid account id '{id}'.";
            var accountDoesNotExistMessage = $"{messagePrefix} Account do not exist for the user with id '{id}'.";

            if (id == Guid.Empty)
            {
                this.LogWarning(invalidUserIdMessage);
                throw new Exception($"Invalid User Id.");
            }

            var user = this.userRepository.GetUserById(id);
            if (user == null)
            {
                this.LogWarning(accountDoesNotExistMessage);
                throw new Exception("Account do not exist.");
            }

            return user;
        }

        private void ValidateAccountFunds(User userAccount, double amount)
        {
            if (userAccount.AccountBalance >= amount)
                return;

            var messagePrefix = $"{nameof(BankService)} > {nameof(ValidateAccountFunds)} >";
            var insufficientFundsMessage = $"{messagePrefix} Insufficient funds in the account with id '{userAccount.Id}'.";

            this.LogWarning(insufficientFundsMessage);
            throw new Exception("Insufficient funds.");
        }
    }
}
