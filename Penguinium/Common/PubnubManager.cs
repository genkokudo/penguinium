using System;
using System.Linq;
using Newtonsoft.Json;
using Penguinium.ApiBridge;
using Penguinium.ProcessData;
using PubnubApi;
using Chickenium;
using System.Collections.Generic;
using Penguinium.Manager;

namespace Penguinium.Common
{
    /// <summary>
    /// PubNubでデータをリアルタイム取得
    /// Start()で開始する
    /// </summary>
    public class PubnubManager
    {
        // TODO:あとで継承
        private PubNubUse useId = PubNubUse.Ticker;

        #region フィールド
        private Pubnub pubnub;
        private RawTicker latestRawTicker = null;
        private Ticker latestTicker = null;
        private string channel = null;
        private string status = null;

        /// <summary>
        /// PubNubきれてないかチェックするカウント
        /// </summary>
        private int restartCount = 0;

        /// <summary>
        /// 設定
        /// この秒数更新がなかったら切れてるかもしれない
        /// </summary>
        private const int checkCount = 300;

        /// <summary>
        /// 前回のローソク足データ
        /// </summary>
        private Candle prevCandle;

        /// <summary>
        /// ローソク足データ
        /// </summary>
        private Candle candle;
        /// <summary>
        /// リアルタイムで最新のローソク足データの取得
        /// </summary>
        public Candle Candle
        {
            get { return candle; }
        }

        private PNConfiguration config;

        // RawExecute集計
        private List<RawExecute> buyVol;
        private List<RawExecute> sellVol;
        /// <summary>
        /// 20秒以内のデータを合計する
        /// </summary>
        public double BuyVol
        {
            get
            {
                var now = DateTime.Now;
                buyVol = buyVol.Where(e => (now - e.Date).Seconds < 20).ToList();
                return buyVol.Sum(e => e.Size);
            }
        }
        /// <summary>
        /// 20秒以内のデータを合計する
        /// </summary>
        public double SellVol
        {
            get
            {
                var now = DateTime.Now;
                sellVol = sellVol.Where(e => (now - e.Date).Seconds < 20).ToList();
                return sellVol.Sum(e => e.Size);
            }
        }

        /// <summary>
        /// 最新のTicker
        /// </summary>
        public Ticker Ticker
        {
            get
            {
                if (status != "CLOSED")
                {
                    restartCount = (restartCount + 1) % checkCount;
                    if (restartCount == 0)
                    {
                        Logger.Log("Pubnub止まってるっぽいので再接続します:" + ToString());
                        ReConnect();
                        restartCount++;
                    }
                }
                return latestTicker;
            }
        }
        #endregion

        /// <summary>
        /// 1分足のリセット
        /// 1分ごとに呼び出すこと
        /// </summary>
        /// <param name="status">取引所ステータス(CLOSEDなら取得休み)</param>
        public void ResetMinMax(string status)
        {
            // 現在のろうそくを退避（使ってないけど）
            prevCandle = candle;
            // リセット
            candle.ResetMinMax();

            this.status = status;
        }

        #region コンストラクタ・初期化
        /// <summary>
        /// Pubnubでデータ取得
        /// </summary>
        /// <param name="subscribeKey">受信用キー(ex:"sub-c-52a9ab50-291b-11e5-baaa-0619f8945a4f")</param>
        /// <param name="channel">チャンネル(ex:"lightning_ticker_FX_BTC_JPY")</param>
        /// <param name="useId">モード0:Ticker 1:約定</param>
        public PubnubManager(string subscribeKey, string channel, PubNubUse useId)
        {
            this.useId = useId;
            buyVol = new List<RawExecute>();
            sellVol = new List<RawExecute>();

            config = new PNConfiguration
            {
                SubscribeKey = subscribeKey // 受信用キー
            };
            this.channel = channel;
            pubnub = new Pubnub(config);
            //config.PublishKey = "demo";   // 配信用キー

            // ろうそく初期化
            prevCandle = new Candle();
            candle = new Candle();
        }
        /// <summary>
        /// 再接続
        /// </summary>
        private void ReConnect()
        {
            pubnub = new Pubnub(config);
            Start();
        }

        #endregion

        public override string ToString()
        {
            return string.Format("[PubnubManager: channel={0}]", channel);
        }

        /// <summary>
        /// pubnubの受信を開始する
        /// </summary>
        public void Start()
        {
            pubnub.AddListener(new SubscribeCallbackExt(
                (pubnubObj, message) =>
                {
                    // 何かメッセージがあったら？
                    if (message != null)
                    {
                        if (message.Channel != null)
                        {
                            // Message has been received on channel group stored in
                            //Console.WriteLine(message.Channel);
                            //Console.WriteLine(message.Message);
                            string json = message.Message.ToString();
                            if (json != "")
                            {
                                switch (useId)
                                {
                                    case PubNubUse.Ticker:
                                        // RowTickerにする
                                        latestRawTicker = JsonConvert.DeserializeObject<RawTicker>(json);
                                        latestTicker = new Ticker(latestRawTicker);
                                        // ろうそく更新
                                        candle.UpdateByTicker(latestTicker);
                                        break;
                                    case PubNubUse.Execution:
                                        // RawExecuteにする
                                        var tempExecute = JsonConvert.DeserializeObject<List<RawExecute>>(json);
                                        foreach (var item in tempExecute)
                                        {
                                            item.parseDate();
                                            if (item.Side == "BUY")
                                            {
                                                buyVol.Add(item);
                                            }
                                            else if (item.Side == "SELL")
                                            {
                                                sellVol.Add(item);
                                            }
                                        }
                                        break;
                                }
                                // PubNub止まってません
                                restartCount = 1;
                            }
                        }
                        else
                        {
                            // Message has been received on channel stored in
                            Logger.Log(message.Subscription);
                        }
                    }
                },
                (pubnubObj, presence) => { },
                (pubnubObj, status) =>
                {
                    if (status.Category == PNStatusCategory.PNUnexpectedDisconnectCategory)
                    {
                        // This event happens when radio / connectivity is lost
                        Logger.Log("PNUnexpectedDisconnectCategory");
                    }
                    else if (status.Category == PNStatusCategory.PNConnectedCategory)
                    {
                        Logger.Log("PNConnectedCategory");
                        // Connect event. You can do stuff like publish, and know you'll get it.
                        // Or just use the connected event to confirm you are subscribed for
                        // UI / internal notifications, etc
                        #region 発信用(不使用)
                        //pubnub.Publish()
                        //            .Channel("awesomeChannel")
                        //            .Message("hello!!")
                        //            .Async(new PNPublishResultExt((publishResult, publishStatus) =>
                        //            {
                        //                // Check whether request successfully completed or not.
                        //                if (!publishStatus.Error)
                        //                {
                        //                    Console.WriteLine("Message successfully published to specified channel.");
                        //                }
                        //                else
                        //                {
                        //                    Console.WriteLine("Request processing failed.");
                        //                    // Handle message publish error. Check 'Category' property to find out possible issue
                        //                    // because of which request did fail.
                        //                }
                        //            }));
                        #endregion
                    }
                    else if (status.Category == PNStatusCategory.PNReconnectedCategory)
                    {
                        Logger.Log("PNReconnectedCategory");
                        // Happens as part of our regular operation. This event happens when
                        // radio / connectivity is lost, then regained.
                    }
                    else if (status.Category == PNStatusCategory.PNDecryptionErrorCategory)
                    {
                        Logger.Log("PNDecryptionErrorCategory");
                        // Handle messsage decryption error. Probably client configured to
                        // encrypt messages and on live data feed it received plain text.
                    }
                }
            ));
            pubnub.Subscribe<string>().Channels(new string[] { channel }).Execute();

        }
    }
}
