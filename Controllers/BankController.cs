namespace TinyBank2.Controllers
{
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;
    using TinyBank2.Model;
    using TinyBank2.Services;
    using Microsoft.AspNetCore.Authorization;
    using Transaction = Model.Transaction;

    [ApiController]
    [Route("api/")]
    public class BankController : ControllerBase
    {
        private readonly IBankService bankService;
        private readonly ILogger<BankController> logger;

        public BankController(IBankService bankService, ILogger<BankController> logger)
        {
            this.bankService = bankService;
            this.logger = logger;
        }

        [HttpPost]
        [Route("account/create")]
        [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser([FromBody]UserCreationRequest user)
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(CreateUser)} >";

            try
            {
                var createdUser = this.bankService.CreateUser(user);

                var logMessage = $"{messagePrefix} User created. Id:{createdUser.Id} Name:{createdUser.Name} Email:{createdUser.Email} PhoneNumber:{createdUser.PhoneNumber}";
                logger.Log(LogLevel.Information, logMessage);

                return Created($"api/[controller]/{{id}}/balance", createdUser);
            }
            catch (Exception ex)
            {
                var logMessage = $"{messagePrefix} User creation failed. {user}";
                logger.Log(LogLevel.Warning, message: logMessage, exception: ex);

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("account/login")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody]UserLoginRequest userLoginRequest)
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(Login)} >";
            var logMessage = string.Empty;

            var user = bankService.AuthenticateUser(userLoginRequest.Email, userLoginRequest.Password);
            if (user != null)
            {
                if (!user.Active)
                {
                    logMessage = $"{messagePrefix} Tried to login in a deactivated account. Email:{userLoginRequest.Email}.";
                    logger.Log(LogLevel.Warning, logMessage);

                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // The cookie will be persisted across sessions
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                logMessage = $"{messagePrefix} User with Id:{user.Id} has logged in.";
                logger.Log(LogLevel.Information, logMessage);

                return Ok(user);
            }

            logMessage = $"{messagePrefix} Invalid credentials used.";
            logger.Log(LogLevel.Warning, logMessage);

            return StatusCode(StatusCodes.Status400BadRequest);
        }

        [Authorize]
        [HttpPost]
        [Route("account/logout")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(UnauthorizedObjectResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(Logout)} >";
            var logMessage = string.Empty;

            var user = ValidateAuthAndGetUserInfo(messagePrefix);
            if (user == null)
                return Unauthorized();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            logMessage = $"{messagePrefix} User with Id:{user.Id} has logged out.";
            logger.Log(LogLevel.Information, logMessage);

            return NoContent();
        }

        [Authorize]
        [HttpPost]
        [Route("account/deactivate")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UnauthorizedObjectResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Deactivate()
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(Deactivate)} >";
            var logMessage = string.Empty;

            var user = ValidateAuthAndGetUserInfo(messagePrefix);
            if (user == null)
                return Unauthorized();

            try
            {
                var deactivatedUser = this.bankService.DeactivateUser(user.Id);

                if (deactivatedUser)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    logMessage = $"{messagePrefix} User with Id:{user.Id} has been deactivated.";
                    logger.Log(LogLevel.Information, logMessage);

                    return Ok();
                }
                else return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("users/{id}/balance")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UnauthorizedObjectResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBalance([FromRoute] string Id)
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(GetBalance)} >";
            var logMessage = string.Empty;

            var user = ValidateAuthAndGetUserInfo(messagePrefix);
            if (user == null)
                return Unauthorized();

            if (!HasValidOwnership(messagePrefix, user, Id))
                return Forbid();

            return Ok(new { user.AccountBalance });
        }

        [Authorize]
        [HttpGet]
        [Route("users/{id}/history")]
        [ProducesResponseType(typeof(IEnumerable<Transaction>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UnauthorizedObjectResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransactionHistory([FromRoute] string Id)
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(GetTransactionHistory)} >";

            var user = ValidateAuthAndGetUserInfo(messagePrefix);
            if (user == null)
                return Unauthorized();

            if (!HasValidOwnership(messagePrefix, user, Id))
                return Forbid();

            var transactions = bankService.GetTransactionHistory(user.Id);
            return Ok(transactions);
        }

        [Authorize]
        [HttpPost]
        [Route("users/{id}/transfer")]
        [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UnauthorizedObjectResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TransferMoney([FromRoute] string Id, [FromBody] TransactionRequest transactionRequest)
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(TransferMoney)} >";
            var logMessage = string.Empty;

            var user = ValidateAuthAndGetUserInfo(messagePrefix);
            if (user == null)
                return Unauthorized();

            if (!HasValidOwnership(messagePrefix, user, Id))
                return Forbid();

            if (IsSameAccountTransfer(messagePrefix, Id, transactionRequest))
                return Conflict();

            if (!IsPositiveAmount(messagePrefix, transactionRequest.Amount))
                return BadRequest();

            try
            {
                var transaction = bankService.TransferMoney(user.Id, transactionRequest);

                logMessage = $"{messagePrefix} User with Id:{transaction.OriginAccount} has transfered {transaction.Amount} into user with id:{transaction.DestinationAccount}.";
                logger.Log(LogLevel.Information, logMessage);

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                logMessage = $"{messagePrefix} Transfer operation failed.";
                logger.Log(LogLevel.Warning, message: logMessage, exception: ex);

                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("users/{id}/deposit")]
        [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UnauthorizedObjectResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DepositMoney([FromRoute] string Id, [FromBody] DepositRequest depositRequest)
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(DepositMoney)} >";
            var logMessage = string.Empty;

            var user = ValidateAuthAndGetUserInfo(messagePrefix);
            if (user == null)
                return Unauthorized();

            if (!HasValidOwnership(messagePrefix, user, Id))
                return Forbid();

            if (!IsPositiveAmount(messagePrefix, depositRequest.Amount))
                return BadRequest();

            try
            {
                var transaction = bankService.DepositMoney(user.Id, depositRequest);

                logMessage = $"{messagePrefix} User with Id:{user.Id} has deposited {transaction.Amount}.";
                logger.Log(LogLevel.Information, logMessage);

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                logMessage = $"{messagePrefix} Deposit operation failed.";
                logger.Log(LogLevel.Warning, message: logMessage, exception: ex);

                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("users/{id}/withdraw")]
        [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UnauthorizedObjectResult), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Exception), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> WithdrawMoney([FromRoute] string Id, [FromBody] WithdrawRequest withdrawRequest)
        {
            var messagePrefix = $"{nameof(BankController)} > {nameof(WithdrawMoney)} >";
            var logMessage = string.Empty;

            var user = ValidateAuthAndGetUserInfo(messagePrefix);
            if (user == null)
                return Unauthorized();

            if (!HasValidOwnership(messagePrefix, user, Id))
                return Forbid();

            if (!IsPositiveAmount(messagePrefix, withdrawRequest.Amount))
                return BadRequest();

            try
            {
                var transaction = bankService.WithdrawMoney(user.Id, withdrawRequest);

                logMessage = $"{messagePrefix} User with Id:{user.Id} has withdraw {transaction.Amount}.";
                logger.Log(LogLevel.Information, logMessage);

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                logMessage = $"{messagePrefix} Withdraw operation failed.";
                logger.Log(LogLevel.Warning, message: logMessage, exception: ex);

                return BadRequest(ex.Message);
            }
        }

        private User? ValidateAuthAndGetUserInfo(string messagePrefix)
        {
            if (TryGetUser(out var user))
            {
                return user;
            }

            var logMessage = $"{messagePrefix} Not authenticated access attempt.";
            logger.Log(LogLevel.Warning, logMessage);
            return null;
        }

        private bool HasValidOwnership(string messagePrefix, User user, string RequestPathId)
        {
            if (user.Id.ToString().Equals(RequestPathId))
            {
                return true;
            }

            var logMessage = $"{messagePrefix} Request account Id and authenticated Id do not match.";
            logger.Log(LogLevel.Warning, logMessage);

            return false;
        }

        private bool IsSameAccountTransfer(string messagePrefix, string userId, TransactionRequest transactionRequest)
        {
            if (userId.Equals(transactionRequest.DestinationAccount.ToString()))
            {
                var logMessage = $"{messagePrefix} Blocked transfers for same account. Id:{userId}.";
                logger.Log(LogLevel.Warning, logMessage);
                return true;
            }

            return false;
        }

        private bool IsPositiveAmount(string messagePrefix, double amount)
        {
            if (amount > 0)
                return true;

            var logMessage = $"{messagePrefix} Can't have negative amounts for operations.";
            logger.Log(LogLevel.Warning, logMessage);
            return false;
        }

        private bool TryGetUser(out User user)
        {
            user = null;
            
            if (HttpContext?.User?.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    user = bankService.GetUserById(userId);
                    return user != null;
                }
            }

            return false;
        }
    }
}
