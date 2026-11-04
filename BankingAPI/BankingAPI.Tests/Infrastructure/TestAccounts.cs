namespace BankingAPI.Tests.Infrastructure;

/// <summary>
/// Test account record used for testing purposes.
/// </summary>
/// <param name="Id"></param>
/// <param name="OwnerId"></param>
/// <param name="AccountNumber"></param>
public sealed record TestAccount(Guid Id, Guid OwnerId, string AccountNumber);
