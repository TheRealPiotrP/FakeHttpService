using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
                ret = _filters[0];
                for (var i = 1; i < _filters.Count; i++)
                {
                    var param = Expression.Parameter(typeof(HttpRequest), "param");
                    var first = ret;
                    var second = _filters[i];
                    var combined = Expression.AndAlso(
                        first.Body.Replace(first.Parameters[0], param),
                        second.Body.Replace(second.Parameters[0], param));
                    ret = Expression.Lambda<Func<HttpRequest, bool>>(combined, param);
                }
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

        private static string GetBody(HttpRequest request)
        {
            using (var sr = new StreamReader(request.Body))
            {
                var bodyString
                    = sr.ReadToEnd();

                return bodyString;
            }
        }
    }
}