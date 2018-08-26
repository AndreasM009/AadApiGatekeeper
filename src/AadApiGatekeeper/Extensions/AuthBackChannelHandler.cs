using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthBackChannelHandler : HttpClientHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthBackChannelHandler(IHttpContextAccessor httpContextAccessor)
        {
            base.AllowAutoRedirect = false;
            _httpContextAccessor = httpContextAccessor;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {            
            var context = _httpContextAccessor.HttpContext;

            if (null == request.Headers.Authorization)
            {
                var claim = context.User.Claims.FirstOrDefault(c => c.Type.Equals("access_token"));
                if (null != claim)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claim.Value);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
