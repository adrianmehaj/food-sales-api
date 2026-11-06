namespace FoodSales.Domain.Enums;

public enum Unit { Kilogram, Liter, Piece }
public enum CustomerType { Individual, Business }
public enum OrderSource { Online, Manual }
public enum Fulfillment { Delivery, Pickup }
public enum OrderStatus { New, Confirmed, Preparing, Delivering, ReadyForPickup, Completed, Cancelled }
public enum PaymentStatus { Unpaid, PartiallyPaid, Paid }
