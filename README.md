# Tiny Bank Assessment 2.0

## Setup local environment

- Clone repository
- Open powershell in the cloned repository
- Run the project

```
dotnet run --environment Development
```

## Application Swagger documentation

- **SWAGGER**: https://localhost:7273/swagger/index.html

## Raw HTML to navigate in the application

- **RawHtml**: https://localhost:7273/index.html

## Implemented features

- Users can create a new account
  - accounts should be unique, validated by email and phone number
- Users can login after they've created a new account (session cookie will be created with 1 hr TTL)
- Users can logout after they've created a new account (session cookie TTL is set to zero)
- Users can deactivate their account.
  - deactivated accounts can no longer login
- Once logged in, a user can perform any of the listed operations below (authentication & ownership validations in place)
  - Check balance
  - Check transaction history
  - Deposit money
    - Negative values protection is in place
  - Withraw money
    - Negative values protection is in place
  - Transfer money to another account (by account ID)
    - Negative values protection is in place
    - Transfers from/to same account are protected
- Application is logging vast operations, providing useful information
