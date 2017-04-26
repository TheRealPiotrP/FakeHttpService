using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestProject1
{
    public class Given_I_want_to_get_fake_responses_in_a_test
    {
        [Fact]
        public async Task When_requesting_a_registered_Uri_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeService.FakeService()
                .OnRequest(r => r.Path.ToString() == path)
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                    r.ContentType = "text/plain";
                    await r.Body.WriteTextAsUtf8BytesAsync(response);
                }))
            {
                var response = await new HttpClient().GetAsync(fakeService.BaseUri);

                response.EnsureSuccessStatusCode();
            }
        }
    }
}
