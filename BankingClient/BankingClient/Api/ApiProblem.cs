namespace BankingClient.Api;

/// <summary>
/// A record that represents a problem returned by the banking API, including a title, optional detail message, and a dictionary of field-specific errors. This record is used to encapsulate validation errors or other issues encountered during API operations, allowing for structured error handling and display in the client application.
/// </summary>
/// <param name="Title"></param>
/// <param name="Detail"></param>
/// <param name="Errors"></param>
public sealed record ApiProblem(string Title, string? Detail, IReadOnlyDictionary<string, string[]> Errors);
