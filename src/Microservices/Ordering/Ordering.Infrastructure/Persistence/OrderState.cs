using MassTransit;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
