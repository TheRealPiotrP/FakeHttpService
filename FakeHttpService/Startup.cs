using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace FakeHttpService
{
    public class Startup
    {
        private readonly FakeHttpService _httpService;

        public Startup(IHostingEnvironment env)
        {
            _httpService = FakeHttpServiceRepository.GetServiceMockById(env.ApplicationName);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<RequestLoggingMiddleware>()
               .Run(async context => await _httpService.Invoke(context));

            app.UseDeveloperExceptionPage();
        }
    }
}