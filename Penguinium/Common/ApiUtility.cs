using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Penguinium.ApiBridge;
using Penguinium.Client;
using Penguinium.ProcessData;

namespace Penguinium.Common
{
    /// <summary>
    /// APIを呼び出す取引所に依存した関数を集める
    /// ※複数の取引所を扱うようになったらここを大改造する
    /// </summary>
    public class ApiUtility
    {
        #region Tejimai:建玉の手じまいをする（BFのみ）
        /// <summary>
        /// Bitflyerの建玉の手じまいをする
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task Tejimai(BitflyerClient client)
        {
            await client.CancelAllOrders();

            double buy = 0;
            double sell = 0;
            try
            {
                Console.WriteLine("leave all positions");
                var positions = await client.GetMyPositions();
                foreach (var p in positions)
                {
                    if (p.Side == OrderSide.BUY)
                    {
                        sell += p.Size;
                    }
                    else if (p.Side == OrderSide.SELL)
                    {
                        buy += p.Size;
                    }
                }
                if (sell >= 0.001)
                {
                    //sell = ToRoundDown(sell, 3);
                    await client.Sell(sell);
                }
                if (buy >= 0.001)
                {
                    //buy = ToRoundDown(buy, 3);
                    await client.Buy(buy);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + "," + sell + "," + buy);
            }
        }

        #endregion


        #region GetPastPrices:Bitflyerの過去の価格履歴をまとめて取得する
        // TODO:リファクタリング対象

        /// <summary>
        /// 過去の価格履歴を取得
        /// https://cryptowatch.jp/docs/api#ohlc
        /// </summary>
        /// <param name="seconds">時間足の秒数（15分足なら900）</param>
        public static async Task<List<Candle>> GetPastPrices(int seconds, bool isFx)
        {
            var getAddress = "/markets/bitflyer/btcjpy/ohlc?periods=";
            if (isFx)
            {
                getAddress = "/markets/bitflyer/btcfxjpy/ohlc?periods=";
            }

            // API
            var client = new ApiClient(10, "https://api.cryptowat.ch");
            var json = await client.Get(getAddress + seconds);
            var resultList = new List<Candle>();

            // 応答パース
            try
            {
                RawCandleResultHead _candle = JsonConvert.DeserializeObject<RawCandleResultHead>(json);
                switch (seconds)
                {
                    case 60:
                        foreach (var item in _candle.Result.Result60)
                        {
                            resultList.Add(new Candle(item));
                        }
                        break;
                    case 180:
                        foreach (var item in _candle.Result.Result180)
                        {
                            resultList.Add(new Candle(item));
                        }
                        break;
                    case 300:
                        foreach (var item in _candle.Result.Result300)
                        {
                            resultList.Add(new Candle(item));
                        }
                        break;
                    case 900:
                        foreach (var item in _candle.Result.Result900)
                        {
                            resultList.Add(new Candle(item));
                        }
                        break;
                    case 1800:
                        foreach (var item in _candle.Result.Result1800)
                        {
                            resultList.Add(new Candle(item));
                        }
                        break;
                    case 3600:
                        foreach (var item in _candle.Result.Result3600)
                        {
                            resultList.Add(new Candle(item));
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetBoardError: " + ex.Message);
            }
            return resultList;
        }
        #endregion

    }
}
