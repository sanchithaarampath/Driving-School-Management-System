namespace DSMS.API.Services;

public interface IReceiptService
{
    string GenerateReceiptHtml(ReceiptData data);
    string GenerateReceiptText(ReceiptData data);
}

public class ReceiptData
{
    public string BillNumber { get; set; } = "";
    public DateTime BillDate { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentNic { get; set; } = "";
    public string StudentPhone { get; set; } = "";
    public string? StudentEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
}
