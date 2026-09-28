using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResolveIQ.Services
{
    /// <summary>
    /// Contract for AI assistance in ResolveIQ.
    /// Provides suggestions only — final actions remain under human control.
    /// </summary>
    public interface IAiService
    {
        /// <summary>
        /// Generates a concise summary from a lengthy ticket description.
        /// </summary>
        Task<string> GenerateSummaryAsync(string description);

        /// <summary>
        /// Analyzes ticket title & description and suggests one category from available categories.
        /// </summary>
        Task<string> SuggestCategoryAsync(string title, string description, IEnumerable<string> availableCategories);

        /// <summary>
        /// Analyzes ticket title & description and suggests a priority (Low, Medium, High, Critical).
        /// </summary>
        Task<string> SuggestPriorityAsync(string title, string description);
    }
}
