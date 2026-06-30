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

namespace AgriSmart.Web.Services
{
    public class GeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<GeminiChatService> _logger;

        // Per-user conversation history (scoped service = per SignalR circuit)
        private readonly List<GeminiMessage> _conversationHistory = new();

        private const string SystemPrompt = @"You are ""AgriSmart Dost"" — a friendly, knowledgeable Pakistani agricultural advisor chatbot built into the AgriSmart farming app.

Your role:
- Help farmers in Pakistan with crop advice, pest management, fertilizer recommendations, irrigation tips, weather guidance, and soil health.
- You know about Pakistani crops: wheat (gandum/گندم), cotton (kapaas/کپاس), rice (chawal/چاول), sugarcane (ganna/گنا), maize (makki/مکئی), potato (aaloo/آلو), tomato (tamatar/ٹماٹر), onion (pyaaz/پیاز), chili (mirch/مرچ), mango (aam/آم), citrus (malta/مالٹا), and more.
- You understand Pakistani seasons (Kharif: April-October, Rabi: October-March), provinces (Punjab, Sindh, KPK, Balochistan, AJK, GB, ICT), and local farming practices.

Rules:
1. If the user writes in Urdu or Roman Urdu, reply in Urdu script.
2. If the user writes in English, reply in English.
3. Keep answers concise and practical (2-4 short paragraphs max).
4. Be actionable — give specific advice (fertilizer amounts per acre, sowing dates, spray names).
5. Reference Pakistani agricultural context and local units (acre, maund, bag).
6. If asked about non-agriculture topics, politely say you specialize in farming and redirect.
7. Use a warm, respectful tone — address farmers as you would in Pakistani culture.
8. When greeting, say Assalam-o-Alaikum.";

        public GeminiChatService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiChatService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"] ?? "";
            _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        }

        /// <summary>
        /// Send a user message and get an AI response. Maintains conversation context.
        /// </summary>
        public async Task<string> SendMessageAsync(string userMessage, bool isUrdu)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                return isUrdu
                    ? "ابھی AI سروس دستیاب نہیں ہے۔ براہ کرم بعد میں دوبارہ کوشش کریں۔"
                    : "AI service is not configured yet. Please contact the administrator to set up the Gemini API key.";
            }

            try
            {
                // Add user message to history
                _conversationHistory.Add(new GeminiMessage
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = userMessage } }
                });

                // Build request body
                var requestBody = new GeminiRequest
                {
                    SystemInstruction = new GeminiContent
                    {
                        Parts = new List<GeminiPart> { new() { Text = SystemPrompt } }
                    },
                    Contents = _conversationHistory,
                    GenerationConfig = new GeminiGenerationConfig
                    {
                        Temperature = 0.7,
                        MaxOutputTokens = 1024,
                        TopP = 0.95
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonBody = JsonSerializer.Serialize(requestBody, jsonOptions);
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, errorBody);

                    // Remove the failed user message from history
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1);

                    if ((int)response.StatusCode == 429)
                    {
                        return isUrdu
                            ? "بہت زیادہ درخواستیں بھیجی گئیں۔ براہ کرم ایک منٹ بعد دوبارہ کوشش کریں۔"
                            : "Too many requests. Please wait a minute and try again.";
                    }

                    return isUrdu
                        ? "معذرت، AI سے جواب حاصل کرنے میں مسئلہ ہوا۔ براہ کرم دوبارہ کوشش کریں۔"
                        : "Sorry, there was an issue getting a response from AI. Please try again.";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var aiText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1);
                    return isUrdu
                        ? "AI سے خالی جواب آیا۔ براہ کرم اپنا سوال دوبارہ لکھیں۔"
                        : "Received an empty response from AI. Please rephrase your question.";
                }

                // Add AI response to conversation history for context
                _conversationHistory.Add(new GeminiMessage
                {
                    Role = "model",
                    Parts = new List<GeminiPart> { new() { Text = aiText } }
                });

                // Keep conversation history manageable (last 20 messages)
                if (_conversationHistory.Count > 20)
                {
                    _conversationHistory.RemoveRange(0, _conversationHistory.Count - 20);
                }

                return aiText.Trim();
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Gemini API request timed out");
                if (_conversationHistory.Count > 0)
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1);

                return isUrdu
                    ? "جواب حاصل کرنے میں وقت لگ رہا ہے۔ براہ کرم دوبارہ کوشش کریں۔"
                    : "The request timed out. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                if (_conversationHistory.Count > 0)
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1);

                return isUrdu
                    ? "معذرت، ایک غیر متوقع مسئلہ آیا۔ براہ کرم دوبارہ کوشش کریں۔"
                    : "Sorry, an unexpected error occurred. Please try again.";
            }
        }

        /// <summary>
        /// Clear conversation history (e.g., when chatbot is re-opened).
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();
        }

        // ── Gemini API JSON Models ─────────────────────────────────

        private class GeminiRequest
        {
            [JsonPropertyName("system_instruction")]
            public GeminiContent SystemInstruction { get; set; }

            public List<GeminiMessage> Contents { get; set; }

            [JsonPropertyName("generationConfig")]
            public GeminiGenerationConfig GenerationConfig { get; set; }
        }

        private class GeminiContent
        {
            public List<GeminiPart> Parts { get; set; }
        }

        private class GeminiMessage
        {
            public string Role { get; set; }
            public List<GeminiPart> Parts { get; set; }
        }

        private class GeminiPart
        {
            public string Text { get; set; }
        }

        private class GeminiGenerationConfig
        {
            public double Temperature { get; set; }

            [JsonPropertyName("maxOutputTokens")]
            public int MaxOutputTokens { get; set; }

            [JsonPropertyName("topP")]
            public double TopP { get; set; }
        }

        private class GeminiResponse
        {
            public List<GeminiCandidate> Candidates { get; set; }
        }

        private class GeminiCandidate
        {
            public GeminiCandidateContent Content { get; set; }
        }

        private class GeminiCandidateContent
        {
            public List<GeminiPart> Parts { get; set; }
        }
    }
}
