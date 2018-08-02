using System.Collections.Generic;
using Chickenium.Dao;
using Penguinium.Common;
using Penguinium.ProcessData;
using Chickenium;

namespace Penguinium.Technical
{
    //MACDの設定期間として一番使われているのが、考案者のジェラルド・アペルにより推奨されていて、一般的なMACDの設定として初期値とされている場合の多い、
    //「MACD: 12日、24日 シグナル: 9日」の設定が基本となります。
    //その後、MACDの反応を更に上げるため、クリス・マニングにより推奨された設定が、
    //「MACD: 9日、17日 シグナル: 7日」となります。

    /// <summary>
    /// MACDの計算を行う
    /// パラメータの内容
    /// 0:短期EMA Length(def:12)
    /// 1:長期EMA Length(def:26)
    /// 2:Signal Length(def:9)
    /// Listの内容
    /// 0:短期EMA
    /// 1:長期EMA
    /// 2:MACD = 短期 - 長期(プラスなら上昇トレンド)
    /// 3:シグナル(MACDのEMAもしくは移動平均線)
    /// 4:ヒストグラム = MACD - シグナル(プラスなら上昇トレンド)
    /// </summary>
    public class Macd : HistoryProperty
    {
        #region フィールド
        double oldHistogram;
        double currentHistogram;

        /// <summary>
        /// パラメータ0:短期EMA Length(def:12)
        /// </summary>
        /// <value>短期EMA Length(def:12)</value>
        private int ShortEmaLength
        {
            get { return (int)this.Parameter[0]; }
        }

        /// <summary>
        /// パラメータ1:長期EMA Length(def:26)
        /// </summary>
        /// <value>長期EMA Length(def:26)</value>
        private int LongEmaLength
        {
            get { return (int)this.Parameter[1]; }
        }

        /// <summary>
        /// パラメータ2:Signal Length(def:9)
        /// </summary>
        /// <value>Signal Length(def:9)</value>
        private int SignalLength
        {
            get { return (int)this.Parameter[2]; }
        }

        /// <summary>
        /// 最新のヒストグラム
        /// プラスなら上昇トレンド
        /// </summary>
        /// <value>最新のMACD</value>
        public double CurrentHistogram
        {
            get { return currentHistogram; }
        }

        /// <summary>
        /// ヒストグラムが増えていたらプラス
        /// </summary>
        /// <value>ヒストグラム増分</value>
        public double DeltaHistogram
        {
            get { return currentHistogram - oldHistogram; }
        }

        /// <summary>
        /// データの取得が十分に行われ、アルゴリズムに使えるなら0以下
        /// </summary>
        /// <value>0以下ならば準備できている</value>
        public override int ReadyCount
        {
            get { return LongEmaLength - List[0].Count; }
        }
        #endregion

        #region 初期化
        /// <summary>
        /// 平均の計算を行う
        /// </summary>
        /// <param name="timeScale">分足コード</param>
        /// <param name="parameter">0:短期EMA Length、1:長期EMA Length、2:Signal Length</param>
        public Macd(TimeScale timeScale, List<double> parameter) : base(HistoryKind.MACD, timeScale, parameter)
        {
            oldHistogram = 0;
            currentHistogram = 0;
            // リストを作成
            // 0:短期EMA
            // 1:長期EMA
            // 2:MACD
            // 3:Signal
            // 4:ヒストグラム
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());

            if (parameter.Count < 3)
            {
                Logger.Log("パラメータが足りません:Macd");
                parameter = new List<double>
                {
                    12,
                    26,
                    9
                };
                Parameter = parameter;
            }
        }
        #endregion

        #region Calculate:計算
        /// <summary>
        /// MACDを計算する
        /// 定められたSecondsValue周期で呼び出される
        /// </summary>
        public override void Calculate()
        {
            oldHistogram = currentHistogram;

            // 12分足EMA
            double temp1 = NonApiUtility.CalcEma(ShortEmaLength, baseList, NonApiUtility.Last(List[0]));
            List[0].Add(temp1);

            // 26分足EMA
            double temp2 = NonApiUtility.CalcEma(LongEmaLength, baseList, NonApiUtility.Last(List[1]));
            List[1].Add(temp2);

            // MACD
            double macd = temp1 - temp2;
            List[2].Add(macd);

            // MACDシグナル
            // 9分
            double signal = NonApiUtility.CalcEma(SignalLength, List[2], NonApiUtility.Last(List[3]));
            List[3].Add(signal);

            // ヒストグラム
            currentHistogram = macd - signal;
            List[4].Add(currentHistogram);
        }
        #endregion
    }
}
