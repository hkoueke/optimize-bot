using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeBot.Model
{
    public class Provider
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProviderId { get; set; }

        [Required, MinLength(2), MaxLength(20)]
        public string Name { get; init; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; }
        public ProviderConfig Config { get; set; }
#nullable enable
        public string? UtilityProviders { get; set; }
#nullable disable
        //Navigation
        public IList<Catalog> Catalogs { get; set; }
    }
}
