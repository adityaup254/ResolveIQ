using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ResolveIQ.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property indicating one User can have many Tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
