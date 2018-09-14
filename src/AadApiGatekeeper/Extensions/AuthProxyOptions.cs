namespace Microsoft.AspNetCore.Authentication
{
    public class AuthProxyOptions
    {
        public string Port { get; set; }
        public string ForwardPort { get; set; }
        public string AnonymousPaths { get; set; }
        public string RedirectUri { get; set; } = "/me";
    }
}
