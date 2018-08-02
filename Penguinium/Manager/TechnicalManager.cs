using Penguinium.Technical;
using System;
using System.Collections.Generic;
using System.Linq;
using Chickenium;
using Chickenium.Dao;
using Penguinium.ProcessData;
using Penguinium.Common;
using Penguinium.ApiBridge;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Penguinium.Manager
{
    // TODO:名前はHistoryManagerの方がいいかもしれない
    /// <summary>
    /// テクニカル管理クラス（取引所・通貨ペアごと）
    /// 取引所IDについての全部の値段とテクニカルの履歴を持つ
    /// シグナルとトレンドを持っていて、アクセスできる
    /// 
    /// 平滑移動平均10,21：使えそう？
    /// MACD10,26,9
    /// 平滑移動平均25,75,200：使う！
    /// </summary>
    public class TechnicalManager
    {
        #region フィールド
        /// <summary>
        /// （設定）基本的な配列長さ
        /// 120分データを残すことにする
        /// </summary>
        private const int BaseLength = 120;

        /// <summary>
        /// 定義されている時間足
        /// </summary>
        public Dictionary<int, TimeScale> TimeScaleList { get; set; }
        
        /// <summary>
        /// 初期化が必要な履歴
        /// </summary>
        private List<HistoryProperty> initializeList;

        /// <summary>
        /// 全部の履歴、テクニカル含む
        /// Key:RecordId
        /// </summary>
        public Dictionary<int, HistoryProperty> AllHistory { get; set; }

        /// <summary>
        /// この取引所の1秒単純履歴（余所から参照する）
        /// </summary>
        public SimpleHistory BaseHistory { get; set; }

        /// <summary>
        /// 現在のTicker
        /// </summary>
        private Ticker ticker = null;

        /// <summary>
        /// 取引所ID
        /// </summary>
        int exchangeId = 1;

        /// <summary>
        /// DB接続
        /// </summary>
        private DbContextOptions dbContextOptions;

        /// <summary>
        /// PubNubを参照する場合は設定
        /// </summary>
        private Dictionary<int, PubnubManager> pubNub = null;

        #endregion

        #region コンストラクタ・初期化
        /// <summary>
        /// 取引所・通貨ペアごとのテクニカル管理
        /// </summary>
        /// <param name="exchangeId">取引所ID</param>
        public TechnicalManager(int exchangeId)
        {
            Initialize(exchangeId);
        }

        /// <summary>
        /// SFDなどで参照する取引所
        /// </summary>
        private Dictionary<int, TechnicalManager> RefExchange = null;

        /// <summary>
        /// 取引所・通貨ペアごとのテクニカル管理
        /// PubNubを参照する場合
        /// </summary>
        /// <param name="exchangeId">取引所ID</param>
        /// <param name="pubNub">この取引所のPubNubへの参照（無ければnull）</param>
        /// <param name="dbContextOptions">DB接続</param>
        /// <param name="refExchange">SFDなどで参照する場合設定する、なかったらnull</param>
        public TechnicalManager(int exchangeId, Dictionary<int, PubnubManager> pubNub, Dictionary<int, TimeScale> timeScaleList, DbContextOptions dbContextOptions, Dictionary<int, TechnicalManager> refExchange)
        {
            this.dbContextOptions = dbContextOptions;
            this.pubNub = pubNub;
            TimeScaleList = timeScaleList;
            RefExchange = refExchange;
            Initialize(exchangeId);
        }

        /// <summary>
        /// Initialize the specified exchangeId.
        /// </summary>
        /// <returns>The initialize.</returns>
        /// <param name="exchangeId">Exchange identifier.</param>
        public void Initialize(int exchangeId)
        {
            Logger.Log("テクニカル管理を初期化します 取引所ID:" + exchangeId);
            this.exchangeId = exchangeId;

            // 履歴リスト初期化
            AllHistory = new Dictionary<int, HistoryProperty>();
            initializeList = new List<HistoryProperty>();

            // DBに登録しているテクニカル全てについて、配列を初期化して登録
            InitArray();

            // あるテクニカルについて初期値を算出する
            InitValue();

        }

        /// <summary>
        /// あるテクニカルについて初期値を算出する
        /// </summary>
        private async void InitValue()
        {
            foreach (var item in initializeList)
            {
                List<Candle> candles = await ApiUtility.GetPastPrices(item.TimeScale.SecondsValue, exchangeId == (int)Exchange.BITFLYER_FX);
                foreach (var candle in candles)
                {
                    item.UpdateForInitialize(candle.End);
                }
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 取引所IDに関して
        /// DBに登録しているテクニカル全てについて
        /// 配列を初期化して登録する。
        /// </summary>
        private void InitArray()
        {
            // 全てのテクニカルを作成
            Logger.Log("全てのテクニカル履歴を初期化します 取引所ID:" + exchangeId);
            AllHistory.Clear();

            SimpleHistory baseHistory = null;

            // ※本当はVIEWにした方が良い
            // MRecordから読み込み
            using (var dbr = new MRecordDbContext(dbContextOptions))
            {
                // データを取得
                // TimeScaleIdが番若いものをBaseHistoryに設定するので並べ替える
                var Record = dbr.MRecord.Where(e => e.ExchangeId == exchangeId && e.Enabled == 1).OrderBy(e => e.TechnicalId).ThenBy(e => e.TimeScaleId);
                foreach (var itemr in Record)
                {
                    // この記録対象のタイムスケールを取得
                    var timeScale = TimeScaleList[itemr.TimeScaleId];

                    // MTechnicalから読み込み
                    using (var dbt = new MTechnicalDbContext(dbContextOptions))
                    {
                        // データを取得
                        var Technical = dbt.MTechnical.Where(e => e.Id == itemr.TechnicalId && e.Enabled == 1);
                        foreach (var itemt in Technical)
                        {
                            // テクニカルのパラメータを取得
                            var parameter = GetTechnicalParameter(itemt.Id);

                            // クラス作成
                            switch ((HistoryKind)itemt.ClassId)
                            {
                                case HistoryKind.HISTORY:
                                    var temp1 = new SimpleHistory(timeScale);
                                    AllHistory.Add(itemr.Id, temp1);
                                    if(baseHistory == null)
                                    {
                                        // TimeScaleIdが番若いものをBaseHistoryに設定する
                                        baseHistory = temp1;
                                    }
                                    break;
                                case HistoryKind.SMA:
                                    AllHistory.Add(itemr.Id, new Sma(timeScale, parameter));
                                    break;
                                case HistoryKind.EMA:
                                    AllHistory.Add(itemr.Id, new Ema(timeScale, parameter));
                                    break;
                                case HistoryKind.MACD:
                                    AllHistory.Add(itemr.Id, new Macd(timeScale, parameter));
                                    break;
                                case HistoryKind.BollingerBand:
                                    AllHistory.Add(itemr.Id, new BollingerBand(timeScale, parameter));
                                    break;
                                case HistoryKind.CHANNEL:
                                    AllHistory.Add(itemr.Id, new Channel(timeScale, parameter));
                                    break;
                                case HistoryKind.SFD:
                                    AllHistory.Add(itemr.Id, new Sfd(timeScale, parameter, baseHistory.BaseList, RefExchange, Exchange.BITFLYER));
                                    break;
                            }
                        }
                    }
                }
            }
            BaseHistory = baseHistory;




            // 単純履歴
            //var baseKey = new SimpleHistory(new TimeScale(1, "1s", "1秒", 1));
            //priceHistoryAll.Add(baseKey.Key, baseKey);

            //var emap2_1 = new List<double> { 10 };
            //var ema2_1 = new Sma(new TimeScale(5, "5m", "5分", 300), emap2_1);
            //priceHistoryAll.Add(ema2_1.Key, ema2_1);
            //initializeList.Add(ema2_1);
            //var emap2_2 = new List<double> { 21 };
            //var ema2_2 = new Sma(new TimeScale(6, "5m", "5分", 300), emap2_2);
            //priceHistoryAll.Add(ema2_2.Key, ema2_2);
            //initializeList.Add(ema2_2);

            //// チャネル
            //var channelParam1 = new List<double> { 5 };
            //var channel1 = new Channel(new TimeScale(5, "15m", "15分", 900), channelParam1);
            //priceHistoryAll.Add(channel1.Key, channel1);
            //initializeList.Add(channel1);
            //var channelParam2 = new List<double> { 24 };
            //var channel2 = new Channel(new TimeScale(6, "30m", "30分", 1800), channelParam2);
            //priceHistoryAll.Add(channel2.Key, channel2);
            //initializeList.Add(channel2);
            //var channelParam3 = new List<double> { 27 };
            //var channel3 = new Channel(new TimeScale(7, "30m", "30分", 1800), channelParam3);
            //priceHistoryAll.Add(channel3.Key, channel3);
            //initializeList.Add(channel3);
            //var channelParam4 = new List<double> { 18 };
            //var channel4 = new Channel(new TimeScale(8, "1h", "1時間", 3600), channelParam4);
            //priceHistoryAll.Add(channel4.Key, channel4);
            //initializeList.Add(channel4);

            //var param1 = new List<double> { 12, 26, 9 };
            //var macd1 = new Macd(new TimeScale(1, "1s", "1秒", 1), param1);
            //priceHistoryAll.Add(macd1.Key, macd1);
            //var macd2 = new Macd(new TimeScale(2, "1m", "1分", 60), param1);
            //priceHistoryAll.Add(macd2.Key, macd2);
            //var macd3 = new Macd(new TimeScale(3, "3m", "3分", 180), param1);
            //priceHistoryAll.Add(macd3.Key, macd3);
            //var macd4 = new Macd(new TimeScale(4, "5m", "5分", 300), param1);
            //priceHistoryAll.Add(macd4.Key, macd4);
            //var macd5 = new Macd(new TimeScale(5, "15m", "15分", 900), param1);
            //priceHistoryAll.Add(macd5.Key, macd5);
            //initializeList.Add(macd5);
            ////var macd6 = new Macd(new TimeScale(6, "30m", "30分", 1800), param1);
            ////priceHistoryAll.Add(macd6.Key, macd6);
            ////var macd7 = new Macd(new TimeScale(7, "1h", "1時間", 3600), param1);
            ////priceHistoryAll.Add(macd7.Key, macd7);
            ////var macd8 = new Macd(new TimeScale(8, "2h", "2時間", 7200), param1);
            ////priceHistoryAll.Add(macd8.Key, macd8);
            ////var macd9 = new Macd(new TimeScale(9, "4h", "4時間", 14400), param1);
            ////priceHistoryAll.Add(macd9.Key, macd9);

            //// ボリバン
            ////var param2 = new List<double> { 20, 1 };
            //var param2 = new List<double> { 10, 1 };
            //var bb1 = new BollingerBand(new TimeScale(1, "1s", "1秒", 1), param2);
            //priceHistoryAll.Add(bb1.Key, bb1);
            //var bb2 = new BollingerBand(new TimeScale(2, "1m", "1分", 60), param2);
            //priceHistoryAll.Add(bb2.Key, bb2);
            //var bb3 = new BollingerBand(new TimeScale(3, "3m", "3分", 180), param2);
            //priceHistoryAll.Add(bb3.Key, bb3);
            //var bb4 = new BollingerBand(new TimeScale(4, "5m", "5分", 300), param2);
            //priceHistoryAll.Add(bb4.Key, bb4);
            //var bb5 = new BollingerBand(new TimeScale(5, "15m", "15分", 900), param2);
            //priceHistoryAll.Add(bb5.Key, bb5);
            ////var bb6 = new BollingerBand(new TimeScale(6, "30m", "30分", 1800), param2);
            ////priceHistoryAll.Add(bb6.Key, bb6);
            ////var bb7 = new BollingerBand(new TimeScale(7, "1h", "1時間", 3600), param2);
            ////priceHistoryAll.Add(bb7.Key, bb7);
            ////var bb8 = new BollingerBand(new TimeScale(8, "2h", "2時間", 7200), param2);
            ////priceHistoryAll.Add(bb8.Key, bb8);
            ////var bb9 = new BollingerBand(new TimeScale(9, "4h", "4時間", 14400), param2);
            ////priceHistoryAll.Add(bb9.Key, bb9);

            //// TODO:SFDはここに置くのやめる
            //if (RefExchange != null)
            //{
            //    var param3 = new List<double> { 20, 1 };
            //    var sfd1 = new Sfd(new TimeScale(1, "1s", "1秒", 1), param3, baseKey.BaseList, RefExchange, Exchange.BITFLYER);
            //    priceHistoryAll.Add(sfd1.Key, sfd1);
            //}

        }
        #endregion

        /// <summary>
        /// 指定したIDのテクニカルのパラメータをDBから取得する
        /// </summary>
        /// <param name="technicalId"></param>
        /// <returns></returns>
        private List<double> GetTechnicalParameter(int technicalId)
        {
            var result = new List<double>();
            using (var db = new MTechnicalParameterDbContext(dbContextOptions))
            {
                // データを取得
                var TechnicalParameter = db.MTechnicalParameter.Where(e => e.TechnicalId == technicalId && e.Enabled == 1).OrderBy(e => e.Id);
                foreach (var item in TechnicalParameter)
                {
                    result.Add(item.Value);
                }
            }
            return result;
        }

        #region Update:毎秒更新（必ず毎秒）
        /// <summary>
        /// 毎秒呼び出して更新
        /// </summary>
        /// <param name="ticker">Ticker</param>
        public void Update(Ticker ticker)
        {
            try
            {
                // tickerを更新
                this.ticker = ticker;

                // TODO:PubNub参照している場合の更新（もーちょっと実装シンプルにならないか？？）
                if (pubNub != null && pubNub.Count() > 1)
                {
                    foreach (var item in AllHistory.Values)
                    {
                        if (item.HistoryKind == HistoryKind.HISTORYREF)
                        {
                            // 履歴に追加
                            // TODO:完全にダメな実装
                            item.Update(pubNub[(int)PubNubUse.Ticker].Ticker);
                        }
                    }
                }

                // 全てのTimeScaleの単純履歴を更新
                foreach (var item in AllHistory.Values)
                {
                    if (item.HistoryKind == HistoryKind.HISTORY)
                    {
                        // 履歴に追加
                        item.Update(ticker);
                    }
                }

                // 全てのTimeScaleの単純履歴以外を更新
                foreach (var item in AllHistory.Values)
                {
                    if (item.HistoryKind != HistoryKind.HISTORY && item.HistoryKind != HistoryKind.HISTORYREF)
                    {
                        item.Update(ticker);
                    }
                }

                #region 指定時間を過ぎてたら古いデータを削除
                foreach (var item in AllHistory.Values)
                {
                    item.RemoveLimit(BaseLength);
                }
                #endregion

            }
            catch (Exception ex)
            {
                Logger.Log("Updateでエラーが発生しました 取引所ID:" + exchangeId);
                System.IO.File.AppendAllText("./error.txt", ex.StackTrace);
            }
        }
        #endregion
    }
}

