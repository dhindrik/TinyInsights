using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TinyInsightsLib
{
    public class TinyInsightsMessageHandler : DelegatingHandler
    {
        public TinyInsightsMessageHandler()
        {
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;

            try
            {


                var response = await base.SendAsync(request, cancellationToken);

                var endTime = DateTime.Now;

                Exception exception = null;

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    exception = e;
                }

                await TinyInsights.TrackDependencyAsync(request.RequestUri.Host, request.RequestUri.ToString(), startTime, endTime - startTime,
                    response.IsSuccessStatusCode, (int)response.StatusCode, exception);

                return response;
            }
            catch (Exception ex)
            {
                var endTime = DateTime.Now;
                await TinyInsights.TrackDependencyAsync(request.RequestUri.Host, request.RequestUri.ToString(), startTime, endTime - startTime,
                   false, 0, ex);

                throw;
            }
        }
    }
}