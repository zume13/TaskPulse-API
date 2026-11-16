namespace Domain.Aggregates.LoginAudit.Enums;

public enum LoginOutcome
{
    Succeeded = 1,
    InvalidCredentials = 2,
    EmailNotVerified = 3
}
