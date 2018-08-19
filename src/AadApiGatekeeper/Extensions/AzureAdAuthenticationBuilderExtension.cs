using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    public static class AzureAdServiceCollectionExtensions
    {
        public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder, Action<AadAuthenticationOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureOptions>();
            builder.AddJwtBearer();
            return builder;
        }

        private class ConfigureAzureOptions : IConfigureNamedOptions<JwtBearerOptions>
        {
            private readonly AadAuthenticationOptions _azureOptions;

            public ConfigureAzureOptions(IOptions<AadAuthenticationOptions> azureOptions)
            {
                _azureOptions = azureOptions.Value;
            }

            public void Configure(string name, JwtBearerOptions options)
            {
                options.Audience = _azureOptions.ClientId;
                options.Authority = $"https://login.microsoftonline.com/{_azureOptions.Tenant}";

                options.Events = new JwtBearerEvents();
                options.Events.OnTokenValidated = context =>
                {
                    // save the token in the user's claim set to access it later
                    var token = context.SecurityToken as JwtSecurityToken;
                    if (null == token)
                        return Task.FromResult(false);

                    var identity = context.Principal.Identity as ClaimsIdentity;
                    identity.AddClaim(new Claim("access_token", token.RawData));
                    return Task.FromResult(true);
                };
            }

            public void Configure(JwtBearerOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}