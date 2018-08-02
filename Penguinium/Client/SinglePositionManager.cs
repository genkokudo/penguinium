using Chickenium;
using Penguinium.Common;
using Penguinium.ProcessData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Penguinium.Client
{
    /// <summary>
    /// 1つだけ建玉を持つアルゴリズムの注文と建玉管理
    /// </summary>
    public class SinglePositionManager
    {
        /// <summary>
        /// 注文を1度もしていない状態
        /// </summary>
        protected const string EmptyOrder = "empty";

        /// <summary>
        /// クライアント
        /// </summary>
        ExchangeClient client;

        // 連続で買い・売りにならないように制御する
        // 次は買うのか売るのか
        public OrderSide NextTurn
        {
            get; set;
        }

        private double orderAmount;
        /// <summary>
        /// 注文量
        /// </summary>
        public double OrderAmount
        {
            get {
                if (IsFirst)
                {
                    return NonApiUtility.ToRoundDown(orderAmount * 0.5, 3);
                }
                return orderAmount;
            }
            set { orderAmount = value; }
        }

        /// <summary>
        ///  注文した買い値
        /// </summary>
        public double BuyOrderPrice
        {
            get; set;
        }

        /// <summary>
        ///  注文した売り値
        /// </summary>
        public double SellOrderPrice
        {
            get; set;
        }

        /// <summary>
        ///  買い注文した数量
        /// </summary>
        public double BuyOrderAmount
        {
            get; set;
        }

        /// <summary>
        ///  売り注文した数量
        /// </summary>
        public double SellOrderAmount
        {
            get; set;
        }

        /// <summary>
        /// 現在の買い注文ID、初回はEmptyOrder
        /// </summary>
        public string BuyOrderId
        {
            get; set;
        }

        /// <summary>
        /// 現在の売り注文ID、初回はEmptyOrder
        /// </summary>
        public string SellOrderId
        {
            get; set;
        }

        /// <summary>
        /// 初回ならばtrue
        /// </summary>
        public bool IsFirst
        {
            get { return BuyOrderId == EmptyOrder && SellOrderId == EmptyOrder; }
        }

        /// <summary>
        /// 成行用
        /// STOP注文を踏み越えているならばtrue
        /// 次の注文方向を変更する
        /// </summary>
        /// <param name="currentPrice">現在価格</param>
        /// <returns>STOP注文を踏み越えているならばtrue</returns>
        public bool GetIsBreaking(double currentPrice)  // ネーミング良くない
        {
            switch (NextTurn)
            {
                case OrderSide.BUYSELL:
                    if (BuyOrderPrice > 0 && currentPrice > BuyOrderPrice)
                    {
                        NextTurn = OrderSide.SELL;
                        return true;
                    }
                    if (SellOrderPrice > 0 && currentPrice < SellOrderPrice)
                    {
                        NextTurn = OrderSide.BUY;
                        return true;
                    }
                    break;
                case OrderSide.BUY:
                    if (BuyOrderPrice > 0 && currentPrice > BuyOrderPrice)
                    {
                        NextTurn = OrderSide.SELL;
                        return true;
                    }
                    break;
                case OrderSide.SELL:
                    if (SellOrderPrice > 0 && currentPrice < SellOrderPrice)
                    {
                        NextTurn = OrderSide.BUY;
                        return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderAmount"></param>
        /// <param name="client"></param>
        public SinglePositionManager(double orderAmount, ExchangeClient client)
        {
            NextTurn = OrderSide.BUYSELL;
            this.client = client;
            OrderAmount = orderAmount;
            BuyOrderPrice = -1;
            SellOrderPrice = -1;
            BuyOrderId = EmptyOrder;
            SellOrderId = EmptyOrder;
        }


        #region 注文
        /// <summary>
        /// Stop買いを行う
        /// </summary>
        /// <param name="price">トリガー価格</param>
        public virtual async void StopBuy(double price)
        {
            //buyOrderPrice = BaseChannel.CurrentMax;
            Console.WriteLine($"Stop買：{price}");
            BuyOrderPrice = price;
            BuyOrderAmount = OrderAmount;
            var buyResult = await client.StopBuy(BuyOrderPrice, BuyOrderAmount);
            BuyOrderId = buyResult.OrderId;
        }

        /// <summary>
        /// Stop売りを行う
        /// </summary>
        /// <param name="price">トリガー価格</param>
        public virtual async void StopSell(double price)
        {
            //sellOrderPrice = BaseChannel.CurrentMin;
            Console.WriteLine($"Stop売：{price}");
            SellOrderPrice = price;
            SellOrderAmount = OrderAmount;
            var sellResult = await client.StopSell(SellOrderPrice, SellOrderAmount);
            SellOrderId = sellResult.OrderId;
        }

        /// <summary>
        /// Stop買いの再注文を行う
        /// </summary>
        /// <param name="orders">現在の注文リスト</param>
        /// <param name="price">トリガー価格</param>
        public virtual async void ReOrderBuy(List<Order> orders, double price)
        {
            if (BuyOrderPrice != price || OrderAmount != BuyOrderAmount)
            {
                var order = GetBuyOrder(orders);
                Logger.DebugLog($"買い再注文：{ BuyOrderPrice } -> { price }, { BuyOrderAmount } -> { OrderAmount }");
                await client.CancelOrder(order);
                StopBuy(price);
            }
        }

        /// <summary>
        /// Stop売りの再注文を行う
        /// </summary>
        /// <param name="orders">現在の注文リスト</param>
        /// <param name="price">トリガー価格</param>
        public virtual async void ReOrderSell(List<Order> orders, double price)
        {
            if (SellOrderPrice != price || OrderAmount != SellOrderAmount)
            {
                var order = GetSellOrder(orders);
                Logger.DebugLog($"売り再注文：{ SellOrderPrice } -> { price }, { SellOrderAmount } -> { OrderAmount }");
                await client.CancelOrder(order);
                StopSell(price);
            }
        }

        /// <summary>
        /// 成買
        /// </summary>
        public virtual async void Buy()
        {
            try
            {
                BuyOrderAmount = OrderAmount;
                var buyResult = await client.Buy(BuyOrderAmount);
                BuyOrderId = buyResult.OrderId;
            }catch(Exception ex)
            {
                Logger.ErrorLog(ex.StackTrace);
            }
        }

        /// <summary>
        /// 成売
        /// </summary>
        public virtual async void Sell()
        {
            try
            {
                SellOrderAmount = OrderAmount;
                var sellResult = await client.Sell(SellOrderAmount);
                SellOrderId = sellResult.OrderId;
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex.StackTrace);
            }
        }




        /// <summary>
        /// Stop買いを行う
        /// そのあとトレーリングする
        /// </summary>
        /// <param name="price">トリガー価格</param>
        /// <param name="offset">トレーリングの幅</param>
        public virtual async void StopBuyTrail(double price, double offset)
        {
            try
            {
                BuyOrderPrice = price;
                var buyResult1 = await client.StopBuyTrail(BuyOrderPrice, OrderAmount, offset);
                BuyOrderId = buyResult1.OrderId;
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex.StackTrace);
            }
        }

        /// <summary>
        /// Stop売りを行う
        /// そのあとトレーリングする
        /// </summary>
        /// <param name="price">トリガー価格</param>
        /// <param name="offset">トレーリングの幅</param>
        public virtual async void StopSellTrail(double price, double offset)
        {
            try
            {
                SellOrderPrice = price;
                var sellResult1 = await client.StopSellTrail(SellOrderPrice, OrderAmount, offset);
                SellOrderId = sellResult1.OrderId;
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex.StackTrace);
            }
        }


        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orders">現在の注文リスト</param>
        /// <returns></returns>
        public Order GetBuyOrder(List<Order> orders)
        {
            return orders.FirstOrDefault(e => (e.OrderId == BuyOrderId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orders">現在の注文リスト</param>
        /// <returns></returns>
        public Order GetSellOrder(List<Order> orders)
        {
            return orders.FirstOrDefault(e => (e.OrderId == SellOrderId));
        }
    }
}
