using System.ComponentModel.DataAnnotations;

namespace AzureBot.Configuration
{
    public class LangGraphApiOptions
    {
        public const string SectionName = "LangGraphApi";

        [Required]
        [Url]
        public string BaseUrl { get; set; } = string.Empty;

        [Range(1, 300)]
        public int TimeoutSeconds { get; set; } = 30;

        [Range(0, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        [Range(0, 60)]
        public int RetryDelaySeconds { get; set; } = 1;
    }
}