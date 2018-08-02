using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chickenium;
using Penguinium.Client;
using Penguinium.Common;
using Penguinium.Manager;
using Penguinium.ProcessData;

namespace Penguinium.Algorithm
{
    /// <summary>
    /// 売買アルゴのスーパークラス
    /// </summary>
    public abstract class Algorithm
    {
        #region フィールド
        // 現在のBOT全体の状態管理
        protected BotStatusManager status;

        protected List<double> Parameter { get; set; }

        /// <summary>
        /// 取引所ID
        /// </summary>
        protected int exchangeId;

        /// <summary>
        /// 注文数量を小数第何位までにするか
        /// </summary>
        protected const int OrderFigures = 2;

        /// <summary>
        /// クライアント
        /// ここから各取引所のAPI操作をする
        /// </summary>
        protected ExchangeClient Client
        {
            get { return status.Client[exchangeId]; }
        }

        /// <summary>
        /// 基本テクニカル
        /// </summary>
        protected TechnicalManager BaseTechnical
        {
            get { return status.TechnicalList[exchangeId]; }
        }

        /// <summary>
        /// 資産情報
        /// ノーポジの時は0になるので注意
        /// </summary>
        protected Collateral BaseCollateral
        {
            get { return status.CollateralList[exchangeId]; }
        }

        /// <summary>
        /// 親注文情報
        /// </summary>
        protected List<Order> ParentOrders
        {
            get { return status.ParentOrderList[exchangeId]; }
        }

        /// <summary>
        /// 子注文情報
        /// </summary>
        protected List<Order> ChildOrders
        {
            get { return status.ChildOrderList[exchangeId]; }
        }

        /// <summary>
        /// 建玉情報
        /// </summary>
        protected List<Position> Positions
        {
            get { return status.PositionList[exchangeId]; }
        }

        // パラメータへの参照
        protected Dictionary<string, string> applicationSettings;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// 売買アルゴのスーパークラス
        /// </summary>
        /// <param name="exchangeId">取引所ID</param>
        /// <param name="status">BOTの状態</param>
        /// <param name="status">パラメータ</param>
        public Algorithm(int exchangeId, BotStatusManager status, List<double> parameter)
        {
            Parameter = parameter;
            this.exchangeId = exchangeId;
            this.status = status;
        }
        #endregion

        #region Update:更新、売買処理
        /// <summary>
        /// 更新
        /// </summary>
        public virtual async void Update()
        {
            await Task.Run(() => Console.WriteLine("継承して何か処理を指定してください"));
        }
        #endregion

        #region CancelOrderAsync:指定範囲の未消化の注文をキャンセルする
        /// <summary>
        /// 指定範囲の未消化の注文をキャンセルする
        /// </summary>
        /// <param name="minPrice"></param>
        /// <param name="maxPrice"></param>
        protected void CancelOrderAsync(double minPrice, double maxPrice)
        {
            if (ParentOrders != null)
            {
                var list = ParentOrders.Where(e => e.Price > minPrice && e.Price < maxPrice && e.Size == e.OutstandingSize);
                foreach (var item in list)
                {
                    Client.AddCancelParentOrderList(item);
                }
            }
            else
            {
                Console.WriteLine("注文情報取得失敗のため、注文のキャンセルをしません");
            }
        }
        #endregion

        /// <summary>
        /// 買い建ての親注文ならばtrue
        /// とりあえず偶数とする
        /// </summary>
        /// <param name="order">注文情報</param>
        /// <returns>買い建ての親注文ならばtrue</returns>
        protected bool IsBuyOrder(Order order)
        {
            return order.Price % 2 == 0;
        }

        protected void Error(Exception ex)
        {
            Logger.Log(ex.TargetSite.DeclaringType + "で何かエラーが出ました");
            Console.WriteLine(ex.Message);
            System.IO.File.AppendAllText("./error.txt", ex.StackTrace);
        }

        #region 一定時間経過した注文に対する処理
        /// <summary>
        /// 一定時間経過した注文の取消を行う
        /// </summary>
        /// <returns></returns>
        protected void OrderCancel(int cancelMinute, OrderSide side)
        {
            if (ParentOrders != null)
            {
                var now = DateTime.Now;
                var list = ParentOrders.Where(e => (now - e.Date).TotalMinutes > cancelMinute && e.Side == side);
                foreach (var item in list)
                {
                    if (item.Size == item.OutstandingSize)
                    {
                        Console.WriteLine(cancelMinute + "分経過のためキャンセル：" + item.Price);
                        Client.AddCancelParentOrderList(item);
                    }
                }
            }
            else
            {
                Console.WriteLine("注文情報取得失敗のため、注文のキャンセルをしません");
            }
        }

        /// <summary>
        /// 一定時間経過した注文を強制決済する
        /// </summary>
        /// <returns></returns>
        protected async Task OrderStopLoss(int stopLossMinute, OrderSide side)
        {
            try
            {
                if (ParentOrders != null)
                {
                    var now = DateTime.Now;
                    var list = ParentOrders.Where(e => (now - e.Date).TotalMinutes > stopLossMinute && e.Side == side);
                    double totalCancelSize = 0;
                    foreach (var item in list)
                    {
                        var cancelSize = item.Size - item.OutstandingSize;
                        if (!IsBuyOrder(item))
                        {
                            // 売りの時はマイナスする
                            cancelSize = cancelSize *= -1;
                        }
                        Console.WriteLine(stopLossMinute + "分経過のため決済：" + item.Price + ":" + cancelSize);
                        totalCancelSize += cancelSize;
                    }
                    if (totalCancelSize != 0)
                    {
                        totalCancelSize = NonApiUtility.ToRoundDown(totalCancelSize, OrderFigures);
                        Console.WriteLine("決済:" + totalCancelSize);
                        if (totalCancelSize > 0)
                        {
                            await Client.Sell(totalCancelSize);
                        }
                        else
                        {
                            await Client.Buy(-totalCancelSize);
                        }
                    }
                    foreach (var item in list)
                    {
                        Client.AddCancelParentOrderList(item);
                    }
                }
                else
                {
                    Console.WriteLine("注文情報取得失敗のため、強制決済をしません");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
        #endregion

        #region 指定した範囲の注文を強制削除（決済はしない）
        /// <summary>
        /// IFDで一定価格以下の売り建ての注文を削除する
        /// 決済はしないので注意
        /// 値段でのフィルタもしていないので注意
        /// </summary>
        /// <returns></returns>
        protected void SellOrderForceCancel(double maxPrice)
        {
            if (ParentOrders != null)
            {
                var list = ParentOrders.Where(e => e.Price < maxPrice && e.Side == OrderSide.BUYSELL);
                foreach (var item in list)
                {
                    Console.WriteLine("売り建て注文を強制キャンセル：" + item.Price);
                    Client.AddCancelParentOrderList(item);
                }
            }
            else
            {
                Console.WriteLine("注文情報取得失敗のため、注文のキャンセルをしません");
            }
        }
        /// <summary>
        /// IFDで買い建ての注文を削除する
        /// 決済はしないので注意
        /// 値段でのフィルタもしていないので注意
        /// </summary>
        /// <returns></returns>
        protected void BuyOrderForceCancel(double minPrice)
        {
            if (ParentOrders != null)
            {
                var list = ParentOrders.Where(e => e.Price > minPrice && e.Side == OrderSide.BUYSELL);
                foreach (var item in list)
                {
                    Console.WriteLine("買い建て注文を強制キャンセル：" + item.Price);
                    Client.AddCancelParentOrderList(item);
                }
            }
            else
            {
                Console.WriteLine("注文情報取得失敗のため、注文のキャンセルをしません");
            }
        }

        /// <summary>
        /// 指定した値段分の売り建ての注文を安い方から削除する
        /// 決済はしないので注意
        /// </summary>
        /// <returns></returns>
        protected async Task SellOrderForceCancelAmount(double totalAmount)
        {
            if (totalAmount > 0.001)
            {
                Console.WriteLine("最新のオーダー情報を臨時取得します");
                var TempOrders = await Client.GetMyActiveParentOrders();

                var list = TempOrders.Where(e => e.Side == OrderSide.BUYSELL && e.Size != e.OutstandingSize).OrderBy(e => e.Price); // 安い順
                foreach (var item in list)
                {
                    Console.WriteLine("売り建て注文を逃げ強制キャンセル：" + item.Price);
                    var cancelSize = item.Size - item.OutstandingSize;

                    await Client.CancelOrder(item);
                    totalAmount -= cancelSize;
                    if (totalAmount < 0.001)
                    {
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// 指定した値段分の買い建ての注文を高い方から削除する
        /// 決済はしないので注意
        /// </summary>
        /// <returns></returns>
        protected async Task BuyOrderForceCancelAmount(double totalAmount)
        {
            if (totalAmount > 0.001)
            {
                Console.WriteLine("最新のオーダー情報を臨時取得します");
                var TempOrders = await Client.GetMyActiveParentOrders();

                var list = TempOrders.Where(e => e.Side == OrderSide.BUYSELL && e.Size != e.OutstandingSize).OrderByDescending(e => e.Price); // 高い順
                foreach (var item in list)
                {
                    Console.WriteLine("買い建て注文を逃げ強制キャンセル：" + item.Price);
                    var cancelSize = item.Size - item.OutstandingSize;

                    await Client.CancelOrder(item);
                    totalAmount -= cancelSize;
                    if (totalAmount < 0.001)
                    {
                        break;
                    }
                }
            }
        }
        #endregion
    }
}