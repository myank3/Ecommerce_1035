using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Ecommerce_1035.Models.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string ISBN { get; set; }
        [Required]
        public string Author { get; set; }


        [Required]
        [Range(1, 1000)]
        public double ListPrice { get; set; }
        [Required]
        [Range(1, 1000)]
        public double Price { get; set; }
        [Required]
        [Range(1, 1000)]
        public double Price50 { get; set; }
        [Required]
        [Range(1, 1000)]

        public double Price100 { get; set; }
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }

        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        public int CoverTypeId { get; set; }
        [ForeignKey("CoverTypeId")]
        public CoverTypes CoverTypes { get; set; }
    }
}
