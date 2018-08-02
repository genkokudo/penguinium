using Penguinium.Client;
using Penguinium.Common;
using Penguinium.Manager;
using Penguinium.Technical;
using System;
using System.Collections.Generic;

namespace Penguinium.Algorithm
{
    /// <summary>
    /// チャネルブレイクアウト戦略
    /// 成行版
    /// </summary>
    public class ChannelBreakOutStrategyNonStop : Algorithm
    {
        #region フィールド
        protected SinglePositionManager positionManager;

        // 注文用カウンタ
        protected Counter orderCounter;
        /// <summary>
        /// 処理の周期（秒）
        /// </summary>
        protected int ExecutePeriod { get { return (int)Parameter[0]; } }

        /// <summary>
        /// 1回の注文量
        /// </summary>
        protected double OrderAmount { get { return Parameter[1]; } }

        #endregion

        #region readonly
        ///// <summary>
        ///// 1秒足の単純履歴
        ///// </summary>
        //private string baseHistory1SKey = "";

        ///// <summary>
        ///// 15分足のチャネル
        ///// </summary>
        //private string baseChannelKey = "";
        #endregion

        /// <summary>
        /// 1秒単純履歴
        /// </summary>
        protected SimpleHistory BaseHistory
        {
            get { return (SimpleHistory)BaseTechnical.AllHistory[(int)Parameter[2]]; }
        }

        /// <summary>
        /// チャネル
        /// </summary>
        protected Channel BaseChannel
        {
            get { return (Channel)BaseTechnical.AllHistory[(int)Parameter[3]]; }
        }

        /// <summary>
        /// チャネルブレイクアウト戦略
        /// 成行版
        /// </summary>
        /// <param name="exchangeId">取引所ID</param>
        /// <param name="status">BOTの状態</param>
        public ChannelBreakOutStrategyNonStop(int exchangeId, BotStatusManager status, string baseChannelKey, List<double> parameter)
            : base(exchangeId, status, parameter)
        {
            orderCounter = new Counter(ExecutePeriod);
            //interval = new CountDown(60);
            //interval.InitCount();

            //baseHistory1SKey = HistoryProperty.GetKey(HistoryKind.HISTORY, 1, "1s");
            //this.baseChannelKey = baseChannelKey;

            // 単体ポジション管理
            positionManager = new SinglePositionManager(OrderAmount, Client);
        }

        #region Update
        /// <summary>
        /// 毎秒実行される
        /// </summary>
        public override async void Update()
        {
            try
            {
                var currentPrice = NonApiUtility.Last(BaseHistory.BaseList);
                //interval.Update();
                //if (!interval.IsCounting)
                //{
                if (positionManager.GetIsBreaking(currentPrice))
                {
                    // ブレイク検出、急いで成行注文
                    if (positionManager.NextTurn == OrderSide.BUY)
                    {
                        // 売る
                        Console.WriteLine($"売：{currentPrice}");
                        positionManager.Sell();
                        //interval.InitCount();
                        positionManager.BuyOrderPrice = BaseChannel.CurrentMax;
                        Console.WriteLine($"次の買トリガー：{currentPrice} -> {positionManager.BuyOrderPrice}");
                    }
                    if (positionManager.NextTurn == OrderSide.SELL)
                    {
                        // 買う
                        Console.WriteLine($"買：{currentPrice}");
                        positionManager.Buy();
                        //interval.InitCount();
                        positionManager.SellOrderPrice = BaseChannel.CurrentMin;
                        Console.WriteLine($"次の売トリガー：{positionManager.SellOrderPrice} <- {currentPrice}");
                    }
                }
                //}

                if (orderCounter.Update())
                {
                    if (await Client.CheckServerStatus())
                    {
                        if (BaseChannel.ReadyCount <= 0)
                        {
                            positionManager.BuyOrderPrice = BaseChannel.CurrentMax;
                            positionManager.SellOrderPrice = BaseChannel.CurrentMin;
                            // 次の注文設定
                            switch (positionManager.NextTurn)
                            {
                                case OrderSide.BUYSELL: // 初回のみ
                                    Console.WriteLine($"初回トリガー：{positionManager.SellOrderPrice} <- {currentPrice} -> {positionManager.BuyOrderPrice}");
                                    break;
                                case OrderSide.BUY:
                                    Console.WriteLine($"買トリガー：{currentPrice} -> {positionManager.BuyOrderPrice}");
                                    break;
                                case OrderSide.SELL:
                                    Console.WriteLine($"売トリガー：{positionManager.SellOrderPrice} <- {currentPrice}");
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"準備中：{BaseChannel.ReadyCount}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }
        #endregion

    }
}
