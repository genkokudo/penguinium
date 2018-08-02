using Newtonsoft.Json;
using System.Collections.Generic;
using Penguinium.Common;
using Penguinium.ProcessData;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using Chickenium;

namespace Penguinium.Client
{
    // ProductCodeごとに異なるインスタンスを作成するように設計すること

    /// <summary>
    /// Bitflyer用のクライアント
    /// このクラスオブジェクトを通して操作を行う
    /// </summary>
    public class BitflyerClient : ExchangeClient
    {
        /// <summary>
        /// 登録した注文を非同期に削除する
        /// </summary>
        Counter cancelOrderCount;

        #region コンストラクタ
        /// <summary>
        /// Bitflyer用のクライアント
        /// </summary>
        /// <param name="exchangeId">取引所ID(1〜3、例えばFX_BTC_JPYは2)</param>
        /// <param name="dbContextOptions">DB接続オプション</param>
        /// <param name="timeoutSec">Timeout sec.</param>
        public BitflyerClient(
            int exchangeId,
            DbContextOptions dbContextOptions,
            double timeoutSec = 4 // タイムアウト (デフォルト4秒)
        ) : base(exchangeId, dbContextOptions, timeoutSec)
        {
            sendchildorder = "/v1/me/sendchildorder";
            sendparentorder = "/v1/me/sendparentorder";
            getMyPositions = "/v1/me/getpositions?product_code=" + PRODUCT_CODE.ToString();
            cancelallchildorders = "/v1/me/cancelallchildorders";
            cancelparentorder = "/v1/me/cancelparentorder";
            board = "/v1/board?product_code=" + PRODUCT_CODE.ToString();
            ticker = "/v1/ticker?product_code=" + PRODUCT_CODE.ToString();
            balance = "/v1/me/getbalance";
            collateral = "/v1/me/getcollateral";
            getMyActiveChildOrders = "/v1/me/getchildorders?product_code=" + PRODUCT_CODE.ToString()
                + "&child_order_state=" + MyOrderState.ACTIVE.ToString()
                + "&count=100";
            getMyActiveParentOrders = "/v1/me/getparentorders?product_code=" + PRODUCT_CODE.ToString()
                + "&parent_order_state=" + MyOrderState.ACTIVE.ToString()
                + "&count=200";
            getBoardState = "/v1/getboardstate";

            BoardEnubleStatusName = new List<string>() { "RUNNING" };
            cancelOrderCount = new Counter(300);
        }
        #endregion

        #region 通常注文
        protected override string MakeJsonBuySell(OrderSide side, double price, double amount)
        {
            // リクエスト構築
            string body = "";
            if (price > 0) // 指値
            {
                var reqobj = new
                {
                    product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                    child_order_type = "LIMIT", // 指値: LIMIT, 成行: MARKET
                    side = side.ToString(),
                    price,
                    size = amount
                };
                body = JsonConvert.SerializeObject(reqobj);
            }
            else // 成行
            {
                var reqobj = new
                {
                    product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                    child_order_type = "MARKET", // 指値: LIMIT, 成行: MARKET
                    side = side.ToString(),
                    size = amount
                };
                body = JsonConvert.SerializeObject(reqobj);
            }
            return body;
        }
        #endregion

        #region 逆指値
        protected override string MakeJsonBuySellStop(OrderSide side, double triggerPrice, double amount)
        {
            // リクエスト構築
            string body = "";
            var reqobj2 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "STOP",
                side = side.ToString(),
                trigger_price = triggerPrice,
                size = amount
            };
            var reqobj1 = new
            {
                order_method = "SIMPLE",
                parameters = new List<object> { reqobj2 }
            };
            body = JsonConvert.SerializeObject(reqobj1);

            return body;
        }

        protected override string MakeJsonBuySellStopTrail(OrderSide side, double triggerPrice, double amount, double offset)
        {
            System.Console.WriteLine(offset + ":" + amount);
            OrderSide sideTrail = OrderSide.BUY;
            if(side == OrderSide.BUY)
            {
                sideTrail = OrderSide.SELL;
            }

            // リクエスト構築
            string body = "";
            var reqobj2 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "STOP",
                side = side.ToString(),
                trigger_price = triggerPrice,
                size = amount
            };
            var reqobj3 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "TRAIL", // 指値: LIMIT, 成行: MARKET
                side = sideTrail.ToString(),
                offset,
                size = amount
            };
            var reqobj1 = new
            {
                order_method = "IFD",
                parameters = new List<object> { reqobj2, reqobj3 }
            };
            body = JsonConvert.SerializeObject(reqobj1);

            System.Console.WriteLine(body);
            return body;
        }
        #endregion

        #region IfDone注文
        protected override string MakeJsonIfDone(OrderSide beforeSide, OrderSide afterSide, double beforePrice, double afterPrice, double amount)
        {
            // リクエスト構築
            string body = "";
            var reqobj2 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "LIMIT", // 指値: LIMIT, 成行: MARKET
                side = beforeSide.ToString(),
                price = beforePrice,
                size = amount
            };
            var reqobj3 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "LIMIT", // 指値: LIMIT, 成行: MARKET
                side = afterSide.ToString(),
                price = afterPrice,
                size = amount
            };
            var reqobj1 = new
            {
                order_method = "IFD", // IFD
                parameters = new List<object> { reqobj2, reqobj3 }
            };
            body = JsonConvert.SerializeObject(reqobj1);

            return body;
        }
        #endregion

        #region IfDoneOco注文
        // イフダンOCO
        protected override string MakeJsonIfDoneOco(OrderSide beforeSide, OrderSide afterSide, double beforePrice, double afterPrice, double losePrice, double amount)
        {
            // リクエスト構築
            string body = "";
            var reqobj2 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "LIMIT", // 指値: LIMIT, 成行: MARKET
                side = beforeSide.ToString(),
                price = beforePrice,
                size = amount
            };
            var reqobj3 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "LIMIT", // 指値: LIMIT, 成行: MARKET
                side = afterSide.ToString(),
                price = afterPrice,
                size = amount
            };
            var reqobj4 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "STOP", // 指値: LIMIT, 成行: MARKET
                side = afterSide.ToString(),
                price = losePrice,
                size = amount
            };
            var reqobj1 = new
            {
                order_method = "IFDOCO", // IFDOCO
                parameters = new List<object> { reqobj2, reqobj3, reqobj4 }
            };
            body = JsonConvert.SerializeObject(reqobj1);

            return body;
        }
        #endregion

        #region Trail注文
        protected override string MakeJsonTrailing(OrderSide side, double offset, double amount)
        {
            // リクエスト構築
            string body = "";
            var reqobj2 = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                condition_type = "TRAIL", // 指値: LIMIT, 成行: MARKET
                side = side.ToString(),
                offset,
                size = amount
            };
            var reqobj1 = new
            {
                order_method = "SIMPLE", // SIMPLE
                parameters = new List<object> { reqobj2 }
            };
            body = JsonConvert.SerializeObject(reqobj1);

            return body;
        }
        #endregion

        #region CancelAllOrders,CancelOrder:注文取り消し
        protected override string MakeCancelAllOrders()
        {
            // リクエスト構築
            var reqobj = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
            };
            return JsonConvert.SerializeObject(reqobj);
        }

        protected override string MakeJsonCancelOrder(Order order)
        {
            // リクエスト構築
            var reqobj = new
            {
                product_code = PRODUCT_CODE.ToString(), // BTC_JPY, FX_BTC_JPY
                parent_order_acceptance_id = order.OrderId
            };
            return JsonConvert.SerializeObject(reqobj);
        }
        #endregion

        public override void Update()
        {
            base.Update();

            // 1分ごとにキャンセル処理
            if (cancelOrderCount.Update())
            {
                CancelOrder();
            }
        }

        #region CancelOrder
        /// <summary>
        /// リストに登録した注文をキャンセルする
        /// 決済はしない
        /// </summary>
        protected async void CancelOrder()
        {
            try
            {
                // null除外
                for (int i = 0; i < cancelOrderList.Count; i++)
                {
                    if (cancelOrderList[i] == null)
                    {
                        cancelOrderList.RemoveAt(i);
                        i--;
                    }
                }

                var completionList = new List<Order>();
                foreach (var item in parentOrders)
                {
                    var order = cancelOrderList.FirstOrDefault(e => e.OrderId == item.OrderId);
                    if (order != null)
                    {
                        await CancelOrder(item);
                    }
                    else
                    {
                        // 無かったらリストから除外
                        completionList.Add(item);
                    }
                }

                // 除外処理
                foreach (var item in completionList)
                {
                    cancelOrderList.Remove(item);
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                Logger.ErrorLog(ex.StackTrace);
            }
        }
        #endregion
    }
}
