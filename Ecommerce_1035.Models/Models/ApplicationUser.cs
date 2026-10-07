using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ecommerce_1035.Models.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Name { get; set; }
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }
        [EmailAddress]
        public string EmailAddress { get; set; }

        public string PhoneNumber { get; set; }
        public string City { get; set; }

        public string State { get; set; }

        public string PostalCode { get; set; }
        [Display(Name = "Company")]
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]

        public Company Company { get; set; }
        [NotMapped]

        public string Role { get; set; }

    }
}