using System.Collections.Generic;
using Penguinium.Common;
using Chickenium.Dao;
using Penguinium.ProcessData;

namespace Penguinium.Technical
{

    #region HistoryKind:リストの種類
    /// <summary>
    /// リストの種類
    /// </summary>
    public enum HistoryKind
    {
        /// <summary>
        /// 単純履歴
        /// </summary>
        HISTORY = 0,
        /// <summary>
        /// 単純平均
        /// </summary>
        SMA = 1,
        /// <summary>
        /// EMA
        /// 指数移動平均線
        /// </summary>
        EMA = 2,
        /// <summary>
        /// MACD
        /// </summary>
        MACD = 3,
        /// <summary>
        /// ボリンジャーバンド
        /// </summary>
        BollingerBand = 4,
        /// <summary>
        /// チャネル
        /// 一定期間の最大と最小
        /// </summary>
        CHANNEL = 5,
        /// <summary>
        /// 乖離
        /// </summary>
        SFD = 6,
        /// <summary>
        /// 参照履歴(SFDなどで他所と比較するとき)
        /// </summary>
        HISTORYREF = 9
    }
    #endregion

    /// <summary>
    /// データの詳細
    /// </summary>
    public abstract class HistoryProperty
    {
        /// <summary>
        /// テクニカルの種類
        /// </summary>
        public HistoryKind HistoryKind { get; set; }

        /// <summary>
        /// 何分足か
        /// TimeSpanのテーブルのコードを使用
        /// </summary>
        public TimeScale TimeScale { get; set; }

        /// <summary>
        /// 更新用カウント
        /// 時間足の秒数経過したら更新するのに使用する
        /// </summary>
        private int updateCount = -1;

        /// <summary>
        /// 基本的に単純な価格履歴
        /// </summary>
        protected List<double> baseList;

        /// <summary>
        /// 最新の価格
        /// </summary>
        protected double CurrentPrice { get { return NonApiUtility.Last(baseList); } }

        /// <summary>
        /// 計算した値の履歴
        /// 複数あるテクニカルもある
        /// </summary>
        public List<List<double>> List
        {
            get; set;
        }

        /// <summary>
        /// パラメータ
        /// </summary>
        public List<double> Parameter
        {
            get; set;
        }

        /// <summary>
        /// このステップで更新したらTrue
        /// </summary>
        public bool IsUpdated
        {
            get { return updateCount == 0; }
        }

        /// <summary>
        /// データの取得が十分に行われているか
        /// 必要なデータ数を表示
        /// </summary>
        /// <value><c>true</c> if is ready; otherwise, <c>false</c>.</value>
        public virtual int ReadyCount
        {
            get { return 0; }
        }

        /// <summary>
        /// 開始してからのカウント
        /// </summary>
        public virtual int DataCount
        {
            get
            {
                if (List == null || List.Count == 0) return 0;
                return List[0].Count;
            }
        }

        /// <summary>
        /// データの詳細
        /// </summary>
        /// <param name="historyKind">配列の種類</param>
        /// <param name="timeScale">分足コード</param>
        /// <param name="parameter">パラメータ、何個目が何かはテクニカルごとに異なる</param>
        public HistoryProperty(HistoryKind historyKind, TimeScale timeScale, List<double> parameter)
        {
            baseList = new List<double>();
            List = new List<List<double>>();
            updateCount = -1;
            HistoryKind = historyKind;
            TimeScale = timeScale;
            Parameter = parameter;
        }

        ///// <summary>
        ///// 引数で指定した条件の履歴にアクセスするためのキーを作成する
        ///// </summary>
        ///// <returns>ID+履歴の種類文字列+時間足名(ex."HISTORY1s")</returns>
        //public static string GetKeyy(HistoryKind historyKind, TimeScale timeScale)
        //{
        //    return timeScale.Id + historyKind.ToString() + timeScale.Name;
        //}

        ///// <summary>
        ///// 引数で指定した条件の履歴にアクセスするためのキーを作成する
        ///// </summary>
        ///// <returns>ID+履歴の種類文字列+時間足名(ex."HISTORY1s")</returns>
        //public static string GetKeyy(HistoryKind historyKind, int Id, string Name)
        //{
        //    return Id + historyKind.ToString() + Name;
        //}

        /// <summary>
        /// 毎秒更新（必ず毎秒呼び出す）
        /// </summary>
        /// <returns>The update.</returns>
        /// <param name="ticker">Ticker.</param>
        public virtual void Update(Ticker ticker)
        {
            updateCount = (updateCount + 1) % TimeScale.SecondsValue;
            if (IsUpdated)
            {
                baseList.Add(ticker.Itp);
                Calculate();
            }
        }

        /// <summary>
        /// BOT起動時に呼び出して初期データを入力する
        /// </summary>
        /// <param name="Itp">取引価格（終値）</param>
        public virtual void UpdateForInitialize(double Itp)
        {
            baseList.Add(Itp);
            Calculate();
        }

        /// <summary>
        /// 最大件数を超えていたら古いデータを削除する
        /// </summary>
        /// <param name="limitLength">最大件数</param>
        public void RemoveLimit(int limitLength)
        {
            foreach (var item in List)
            {
                while (item.Count > limitLength)
                {
                    item.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 計算する
        /// 定められたSecondsValue周期で呼び出される
        /// ticker.Itp:最終取引価格
        /// </summary>
        public abstract void Calculate();
    }
}