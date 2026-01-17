using System;
using System.Collections.Generic;

namespace AI.Models
{
    // Main class for API /models response
    public class OpenRouterModelsResponse
    {
        public List<OpenRouterModelInfo> Data { get; set; }
    }

    // Model information
    public class OpenRouterModelInfo
    {
        public string Id { get; set; }
        public string CanonicalSlug { get; set; }
        public string HuggingFaceId { get; set; }
        public string Name { get; set; }
        public long Created { get; set; }
        public string Description { get; set; }
        public int ContextLength { get; set; }
        public Architecture Architecture { get; set; }
        public Pricing Pricing { get; set; }
        public TopProvider TopProvider { get; set; }
        public List<string> SupportedParameters { get; set; }
    }

    // Model architecture
    public class Architecture
    {
        public string Modality { get; set; }
        public List<string> InputModalities { get; set; }
        public List<string> OutputModalities { get; set; }
        public string Tokenizer { get; set; }
        public string InstructType { get; set; }
    }

    // Pricing
    public class Pricing
    {
        public decimal Prompt { get; set; }
        public decimal Completion { get; set; }
        public decimal Request { get; set; }
        public decimal Image { get; set; }
        public decimal WebSearch { get; set; }
        public decimal InternalReasoning { get; set; }

        public bool IsFree => Prompt == 0 && Completion == 0;

        public string GetPricingInfo()
        {
            if (IsFree)
                return "Free";

            return $"Input: ${Prompt}/1K tokens, Output: ${Completion}/1K tokens";
        }
    }

    // Main provider
    public class TopProvider
    {
        public int ContextLength { get; set; }
        public int MaxCompletionTokens { get; set; }
        public bool IsModerated { get; set; }
    }
}