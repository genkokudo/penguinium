using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Penguinium.ApiBridge;
using Penguinium.Common;

namespace Penguinium.ProcessData
{
    /// <summary>
    /// 板内の1つの注文情報
    /// </summary>
    public class BoardOrder
    {
        #region プロパティ
        // 売りまたは買い
        [JsonConverter(typeof(StringEnumConverter))]
        public BoardOrderSide Side { get; set; }

        // 価格 (BTCJPY等では常に整数だがETHJPY等では小数もあり得ることに注意)
        public double Price { get; set; }

        // 注文量
        public double Size { get; set; }
        #endregion

        public override string ToString()
        {
            return "BoardOrder" + JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// 板情報
    /// 現在の買い売り注文リスト
    /// 中間価格（？）
    /// </summary>
    public class Board
    {
        public Board(RawBoard _board)
        {
            MiddlePrice = _board.MiddlePrice;
            Asks = _board.Asks.Select(e => new BoardOrder { Side = BoardOrderSide.ASK, Price = e.Price, Size = e.Size }).ToList();
            Bids = _board.Bids.Select(e => new BoardOrder { Side = BoardOrderSide.BID, Price = e.Price, Size = e.Size }).ToList();
        }

        #region プロパティ
        // 中間価格
        public double MiddlePrice { get; set; }

        // 売り板
        public List<BoardOrder> Asks { get; set; }

        // 買い板
        public List<BoardOrder> Bids { get; set; }
        #endregion
    }

    /// <summary>
    /// Ticker
    /// </summary>
    public class Ticker
    {
        // 今は1取引所なので全部取る。
        public Ticker(RawTicker _ticker)
        {
            ProductCode = _ticker.ProductCode;
            TimeStamp = _ticker.TimeStamp;
            TickId = _ticker.TickId;
            BestBid = _ticker.BestBid;
            BestBidSize = _ticker.BestBidSize;
            BestAsk = _ticker.BestAsk;
            BestAskSize = _ticker.BestAskSize;
            TotalBidDepth = _ticker.TotalBidDepth;
            TotalAskDepth = _ticker.TotalAskDepth;
            Itp = _ticker.Itp;
            Volume = _ticker.Volume;
            VolumeByProduct = _ticker.VolumeByProduct;
        }

        #region プロパティ
        // 種類
        public string ProductCode { get; set; }

        // 時刻
        public string TimeStamp { get; set; }

        // TickID
        public string TickId { get; set; }

        // 買い値の最高値
        public double BestBid { get; set; }

        // 買い値最高値の取引量
        public double BestBidSize { get; set; }

        // 売り値の最低値
        public double BestAsk { get; set; }

        // 売り値最低値の取引量
        public double BestAskSize { get; set; }

        // 買いの総注文量
        public double TotalBidDepth { get; set; }

        // 売りの総注文量
        public double TotalAskDepth { get; set; }

        // 最終取引価格
        public double Itp { get; set; }

        // 現物、FX、先物の合計の取引実績量
        public double Volume { get; set; }

        // 24時間の取引量
        public double VolumeByProduct { get; set; }
        #endregion
    }
}
