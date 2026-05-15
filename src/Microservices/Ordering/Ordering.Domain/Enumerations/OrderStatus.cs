namespace Ordering.Domain.Enumerations;

public enum OrderStatus
{
    Submitted = 0,
    InventoryReserved = 1,
    PaymentProcessing = 2,
    Completed = 3,
    Cancelled = 4,
    Faulted = 5
}
