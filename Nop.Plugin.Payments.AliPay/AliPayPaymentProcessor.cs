using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.Routing;
using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.AliPay.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using System.Text.RegularExpressions;
using Nop.Services.Logging;
using Nop.Services.Directory;
using Nop.Core.Domain.Directory;

namespace Nop.Plugin.Payments.AliPay
{
    /// <summary>
    /// AliPay payment processor
    /// </summary>
    public class AliPayPaymentProcessor : BasePlugin, IPaymentMethod
    {

        #region Fields
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly AliPayPaymentSettings _aliPayPaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly HttpContextBase _httpContext;
        #endregion

        #region Ctor

        public AliPayPaymentProcessor(ILogger logger,
            ISettingService settingService, 
            IWebHelper webHelper,
            IStoreContext storeContext,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            AliPayPaymentSettings aliPayPaymentSettings,
            ILocalizationService localizationService,
            HttpContextBase httpContext)
        {
            this._logger = logger;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._storeContext = storeContext;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._aliPayPaymentSettings = aliPayPaymentSettings;
            this._localizationService = localizationService;
            this._httpContext = httpContext;
        }

        #endregion
        

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //RemotePost
            DefaultAopClient client = new DefaultAopClient(_aliPayPaymentSettings.GatewayUrl, _aliPayPaymentSettings.AppID, _aliPayPaymentSettings.PrivateKey, "json", "1.0", _aliPayPaymentSettings.SignType, _aliPayPaymentSettings.AlipayPublicKey, "UTF-8", false);

            // 外部订单号，商户网站订单系统中唯一的订单号
            string out_trade_no = postProcessPaymentRequest.Order.Id.ToString();

            
            // 订单名称
            string subject = _storeContext.CurrentStore.Name;

            //订单原始价格按主货币，
            var CNY = _currencyService.GetCurrencyByCode("CNY");
            var primaryExchangeCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
            if (primaryExchangeCurrency == null)
                throw new NopException("Primary exchange rate currency is not set");
            // 付款金额 ,换算成 人民币 
            decimal cur_total = postProcessPaymentRequest.Order.OrderTotal;
            cur_total = cur_total / primaryExchangeCurrency.Rate * CNY.Rate;
            
            string total_amout = cur_total.ToString("0.00", CultureInfo.InvariantCulture);


            // 商品描述
            string body = "Order from " + _storeContext.CurrentStore.Name;

            AopResponse response = null;

            string s = _httpContext.Request.UserAgent;
            Regex b = new Regex(@"android.+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(di|rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (!(b.IsMatch(s) || v.IsMatch(s.Substring(0, 4))))
            {

                AlipayTradePagePayRequest request = new AlipayTradePagePayRequest();
                // 设置同步回调地址
                request.SetReturnUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Return");
                // 设置异步通知接收地址
                request.SetNotifyUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Notify");
                // 将业务model载入到request
                request.SetBizModel(new AlipayTradePagePayModel() {
                    Body = body,
                    Subject = subject,
                    TotalAmount = total_amout,
                    OutTradeNo = out_trade_no,
                    GoodsType = "0",
                    TimeoutExpress = "15m",
                    ProductCode = "FAST_INSTANT_TRADE_PAY"
                });
                
                try
                {
                    response = client.pageExecute(request, null, "post");
                }
                catch (Exception exp)
                {
                    _logger.Warning($"{DateTime.Now.ToString()}Ali Page PayError:{exp.Message}");
                }

            }
            else
            {
                AlipayTradeWapPayRequest request = new AlipayTradeWapPayRequest();
                // 设置同步回调地址
                request.SetReturnUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Return");
                // 设置异步通知接收地址
                request.SetNotifyUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Notify");
                // 将业务model载入到request
                request.SetBizModel(new AlipayTradeWapPayModel()
                {
                    Body = body,
                    Subject = subject,
                    TotalAmount = total_amout,
                    OutTradeNo = out_trade_no,
                    GoodsType = "0",
                    TimeoutExpress = "15m",
                    ProductCode = "FAST_INSTANT_TRADE_PAY"
                });

                try
                {
                    response = client.pageExecute(request, null, "post");
                }
                catch (Exception exp)
                {
                    _logger.Warning($"{DateTime.Now.ToString()}Ali Wap PayError:{exp.Message}");
                }
            }

            _httpContext.Response.Clear();
            _httpContext.Response.Write("<html><head><meta http-equiv=\"Content - Type\" content=\"text / html; charset = utf - 8\"/></head><body style=\"display:none\">");
            _httpContext.Response.Write(response.Body);
            _httpContext.Response.Write("</body></html>");
            _httpContext.Response.End();

        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _aliPayPaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();

            result.AddError("Capture method not supported");

            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();

            result.AddError("Refund method not supported");

            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();

            result.AddError("Void method not supported");

            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            result.AddError("Recurring payment not supported");

            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();

            result.AddError("Recurring payment not supported");

            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //AliPay is the redirection payment method
            //It also validates whether order is also paid (after redirection) so customers will not be able to pay twice
            
            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return false;

            //let's ensure that at least 1 minute passed after order is placed and cancel after 15 minutes
            return !((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1 || (DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes > 15);
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentAliPay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.AliPay.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentAliPay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.AliPay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentAliPayController);
        }

        public override void Install()
        {
            //settings
            var settings = new AliPayPaymentSettings
            {
                AppID = "",
                GatewayUrl = "",
                PrivateKey = "",
                AlipayPublicKey = "",
                SignType = "",
                AdditionalFee = 0,
            };

            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.RedirectionTip", "将为您转到支付宝进行支付.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AppID", "AppID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AppID.Hint", "Enter AppID.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.GatewayUrl", "GatewayUrl");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.GatewayUrl.Hint", "Enter GatewayUrl.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.PrivateKey", "PrivateKey");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.PrivateKey.Hint", "Enter PrivateKey.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AlipayPublicKey", "AlipayPublicKey");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AlipayPublicKey.Hint", "Enter AlipayPublicKey.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.SignType", "SignType");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.SignType.Hint", "Enter SignType.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee.Hint", "输入额外费用，没有则输入0");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.AliPay.PaymentMethodDescription", "您将被重定向到支付宝进行支付.");
            
            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AppID");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AppID.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.GatewayUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.GatewayUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.PrivateKey");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.PrivateKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AlipayPublicKey");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AlipayPublicKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.SignType");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.SignType.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.AliPay.PaymentMethodDescription");
            
            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.AliPay.PaymentMethodDescription"); }
        }

        #endregion
    }
}
