using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SDP1.Models;

namespace SDP1.Services
{
    public class GeminiAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAIService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public GeminiAIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiAIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public async Task<AIRecommendationResultViewModel> GetRecommendationsAsync(
            string userQuery,
            DisabilityUser? userProfile,
            List<CandidateOpportunity> candidates)
        {
            var result = new AIRecommendationResultViewModel
            {
                Query = userQuery,
                TotalMatchesFound = candidates.Count
            };

            if (userProfile != null)
            {
                var profileParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(userProfile.DisabilityType))
                    profileParts.Add($"Disability: {userProfile.DisabilityType}");
                if (!string.IsNullOrWhiteSpace(userProfile.Skills))
                    profileParts.Add($"Skills: {userProfile.Skills}");
                if (!string.IsNullOrWhiteSpace(userProfile.Education))
                    profileParts.Add($"Education: {userProfile.Education}");
                if (!string.IsNullOrWhiteSpace(userProfile.Interests))
                    profileParts.Add($"Interests: {userProfile.Interests}");
                if (!string.IsNullOrWhiteSpace(userProfile.Location))
                    profileParts.Add($"Location: {userProfile.Location}");

                result.DisabilityContext = string.Join(" • ", profileParts);
            }

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogInformation("Gemini API key is not configured. Using database keyword-matching recommendation.");
                return GenerateFallbackRecommendations(userQuery, userProfile, candidates,
                    notice: "Showing direct matching opportunities from the AbilityConnect BD platform database (Gemini API key not configured).");
            }

            var model = _configuration["Gemini:Model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gemini-2.5-flash";
            }

            try
            {
                var candidatesPayload = candidates.Select(c => new
                {
                    id = c.Id,
                    type = c.Type,
                    title = c.Title,
                    provider = c.ProviderOrOrg,
                    category = c.CategoryOrField,
                    location = c.Location,
                    description = c.Description.Length > 200 ? c.Description.Substring(0, 200) + "..." : c.Description,
                    details = c.Details,
                    url = c.Url
                }).ToList();

                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("You are the AI Recommendation Assistant for AbilityConnect BD, an inclusive platform in Bangladesh for persons with disabilities.");
                promptBuilder.AppendLine("Your goal is to recommend the best matching real opportunities and services currently in the database to help this user.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"USER QUESTION: \"{userQuery}\"");

                if (!string.IsNullOrWhiteSpace(result.DisabilityContext))
                {
                    promptBuilder.AppendLine($"USER PROFILE CONTEXT: {result.DisabilityContext}");
                }

                promptBuilder.AppendLine();
                promptBuilder.AppendLine("AVAILABLE PLATFORM RECORDS (JSON):");
                promptBuilder.AppendLine(JsonSerializer.Serialize(candidatesPayload));
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("CRITICAL SAFETY AND ACCURACY RULES:");
                promptBuilder.AppendLine("1. You must ONLY recommend items that exist in the AVAILABLE PLATFORM RECORDS list above.");
                promptBuilder.AppendLine("2. DO NOT invent or hallucinate any job, scholarship, doctor, organization, or training program.");
                promptBuilder.AppendLine("3. If no record in the list matches the user's request, explain clearly in 'summary' that no matching opportunity was found in current platform data, and leave 'recommendations' array empty.");
                promptBuilder.AppendLine("4. For each recommended item, include the exact 'itemId', 'itemType', 'title', 'organizationOrProvider', 'url', and write a concise 1-2 sentence 'reason' explaining why it fits.");
                promptBuilder.AppendLine("5. Output MUST be valid JSON adhering strictly to this schema:");
                promptBuilder.AppendLine("{");
                promptBuilder.AppendLine("  \"summary\": \"Short personalized overview explaining which opportunities suit the user's request and profile\",");
                promptBuilder.AppendLine("  \"recommendations\": [");
                promptBuilder.AppendLine("    {");
                promptBuilder.AppendLine("      \"itemId\": 123,");
                promptBuilder.AppendLine("      \"itemType\": \"Job | Training Program | Scholarship | Healthcare Provider | Learning Hub | Awareness Event\",");
                promptBuilder.AppendLine("      \"title\": \"Title\",");
                promptBuilder.AppendLine("      \"organizationOrProvider\": \"Org Name\",");
                promptBuilder.AppendLine("      \"reason\": \"Why this fits user query and profile\",");
                promptBuilder.AppendLine("      \"url\": \"/Jobs/Details/123\",");
                promptBuilder.AppendLine("      \"location\": \"Location\",");
                promptBuilder.AppendLine("      \"extraDetails\": \"Deadline/Salary/Duration if any\"");
                promptBuilder.AppendLine("    }");
                promptBuilder.AppendLine("  ]");
                promptBuilder.AppendLine("}");

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = promptBuilder.ToString() }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        responseMimeType = "application/json"
                    }
                };

                var jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini API call failed with status code {StatusCode}: {Error}", response.StatusCode, errorDetails);
                    return GenerateFallbackRecommendations(userQuery, userProfile, candidates,
                        notice: "AI service is currently busy. Displaying direct matching opportunities from AbilityConnect BD.");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiApiRawResponse>(responseString, _jsonOptions);

                var generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    _logger.LogWarning("Gemini returned empty text response.");
                    return GenerateFallbackRecommendations(userQuery, userProfile, candidates);
                }

                generatedText = CleanJsonString(generatedText);

                var parsedResponse = JsonSerializer.Deserialize<GeminiParsedRecommendation>(generatedText, _jsonOptions);

                if (parsedResponse == null)
                {
                    return GenerateFallbackRecommendations(userQuery, userProfile, candidates);
                }

                result.Summary = parsedResponse.Summary ?? "Here are recommended opportunities matching your needs:";
                result.IsAiGenerated = true;

                if (parsedResponse.Recommendations != null && parsedResponse.Recommendations.Any())
                {
                    foreach (var rec in parsedResponse.Recommendations)
                    {
                        var matchingCandidate = candidates.FirstOrDefault(c => c.Id == rec.ItemId && c.Type.Equals(rec.ItemType, StringComparison.OrdinalIgnoreCase))
                                              ?? candidates.FirstOrDefault(c => c.Title.Equals(rec.Title, StringComparison.OrdinalIgnoreCase));

                        if (matchingCandidate != null)
                        {
                            result.Recommendations.Add(new AIRecommendedItemViewModel
                            {
                                ItemId = matchingCandidate.Id,
                                ItemType = matchingCandidate.Type,
                                Title = matchingCandidate.Title,
                                OrganizationOrProvider = matchingCandidate.ProviderOrOrg,
                                Reason = !string.IsNullOrWhiteSpace(rec.Reason) ? rec.Reason : $"Matches your search for {userQuery}",
                                Url = matchingCandidate.Url,
                                BadgeClass = matchingCandidate.BadgeClass,
                                IconClass = matchingCandidate.IconClass,
                                Location = matchingCandidate.Location,
                                ExtraDetails = matchingCandidate.Details
                            });
                        }
                    }
                }

                if (result.Recommendations.Count == 0 && candidates.Count > 0)
                {
                    
                    if (string.IsNullOrWhiteSpace(result.Summary) || result.Summary.Length < 10)
                    {
                        result.Summary = "No direct matching opportunities were found for your specific query in current platform data.";
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Gemini API");
                return GenerateFallbackRecommendations(userQuery, userProfile, candidates,
                    notice: "AI service is temporarily unavailable. Displaying direct database matches from AbilityConnect BD.");
            }
        }

        private static string CleanJsonString(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```json"))
            {
                trimmed = trimmed.Substring(7);
            }
            else if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed.Substring(3);
            }

            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 3);
            }

            return trimmed.Trim();
        }

        private AIRecommendationResultViewModel GenerateFallbackRecommendations(
            string userQuery,
            DisabilityUser? userProfile,
            List<CandidateOpportunity> candidates,
            string? notice = null)
        {
            var result = new AIRecommendationResultViewModel
            {
                Query = userQuery,
                IsAiGenerated = false,
                NoticeMessage = notice,
                TotalMatchesFound = candidates.Count
            };

            if (userProfile != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(userProfile.DisabilityType)) parts.Add($"Disability: {userProfile.DisabilityType}");
                if (!string.IsNullOrWhiteSpace(userProfile.Skills)) parts.Add($"Skills: {userProfile.Skills}");
                if (!string.IsNullOrWhiteSpace(userProfile.Location)) parts.Add($"Location: {userProfile.Location}");
                result.DisabilityContext = string.Join(" • ", parts);
            }

            if (candidates.Count == 0)
            {
                result.Summary = "No matching opportunities or services were found in the current platform database for your query. Please try searching with different keywords or check back soon!";
                return result;
            }

            var queryLower = userQuery.ToLower();
            var keywords = queryLower.Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '/', '\\', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Where(k => k.Length > 2)
                                     .ToList();

            var generalWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "recommend", "recommendation", "recommendations", "suggest", "suggestion",
                "opportunity", "opportunities", "available", "suitable", "eligible", "help", "support",
                "what", "which", "where", "how", "give", "show", "find", "for", "with",
                "want", "need", "like", "best", "some", "any", "please", "can", "could", "would",
                "looking", "about", "there", "good"
            };
            var substantiveKeywords = keywords.Where(k => !generalWords.Contains(k)).ToList();

            var scoredCandidates = candidates.Select(c =>
            {
                int keywordScore = 0;
                var text = (c.Title + " " + c.Description + " " + c.CategoryOrField + " " + c.Details).ToLower();

                foreach (var kw in substantiveKeywords)
                {
                    if (c.Title.ToLower().Contains(kw)) keywordScore += 5;
                    else if (text.Contains(kw)) keywordScore += 2;
                }

                int profileScore = 0;
                if (userProfile != null)
                {
                    if (!string.IsNullOrWhiteSpace(userProfile.DisabilityType) && text.Contains(userProfile.DisabilityType.ToLower()))
                        profileScore += 3;
                    if (!string.IsNullOrWhiteSpace(userProfile.Location) && (c.Location ?? "").ToLower().Contains(userProfile.Location.ToLower()))
                        profileScore += 2;
                }

                return new { Candidate = c, KeywordScore = keywordScore, TotalScore = keywordScore + profileScore };
            }).ToList();

            if (substantiveKeywords.Any() && !scoredCandidates.Any(x => x.KeywordScore > 0))
            {
                result.Summary = "No matching opportunities or services were found in the current platform database for your query. Please try searching with different keywords or check back soon!";
                return result;
            }

            var topCandidates = scoredCandidates
                .Where(x => !substantiveKeywords.Any() || x.KeywordScore > 0)
                .OrderByDescending(x => x.TotalScore)
                .Take(6)
                .ToList();

            if (!topCandidates.Any())
            {
                result.Summary = "No matching opportunities or services were found in the current platform database for your query. Please try searching with different keywords or check back soon!";
                return result;
            }

            result.Summary = $"Found {topCandidates.Count} opportunity options related to your search. Here are the most relevant recommendations for you:";

            foreach (var item in topCandidates)
            {
                var c = item.Candidate;
                string reason;

                if (c.Type == "Job")
                    reason = $"Accessible job opportunity from {c.ProviderOrOrg} matching your career interests.";
                else if (c.Type == "Training Program")
                    reason = $"Skill development course by {c.ProviderOrOrg} tailored to build practical employment abilities.";
                else if (c.Type == "Scholarship")
                    reason = $"Financial support opportunity funded by {c.ProviderOrOrg} for educational advancement.";
                else if (c.Type == "Healthcare Provider")
                    reason = $"Specialized medical or therapy care provided by {c.ProviderOrOrg}.";
                else if (c.Type == "Learning Hub")
                    reason = $"Free accessible video lesson to empower your independent skills.";
                else
                    reason = $"Relevant community program from {c.ProviderOrOrg}.";

                result.Recommendations.Add(new AIRecommendedItemViewModel
                {
                    ItemId = c.Id,
                    ItemType = c.Type,
                    Title = c.Title,
                    OrganizationOrProvider = c.ProviderOrOrg,
                    Reason = reason,
                    Url = c.Url,
                    BadgeClass = c.BadgeClass,
                    IconClass = c.IconClass,
                    Location = c.Location,
                    ExtraDetails = c.Details
                });
            }

            return result;
        }

        private class GeminiApiRawResponse
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidate>? Candidates { get; set; }
        }

        private class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent? Content { get; set; }
        }

        private class GeminiContent
        {
            [JsonPropertyName("parts")]
            public List<GeminiPart>? Parts { get; set; }
        }

        private class GeminiPart
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class GeminiParsedRecommendation
        {
            [JsonPropertyName("summary")]
            public string? Summary { get; set; }

            [JsonPropertyName("recommendations")]
            public List<GeminiParsedItem>? Recommendations { get; set; }
        }

        private class GeminiParsedItem
        {
            [JsonPropertyName("itemId")]
            public int ItemId { get; set; }

            [JsonPropertyName("itemType")]
            public string? ItemType { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("organizationOrProvider")]
            public string? OrganizationOrProvider { get; set; }

            [JsonPropertyName("reason")]
            public string? Reason { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("location")]
            public string? Location { get; set; }

            [JsonPropertyName("extraDetails")]
            public string? ExtraDetails { get; set; }
        }
    }
}
