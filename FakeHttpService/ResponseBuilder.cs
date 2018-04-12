using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace FakeHttpService
{
    public class RequestFilterExpressionBuilder
    {
        private readonly FakeHttpService _fakeHttpService;
        private readonly List<Expression<Func<HttpRequest, bool>>> _filters = new List<Expression<Func<HttpRequest, bool>>>();
        public RequestFilterExpressionBuilder(FakeHttpService fakeHttpService)
        {
            _fakeHttpService = fakeHttpService;
        }

        public ResponseBuilder Then()
        {
           var condition = ComputeCondition();
            return new ResponseBuilder(_fakeHttpService, condition);
        }

        private Expression<Func<HttpRequest, bool>> ComputeCondition()
        {
            Expression<Func<HttpRequest, bool>> ret = _ => true;
            if (_filters.Count > 0)
            {
                ret = Combine(_filters[0], _filters.Skip(1).ToList());
            }
            return ret;
        }

        public RequestFilterExpressionBuilder WhereUri(Expression<Func<Uri, bool>> condition)
        {
            Expression<Func<HttpRequest, Uri>> extractor = request => request.GetUri();
            var combined = extractor.Compose(condition);
            _filters.Add(combined);
            return this;
        }

        public RequestFilterExpressionBuilder WhereMehtod(Expression<Func<string, bool>> condition)
        {
            Expression<Func<HttpRequest, string>> extractor = request => request.Method;
            var combined = extractor.Compose(condition);
            _filters.Add(combined);
            return this;
        }

        public RequestFilterExpressionBuilder WhereBodyAsString(Expression<Func<string, bool>> condition)
        {
            Expression<Func<HttpRequest, string>> extractor = request => GetBody(request);
            var combined = extractor.Compose(condition);
            _filters.Add(combined);
            return this;
        }

        public RequestFilterExpressionBuilder WhereBodyAsJson(Expression<Func<JToken, bool>> condition)
        {
            Expression<Func<HttpRequest, JToken>> extractor = request => JToken.Parse(GetBody(request));
            var combined = extractor.Compose(condition);
            _filters.Add(combined);
            return this;
        }

        private string GetBody(HttpRequest request)
        {
            using (var sr = new StreamReader(request.Body))
            {
                var bodyString
                    = sr.ReadToEnd();

                return bodyString;
            }
        }

        private Expression<Func<HttpRequest, bool>> Combine(Expression<Func<HttpRequest, bool>> expression, IReadOnlyList<Expression<Func<HttpRequest, bool>>> reminder)
        {
            if (reminder.Count == 0)
            {
                return expression;
            }

            var body = Expression.And(expression.Body, Combine(reminder[0], reminder.Skip(1).ToList()));
            return Expression.Lambda<Func<HttpRequest, bool>>(body, expression.Parameters[0]);
        }
    }

    public class ResponseBuilder
    {
        private readonly FakeHttpService _fakeHttpService;

        private readonly Expression<Func<HttpRequest, bool>> _requestValidator;

        internal ResponseBuilder(FakeHttpService fakeHttpService, Expression<Func<HttpRequest, bool>> requestValidator)
        {
            _requestValidator = requestValidator;

            _fakeHttpService = fakeHttpService;
        }

        public FakeHttpService RespondWith(Func<HttpResponse, Task> responseConfiguration)
        {
            if (responseConfiguration == null) throw new ArgumentNullException(nameof(responseConfiguration));

            async Task ResponseFunction(HttpResponse c)
            {
                await responseConfiguration(c);
            }

            _fakeHttpService.Setup(_requestValidator, ResponseFunction);

            return _fakeHttpService;
        }

        public FakeHttpService RespondWith(Func<HttpResponse, Uri, Task> responseConfiguration)
        {
            if (responseConfiguration == null) throw new ArgumentNullException(nameof(responseConfiguration));

            async Task ResponseFunction(HttpResponse c)
            {
                await responseConfiguration(c, _fakeHttpService.BaseAddress);
            }

            _fakeHttpService.Setup(_requestValidator, ResponseFunction);

            return _fakeHttpService;
        }
    }
}