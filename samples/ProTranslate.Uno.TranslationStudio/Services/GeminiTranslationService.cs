using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

#pragma warning disable CA1031
namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Google Gemini translation service using the generateContent API.
/// </summary>
public sealed class GeminiTranslationService : IAiTranslationService
{
    private readonly HttpClient _httpClient;
    private AiProviderConfig _config;

    public GeminiTranslationService(HttpClient httpClient, AiProviderConfig? config = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? new AiProviderConfig
        {
            Provider = AiProvider.Gemini,
            ModelId = AiProviderConfig.GetDefaultModel(AiProvider.Gemini),
            BaseUrl = AiProviderConfig.GetDefaultBaseUrl(AiProvider.Gemini)
        };
    }

    public AiProvider Provider => AiProvider.Gemini;
    public string ProviderName => "Gemini";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config.ApiKey) && _config.IsEnabled;

    public void Configure(AiProviderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<AiTranslationResult> TranslateAsync(
        AiTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sw = Stopwatch.StartNew();
        var result = new AiTranslationResult { Provider = AiProvider.Gemini, ModelUsed = _config.ModelId };

        if (!IsConfigured)
        {
            result.ErrorMessage = "Gemini is not configured. Please provide an API key.";
            return result;
        }

        try
        {
            string systemPrompt = BuildSystemPrompt(request.SourceLanguage, request.TargetLanguage, request.Context);
            string userContent = BuildBatchUserContent(request.Entries);

            var body = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = userContent } } }
                },
                generationConfig = new
                {
                    temperature = _config.Temperature,
                    maxOutputTokens = _config.MaxTokens
                }
            };

            string baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
                ? "https://generativelanguage.googleapis.com"
                : _config.BaseUrl.TrimEnd('/');

            string url = $"{baseUrl}/v1beta/models/{_config.ModelId}:generateContent?key={_config.ApiKey}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"Gemini API error {(int)response.StatusCode}: {responseBody}";
                return result;
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                int prompt = usage.TryGetProperty("promptTokenCount", out var p) ? p.GetInt32() : 0;
                int candidates = usage.TryGetProperty("candidatesTokenCount", out var c) ? c.GetInt32() : 0;
                result.TokensUsed = prompt + candidates;
            }

            string content = "";
            if (root.TryGetProperty("candidates", out var candidates2) && candidates2.GetArrayLength() > 0)
            {
                var firstCandidate = candidates2[0];
                if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    content = parts[0].GetProperty("text").GetString() ?? "";
                }
            }

            result.Entries = ParseTranslationResponse(content, request.Entries);
            result.IsSuccess = true;
        }
        catch (OperationCanceledException)
        {
            result.ErrorMessage = "Translation was cancelled.";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Gemini error: {ex.Message}";
        }

        sw.Stop();
        result.DurationMs = sw.ElapsedMilliseconds;
        return result;
    }

    public async Task<string> TranslateSingleAsync(
        string sourceText,
        string sourceLanguage,
        string targetLanguage,
        string context = "",
        CancellationToken cancellationToken = default)
    {
        var request = new AiTranslationRequest(
            sourceLanguage,
            targetLanguage,
            [new AiTranslationRequestEntry("single", sourceText)],
            context);

        var result = await TranslateAsync(request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess && result.Entries.Count > 0
            ? result.Entries[0].TranslatedText
            : sourceText;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await TranslateSingleAsync("Hello", "English", "Spanish", "", cancellationToken)
                .ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(result) && result != "Hello";
        }
        catch
        {
            return false;
        }
    }

    private static string BuildSystemPrompt(string source, string target, string context)
    {
        string prompt = "You are a professional translator. Translate text from " + source + " to " + target + ".\n" +
            "IMPORTANT RULES:\n" +
            "1. Preserve ALL placeholders exactly as-is: {0}, {1}, {0:D}, {0:N}, etc.\n" +
            "2. Maintain the same tone and formality level.\n" +
            "3. Return ONLY a JSON array of objects with \"key\" and \"text\" fields.\n" +
            "4. Do not add explanations or markdown formatting.";

        if (!string.IsNullOrWhiteSpace(context))
        {
            prompt += "\nDomain context: " + context;
        }

        return prompt;
    }

    private static string BuildBatchUserContent(IReadOnlyList<AiTranslationRequestEntry> entries)
    {
        var items = entries.Select(e => new { key = e.Key, text = e.SourceText });
        return JsonSerializer.Serialize(items);
    }

    private static List<AiTranslationResultEntry> ParseTranslationResponse(
        string content,
        IReadOnlyList<AiTranslationRequestEntry> originalEntries)
    {
        var results = new List<AiTranslationResultEntry>();

        try
        {
            string json = content.Trim();
            if (json.StartsWith("```"))
            {
                int firstNewline = json.IndexOf('\n');
                int lastFence = json.LastIndexOf("```");
                if (firstNewline > 0 && lastFence > firstNewline)
                {
                    json = json[(firstNewline + 1)..lastFence].Trim();
                }
            }

            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                string key = element.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                string text = element.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                results.Add(new AiTranslationResultEntry(key, text));
            }
        }
        catch
        {
            if (originalEntries.Count == 1)
            {
                results.Add(new AiTranslationResultEntry(originalEntries[0].Key, content.Trim()));
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return AiProviderConfig.GetAvailableModels(AiProvider.Gemini);
        }

        try
        {
            string baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
                ? "https://generativelanguage.googleapis.com"
                : _config.BaseUrl.TrimEnd('/');

            string url = $"{baseUrl}/v1beta/models?key={_config.ApiKey}";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return AiProviderConfig.GetAvailableModels(AiProvider.Gemini);
            }

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
            {
                var modelsList = new List<string>();
                foreach (var item in models.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var nameProp))
                    {
                        string name = nameProp.GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (name.StartsWith("models/"))
                            {
                                name = name.Substring("models/".Length);
                            }

                            string nameLower = name.ToLowerInvariant();
                            if (nameLower.Contains("gemini") &&
                                !nameLower.Contains("embed") &&
                                !nameLower.Contains("aqa") &&
                                !nameLower.Contains("vision") &&
                                !nameLower.Contains("bison"))
                            {
                                modelsList.Add(name);
                            }
                        }
                    }
                }
                if (modelsList.Count > 0)
                {
                    return modelsList.OrderBy(m => m).ToList();
                }
            }
        }
        catch
        {
            // Fallback to defaults
        }

        return AiProviderConfig.GetAvailableModels(AiProvider.Gemini);
    }
}
#pragma warning restore CA1031

