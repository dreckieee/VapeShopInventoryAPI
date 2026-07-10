public class UpdateExpenseRequest
{
    public required string Description {get; set;}
    public decimal Amount {get; set;}
    public required string Category {get; set;}
    public DateTime Date {get; set;}
}