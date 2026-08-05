using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tanakh.Domain;
using Tanakh.Infrastructure.Options;
using Tanakh.Infrastructure.Services;

namespace Tanakh.Tests
{
    public class SmsBalanceServiceTests
    {
        private static SmsBalanceService CreateService(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            HttpClient httpClient = new(new StubHandler(responseBody, statusCode));
            SmsOptions options = new() { Key = "k", User = "u", Pass = "p" };
            return new SmsBalanceService(
                httpClient, Options.Create(options), new MemoryCache(new MemoryCacheOptions()), NullLogger<SmsBalanceService>.Instance);
        }

        [Fact]
        public async Task GetBalanceAsync_Returns_Ok_For_Positive_Balance()
        {
            SmsBalanceService service = CreateService("150");

            SmsBalanceResult result = await service.GetBalanceAsync();

            Assert.True(result.Ok);
            Assert.Equal(150, result.Balance);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task GetBalanceAsync_Returns_Error_For_Negative_Status_Code()
        {
            SmsBalanceService service = CreateService("-4");

            SmsBalanceResult result = await service.GetBalanceAsync();

            Assert.False(result.Ok);
            Assert.Null(result.Balance);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task GetBalanceAsync_Returns_Error_For_Unparseable_Response()
        {
            SmsBalanceService service = CreateService("<html>not sms4free</html>");

            SmsBalanceResult result = await service.GetBalanceAsync();

            Assert.False(result.Ok);
            Assert.Null(result.Balance);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task GetBalanceAsync_Caches_Result_Within_The_Ttl()
        {
            CountingHandler handler = new("42");
            HttpClient httpClient = new(handler);
            SmsOptions options = new() { Key = "k", User = "u", Pass = "p" };
            SmsBalanceService service = new(
                httpClient, Options.Create(options), new MemoryCache(new MemoryCacheOptions()), NullLogger<SmsBalanceService>.Instance);

            await service.GetBalanceAsync();
            await service.GetBalanceAsync();

            Assert.Equal(1, handler.CallCount);
        }

        private class StubHandler : HttpMessageHandler
        {
            private readonly string body;
            private readonly HttpStatusCode statusCode;

            public StubHandler(string body, HttpStatusCode statusCode)
            {
                this.body = body;
                this.statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
        }

        private class CountingHandler : HttpMessageHandler
        {
            private readonly string body;

            public CountingHandler(string body)
            {
                this.body = body;
            }

            public int CallCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
            }
        }
    }
}
