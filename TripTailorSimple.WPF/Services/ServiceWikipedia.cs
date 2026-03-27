using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TripTailorSimple.WPF.Models;

namespace TripTailorSimple.WPF.Services;

public class ServiceWikipedia
{
    private readonly HttpClient _httpClient;

    public ServiceWikipedia(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(string Description, string ImageUrl)> RecupererResumeAsync(string ville)
    {
        try
        {
            string url = $"https://fr.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(ville)}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return ("", "");

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<WikipediaResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return (
                data?.Extract ?? "",
                data?.Thumbnail?.Source ?? ""
            );
        }
        catch
        {
            return ("", "");
        }
    }

    private class WikipediaResponse
    {
        public string? Extract { get; set; }
        public WikipediaThumbnail? Thumbnail { get; set; }
    }

    private class WikipediaThumbnail
    {
        public string? Source { get; set; }
    }
}