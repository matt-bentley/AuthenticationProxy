namespace Gateway.Settings
{
    public class IdentityServerSettings
    {
        public string Authority { get; set; }
        public string Audience { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string IdentityProvider { get; set; }
    }
}
