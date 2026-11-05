namespace BankingClient.Authentication;

/// <summary>
/// A static class that defines the roles used in the banking client application for authorization purposes. The roles include "Customer" for regular users and "Admin" for administrative users, allowing the application to differentiate access levels and permissions based on the user's role.
/// </summary>
public static class ClientRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}
