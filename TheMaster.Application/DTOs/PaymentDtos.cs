namespace TheMaster.Application.DTOs
{
    public class SslCommerzInitResponse
    {
        public string? status { get; set; }
        public string? failedreason { get; set; }
        public string? sessionkey { get; set; }
        public string? GatewayPageURL { get; set; }
    }

    public class SslCommerzValidationResponse
    {
        public string? status { get; set; }
        public string? tran_date { get; set; }
        public string? tran_id { get; set; }
        public string? val_id { get; set; }
        public string? amount { get; set; }
        public string? store_amount { get; set; }
        public string? bank_tran_id { get; set; }
        public string? card_type { get; set; }
        public string? card_no { get; set; }
        public string? card_issuer { get; set; }
        public string? card_brand { get; set; }
        public string? error { get; set; }
    }

    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "The Master Academy";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

    public class SslCommerzSettings
    {
        public string StoreId { get; set; } = string.Empty;
        public string StorePassword { get; set; } = string.Empty;
        public bool IsSandbox { get; set; } = true;
        public string InitUrl { get; set; } = "https://sandbox.sslcommerz.com/gwprocess/v4/api.php";
        public string ValidationUrl { get; set; } = "https://sandbox.sslcommerz.com/validator/api/validationserverAPI.php";
        public string SuccessUrl { get; set; } = string.Empty;
        public string FailUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string IpnUrl { get; set; } = string.Empty;
    }
}
