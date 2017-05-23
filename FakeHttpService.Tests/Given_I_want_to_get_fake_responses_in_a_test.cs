using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace FakeHttpService.Tests
{
    public class Given_I_want_to_get_fake_responses_in_a_test
    {
        [Fact]
        public async Task When_requesting_a_registered_Uri_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest(r => true)
                .RespondWith(async r => {}))
            {
                var response = await new HttpClient().GetAsync(fakeService.BaseAddress);

                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task When_response_includes_headers_Then_they_are_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest(r => true)
                .RespondWith(async r =>
                {
                    r.Headers.Append("foo", "bar");
                    r.Headers.Append("colors", new StringValues(new[] {"red", "green", "blue"}));
                }))
            {
                var response = await new HttpClient().GetAsync(fakeService.BaseAddress);

                response.Headers.GetValues("foo").Should().BeEquivalentTo("bar");

                response.Headers.GetValues("colors").Should().BeEquivalentTo("red", "green", "blue");
            }
        }

        [Fact]
        public async Task When_response_includes_a_body_Then_it_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest(r => true)
                .RespondWith(async r =>
                {
                    r.Body.WriteTextAsUtf8BytesAsync("foo bar zap");
                }))
            {
                var response = await new HttpClient().GetAsync(fakeService.BaseAddress);

                var body = await response.Content.ReadAsStringAsync();

                body.Should().Be("foo bar zap");
            }
        }

        [Fact]
        public async Task When_an_expected_request_is_not_made_but_FakeServer_is_configured_to_throw_Then_an_exception_is_thrown()
        {
            Uri address = null;

            Action createServiceWithoutInvoking = () =>
            {
                using (var fakeService = new FakeHttpService(
                    "my service", 
                    throwOnUnusedHandlers: true)
                    .OnRequest(r => r.Path == "foo")
                    .RespondWith(async r => { }))
                {
                    address = fakeService.BaseAddress;
                }
            };

            createServiceWithoutInvoking
                .ShouldThrow<InvalidOperationException>(
                "Because failing to perform a request can be an error")
                .Which
                .Message
                .Should()
                .StartWith( $"{nameof(FakeHttpService)} \"my service\" @ {address} expected requests");
        }

        [Fact]
        public async Task When_an_expected_request_is_not_made_Then_an_exception_is_not_thrown()
        {
            Action createServiceWithoutInvoking = () =>
            {
                using (new FakeHttpService()
                    .OnRequest(r => r.Path == "foo")
                    .RespondWith(async r =>
                    {
                    }))
                {
                }
            };

            createServiceWithoutInvoking.ShouldNotThrow(
                "Because FakeService is configured to ignore missed configurations.");
        }

        [Fact]
        public async Task When_Disposed_Then_the_FakeServer_releases_its_port()
        {
            Uri baseAddress;

            Func<Task<HttpResponseMessage>> makeRequest;

            using (var fakeService = new FakeHttpService()
                .OnRequest(r => true)
                .RespondWith(async r => { }))
            {
                baseAddress = fakeService.BaseAddress;

                makeRequest = async () => await new HttpClient().GetAsync(baseAddress);

                var firstResponse = await makeRequest();

                firstResponse.EnsureSuccessStatusCode();
            }

            Action makeAnotherRequest = () =>
            {
                makeRequest().Wait();
            };

            makeAnotherRequest.ShouldThrow<HttpRequestException>("Because the port should no longer be available.");
        }

        [Fact]
        public void When_request_condition_met_Then_provides_response()
        {
            const string responseBody = "a quick brown fox";

            var path = new Uri("/some/path", UriKind.Relative);

            using (var fakeService = new FakeHttpService()
                .OnRequest(r => r.Path.ToString() == path.ToString())
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    r.ContentType="text/plain";
                    await r.Body.WriteTextAsUtf8BytesAsync(responseBody);
                }))
            {
                var client = new HttpClient {BaseAddress = fakeService.BaseAddress};

                client.GetStringAsync(path).Result
                    .Should().Be(responseBody);
            }
        }

        [Fact]
        public void When_request_condition_met_Then_provides_response_with_baseAddress()
        {
            var path = new Uri("/some/path", UriKind.Relative);

            using (var fakeService = new FakeHttpService()
                .OnRequest(r => r.Path.ToString() == path.ToString())
                .RespondWith(async (r,s) => await r.Body.WriteTextAsUtf8BytesAsync(s.AbsolutePath)))
            {
                var client = new HttpClient {BaseAddress = fakeService.BaseAddress};

                client.GetStringAsync(path).Result
                    .Should().Be(fakeService.BaseAddress.AbsolutePath);
            }
        }

        [Fact]
        public void When_request_not_expected_Then_responds_404()
        {
            HttpResponseMessage response;

            using (var fakeService = new FakeHttpService())
            {
                var client = new HttpClient {BaseAddress = fakeService.BaseAddress};

                response = client.GetAsync("").Result;
            }

            response.StatusCode.
                Should().Be(System.Net.HttpStatusCode.NotFound, "Because the request was not expected");
        }

        [Fact]
        public void When_request_processing_throws_an_exception_Then_responds_500_with_exception_body()
        {
            const string exceptionMessage = "exception!";

            HttpResponseMessage response;

            using (var fakeService = new FakeHttpService()
                .OnRequest(r => true)
                .RespondWith(r => { throw new Exception(exceptionMessage); }))
            {
                var client = new HttpClient {BaseAddress = fakeService.BaseAddress};

                response = client.GetAsync("").Result;
            }

            response.StatusCode
                .Should().Be(System.Net.HttpStatusCode.InternalServerError,
                    "Because request processing threw an exception");

            response.Content.ReadAsStringAsync().Result
                .Should().Contain(exceptionMessage);
        }
    }
}
