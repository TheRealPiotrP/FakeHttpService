using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace FakeService
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            await WriteRequestSummary(context);

            var bodyStream = context.Response.Body;
            var bodyBuffer = new MemoryStream();
            context.Response.Body = bodyBuffer;

            await _next.Invoke(context);

            var responseBody = await ReadResponseBody(bodyBuffer);

            WriteResponseSummary(context, responseBody);

            bodyBuffer.Seek(0, SeekOrigin.Begin);
            await bodyBuffer.CopyToAsync(bodyStream);
        }

        private static async Task<string> ReadResponseBody(MemoryStream bodyBuffer)
        {
            bodyBuffer.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(bodyBuffer);
            var responseBody = await reader.ReadToEndAsync();
            return responseBody;
        }

        private void WriteResponseSummary(HttpContext context, string responseBody)
        {
            _logger.LogInformation($@"
<<<RESPONSE<<<<<<<<<<<<<<<<<<<<<<<<<<<<
HTTP/{context.Features.Get<IHttpResponseFeature>()?.ReasonPhrase} {(int) context.Response.StatusCode}
{
                    string.Join("",
                        context.Response.Headers.Select(
                            h =>
                                string.Format("{0}: {1}\r\n", h.Key,
                                    string.Join(", ", h.Value.Select(v => v.ToString())))))
                }
{responseBody}
<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
        }

        private async Task WriteRequestSummary(HttpContext context)
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

            _logger.LogInformation($@"
>>>REQUEST>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
{context.Request.Method} {context.Request.GetDisplayUrl()}
{
                    string.Join("",
                        context.Request.Headers.Select(
                            h =>
                                string.Format("{0}: {1}\r\n", h.Key,
                                    string.Join(", ", h.Value.Select(v => v)))))
                }
{requestBody}
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
        }
    }
}
