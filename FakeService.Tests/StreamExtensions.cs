using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FakeService.Tests
{
    public static class StreamExtensions
    {
        public static async Task WriteTextAsUtf8BytesAsync(this Stream subject, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            await subject.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}