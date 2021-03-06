﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AadApiGatekeeper.Controllers
{
    [Route("[Controller]")]
    [Authorize]
    public class LoginController : Controller
    {
        private readonly AadAuthenticationOptions _options;

        public LoginController(IOptions<AadAuthenticationOptions> options)
        {
            _options = options.Value;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            if (_options.UseAadB2c)
                return BadRequest("AAD B2C login not supported");

            return Challenge(new AuthenticationProperties
            {
                RedirectUri = _options.RedirectUri
            }, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
