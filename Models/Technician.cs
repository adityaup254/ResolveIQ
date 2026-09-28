using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ResolveIQ.Models
{
    public class Technician
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property indicating one Technician can have many Tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
