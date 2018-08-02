using System.Collections.Generic;
using Chickenium.Dao;
using Penguinium.Common;
using Penguinium.ProcessData;
using Chickenium;

namespace Penguinium.Technical
{
    // ボリンジャーバンド

    /// <summary>
    /// ボリンジャーバンドの計算を行う
    /// パラメータの内容
    /// 0:期間(def:20)
    /// 1:シグマ倍率(def:1)
    /// Listの内容
    /// 0:移動平均
    /// 1:1シグマ上
    /// 2:1シグマ下
    /// 3:2シグマ上
    /// 4:2シグマ下
    /// 5:標準偏差
    /// </summary>
    public class BollingerBand : HistoryProperty
    {
        #region フィールド

        /// <summary>
        /// パラメータ0:期間(def:20)
        /// </summary>
        /// <value>期間(def:20)</value>
        private int Length
        {
            get { return (int)this.Parameter[0]; }
        }

        /// <summary>
        /// パラメータ1:シグマ倍率(def:1)
        /// </summary>
        /// <value>シグマ倍率(def:1)</value>
        private int Magnification
        {
            get { return (int)this.Parameter[1]; }
        }

        /// <summary>
        /// 標準偏差
        /// </summary>
        /// <value>The sigma.</value>
        public double Sigma
        {
            get; set;
        }

        /// <summary>
        /// データの取得が十分に行われ、アルゴリズムに使えるなら0以下
        /// </summary>
        /// <value>0以下ならば準備できている</value>
        public override int ReadyCount
        {
            get { return Length - List[0].Count; }
        }
        #endregion

        #region 初期化
        /// <summary>
        /// ボリンジャーバンドの計算を行う
        /// </summary>
        /// <param name="timeScale">分足コード</param>
        /// <param name="parameter">0:期間、1:シグマ倍率</param>
        public BollingerBand(TimeScale timeScale, List<double> parameter) : base(HistoryKind.BollingerBand, timeScale, parameter)
        {
            // リストを作成
            // 0:移動平均
            // 1:1シグマ上
            // 2:1シグマ下
            // 3:2シグマ上
            // 4:2シグマ下
            // 5:標準偏差
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());

            if (parameter.Count < 2)
            {
                Logger.Log("パラメータが足りません:BollingerBand");
                parameter = new List<double>
                {
                    20,
                    1
                };
                Parameter = parameter;
            }
        }
        #endregion

        #region Calculate:計算
        /// <summary>
        /// ボリンジャーバンドを計算する
        /// 定められたSecondsValue周期で呼び出される
        /// </summary>
        public override void Calculate()
        {
            // 移動平均線
            double average = NonApiUtility.CalcSma(baseList, Length);
            if (average <= 0)
            {
                average = CurrentPrice;
            }
            List[0].Add(average);

            // シグマ計算
            Sigma = NonApiUtility.CalcSigma(Length, baseList, average) * Magnification;

            // 1シグマ上
            List[1].Add(average + Sigma);

            // 1シグマ下
            List[2].Add(average - Sigma);

            // 2シグマ上
            List[3].Add(average + Sigma * 2);

            // 2シグマ下
            List[4].Add(average - Sigma * 2);

            // 標準偏差
            List[5].Add(Sigma);
        }
        #endregion
    }
}
