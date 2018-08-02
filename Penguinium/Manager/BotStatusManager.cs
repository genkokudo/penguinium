using Chickenium;
using Chickenium.Dao;
using Microsoft.EntityFrameworkCore;
using Penguinium.ApiBridge;
using Penguinium.Client;
using Penguinium.Common;
using Penguinium.ProcessData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Penguinium.Manager
{
    public enum WebSocketCategory
    {
        PubNub = 11
    }
    public enum PubNubUse
    {
        Key = 1,
        Ticker = 2,
        Execution = 3,
        BoardSnapshot = 4,
        Board = 5
    }

    /// <summary>
    /// BOTを起動してからの各状態を管理
    /// あくまでデータを持つところで、操作は行わない方針？
    /// 初期化・接続などは行う
    /// 全取引所の情報を持つ
    /// </summary>
    public class BotStatusManager
    {
        const int Enabled = 1;

        #region フィールド
        /// <summary>
        /// アプリケーション設定の参照
        /// 1:取引所ID
        /// 2:項目名
        /// </summary>
        public Dictionary<int, Dictionary<string, string>> ApplicationSettings { get; private set; }

        /// <summary>
        /// 資産情報
        /// 1:取引所ID
        /// </summary>
        public Dictionary<int, Collateral> CollateralList { get; private set; }

        /// <summary>
        /// 注文情報（BFの特殊注文はここ！）
        /// 1:取引所ID
        /// </summary>
        public Dictionary<int, List<Order>> ParentOrderList { get; private set; }

        /// <summary>
        /// 注文情報（BFの通常注文はここ！）
        /// 1:取引所ID
        /// </summary>
        public Dictionary<int, List<Order>> ChildOrderList { get; private set; }

        /// <summary>
        /// 建玉情報
        /// 1:取引所ID
        /// </summary>
        public Dictionary<int, List<Position>> PositionList { get; private set; }

        /// <summary>
        /// 取引所の状態情報
        /// 1:取引所ID
        /// </summary>
        public Dictionary<int, RawBoardState> StateList { get; private set; }

        /// <summary>
        /// 平均線・トレンドの計算
        /// 1:取引所ID
        /// </summary>
        public Dictionary<int, TechnicalManager> TechnicalList { get; private set; }

        /// <summary>
        /// クライアント
        /// 1:取引所ID
        /// </summary>
        public Dictionary<int, ExchangeClient> Client { get; private set; }

        /// <summary>
        /// 定義されている時間足
        /// </summary>
        public Dictionary<int, TimeScale> TimeScaleList { get; private set; }

        // DB接続
        private DbContextOptions DbContextOptions { get; set; }

        /// <summary>
        /// PubNubのリスト
        /// 1:取引所ID
        /// 2:用途ID
        /// </summary>
        public Dictionary<int, Dictionary<int, PubnubManager>> PubnubList { get; private set; }
        #endregion

        #region 初期化
        public BotStatusManager(DbContextOptions dbContextOptions)
        {
            DbContextOptions = dbContextOptions;
            Initialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            // アプリ設定取得
            ReloadApplicationSettings();

            // 時間足リスト読み込み
            InitializeTimeScale();

            // PubNubを開始する
            InitializeWebSocket();

            // 各種テクニカル・クライアント初期化
            InitializeExchange();
        }
        #endregion

        #region DBからの読み込み
        /// <summary>
        /// DBからデータを取得して
        /// アプリケーション設定を最新化する
        /// </summary>
        public void ReloadApplicationSettings()
        {
            // 初期化
            if(ApplicationSettings == null)
            {
                ApplicationSettings = new Dictionary<int, Dictionary<string, string>>();
            }
            ApplicationSettings.Clear();

            // パラメータ読み込み
            using (var db = new MApplicationSettingsDbContext(DbContextOptions))
            {
                // データを取得
                var applicationSettings = db.MApplicationSettings;
                foreach (var applicationSetting in applicationSettings)
                {
                    int recordId = applicationSetting.ExchangeId;
                    if (!ApplicationSettings.ContainsKey(recordId))
                    {
                        ApplicationSettings.Add(recordId, new Dictionary<string, string>());
                    }
                    ApplicationSettings[recordId].Add(applicationSetting.Name, applicationSetting.Value);
                }
            }
            // TODO:取引所ごとに項目が異なるので、アクセサを作成すればよい
        }

        private Dictionary<T1, T2> InitializeDictionary<T1, T2>(Dictionary<T1, T2> dictionary)
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<T1, T2>();
            }
            else
            {
                dictionary.Clear();
            }
            return dictionary;
        }

        /// <summary>
        /// MTimeScaleテーブルの設定内容で初期化する
        /// </summary>
        public void InitializeTimeScale()
        {
            // 初期化
            TimeScaleList = InitializeDictionary(TimeScaleList);

            // パラメータ読み込み
            using (var db = new MTimeScaleDbContext(DbContextOptions))
            {
                // データを取得
                var mTimeScale = db.MTimeScale;

                foreach (var item in mTimeScale)
                {
                    if (item.Enabled == Enabled)
                    {
                        TimeScaleList.Add(item.Id, new TimeScale(item.Id, item.Name, item.DisplayName, item.SecondsValue));
                    }
                }
            }
        }

        /// <summary>
        /// MExchangeテーブルの設定内容で初期化する
        /// </summary>
        public void InitializeExchange()
        {
            // 初期化
            // 資産情報
            CollateralList = InitializeDictionary(CollateralList);

            // 注文情報
            ParentOrderList = InitializeDictionary(ParentOrderList);
            ChildOrderList = InitializeDictionary(ChildOrderList);

            // 建玉情報
            PositionList = InitializeDictionary(PositionList);

            // 取引所状態情報
            StateList = InitializeDictionary(StateList);

            // テクニカル管理
            TechnicalList = InitializeDictionary(TechnicalList);

            // クライアント
            Client = InitializeDictionary(Client);

            // パラメータ読み込み
            using (var db = new MExchangeDbContext(DbContextOptions))
            {
                // データを取得
                var mExchange = db.MExchange;

                foreach (var item in mExchange)
                {
                    if (item.Enabled == Enabled)
                    {
                        // 資産情報
                        CollateralList.Add(item.Id, new Collateral(new RawCollateral()));

                        // 注文情報
                        ParentOrderList.Add(item.Id, new List<Order>());
                        ChildOrderList.Add(item.Id, new List<Order>());

                        // 建玉情報
                        PositionList.Add(item.Id, new List<Position>());

                        // 取引所状態情報
                        StateList.Add(item.Id, new RawBoardState());

                        // テクニカル管理生成
                        Dictionary<int, PubnubManager> pubnub = null;
                        if (PubnubList.ContainsKey(item.Id))
                        {
                            pubnub = PubnubList[item.Id];
                        }
                        TechnicalList.Add(item.Id, new TechnicalManager(item.Id, pubnub, TimeScaleList, DbContextOptions, TechnicalList));

                        // 通信用クライアント生成
                        Client.Add(item.Id, new BitflyerClient(item.Id, DbContextOptions));
                    }
                }
            }
        }

        /// <summary>
        /// MWebSocketテーブルの設定内容で初期化する
        /// PubNubなどに接続する
        /// </summary>
        public void InitializeWebSocket()
        {
            // 初期化
            PubnubList = InitializeDictionary(PubnubList);

            // パラメータ読み込み
            using (var db = new MWebSocketDbContext(DbContextOptions))
            {
                // データを取得
                var mWebSocket = db.MWebSocket;

                // PubNubのキーリスト作成
                var PubNubKeys = new Dictionary<int, string>();
                foreach (var item in mWebSocket)
                {
                    if (item.Enabled == Enabled)
                    {
                        if (item.CategoryId == (int)WebSocketCategory.PubNub)
                        {
                            if(item.UseId == (int)PubNubUse.Key)
                            {
                                PubNubKeys.Add(item.ExchangeId, item.Value);
                            }
                        }
                    }
                }

                // キーが見つかった取引所のPubNubを作成
                foreach (var item in mWebSocket)
                {
                    if (item.Enabled == Enabled)
                    {
                        if (item.CategoryId == (int)WebSocketCategory.PubNub)
                        {
                            // PubNubの場合、PubNubのリストに追加
                            // 取引所ID
                            int RecordId = item.ExchangeId;
                            if (PubNubKeys.ContainsKey(RecordId))
                            {
                                // 用途ID
                                PubNubUse useId = (PubNubUse)item.UseId;
                                if(useId != PubNubUse.Key)
                                {
                                    if (!PubnubList.ContainsKey(item.ExchangeId))
                                    {
                                        PubnubList.Add(item.ExchangeId, new Dictionary<int, PubnubManager>());
                                    }
                                    PubnubList[item.ExchangeId].Add(item.UseId, new PubnubManager(PubNubKeys[item.ExchangeId], item.Value, useId));
                                }
                            }
                        }
                    }
                }
            }


            foreach (var pubnub in PubnubList.Values)
            {
                foreach (var item in pubnub.Values)
                {
                    Logger.Log("PubNubを開始します:" + item.ToString());
                    item.Start();
                }
            }
        }
        #endregion

        // 現在は1分に1回更新なので注意
        #region Update
        /// <summary>
        /// 毎秒呼び出す
        /// メンテ中は呼ばない
        /// 定義しているもののみ更新する
        /// </summary>
        /// <param name="key">メンテ中は呼ばないようにしているのでキー指定</param>
        /// <param name="ticker"></param>
        public void Update(int key, Ticker ticker)
        {
            // 各テクニカル更新
            if (TechnicalList.ContainsKey(key))
            {
                TechnicalList[key].Update(ticker);
            }
            // 各クライアント更新
            if (Client.ContainsKey(key))
            {
                Client[key].Update();
            }
        }
        #endregion
    }
}
