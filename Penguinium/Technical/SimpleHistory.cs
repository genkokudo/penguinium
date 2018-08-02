using System;
using System.Collections.Generic;
using Chickenium.Dao;
using Penguinium.Common;
using Penguinium.ProcessData;

namespace Penguinium.Technical
{
    /// <summary>
    /// 単純履歴
    /// パラメータなし
    /// </summary>
    public class SimpleHistory : HistoryProperty
    {
        /// <summary>
        /// 単純履歴
        /// </summary>
        public List<double> BaseList { get { return baseList; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Penguinium.Technical.SimpleHistory"/> class.
        /// </summary>
        /// <param name="timeScale">分足コード</param>
        public SimpleHistory(TimeScale timeScale) : base(HistoryKind.HISTORY, timeScale, new List<double>())
        {
        }

        #region Calculate:計算
        /// <summary>
        /// 最新のtickerを履歴に追加する
        /// 定められたSecondsValue周期で呼び出される
        /// </summary>
        public override void Calculate()
        {
            // 何もなし
        }
        #endregion
    }
}
