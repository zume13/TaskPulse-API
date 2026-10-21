using Domain.Aggregates.User.Enums;
using Domain.Aggregates.User.Errors;
using Domain.Aggregates.User.Events;
using Domain.Aggregates.User.ValueObjects;
using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;
using Domain.Shared.ValueObjects;

namespace Domain.Aggregates.User;

public class User : AggregateRoot
{
    private User() { }
    private User(
        Guid id, Email email, HashedPassword passwordHash, Name firstName, Name lastName, Role role, string verificationToken, DateTime registeredAtUtc) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        VerificationToken = verificationToken;
        RegisteredAtUtc = registeredAtUtc;
    }

    public Email Email { get; private set; } = null!;
    public HashedPassword PasswordHash { get; private set; } = null!;
    public Name FirstName { get; private set; } = null!;
    public Name LastName { get; private set; } = null!;
    public Role Role { get; private set; }
    public string? VerificationToken { get; private set; }
    public DateTime RegisteredAtUtc { get; private set; }
    public bool IsEmailVerified => VerificationToken is null;


    public static ResultT<User> Create(string email, string hashedPassword, string firstName, string lastName, Role role, string verificationToken, DateTime nowUtc)
    {
        var newEmail = Email.Create(email);
        var newPasswordHash = HashedPassword.Create(hashedPassword);
        var newFirstName = Name.Create(firstName);
        var newLastName = Name.Create(lastName);

        if (newEmail.IsFailure)
            return newEmail.Error;

        if (newPasswordHash.IsFailure)
            return newPasswordHash.Error;

        if (newFirstName.IsFailure)
            return newFirstName.Error;

        if (newLastName.IsFailure)
            return newLastName.Error;

        if (!System.Enum.IsDefined(role))
            return UserErrors.InvalidRole;

        if (string.IsNullOrWhiteSpace(verificationToken))
            return GeneralErrors.General.Empty("VerificationToken");

        var user = new User(
            Guid.NewGuid(),
            newEmail.value,
            newPasswordHash.value,
            newFirstName.value,
            newLastName.value,
            role,
            verificationToken,
            nowUtc);

        user.RaiseDomainEvent(new UserRegistered(user.Id, user.Email.Value, user.FirstName.Value, verificationToken));

        return user;
    }

    public Result ChangePassword(string newHashedPassword)
    {
        var newPasswordHash = HashedPassword.Create(newHashedPassword);

        if (newPasswordHash.IsFailure)
            return newPasswordHash.Error;

        PasswordHash = newPasswordHash.value;

        RaiseDomainEvent(new PasswordChanged(Id, Email.Value));

        return Result.Success();
    }

    public Result VerifyEmail(string token)
    {
        if (IsEmailVerified)
            return UserErrors.EmailAlreadyVerified;

        if (string.IsNullOrWhiteSpace(token) || !string.Equals(token, VerificationToken, StringComparison.Ordinal))
            return UserErrors.InvalidVerificationToken;

        VerificationToken = null;

        return Result.Success();
    }

    public Result RefreshVerificationToken(string newVerificationToken)
    {
        if (IsEmailVerified)
            return UserErrors.EmailAlreadyVerified;

        if (string.IsNullOrWhiteSpace(newVerificationToken))
            return GeneralErrors.General.Empty("VerificationToken");

        VerificationToken = newVerificationToken;

        RaiseDomainEvent(new VerificationEmailRequested(Id, Email.Value, FirstName.Value, newVerificationToken));

        return Result.Success();
    }

    public Result ChangeRole(Role newRole)
    {
        if (!System.Enum.IsDefined(newRole))
            return UserErrors.InvalidRole;

        if (Role == newRole)
            return UserErrors.AlreadyInRole;

        Role = newRole;

        return Result.Success();
    }

    // changing email etc in the future
}