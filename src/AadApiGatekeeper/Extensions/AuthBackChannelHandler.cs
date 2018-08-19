using System.Net.Http;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthBackChannelHandler : HttpClientHandler
    {
        public AuthBackChannelHandler()
        {
            base.AllowAutoRedirect = false;
        }
    }
}
