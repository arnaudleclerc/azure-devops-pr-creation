using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureSamplesPRCreation
{
    public static class CreatePr
    {
        [FunctionName("CreatePR")]
        public static async Task RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string repositoryName = data.repository;
            string sourceBranch = data.branches.source;
            string targetBranch = data.branches.target;
            string releaseId = data.releaseId;

            var credentials = new VssBasicCredential(Environment.GetEnvironmentVariable("Useremail"),
                Environment.GetEnvironmentVariable("AccessToken"));
            using (var connection = new VssConnection(new Uri(Environment.GetEnvironmentVariable("OrganizationURL")),
                credentials))
            {
                using (var gitClient = await connection.GetClientAsync<GitHttpClient>())
                {
                    var repository = (await gitClient.GetRepositoriesAsync())
                        .FirstOrDefault(r => string.Equals(repositoryName, r.Name, StringComparison.InvariantCultureIgnoreCase));

                    var commitsBatch = await gitClient.GetCommitsBatchAsync(new GitQueryCommitsCriteria()
                    {
                        CompareVersion = new GitVersionDescriptor
                        {
                            Version = sourceBranch,
                            VersionType = GitVersionType.Branch
                        },
                        ItemVersion = new GitVersionDescriptor
                        {
                            Version = targetBranch,
                            VersionType = GitVersionType.Branch
                        },
                        IncludeWorkItems = true
                    }, repository.Id);

                    if (commitsBatch.Any())
                    {
                        var comments = "## Commits";
                        foreach (var commit in commitsBatch.OrderBy(c => c.Committer.Name))
                        {
                            comments += $"\n - {commit.CommitId} - {commit.Comment} - {commit.Committer?.Name}";
                        }

                        var workItems = commitsBatch.Where(c => c.WorkItems.Any()).SelectMany(c => c.WorkItems);

                        var existingPrs = await gitClient.GetPullRequestsAsync(repository.Id,
                            new GitPullRequestSearchCriteria()
                            {
                                SourceRefName = $"refs/heads/{sourceBranch}",
                                TargetRefName = $"refs/heads/{targetBranch}"
                            });

                        if (!existingPrs.Any())
                        {
                            await gitClient.CreatePullRequestAsync(new GitPullRequest
                            {
                                SourceRefName = $"refs/heads/{sourceBranch}",
                                TargetRefName = $"refs/heads/{targetBranch}",
                                Title = $"PR Release {releaseId} - {DateTime.Today:d}",
                                Description = comments.Length >= 4000 ? comments.Substring(0, 4000) : comments,
                                WorkItemRefs = workItems.ToArray()
                            }, repository.Id);
                        }
                        else
                        {
                            await gitClient.UpdatePullRequestAsync(new GitPullRequest
                            {
                                Description = comments
                            }, repository.Id, existingPrs.First().PullRequestId);
                        }
                    }
                }
            }
        }
    }
}
