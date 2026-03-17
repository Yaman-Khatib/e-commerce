namespace E_Commerce_API.BackgroundServices;

public sealed class OrderExpirationCancellationOptions
{
    public int IntervalSeconds { get; init; } = 60;
}

