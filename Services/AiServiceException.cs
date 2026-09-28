using System;

namespace ResolveIQ.Services
{
    /// <summary>
    /// Custom exception for AI service operations.
    /// Captures descriptive error messages to show users when AI is unavailable or fails gracefully.
    /// </summary>
    public class AiServiceException : Exception
    {
        public AiServiceException(string message) : base(message)
        {
        }

        public AiServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
