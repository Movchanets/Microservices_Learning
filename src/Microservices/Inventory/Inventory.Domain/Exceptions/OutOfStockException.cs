namespace Inventory.Domain.Exceptions;

public class OutOfStockException : Exception
{
    public OutOfStockException(string sku, int requested, int available) 
        : base($"Insufficient stock for SKU {sku}. Requested: {requested}, Available: {available}")
    {
    }
}