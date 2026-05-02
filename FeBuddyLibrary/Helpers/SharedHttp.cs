using System;
using System.Net.Http;

namespace FeBuddyLibrary.Helpers
{
    /// <summary>
    /// Single process-wide HttpClient. Reusing one client avoids socket
    /// exhaustion from the classic "new HttpClient() per call" anti-pattern
    /// and picks up the OS TLS 1.2/1.3 defaults on .NET 6.
    /// </summary>
    public static class SharedHttp
    {
        public static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2),
        };
    }
}
