using System;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Nop.Plugin.Payments.AliPay.Services
{
    public interface IAlipayPaymentService
    {
        bool OrderQuery(Order order);
        void ProcessOrderPaid(Order order);
        string AlipayTradePagePay(Order order);
        string AlipayTradeWapPay(Order order);
    }

    public class AlipayPaymentService : IAlipayPaymentService
    {
        #region Fields
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;
        private readonly AliPayPaymentSettings _aliPayPaymentSettings;
        private readonly IOrderProcessingService _orderProcessingService;
        #endregion


        #region Ctor
        public AlipayPaymentService(ILogger logger,
            IWebHelper webHelper,
            IStoreContext storeContext,
            AliPayPaymentSettings aliPayPaymentSettings,
            IOrderProcessingService orderProcessingService)
        {
            this._logger = logger;
            this._webHelper = webHelper;
            this._storeContext = storeContext;
            this._aliPayPaymentSettings = aliPayPaymentSettings;
            this._orderProcessingService = orderProcessingService;
        }

        #endregion


        #region Method

        public string AlipayTradePagePay(Order order)
        {
            DefaultAopClient client = new DefaultAopClient(_aliPayPaymentSettings.GatewayUrl, _aliPayPaymentSettings.AppID, _aliPayPaymentSettings.PrivateKey, "json", "1.0", _aliPayPaymentSettings.SignType, _aliPayPaymentSettings.AlipayPublicKey, "UTF-8", false);

            // 外部订单号，商户网站订单系统中唯一的订单号
            string out_trade_no = order.Id.ToString();

            // 订单名称
            string subject = _storeContext.CurrentStore.Name;

            // 付款金额
            string total_amout = order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture);

            // 商品描述
            string body = "Order from " + _storeContext.CurrentStore.Name;
            
            AlipayTradePagePayResponse response = null;
            //PC访问 
            AlipayTradePagePayRequest request = new AlipayTradePagePayRequest();
            // 设置同步回调地址
            request.SetReturnUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Return");
            // 设置异步通知接收地址
            request.SetNotifyUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Notify");
            // 将业务model载入到request
            request.SetBizModel(new AlipayTradePagePayModel()
            {
                Body = body,
                Subject = subject,
                TotalAmount = total_amout,
                OutTradeNo = out_trade_no,
                ProductCode = "FAST_INSTANT_TRADE_PAY"
            });
            try
            {

                response = client.pageExecute(request, null, "post");
                return response.Body;
            }
            catch (Exception exp)
            {
                _logger.Information($"{DateTime.Now.ToLocalTime().ToString()}AliPagePay：{exp.Message}");
                return "<script>alert('AlipayTradePagePay Failed!');</script>";
            }

        }

        public string AlipayTradeWapPay(Order order)
        {
            DefaultAopClient client = new DefaultAopClient(_aliPayPaymentSettings.GatewayUrl, _aliPayPaymentSettings.AppID, _aliPayPaymentSettings.PrivateKey, "json", "1.0", _aliPayPaymentSettings.SignType, _aliPayPaymentSettings.AlipayPublicKey, "UTF-8", false);

            // 外部订单号，商户网站订单系统中唯一的订单号
            string out_trade_no = order.Id.ToString();
            string subject = _storeContext.CurrentStore.Name;
            string total_amout = order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture);
            string body = "Order from " + _storeContext.CurrentStore.Name;
            
            //手机访问 
            AlipayTradeWapPayResponse response = null;
            AlipayTradeWapPayRequest request = new AlipayTradeWapPayRequest();
            request.SetReturnUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Return");
            request.SetNotifyUrl(_webHelper.GetStoreLocation(false) + "Plugins/PaymentAliPay/Notify");
            request.SetBizModel(new AlipayTradeWapPayModel()
            {
                Body = body,
                Subject = subject,
                TotalAmount = total_amout,
                OutTradeNo = out_trade_no,
                ProductCode = "FAST_INSTANT_TRADE_PAY"
            });
            try
            {
                response = client.pageExecute(request, null, "post");
                return response.Body;
            }
            catch (Exception exp)
            {
                _logger.Information($"{DateTime.Now.ToLocalTime().ToString()}AliWapPay：{exp.Message}");
                return "<script>alert('AlipayTradeWapPay Failed!');</script>";
            }
        }

        //订单查询接口
        public bool OrderQuery(Order order)
        {
            
            // 根据商户订单号查支付宝服务器上的订单信息
            string out_trade_no = order.Id.ToString();
            if (!string.IsNullOrEmpty(out_trade_no))
            {
                DefaultAopClient client = new DefaultAopClient(_aliPayPaymentSettings.GatewayUrl, _aliPayPaymentSettings.AppID, _aliPayPaymentSettings.PrivateKey, "json", "1.0", _aliPayPaymentSettings.SignType, _aliPayPaymentSettings.AlipayPublicKey, "UTF-8", false);
                AlipayTradeQueryRequest request = new AlipayTradeQueryRequest();
                request.SetBizModel(new AlipayTradeQueryModel(){ OutTradeNo = out_trade_no });
                AlipayTradeQueryResponse response = null;
                try
                {
                    response = client.Execute(request);
                    var result = JObject.Parse(response.Body);
                    string trade_status = Convert.ToString(result["alipay_trade_query_response"]["trade_status"]);
                    if (trade_status == "TRADE_SUCCESS" || trade_status == "TRADE_FINISHED") { return true; }
                }
                catch (Exception exp)
                {
                    //log
                    _logger.Information($"{DateTime.Now.ToLocalTime().ToString()}商城订单{order.Id}接口查询失败:{exp.Message}");
                    return false;
                }
            }
            else
            {
                _logger.Information($"{DateTime.Now.ToLocalTime().ToString()}商城订单查询失败:无订单号");
            }
            return false;
        }

        public void ProcessOrderPaid(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                _orderProcessingService.MarkOrderAsPaid(order);
            }
        }
        #endregion

        
    }
}
