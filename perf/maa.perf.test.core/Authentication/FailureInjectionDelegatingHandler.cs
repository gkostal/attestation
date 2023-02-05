using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace maa.perf.test.core.Authentication
{
    class FailureInjectionDelegatingHandler : DelegatingHandler
    {
        private List<Tuple<string, string>> _replacementPairs;

        public FailureInjectionDelegatingHandler(HttpMessageHandler innerHandler, List<Tuple<string, string>> replacementPairs)
        : base(innerHandler)
        {
            _replacementPairs = replacementPairs;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var originalHeaders = request.Content?.Headers;
            if (originalHeaders?.ContentType != null)
            {
                if ((originalHeaders.ContentType.MediaType == "application/json") ||
                    (originalHeaders.ContentType.MediaType == "text/plain"))
                {
                    var json = await request.Content.ReadAsStringAsync();
                    foreach (var r in _replacementPairs)
                    {
                        json = json.Replace(r.Item1, r.Item2, StringComparison.InvariantCulture);
                    }
                    request.Content = new StringContent(json, Encoding.UTF8, originalHeaders.ContentType.MediaType);
                }
            }
            return await base.SendAsync(request, cancellationToken);
        }

    }
}
