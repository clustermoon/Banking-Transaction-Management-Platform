namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for a login request to the banking API, including the user's email and password. This record is used to encapsulate the necessary information for initiating a login operation through the API.
/// </summary>
/// <param name="Email"></param>
/// <param name="Password"></param>
public sealed record LoginRequest(string Email, string Password);
