using System.Diagnostics;
using MeterFlow.Domain;

namespace MeterFlow.Simulator;

public static class SyntheticEan
{
    // 999 is not an issued prefix, so these never collide with real connections.
    public static Ean For(int index)
    {
        string body = $"999{index:D14}";
        for (int digit = 0; digit <= 9; digit++)
        {
            if (Ean.TryParse($"{body}{digit}", out var ean))
            {
                return ean;
            }
        }
        throw new UnreachableException();
    }
}
