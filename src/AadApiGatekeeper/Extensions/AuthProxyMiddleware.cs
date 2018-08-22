using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication
{
    public static class IAppBuilderExtension
    {
        public static IServiceCollection AddAuthProxy(this IServiceCollection collection, Action<AuthProxyOptions> proxyOptions, Action<AadAuthenticationOptions> authOptions)
        {
            collection.Configure(proxyOptions);

            collection.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddAzureAdBearer(authOptions);

            return collection;
        }

        public static IApplicationBuilder UseAuthProxy(this IApplicationBuilder builder)
        {
            var proxyOptions = builder.ApplicationServices.GetService<IOptions<AuthProxyOptions>>().Value;            

            builder.UseAuthentication();

            builder.UseMiddleware<AuthProxyMiddleware>();

            builder.MapWhen(MustForward, b => b.RunProxy(new ProxyOptions
            {
                Scheme = "http",
                Host = "localhost",
                Port = proxyOptions.ForwardPort,
                BackChannelMessageHandler = new AuthBackChannelHandler(),
            }));

            return builder;
        }

        private static bool MustForward(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(new PathString("/me")))
            {
                return false;
            }

            return true;
        }
    }

    public class AuthProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthProxyOptions _authProxyOptions;
        private readonly List<string> _anonymousPaths;        

        public AuthProxyMiddleware(RequestDelegate next, IOptions<AuthProxyOptions> authProxyOptions)
        {
            _next = next;
            _authProxyOptions = authProxyOptions.Value;

            _anonymousPaths = _authProxyOptions.AnonymousPaths?.Split(',').ToList() ?? new List<string>();
        }

        public async Task Invoke(HttpContext context)
        {
            // Authenticated?
            if (context.User.Identity.IsAuthenticated)
            {
                await _next(context);
            }
            else if (_anonymousPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
            {
                await _next(context);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Unauthorized: 401");
            }
        }

    }
}
