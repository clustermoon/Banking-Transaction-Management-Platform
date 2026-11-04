namespace BankingClient.Api.Contracts;

/// <summary>
/// Contract for requesting a status change on a bank account, including the reason for the change. This record is used to encapsulate the necessary information for initiating a status change operation through the banking API.
/// </summary>
/// <param name="Reason"></param>
public sealed record StatusChangeRequest(string Reason);
