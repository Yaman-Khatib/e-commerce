using E_Commerce.Application.Orders.Models;

namespace E_Commerce.Application.Orders;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<OrderResponse?> UpdateAsync(Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(Guid orderId, CancellationToken cancellationToken = default);
}

