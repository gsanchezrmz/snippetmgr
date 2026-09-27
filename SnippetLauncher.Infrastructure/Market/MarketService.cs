using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NJsonSchema;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Infrastructure.Market;

public class MarketService : IMarketService
{
    private readonly HttpClient _httpClient;
    private readonly string _schemaJson = @"
    {
      ""$schema"": ""http://json-schema.org/draft-04/schema#"",
      ""type"": ""object"",
      ""properties"": {
        ""Id"": { ""type"": ""string"" },
        ""Title"": { ""type"": ""string"", ""minLength"": 1 },
        ""Description"": { ""type"": ""string"" },
        ""Content"": { ""type"": ""string"", ""minLength"": 1 },
        ""Tags"": { ""type"": ""string"" }
      },
      ""required"": [ ""Id"", ""Title"", ""Content"" ]
    }";

    public MarketService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Snippet> DownloadSnippetAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // Validate JSON Schema
        var schema = await JsonSchema.FromJsonAsync(_schemaJson);
        var errors = schema.Validate(json);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Invalid snippet schema.");
        }

        var snippet = JsonSerializer.Deserialize<Snippet>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (snippet == null)
        {
            throw new InvalidOperationException("Could not deserialize snippet.");
        }

        return snippet;
    }
}
