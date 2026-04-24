using Microsoft.Extensions.Configuration;

namespace DSMS.API.Services;

public class ReceiptService : IReceiptService
{
    private readonly IConfiguration _config;

    public ReceiptService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateReceiptHtml(ReceiptData data)
    {
        var school = _config.GetSection("SchoolInfo");
        var schoolName = school["Name"] ?? "Arampath Driving School";
        var schoolAddress = school["Address"] ?? "";
        var schoolPhone = school["Phone"] ?? "";
        var schoolEmail = school["Email"] ?? "";

        var statusColor = data.Status == "Paid" ? "#3fb950" : data.Status == "Partial" ? "#d29922" : "#e63946";
        var paidColor = data.PaidAmount > 0 ? "#3fb950" : "#8b949e";

        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    * {{ margin:0; padding:0; box-sizing:border-box; }}
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background:#f5f5f5; color:#222; }}
    .receipt {{ max-width:620px; margin:30px auto; background:#fff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.12); }}
    .header {{ background:linear-gradient(135deg,#e63946,#c1121f); color:#fff; padding:28px 32px; text-align:center; }}
    .header .logo {{ font-size:2.5rem; margin-bottom:6px; }}
    .header h1 {{ font-size:1.4rem; font-weight:700; margin-bottom:4px; }}
    .header p {{ font-size:0.85rem; opacity:0.9; }}
    .receipt-number {{ background:#f8f8f8; border-bottom:2px dashed #ddd; padding:16px 32px; display:flex; justify-content:space-between; align-items:center; }}
    .receipt-number .label {{ color:#666; font-size:0.85rem; text-transform:uppercase; letter-spacing:0.5px; }}
    .receipt-number .value {{ font-size:1.1rem; font-weight:700; color:#222; }}
    .status-badge {{ padding:4px 12px; border-radius:20px; font-size:0.8rem; font-weight:700; color:#fff; background:{statusColor}; }}
    .body {{ padding:28px 32px; }}
    .section {{ margin-bottom:22px; }}
    .section h3 {{ font-size:0.8rem; text-transform:uppercase; letter-spacing:0.8px; color:#999; margin-bottom:10px; border-bottom:1px solid #eee; padding-bottom:6px; }}
    .info-grid {{ display:grid; grid-template-columns:1fr 1fr; gap:8px; }}
    .info-item .key {{ font-size:0.78rem; color:#999; margin-bottom:2px; }}
    .info-item .val {{ font-size:0.9rem; font-weight:600; color:#222; }}
    .amounts-table {{ width:100%; border-collapse:collapse; margin-top:4px; }}
    .amounts-table tr td {{ padding:8px 0; border-bottom:1px solid #f0f0f0; font-size:0.9rem; }}
    .amounts-table tr td:last-child {{ text-align:right; font-weight:600; }}
    .amounts-table .total-row td {{ border-top:2px solid #e63946; border-bottom:none; padding-top:12px; font-size:1rem; font-weight:700; color:#e63946; }}
    .amounts-table .paid-row td {{ color:{paidColor}; }}
    .amounts-table .balance-row td {{ color:#d29922; }}
    .payment-info {{ background:#f8fff8; border:1px solid #d4edda; border-radius:8px; padding:14px 16px; margin-top:8px; }}
    .payment-info .pm {{ font-size:0.85rem; color:#155724; font-weight:600; }}
    .payment-info .ref {{ font-size:0.78rem; color:#666; margin-top:4px; }}
    .footer {{ background:#f8f8f8; padding:20px 32px; text-align:center; border-top:1px solid #eee; }}
    .footer p {{ font-size:0.78rem; color:#999; margin-bottom:4px; }}
    .footer .thank-you {{ font-size:1rem; font-weight:700; color:#e63946; margin-bottom:8px; }}
    .watermark {{ font-size:0.7rem; color:#ccc; margin-top:8px; }}
  </style>
</head>
<body>
<div class='receipt'>
  <div class='header'>
    <div class='logo'>🚗</div>
    <h1>{schoolName}</h1>
    <p>{schoolAddress}</p>
    <p>{schoolPhone} &bull; {schoolEmail}</p>
  </div>

  <div class='receipt-number'>
    <div>
      <div class='label'>Payment Receipt</div>
      <div class='value'>{data.BillNumber}</div>
    </div>
    <div>
      <div class='label'>Date</div>
      <div class='value'>{data.BillDate:dd MMM yyyy}</div>
    </div>
    <div><span class='status-badge'>{data.Status}</span></div>
  </div>

  <div class='body'>
    <div class='section'>
      <h3>Student Details</h3>
      <div class='info-grid'>
        <div class='info-item'><div class='key'>Full Name</div><div class='val'>{data.StudentName}</div></div>
        <div class='info-item'><div class='key'>NIC Number</div><div class='val'>{data.StudentNic}</div></div>
        <div class='info-item'><div class='key'>Phone</div><div class='val'>{data.StudentPhone}</div></div>
        {(data.StudentEmail != null ? $"<div class='info-item'><div class='key'>Email</div><div class='val'>{data.StudentEmail}</div></div>" : "")}
      </div>
    </div>

    <div class='section'>
      <h3>Payment Breakdown</h3>
      <table class='amounts-table'>
        <tr><td>Course Fee</td><td>Rs. {data.TotalAmount:N2}</td></tr>
        {(data.DiscountAmount > 0 ? $"<tr><td>Discount</td><td style='color:#3fb950'>- Rs. {data.DiscountAmount:N2}</td></tr>" : "")}
        <tr class='total-row'><td>Net Amount</td><td>Rs. {data.NetAmount:N2}</td></tr>
        <tr class='paid-row'><td>Amount Paid</td><td>Rs. {data.PaidAmount:N2}</td></tr>
        {(data.BalanceAmount > 0 ? $"<tr class='balance-row'><td>Balance Due</td><td>Rs. {data.BalanceAmount:N2}</td></tr>" : "")}
      </table>
    </div>

    {(data.PaidAmount > 0 ? $@"
    <div class='section'>
      <h3>Payment Details</h3>
      <div class='payment-info'>
        <div class='pm'>&#9989; Paid via {data.PaymentMethod}</div>
        {(data.ReferenceNo != null ? $"<div class='ref'>Reference: {data.ReferenceNo}</div>" : "")}
        {(data.Remarks != null ? $"<div class='ref'>Note: {data.Remarks}</div>" : "")}
      </div>
    </div>" : "")}
  </div>

  <div class='footer'>
    <div class='thank-you'>Thank you for choosing {schoolName}!</div>
    <p>Please keep this receipt for your records.</p>
    {(data.BalanceAmount > 0 ? $"<p style='color:#d29922; font-weight:600;'>Balance of Rs. {data.BalanceAmount:N2} is due. Please settle at your earliest convenience.</p>" : "")}
    <div class='watermark'>Generated on {DateTime.Now:dd MMM yyyy HH:mm} &bull; DSMS v2</div>
  </div>
</div>
</body>
</html>";
    }

    public string GenerateReceiptText(ReceiptData data)
    {
        var school = _config.GetSection("SchoolInfo");
        var schoolName = school["Name"] ?? "Arampath Driving School";
        var schoolPhone = school["Phone"] ?? "";

        var lines = new List<string>
        {
            $"🚗 *{schoolName}*",
            $"📄 *PAYMENT RECEIPT*",
            $"━━━━━━━━━━━━━━━━━━━━",
            $"Bill No: *{data.BillNumber}*",
            $"Date: {data.BillDate:dd MMM yyyy}",
            $"Status: *{data.Status}*",
            $"━━━━━━━━━━━━━━━━━━━━",
            $"👤 *Student Details*",
            $"Name: {data.StudentName}",
            $"NIC: {data.StudentNic}",
            $"Phone: {data.StudentPhone}",
            $"━━━━━━━━━━━━━━━━━━━━",
            $"💰 *Payment Breakdown*",
            $"Course Fee: Rs. {data.TotalAmount:N2}"
        };

        if (data.DiscountAmount > 0)
            lines.Add($"Discount: - Rs. {data.DiscountAmount:N2}");

        lines.Add($"Net Amount: Rs. {data.NetAmount:N2}");
        lines.Add($"✅ Amount Paid: *Rs. {data.PaidAmount:N2}*");

        if (data.BalanceAmount > 0)
            lines.Add($"⚠️ Balance Due: *Rs. {data.BalanceAmount:N2}*");

        if (data.PaidAmount > 0)
        {
            lines.Add($"━━━━━━━━━━━━━━━━━━━━");
            lines.Add($"Payment Method: {data.PaymentMethod}");
            if (!string.IsNullOrEmpty(data.ReferenceNo))
                lines.Add($"Reference: {data.ReferenceNo}");
        }

        lines.Add($"━━━━━━━━━━━━━━━━━━━━");
        lines.Add($"Thank you for choosing {schoolName}! 🙏");
        if (data.BalanceAmount > 0)
            lines.Add($"Please settle the balance at your earliest convenience.");
        lines.Add($"📞 {schoolPhone}");

        return string.Join("\n", lines);
    }
}
