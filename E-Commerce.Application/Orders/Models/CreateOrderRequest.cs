namespace E_Commerce.Application.Orders.Models;

public sealed class CreateOrderRequest
{
    public required IReadOnlyList<CreateOrderItemRequest> Items { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed class CreateOrderItemRequest
{
    public required Guid ProductId { get; init; }

    public required int Quantity { get; init; }
}

