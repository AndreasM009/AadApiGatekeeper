﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AuthProxy.Controllers
{
    [Authorize]
    [Route("[Controller]")]
    public class MeController : Controller
    {
        private readonly AadAuthenticationOptions _authOptions;
        private readonly ILogger<MeController> _logger;

        public MeController(IOptions<AadAuthenticationOptions> options, ILogger<MeController> logger)
        {
            _authOptions = options.Value;
            _logger = logger;
        }

        [HttpGet]
        public Dictionary<string, string> GetClaims()
        {
            var result = new Dictionary<string, string>();

            foreach (var c in this.HttpContext.User.Claims)
            {
                if (!c.Type.Equals("access_token") && !result.ContainsKey(c.Type))
                    result.Add(c.Type, c.Value);
            }

            return result;            
        }

        [HttpGet("token/{resource}")]
        public async Task<IActionResult> AcquireToken(string resource)
        {
            if (_authOptions.UseAadB2c)
                return BadRequest("AAD B2C does not support on-behalf-of flow.");
            try
            {
                var claim = this.HttpContext.User.Claims.First(c => c.Type.Equals("access_token"));
                if (null == claim)
                    return Ok(string.Empty);

                // Get the access token
                var token = claim.Value;
                var assertionType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
                
                var user = this.HttpContext.User;
                string userName = user.FindFirstValue(ClaimTypes.Upn) ?? user.FindFirstValue(ClaimTypes.Email);
                var userAssertion = new UserAssertion(token, assertionType, userName);

                var tokenCache = new MemoryTokenCache(userName, resource);
                var authContext = new AuthenticationContext($"https://login.microsoftonline.com/{_authOptions.Tenant}", tokenCache);
                var clientCredential = new ClientCredential(_authOptions.ClientId, _authOptions.ClientSecret);
                var result = await authContext.AcquireTokenAsync(resource, clientCredential, userAssertion);
            
                return Ok(result.AccessToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to acquire token.");
                throw;
            }
        }
    }
}