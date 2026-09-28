using System.ComponentModel.DataAnnotations;

namespace ResolveIQ.DTOs
{
    public class AiSummaryRequest
    {
        [Required(ErrorMessage = "Description is required to generate a summary.")]
        public string Description { get; set; } = string.Empty;
    }

    public class AiSuggestionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AiResponseDto
    {
        public bool Success { get; set; }
        public string? Result { get; set; }
        public string? Message { get; set; }
    }
}
