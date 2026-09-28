using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Infrastructure.Repositories;

public class SQLiteSnippetRepository : ISnippetRepository
{
    private readonly string _dbPath = string.Empty;
    private readonly string _connectionString;

    public SQLiteSnippetRepository(IStorageService storageService)
    {
        _dbPath = storageService.GetStorageDirectory();

        // Handling memory db for tests
        if (_dbPath.Contains("Mode=Memory") || _dbPath.Contains(":memory:"))
        {
            _connectionString = _dbPath;
        }
        else
        {
            storageService.EnsureStorageDirectoryExists();
            _connectionString = $"Data Source={Path.Combine(_dbPath, "snippets.db")}";
        }

        InitializeDatabase();
    }

    public SQLiteSnippetRepository(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Snippets (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Code TEXT NOT NULL,
                Language TEXT,
                Tags TEXT,
                SourcePath TEXT,
                UNIQUE(Title, Code)
            );
        ";
        command.ExecuteNonQuery();
    }

    public async Task<IEnumerable<Snippet>> GetAllSnippetsAsync()
    {
        var snippets = new List<Snippet>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Code, Language, Tags, SourcePath FROM Snippets";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            snippets.Add(new Snippet
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Code = reader.GetString(2),
                Language = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Tags = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                SourcePath = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
            });
        }

        return snippets;
    }

    public async Task SaveSnippetAsync(Snippet snippet)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Snippets (Id, Title, Code, Language, Tags, SourcePath)
            VALUES (@Id, @Title, @Code, @Language, @Tags, @SourcePath)
            ON CONFLICT(Title, Code) DO UPDATE SET
                Tags = @Tags,
                Language = @Language,
                SourcePath = @SourcePath;
        ";
        command.Parameters.AddWithValue("@Id", string.IsNullOrEmpty(snippet.Id) ? Guid.NewGuid().ToString() : snippet.Id);
        command.Parameters.AddWithValue("@Title", snippet.Title ?? "");
        command.Parameters.AddWithValue("@Code", snippet.Code ?? "");
        command.Parameters.AddWithValue("@Language", snippet.Language ?? "");
        command.Parameters.AddWithValue("@Tags", snippet.Tags ?? "");
        command.Parameters.AddWithValue("@SourcePath", snippet.SourcePath ?? "");

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteSnippetAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Snippets WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Snippet>> SearchSnippetsAsync(string query)
    {
        var allSnippets = await GetAllSnippetsAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            return allSnippets;
        }

        var results = allSnippets.Select(s => new
        {
            Snippet = s,
            Score = FuzzySharp.Fuzz.PartialRatio(query.ToLowerInvariant(), $"{s.Title} {s.Tags}".ToLowerInvariant())
        })
        .Where(x => x.Score > 50)
        .OrderByDescending(x => x.Score)
        .Select(x => x.Snippet);

        return results;
    }
}
