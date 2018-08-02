using System;
using System.Collections.Generic;
using System.Linq;

namespace Penguinium.Common
{
    /// <summary>
    /// APIなどを呼び出さない単純な関数を集める
    /// 計算用関数など
    /// </summary>
    public class NonApiUtility
    {
        #region ToRoundDown:指定した精度の数値に切り捨てします。
        /// ------------------------------------------------------------------------
        /// <summary>
        ///     指定した精度の数値に切り捨てします。</summary>
        /// <param name="dValue">
        ///     丸め対象の倍精度浮動小数点数。</param>
        /// <param name="iDigits">
        ///     戻り値の有効桁数の精度。小数第何位か</param>
        /// <returns>
        ///     iDigits に等しい精度の数値に切り捨てられた数値。</returns>
        /// ------------------------------------------------------------------------
        public static double ToRoundDown(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Floor(dValue * dCoef) / dCoef :
                                System.Math.Ceiling(dValue * dCoef) / dCoef;
        }
        #endregion

        #region GetDecimal:小数第n位の数値を取得する
        /// <summary>
        /// 小数第n位の数値を取得する
        /// </summary>
        /// <param name="dValue">値</param>
        /// <param name="iDigits">小数第何位か</param>
        /// <returns></returns>
        public static int GetDecimal(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);
            int coef = (int)dCoef;
            coef = coef % 10;

            return coef;
        }
        #endregion

        #region calcTrend:配列のラストが上昇か下降かを判定する、今は使っていない
        /// <summary>
        /// 配列のラストが上昇か下降かを判定する
        /// 今は使っていない
        /// </summary>
        /// <param name="list">判定対象の価格履歴</param>
        /// <returns>"UP"か"DOWN",データが足りなければ"BUYSELL"</returns>
        public static OrderSide CalcTrend(List<double> list)
        {
            OrderSide result = OrderSide.BUYSELL;
            if (list.Count > 1)
            {
                double delta = Last(list, 0) - Last(list, 1);
                if (delta > 0)
                {
                    result = OrderSide.BUY; //上昇トレンド
                }
                else
                {
                    result = OrderSide.SELL;   // 下降トレンド
                }
            }
            return result;
        }
        #endregion

        #region last:配列を逆からアクセスします

        // TODO: LastOrDefaultと置き換えること！
        // LastOrDefaultは該当要素がない場合nullや規定値を返却

        /// <summary>
        /// 配列を逆からアクセスします
        /// 最後の要素を返却
        /// </summary>
        public static double Last(List<double> list)
        {
            if (list.Count == 0)
            {
                return 0;
            }
            return Last<double>(list);
        }
        /// <summary>
        /// 配列を逆からアクセスします
        /// 0から
        /// </summary>
        public static T Last<T>(List<T> list)
        {
            return Last<T>(list, 0);
        }
        /// <summary>
        /// 配列を逆からアクセスします
        /// 0から
        /// </summary>
        public static double Last(List<double> list, int i)
        {
            if (list.Count == 0)
            {
                return 0;
            }
            return Last<double>(list, i);
        }
        /// <summary>
        /// 配列を逆からアクセスします
        /// 0から
        /// </summary>
        public static T Last<T>(List<T> list, int i)
        {
            return list[list.Count - 1 - i];
        }
        #endregion

        #region 配列から、最後から数件を取り出した差分配列を取得する
        /// <summary>
        /// 配列から、最後から数件を取り出した差分配列を取得する
        /// 簡易コピーなので、オブジェクト型の配列の場合は中身を変更しないこと
        /// 指定した長さ未満だった場合、取れるだけ取る
        /// </summary>
        /// <typeparam name="T">配列型</typeparam>
        /// <param name="list">元ソース配列</param>
        /// <param name="size">最後からいくつ取り出すか</param>
        /// <returns>配列</returns>
        public static List<T> LastSubList<T>(List<T> list, int size)
        {
            int index = 0;
            int count = 0;
            // どのくらいを引き出すか計算する
            if (list.Count >= size)
            {
                // 十分な要素数の場合、普通に取得
                index = list.Count - size;
                count = size;
                return list.GetRange(index, count);
            }
            else
            {
                // 足りない場合はそのまま返す
                return list;
            }
        }
        #endregion

        #region calcAve:配列の最新数件の平均を求める

        /// <summary>
        /// 配列の最新数件の平均を求める
        /// </summary>
        /// <param name="length">最後から何件を平均するか、配列が足りない場合はあるだけ平均する</param>
        /// <returns></returns>
        public static double CalcSma(List<double> list, int length)
        {
            decimal ave = 0;
            length = Math.Min(list.Count, length);
            // 合計を求める
            for (int i = 0; i < length; i++)
            {
                ave += (decimal)Last(list, i);
            }
            // 割る
            decimal result = ave / length;
            return (double)result;
        }
        #endregion

        #region calcEma:指数移動平均線を求める

        /// <summary>
        /// 指数移動平均(EMA)を求める
        /// </summary>
        /// <param name="length">長さ</param>
        /// <param name="refList">計算元の値のリスト</param>
        /// <param name="lastEma">最新のEMA（無かったら適当な値）</param>
        /// <returns></returns>
        public static double CalcEma(int length, List<double> refList, double lastEma)
        {
            if (refList.Count <= length)
            {
                return CalcSma(refList, length);
            }
            else
            {
                decimal k = 2.0m / (length + 1);
                decimal close = (decimal)Last(refList);
                decimal dLastEma = (decimal)lastEma;
                return (double)(dLastEma + (close - dLastEma) * k);
            }
        }

        #endregion

        #region calcSigma:標準偏差を求める

        /// <summary>
        /// 標準偏差を求める
        /// </summary>
        /// <returns>標準偏差</returns>
        /// <param name="length">ラストから何件の標準偏差を求めるか</param>
        /// <param name="refList">単純履歴</param>
        public static double CalcSigma(int length, List<double> refList, double average)
        {
            var lastList = NonApiUtility.LastSubList(refList, length);

            if (lastList.Count > 0 && length > 0)
            {
                double sum1 = 0;
                foreach (var item in lastList)
                {
                    var temp = item - average;
                    sum1 += temp * temp;
                }
                return Math.Sqrt(sum1 / (length - 1));
            }
            return -1;
        }
        #endregion

        #region calcDelta:1回の変化量を求める
        /// <summary>
        /// 1回の変化量を求める
        /// 配列の最後2要素の変化量を算出する
        /// </summary>
        /// <param name="refList">平均</param>
        /// <returns></returns>
        public static double CalcDelta(List<double> refList)
        {
            return Last(refList, 0) - Last(refList, 1);
        }
        #endregion

        #region isBuySignal:売買シグナル
        /// <summary>
        /// 売買シグナル
        /// list1の方に短いスパンの配列を入れること
        /// リストの最後の要素を入れること
        /// </summary>
        /// <param name="list1Last"></param>
        /// <param name="list2Last"></param>
        /// <returns>短期の方の値が高ければtrueで買いトレンドとなる、そうでなければfalseで売りトレンド</returns>
        public static bool IsBuySignal(double list1Last, double list2Last)
        {
            return list1Last > list2Last;
        }
        #endregion

        #region isCross:クロスしたかどうかを判定する
        /// <summary>
        /// クロスしたかどうかを判定する
        /// </summary>
        /// <param name="aOld">Aの1個前のデータ</param>
        /// <param name="bOld">Bの1個前のデータ</param>
        /// <param name="aCurrent">Aの現在のデータ</param>
        /// <param name="bCurrent">Bの現在のデータ</param>
        /// <returns>クロスしていたらtrue</returns>
        public static bool IsCross(double aOld, double bOld, double aCurrent, double bCurrent)
        {
            double lastd = aOld - bOld;
            double current = aCurrent - bCurrent;

            // 符号が違ってたらクロス
            return lastd * current < 0;
        }
        #endregion
    }
}
