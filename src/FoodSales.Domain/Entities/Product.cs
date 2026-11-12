using FoodSales.Domain.Enums;
using FoodSales.Domain.Exceptions;

namespace FoodSales.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string ImageUrl { get; private set; } = "";
    public string Category { get; private set; } = "";
    public decimal Price
    {
        get; private set;
    }
    public decimal Stock
    {
        get; private set;
    }
    public Unit Unit
    {
        get; private set;
    }
    public bool IsActive { get; private set; } = true;
    public decimal? OfferPrice
    {
        get; private set;
    }
    public DateTime? OfferStartsAt
    {
        get; private set;
    }
    public DateTime? OfferEndsAt
    {
        get; private set;
    }
    public byte[] Version { get; private set; } = [];

    public decimal PriceAt(DateTime now) => OfferPrice.HasValue && now >= OfferStartsAt && now <= OfferEndsAt ? OfferPrice.Value : Price;

    public void Update(string name, string description, string imageUrl, string category, decimal price, decimal stock, Unit unit, bool active,
        decimal? offerPrice, DateTime? starts, DateTime? ends)
    {
        if (string.IsNullOrWhiteSpace(name) || price <= 0 || stock < 0 || !Enum.IsDefined(unit))
            throw new BusinessException("Emri, çmimi ose stoku nuk është i vlefshëm.");
        if (decimal.Round(price, 2) != price || decimal.Round(stock, 3) != stock || (unit == Unit.Piece && stock != decimal.Truncate(stock)))
            throw new BusinessException("Kontrolloni saktësinë e çmimit dhe njësisë së stokut.");
        if (offerPrice.HasValue && (offerPrice <= 0 || offerPrice >= price || decimal.Round(offerPrice.Value, 2) != offerPrice || !starts.HasValue || !ends.HasValue || ends <= starts))
            throw new BusinessException("Oferta duhet të ketë çmim më të ulët dhe periudhë të vlefshme.");
        Name = name.Trim();
        Description = description.Trim();
        ImageUrl = imageUrl.Trim();
        Category = category.Trim();
        Price = price;
        Stock = stock;
        Unit = unit;
        IsActive = active;
        OfferPrice = offerPrice;
        OfferStartsAt = offerPrice.HasValue ? starts : null;
        OfferEndsAt = offerPrice.HasValue ? ends : null;
    }

    public void DeductStock(decimal quantity)
    {
        if (quantity <= 0 || quantity > Stock)
            throw new BusinessException($"Stok i pamjaftueshëm për {Name}.");
        Stock -= quantity;
    }
}
