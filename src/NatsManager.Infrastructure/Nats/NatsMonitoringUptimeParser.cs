namespace NatsManager.Infrastructure.Nats;

internal static class NatsMonitoringUptimeParser
{
    public static long ParseSeconds(string? uptime)
    {
        if (string.IsNullOrEmpty(uptime))
        {
            return 0;
        }

        long totalSeconds = 0;
        var remaining = uptime.AsSpan();

        while (!remaining.IsEmpty)
        {
            var digitLength = 0;
            while (digitLength < remaining.Length && char.IsDigit(remaining[digitLength]))
            {
                digitLength++;
            }

            if (digitLength == 0 || !long.TryParse(remaining[..digitLength], out var value))
            {
                break;
            }

            remaining = remaining[digitLength..];
            if (remaining.IsEmpty)
            {
                break;
            }

            totalSeconds += remaining[0] switch
            {
                'd' => value * 86_400,
                'h' => value * 3_600,
                'm' => value * 60,
                's' => value,
                _ => 0
            };

            remaining = remaining[1..];
        }

        return totalSeconds;
    }
}
