using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Penguinium.ProcessData;

namespace Penguinium.ApiBridge
{
    // 生データ格納用クラス
    // 現在はBF用なので、異なる取引所を実装するときは名前を修正して、コピーしてクラス作成すること

    /// <summary>
    /// 資産情報取得
    /// </summary>
    public class RawAsset
    {
        public string currency_code { get; set; } // JPY, BTC, ETH
        public double amount { get; set; }
        public double available { get; set; }
    }

    /// <summary>
    /// 証拠金情報取得
    /// </summary>
    public class RawCollateral
    {
        public double collateral { get; set; } // 預け入れた日本円証拠金の額
        public double open_position_pnl { get; set; } // 建玉の評価損益
        public double require_collateral { get; set; } // 現在の必要証拠金
        public double keep_rate { get; set; } // 現在の証拠金維持率
    }

    /// <summary>
    /// 子注文取得
    /// </summary>
    public class RawChildOrder
    {
        public int id { get; set; }
        public string child_order_id { get; set; }
        public string product_code { get; set; }
        public string side { get; set; }
        public string child_order_type { get; set; }
        public double price { get; set; }
        public double average_price { get; set; }
        public double size { get; set; }
        public string child_order_state { get; set; }
        public string expire_date { get; set; }
        public string child_order_date { get; set; }
        public string child_order_acceptance_id { get; set; }
        public double outstanding_size { get; set; }
        public double cancel_size { get; set; }
        public double executed_size { get; set; }
        public double total_commission { get; set; }
    }
    /// <summary>
    /// 親注文取得
    /// </summary>
    public class RawParentOrder
    {
        public int id { get; set; }
        public string parent_order_id { get; set; }
        public string product_code { get; set; }
        public string side { get; set; }
        public string parent_order_type { get; set; }
        public double price { get; set; }
        public double average_price { get; set; }
        public double size { get; set; }
        public string parent_order_state { get; set; }
        public string expire_date { get; set; }
        public string parent_order_date { get; set; }
        public string parent_order_acceptance_id { get; set; }
        public double outstanding_size { get; set; }
        public double cancel_size { get; set; }
        public double executed_size { get; set; }
        public double total_commission { get; set; }
    }

    /// <summary>
    /// 板の注文情報取得
    /// </summary>
    public class RawBoardOrder
    {
        // 価格 (BTCJPY等では常に整数だがETHJPY等では小数もあり得ることに注意)
        [JsonProperty(PropertyName = "price")]
        public double Price { get; set; }

        // 注文量
        [JsonProperty(PropertyName = "size")]
        public double Size { get; set; }
    }
    /// <summary>
    /// 板情報取得
    /// </summary>
    public class RawBoard
    {
        // 中間価格
        [JsonProperty(PropertyName = "mid_price")]
        public double MiddlePrice { get; set; }

        // 売り板
        [JsonProperty(PropertyName = "asks")]
        public List<BoardOrder> Asks { get; set; }

        // 買い板
        [JsonProperty(PropertyName = "bids")]
        public List<BoardOrder> Bids { get; set; }
    }

    /// <summary>
    /// ローソク足情報取得
    /// cryptowatch用
    /// </summary>
    public class RawCandleResultHead
    {
        [JsonProperty(PropertyName = "result")]
        public RawCandleResult Result { get; set; }
    }
    /// <summary>
    /// ローソク足情報取得
    /// cryptowatch用
    /// </summary>
    public class RawCandleResult
    {
        // 1分足
        [JsonProperty(PropertyName = "60")]
        public List<List<double>> Result60 { get; set; }
        // 3分足
        [JsonProperty(PropertyName = "180")]
        public List<List<double>> Result180 { get; set; }
        // 5分足
        [JsonProperty(PropertyName = "300")]
        public List<List<double>> Result300 { get; set; }
        // 15分足
        [JsonProperty(PropertyName = "900")]
        public List<List<double>> Result900 { get; set; }
        // 30分足
        [JsonProperty(PropertyName = "1800")]
        public List<List<double>> Result1800 { get; set; }
        // 1時間足
        [JsonProperty(PropertyName = "3600")]
        public List<List<double>> Result3600 { get; set; }
    }

    //volume_by_productはtickerで呼び出している銘柄（この場合BTCJPYの現物）
    //volumeは現物、FX、先物の合計のようです。さすがにETH/BTCなどは含まない。
    /// <summary>
    /// Ticker取得
    /// </summary>
    public class RawTicker
    {
        // 種類
        [JsonProperty(PropertyName = "product_code")]
        public string ProductCode { get; set; }

        // 時刻"2017-02-11T06:01:53.66"
        [JsonProperty(PropertyName = "timestamp")]
        public string TimeStamp { get; set; }

        // TickID
        [JsonProperty(PropertyName = "tick_id")]
        public string TickId { get; set; }

        // 買い値の最高値
        [JsonProperty(PropertyName = "best_bid")]
        public double BestBid { get; set; }

        // 買い値最高値の取引量
        [JsonProperty(PropertyName = "best_bid_size")]
        public double BestBidSize { get; set; }

        // 売り値の最低値
        [JsonProperty(PropertyName = "best_ask")]
        public double BestAsk { get; set; }

        // 売り値の最低値の取引量
        [JsonProperty(PropertyName = "best_ask_size")]
        public double BestAskSize { get; set; }

        // 買いの総注文量
        [JsonProperty(PropertyName = "total_bid_depth")]
        public double TotalBidDepth { get; set; }

        // 売りの総注文量
        [JsonProperty(PropertyName = "total_ask_depth")]
        public double TotalAskDepth { get; set; }

        // 最終取引価格
        [JsonProperty(PropertyName = "ltp")]
        public double Itp { get; set; }

        // 現物、FX、先物の合計の取引実績量
        [JsonProperty(PropertyName = "volume")]
        public double Volume { get; set; }

        // 24 時間の取引実績量
        [JsonProperty(PropertyName = "volume_by_product")]
        public double VolumeByProduct { get; set; }
    }

    // 例: {"child_order_acceptance_id":"JRF20161212-104117-716911"}
    // 例: {"status":-110,"error_message":"The minimum order size is 0.001 BTC.","data":null}
    /// <summary>
    /// 注文結果
    /// </summary>
    public class RawOrderResult
    {
        public string child_order_acceptance_id { get; set; }
        public string parent_order_acceptance_id { get; set; }
        public string error_message { get; set; }

        public bool IsError()
        {
            return !string.IsNullOrEmpty(error_message);
        }
    }
    
    /// <summary>
    /// 約定結果
    /// </summary>
    public class RawExecute
    {
        // TickID
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        // Side
        [JsonProperty(PropertyName = "side")]
        public string Side { get; set; }

        // 価格
        [JsonProperty(PropertyName = "price")]
        public double Price { get; set; }

        // 注文量
        [JsonProperty(PropertyName = "size")]
        public double Size { get; set; }

        // 時刻"2017-02-11T06:01:53.66"
        [JsonProperty(PropertyName = "exec_date")]
        public string ExecDate { get; set; }

        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "buy_child_order_acceptance_id")]
        public string BuyChildOrderAcceptanceId { get; set; }
        [JsonProperty(PropertyName = "sell_child_order_acceptance_id")]
        public string SellChildOrderAcceptanceId { get; set; }

        public void parseDate()
        {
            Date = DateTime.Parse(ExecDate.Substring(0, 23) + "+00:00"); // 注文日時
        }
    }

    /// <summary>
    /// 板情報取得
    /// </summary>
    public class RawBoardState
    {
        // 取引所の稼動状態
        [JsonProperty(PropertyName = "health")]
        public string Health { get; set; }

        // 板の状態
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }
    }
}
