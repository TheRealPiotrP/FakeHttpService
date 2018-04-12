using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pocket;
using Xunit;
using Xunit.Abstractions;
using FakeHttpService.FilterBuilders;

namespace FakeHttpService.Tests
{
    public class Given_I_want_to_get_fake_responses_in_a_test : IDisposable
    {
        public class SamplePOCO
        {
            public SamplePOCO(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
        private readonly IDisposable _disposables;

        public Given_I_want_to_get_fake_responses_in_a_test(ITestOutputHelper output)
        {
            _disposables = LogEvents.Subscribe(e => output.WriteLine(e.ToLogString()));
        }

        public void Dispose() => _disposables.Dispose();

        [Fact]
        public async Task When_requesting_a_registered_Uri_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest(r => true)
                .RespondWith(async r => { await Task.Yield(); }))
            {
                var response = await new HttpClient().GetAsync(fakeService.BaseAddress);

                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task When_filtering_Uri_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest()
                .WhereUri(uri => uri.ToString().EndsWith("customapicall"))
                .Then()
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    await Task.Yield();
                }))
            {
                var response = await new HttpClient().GetAsync(new Uri(fakeService.BaseAddress, "/customapicall"));

                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task When_filtering_method_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest()
                .WhereMehtod(method => method == "POST")
                .Then()
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    await Task.Yield();
                })
                .FailOnUnexpectedRequest())
            {
                var response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent("nothing"));

                response.EnsureSuccessStatusCode();

                response = await new HttpClient().GetAsync(new Uri(fakeService.BaseAddress, "/customapicall"));

                response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task When_filtering_body_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest()
                .WhereBodyAsString(body => body == "nothing")
                .Then()
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    await Task.Yield();
                })
                .FailOnUnexpectedRequest())
            {
                var response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent("nothing"));

                response.EnsureSuccessStatusCode();

                response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent("different"));

                response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task When_filtering_body_as_json_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest()
                .WhereBodyAsJson(body => JToken.DeepEquals(body, JToken.Parse("{ field: 1}")))
                .Then()
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    await Task.Yield();
                })
                .FailOnUnexpectedRequest())
            {
                var response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent("{ field: 1}"));

                response.EnsureSuccessStatusCode();

                response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent("{}"));

                response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task When_filtering_body_as_POCO_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest()
                .WhereBodyAs<SamplePOCO>(obj => obj.Value == "defined")
                .Then()
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    await Task.Yield();
                })
                .FailOnUnexpectedRequest())
            {
                var response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent(JsonConvert.SerializeObject(new SamplePOCO("defined"))));

                response.EnsureSuccessStatusCode();

                response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent(JsonConvert.SerializeObject(new SamplePOCO("undefined"))));

                response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task When_filtering_on_body_and_uri_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeHttpService()
                .OnRequest()
                .WhereUri(uri => uri.ToString().EndsWith("api1"))
                .WhereBodyAsJson(body => JToken.DeepEquals(body, JToken.Parse("{ field: 1}")))
                .Then()
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    await Task.Yield();
                })
                .FailOnUnexpectedRequest())
            {
                var response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/api1"), new StringContent("{ field: 1}"));

                response.EnsureSuccessStatusCode();

                response = await new HttpClient().PostAsync(new Uri(fakeService.BaseAddress, "/customapicall"), new StringContent("{ field: 1}"));

                response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
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
                    r.Headers.Append("colors", new StringValues(new[] { "red", "green", "blue" }));
                    await Task.Yield();
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
                    await r.Body.WriteTextAsUtf8BytesAsync("foo bar zap");
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
                    .RespondWith(async r => { await Task.Yield(); }))
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
                .StartWith($"{nameof(FakeHttpService)} \"my service\" @ {address} expected requests");
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
                        await Task.Yield();
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
                .RespondWith(async r => { await Task.Yield(); }))
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
                    r.ContentType = "text/plain";
                    await r.Body.WriteTextAsUtf8BytesAsync(responseBody);
                }))
            {
                var client = new HttpClient { BaseAddress = fakeService.BaseAddress };

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
                .RespondWith(async (r, s) => await r.Body.WriteTextAsUtf8BytesAsync(s.AbsolutePath)))
            {
                var client = new HttpClient { BaseAddress = fakeService.BaseAddress };

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
                var client = new HttpClient { BaseAddress = fakeService.BaseAddress };

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
                .RespondWith(r => throw new Exception(exceptionMessage)))
            {
                var client = new HttpClient { BaseAddress = fakeService.BaseAddress };

                response = client.GetAsync("").Result;
            }

            response.StatusCode
                .Should().Be(System.Net.HttpStatusCode.InternalServerError,
                    "Because request processing threw an exception");

            response.Content.ReadAsStringAsync().Result
                .Should().Contain(exceptionMessage);
        }

        [Fact]
        public async Task Request_information_is_sent_to_PocketLogger()
        {
            var log = new List<string>();

            using (var fakeService = new FakeHttpService()
                .OnRequest(r => true)
                .RespondWith(async r =>
                {
                    r.Headers.Add("a-response-header", "a-response-header-value");
                    await r.WriteAsync(JsonConvert.SerializeObject(new { ResponseProperty = "response-property-value" }));
                }))
            using (LogEvents.Subscribe(e => log.Add(e.ToLogString())))
            {
                await new HttpClient().PostAsync(
                    new Uri(fakeService.BaseAddress, "/and/the/path?and=query"),
                    new StringContent(JsonConvert.SerializeObject(new { RequestProperty = "request-property-value" }), Encoding.UTF8, "application/json"));
            }

            log.Should()
               .HaveCount(2);

            log[0]
                .NormalizeLineEndings()
                .Should()
                .Match(@"*[FakeHttpService.RequestLoggingMiddleware] [Invoke]  ▶ 
  POST http://127.0.0.1:*/and/the/path?and=query
    Connection: Keep-Alive
    Content-Type: application/json; charset=utf-8
    Host: 127.0.0.1:*
    Content-Length: *
  {""RequestProperty"":""request-property-value""}*".NormalizeLineEndings());

            log[1]
                .NormalizeLineEndings()
                .Should()
                .Match(@"*[FakeHttpService.RequestLoggingMiddleware] [Invoke]  ⏹ -> ✔ (*ms) 
  HTTP/ 200
    a-response-header: a-response-header-value
  {""ResponseProperty"":""response-property-value""}*".NormalizeLineEndings());
        }
    }
}
