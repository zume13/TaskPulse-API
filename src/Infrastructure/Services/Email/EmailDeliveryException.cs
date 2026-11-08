namespace Infrastructure.Services.Mailing;

/// <summary>
/// The email provider refused or could not take the email. The outbox job
/// catches it, stores the message and retries with backoff.
/// </summary>
public sealed class EmailDeliveryException(string message, Exception? innerException = null)
    : Exception(message, innerException);