using Domain.Aggregates.User;

namespace Application.Features.Notifications;

internal static class UserNames
{
    public static string DisplayName(User? user) =>
        user is null
            ? "Someone"
            : $"{user.FirstName.Value} {user.LastName.Value}";
}
