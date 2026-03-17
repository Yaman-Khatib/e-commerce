using E_Commerce.Application.Orders.Models;
using E_Commerce.Application.Shared;
using E_Commerce.Domain.Orders;
using E_Commerce.Domain.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace E_Commerce.Application.Orders;

public sealed class OrderService(IApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor) : IOrderService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.", nameof(request));
        }

        if (request.Items.Any(i => i.Quantity <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "All item quantities must be greater than zero.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        ValidateExpiresAt(request.ExpiresAt, nowUtc);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var itemsByProductId = request.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            var lockedProducts = new Dictionary<Guid, Product>(itemsByProductId.Count);

            foreach (var productId in itemsByProductId.Keys.OrderBy(id => id))
            {
                var product = await LockProductForUpdateAsync(productId, cancellationToken);
                if (product is null)
                {
                    throw new KeyNotFoundException($"Product not found: {productId}");
                }

                var qty = itemsByProductId[productId];
                product.DecreaseStock(qty);
                lockedProducts[productId] = product;
            }

            var order = new Order(
                Guid.NewGuid(),
                userId.Value,
                createdAt: nowUtc,
                expiresAt: request.ExpiresAt);

            foreach (var productId in itemsByProductId.Keys.OrderBy(id => id))
            {
                var product = lockedProducts[productId];

                var item = new OrderItem(
                    Guid.NewGuid(),
                    order.Id,
                    productId,
                    itemsByProductId[productId],
                    unitPrice: product.Price);

                order.AddItem(item);
            }

            await _dbContext.Orders.AddAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await GetRequiredByIdAsync(order.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await ProjectToOrderResponse(_dbContext.Orders.AsNoTracking())
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await ProjectToOrderResponse(_dbContext.Orders.AsNoTracking())
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderResponse?> UpdateAsync(Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await _dbContext.Orders
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var hasExpiresChange = request.ExpiresAt != order.ExpiresAt;
            var hasItemsToAdd = request.ItemsToAdd is { Count: > 0 };

            if ((hasExpiresChange || hasItemsToAdd) && order.Status != OrderStatus.Pending)
            {
                throw new InvalidOperationException("Only pending orders can be updated.");
            }

            if (hasExpiresChange)
            {
                ValidateExpiresAt(request.ExpiresAt, DateTimeOffset.UtcNow);
                order.SetExpiresAt(request.ExpiresAt);
            }

            if (hasItemsToAdd)
            {
                if (request.ItemsToAdd!.Any(i => i.Quantity <= 0))
                {
                    throw new ArgumentOutOfRangeException(nameof(request), "All item quantities must be greater than zero.");
                }

                var quantityByProductId = request.ItemsToAdd!
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

                var lockedProducts = new Dictionary<Guid, Product>(quantityByProductId.Count);

                foreach (var productId in quantityByProductId.Keys.OrderBy(id => id))
                {
                    var product = await LockProductForUpdateAsync(productId, cancellationToken);
                    if (product is null)
                    {
                        throw new KeyNotFoundException($"Product not found: {productId}");
                    }

                    product.DecreaseStock(quantityByProductId[productId]);
                    lockedProducts[productId] = product;
                }

                foreach (var productId in quantityByProductId.Keys.OrderBy(id => id))
                {
                    var product = lockedProducts[productId];

                    var item = new OrderItem(
                        Guid.NewGuid(),
                        order.Id,
                        productId,
                        quantityByProductId[productId],
                        unitPrice: product.Price);

                    order.AddItem(item);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(orderId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> CancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await _dbContext.Orders
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new InvalidOperationException("Only pending orders can be cancelled.");
            }

            var quantityByProductId = order.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
            // Even though ids are GUIDs, we order them for deadlock prevention:
            // each transaction accesses products in the same order (no circular dependency).
            foreach (var productId in quantityByProductId.Keys.OrderBy(id => id))
            {
                var product = await LockProductForUpdateAsync(productId, cancellationToken);
                if (product is null)
                {
                    throw new KeyNotFoundException($"Product not found: {productId}");
                }

                product.IncreaseStock(quantityByProductId[productId]);
            }

            order.MarkCancelled();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private Guid? GetCurrentUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is null)
        {
            return null;
        }

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirstValue(ClaimTypes.Name)
                         ?? principal.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task<OrderResponse> GetRequiredByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException();
        }

        return order;
    }

    private static void ValidateExpiresAt(DateTimeOffset? expiresAt, DateTimeOffset nowUtc)
    {
        if (expiresAt is null)
        {
            return;
        }

        if (expiresAt <= nowUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "ExpiresAt must be in the future.");
        }
    }

    private Task<Product?> LockProductForUpdateAsync(Guid productId, CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .FromSqlInterpolated($@"SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {productId}")
            .SingleOrDefaultAsync(cancellationToken);
    }
    private static IQueryable<OrderResponse> ProjectToOrderResponse(IQueryable<Order> query) =>
        query
            .Select(o => new OrderResponse
            {
                Id = o.Id,
                UserId = o.UserId,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                ExpiresAt = o.ExpiresAt,
                Items = o.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new OrderItemResponse
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    })
                    .ToList()
            });
}

