using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Penguinium.ApiBridge;
using Penguinium.Common;

namespace Penguinium.ProcessData
{
    #region Asset:資産情報
    // TODO:通貨の種類はハードコーディングされているので、要修正
    /// <summary>
    /// 資産情報
    /// </summary>
    public class Asset
    {
        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="_asset">取得した資産情報</param>
        public Asset(RawAsset _asset)
        {
            Code = (AssetCode)Enum.Parse(typeof(AssetCode), _asset.currency_code, true);
            Amount = _asset.amount;
            Available = _asset.available;
        }
        #endregion

        #region プロパティ
        [JsonConverter(typeof(StringEnumConverter))]
        // 通貨の種類
        public AssetCode Code { get; set; }
        // 数量
        public double Amount { get; set; }
        // 数量のうち、利用出来る量
        public double Available { get; set; }
        #endregion

        public override string ToString()
        {
            return "Asset" + JsonConvert.SerializeObject(this);
        }
    }
    #endregion

    #region AssetList:保有している通貨と量のリスト
    /// <summary>
    /// 保有している通貨と量のリスト
    /// </summary>
    public class AssetList
    {
        #region コンストラクタ
        public AssetList(List<Asset> _assets)
        {
            foreach (var asset in _assets)
            {
                switch (asset.Code)
                {
                    case AssetCode.JPY:
                        Jpy = asset; break;
                    case AssetCode.BTC:
                        Btc = asset; break;
                    case AssetCode.ETH:
                        Eth = asset; break;
                }
            }
        }
        #endregion

        #region プロパティ
        public Asset Jpy { get; set; }
        public Asset Btc { get; set; }
        public Asset Eth { get; set; }
        #endregion

        public override string ToString()
        {
            return "Asset" + JsonConvert.SerializeObject(this);
        }
    }
    #endregion

    #region Collateral:証拠金情報
    /// <summary>
    /// 証拠金情報
    /// </summary>
    public class Collateral
    {
        #region コンストラクタ
        public Collateral(RawCollateral _c)
        {
            CollateralAmount = _c.collateral;
            OpenPositionProfitAndLoss = _c.open_position_pnl;
            RequiredCollateral = _c.require_collateral;
            KeepRate = _c.keep_rate;
        }
        #endregion

        #region プロパティ
        public double CollateralAmount { get; set; } // 預け入れた日本円証拠金の額(円)
        public double OpenPositionProfitAndLoss { get; set; } // 建玉の評価損益合計(円)
        public double RequiredCollateral { get; set; }// 現在の必要証拠金(円)
        public double KeepRate { get; set; }// 現在の証拠金維持率
        #endregion

        public override string ToString()
        {
            return "Collateral" + JsonConvert.SerializeObject(this);
        }
    }
    #endregion

}
