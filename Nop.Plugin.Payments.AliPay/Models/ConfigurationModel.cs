using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.AliPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.AliPay.AppID")]
        public string AppID { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.GatewayUrl")]
        public string GatewayUrl { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.PrivateKey")]
        public string PrivateKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.AlipayPublicKey")]
        public string AlipayPublicKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.SignType")]
        public string SignType { get; set; }

        [NopResourceDisplayName("Plugins.Payments.AliPay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
    }
}