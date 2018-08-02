using System.Collections.Generic;
using Chickenium.Dao;
using Penguinium.Common;
using Penguinium.ProcessData;
using Chickenium;

namespace Penguinium.Technical
{
    /// <summary>
    /// 単純移動平均(SMA)の計算を行う
    /// 0:単純移動平均
    /// </summary>
    public class Sma : HistoryProperty
    {
        #region フィールド
        /// <summary>
        /// パラメータ0:平均の長さ、幾つのデータの平均か
        /// </summary>
        /// <value>The length.</value>
        private int Length
        {
            get { return (int)this.Parameter[0]; }
        }

        /// <summary>
        /// データの取得が十分に行われ、アルゴリズムに使えるなら0以下
        /// </summary>
        /// <value>十分にデータ取得が行われていたら0以下</value>
        public override int ReadyCount
        {
            get { return Length - List[0].Count; }
        }
        #endregion

        /// <summary>
        /// 現在の値
        /// </summary>
        public double CurrentValue
        {
            get
            {
                if (List[0].Count <= 0)
                {
                    return 0;
                }
                return NonApiUtility.Last(List[0], 0);
            }
        }

        #region 初期化
        /// <summary>
        /// 平均の計算を行う
        /// </summary>
        /// <param name="timeScale">分足コード</param>
        /// <param name="parameter">0:長さ</param>
        public Sma(TimeScale timeScale, List<double> parameter) : base(HistoryKind.SMA, timeScale, parameter)
        {
            // リストを作成
            // 0:単純移動平均
            List.Add(new List<double>());

            if (parameter.Count < 1)
            {
                Logger.Log("パラメータが足りません:Sma");
                parameter = new List<double>
                {
                    1
                };
                Parameter = parameter;
            }
        }
        #endregion

        #region Calculate:計算
        /// <summary>
        /// 平均を計算する
        /// 定められたSecondsValue周期で呼び出される
        /// </summary>
        public override void Calculate()
        {
            // 履歴に追加
            List[0].Add(NonApiUtility.CalcSma(baseList, Length));
        }
        #endregion
    }
}
