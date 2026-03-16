using E_Commerce.Domain.Users;

namespace E_Commerce.Domain.Orders;

public class Order
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
    }

    public Order(Guid id, Guid userId, DateTimeOffset createdAt, DateTimeOffset? expiresAt = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id must be a non-empty GUID.", nameof(id));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must be a non-empty GUID.", nameof(userId));

        Id = id;
        UserId = userId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        Status = OrderStatus.Pending;
    }

    public void AddItem(OrderItem item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (item.OrderId != Id)
            throw new InvalidOperationException("Order item OrderId must match order Id.");

        _items.Add(item);
    }

    public void SetExpiresAt(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    public void MarkProcessing()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be marked as processing.");

        Status = OrderStatus.Processing;
    }

    public void MarkCompleted()
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException("Only processing orders can be completed.");

        Status = OrderStatus.Completed;
    }

    public void MarkCancelled()
    {
        if (Status is OrderStatus.Completed or OrderStatus.Failed)
            throw new InvalidOperationException("Completed or failed orders cannot be cancelled.");

        Status = OrderStatus.Cancelled;
    }

    public void MarkFailed()
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Completed orders cannot be marked as failed.");

        Status = OrderStatus.Failed;
    }
}

