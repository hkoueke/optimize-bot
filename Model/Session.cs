using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizeBot.Model
{
#nullable enable
    public class Session
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreationDate { get; }
        public string? Context { get; set; } = default;
        public string? State { get; set; } = default;
        public string? ContextData { get; set; } = default;
    }
}
