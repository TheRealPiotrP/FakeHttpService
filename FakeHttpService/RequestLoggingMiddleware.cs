using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Pocket;
using static Pocket.Logger<FakeHttpService.RequestLoggingMiddleware>;

namespace FakeHttpService
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            using (var operation = await LogRequest(context))
            {
                var bodyStream = context.Response.Body;
                var bodyBuffer = new MemoryStream();
                context.Response.Body = bodyBuffer;

                await _next.Invoke(context);

                LogResponse(
                    context,
                    await ReadResponseBody(bodyBuffer),
                    operation);

                bodyBuffer.Seek(0, SeekOrigin.Begin);

                await bodyBuffer.CopyToAsync(bodyStream);
            }
        }

        private static async Task<string> ReadResponseBody(Stream bodyBuffer)
        {
            bodyBuffer.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(bodyBuffer);
            var responseBody = await reader.ReadToEndAsync();
            return responseBody;
        }

        private void LogResponse(HttpContext context, string responseBody, ConfirmationLogger operation)
        {
            var responseHeaders = context.Response.Headers.HeaderSummary();

            var reasonPhrase = context.Features.Get<IHttpResponseFeature>()?.ReasonPhrase ?? "";

            var responseStatusCode = context.Response.StatusCode;

            operation.Succeed(@"
  HTTP/{reasonPhrase} {responseStatusCode}
{responseHeaders}
  {responseBody}",
                              reasonPhrase,
                              responseStatusCode,
                              responseHeaders,
                              responseBody);
        }

        private async Task<ConfirmationLogger> LogRequest(
            HttpContext context)
        {
            var requestBody = "";

            if (context.Request.Body != null)
            {
                var bodyBuffer = new MemoryStream();
                await context.Request.Body.CopyToAsync(bodyBuffer);
                bodyBuffer.Seek(0, SeekOrigin.Begin);
                context.Request.Body = bodyBuffer;
                requestBody = await new StreamReader(bodyBuffer).ReadToEndAsync();
                bodyBuffer.Seek(0, SeekOrigin.Begin);
            }

            var responseHeaders = context.Request.Headers.HeaderSummary();

            var requestMethod = context.Request.Method;

            var displayUrl = context.Request.GetDisplayUrl();

            return new ConfirmationLogger(
                nameof(Invoke),
                Log.Category,
                @"
  {requestMethod} {displayUrl}
{responseHeaders}
  {requestBody}",
                logOnStart: true,
                args: new[] { requestMethod, displayUrl, responseHeaders, requestBody });
        }
    }
}
