using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FakeService
{
    public class ResponseBuilder
    {
        private readonly FakeService _fakeService;

        private readonly Expression<Func<HttpRequest, bool>> _requestValidator;

        internal ResponseBuilder(FakeService fakeService, Expression<Func<HttpRequest, bool>> requestValidator)
        {
            _requestValidator = requestValidator;

            _fakeService = fakeService;
        }

        public FakeService RespondWith(Func<HttpResponse, Task> responseConfiguration)
        {
            if (responseConfiguration == null) throw new ArgumentNullException("responseConfiguration");

            Func<HttpResponse, Task> responseFunction = async c =>
            {
                await responseConfiguration(c);
            };

            _fakeService.Setup(_requestValidator, responseFunction);

            return _fakeService;
        }

        public FakeService RespondWith(Func<HttpResponse, Uri, Task> responseConfiguration)
        {
            if (responseConfiguration == null) throw new ArgumentNullException("responseConfiguration");

            Func<HttpResponse, Task> responseFunction = async c =>
            {
                await responseConfiguration(c, _fakeService.BaseAddress);
            };

            _fakeService.Setup(_requestValidator, responseFunction);

            return _fakeService;
        }
    }
}