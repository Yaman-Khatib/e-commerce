using E_Commerce.Domain.Products;

namespace E_Commerce.Domain.Orders;

public class OrderItem
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    private OrderItem()
    {
    }

    public OrderItem(Guid id, Guid orderId, Guid productId, int quantity, decimal unitPrice)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id must be a non-empty GUID.", nameof(id));

        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId must be a non-empty GUID.", nameof(orderId));

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must be a non-empty GUID.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        Id = id;
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

