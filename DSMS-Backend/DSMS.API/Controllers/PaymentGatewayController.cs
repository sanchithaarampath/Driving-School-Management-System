using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using DSMS.API.DTOs;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/payment-gateway")]
    [Authorize]
    public class PaymentGatewayController : ControllerBase
    {
        private readonly IConfiguration _config;

        public PaymentGatewayController(IConfiguration config)
        {
            _config = config;
        }

        // ─── Stripe ───────────────────────────────────────────────────────────

        /// <summary>Returns the Stripe publishable key so the frontend can init Stripe.js</summary>
        [HttpGet("stripe/config")]
        public IActionResult GetStripeConfig()
        {
            return Ok(new { publishableKey = _config["Stripe:PublishableKey"] });
        }

        /// <summary>Creates a Stripe PaymentIntent and returns the clientSecret</summary>
        [HttpPost("stripe/create-intent")]
        public IActionResult CreateStripeIntent([FromBody] StripeIntentDto dto)
        {
            try
            {
                StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
                var currency = _config["Stripe:Currency"] ?? "lkr";

                // LKR is a zero-decimal currency in Stripe — pass amount as-is (in rupees)
                // For USD, multiply by 100 to get cents
                long stripeAmount = currency.ToLower() == "usd"
                    ? (long)(dto.Amount * 100)
                    : (long)dto.Amount;

                var options = new PaymentIntentCreateOptions
                {
                    Amount   = stripeAmount,
                    Currency = currency,
                    Metadata = new Dictionary<string, string>
                    {
                        { "billId",      dto.BillId.ToString() },
                        { "studentName", dto.StudentName ?? "" }
                    }
                };

                var service = new PaymentIntentService();
                var intent  = service.Create(options);

                return Ok(new { clientSecret = intent.ClientSecret, intentId = intent.Id });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ─── PayPal ───────────────────────────────────────────────────────────

        /// <summary>Returns PayPal client ID + currency so frontend can load the PayPal JS SDK</summary>
        [HttpGet("paypal/config")]
        public IActionResult GetPayPalConfig()
        {
            return Ok(new
            {
                clientId = _config["PayPal:ClientId"],
                currency = _config["PayPal:Currency"] ?? "USD"
            });
        }
    }
}
