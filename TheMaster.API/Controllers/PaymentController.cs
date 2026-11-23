using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TheMaster.Application.Common;
using TheMaster.Application.Interfaces;

namespace TheMaster.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ISslCommerzService _sslService;
        private readonly IConfiguration _config;

        public PaymentController(IOrderService orderService, ISslCommerzService sslService, IConfiguration config)
        {
            _orderService = orderService;
            _sslService = sslService;
            _config = config;
        }

        [HttpPost("success")]
        [HttpGet("success")]
        public async Task<IActionResult> PaymentSuccess()
        {
            var tranId = Request.HasFormContentType && Request.Form.ContainsKey("tran_id")
                ? Request.Form["tran_id"].ToString()
                : Request.Query["tran_id"].ToString();

            var valId = Request.HasFormContentType && Request.Form.ContainsKey("val_id")
                ? Request.Form["val_id"].ToString()
                : Request.Query["val_id"].ToString();

            var amount = Request.HasFormContentType && Request.Form.ContainsKey("amount")
                ? Request.Form["amount"].ToString()
                : Request.Query["amount"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                var isValid = string.IsNullOrWhiteSpace(valId) || await _sslService.ValidatePaymentAsync(valId);
                if (isValid)
                {
                    await _orderService.CompletePaymentAsync(tranId, valId, $"Amount: {amount}, ValId: {valId}");
                }
            }

            var clientUrl = _config["AppUrls:ClientBaseUrl"] ?? "http://localhost:4200";
            return Redirect($"{clientUrl}?payment=success&tran_id={tranId}");
        }

        [HttpPost("fail")]
        [HttpGet("fail")]
        public async Task<IActionResult> PaymentFail()
        {
            var tranId = Request.HasFormContentType && Request.Form.ContainsKey("tran_id")
                ? Request.Form["tran_id"].ToString()
                : Request.Query["tran_id"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                await _orderService.FailPaymentAsync(tranId, "Transaction failed by gateway.");
            }

            var clientUrl = _config["AppUrls:ClientBaseUrl"] ?? "http://localhost:4200";
            return Redirect($"{clientUrl}?payment=failed&tran_id={tranId}");
        }

        [HttpPost("cancel")]
        [HttpGet("cancel")]
        public async Task<IActionResult> PaymentCancel()
        {
            var tranId = Request.HasFormContentType && Request.Form.ContainsKey("tran_id")
                ? Request.Form["tran_id"].ToString()
                : Request.Query["tran_id"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId))
            {
                await _orderService.FailPaymentAsync(tranId, "Transaction cancelled by user.");
            }

            var clientUrl = _config["AppUrls:ClientBaseUrl"] ?? "http://localhost:4200";
            return Redirect($"{clientUrl}?payment=cancelled&tran_id={tranId}");
        }

        [HttpPost("ipn")]
        [HttpGet("ipn")]
        public async Task<IActionResult> PaymentIpn()
        {
            var tranId = Request.HasFormContentType && Request.Form.ContainsKey("tran_id")
                ? Request.Form["tran_id"].ToString()
                : Request.Query["tran_id"].ToString();

            var valId = Request.HasFormContentType && Request.Form.ContainsKey("val_id")
                ? Request.Form["val_id"].ToString()
                : Request.Query["val_id"].ToString();

            var status = Request.HasFormContentType && Request.Form.ContainsKey("status")
                ? Request.Form["status"].ToString()
                : Request.Query["status"].ToString();

            if (!string.IsNullOrWhiteSpace(tranId) && (status.ToUpper() == "VALID" || status.ToUpper() == "VALIDATED"))
            {
                await _orderService.CompletePaymentAsync(tranId, valId, "IPN verified callback.");
            }

            return Ok(ApiResponse<string>.Ok("IPN received."));
        }

        [HttpPost("simulate-success")]
        public async Task<IActionResult> SimulateSuccess([FromBody] SimulatePaymentRequest request)
        {
            var tranId = request?.TranId?.Trim();
            if (string.IsNullOrWhiteSpace(tranId))
            {
                return BadRequest(ApiResponse<object>.Fail("Transaction ID is required.", 400));
            }

            var success = await _orderService.CompletePaymentAsync(tranId, "SIMULATED_VAL_ID", "Simulation Test Payment");
            if (!success)
            {
                return NotFound(ApiResponse<object>.Fail("Order with this transaction ID was not found.", 404));
            }

            return Ok(ApiResponse<object>.Ok(new { tran_id = tranId }, "Payment simulated successfully. Course access email sent."));
        }
    }

    public class SimulatePaymentRequest
    {
        public string TranId { get; set; } = string.Empty;
    }
}
