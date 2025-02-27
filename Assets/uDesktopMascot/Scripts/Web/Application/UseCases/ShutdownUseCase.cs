using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace uDesktopMascot.Web.Application
{
    /// <summary>
    ///  シャットダウンユースケース
    /// </summary>
    public class ShutdownUseCase
    {
        /// <summary>
        ///  シャットダウン
        /// </summary>
        /// <param name="context">コンテキスト</param>
        public void Shutdown(HttpListenerContext context)
        {
            try
            {
                SendSuccessResponse(context, "サーバーを停止しました");

                SystemManager.Instance.DisposeWebServer();
            }
            catch (Exception e)
            {
                ReturnInternalError(context.Response, e);
            }
        }

        /// <summary>
        ///  成功レスポンスを送信する
        /// </summary>
        /// <param name="context">コンテキスト</param>
        /// <param name="message">メッセージ</param>
        private void SendSuccessResponse(HttpListenerContext context, string message)
        {
            var responseData = new { message };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responseData));

            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        /// <summary>
        ///  内部エラーを返す
        /// </summary>
        /// <param name="response">レスポンス</param>
        /// <param name="e">例外</param>
        private void ReturnInternalError(HttpListenerResponse response, Exception e)
        {
            var errorData = new { error = "シャットダウンに失敗しました", detail = e.Message };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(errorData));

            response.AppendHeader("Access-Control-Allow-Origin", "*");
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.ContentType = "application/json";
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }
    }
}