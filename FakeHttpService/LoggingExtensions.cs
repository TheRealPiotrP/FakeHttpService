using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace FakeHttpService
{
    internal static class LoggingExtensions
    {
        internal static string HeaderSummary(this IHeaderDictionary headers) =>
            string.Join(
                Environment.NewLine,
                headers.Select(h =>
                {
                    var headerName = h.Key;

                    var headerValueSummary = string.Join(", ", h.Value.Select(v => v));

                    return $"    {headerName}: {headerValueSummary}";
                }));
    }
}
