using E_Commerce.Application.Orders;
using E_Commerce.Application.Shared;
using E_Commerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace E_Commerce_API.BackgroundServices;

public sealed class CancelExpiredPendingOrdersBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<OrderExpirationCancellationOptions> optionsMonitor,
    ILogger<CancelExpiredPendingOrdersBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IOptionsMonitor<OrderExpirationCancellationOptions> _optionsMonitor = optionsMonitor;
    private readonly ILogger<CancelExpiredPendingOrdersBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelExpiredPendingOrdersOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while cancelling expired pending orders.");
            }

            var interval = TimeSpan.FromSeconds(Math.Max(1, _optionsMonitor.CurrentValue.IntervalSeconds));
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CancelExpiredPendingOrdersOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var nowUtc = DateTimeOffset.UtcNow;

        var expiredPendingOrderIds = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt != null && o.ExpiresAt <= nowUtc)
            .OrderBy(o => o.ExpiresAt)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        if (expiredPendingOrderIds.Count == 0)
        {
            return;
        }

        var cancelledCount = 0;
        foreach (var orderId in expiredPendingOrderIds)
        {
            try
            {
                if (await orderService.CancelAsync(orderId, cancellationToken))
                {
                    cancelledCount++;
                }
            }
            catch (InvalidOperationException)
            {
                //Here the order was cancelled before this iteration
            }
        }

        if (cancelledCount > 0)
        {
            _logger.LogInformation("Cancelled {CancelledCount} expired pending order(s).", cancelledCount);
        }
    }
}

