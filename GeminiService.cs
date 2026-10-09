using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Google.GenAI;
using Google.GenAI.Types;

namespace MagicCursor;

public record ModelListResult(bool Success, IReadOnlyList<string> Models, string Error);

// Pre-compiled regex patterns — compiled once at startup, reused across all API calls.
// Avoids the ~1-3ms overhead of re-interpreting patterns on every CleanResponse invocation.

public class GeminiService
{
    private Client? _client;
    private string _textModel = "gemini-3.5-flash-lite";
    private string _imageModel = "gemini-3.6-flash";

    // Verified: 2026-10-09 (ai.google.dev: flash models have free tier with generateContent and multimodal input)
    private static readonly Regex FreeTierIncludePattern = new(@"flash", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExcludePattern = new(@"tts|image|imagen|veo|embedding|live|audio|aqa|robotics|computer-use|learnlm", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    // Concise system prompt — avoids listing rules the model might echo back
    private const string SystemInstruction = 
        "You are Magic Cursor, a desktop AI tooltip. " +
        "Reply with ONLY the final answer — no reasoning, no restating the question, no preamble. " +
        "Be extremely concise (under 80 words). Use **bold** and • bullets for structure. " +
        "Never use think/thought tags or show your internal reasoning process.";

    public GeminiService(string apiKey)
    {
        UpdateApiKey(apiKey);
    }

    public void UpdateApiKey(string apiKey)
    {
        _client = !string.IsNullOrWhiteSpace(apiKey) ? new Client(apiKey: apiKey) : null;
    }

    public void UpdateModels(string textModel, string imageModel)
    {
        if (!string.IsNullOrWhiteSpace(textModel)) _textModel = textModel.Trim();
        if (!string.IsNullOrWhiteSpace(imageModel)) _imageModel = imageModel.Trim();
    }

    public bool IsInitialized => _client != null;

    public async Task<string> AnalyzeTextAsync(string input, byte[]? imageBytes = null, bool treatAsImage = false)
    {
        if (_client == null)
        {
            return "⚠ AI Error: Gemini API key is not configured. Please open Settings in the action menu to enter a valid key.";
        }

        try
        {
            var contents = new List<Content>();
            string modelToUse = _textModel;

            if (treatAsImage && imageBytes != null)
            {
                modelToUse = _imageModel;
                contents.Add(new Content
                {
                    Role = "user",
                    Parts = new List<Part>
                    {
                        new Part
                        {
                            InlineData = new Blob
                            {
                                Data = imageBytes,
                                MimeType = "image/png"
                            }
                        },
                        new Part { Text = input }
                    }
                });
            }
            else
            {
                contents.Add(new Content
                {
                    Role = "user",
                    Parts = new List<Part> { new Part { Text = input } }
                });
            }

            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Parts = new List<Part> { new Part { Text = SystemInstruction } }
                }
            };

            var response = await _client.Models.GenerateContentAsync(modelToUse, contents, config);

            // Primary: extract only non-thought parts from the response
            string rawText = ExtractCleanText(response);

            // Secondary: regex cleanup for any remaining artifacts
            return CleanResponse(rawText);
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            if (msg.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                msg += " (This model may not be available on the free tier — choose another in Settings.)";
            }
            return $"⚠ AI Error: {msg}";
        }
    }

    /// <summary>
    /// Extracts only non-thought text from the model response.
    /// Gemma thinking models tag internal reasoning in Part.Thought = true.
    /// By filtering these out, we get only the final answer.
    /// </summary>
    private static string ExtractCleanText(GenerateContentResponse response)
    {
        try
        {
            var candidates = response.Candidates;
            if (candidates != null && candidates.Count > 0)
            {
                var parts = candidates[0].Content?.Parts;
                if (parts != null && parts.Count > 0)
                {
                    // Filter out parts that are marked as thoughts/reasoning
                    var textParts = parts
                        .Where(p => p.Thought != true && !string.IsNullOrEmpty(p.Text))
                        .Select(p => p.Text);
                    
                    string result = string.Join("", textParts);
                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }
            }
        }
        catch { /* Fall through to .Text fallback */ }

        return response.Text ?? "No response from AI.";
    }

    /// <summary>
    /// Multi-pass cleanup to strip any remaining reasoning artifacts from the text.
    /// Catches: XML think tags, prompt echo-back, reasoning phrases, thinking blocks.
    /// </summary>
    // Pre-compiled regexes — compiled once, zero overhead per call
    private static readonly Regex RxThinkPaired   = new(@"<\s*(think|thought)\s*>[\s\S]*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxThinkOpen     = new(@"<\s*(think|thought)\s*>[\s\S]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxThinkClose    = new(@"<\s*/\s*(think|thought)\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxDetails       = new(@"<\s*details\s*>[\s\S]*?<\s*/\s*details\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxPromptEcho    = new(@"^[\*\-•\s]*(?:Context|Task|Constraint\s*\d*|Input|Output\s*requirements?|Instructions?)\s*\d*\s*[:.].*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex RxReasoning     = new(@"^[\*\-•\s]*(?:Since\s+(?:the|this|it|we|I)|However[,\s]|Given\s+(?:the|that|this)|Actually[,\s]|The\s+most\s+logical|I\s+(?:will|need|should|can)\s|Let\s+me|Note\s+that|Noting\s+that|So[,\s]+I|First[,\s]+I|they\s+either\s+want).*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex RxQuotedThink   = new(@"^>\s*(Thought|Thinking|Internal):.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex RxBoldThink     = new(@"^\*{0,2}(Thinking|Thought|Internal[\s]?monologue)\*{0,2}:.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex RxExcessiveNl   = new(@"\n{3,}", RegexOptions.Compiled);

    private static string CleanResponse(string raw)
    {
        string text = raw;

        // --- Pass 1: Strip XML-style thinking tags ---
        text = RxThinkPaired.Replace(text, "");
        text = RxThinkOpen.Replace(text, "");
        text = RxThinkClose.Replace(text, "");
        text = RxDetails.Replace(text, "");

        // --- Pass 2: Strip prompt echo-back ---
        text = RxPromptEcho.Replace(text, "");

        // --- Pass 3: Strip reasoning/meta-commentary lines ---
        text = RxReasoning.Replace(text, "");

        // --- Pass 4: Strip markdown-format thinking blocks ---
        text = RxQuotedThink.Replace(text, "");
        text = RxBoldThink.Replace(text, "");

        // --- Pass 5: Clean up excessive blank lines ---
        text = RxExcessiveNl.Replace(text, "\n\n");
        text = text.Trim();

        return string.IsNullOrWhiteSpace(text) ? raw.Trim() : text;
    }

    public static async Task<ModelListResult> ListFreeTierModelsAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ModelListResult(false, Array.Empty<string>(), "API key is required.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://generativelanguage.googleapis.com/v1beta/models?pageSize=1000"
            );
            request.Headers.Add("x-goog-api-key", apiKey);

            using var response = await HttpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string statusMsg = $"API error {(int)response.StatusCode}: {response.ReasonPhrase}";
                Log.Error($"Models: refresh failed ({statusMsg})");
                return new ModelListResult(false, Array.Empty<string>(), statusMsg);
            }

            string json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var list = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var modelsElem) && modelsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in modelsElem.EnumerateArray())
                {
                    string name = m.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (name.StartsWith("models/"))
                    {
                        name = name.Substring(7);
                    }

                    bool canGenerate = false;
                    if (m.TryGetProperty("supportedGenerationMethods", out var methods) && methods.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var method in methods.EnumerateArray())
                        {
                            if (method.GetString() == "generateContent")
                            {
                                canGenerate = true;
                                break;
                            }
                        }
                    }

                    if (canGenerate && FreeTierIncludePattern.IsMatch(name) && !ExcludePattern.IsMatch(name))
                    {
                        list.Add(name);
                    }
                }
            }

            var deduped = list.Distinct(StringComparer.OrdinalIgnoreCase)
                              .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)
                              .ToList();

            Log.Info($"Models: refresh ok count={deduped.Count}");
            return new ModelListResult(true, deduped, string.Empty);
        }
        catch (Exception ex)
        {
            Log.Error($"Models: refresh failed ({ex.GetType().Name}: {ex.Message})", ex);
            return new ModelListResult(false, Array.Empty<string>(), ex.Message);
        }
    }
}
