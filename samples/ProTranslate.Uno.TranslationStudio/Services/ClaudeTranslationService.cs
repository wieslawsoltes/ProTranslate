using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

#pragma warning disable CA1031
namespace ProTranslate.Uno.TranslationStudio;

/// <summary>
/// Anthropic Claude translation service using the Messages API.
/// </summary>
public sealed class ClaudeTranslationService : IAiTranslationService
{
    private readonly HttpClient _httpClient;
    private AiProviderConfig _config;

    public ClaudeTranslationService(HttpClient httpClient, AiProviderConfig? config = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? new AiProviderConfig
        {
            Provider = AiProvider.Claude,
            ModelId = AiProviderConfig.GetDefaultModel(AiProvider.Claude),
            BaseUrl = AiProviderConfig.GetDefaultBaseUrl(AiProvider.Claude)
        };
    }

    public AiProvider Provider => AiProvider.Claude;
    public string ProviderName => "Claude";
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
        var result = new AiTranslationResult { Provider = AiProvider.Claude, ModelUsed = _config.ModelId };

        if (!IsConfigured)
        {
            result.ErrorMessage = "Claude is not configured. Please provide an API key.";
            return result;
        }

        try
        {
            string systemPrompt = BuildSystemPrompt(request.SourceLanguage, request.TargetLanguage, request.Context);
            string userContent = BuildBatchUserContent(request.Entries);

            var body = new
            {
                model = _config.ModelId,
                max_tokens = _config.MaxTokens,
                system = systemPrompt,
                messages = new object[]
                {
                    new { role = "user", content = userContent }
                }
            };

            string baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
                ? "https://api.anthropic.com"
                : _config.BaseUrl.TrimEnd('/');

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages");
            httpRequest.Headers.Add("x-api-key", _config.ApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"Claude API error {(int)response.StatusCode}: {responseBody}";
                return result;
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("usage", out var usage))
            {
                int input = usage.TryGetProperty("input_tokens", out var i) ? i.GetInt32() : 0;
                int output = usage.TryGetProperty("output_tokens", out var o) ? o.GetInt32() : 0;
                result.TokensUsed = input + output;
            }

            string content = "";
            if (root.TryGetProperty("content", out var contentArray))
            {
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var type) && type.GetString() == "text")
                    {
                        content = block.GetProperty("text").GetString() ?? "";
                        break;
                    }
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
            result.ErrorMessage = $"Claude error: {ex.Message}";
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
            return AiProviderConfig.GetAvailableModels(AiProvider.Claude);
        }

        try
        {
            string baseUrl = string.IsNullOrWhiteSpace(_config.BaseUrl)
                ? "https://api.anthropic.com"
                : _config.BaseUrl.TrimEnd('/');

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/models");
            httpRequest.Headers.Add("x-api-key", _config.ApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return AiProviderConfig.GetAvailableModels(AiProvider.Claude);
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
                        if (!string.IsNullOrEmpty(id) && id.Contains("claude"))
                        {
                            modelsList.Add(id);
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

        return AiProviderConfig.GetAvailableModels(AiProvider.Claude);
    }
}
#pragma warning restore CA1031

