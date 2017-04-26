using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FakeHttpService
{
    public class Startup
    {
        private readonly FakeHttpService _httpService;

        public Startup(IHostingEnvironment env)
        {
            _httpService = FakeHttpServiceRepository.GetServiceMockById(env.ApplicationName);
        }

        public IConfigurationRoot Configuration { get; private set; }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            _httpService.Logger = loggerFactory.CreateLogger("MockLogger");

            app.UseMiddleware<RequestLoggingMiddleware>()
                .Run(async context =>
                {
                    await _httpService.Invoke(context);
                });

            app.UseDeveloperExceptionPage();
        }
    }
}
