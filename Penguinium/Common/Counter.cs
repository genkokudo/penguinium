using System;
using System.Collections.Generic;
using System.Text;

namespace Penguinium.Common
{
    /// <summary>
    /// カウントして、時間が来たら実行みたいな感じ
    /// </summary>
    public class Counter
    {
        #region フィールド
        // 現在のカウント
        int count;
        // 周期
        int limit;
        // 実行するカウント
        int triggerCount;
        #endregion

        /// <summary>
        /// 強制的にこの時間待機させ
        /// この時間経過時にトリガー0を引く
        /// </summary>
        /// <param name="time"></param>
        public void WaitForce(int time)
        {
            count = -time;
        }

        #region 初期化
        /// <summary>
        /// 設定した回数カウントしたら実行
        /// 1カウント目に初回実行する
        /// </summary>
        /// <param name="limit">回数（周期）</param>
        public Counter(int limit)
        {
            Initialize(limit, 0);
        }

        /// <summary>
        /// 設定した回数カウントしたら実行
        /// </summary>
        /// <param name="limit">回数（周期）</param>
        /// <param name="triggerCount">イベントを起こすカウント</param>
        public Counter(int limit, int triggerCount)
        {
            Initialize(limit, triggerCount);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="limit">回数（周期）</param>
        /// <param name="triggerCount">イベントを起こすカウント</param>
        private void Initialize(int limit, int triggerCount)
        {
            this.count = -1;
            this.limit = limit;
            this.triggerCount = triggerCount;
        }
        #endregion

        #region 更新
        /// <summary>
        /// 更新する
        /// </summary>
        /// <returns>イベント発生タイミングならtrue</returns>
        public bool Update()
        {
            if (count < 0)
            {
                count++;
            }
            else
            {
                count = (count + 1) % limit;
            }
            return count == triggerCount;
        }
        #endregion
    }
}
