using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject4
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _fakeResponse;
        private readonly HttpStatusCode _fakeStatusCode;

        public MockHttpMessageHandler(string fakeResponse, HttpStatusCode fakeStatusCode)
        {
            _fakeResponse = fakeResponse;
            _fakeStatusCode = fakeStatusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = _fakeStatusCode,
                Content = new StringContent(_fakeResponse)
            };
            return Task.FromResult(responseMessage);
        }
    }
}