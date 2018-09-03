using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication
{
    public static class AzureAdServiceCollectionExtensions
    {
        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder, Action<AadAuthenticationOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureOptions>();
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureOpenIdConnecteOptions>();
            builder.AddOpenIdConnect();
            builder.AddJwtBearer();
            return builder;
        }

        #region JwtBearerOptions

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

        #endregion

        #region OpenIdConnectOptions

        private class ConfigureOpenIdConnecteOptions : IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly AadAuthenticationOptions _azureOptions;

            public ConfigureOpenIdConnecteOptions(IOptions<AadAuthenticationOptions> azureOptions)
            {
                _azureOptions = azureOptions.Value;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {                
                options.ClientId = _azureOptions.ClientId;
                options.ClientSecret = _azureOptions.ClientSecret;
                options.Authority = $"https://login.microsoftonline.com/{_azureOptions.Tenant}";
                options.UseTokenLifetime = true;
                options.CallbackPath = "/signin-oidc";
                options.RequireHttpsMetadata = false;
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;

                options.Events = new OpenIdConnectEvents
                {
                    OnAuthorizationCodeReceived = async context =>
                    {
                        // Acquire an Id Token and access_token
                        var user = context.HttpContext.User;
                        string userName = user.FindFirstValue(ClaimTypes.Upn) ?? user.FindFirstValue(ClaimTypes.Email);
                        var clientCredentials = new ClientCredential(_azureOptions.ClientId, _azureOptions.ClientSecret);
                        var authContext = new AuthenticationContext($"https://login.microsoftonline.com/{_azureOptions.Tenant}/");
                        var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(context.ProtocolMessage.Code, new Uri($"{context.Request.Scheme}://{context.Request.Host}/signin-oidc"), clientCredentials);

                        // save the token in the user's claim set to access it later
                        var identity = context.Principal.Identity as ClaimsIdentity;
                        identity.AddClaim(new Claim("access_token", authResult.IdToken));

                        // Notify the OIDC middleware that we already took care of code redemption.
                        context.HandleCodeRedemption(context.ProtocolMessage);
                    }
                };
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }

        #endregion
    }
}