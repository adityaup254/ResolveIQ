using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ResolveIQ.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // Navigation property indicating one Category can have many Tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
