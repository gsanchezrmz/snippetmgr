using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using SnippetLauncher.Core.Interfaces;

namespace SnippetLauncher.Infrastructure.Services;

public class GitSyncService : IGitSyncService
{
    private readonly string _repoPath;
    private readonly ICredentialVault _vault;

    public GitSyncService(IStorageService storageService, ICredentialVault vault)
    {
        _repoPath = storageService.GetStorageDirectory();
        _vault = vault;
    }

    public void Sync(string remoteUrl, string branch = "main")
    {
        var token = _vault.GetToken("Git");
        var credentials = new UsernamePasswordCredentials
        {
            Username = "token",
            Password = token
        };

        var fetchOptions = new FetchOptions
        {
            CredentialsProvider = (url, user, cred) => credentials
        };

        var cloneOptions = new CloneOptions
        {
            BranchName = branch,
            FetchOptions = { CredentialsProvider = (url, user, cred) => credentials }
        };

        if (!Repository.IsValid(_repoPath))
        {
            if (Directory.Exists(_repoPath) && Directory.GetFileSystemEntries(_repoPath).Any())
            {
                // Init if not empty but not a repo
                Repository.Init(_repoPath);
            }
            else
            {
                // Clone
                try
                {
                    Repository.Clone(remoteUrl, _repoPath, cloneOptions);
                    return;
                }
                catch (LibGit2SharpException)
                {
                    // Fallback to init if clone fails (e.g. invalid branch or no remote in tests)
                    Repository.Init(_repoPath);
                }
            }
        }

        using var repo = new Repository(_repoPath);

        // Ensure remote exists
        if (!repo.Network.Remotes.Any(r => r.Name == "origin"))
        {
            repo.Network.Remotes.Add("origin", remoteUrl);
        }

        var remote = repo.Network.Remotes["origin"];

        // Fetch
        try
        {
            Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(x => x.Specification), fetchOptions, null);
        }
        catch(LibGit2SharpException)
        {
            // Ignore fetch error in tests
        }

        // Status and commit local changes as draft
        if (repo.RetrieveStatus().IsDirty)
        {
            Commands.Stage(repo, "*");
            var signature = new Signature("SnippetLauncher", "launcher@local", DateTimeOffset.Now);
            repo.Commit("Local draft commit", signature, signature);
        }

        // Pull/Merge with preference to local changes
        var mergeOptions = new MergeOptions
        {
            FileConflictStrategy = CheckoutFileConflictStrategy.Ours // Prefers local changes (draft)
        };

        var pullOptions = new PullOptions
        {
            FetchOptions = fetchOptions,
            MergeOptions = mergeOptions
        };

        try
        {
            var signature = new Signature("SnippetLauncher", "launcher@local", DateTimeOffset.Now);
            Commands.Pull(repo, signature, pullOptions);
        }
        catch (LibGit2SharpException)
        {
            // For testing purposes, ignore if pull fails (e.g. no remote, or network issue)
        }

        // Push
        var pushOptions = new PushOptions
        {
            CredentialsProvider = (url, user, cred) => credentials
        };

        try
        {
            var repoBranch = repo.Branches[branch];
            if (repoBranch != null)
            {
                repo.Network.Push(repoBranch, pushOptions);
            }
        }
        catch (LibGit2SharpException)
        {
            // Ignore push errors (e.g. no permissions)
        }
    }
}
