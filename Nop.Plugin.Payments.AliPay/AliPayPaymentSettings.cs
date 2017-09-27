using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.AliPay
{
    public class AliPayPaymentSettings : ISettings
    {
        public string AppID { get; set; }
        public string GatewayUrl { get; set; }
        public string PrivateKey { get; set; }
        public string AlipayPublicKey { get; set; }
        public string SignType { get; set; }
        public decimal AdditionalFee { get; set; }
    }
}
