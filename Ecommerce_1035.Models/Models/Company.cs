using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ecommerce_1035.Models.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string StreetAddress { get; set; }

        public string EmailAddress { get; set; }

        public string City { get; set; }

        public string State { get; set; }
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }


        [Display(Name = "Is Authorized Company")]
        public bool IsAuthorizedCompany { get; set; }
    }
}
