namespace Domain.Shared.DomainEvent;

public static class DomainEventsNames
{
    public static class UserEventNames
    {
        public const string UserRegistered = "UserRegistered";
        public const string PasswordChanged = "PasswordChanged";
        public const string VerificationEmailRequested = "VerificationEmailRequested";
    }
}