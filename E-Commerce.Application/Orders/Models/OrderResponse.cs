using E_Commerce.Domain.Orders;

namespace E_Commerce.Application.Orders.Models;

public sealed class OrderResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public OrderStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public required IReadOnlyList<OrderItemResponse> Items { get; init; }

    public decimal Total => Items.Sum(i => i.LineTotal);
}

public sealed class OrderItemResponse
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal => Quantity * UnitPrice;
}

