﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeBot.Model
{
    public class Pricing
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PricingId { get; set; }

        [Required]
        public string Name { get; init; }

        [Required]
        public string Lines { get; init; }

        [Required]
        public string Url { get; init; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; }

        //Navigation
        public Catalog Catalogs { get; set; }
    }
}
