using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject1
{
    public class Given_I_want_to_get_fake_responses_in_a_test
    {
        private readonly ITestOutputHelper _output;

        public Given_I_want_to_get_fake_responses_in_a_test(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task When_requesting_a_registered_Uri_Then_the_expected_response_is_returned()
        {
            using (var fakeService = new FakeService.FakeService(true)
                .OnRequest(r => true)
                .RespondWith(async r =>
                {
                    r.StatusCode = 200;
                }))
            {
                _output.WriteLine($"BaseAddress: {fakeService.BaseAddress}");

                var response = await new HttpClient().GetAsync(fakeService.BaseAddress);

                response.EnsureSuccessStatusCode();
            }
        }
    }
}
