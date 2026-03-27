using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TripTailorSimple.WPF.Models;

namespace TripTailorSimple.WPF.Services;

public class GitHubAiService
{
    private readonly HttpClient _http;
    private readonly string _token;

    public GitHubAiService(HttpClient httpClient)
    {
        _http = httpClient;

        _token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_token))
        {
            throw new InvalidOperationException(
                "Le token GitHub est introuvable. Ajoute la variable d'environnement GITHUB_TOKEN.");
        }

        _http.BaseAddress = new Uri("https://models.inference.ai.azure.com/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<string> ChatAsync(IEnumerable<ChatMessage> messages)
    {
        var payload = new
        {
            model = "gpt-4o-mini",
            messages = messages,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Erreur API ({(int)response.StatusCode}) : {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);

        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? "(réponse vide)";
    }
}