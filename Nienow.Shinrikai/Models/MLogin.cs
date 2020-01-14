using System.ComponentModel.DataAnnotations;

namespace Nienow.Shinrikai.Models
{
    public class MLogin
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public bool Wrong { get; set; }
    }
}
