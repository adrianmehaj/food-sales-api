namespace FoodSales.Application.Common;

public sealed class NotFoundException : Exception;
public sealed class ConflictException(string message) : Exception(message);
public sealed class ForbiddenException(string message) : Exception(message);
public sealed class AccountException(string message, bool unauthorized = false) : Exception(message)
{
    public bool Unauthorized { get; } = unauthorized;
}

public sealed class PaginationOptions
{
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
    public (int Page, int Size) Normalize(int page, int? size) => (Math.Max(1, Math.Min(page, 100000)), Math.Clamp(size ?? DefaultPageSize, 1, MaxPageSize));
}
