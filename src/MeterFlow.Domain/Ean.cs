using System.Diagnostics.CodeAnalysis;

namespace MeterFlow.Domain;

/// <summary>An 18-digit energy connection identifier. An instance is always valid.</summary>
public sealed record Ean
{
    public const int Length = 18;

    private Ean(string value) => Value = value;

    public string Value { get; }

    public static Ean Parse(string value) =>
        TryParse(value, out Ean? ean)
            ? ean
            : throw new FormatException($"'{value}' is not a valid {Length}-digit EAN code.");

    public static bool TryParse([NotNullWhen(true)] string? value, [NotNullWhen(true)] out Ean? ean)
    {
        if (value is null || value.Length != Length)
        {
            ean = null;
            return false;
        }
        for (int i = 0; i < Length; i++)
        {
            if (!char.IsAsciiDigit(value[i]))
            {
                ean = null;
                return false;
            }
        }
        if (value[Length - 1] != CheckDigit(value[..(Length - 1)]))
        {
            ean = null;
            return false;
        }
        ean = new Ean(value);
        return true;
    }

    // GS1 modulo 10: weight the 17 body digits 3 and 1 alternating, then take the
    // amount needed to reach the next multiple of ten.
    private static char CheckDigit(string body)
    {
        int sum = 0;
        for (int i = 0; i < body.Length; i++)
        {
            int digit = body[i] - '0';
            sum += digit * (i % 2 == 0 ? 3 : 1);
        }

        int checkDigit = (10 - sum % 10) % 10;
        return (char)('0' + checkDigit);
    }

    public override string ToString() => Value;
}
