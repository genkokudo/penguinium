using System.Collections.Generic;
using Chickenium.Dao;
using Penguinium.Common;
using Penguinium.ProcessData;
using Chickenium;
using System.Linq;
using System;

namespace Penguinium.Technical
{
    /// <summary>
    /// チャネルを管理する
    /// 一定期間の最小値と最大値
    /// </summary>
    public class Channel : HistoryProperty
    {
        #region フィールド
        /// <summary>
        /// パラメータ0:長さ、何日分の最大最小を取るか
        /// </summary>
        /// <value>The length.</value>
        private int Length
        {
            get { return (int)this.Parameter[0]; }
        }

        /// <summary>
        /// データの取得が十分に行われ、アルゴリズムに使えるなら0以下
        /// </summary>
        /// <value>0以下ならば準備できている</value>
        public override int ReadyCount
        {
            get { return Length - List[0].Count; }
        }

        /// <summary>
        /// 一時的な最大値
        /// </summary>
        private double tempMax = -1;
        /// <summary>
        /// 一時的な最小値
        /// </summary>
        private double tempMin = -1;

        /// <summary>
        /// 最大値履歴
        /// </summary>
        public List<double> MaxList { get { return List[0]; } }

        /// <summary>
        /// 最小値履歴
        /// </summary>
        public List<double> MinList { get { return List[1]; } }

        /// <summary>
        /// レンジを割っていたらtrue
        /// </summary>
        public bool IsBreaking { get; private set; }

        /// <summary>
        /// 現在の最大値
        /// </summary>
        public double CurrentMax
        {
            get
            {
                if (baseList.Count <= 0)
                {
                    return 0;
                }
                return MaxList.Max();
            }
        }

        /// <summary>
        /// 現在の最小値
        /// </summary>
        public double CurrentMin
        {
            get
            {
                if (baseList.Count <= 0)
                {
                    return 0;
                }
                return MinList.Min();
            }
        }

        /// <summary>
        /// 現在の最大値と最小値の差
        /// </summary>
        public double CurrentRange
        {
            get
            {
                return CurrentMax - CurrentMin;
            }
        }
        #endregion

        #region 初期化
        /// <summary>
        /// チャネルの更新を行う
        /// </summary>
        /// <param name="timeScale">分足コード</param>
        /// <param name="parameter">0:長さ</param>
        public Channel(TimeScale timeScale, List<double> parameter) : base(HistoryKind.CHANNEL, timeScale, parameter)
        {
            // リストを作成
            // 0:最大値履歴
            // 1:最小値履歴
            List.Add(new List<double>());
            List.Add(new List<double>());

            if (parameter.Count < 1)
            {
                Logger.Log("パラメータが足りません:Channel");
                parameter = new List<double> { 5 };
                Parameter = parameter;
            }
        }
        #endregion

        #region Update:更新
        /// <summary>
        /// 毎秒呼ばれる
        /// 最大値・最小値更新
        /// </summary>
        /// <param name="ticker"></param>
        public override void Update(Ticker ticker)
        {
            base.Update(ticker);
            if(tempMax < 0)
            {
                tempMax = ticker.Itp;
                tempMin = ticker.Itp;
            }
            tempMax = Math.Max(tempMax, ticker.Itp);
            tempMin = Math.Min(tempMin, ticker.Itp);

            IsBreaking = tempMax > CurrentMax || tempMin < CurrentMin;
        }
        #endregion

        #region Calculate:計算
        /// <summary>
        /// チャネルを更新する
        /// 定められたSecondsValue周期で呼び出される
        /// </summary>
        public override void Calculate()
        {
            // 最大値・最小値の更新
            if(tempMax > 0)
            {
                MaxList.Add(tempMax);
                MinList.Add(tempMin);
                tempMax = -1;
                tempMin = -1;
            }
            else
            {
                MaxList.Add(NonApiUtility.Last(baseList));
                MinList.Add(NonApiUtility.Last(baseList));
            }

            // 要素数を超えたらカットする
            if (baseList.Count > Length)
            {
                baseList.RemoveAt(0);
            }
            if (MaxList.Count > Length)
            {
                MaxList.RemoveAt(0);
            }
            if (MinList.Count > Length)
            {
                MinList.RemoveAt(0);
            }
        }
        #endregion
    }
}
