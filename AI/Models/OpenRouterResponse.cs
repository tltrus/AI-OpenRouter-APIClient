using System;

namespace AI.Models
{
    public class OpenRouterResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class OpenRouterModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsFree { get; set; }
        public string Provider { get; set; }
        public string ContextLength { get; set; }

        // Новые поля для интеграции с OpenRouter API
        public decimal PromptPrice { get; set; }
        public decimal CompletionPrice { get; set; }
        public string PricingInfo { get; set; }
        public string ModelType { get; set; }
        public string Tokenizer { get; set; }

        // Метод для создания из OpenRouterModelInfo
        public static OpenRouterModel FromApiModel(OpenRouterModelInfo apiModel)
        {
            if (apiModel == null) return null;

            // Извлекаем провайдера из имени
            var provider = apiModel.Name.Contains(":")
                ? apiModel.Name.Split(':')[0].Trim()
                : "Unknown";

            return new OpenRouterModel
            {
                Id = apiModel.Id,
                Name = apiModel.Name,
                Description = apiModel.Description ?? "No description available",
                IsFree = apiModel.Pricing?.IsFree ?? false,
                Provider = provider,
                ContextLength = apiModel.ContextLength.ToString("N0") + " tokens",
                PromptPrice = apiModel.Pricing?.Prompt ?? 0,
                CompletionPrice = apiModel.Pricing?.Completion ?? 0,
                PricingInfo = apiModel.Pricing?.GetPricingInfo() ?? "Not specified",
                ModelType = apiModel.Architecture?.Modality ?? "Unknown",
                Tokenizer = apiModel.Architecture?.Tokenizer ?? "Unknown"
            };
        }
    }

    // класс для хранения сообщения в истории
    public class ChatMessage
    {
        public string Role { get; set; } // "user" или "assistant"
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }

    // класс для настроек диалога
    public class ChatSettings
    {
        public bool EnableContextMemory { get; set; } = true;
        public int MaxContextMessages { get; set; } = 10; // Максимальное количество сообщений в контексте
        public bool ShowHistory { get; set; } = true;
    }
}