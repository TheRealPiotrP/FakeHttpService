using System;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace FakeHttpService
{
    public static class HttpRequestExtensions 
    { 
        public static Uri GetUri(this HttpRequest subject) 
        { 
            if (null == subject) 
            { 
                throw new ArgumentNullException(nameof(subject)); 
            } 
 
            if (string.IsNullOrWhiteSpace(subject.Scheme)) 
            { 
                throw new ArgumentException("Http request Scheme is not specified"); 
            } 
 
            if (false == subject.Host.HasValue) 
            { 
                throw new ArgumentException("Http request Host is not specified"); 
            } 
 
            var builder = new StringBuilder(); 
 
            builder.Append(subject.Scheme) 
                .Append("://") 
                .Append(subject.Host); 
 
            if (subject.Path.HasValue) 
            { 
                builder.Append(subject.Path.Value); 
            } 
 
            if (subject.QueryString.HasValue) 
            { 
                builder.Append(subject.QueryString); 
            } 
 
            return new Uri(builder.ToString()); 
        } 
        
    }
}