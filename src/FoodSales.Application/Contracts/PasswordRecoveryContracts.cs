using System.ComponentModel.DataAnnotations;

namespace FoodSales.Application.Contracts;

public sealed record ForgotPasswordRequest([Required, EmailAddress, MaxLength(254)] string Email);
public sealed record ResetPasswordRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MaxLength(4096)] string Token,
    [Required, MinLength(12), MaxLength(128)] string Password);
