using Penguinium.ApiBridge;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Penguinium.Common;
using Penguinium.ProcessData;
using Microsoft.EntityFrameworkCore;
using Chickenium.Dao;

namespace Penguinium.Client
{
    public enum ProductCode
    {
        BTC_JPY,
        FX_BTC_JPY,
        ETH_BTC
    }
    enum MyOrderState
    {
        ACTIVE,     // オープンな注文の一覧を取得します。
        COMPLETED,  // 全額が取引完了した注文の一覧を取得します。
        CANCELED,   // お客様がキャンセルした注文です。
        EXPIRED,    // 有効期限に到達したため取り消された注文の一覧を取得します。
        REJECTED,   // 失敗した注文です。
    }

    // ProductCodeごとに異なるインスタンスを作成するように設計すること

    /// <summary>
    /// 各取引所のクライアント
    /// </summary>
    public abstract class ExchangeClient
    {
        #region フィールド
        /// <summary>
        ///DB接続
        /// </summary>
        protected DbContextOptions dbContextOptions;

        /// <summary>
        /// APIを送るクラス
        /// </summary>
        protected ApiClient m_apiClient;

        /// <summary>
        /// 通貨ペア
        /// </summary>
        protected ProductCode PRODUCT_CODE;

        /// <summary>
        /// 取引所ID
        /// </summary>
        protected int ExchangeId;

        /// <summary>
        /// 親注文一覧
        /// GetMyActiveParentOrdersで更新
        /// </summary>
        protected List<Order> parentOrders;

        /// <summary>
        /// ここに入れたオーダーは決済せずにキャンセルする
        /// 非同期
        /// </summary>
        protected List<Order> cancelOrderList;

        /// <summary>
        /// BOTの状態
        /// 各アルゴリズム間の連絡に使用する
        /// </summary>
        protected Dictionary<string, string> BotStatus;

        const string StatusIsBreaking = "IsBreaking";
        /// <summary>
        /// ブレイクアウト戦略でレンジブレイクを検出した場合
        /// ブレイク処理中ならばtrue
        /// </summary>
        public bool IsBreaking
        {
            get
            {
                if (BotStatus.ContainsKey(StatusIsBreaking))
                {
                    if (BotStatus[StatusIsBreaking] == "true")
                    {
                        return true;
                    }
                }
                return false;
            }
            set
            {
                if (!BotStatus.ContainsKey(StatusIsBreaking))
                {
                    BotStatus.Add(StatusIsBreaking, "false");
                }
                if (value)
                {
                    BotStatus[StatusIsBreaking] = "true";
                }
                else
                {
                    BotStatus[StatusIsBreaking] = "false";
                }
            }
        }

        /// <summary>
        /// The API key.
        /// </summary>
        protected string apiKey = "";
        protected string apiSelect = "";

        protected string sendchildorder = "";
        protected string sendparentorder = "";
        protected string getMyPositions = "";
        protected string cancelallchildorders = "";
        protected string cancelparentorder = "";
        protected string board = "";
        protected string ticker = "";
        protected string balance = "";
        protected string collateral = "";
        protected string getMyActiveChildOrders = "";
        protected string getMyActiveParentOrders = "";
        protected string getBoardState = "";

        /// <summary>
        /// 取引所が稼働中であることを示すステータス名
        /// </summary>
        protected List<string> BoardEnubleStatusName;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// 取引所APIクライアントの抽象クラス
        /// </summary>
        /// <param name="exchangeId">取引所ID(1〜3、例えばFX_BTC_JPYは2)</param>
        /// <param name="dbContextOptions">DB接続</param>
        /// <param name="timeoutSec">Timeout sec.</param>
        public ExchangeClient(
            int exchangeId,
            DbContextOptions dbContextOptions,
            double timeoutSec = 4 // タイムアウト (デフォルト4秒)
        )
        {
            cancelOrderList = new List<Order>();
            ExchangeId = exchangeId;
            BotStatus = new Dictionary<string, string>();
            parentOrders = new List<Order>();

            // TODO:パラメータ更新
            using (var db = new MApplicationSettingsDbContext(dbContextOptions))
            {
                // データを取得
                var ApplicationSettings = db.MApplicationSettings;

                Console.WriteLine("ApplicationSettingsをセットアップします");
                foreach (var ApplicationSetting in ApplicationSettings)
                {
                    // TODO:FXのパラメータをセット
                    if (ApplicationSetting.ExchangeId == exchangeId)
                    {
                        if (ApplicationSetting.Name == "productCode")
                        {
                            PRODUCT_CODE = (ProductCode)Enum.Parse(typeof(ProductCode), ApplicationSetting.Value, true);
                        }
                        if (ApplicationSetting.Name == "apiKey")
                        {
                            apiKey = ApplicationSetting.Value;
                        }
                        if (ApplicationSetting.Name == "apiSelect")
                        {
                            apiSelect = ApplicationSetting.Value;
                        }
                    }
                }
            }

            // DBから取得したAPIキーをセット
            m_apiClient = new ApiClient(
                apiKey,
                apiSelect,
                timeoutSec,
                "https://api.bitflyer.jp"           // TODO:他の取引所に対応させること
            );
        }
        #endregion

        #region 通常注文
        // 指値で買い注文
        public virtual async Task<OrderResult> Buy(double price, double amount)
        {
            if (price <= 0) throw new Exception("Invalid buy price: " + price);

            // リクエスト構築
            string body = MakeJsonBuySell(OrderSide.BUY, price, amount);
            return await SendApi(body, sendchildorder);
        }

        // 成行で買い注文
        public virtual async Task<OrderResult> Buy(double amount)
        {
            // リクエスト構築
            string body = MakeJsonBuySell(OrderSide.BUY, 0, amount);
            return await SendApi(body, sendchildorder);
        }

        // 指値で売り注文
        public virtual async Task<OrderResult> Sell(double price, double amount)
        {
            if (price <= 0) throw new Exception("Invalid sell price: " + price);

            // リクエスト構築
            string body = MakeJsonBuySell(OrderSide.SELL, price, amount);
            return await SendApi(body, sendchildorder);
        }

        // 成行で売り注文
        public virtual async Task<OrderResult> Sell(double amount)
        {
            // リクエスト構築
            string body = MakeJsonBuySell(OrderSide.SELL, 0, amount);
            return await SendApi(body, sendchildorder);
        }

        // Jsonリクエスト作成
        protected abstract string MakeJsonBuySell(OrderSide side, double price, double amount);
        #endregion


        #region 逆指値注文
        public virtual async Task<OrderResult> StopBuy(double price, double amount)
        {
            string body = MakeJsonBuySellStop(OrderSide.BUY, price, amount);
            return await SendApi(body, sendparentorder);
        }

        public virtual async Task<OrderResult> StopSell(double price, double amount)
        {
            string body = MakeJsonBuySellStop(OrderSide.SELL, price, amount);
            return await SendApi(body, sendparentorder);
        }

        public virtual async Task<OrderResult> StopBuyTrail(double price, double amount, double offset)
        {
            string body = MakeJsonBuySellStopTrail(OrderSide.BUY, price, amount, offset);
            return await SendApi(body, sendparentorder);
        }

        public virtual async Task<OrderResult> StopSellTrail(double price, double amount, double offset)
        {
            string body = MakeJsonBuySellStopTrail(OrderSide.SELL, price, amount, offset);
            return await SendApi(body, sendparentorder);
        }

        // Jsonリクエスト作成
        protected abstract string MakeJsonBuySellStop(OrderSide side, double triggerPrice, double amount);
        protected abstract string MakeJsonBuySellStopTrail(OrderSide side, double triggerPrice, double amount, double offset);
        #endregion

        #region sendApi:APIを送信する
        /// <summary>
        /// Sends the API.
        /// </summary>
        /// <returns>The API.</returns>
        /// <param name="body">json</param>
        /// <param name="post">"/v1/me/????"</param>
        protected virtual async Task<OrderResult> SendApi(string body, string post)
        {

            // リクエスト送信
            string json = await m_apiClient.Post(post, body);

            // 応答パース
            try
            {
                RawOrderResult _result = JsonConvert.DeserializeObject<RawOrderResult>(json);
                if (_result.IsError()) throw new Exception(_result.error_message);
                return new OrderResult(_result);
            }
            catch (Exception ex)
            {
                throw new Exception("OrderError: " + ex.Message);
            }
        }
        #endregion

        #region IfDone注文
        // IfDone注文
        public virtual async Task<OrderResult> IfDone(double beforePrice, double afterPrice, double amount)
        {
            if (beforePrice > afterPrice)
            {
                // リクエスト構築
                string body = MakeJsonIfDone(OrderSide.SELL, OrderSide.BUY, beforePrice, afterPrice, amount);
                return await SendApi(body, sendparentorder);
            }
            else
            {
                string body = MakeJsonIfDone(OrderSide.BUY, OrderSide.SELL, beforePrice, afterPrice, amount);
                return await SendApi(body, sendparentorder);
            }
        }

        // Jsonリクエスト作成
        protected abstract string MakeJsonIfDone(OrderSide beforeSide, OrderSide afterSide, double beforePrice, double afterPrice, double amount);
        #endregion

        #region IfDoneOco注文
        // IfDoneOco注文
        public virtual async Task<OrderResult> IfDoneOco(double beforePrice, double afterPrice, double losePrice, double amount)
        {
            if (beforePrice > afterPrice)
            {
                string body = MakeJsonIfDoneOco(OrderSide.SELL, OrderSide.BUY, beforePrice, afterPrice, losePrice, amount);
                return await SendApi(body, sendparentorder);
            }
            else
            {
                string body = MakeJsonIfDoneOco(OrderSide.BUY, OrderSide.SELL, beforePrice, afterPrice, losePrice, amount);
                return await SendApi(body, sendparentorder);
            }
        }

        // Jsonリクエスト作成
        protected abstract string MakeJsonIfDoneOco(OrderSide beforeSide, OrderSide afterSide, double beforePrice, double afterPrice, double losePrice, double amount);

        #endregion

        #region Trail注文
        public virtual async Task<OrderResult> Trailing(OrderSide side, double offset, double amount)
        {
            string body = MakeJsonTrailing(side, offset, amount);
            return await SendApi(body, sendparentorder);
        }

        // Jsonリクエスト作成
        protected abstract string MakeJsonTrailing(OrderSide side, double offset, double amount);
        #endregion

        #region GetMyPositions:建玉情報の取得
        public virtual async Task<List<Position>> GetMyPositions()
        {
            // リクエスト送信
            string json = await m_apiClient.Get(getMyPositions);

            // 応答パース
            try
            {
                List<RawPosition> _positions = JsonConvert.DeserializeObject<List<RawPosition>>(json);
                return _positions.Select(p => new Position(p)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("GetBoardError: " + ex.Message);
            }
        }
        #endregion

        #region CancelAllOrders,CancelOrder:注文取り消し
        // Jsonリクエスト作成
        protected abstract string MakeCancelAllOrders();
        // Jsonリクエスト作成
        protected abstract string MakeJsonCancelOrder(Order order);

        /// <summary>
        /// 全キャンセル
        /// </summary>
        /// <returns></returns>
        public virtual async Task CancelAllOrders()
        {
            string body = MakeCancelAllOrders();
            await m_apiClient.Post(cancelallchildorders, body);
        }

        /// <summary>
        /// 個別キャンセル
        /// </summary>
        /// <param name="order">対象のオーダー情報</param>
        /// <returns></returns>
        public virtual async Task CancelOrder(Order order)
        {
            string body = MakeJsonCancelOrder(order);
            await m_apiClient.Post(cancelparentorder, body);
        }
        #endregion

        #region GetBoard:板情報を取得
        public virtual async Task<Board> GetBoard()
        {
            // リクエスト送信
            string json = await m_apiClient.Get(board);

            // 応答パース
            try
            {
                RawBoard _board = JsonConvert.DeserializeObject<RawBoard>(json);
                return new Board(_board);
            }
            catch (Exception ex)
            {
                throw new Exception("GetBoardError: " + ex.Message);
            }
        }
        #endregion

        #region GetTicker:最新の価格情報を取得
        public virtual async Task<Ticker> GetTicker()
        {
            // リクエスト送信
            string json = await m_apiClient.Get(ticker);

            // 応答パース
            try
            {
                RawTicker _ticker = JsonConvert.DeserializeObject<RawTicker>(json);
                return new Ticker(_ticker);
            }
            catch (Exception ex)
            {
                throw new Exception("GetTickerError: " + ex.Message);
            }
        }
        #endregion

        #region GetMyAssetList:資産情報を取得
        /// <summary>
        /// 資産情報を取得
        /// </summary>
        /// <returns>各通貨の資産情報</returns>
        public virtual async Task<AssetList> GetMyAssetList()
        {
            // リクエスト送信
            string json = await m_apiClient.Get(balance);

            // 応答パース
            try
            {
                List<RawAsset> _assets = JsonConvert.DeserializeObject<List<RawAsset>>(json);
                List<Asset> assets = _assets.Select(a => new Asset(a)).ToList();
                return new AssetList(assets);
            }
            catch (Exception ex)
            {
                throw new Exception("GetMyAssetsError: " + ex.Message);
            }
        }
        #endregion

        #region GetMyCollateral:FX証拠金の状態を取得
        public virtual async Task<Collateral> GetMyCollateral()
        {
            // リクエスト送信
            string json = await m_apiClient.Get(collateral);

            // 応答パース
            try
            {
                RawCollateral _collateral = JsonConvert.DeserializeObject<RawCollateral>(json);
                return new Collateral(_collateral);
            }
            catch (Exception ex)
            {
                throw new Exception("GetMyAssetsError: " + ex.Message);
            }
        }
        #endregion

        #region GetMyActiveOrders:自分の注文一覧を取得(※最大100件まで)
        public async Task<List<Order>> GetMyActiveChildOrders()
        {
            return await _GetMyChildOrders();
        }
        protected virtual async Task<List<Order>> _GetMyChildOrders()
        {
            // リクエスト送信
            string json = await m_apiClient.Get(getMyActiveChildOrders);

            // 応答パース
            try
            {
                // Debug.WriteLine(json);
                List<RawChildOrder> rawOrders = JsonConvert.DeserializeObject<List<RawChildOrder>>(json);
                List<Order> orders = rawOrders.Select(o => new Order(o)).ToList();
                return orders;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("JsonError: " + ex.Message + "\nJSON=" + json);
                return null;
            }
        }
        public virtual async Task<List<Order>> GetMyActiveParentOrders()
        {
            return await _GetMyParentOrders();
        }
        protected virtual async Task<List<Order>> _GetMyParentOrders()
        {
            // リクエスト送信
            string json = await m_apiClient.Get(getMyActiveParentOrders);

            // 応答パース
            try
            {
                // Debug.WriteLine(json);
                List<RawParentOrder> rawOrders = JsonConvert.DeserializeObject<List<RawParentOrder>>(json);
                parentOrders = rawOrders.Select(o => new Order(o)).ToList();
                return parentOrders;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("JsonError: " + ex.Message + "\nJSON=" + json);
                return null;
            }
        }
        #endregion
        #region GetBoardState:サーバの状態
        public virtual async Task<RawBoardState> GetBoardState()
        {
            // リクエスト構築
            string json = await m_apiClient.Get(getBoardState);
            try
            {
                RawBoardState _board = JsonConvert.DeserializeObject<RawBoardState>(json);
                return _board;
            }
            catch (Exception ex)
            {
                throw new Exception("GetBoardStateError: " + ex.Message);
            }
        }
        #endregion

        #region CheckServerStatus:取引所が動いていたらtrue
        /// <summary>
        /// サーバ稼働中ならtrue
        /// サーバステータス名未定義の場合、とりあえず動かすためtrue
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> CheckServerStatus()
        {
            if (BoardEnubleStatusName == null)
            {
                Console.WriteLine("この取引所のサーバステータス名が未定義です:" + ExchangeId);
                return true;
            }
            var state = await GetBoardState();
            return BoardEnubleStatusName.Contains(state.State);
        }
        #endregion

        public virtual void Update()
        {
            // 特になし
        }

        /// <summary>
        /// ここに追加した親注文は非同期で削除する
        /// 削除間隔は基本5分とする
        /// 動作には各クライアントでUpdateを継承する必要あり
        /// </summary>
        /// <param name="item"></param>
        public void AddCancelParentOrderList(Order item)
        {
            cancelOrderList.Add(item);
        }
    }
}
