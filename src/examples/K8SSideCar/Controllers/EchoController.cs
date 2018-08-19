using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace K8SSideCar
{
    [Authorize]
    [Route("api/[Controller]")]
    public class EchoController : Controller
    {
        [HttpGet("headers")]
        public string EchoHeaders()
        {
            // collect all http headers and return a string
            var builder = new StringBuilder(Environment.NewLine);

            foreach (var claim in this.HttpContext.User.Claims)
            {
                builder.AppendLine($"{claim.Type} {claim.Value} {claim.ValueType}");
            }            

            foreach (var header in this.HttpContext.Request.Headers)
            { 
                builder.AppendLine($"{header.Key}:{header.Value}");
            }

            builder.AppendLine(this.HttpContext.Request.Path.ToString());
            builder.AppendLine(this.HttpContext.Request.Host.ToString());
            return builder.ToString();
        }

        [HttpGet("claims")]
        public async Task<Dictionary<string, string>> GetClaims()
        {
            // get bearer token of current request
            var token = new StringValues();
            if (!Request.Headers.TryGetValue("Authorization", out token))
                return null;

            // the token
            var bearerToken = token.First().Replace("Bearer ", "");

            // create a http GET request and set authorization header
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var proxyPort = Environment.GetEnvironmentVariable("Api__ProxyPort");
            var result = await httpClient.GetStringAsync($"http://localhost:{proxyPort}/me");

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
        }
    }
}