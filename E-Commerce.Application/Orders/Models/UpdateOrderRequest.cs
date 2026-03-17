namespace E_Commerce.Application.Orders.Models;

public sealed class UpdateOrderRequest
{
    /// <summary>
    /// Adds items to an existing order (it is neesecary for Pending orders).
    /// </summary>
    public IReadOnlyList<CreateOrderItemRequest>? ItemsToAdd { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}

