using System.Security.Cryptography;

namespace BankingAPI.Domain.Accounts;

/// <summary>
/// Random 10-digit account numbers. Random rather than sequential, so a number reveals
/// nothing about how many accounts exist and its neighbors can't be guessed. The unique
/// index guarantees no two accounts share one; at this scale a collision is negligible,
/// and one would fail the request rather than create a duplicate.
/// </summary>
public static class AccountNumberGenerator
{
    public const int Length = 10;

    public static string Generate()
    {
        Span<char> digits = stackalloc char[Length];

        digits[0] = (char)('1' + RandomNumberGenerator.GetInt32(9));
        for (var index = 1; index < Length; index++)
        {
            digits[index] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        }

        return new string(digits);
    }
}
