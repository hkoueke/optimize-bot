using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeBot.Model
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        public long TelegramId { get; set; }

        [Required]
        public string FirstName { get; set; }

#nullable enable

        public string? LastName { get; set; }

        public string? Username { get; set; }

        public string? LanguageCode { get; set; }

#nullable disable

        public Session Session { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsBot { get; set; }

        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; }
    }
}