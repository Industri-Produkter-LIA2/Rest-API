using IPShop.Api.Data;
using IPShop.Api.Dtos;
using IPShop.Api.Models;
using IPShop.Api.Models.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IPShopDbContext _context;

    public OrderController(IPShopDbContext context)
    {
        _context = context;
    }

    // GET: api/order
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return Ok(orders.Select(MapToOrderDto));
    }

    // GET: api/order/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        return Ok(MapToOrderDto(order));
    }

    // GET: api/order/customer/{customerId}
    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByCustomer(int customerId)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
        if (!customerExists)
        {
            return NotFound(new { message = $"Customer with ID {customerId} not found." });
        }

        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return Ok(orders.Select(MapToOrderDto));
    }

    // GET: api/order/customer/{customerId}/updates
    [HttpGet("customer/{customerId:int}/updates")]
    public async Task<ActionResult<IEnumerable<OrderNotificationDto>>> GetOrderUpdatesByCustomer(int customerId)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
        if (!customerExists)
        {
            return NotFound(new { message = $"Customer with ID {customerId} not found." });
        }

        var updates = await _context.OrderNotifications
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new OrderNotificationDto
            {
                Id = n.Id,
                OrderId = n.OrderId,
                CustomerId = n.CustomerId,
                Message = n.Message,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(updates);
    }

    // POST: api/order
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {request.CustomerId} not found." });
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            return BadRequest(new { message = "One or more products do not exist." });
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatuses.Behandlas
        };

        foreach (var item in request.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                LineTotal = product.Price * item.Quantity
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.LineTotal);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var createdOrder = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstAsync(o => o.Id == order.Id);

        return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, MapToOrderDto(createdOrder));
    }

    // PATCH: api/order/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderNotificationDto>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto request)
    {
        if (!OrderStatuses.IsValid(request.Status))
        {
            return BadRequest(new
            {
                message = "Invalid order status.",
                validStatuses = OrderStatuses.All
            });
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} not found." });
        }

        var normalizedStatus = OrderStatuses.All
            .First(s => s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));

        var message = string.IsNullOrWhiteSpace(request.Message)
            ? BuildDefaultStatusMessage(order.OrderNumber, normalizedStatus)
            : request.Message.Trim();

        var notification = await UpdateOrderStatusAndCreateNotification(order, normalizedStatus, message);

        return Ok(new OrderNotificationDto
        {
            Id = notification.Id,
            OrderId = notification.OrderId,
            CustomerId = notification.CustomerId,
            Message = notification.Message,
            CreatedAtUtc = notification.CreatedAtUtc
        });
    }

    private async Task<OrderNotification> UpdateOrderStatusAndCreateNotification(Order order, string newStatus, string message)
    {
        order.Status = newStatus;

        var notification = new OrderNotification
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.OrderNotifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    private static string BuildDefaultStatusMessage(string orderNumber, string status)
    {
        return status switch
        {
            var s when s == OrderStatuses.Behandlas => $"Din beställning {orderNumber} behandlas nu.",
            var s when s == OrderStatuses.Levereras => $"Din beställning {orderNumber} är på väg till butiken.",
            var s when s == OrderStatuses.Levererad => $"Din beställning {orderNumber} är redo för upphämtning.",
            var s when s == OrderStatuses.Fakturerad => $"Din beställning {orderNumber} har fakturerats.",
            _ => $"Status för beställning {orderNumber} uppdaterad till {status}."
        };
    }

    private static OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name ?? string.Empty,
            CreatedAtUtc = order.CreatedAtUtc,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }
}
