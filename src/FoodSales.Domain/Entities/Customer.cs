using FoodSales.Domain.Enums;

namespace FoodSales.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId
    {
        get; set;
    }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "Skenderaj";
    public string Address { get; set; } = "";
    public CustomerType Type
    {
        get; set;
    }
    public int PaymentTermDays
    {
        get; set;
    }
    public byte[] Version { get; set; } = [];
}

public sealed class CustomerPrice
{
    public Guid CustomerId
    {
        get; set;
    }
    public Guid ProductId
    {
        get; set;
    }
    public decimal Price
    {
        get; set;
    }
}

public sealed class Banner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public List<string> ImageUrls { get; set; } = [];
    public DateTime StartsAt
    {
        get; set;
    }
    public DateTime EndsAt
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;
}
