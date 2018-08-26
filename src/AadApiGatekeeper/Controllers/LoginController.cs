using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AadApiGatekeeper.Controllers
{
    [Route("[Controller]")]
    [Authorize]
    public class LoginController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = "/me"
            }, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
