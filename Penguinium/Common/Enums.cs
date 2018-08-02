using System.Collections.Generic;
using Newtonsoft.Json;

namespace Penguinium.Common
{
    // TODO:通貨コード、ハードコーディングする必要なさそう、あとでDBにする
    public enum AssetCode
    {
        JPY,
        BTC,
        ETH
    }

    // これもハードコーディングいらないかも
    public enum BoardOrderSide
    {
        ASK, // 売り
        BID // 買い
    }

    public enum OrderSide
    {
        BUY,
        SELL,
        BUYSELL
    }

    public enum Exchange
    {
        BITFLYER = 1,
        BITFLYER_FX = 2
    }

}
