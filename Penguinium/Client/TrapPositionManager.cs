using Chickenium;
using Penguinium.Common;
using Penguinium.ProcessData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penguinium.Client
{

    #region Trap
    /// <summary>
    /// トラップ用注文情報
    /// </summary>
    public class Trap
    {
        public Trap(DateTime date, OrderSide side, double openPrice, double closePrice, double size, string orderId)
        {
            Date = date;
            OpenPrice = openPrice;
            ClosePrice = closePrice;
            Size = size;
            side = openPrice > closePrice ? OrderSide.SELL : OrderSide.BUY;
            OrderId = orderId;
            OutstandingSize = size;
            IsStopLoss = false;
        }

        #region プロパティ
        public DateTime Date { get; set; } // 注文日時
        public OrderSide Side { get; set; } // 売・買
        public double OpenPrice { get; set; } // 指値価格
        public double ClosePrice { get; set; } // 決済価格
        public double Size { get; set; } // 数量（買いと売りの合計値）
        public string OrderId { get; set; } // 対応するオーダーの注文番号
        public double OutstandingSize { get; set; } // 未消化の数量
        public bool IsStopLoss { get; set; } // 強制決済済ならtrue

        /// <summary>
        /// 完全に未消化ならばtrue
        /// </summary>
        public bool IsFull { get { return Size == OutstandingSize; } }

        /// <summary>
        /// 消化済みサイズ
        /// </summary>
        public double ExecutedSize { get { return Size - OutstandingSize; } }
        #endregion

        /// <summary>
        /// 対応する注文情報で更新する
        /// </summary>
        /// <param name="order"></param>
        public void Update(Order order)
        {
            if (OrderId == order.OrderId)
            {
                Size = order.Size;
                OutstandingSize = order.OutstandingSize;
            }
        }

        // 注文状態、対応するオーダー情報の参照

        //public override string ToString()
        //{
        //    return "";
        //}
    }
    #endregion

    /// <summary>
    /// トラリピ注文管理
    /// </summary>
    public class TrapPositionManager
    {
        /// <summary>
        /// 注文数量を小数第何位までにするか
        /// </summary>
        protected const int OrderFigures = 2;

        /// <summary>
        /// クライアント
        /// </summary>
        ExchangeClient client;

        /// <summary>
        /// トラップ管理
        /// </summary>
        List<Trap> trapList;

        /// <summary>
        /// キャンセルまでの時間（分）
        /// </summary>
        int CancelMinute;

        /// <summary>
        /// 損切りまでの時間（分）
        /// </summary>
        int StopLossMinute;

        /// <summary>
        /// トラリピ注文管理
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cancelMinute">未約定注文キャンセルまでの時間</param>
        /// <param name="stopLossMinute">未決済注文損切りまでの時間</param>
        public TrapPositionManager(ExchangeClient client, int cancelMinute, int stopLossMinute)
        {
            this.client = client;
            CancelMinute = cancelMinute;
            StopLossMinute = stopLossMinute;
            trapList = new List<Trap>();
        }

        /// <summary>
        /// 買い建てのIfDone注文
        /// </summary>
        /// <param name="buyPrice"></param>
        /// <param name="orderHeight"></param>
        /// <param name="orderAmount"></param>
        public async void Buy(double buyPrice, double orderHeight, double orderAmount)
        {
            try
            {
                OrderSide side = OrderSide.BUY;
                buyPrice = CalcTrapPrice(buyPrice, orderHeight, side);
                // この値段で注文してない場合は注文する
                if (trapList.FirstOrDefault(e => (e.OpenPrice == buyPrice)) == null)
                {
                    var sellPrice = buyPrice + orderHeight;
                    Console.WriteLine($"買:{ buyPrice }\t売:{ sellPrice }");
                    var order = await client.IfDone(buyPrice, sellPrice, orderAmount);
                    // ローカル管理追加
                    trapList.Add(new Trap(DateTime.Now, side, buyPrice, sellPrice, orderAmount * 2, order.OrderId));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Logger.ErrorLog(ex.StackTrace);
            }
        }
        /// <summary>
        /// 売り建てのIfDone注文
        /// </summary>
        /// <param name="sellPrice"></param>
        /// <param name="orderHeight"></param>
        /// <param name="orderAmount"></param>
        public async void Sell(double sellPrice, double orderHeight, double orderAmount)
        {
            try
            {
                OrderSide side = OrderSide.SELL;
                sellPrice = CalcTrapPrice(sellPrice, orderHeight, side);
                // この値段で注文してない場合は注文する
                if (trapList.FirstOrDefault(e => (e.OpenPrice == sellPrice)) == null)
                {
                    var buyPrice = sellPrice - orderHeight;
                    Console.WriteLine("売:" + sellPrice + "\t買:" + buyPrice);
                    var order = await client.IfDone(sellPrice, buyPrice, orderAmount);

                    // ローカル管理追加
                    trapList.Add(new Trap(DateTime.Now, side, sellPrice, buyPrice, orderAmount, order.OrderId));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Logger.ErrorLog(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// クライアントのキャンセルリストに登録する
        /// </summary>
        /// <param name="orderId">注文受付ID</param>
        public void Cancel(string orderId)
        {
            var order = new Order(orderId);
            client.AddCancelParentOrderList(order);
        }
        
        /// <summary>
        /// 売買注文の区別をつけるため、注文価格に下駄を付加するメソッド
        /// 整数のみ対応
        /// </summary>
        /// <param name="price"></param>
        /// <param name="orderHeight"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        private double CalcTrapPrice(double price, double orderHeight, OrderSide side)
        {
            // 切り捨てる
            var orderPrice = price - price % orderHeight;

            // 売りの場合は1段上から売り、下一桁に1円加える
            if(side == OrderSide.SELL)
            {
                orderPrice += orderHeight + 1;
            }
            return orderPrice;
        }

        #region Update
        /// <summary>
        /// 取引所の注文情報によってローカルのトラップ管理を更新
        /// 消化されていたらローカル管理から削除する
        /// </summary>
        /// <param name="parentOrders">取引所から取得した注文情報</param>
        /// <param name="trend">現在のトレンド（なければBUYSELLにする）</param>
        public async void Update(List<Order> parentOrders, OrderSide trend)
        {
            try
            {
                if (parentOrders != null)
                {
                    var removeList = new List<Trap>();

                    // 取引所のデータと比較し、消化済みのローカル注文を削除する
                    foreach (var item in trapList)
                    {
                        // 同じ注文IDを探す
                        var order = parentOrders.FirstOrDefault(e => e.OrderId == item.OrderId);
                        if (order == null)
                        {
                            // 無くなっていたらローカルの注文管理から削除
                            removeList.Add(item);

                            // 注文してから1分経過しているはずなのであまり考えられないが、もし取引所の注文の反映が遅くて見つからなかったら？
                            // 諦める
                        }
                        else
                        {
                            // 見つかった場合情報更新する
                            item.Update(order);
                        }
                    }

                    // 除外処理
                    foreach (var item in removeList)
                    {
                        trapList.Remove(item);
                    }

                    // 一定時間経過したものをキャンセルする
                    OrderCancel();
                    // 一定時間経過したものを損切りする
                    await OrderStopLoss(trend);
                }
                else
                {
                    Console.WriteLine("注文情報取得失敗のため、ローカル注文管理の更新をしません");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Logger.ErrorLog(ex.StackTrace);
            }
        }
        #endregion

        #region 一定時間経過した注文に対する処理
        /// <summary>
        /// 一定時間経過した注文の取消を行う
        /// </summary>
        /// <returns></returns>
        protected void OrderCancel()
        {
            var now = DateTime.Now;
            var list = trapList.Where(e => (now - e.Date).TotalMinutes > CancelMinute);

            foreach (var item in list)
            {
                if (item.IsFull)
                {
                    Console.WriteLine($"{CancelMinute}分経過のためキャンセル：{item.OpenPrice}");
                    Cancel(item.OrderId);
                }
            }
        }

        /// <summary>
        /// 一定時間経過した注文を強制決済する
        /// </summary>
        /// <returns></returns>
        protected async Task OrderStopLoss(OrderSide trend)
        {
            try
            {
                var now = DateTime.Now;
                IEnumerable<Trap> list;

                //if (trend == OrderSide.BUYSELL)
                //{
                // トレンドなし
                list = trapList.Where(e => (now - e.Date).TotalMinutes > StopLossMinute && !e.IsStopLoss);
                //}
                //else
                //{
                //    // トレンドがある場合、反対の注文も損切りする
                //    list = trapList.Where(e => (now - e.Date).TotalMinutes > StopLossMinute && !e.IsStopLoss || e.Side != trend);
                //}
                double totalCancelSize = 0;
                foreach (var item in list)
                {
                    var cancelSize = item.ExecutedSize;
                    if (item.Side == OrderSide.SELL)
                    {
                        // 売りの時はマイナスする
                        cancelSize = cancelSize *= -1;
                    }
                    Console.WriteLine($"{StopLossMinute} 分経過のため決済：{item.OpenPrice}");
                    totalCancelSize += cancelSize;
                    item.IsStopLoss = true;
                    Cancel(item.OrderId);
                }
                if (totalCancelSize != 0)
                {
                    totalCancelSize = NonApiUtility.ToRoundDown(totalCancelSize, OrderFigures);
                    Console.WriteLine("決済:" + totalCancelSize);
                    if (totalCancelSize > 0)
                    {
                        await client.Sell(totalCancelSize);
                    }
                    else
                    {
                        await client.Buy(-totalCancelSize);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Logger.ErrorLog(ex.StackTrace);
            }
        }
        #endregion
    }
}
