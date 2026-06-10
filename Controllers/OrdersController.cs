using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Order history — view and manage completed product orders.</summary>
[ApiController]
[Route("orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>List every order in the system. (Admin only)</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of all orders, newest first.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var paged = await _db.Orders
            .Include(o => o.Product)
            .Include(o => o.CustomerProfile).ThenInclude(cp => cp.User)
            .OrderByDescending(o => o.CreatedAt)
            .PaginateAsync(page, limit);

        return Ok(paged.MapData(MapOrder));
    }

    /// <summary>Retrieve a specific order by ID. (Admin only)</summary>
    /// <param name="id">Order ID.</param>
    /// <response code="200">Order found and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No order with the given ID.</response>
    [HttpGet("admin/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByIdAdmin(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Product)
            .Include(o => o.CustomerProfile).ThenInclude(cp => cp.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        return Ok(MapOrder(order));
    }

    /// <summary>List all orders belonging to the currently authenticated customer.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of the customer's orders, newest first.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Customer profile not found.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var profile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (profile is null)
            return NotFound(new { message = "Customer profile not found." });

        var paged = await _db.Orders
            .Where(o => o.CustomerProfileId == profile.Id)
            .Include(o => o.Product)
            .Include(o => o.CustomerProfile).ThenInclude(cp => cp.User)
            .OrderByDescending(o => o.CreatedAt)
            .PaginateAsync(page, limit);

        return Ok(paged.MapData(MapOrder));
    }

    /// <summary>Retrieve a specific order that belongs to the authenticated customer.</summary>
    /// <param name="id">Order ID.</param>
    /// <response code="200">Order found and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Order not found or does not belong to this customer.</response>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyOrderById(int id)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var profile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (profile is null)
            return NotFound(new { message = "Customer profile not found." });

        var order = await _db.Orders
            .Where(o => o.CustomerProfileId == profile.Id)
            .Include(o => o.Product)
            .Include(o => o.CustomerProfile).ThenInclude(cp => cp.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        return Ok(MapOrder(order));
    }

    /// <summary>Delete an order. Customers may only delete their own; Admins may delete any.</summary>
    /// <param name="id">Order ID.</param>
    /// <response code="200">Order deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Order does not belong to this customer.</response>
    /// <response code="404">No order with the given ID.</response>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var order = await _db.Orders
            .Include(o => o.CustomerProfile).ThenInclude(cp => cp.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin)
        {
            var profile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (profile is null || order.CustomerProfileId != profile.Id)
                return Forbid();
        }

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Order deleted." });
    }

    private static OrderResponseDto MapOrder(Order order) => new()
    {
        Id = order.Id,
        Quantity = order.Quantity,
        PurchasePrice = order.PurchasePrice,
        TotalPrice = order.PurchasePrice * order.Quantity,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Product = new OrderProductDetailsDto
        {
            Id = order.Product.Id,
            Name = order.Product.Name,
            Summary = order.Product.Summary,
            Image = order.Product.Image,
            Price = order.Product.Price,
            DiscountedPrice = order.Product.DiscountedPrice,
            Quantity = order.Quantity,
            Total = order.PurchasePrice * order.Quantity
        },
        Customer = new OrderCustomerDetailsDto
        {
            CustomerProfileId = order.CustomerProfile.Id,
            Name = order.CustomerProfile.User?.Name,
            Email = order.CustomerProfile.User?.Email,
            Phone = order.CustomerProfile.User?.Phone
        }
    };
}
