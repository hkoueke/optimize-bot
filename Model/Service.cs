﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeBot.Model
{
    public class Service
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServiceId { get; set; }

        [Required, MinLength(2), MaxLength(10)]
        public string Command { get; init; } = default!;

        [Required]
        public string FrDesc { get; init; } = default!;

        [Required]
        public string EnDesc { get; init; } = default!;

        public bool AdminOnly { get; set; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; }

        //Relations
        public IList<Catalog> Catalogs { get; set; } = new List<Catalog>();

        //Every child service has a parent, /start. This will help show a return button on UI
        public int? ParentId { get; set; }
        public Service? Parent { get; set; }

        public IList<Service> Services { get; set; } = new List<Service>();
    }
}