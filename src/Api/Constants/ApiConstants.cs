namespace Api.Constants;

public static class ApiConstants
{
    public static class AuthPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string ManagerOrAbove = "ManagerOrAbove";
    }

    public static class RateLimits
    {
        public const string Anonymous = "anonymous";
    }
}