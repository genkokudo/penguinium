using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Penguinium
{
    // このままでも使えるけど、実装方法を考え直す事
    // Chickeniumを通して取得するように変更する

    /// <summary>
    /// パラメータ（ApplicationSettings）の項目名
    /// </summary>
    public enum ParameterEnums
    {
        reflashSwitch,
        rangeHeight,
        isRangeBreak,
        rangeBreakWaitTime,
        amount,
        ifDoneSpread,
        updateTime,
        rangeMinutes,
        isManualStop,
        margin,
        higenerai,
        isNoSell,
        isNoBuy,
        isOco,
        ocoLimit,
        isCompound,
        compoundMultiple,
        debugLog,
        apiKey,
        apiSelect,
        productCode
    }

    /// <summary>
    /// 取引所ごとにパラメータを管理する
    /// このクラスオブジェクト1つで全取引所のパラメータを管理する
    /// </summary>
    public class Parameter
    {
        #region アプリケーション設定
        /// <summary>
        /// アプリケーション設定を取得するコマンド
        /// </summary>
        private SqlCommand ApplicationSettingsCommand;

        /// <summary>
        /// 全取引所アプリケーション設定
        /// 取引所ID、パラメータ名
        /// </summary>
        private Dictionary<int, Dictionary<string, string>> parameter = new Dictionary<int, Dictionary<string, string>>();

        /// <summary>
        /// 全取引所アプリケーション設定
        /// </summary>
        public Dictionary<int, Dictionary<string, string>> ApplicationSettings
        {
            get { return parameter; }
        }
        #endregion

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="connection">接続</param>
        public Parameter(SqlConnection connection)
        {
            // 実行するSQLの準備（アプリケーション設定用）
            ApplicationSettingsCommand = new SqlCommand
            {
                Connection = connection,
                CommandText = @"SELECT ExchangeId, Name, Value FROM ApplicationSettings"
            };
        }

        /// <summary>
        /// アプリケーション設定を再取得する
        /// </summary>
        public void ReloadApplicationSettings()
        {
            // SQLの実行
            using (var reader = ApplicationSettingsCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    string Name = reader["Name"] as string;
                    int ExchangeId = (int)reader["ExchangeId"];
                    if (!parameter.ContainsKey(ExchangeId))
                    {
                        parameter[ExchangeId] = new Dictionary<string, string>();
                    }
                    parameter[ExchangeId][Name] = reader["Value"] as string;
                }
            }
        }
    }
}
