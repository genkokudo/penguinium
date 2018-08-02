using System.Collections.Generic;
using Chickenium.Dao;
using Penguinium.Common;
using Penguinium.ProcessData;
using Chickenium;
using Penguinium.Manager;

namespace Penguinium.Technical
{
    /// <summary>
    /// 2つの相場の乖離を計算する
    /// </summary>
    public class Sfd : HistoryProperty
    {
        #region フィールド
        /// <summary>
        /// 計算の元になるリストへの参照、操作側
        /// ※現物とFXが逆です。。。このまま使わないこと。
        /// </summary>
        List<double> baseList1;
        /// <summary>
        /// 計算の元になる取引所への参照、参照・比較対象側
        /// </summary>
        Dictionary<int, TechnicalManager> RefExchange;

        /// <summary>
        /// 参照取引所ID
        /// </summary>
        Exchange RefId;

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
        /// 現在のSFD：百分率
        /// </summary>
        public double CurrentValue
        {
            get { return NonApiUtility.Last(List[1]); }
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
        /// BFのSFD適用範囲かどうか
        /// このモード時はBOTの挙動を変更した方が良い
        /// 高いのでFXで買わない方が良い
        /// </summary>
        /// <value>BFのSFD適用範囲かどうか</value>
        public bool IsSfdModeHigh
        {
            get
            {
                var val = NonApiUtility.Last(List[1]);
                return val >= 4.5 && val <= 5.5
                               || val >= 9 && val <= 11
                               || val >= 12 && val <= 18;
            }
        }

        /// <summary>
        /// BFのSFD適用範囲かどうか
        /// このモード時はBOTの挙動を変更した方が良い
        /// 低いのでFXで売らない方が良い
        /// </summary>
        public bool IsSfdModeLow
        {
            get
            {
                var val = NonApiUtility.Last(List[1]);
                return val >= -5.5 && val <= -4.5
                               || val >= -11 && val <= -9
                               || val >= -18 && val <= -12;
            }
        }

        // 0.1%の範囲でSFD売買
        double sfdWidth = 0.1;

        /// <summary>
        /// 境界を超えている
        /// </summary>
        /// <value>境界を超えている</value>
        public bool IsSfdHigh
        {
            get
            {
                var val = NonApiUtility.Last(List[1]);
                return val >= 5 && val <= 5.5
                               || val >= 10 && val <= 11
                               || val >= 15 && val <= 18
                               || val >= -5 && val <= -4.5
                               || val >= -10 && val <= -9
                               || val >= -15 && val <= -12;
            }
        }
        /// <summary>
        /// 境界を下回っている
        /// </summary>
        /// <value>境界を下回っている</value>
        public bool IsSfdLow
        {
            get
            {
                var val = NonApiUtility.Last(List[1]);
                return val >= 4.5 && val < 5
                               || val >= 9 && val < 10
                               || val >= 12 && val < 15
                               || val >= -5.5 && val < -5
                               || val >= -11 && val < -10
                               || val >= -18 && val < -15;
            }
        }

        /// <summary>
        /// SFDの攻防領域内
        /// </summary>
        /// <value>境界より0.1%以内ならtrue</value>
        public bool IsCriticalZone
        {
            get
            {
                var val = NonApiUtility.Last(List[1]);
                return val >= 5.0 - sfdWidth && val <= 5.0 + sfdWidth
                               || val >= 10.0 - sfdWidth && val <= 10.0 + sfdWidth
                               || val >= 15.0 - sfdWidth && val <= 15.0 + sfdWidth
                               || val >= -5.0 - sfdWidth && val <= -5.0 + sfdWidth
                               || val >= -10.0 - sfdWidth && val <= -10.0 + sfdWidth
                               || val >= -15.0 - sfdWidth && val <= -15.0 + sfdWidth;
            }
        }
        #endregion

        #region 初期化
        /// <summary>
        /// 2つの相場の乖離を計算する
        /// </summary>
        /// <param name="timeScale">分足コード</param>
        /// <param name="parameter">0:移動平均長さ,1:標準偏差倍率</param>
        /// <param name="baseList1">計算の元になるリストへの参照（同じ時間足の単純履歴）操作側</param>
        /// <param name="refExchange">計算の元になる取引所への参照（同じ時間足の単純履歴）参照・比較対象側</param>
        public Sfd(TimeScale timeScale, List<double> parameter, List<double> baseList1, Dictionary<int, TechnicalManager> refExchange, Exchange refId) : base(HistoryKind.SFD, timeScale, parameter)
        {
            // リストを作成
            // 0:操作側価格 - 参照・比較対象側価格、操作側が高ければプラス
            // 1:乖離率：価格差 / 操作側価格
            // 2:価格差の移動平均
            // 3:価格差の標準偏差
            // 4:価格差の1シグマ上
            // 5:価格差の1シグマ下
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());
            List.Add(new List<double>());

            this.baseList1 = baseList1;
            RefExchange = refExchange;
            RefId = refId;

            if (parameter.Count < 2)
            {
                Logger.Log("パラメータが足りません:Sfd");
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
        /// 解離計算する
        /// </summary>
        public override void Calculate()
        {
            var last1 = NonApiUtility.Last(baseList1, 0);
            var last2 = NonApiUtility.Last(RefExchange[(int)RefId].BaseHistory.BaseList, 0);

            // 差
            var subtract = last1 - last2;
            List[0].Add(subtract);

            // 乖離率
            var subPoint = subtract / last2 * 100.0;
            List[1].Add(subPoint);

            // 差の移動平均線
            double average = NonApiUtility.CalcSma(List[0], Length);
            if (average <= 0)
            {
                average = CurrentPrice;
            }
            List[2].Add(average);

            // 差のシグマ計算
            Sigma = NonApiUtility.CalcSigma(Length, List[0], average) * Magnification;
            List[3].Add(Sigma);

            // 1シグマ上
            List[4].Add(average + Sigma);

            // 1シグマ下
            List[5].Add(average - Sigma);
        }
        #endregion
    }
}
