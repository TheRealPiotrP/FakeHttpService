using System;

namespace FakeHttpService.Tests
{
    internal static class StringExtensions
    {
        public static string NormalizeLineEndings(this string value) =>
            value
                .Replace("\r\n", "\n")
                .Replace("\n", Environment.NewLine);
    }
}
