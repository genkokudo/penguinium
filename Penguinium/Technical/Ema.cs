using System.Collections.Generic;
using Chickenium.Dao;
using Penguinium.Common;
using Penguinium.ProcessData;
using Chickenium;

namespace Penguinium.Technical
{
    /// <summary>
    /// 指数平滑移動平均の計算を行う（SMAよりも反応早い）
    /// </summary>
    public class Ema : HistoryProperty
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

        /// <summary>
        /// 増分
        /// 0以上なら上昇トレンド
        /// </summary>
        public double Delta
        {
            get
            {
                if(List[0].Count <= 1)
                {
                    return 0;
                }
                return NonApiUtility.Last(List[0], 0) - NonApiUtility.Last(List[0], 1);
            }
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
        /// 指数平均移動線(EMA)の計算を行う
        /// </summary>
        /// <param name="timeScale">分足コード</param>
        /// <param name="parameter">0:長さ</param>
        public Ema(TimeScale timeScale, List<double> parameter) : base(HistoryKind.EMA, timeScale, parameter)
        {
            // リストを作成
            // 0:単純移動平均
            List.Add(new List<double>());

            if (parameter.Count < 1)
            {
                Logger.Log("パラメータが足りません:Ema");
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
        /// 指数平均移動線(EMA)を計算する
        /// 定められたSecondsValue周期で呼び出される
        /// </summary>
        public override void Calculate()
        {
            // 履歴に追加
            List[0].Add(NonApiUtility.CalcEma(Length, baseList, NonApiUtility.Last(List[0])));
        }
        #endregion
    }
}
