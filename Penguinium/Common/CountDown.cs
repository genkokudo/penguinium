using System;
using System.Collections.Generic;
using System.Text;

namespace Penguinium.Common
{
    /// <summary>
    /// カウントダウンして、時間が来たら1度だけ実行
    /// </summary>
    public class CountDown
    {
        #region フィールド
        // 現在のカウント
        int count;
        // 初期値
        int limit;
        // 実行するカウント
        int triggerCount;

        /// <summary>
        /// カウント中ならtrue
        /// トリガーのタイミングは含まない
        /// </summary>
        public bool IsCounting
        {
            get { return count > 0; }
        }
        #endregion

        #region 初期化
        /// <summary>
        /// 設定した回数カウントしたら実行
        /// 1カウント目に初回実行する
        /// </summary>
        /// <param name="limit">カウント回数</param>
        public CountDown(int limit)
        {
            Initialize(limit, 0);
        }

        /// <summary>
        /// 設定した回数カウントしたら実行
        /// </summary>
        /// <param name="limit">カウントスタート回数</param>
        /// <param name="triggerCount">イベントを起こすカウント</param>
        public CountDown(int limit, int triggerCount)
        {
            Initialize(limit, triggerCount);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="limit">回数</param>
        /// <param name="triggerCount">イベントを起こすカウント</param>
        private void Initialize(int limit, int triggerCount)
        {
            this.limit = limit;
            this.triggerCount = triggerCount;
            StopForce();
        }

        /// <summary>
        /// 最初からカウントダウンする
        /// </summary>
        public void InitCount()
        {
            this.count = limit;
        }

        /// <summary>
        /// カウントダウン強制停止
        /// </summary>
        public void StopForce()
        {
            this.count = -1;
        }
        #endregion

        #region 更新
        /// <summary>
        /// 更新する
        /// </summary>
        /// <returns>イベント発生タイミングならtrue</returns>
        public bool Update()
        {
            count = Math.Max(-1, count - 1);
            return count == triggerCount;
        }
        #endregion
    }
}
