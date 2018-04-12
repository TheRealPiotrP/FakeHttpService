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
using Pocket;
using static Pocket.Logger<FakeHttpService.FakeHttpService>;

namespace FakeHttpService
{
    public class FakeHttpService : IDisposable
    {
        private readonly bool _serviceIdIsUserSpecified;
        private readonly IWebHost _host;

        private readonly List<Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>> _handlers;

        private readonly IList<Expression<Func<HttpRequest, bool>>> _unusedHandlers;

        private readonly bool _throwOnUnusedHandlers;

        public FakeHttpService(
            string serviceId = null,
            bool throwOnUnusedHandlers = false)
        {
            _handlers = new List<Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>>();
            _unusedHandlers = new List<Expression<Func<HttpRequest, bool>>>();
            _throwOnUnusedHandlers = throwOnUnusedHandlers;
            ServiceId = serviceId ?? Guid.NewGuid().ToString();

            _serviceIdIsUserSpecified = serviceId != null;

            FakeHttpServiceRepository.Register(this);

            var config = new ConfigurationBuilder().Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                 .UseKestrel()
                 .UseStartup<Startup>()
                 .UseSetting("applicationName", ServiceId)
                 .UseUrls("http://127.0.0.1:0");

            _host = builder.Build();

            _host.Start();

            BaseAddress = new Uri(_host
                                      .ServerFeatures.Get<IServerAddressesFeature>()
                                       .Addresses.First());
        }

        internal FakeHttpService Setup(Expression<Func<HttpRequest, bool>> condition, Func<HttpResponse, Task> response)
        {
            _handlers.Add(new Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>(condition, response));
            _unusedHandlers.Add(condition);

            Log.Info("Setting up condition {condition}",
                     new ConstantMemberEvaluationVisitor().Visit(condition));

            return this;
        }

        public ResponseBuilder OnRequest(Expression<Func<HttpRequest, bool>> condition)
        {
            return new ResponseBuilder(this, condition);
        }

        public FakeHttpService FailOnUnexpectedRequest()
        {
            var rb = new ResponseBuilder(this, _ => true);
                return rb.RespondWith(async r =>
            {
                r.StatusCode = 500;
                await Task.Yield();
            });
        }

        public RequestFilterExpressionBuilder OnRequest()
        {
            return new RequestFilterExpressionBuilder(this);
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

                Debug.WriteLine($"No handler for request{Environment.NewLine}{context.Request.Method} {context.Request.Path}");
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;

                await context.Response.WriteAsync(e.ToString());
            }
        }

        public Uri BaseAddress { get; }

        public string ServiceId { get; }

        public void Dispose()
        {
            Task.Run(() => _host.Dispose()).Wait();

            FakeHttpServiceRepository.Unregister(this);

            var shouldThrowForMissingRequests = _throwOnUnusedHandlers && _unusedHandlers.Any();

            if (shouldThrowForMissingRequests)
            {
                var unusedHandlerSummary = _unusedHandlers
                    .Select(h => new ConstantMemberEvaluationVisitor().Visit(h).ToString())
                    .Aggregate((c, n) => $"{c}{Environment.NewLine}{n}");

                var exception = new InvalidOperationException(
                    $@"{GetType().Name} {ToString()} expected requests
{unusedHandlerSummary}
but they were not made.");

                throw exception;
            }
        }

        public override string ToString() =>
            _serviceIdIsUserSpecified ? $"\"{ServiceId}\" @ {BaseAddress}" : $"@ {BaseAddress}";
    }
}