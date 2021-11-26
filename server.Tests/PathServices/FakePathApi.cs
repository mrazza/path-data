namespace PathApi.Server.Tests.PathServices
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PathApi.Server.PathServices;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A fake implementation of the PATH HTTP API.
    /// </summary>
    /// <remarks>
    /// This starts up a local HTTP server to serve fake PATH API responses.
    /// </remarks>
    internal sealed class FakePathApi : IDisposable
    {
        private HttpListener httpListener;
        private Dictionary<string, Func<HttpListenerContext, string>> httpResponses;
        private string apiKey;

        public FakePathApi(int port, string apiKey)
        {
            this.apiKey = apiKey;
            this.httpResponses = new Dictionary<string, Func<HttpListenerContext, string>>();
            this.httpListener = new HttpListener();
            this.httpListener.Prefixes.Add($"http://+:{port}/");
        }

        public void StartListening()
        {
            this.httpListener.Start();
            this.httpListener.BeginGetContext(new AsyncCallback(this.HandleHttpRequest), null);
        }

        public void AddExpectedRequest(string url, string response)
        {
            this.AddExpectedRequest(url, (_) => response);
        }

        public void AddExpectedRequest(string url, Func<HttpListenerContext, string> responseResolver, bool requireApiKey = true)
        {
            if (requireApiKey)
            {
                this.httpResponses[url] = (context) =>
                {
                    RequireHeader(context, "apikey", this.apiKey);
                    return responseResolver(context);
                };
            }
            else
            {
                this.httpResponses[url] = responseResolver;
            }
        }

        private static void RequireHeader(HttpListenerContext context, string header, string value)
        {
            if (context.Request.Headers[header] != value)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentLength64 = 0;
                context.Response.OutputStream.Close();
                throw new InvalidOperationException(string.Format("Bad or missing header {0}.", header));
            }
        }

        private void HandleHttpRequest(IAsyncResult asyncResult)
        {
            try
            {
                var context = this.httpListener.EndGetContext(asyncResult);
                var url = context.Request.Url.ToString();

                if (this.httpResponses.ContainsKey(url))
                {
                    string result;

                    try
                    {
                        result = this.httpResponses[url](context);
                    }
                    catch (Exception e)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentLength64 = 0;
                        context.Response.OutputStream.Close();
                        throw;
                    }

                    if (result == null)
                    {
                        context.Response.ContentLength64 = 0;
                        context.Response.StatusCode = 404;
                        context.Response.OutputStream.Close();
                    }
                    else
                    {
                        var response = Encoding.UTF8.GetBytes(result);
                        context.Response.ContentLength64 = response.Length;
                        context.Response.SendChunked = false;
                        context.Response.StatusCode = 200;
                        context.Response.OutputStream.Write(response);
                        context.Response.OutputStream.Close();
                    }
                }
                else
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentLength64 = 0;
                    context.Response.OutputStream.Close();
                    throw new InvalidOperationException("Unexpected HTTP request! " + url);
                }
            }
            finally
            {
                this.httpListener.BeginGetContext(new AsyncCallback(this.HandleHttpRequest), null);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.httpListener.Stop();
                    this.httpListener.Close();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}