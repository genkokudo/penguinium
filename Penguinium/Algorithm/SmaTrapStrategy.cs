using Penguinium.Client;
using Penguinium.Common;
using Penguinium.Manager;
using Penguinium.Technical;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Penguinium.Algorithm
{
    /// <summary>
    /// SMAでトラリピ売買
    /// </summary>
    public class SmaTrapStrategy : Algorithm
    {
        #region パラメータ
        /// <summary>
        /// SFDによって売買変更する
        /// BFFX以外は0にすること
        /// </summary>
        protected bool SfdMode { get { return Parameter[0] == 1; } }
        
        /// <summary>
        /// 処理の周期（秒）
        /// </summary>
        protected int ExecutePeriod { get { return (int)Parameter[1]; } }  // 適当に設定している
        
        /// <summary>
        /// 決済値幅の基本値
        /// </summary>
        protected double BaseOrderHeight { get { return Parameter[2]; } }
        
        /// <summary>
        /// 1回の注文量
        /// </summary>
        protected double OrderAmount { get { return Parameter[3]; } }

        /// <summary>
        /// キャンセルまでの時間（分）
        /// </summary>
        int CancelMinute { get { return (int)Parameter[4]; } }

        /// <summary>
        /// 損切りまでの時間（分）
        /// </summary>
        int StopLossMinute { get { return (int)Parameter[5]; } }

        #endregion

        #region フィールド
        // 注文用カウンタ
        protected Counter orderCounter;

        // 注文管理
        protected TrapPositionManager tpm;
        #endregion

        //#region readonly
        ///// <summary>
        ///// 1秒足の単純履歴
        ///// </summary>
        //private string baseHistory1SKey = "";

        ///// <summary>
        ///// SMA短期
        ///// </summary>
        //private string baseSmaShort = "";
        ///// <summary>
        ///// SMA長期
        ///// </summary>
        //private string baseSmaLong = "";
        ///// <summary>
        ///// SFD
        ///// </summary>
        //private string baseSfd = "";
        //#endregion

        /// <summary>
        /// 1秒単純履歴
        /// </summary>
        protected SimpleHistory BaseHistory
        {
            get { return (SimpleHistory)BaseTechnical.AllHistory[(int)Parameter[6]]; }
        }

        /// <summary>
        /// SMA短期
        /// </summary>
        protected Sma BaseSmaShort
        {
            get { return (Sma)BaseTechnical.AllHistory[(int)Parameter[7]]; }
        }

        /// <summary>
        /// SMA長期
        /// </summary>
        protected Sma BaseSmaLong
        {
            get { return (Sma)BaseTechnical.AllHistory[(int)Parameter[8]]; }
        }

        /// <summary>
        /// SFD
        /// </summary>
        protected Sfd BaseSfd
        {
            get
            {
                if (Parameter.Count() < 9) return null;
                return (Sfd)BaseTechnical.AllHistory[(int)Parameter[9]];
            }
        }

        /// <summary>
        /// SMAでトラリピ売買
        /// パラメータ
        /// 0:SfdMode SFDの時は一方向のみ 0
        /// 1:executePeriod 処理の周期（秒）60
        /// 2:baseOrderHeight 値幅の基本値 4000
        /// 3:orderAmount 1回の注文量 0.01
        /// 4:CancelMinute キャンセルまでの時間（分） 15
        /// 5:StopLossMinute 損切りまでの時間（分） 10080
        /// 6:価格参照キー
        /// 7:SMA短期キー
        /// 8:SMA長期キー
        /// 9:SFDキー（null可）
        /// </summary>
        /// <param name="exchangeId">取引所ID</param>
        /// <param name="status">BOTの状態</param>
        /// <param name="status">パラメータ</param>
        public SmaTrapStrategy(int exchangeId, BotStatusManager status, List<double> parameter)
            : base(exchangeId, status, parameter)
        {
            orderCounter = new Counter(ExecutePeriod);

            //baseHistory1SKey = HistoryProperty.GetKey(HistoryKind.HISTORY, 1, "1s");

            //// SMA
            //baseSmaShort = HistoryProperty.GetKey(HistoryKind.SMA, 5, "5m");
            //baseSmaLong = HistoryProperty.GetKey(HistoryKind.SMA, 6, "5m");

            //// SFD
            //baseSfd = HistoryProperty.GetKey(HistoryKind.SFD, 1, "1s");

            // トラリピの注文管理
            tpm = new TrapPositionManager(Client, CancelMinute, StopLossMinute);
        }

        #region Update
        /// <summary>
        /// 毎秒実行される
        /// </summary>
        public override async void Update()
        {
            try
            {
                // 現在価格
                var currentPrice = NonApiUtility.Last(BaseHistory.BaseList);

                if (orderCounter.Update())
                {
                    if (ParentOrders != null)
                    {
                        // トレンド判定
                        OrderSide trend = OrderSide.BUYSELL;
                        string message = "";
                        Console.WriteLine($"S{BaseSmaShort.CurrentValue} L{BaseSmaLong.CurrentValue}");

                        if (BaseSmaShort.CurrentValue > BaseSmaLong.CurrentValue)
                        {
                            trend = OrderSide.BUY;
                            message = "上げ買い";
                        }
                        else if (BaseSmaShort.CurrentValue < BaseSmaLong.CurrentValue)
                        {
                            trend = OrderSide.SELL;
                            message = "下げ売り";
                        }

                        if (SfdMode)
                        {
                            if (trend == OrderSide.BUY && BaseSfd.IsSfdModeHigh)
                            {
                                message = $"SFD{BaseSfd.CurrentValue}なので買い中止";
                                trend = OrderSide.BUYSELL;
                            }
                            else if (trend == OrderSide.SELL && BaseSfd.IsSfdModeLow)
                            {
                                message = $"SFD{BaseSfd.CurrentValue}なので売り中止";
                                trend = OrderSide.BUYSELL;
                            }
                        }

                        // 注文管理更新
                        tpm.Update(ParentOrders, trend);

                        if (await Client.CheckServerStatus())
                        {
                            // 値幅を決定
                            var orderHeight = CalcOrderHeight();

                            if (trend == OrderSide.BUY)
                            {
                                // 上げトレンド
                                Console.WriteLine(message);
                                tpm.Buy(currentPrice, orderHeight, OrderAmount);
                            }
                            else if (trend == OrderSide.SELL)
                            {
                                // 下げトレンド
                                Console.WriteLine(message);
                                tpm.Sell(currentPrice, orderHeight, OrderAmount);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("注文情報取得失敗のため、トラリピの更新をしません");
                    }
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }
        #endregion

        #region CalcOrderHeight:注文幅の算出
        /// <summary>
        /// 注文する幅を算出する
        /// </summary>
        /// <returns>引数の値未満で基本値の2のn乗数</returns>
        protected double CalcOrderHeight()
        {
            var max = BaseOrderHeight;
            return max;
        }
        #endregion
    }
}
