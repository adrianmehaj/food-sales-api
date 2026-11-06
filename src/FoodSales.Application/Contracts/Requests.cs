using System.ComponentModel.DataAnnotations;
using FoodSales.Domain.Enums;

namespace FoodSales.Application.Contracts;

public sealed record ProductRequest(
    [Required, MaxLength(120)] string Name,
    [Required, MaxLength(1500)] string Description,
    [MaxLength(1000)] string ImageUrl,
    [Required, MaxLength(80)] string Category,
    [Range(typeof(decimal), "0.01", "100000")] decimal Price,
    [Range(typeof(decimal), "0", "1000000")] decimal Stock,
    Unit Unit, bool IsActive, decimal? OfferPrice, DateTime? OfferStartsAt, DateTime? OfferEndsAt,
    string? Version);

public sealed record CustomerRequest(
    [Required, MaxLength(120)] string Name,
    [Required, RegularExpression(@"^\+?[0-9 ()-]{6,25}$")] string Phone,
    [Required, MaxLength(80)] string City,
    [MaxLength(500)] string Address,
    CustomerType Type,
    [Range(0, 365)] int PaymentTermDays, string? Version);

public sealed record OrderItemRequest(Guid ProductId, [Range(typeof(decimal), "0.001", "10000")] decimal Quantity);
public sealed record CreateOrderRequest(
    Guid RequestId,
    [Required, MaxLength(120)] string Name,
    [Required, RegularExpression(@"^\+?[0-9 ()-]{6,25}$")] string Phone,
    [Required, MaxLength(80)] string City,
    [MaxLength(500)] string Address,
    [MaxLength(1000)] string Notes,
    Fulfillment Fulfillment,
    [Required, MinLength(1), MaxLength(50)] List<OrderItemRequest> Items,
    Guid? CustomerId = null, bool CompleteImmediately = false, decimal InitialPayment = 0, bool ApproveCredit = false,
    DateOnly? RequestedDeliveryDate = null);

public sealed record ChangeStatusRequest(OrderStatus Status, [Required] string Version);
public sealed record FinalQuantityRequest(Guid LineId, decimal Quantity, [Required] string Version);
public sealed record CompleteOrderRequest(decimal InitialPayment, bool ApproveCredit, Guid PaymentRequestId, [Required] string Version);
public sealed record PaymentRequest(Guid RequestId, decimal Amount, [MaxLength(300)] string Note);
public sealed record CustomerPriceRequest(Guid ProductId, [Range(typeof(decimal), "0.01", "100000")] decimal Price);
public sealed record BannerRequest([Required, MaxLength(150)] string Title, [Required, MaxLength(500)] string Text,
    [MaxLength(1000)] string ImageUrl, DateTime StartsAt, DateTime EndsAt, bool IsActive,
    [MaxLength(10)] List<string>? ImageUrls = null);
public sealed record LoginRequest([Required, EmailAddress, MaxLength(254)] string Email, [Required, MaxLength(128)] string Password);
public sealed record RegisterRequest([Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(12), MaxLength(128)] string Password);
