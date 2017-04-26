using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace FakeService
{
    public class FakeService : IDisposable
    {
        private Uri _baseAddress;

        private readonly IWebHost _host;

        private readonly List<Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>> _handlers;

        private readonly IList<Expression<Func<HttpRequest, bool>>> _unusedHandlers;

        private readonly bool _ignoreUnusedHandlers;

        public FakeService(bool ignoreUnusedHandlers = false)
        {
            _handlers = new List<Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>>();
            _unusedHandlers = new List<Expression<Func<HttpRequest, bool>>>();
            _ignoreUnusedHandlers = ignoreUnusedHandlers;

            MockServiceRepository.Register(this);


            var config = new ConfigurationBuilder().Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .UseStartup<Startup>()
                .UseSetting("applicationName", ServiceId)
                .UseUrls("http://127.0.0.1:0");

            _host = builder.Build();

            _host.Start();

            var sf = _host.ServerFeatures;
            var f = sf.Get<IServerAddressesFeature>();
            var @as = f.Addresses;
            var a = @as.First();

            BaseAddress = new Uri(_host
                .ServerFeatures.Get<IServerAddressesFeature>()
                .Addresses.First());
        }


        internal FakeService Setup(Expression<Func<HttpRequest, bool>> condition, Func<HttpResponse, Task> response)
        {
            _handlers.Add(new Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>(condition, response));
            _unusedHandlers.Add(condition);

            Logger.LogInformation(new ConstantMemberEvaluationVisitor().Visit(condition).ToString());

            return this;
        }

        public ResponseBuilder OnRequest(Expression<Func<HttpRequest, bool>> condition)
        {
            return new ResponseBuilder(this, condition);
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                foreach (var handler in _handlers)
                {
                    if (!handler.Item1.Compile().Invoke(context.Request))
                    {
                        continue;
                    }

                    _unusedHandlers.Remove(handler.Item1);

                    await handler.Item2(context.Response);

                    return;
                }

                context.Response.StatusCode = 404;

                Debug.WriteLine("No handler for request\n\r{0} {1}", context.Request.Method, context.Request.Path);
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;

                await context.Response.WriteAsync(JObject.FromObject(e).ToString());
            }
        }

        public Uri BaseAddress
        {
            get => _baseAddress;

            set
            {
                if (_baseAddress != null)
                {
                    throw new Exception("Base Address already set");
                }

                _baseAddress = value;
            }
        }

        public string ServiceId { get; } = Guid.NewGuid().ToString();

        public void Dispose()
        {
            _host.Dispose();

            MockServiceRepository.Unregister(this);

            if (!_ignoreUnusedHandlers && _unusedHandlers.Any())
                throw new InvalidOperationException(
                    String.Format("Mock Server {0} expected requests \r\n\r\n {1} \r\n\r\n but they were not made.",
                        BaseAddress,
                        _unusedHandlers.Select(h => new ConstantMemberEvaluationVisitor().Visit(h).ToString())
                            .Aggregate((c, n) => c + "\r\n" + n)));
        }

        internal ILogger Logger { get; set; }
    }
}