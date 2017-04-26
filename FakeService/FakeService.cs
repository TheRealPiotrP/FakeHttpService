using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FakeService
{
    public class MockService : IDisposable
    {
        private string _baseAddress;

        private readonly string _serviceId = Guid.NewGuid().ToString();

        private readonly TestHost _host;

        private readonly List<Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>> _handlers;

        private readonly IList<Expression<Func<HttpRequest, bool>>> _unusedHandlers;

        private readonly bool _ignoreUnusedHandlers;

        private ILogger _logger;

        public MockService(bool ignoreUnusedHandlers = false)
        {
            _handlers = new List<Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>>();
            _unusedHandlers = new List<Expression<Func<HttpRequest, bool>>>();
            _ignoreUnusedHandlers = ignoreUnusedHandlers;

            MockServiceRepository.Register(this);


            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .UseKestrel()
                .UseSetting("applicationName", ServiceId)
                .UseUrls("http://127.0.0.1:0");

            _host = builder.Build();

            _host.Start();

            BaseAddress = _host
                .ServerFeatures.Get<IServerAddressesFeature>()
                .Addresses.First();
        }


        internal MockService Setup(Expression<Func<HttpRequest, bool>> condition, Func<HttpResponse, Task> response)
        {
            _handlers.Add(new Tuple<Expression<Func<HttpRequest, bool>>, Func<HttpResponse, Task>>(condition, response));
            _unusedHandlers.Add(condition);

            _logger.LogInformation(new ConstantMemberEvaluationVisitor().Visit(condition).ToString());

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

        public string BaseAddress
        {
            get
            {
                return _baseAddress;
            }

            set
            {
                if (_baseAddress != null)
                {
                    throw new Exception("Base Address already set");
                }

                _baseAddress = value;
            }
        }

        public string ServiceId => _serviceId;

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

        internal ILogger Logger
        {
            get
            {
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }
    }
}