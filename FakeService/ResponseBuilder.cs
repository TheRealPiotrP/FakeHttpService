using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FakeService
{
    public class ResponseBuilder
    {
        private readonly MockService _mockService;

        private readonly Expression<Func<HttpRequest, bool>> _requestValidator;

        internal ResponseBuilder(MockService mockService, Expression<Func<HttpRequest, bool>> requestValidator)
        {
            _requestValidator = requestValidator;

            _mockService = mockService;
        }

        public MockService RespondWith(Func<HttpResponse, Task> responseConfiguration)
        {
            if (responseConfiguration == null) throw new ArgumentNullException("responseConfiguration");

            Func<HttpResponse, Task> responseFunction = async c =>
            {
                await responseConfiguration(c);
            };

            _mockService.Setup(_requestValidator, responseFunction);

            return _mockService;
        }

        public MockService RespondWith(Func<HttpResponse, string, Task> responseConfiguration)
        {
            if (responseConfiguration == null) throw new ArgumentNullException("responseConfiguration");

            Func<HttpResponse, Task> responseFunction = async c =>
            {
                await responseConfiguration(c, _mockService.BaseAddress);
            };

            _mockService.Setup(_requestValidator, responseFunction);

            return _mockService;
        }
    }
}