using System;
using System.ComponentModel.DataAnnotations;

namespace ResolveIQ.DTOs
{
    /// <summary>
    /// Data Transfer Object returned to clients when viewing tickets.
    /// Prevents circular JSON references and provides clean, flat data.
    /// </summary>
    public class TicketDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? ResolutionNote { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public int? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
    }

    /// <summary>
    /// DTO received from clients when creating a new ticket.
    /// Server handles CreatedDate and initial Status = Open.
    /// </summary>
    public class CreateTicketDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priority is required.")]
        public string Priority { get; set; } = "Low";

        [Required(ErrorMessage = "CategoryId is required.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "UserId is required.")]
        public int UserId { get; set; }
    }

    /// <summary>
    /// DTO received from clients when updating an existing ticket.
    /// </summary>
    public class UpdateTicketDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priority is required.")]
        public string Priority { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;

        [Required(ErrorMessage = "CategoryId is required.")]
        public int CategoryId { get; set; }

        public int? TechnicianId { get; set; }

        public string? ResolutionNote { get; set; }
    }
}
