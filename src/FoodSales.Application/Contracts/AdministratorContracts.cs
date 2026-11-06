using System.ComponentModel.DataAnnotations;

namespace FoodSales.Application.Contracts;

public sealed record AdministratorView(string Id, string Email);
public sealed record CreateAdministratorRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(12), MaxLength(128)] string Password);
public sealed record GrantAdministratorRequest([Required, EmailAddress, MaxLength(254)] string Email);
