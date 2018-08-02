using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Penguinium.ApiBridge
{
    /// <summary>
    /// クライアント、このクラスを通してAPI操作を行う
    /// </summary>
    public class ApiClient
    {
        const string POST = "POST";
        const string GET = "GET";
        string http = "https://api.bitflyer.jp";

        string API_SECRET = null;
        string API_KEY = null;
        double m_timeoutSec;

        /// <summary>
        /// API操作を行う
        /// </summary>
        /// <param name="timeoutSec"></param>
        /// <param name="http">例えば"https://api.bitflyer.jp/v1/ticker"ならば"https://api.bitflyer.jp"を設定</param>
        public ApiClient(double timeoutSec, string http)
        {
            this.http = http;
            this.m_timeoutSec = timeoutSec;
        }

        /// <summary>
        /// API操作を行う
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="apiSecret"></param>
        /// <param name="timeoutSec"></param>
        /// <param name="http">例えば"https://api.bitflyer.jp/v1/ticker"ならば"https://api.bitflyer.jp"を設定</param>
        public ApiClient(string apiKey, string apiSecret, double timeoutSec, string http)
        {
            this.http = http;
            this.API_KEY = apiKey;
            this.API_SECRET = apiSecret;
            this.m_timeoutSec = timeoutSec;
        }

        public async Task<string> Get(string path)
        {
            return await _Send(GET, path, "");
        }

        public async Task<string> Post(string path, string body)
        {
            return await _Send(POST, path, body);
        }

        private async Task<string> _Send(string httpMethod, string path, string body)
        {
            Int64 timestamp_ = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            string timestamp = "" + timestamp_;

            // 例: 1POST/v1/me/sendchildorder{"product_code":"FX_BTC_JPY","child_order_type":"LIMIT","side":"BUY","price":30000,"size":0.1}
            string text = timestamp + httpMethod + path + body;

            // クライアント
            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(m_timeoutSec) // タイムアウト
            };
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            if (API_KEY != null)
            {
                client.DefaultRequestHeaders.Add("ACCESS-KEY", API_KEY);
            }
            client.DefaultRequestHeaders.Add("ACCESS-TIMESTAMP", timestamp);

            // ハッシュ計算
            // 例: 008f86d0c56d36f1231596dfddd31cef85452fd2189357e2837f9cb3791d7144
            if (API_SECRET != null)
            {
                client.DefaultRequestHeaders.Add("ACCESS-SIGN", GetSign(text, API_SECRET));
            }

            // リクエスト送信
            HttpResponseMessage response;
            try
            {
                if (httpMethod == POST)
                {
                    response = await client.PostAsync(http + path, content);
                }
                else
                {
                    response = await client.GetAsync(http + path);
                }
            }
            catch (TaskCanceledException)
            {
                throw new Exception("API TIMEOUT: " + path);
            }

            // 応答受け取り
            var responseText = await response.Content.ReadAsStringAsync();
            return responseText;
        }

        /// <summary>
        /// テキストとAPI鍵でサイン作成
        /// </summary>
        /// <param name="text"></param>
        /// <param name="apiSecret"></param>
        /// <returns></returns>
        private string GetSign(string text, string apiSecret)
        {
            if(apiSecret == null)
            {
                return null;
            }
            HMACSHA256 hmac = new HMACSHA256(Encoding.ASCII.GetBytes(API_SECRET));
            byte[] sign_ = hmac.ComputeHash(Encoding.ASCII.GetBytes(text));
            string sign = "";
            for (int i = 0; i < sign_.Length; i++)
            {
                sign += string.Format("{0:x2}", sign_[i]);
            }
            return sign;
        }
    }
}
