using FoodSales.Domain.Enums;
using FoodSales.Domain.Exceptions;

namespace FoodSales.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId
    {
        get; private set;
    }
    public string RequestHash { get; private set; } = "";
    public string? CreatedBy
    {
        get; private set;
    }
    public Guid CustomerId
    {
        get; private set;
    }
    public Customer Customer { get; private set; } = null!;
    public string CustomerName { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string City { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string Notes { get; private set; } = "";
    public Fulfillment Fulfillment
    {
        get; private set;
    }
    public OrderSource Source
    {
        get; private set;
    }
    public OrderStatus Status { get; private set; } = OrderStatus.New;
    public DateTime CreatedAt
    {
        get; private set;
    }
    public DateOnly? RequestedDeliveryDate
    {
        get; private set;
    }
    public DateTime? CompletedAt
    {
        get; private set;
    }
    public DateTime? DueAt
    {
        get; private set;
    }
    public string? CreditApprovedBy
    {
        get; private set;
    }
    public List<OrderLine> Lines { get; private set; } = [];
    public List<Payment> Payments { get; private set; } = [];
    public List<OrderEvent> History { get; private set; } = [];
    public byte[] Version { get; private set; } = [];
    public long Revision
    {
        get; private set;
    }
    public decimal Total => Lines.Sum(x => x.Total);
    public decimal Paid => Payments.Sum(x => x.Amount);
    public decimal Debt => Status == OrderStatus.Completed ? Total - Paid : 0;
    public PaymentStatus PaymentStatus => Paid == 0 ? PaymentStatus.Unpaid : Paid < Total ? PaymentStatus.PartiallyPaid : PaymentStatus.Paid;

    private Order()
    {
    }

    public static Order Create(Customer customer, Guid requestId, string hash, OrderSource source, Fulfillment fulfillment,
        string name, string phone, string city, string address, string notes, string? actor, DateTime now, DateOnly? requestedDeliveryDate = null)
    {
        if (requestId == Guid.Empty || !Enum.IsDefined(fulfillment))
            throw new BusinessException("Porosi e pavlefshme.");
        if (requestedDeliveryDate < DateOnly.FromDateTime(now))
            throw new BusinessException("Data e dërgesës nuk mund të jetë në të kaluarën.");
        var order = new Order
        {
            CustomerId = customer.Id,
            Customer = customer,
            RequestId = requestId,
            RequestHash = hash,
            Source = source,
            Fulfillment = fulfillment,
            CustomerName = name,
            Phone = phone,
            City = city,
            Address = address,
            Notes = notes,
            CreatedBy = actor,
            CreatedAt = now,
            RequestedDeliveryDate = requestedDeliveryDate
        };
        order.History.Add(new OrderEvent(OrderStatus.New, now, actor));
        return order;
    }

    public void AddLine(Product product, decimal quantity, decimal price)
    {
        if (!product.IsActive || quantity > product.Stock || Lines.Any(x => x.ProductId == product.Id))
            throw new BusinessException("Produkti është i padisponueshëm ose i përsëritur.");
        Lines.Add(new OrderLine(product, quantity, price));
    }

    public void SetFinalQuantity(Guid lineId, decimal quantity)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new BusinessException("Porosia është mbyllur.");
        var line = Lines.SingleOrDefault(x => x.Id == lineId) ?? throw new BusinessException("Artikulli nuk u gjet.");
        line.SetFinalQuantity(quantity);
        Revision++;
    }

    public void MoveTo(OrderStatus next, string actor, DateTime now)
    {
        var allowed = next == OrderStatus.Cancelled && Status is not (OrderStatus.Completed or OrderStatus.Cancelled)
            || (Status, next) switch
            {
                (OrderStatus.New, OrderStatus.Confirmed) => true,
                (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
                (OrderStatus.Preparing, OrderStatus.Delivering) => Fulfillment == Fulfillment.Delivery,
                (OrderStatus.Preparing, OrderStatus.ReadyForPickup) => Fulfillment == Fulfillment.Pickup,
                _ => false
            };
        if (!allowed)
            throw new BusinessException("Ky ndryshim i statusit nuk lejohet.");
        Status = next;
        Revision++;
        History.Add(new OrderEvent(next, now, actor));
    }

    public void Complete(string actor, DateTime now, bool approveCredit, int termDays, decimal initialPayment, Guid paymentId)
    {
        if (Lines.Count == 0 || Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new BusinessException("Porosia nuk mund të përfundohet.");
        if (Source == OrderSource.Online && Status is not (OrderStatus.Delivering or OrderStatus.ReadyForPickup))
            throw new BusinessException("Porosia duhet të jetë në dërgesë ose gati për marrje.");
        if (initialPayment < 0 || initialPayment > Total)
            throw new BusinessException("Pagesa duhet të jetë ndërmjet zeros dhe totalit.");
        if (initialPayment < Total && !approveCredit)
            throw new BusinessException("Borxhi kërkon miratimin e adminit.");
        Status = OrderStatus.Completed;
        CompletedAt = now;
        Revision++;
        if (initialPayment < Total)
        {
            CreditApprovedBy = actor;
            DueAt = now.Date.AddDays(termDays);
        }
        History.Add(new OrderEvent(Status, now, actor));
        if (initialPayment > 0)
            AddPayment(initialPayment, paymentId, actor, now, "Pagesë në përfundim");
    }

    public void AddPayment(decimal amount, Guid requestId, string actor, DateTime now, string note)
    {
        var existing = Payments.SingleOrDefault(x => x.RequestId == requestId);
        if (existing != null)
        {
            if (existing.Amount != amount || existing.Note != note)
                throw new BusinessException("Identifikuesi i pagesës është përdorur për një pagesë tjetër.");
            return;
        }
        if (Status != OrderStatus.Completed || amount <= 0 || decimal.Round(amount, 2) != amount || amount > Total - Paid || requestId == Guid.Empty)
            throw new BusinessException("Pagesë e pavlefshme ose më e madhe se borxhi.");
        Payments.Add(new Payment(requestId, amount, now, actor, note));
        Revision++;
    }
}

public sealed class OrderLine
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId
    {
        get; private set;
    }
    public Guid ProductId
    {
        get; private set;
    }
    public string ProductName { get; private set; } = "";
    public Unit Unit
    {
        get; private set;
    }
    public decimal RequestedQuantity
    {
        get; private set;
    }
    public decimal FinalQuantity
    {
        get; private set;
    }
    public decimal UnitPrice
    {
        get; private set;
    }
    public decimal Total
    {
        get; private set;
    }
    private OrderLine()
    {
    }
    internal OrderLine(Product product, decimal quantity, decimal price)
    {
        ProductId = product.Id;
        ProductName = product.Name;
        Unit = product.Unit;
        UnitPrice = price;
        SetFinalQuantity(quantity);
        RequestedQuantity = quantity;
    }
    internal void SetFinalQuantity(decimal quantity)
    {
        if (quantity <= 0 || quantity > 10000 || decimal.Round(quantity, 3) != quantity || (Unit == Unit.Piece && quantity != decimal.Truncate(quantity)))
            throw new BusinessException("Sasia duhet të jetë pozitive; copët duhet të jenë numra të plotë.");
        FinalQuantity = quantity;
        Total = decimal.Round(quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class Payment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId
    {
        get; private set;
    }
    public Guid RequestId
    {
        get; private set;
    }
    public decimal Amount
    {
        get; private set;
    }
    public DateTime ReceivedAt
    {
        get; private set;
    }
    public string ReceivedBy { get; private set; } = "";
    public string Note { get; private set; } = "";
    private Payment()
    {
    }
    internal Payment(Guid requestId, decimal amount, DateTime now, string actor, string note)
    {
        RequestId = requestId;
        Amount = amount;
        ReceivedAt = now;
        ReceivedBy = actor;
        Note = note;
    }
}

public sealed class OrderEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId
    {
        get; private set;
    }
    public OrderStatus Status
    {
        get; private set;
    }
    public DateTime At
    {
        get; private set;
    }
    public string? ActorId
    {
        get; private set;
    }
    private OrderEvent()
    {
    }
    internal OrderEvent(OrderStatus status, DateTime at, string? actor)
    {
        Status = status;
        At = at;
        ActorId = actor;
    }
}
