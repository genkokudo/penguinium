//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Chickenium;
//using Chickenium.Dao;
//using Penguinium.Client;
//using Penguinium.Common;
//using Penguinium.Manager;
//using Penguinium.ProcessData;
//using Penguinium.Technical;

//namespace Penguinium.Algorithm
//{
//    /// <summary>
//    /// 20秒間のVolを見て、多い方に乗る
//    /// 儲からない
//    /// </summary>
//    public class TradeByVol : Algorithm
//    {
//        // 注文量
//        protected double orderAmount = 0.01;
//        // 値幅の基本値
//        protected double baseOrderHeight = 400;

        
//        // 建玉の最大量
//        protected double maxOrderLimit = 0.08;
//        // 現在の建玉量
//        protected double currentAmount = 0;
//        // 閾値
//        protected double lineAmount = 10;

//        // 値幅を可変にする
//        protected bool isFlexLot = false;
//        // 標準偏差の何倍の値幅を取るか
//        protected double heightMultipleSigma = 1.0;

//        // キャンセルまでの時間（分）
//        protected int cancelMinute = 10;
//        // 損切りまでの時間（分）
//        protected int stopLossMinute = 30;
//        // 処理の周期（秒）
//        protected int executePeriod = 20;
//        // 処理の周期上でキャンセル処理を行うタイミング（秒）
//        protected int cancelTiming = 10;

//        // 注文用カウンタ
//        Counter orderCounter;
//        // キャンセル用カウンタ
//        Counter cancelCounter;

//        #region readonly
//        /// <summary>
//        /// 1秒足の単純履歴
//        /// </summary>
//        private string baseHistory1SKey = "";

//        /// <summary>
//        /// 1分足のボリバン
//        /// </summary>
//        private string baseBand1MKey = "";
//        #endregion

//        /// <summary>
//        /// 1秒単純履歴
//        /// </summary>
//        protected SimpleHistory BaseHistory
//        {
//            get { return (SimpleHistory)BaseTechnical.AllHistory[baseHistory1SKey]; }
//        }

//        /// <summary>
//        /// 1分ボリバン
//        /// </summary>
//        protected BollingerBand BaseBollingerBand
//        {
//            get { return (BollingerBand)BaseTechnical.AllHistory[baseBand1MKey]; }
//        }

//        /// <summary>
//        /// 買いと売りのボリューム差
//        /// プラスなら買いの方が多い
//        /// TODO:1度呼び出すとリセットされるので注意
//        /// 直し方を考えること！
//        /// </summary>
//        protected double Vol
//        {
//            get
//            {
//                return status.PubnubList[exchangeId][(int)PubNubUse.Execution].BuyVol - status.PubnubList[exchangeId][(int)PubNubUse.Execution].SellVol;
//            }
//        }

//        /// <summary>
//        /// ループイフダン
//        /// </summary>
//        /// <param name="exchangeId">取引所ID</param>
//        /// <param name="status">BOTの状態</param>
//        public TradeByVol(int exchangeId, BotStatusManager status, List<double> parameter)
//            : base(exchangeId, status, parameter)
//        {
//            orderCounter = new Counter(executePeriod);
//            cancelCounter = new Counter(executePeriod, cancelTiming);

//            //baseHistory1SKey = HistoryProperty.GetKey(HistoryKind.HISTORY, 1, "1s");
//            //baseBand1MKey = HistoryProperty.GetKey(HistoryKind.BollingerBand, 2, "1m");
//        }    

//        /// <summary>
//        /// 毎秒実行されるよ
//        /// </summary>
//        public override async void Update()
//        {
//            // 現在価格を取得
//            var currentPrice = NonApiUtility.last(BaseHistory.BaseList);
//            if (currentPrice <= 0) return;

//            try
//            {
//                if (cancelCounter.Update())
//                {
//                    OrderCancel(cancelMinute, OrderSide.BUYSELL);
//                    await OrderStopLoss(stopLossMinute, OrderSide.BUYSELL);
//                }

//                // 20秒ごとに実行
//                if (orderCounter.Update())
//                {
//                    if (Client.IsBreaking)
//                    {
//                        Console.WriteLine("ブレイク処理中なのでIFDはお休みします。");
//                    }
//                    else
//                    {
//                        if (await Client.CheckServerStatus())
//                        {
//                            // 最終的に注文する額を集計する（クラス化した方が良い？）
//                            double finalOrderAmount = 0;

//                            // 値幅を決定
//                            var orderHeight = CalcOrderHeight(BaseBollingerBand.Sigma);
//                            // 注文価格を決定(値幅の余りを切り捨てる)
//                            var orderPrice = currentPrice - currentPrice % orderHeight;

//                            var tempVol = Vol;

//                            // 手じまい
//                            if (tempVol > 0 && currentAmount < 0)
//                            {
//                                var cancelSize = -currentAmount;
//                                Console.WriteLine($"決済買:{ currentPrice }\tVol:{ tempVol }\tAmount:{ cancelSize }");
//                                finalOrderAmount += cancelSize;
//                            }
//                            else if(tempVol < 0 && currentAmount > 0)
//                            {
//                                var cancelSize = currentAmount;
//                                Console.WriteLine($"決済売:{ currentPrice }\tVol:{ tempVol }\tAmount:{ cancelSize }");
//                                finalOrderAmount -= cancelSize;
//                            }

//                            if (tempVol > lineAmount)
//                            {
//                                // 買う
//                                finalOrderAmount += orderAmount;
//                                Console.WriteLine($"買:{ currentPrice }\tVol:{ tempVol }\tAmount:{ orderAmount }");
//                            }
//                            else if (tempVol < -lineAmount)
//                            {
//                                // 売る
//                                finalOrderAmount -= orderAmount;
//                                Console.WriteLine($"売:{ currentPrice }\tVol:{ tempVol }\tAmount:{ orderAmount }");
//                            }

//                            // 実際の売買
//                            if (finalOrderAmount > 0)
//                            {
//                                // 買う
//                                finalOrderAmount = NonApiUtility.ToRoundDown(finalOrderAmount, OrderFigures);
//                                Console.WriteLine($"買合計:{ finalOrderAmount }");
//                                await Client.Buy(finalOrderAmount);
//                                currentAmount += finalOrderAmount;
//                            }
//                            else if (finalOrderAmount < 0)
//                            {
//                                // 売る
//                                finalOrderAmount = NonApiUtility.ToRoundDown(-finalOrderAmount, OrderFigures);
//                                Console.WriteLine($"売合計:{ finalOrderAmount }");
//                                await Client.Sell(finalOrderAmount);
//                                currentAmount -= finalOrderAmount;
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Error(ex);
//            }
//        }

//        #region CalcOrderHeight:注文幅の算出
//        /// <summary>
//        /// 注文する幅を算出する
//        /// </summary>
//        /// <param name="sigma">最大値（現在の標準偏差）</param>
//        /// <returns>引数の値未満で基本値の2のn乗数</returns>
//        protected double CalcOrderHeight(double sigma)
//        {
//            if (isFlexLot)
//            {
//                var max = baseOrderHeight + sigma * heightMultipleSigma;
//                if (baseOrderHeight <= 0)
//                {
//                    return 0;
//                }
//                var result = baseOrderHeight;
//                while (result * 2 < max)
//                {
//                    result *= 2;
//                }
//                return result;
//            }
//            else
//            {
//                return baseOrderHeight;
//            }
//        }
//        /// <summary>
//        /// 注文する幅を算出する
//        /// </summary>
//        /// <param name="sigma">最大値（現在の標準偏差）</param>
//        /// <returns>引数の値未満で基本値の2のn乗数</returns>
//        protected double CalcOrderHeight2(double sigma)
//        {
//            var max = sigma * heightMultipleSigma;
//            if (baseOrderHeight <= 0)
//            {
//                return 0;
//            }
//            var result = baseOrderHeight;
//            while (result * 2 < max)
//            {
//                result *= 2;
//            }
//            return result;
//        }
//        #endregion

//    }
//}
