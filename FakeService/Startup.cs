using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FakeService
{
    public class Startup
    {
        private readonly FakeService _service;

        public Startup(IHostingEnvironment env)
        {
            _service = FakeServiceRepository.GetServiceMockById(env.ApplicationName);
        }

        public IConfigurationRoot Configuration { get; private set; }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            _service.Logger = loggerFactory.CreateLogger("MockLogger");

            app.UseMiddleware<RequestLoggingMiddleware>()
                .Run(async context =>
                {
                    await _service.Invoke(context);
                });

            app.UseDeveloperExceptionPage();
        }
    }
}
