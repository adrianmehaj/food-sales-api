using System.ComponentModel.DataAnnotations;

namespace FoodSales.Application.Contracts;

public sealed record ProfileView(string Name, string Email, string Phone, string City, string Address, string? Version);
public sealed record UpdateProfileRequest(
    [Required, MaxLength(120)] string Name,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, Phone, MaxLength(25)] string Phone,
    [Required, MaxLength(80)] string City,
    [Required(AllowEmptyStrings = true), MaxLength(500)] string Address,
    [MaxLength(128)] string? CurrentPassword,
    string? Version);
