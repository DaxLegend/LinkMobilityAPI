namespace LinkMobilityAPI.Models
{
    public class Entities
    {
    }

    public class Customer
    {
        public string? Id { get; set; }
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? SubscriptionState { get; set; }
        public int NumberOfInvoices { get; set; } = 0;
    }

    public class Invoice
    {
        public string? InvoiceNumber { get; set; }
        public string? Date { get; set; }
        public int Total { get; set; }
        public string? CustomerId { get; set; }
    }

    public class FieldChange
    {
        public string? Field { get; set; }
        public string? Value { get; set; }
    }

    public class CustomerData
    {
        public List<Customer>? Customers { get; set; }
        public int Total { get; set; }
    }

    public class CustomerChanges
    {
        public string? Id { get; set; }
        public List<FieldChange>? Changes { get; set; }
    }

    public class Paginator
    {
        public int First { get; set; }
        public int Rows { get; set; }
    }

}
