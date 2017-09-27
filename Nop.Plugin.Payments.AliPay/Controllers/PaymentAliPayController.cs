using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Aop.Api.Util;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.AliPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;
using Nop.Services.Directory;
using Nop.Core.Domain.Directory;

namespace Nop.Plugin.Payments.AliPay.Controllers
{
    public class PaymentAliPayController : BasePaymentController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly ILocalizationService _localizationService;
        private readonly AliPayPaymentSettings _aliPayPaymentSettings;
        private readonly PaymentSettings _paymentSettings;

        #endregion

        #region Ctor

        public PaymentAliPayController(ISettingService settingService,
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger,
            ILocalizationService localizationService,
            AliPayPaymentSettings aliPayPaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
            this._localizationService = localizationService;
            this._aliPayPaymentSettings = aliPayPaymentSettings;
            this._paymentSettings = paymentSettings;
        }

        #endregion

        #region Methods

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {

            return View("~/Plugins/Payments.AliPay/Views/Configure.cshtml", new ConfigurationModel()
            {
                AppID = _aliPayPaymentSettings.AppID,
                GatewayUrl = _aliPayPaymentSettings.GatewayUrl,
                AlipayPublicKey = _aliPayPaymentSettings.AlipayPublicKey,
                PrivateKey = _aliPayPaymentSettings.PrivateKey,
                SignType = _aliPayPaymentSettings.SignType,
                AdditionalFee = _aliPayPaymentSettings.AdditionalFee
            });
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _aliPayPaymentSettings.AppID = model.AppID;
            _aliPayPaymentSettings.GatewayUrl = model.GatewayUrl;
            _aliPayPaymentSettings.AlipayPublicKey = model.AlipayPublicKey;
            _aliPayPaymentSettings.PrivateKey = model.PrivateKey;
            _aliPayPaymentSettings.SignType = model.SignType;
            _aliPayPaymentSettings.AdditionalFee = model.AdditionalFee;

            _settingService.SaveSetting(_aliPayPaymentSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
             return View("~/Plugins/Payments.AliPay/Views/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();

            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult Notify(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.AliPay") as AliPayPaymentProcessor;

            if (processor == null || !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("AliPay module cannot be loaded");

            Dictionary<string, string> sArray = GetRequestPost();
            if (sArray.Count != 0)
            {
                bool flag = AlipaySignature.RSACheckV1(sArray, _aliPayPaymentSettings.AlipayPublicKey, "UTF-8", _aliPayPaymentSettings.SignType, false);
                if (flag)
                {
                    //交易状态
                    //判断该笔订单是否在商户网站中已经做过处理
                    //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
                    //请务必判断请求时的total_amount与通知时获取的total_fee为一致的
                    //如果有做过处理，不执行商户的业务程序

                    //注意：
                    //退款日期超过可退款期限后（如三个月可退款），支付宝系统发送该交易状态通知
                    if (sArray["trade_status"] == "TRADE_FINISHED" || sArray["trade_status"] == "TRADE_SUCCESS")
                    {
                        var orderTotal = Decimal.Parse(sArray["total_amount"], NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowCurrencySymbol);
                        if (int.TryParse(sArray["out_trade_no"], out int orderId) && _aliPayPaymentSettings.AppID.Equals(sArray["app_id"]))
                        {
                            var order = _orderService.GetOrderById(orderId);

                            //订单原始价格按主货币，
                            var CNY = _currencyService.GetCurrencyByCode("CNY");
                            var primaryExchangeCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
                            if (primaryExchangeCurrency == null)
                                throw new NopException("Primary exchange rate currency is not set");
                            // 付款金额 ,换算成 人民币 
                            decimal cur_total = order.OrderTotal;
                            cur_total = cur_total / primaryExchangeCurrency.Rate * CNY.Rate;

                            if (orderTotal == Math.Round(cur_total, 2))
                            {
                                if (order != null && _orderProcessingService.CanMarkOrderAsPaid(order))
                                {
                                    _orderProcessingService.MarkOrderAsPaid(order);
                                }

                                Response.Write("success");
                            }
                            else { _logger.Warning($"{DateTime.Now.ToString()}（支付宝支付）订单号{order.Id}: 付款金额{orderTotal} 与订单价格不符"); }
                        }
                        else { _logger.Warning($"{DateTime.Now.ToString()}（支付宝支付）： Out orderId or appid failed"); }

                    }
                }
                else
                {
                    _logger.Error($"{DateTime.Now.ToString()}（支付宝支付）: RSACheckV1 flag false"); 
                    Response.Write("fail");
                }
            }

            return Content("");
        }

        public Dictionary<string, string> GetRequestPost()
        {
            int i = 0;
            Dictionary<string, string> sArray = new Dictionary<string, string>();
            NameValueCollection coll;
            //coll = Request.Form;
            coll = Request.Form;
            String[] requestItem = coll.AllKeys;
            for (i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], Request.Form[requestItem[i]]);
            }
            return sArray;

        }

        [ValidateInput(false)]
        public ActionResult Return()
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.AliPay") as AliPayPaymentProcessor;

            if (processor == null || !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("AliPay module cannot be loaded");

            /* 实际验证过程建议商户添加以下校验。
            1、商户需要验证该通知数据中的out_trade_no是否为商户系统中创建的订单号，
            2、判断total_amount是否确实为该订单的实际金额（即商户订单创建时的金额），
            3、校验通知中的seller_id（或者seller_email) 是否为out_trade_no这笔单据的对应的操作方（有的时候，一个商户可能有多个seller_id/seller_email）
            4、验证app_id是否为该商户本身。
            */
            Dictionary<string, string> sArray = GetRequestGet();
            if (sArray.Count != 0)
            {
                bool flag = AlipaySignature.RSACheckV1(sArray, _aliPayPaymentSettings.AlipayPublicKey, "UTF-8", _aliPayPaymentSettings.SignType, false);
                if (flag)
                {
                    var orderTotal = Decimal.Parse(sArray["total_amount"], NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowCurrencySymbol);
                    if (int.TryParse(sArray["out_trade_no"], out int orderId) && _aliPayPaymentSettings.AppID.Equals(sArray["app_id"]))
                    {
                        var order = _orderService.GetOrderById(orderId);

                        //订单原始价格按主货币，
                        var CNY = _currencyService.GetCurrencyByCode("CNY");
                        var primaryExchangeCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
                        if (primaryExchangeCurrency == null)
                            throw new NopException("Primary exchange rate currency is not set");
                        // 付款金额 ,换算成 人民币 
                        decimal cur_total = order.OrderTotal;
                        cur_total = cur_total / primaryExchangeCurrency.Rate * CNY.Rate;

                        if (orderTotal == Math.Round(cur_total, 2))
                        {
                            if (order != null && _orderProcessingService.CanMarkOrderAsPaid(order))
                            {
                                _orderProcessingService.MarkOrderAsPaid(order);
                            }
                        }
                        else { _logger.Warning($"{DateTime.Now.ToString()}（支付宝支付）订单号{order.Id}: 付款金额{orderTotal} 与订单价格不符"); }
                    }
                    else { _logger.Warning($"{DateTime.Now.ToString()}（支付宝支付）： Out orderId or appid failed"); }
                }
                else { _logger.Warning($"{DateTime.Now.ToString()}（支付宝支付）: RSACheckV1 flag false"); }
            }
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public Dictionary<string, string> GetRequestGet()
        {
            int i = 0;
            Dictionary<string, string> sArray = new Dictionary<string, string>();
            NameValueCollection coll;
            //coll = Request.Form;
            coll = Request.QueryString;
            String[] requestItem = coll.AllKeys;
            for (i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], Request.QueryString[requestItem[i]]);
            }
            return sArray;

        }

        #endregion
    }
}