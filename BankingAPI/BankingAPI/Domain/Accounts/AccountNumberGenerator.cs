using System.Security.Cryptography;

namespace BankingAPI.Domain.Accounts;

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
