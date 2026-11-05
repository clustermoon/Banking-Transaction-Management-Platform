namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for registering a new user with the banking API, including the user's email, password, and display name. This record is used to encapsulate the necessary information for creating a new user account through the API.
/// </summary>
/// <param name="Email"></param>
/// <param name="Password"></param>
/// <param name="DisplayName"></param>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
