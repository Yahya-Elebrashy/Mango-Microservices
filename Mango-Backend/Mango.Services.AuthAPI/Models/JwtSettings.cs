using System.ComponentModel.DataAnnotations;

namespace Mango.Services.AuthAPI.Models
{
    public class JwtSettings
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }

    }
}
