using Penguinium.ApiBridge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penguinium.ProcessData
{
    #region Candle:ローソクデータ
    /// <summary>
    /// ローソクデータ
    /// </summary>
    public class Candle
    {
        public const int UNKNOWN = -1;
        /// <summary>
        /// 時刻
        /// </summary>
        public string TimeStamp { get; set; }

        /// <summary>
        /// 最小値
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// 最大値
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// 始値
        /// </summary>
        public double Begin { get; set; }

        /// <summary>
        /// 終値
        /// </summary>
        public double End { get; set; }

        /// <summary>
        /// ボリューム
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// ローソク足データ
        /// </summary>
        public Candle(List<double> source)
        {
            TimeStamp = source[0].ToString();
            Begin = source[1];
            Max = source[2];
            Min = source[3];
            End = source[4];
            Volume = source[5];
        }

        /// <summary>
        /// ローソク足データ
        /// </summary>
        public Candle()
        {
            TimeStamp = "";
            Min = UNKNOWN;
            Max = UNKNOWN;
            Begin = UNKNOWN;
            End = UNKNOWN;
            Volume = UNKNOWN;
        }

        /// <summary>
        /// 1分足の最大と最小を
        /// リセットする
        /// 各値を現在の終値でセット
        /// </summary>
        public void ResetMinMax()
        {
            Begin = End;
            Min = End;
            Max = End;
        }

        /// <summary>
        /// Tickerによってろうそくを更新する
        /// </summary>
        /// <param name="ticker">Ticker</param>
        public void UpdateByTicker(Ticker ticker)
        {
            // 終値
            End = ticker.Itp;
            // 始値（初回のみ）
            if (Begin < 0)
            {
                Begin = ticker.Itp;
            }
            if (Min < 0)
            {
                Min = ticker.Itp;
            }
            else
            {
                Min = Math.Min(Min, ticker.Itp);
            }
            Max = Math.Max(Max, ticker.Itp);
        }
    }
    #endregion
}
