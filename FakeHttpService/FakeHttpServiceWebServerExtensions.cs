namespace FakeHttpService
{
    public static class FakeHttpServiceWebServerExtensions
    { 
        public static FakeHttpService WithContentAt(this FakeHttpService subject, string relativeUri, string content) 
        { 
            subject 
                .OnRequest(r => r.GetUri().ToString().EndsWith(relativeUri)) 
                .RespondWith(async r => 
                { 
                    await r.Body.WriteTextAsUtf8BytesAsync(content); 
                }); 
 
            return subject; 
        } 
    }
}