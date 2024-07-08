namespace AuthentikProxy.Models
{
    public class AuthentikClientSettings
    {
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string Slug { get; set; }
    }
    public class AuthentikSettings
    {
        public required string Url { get; set; }
        public required List<AuthentikClientSettings> Clients { get; set; }
    }
}
