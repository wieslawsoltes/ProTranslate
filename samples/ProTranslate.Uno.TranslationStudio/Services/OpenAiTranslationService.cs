using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

#pragma warning disable CA1031
namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// OpenAI GPT-based translation service using the Chat Completions API.
/// </summary>
public sealed class OpenAiTranslationService : IAiTranslationService
{
    private readonly HttpClient _httpClient;
    private AiProviderConfig _config;

    public OpenAiTranslationService(HttpClient httpClient, AiProviderConfig? config = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? new AiProviderConfig
        {
            Provider = AiProvider.OpenAI,
            ModelId = AiProviderConfig.GetDefaultModel(AiProvider.OpenAI),
            BaseUrl = AiProviderConfig.GetDefaultBaseUrl(AiProvider.OpenAI)
        };
    }

    public AiProvider Provider => AiProvider.OpenAI;
    public string ProviderName => "OpenAI";
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
        var result = new AiTranslationResult { Provider = AiProvider.OpenAI, ModelUsed = _config.ModelId };

        if (!IsConfigured)
        {
            result.ErrorMessage = "OpenAI is not configured. Please provide an API key.";
            return result;
        }

        try
        {
            string systemPrompt = BuildSystemPrompt(request.SourceLanguage, request.TargetLanguage, request.Context);
            string userContent = BuildBatchUserContent(request.Entries);

            var body = new
            {
                model = _config.ModelId,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userContent }
                },
                temperature = _config.Temperature,
                max_tokens = _config.MaxTokens
            };

            string baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
                ? "https://api.openai.com"
                : _config.BaseUrl.TrimEnd('/');

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"OpenAI API error {(int)response.StatusCode}: {responseBody}";
                return result;
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("usage", out var usage))
            {
                result.TokensUsed = usage.GetProperty("total_tokens").GetInt32();
            }

            string content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            result.Entries = ParseTranslationResponse(content, request.Entries);
            result.IsSuccess = true;
        }
        catch (OperationCanceledException)
        {
            result.ErrorMessage = "Translation was cancelled.";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"OpenAI error: {ex.Message}";
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
            // Strip markdown code fences if present
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
            // If JSON parsing fails, try to use the raw content for single translations
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
            return AiProviderConfig.GetAvailableModels(AiProvider.OpenAI);
        }

        try
        {
            string baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
                ? "https://api.openai.com"
                : _config.BaseUrl.TrimEnd('/');

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/models");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return AiProviderConfig.GetAvailableModels(AiProvider.OpenAI);
            }

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                var modelsList = new List<string>();
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idProp))
                    {
                        string id = idProp.GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(id))
                        {
                            string idLower = id.ToLowerInvariant();
                            if ((idLower.Contains("gpt") || idLower.Contains("o1") || idLower.Contains("o3") || idLower.Contains("o4")) &&
                                !idLower.Contains("embed") &&
                                !idLower.Contains("audio") &&
                                !idLower.Contains("dall-e") &&
                                !idLower.Contains("tts") &&
                                !idLower.Contains("whisper") &&
                                !idLower.Contains("moderation") &&
                                !idLower.Contains("realtime"))
                            {
                                modelsList.Add(id);
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

        return AiProviderConfig.GetAvailableModels(AiProvider.OpenAI);
    }
}
#pragma warning restore CA1031

