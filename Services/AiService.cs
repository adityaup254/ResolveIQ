using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ResolveIQ.Services
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GenerateSummaryAsync(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Description cannot be empty for summary generation.", nameof(description));
            }

            var prompt = $"You are an IT support assistant. Summarize this IT support ticket description in one concise sentence (maximum 15 words). Return ONLY the plain text summary without quotes or preamble.\n\nDescription:\n{description}";

            var result = await SendChatCompletionRequestAsync(prompt);
            return result.Trim('"', ' ', '\r', '\n');
        }

        public async Task<string> SuggestCategoryAsync(string title, string description, IEnumerable<string> availableCategories)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Title or description must be provided to suggest a category.");
            }

            var categoryList = availableCategories.ToList();
            if (!categoryList.Any())
            {
                throw new InvalidOperationException("No categories are available for selection.");
            }

            var categoryOptions = string.Join(", ", categoryList);
            var prompt = $"You are an IT helpdesk assistant. Based on the ticket title and description below, select the SINGLE best category strictly from this list: [{categoryOptions}]. Output ONLY the exact category name and nothing else.\n\nTitle: {title}\nDescription: {description}";

            var rawResult = await SendChatCompletionRequestAsync(prompt);
            var cleanedResult = rawResult.Trim('"', ' ', '.', '\r', '\n');

            // Strictly validate returned category against allowed list
            var matchedCategory = categoryList.FirstOrDefault(c => string.Equals(c, cleanedResult, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(matchedCategory))
            {
                // Fallback: Check if response contains any of the categories
                matchedCategory = categoryList.FirstOrDefault(c => cleanedResult.Contains(c, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrEmpty(matchedCategory))
            {
                throw new AiServiceException($"AI suggested '{cleanedResult}', which is not a recognized category. Please select manually.");
            }

            return matchedCategory;
        }

        public async Task<string> SuggestPriorityAsync(string title, string description)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Title or description must be provided to suggest a priority.");
            }

            var allowedPriorities = new List<string> { "Low", "Medium", "High", "Critical" };
            var prompt = $"You are an IT helpdesk assistant. Based on the ticket title and description below, evaluate the urgency and severity, and choose exactly ONE priority strictly from: [Low, Medium, High, Critical]. Output ONLY the chosen priority word and nothing else.\n\nTitle: {title}\nDescription: {description}";

            var rawResult = await SendChatCompletionRequestAsync(prompt);
            var cleanedResult = rawResult.Trim('"', ' ', '.', '\r', '\n');

            // Strictly validate returned priority
            var matchedPriority = allowedPriorities.FirstOrDefault(p => string.Equals(p, cleanedResult, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(matchedPriority))
            {
                matchedPriority = allowedPriorities.FirstOrDefault(p => cleanedResult.Contains(p, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrEmpty(matchedPriority))
            {
                throw new AiServiceException($"AI suggested '{cleanedResult}', which is not a valid priority. Please select manually.");
            }

            return matchedPriority;
        }

        /// <summary>
        /// Sends an HTTP request to the configured AI API (OpenAI/Compatible chat completions endpoint).
        /// Handles missing keys, network errors, timeouts, and non-200 responses gracefully.
        /// </summary>
        private async Task<string> SendChatCompletionRequestAsync(string userPrompt)
        {
            var apiKey = _configuration["AiSettings:ApiKey"];
            var endpoint = _configuration["AiSettings:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            var model = _configuration["AiSettings:Model"] ?? "gpt-4o-mini";

            // Guard: Check if API key is missing or set to placeholder
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("YOUR_AI_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                throw new AiServiceException("AI service is currently unavailable. API key is not configured. You can continue creating the ticket manually.");
            }

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful IT support triage assistant for ResolveIQ. Be concise, direct, and factual." },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2,
                max_tokens = 100
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = jsonContent;

            try
            {
                // 10-second timeout to prevent blocking UI
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _httpClient.SendAsync(request, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    // Fail gracefully on 401, 429, 500 without leaking sensitive information
                    throw new AiServiceException("AI service is currently unavailable. You can continue creating the ticket manually.");
                }

                var responseString = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(responseString);

                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var messageElement) &&
                        messageElement.TryGetProperty("content", out var contentElement))
                    {
                        var content = contentElement.GetString();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            return content;
                        }
                    }
                }

                throw new AiServiceException("AI service returned an empty response. You can continue creating the ticket manually.");
            }
            catch (TaskCanceledException)
            {
                throw new AiServiceException("AI service timed out. You can continue creating the ticket manually.");
            }
            catch (HttpRequestException)
            {
                throw new AiServiceException("AI service network error. You can continue creating the ticket manually.");
            }
            catch (AiServiceException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new AiServiceException("AI service is currently unavailable. You can continue creating the ticket manually.");
            }
        }
    }
}
