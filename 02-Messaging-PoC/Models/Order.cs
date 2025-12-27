namespace MessagingPoC.Models;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}

public enum OrderStatus
{
    Created,
    Shipped,
    Cancelled
}

public record CreateOrderRequest(
    string CustomerName,
    List<string> Items,
    decimal TotalAmount
);
