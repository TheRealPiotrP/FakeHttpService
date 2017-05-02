using System.Net.Http;
using FluentAssertions;
using Xunit;

namespace FakeHttpService.Tests
{
    public class FakeServiceWebServerExtensionsTests 
    { 
        [Fact] 
        public async void When_specifying_content_for_a_relative_Uri_Then_that_content_is_returned() 
        { 
            using (var fakeService = new FakeHttpService() 
                .WithContentAt("/foo/bar/zap.zip", "quick brown fox")) 
            { 
                var response = await new HttpClient().GetAsync(fakeService.BaseAddress + "/foo/bar/zap.zip");
 
                var body = await response.Content.ReadAsStringAsync(); 
 
                body.Should().Be("quick brown fox"); 
            } 
        } 
    }
}