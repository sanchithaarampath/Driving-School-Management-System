using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.DTOs;
using DSMS.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/payhere")]
    public class PayHereController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly DsmsDbContext _context;

        public PayHereController(IConfiguration config, DsmsDbContext context)
        {
            _config = config;
            _context = context;
        }

        // Returns PayHere config so frontend knows if it's configured
        [HttpGet("config")]
        [Authorize]
        public IActionResult GetConfig()
        {
            var merchantId = _config["PayHere:MerchantId"] ?? "";
            var sandbox    = bool.Parse(_config["PayHere:Sandbox"] ?? "true");
            var configured = !string.IsNullOrEmpty(merchantId) && !merchantId.StartsWith("YOUR_");
            return Ok(new { merchantId, sandbox, configured });
        }

        // Creates a PayHere payment session — generates MD5 hash required by PayHere
        [HttpPost("create-payment")]
        [Authorize]
        public IActionResult CreatePayment([FromBody] PayHereCreateDto dto)
        {
            var merchantId     = _config["PayHere:MerchantId"] ?? "";
            var merchantSecret = _config["PayHere:MerchantSecret"] ?? "";
            var sandbox        = bool.Parse(_config["PayHere:Sandbox"] ?? "true");
            var currency       = _config["PayHere:Currency"] ?? "LKR";

            if (string.IsNullOrEmpty(merchantId) || merchantId.StartsWith("YOUR_"))
                return BadRequest(new { message = "PayHere is not configured. Add MerchantId and MerchantSecret to appsettings.json." });

            var amountFormatted = dto.Amount.ToString("F2");

            // PayHere hash: MD5(merchant_id + order_id + amount + currency + MD5(secret).UPPER).UPPER
            var secretHash = ComputeMd5(merchantSecret).ToUpper();
            var raw        = $"{merchantId}{dto.OrderId}{amountFormatted}{currency}{secretHash}";
            var hash       = ComputeMd5(raw).ToUpper();

            return Ok(new
            {
                merchantId,
                sandbox,
                currency,
                hash,
                amountFormatted,
                orderId = dto.OrderId
            });
        }

        // PayHere server-to-server payment notification (no auth — called by PayHere servers)
        [HttpPost("notify")]
        [AllowAnonymous]
        public async Task<IActionResult> Notify()
        {
            var form           = Request.Form;
            var merchantId     = form["merchant_id"].ToString();
            var orderId        = form["order_id"].ToString();
            var paymentId      = form["payment_id"].ToString();
            var payhereAmount  = form["payhere_amount"].ToString();
            var payhereCurrency= form["payhere_currency"].ToString();
            var statusCodeStr  = form["status_code"].ToString();
            var md5sig         = form["md5sig"].ToString();

            if (!int.TryParse(statusCodeStr, out int statusCode)) return BadRequest();

            var merchantSecret = _config["PayHere:MerchantSecret"] ?? "";

            // Verify MD5 signature
            var secretHash   = ComputeMd5(merchantSecret).ToUpper();
            var raw          = $"{merchantId}{orderId}{payhereAmount}{payhereCurrency}{statusCode}{secretHash}";
            var expectedSig  = ComputeMd5(raw).ToUpper();

            if (!string.Equals(expectedSig, md5sig, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid signature");

            // status_code 2 = Success
            if (statusCode == 2)
            {
                // orderId format: DSMS-{billId}
                var idPart = orderId.Replace("DSMS-", "");
                if (int.TryParse(idPart, out int billId))
                {
                    var bill = await _context.Bills.FindAsync(billId);
                    if (bill != null && bill.BalanceAmount > 0)
                    {
                        if (!decimal.TryParse(payhereAmount, out decimal paidAmt)) paidAmt = 0;

                        var payment = new Payment
                        {
                            BillId            = billId,
                            StudentId         = bill.StudentId,
                            PaymentDate       = DateTime.Now,
                            Amount            = paidAmt,
                            PaymentMethod     = "PayHere (Card)",
                            ReferenceNo       = paymentId,
                            Remarks           = "PayHere online payment — auto-recorded via webhook",
                            CreatedBy         = "PayHere",
                            CreatedDateTime   = DateTime.Now
                        };
                        _context.Payments.Add(payment);

                        bill.PaidAmount          += paidAmt;
                        bill.BalanceAmount        -= paidAmt;
                        bill.Status               = bill.BalanceAmount <= 0 ? "Paid" : "Partial";
                        bill.LastModifiedBy       = "PayHere";
                        bill.LastModifiedDateTime = DateTime.Now;

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok();
        }

        private static string ComputeMd5(string input)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
