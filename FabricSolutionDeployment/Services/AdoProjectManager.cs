using Microsoft.Fabric.Api.Core.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class AdoProjectManager {

  private const string AdoProjectTemplateId = "b8a3a935-7e91-48b8-a94c-606d37c3e9f2";

  private static readonly string[] AdoUserPermissionScopes = new string[] {
      "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation"
  };

  private static readonly string[] AdoServicePrincialPermissionScopes = new string[] {
      "499b84ac-1321-427f-aa17-267ca6975798/.default"
  };

  public static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private static string GetAzureDevOpsAccessToken() {
    if (AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth) {
      return EntraIdTokenManager.GetAccessTokenResult(AdoServicePrincialPermissionScopes).AccessToken;
    }
    else {
      return EntraIdTokenManager.GetAccessTokenResult(AdoUserPermissionScopes).AccessToken;
    }
  }

  private static VssConnection GetAzureDevOpsConnection() {
    var orgUrl = new Uri(AppSettings.AzureDevOpsApiBaseUrl);
    return new VssConnection(orgUrl, new VssOAuthAccessTokenCredential(GetAzureDevOpsAccessToken()));
  }

  private static GitHttpClient gitHttpClient = GetAzureDevOpsConnection().GetClient<GitHttpClient>();
  private static ProjectHttpClient projectClient = GetAzureDevOpsConnection().GetClient<ProjectHttpClient>();
  private static OperationsHttpClient operationsClient = GetAzureDevOpsConnection().GetClient<OperationsHttpClient>();

  public static List<TeamProjectReference> GetProjects() {
    return projectClient.GetProjects().Result.ToList();
  }

  public static TeamProjectReference GetProject(string ProjectName) {

    var projects = GetProjects();

    foreach (var project in projects) {
      if (project.Name == ProjectName) return project;
    }

    throw new ApplicationException("Could not find requested project");

  }

  private static Guid GetProjectId(string ProjectName) {

    var projects = GetProjects();

    foreach (var project in projects) {
      if (project.Name == ProjectName) return project.Id;
    }

    throw new ApplicationException("Could not find requested project");

  }

  public static string CreateProject(string ProjectName, Workspace TargetWorkspace = null) {

    AppLogger.LogStep($"Create Azure DevOps project named [{ProjectName}]");

    DeleteProjectIfItExists(ProjectName);

    var project = new TeamProject {
      Name = ProjectName,
      Description = "This is a sample project created to demonstrate GIT integration with Fabric.",
      Visibility = ProjectVisibility.Private,
      Capabilities = new Dictionary<string, Dictionary<string, string>>() {
         {  "versioncontrol", new Dictionary<string, string>() { { "sourceControlType", "Git" } } },
         {  "processTemplate", new Dictionary<string, string>() { { "templateTypeId", AdoProjectTemplateId } } }
       }
    };

    Task<OperationReference> queueCreateProjectTask = projectClient.QueueCreateProject(project);
    OperationReference operationReference = queueCreateProjectTask.Result;

    Operation operation = operationsClient.GetOperationAsync(operationReference).Result;

    while (!operation.Completed) {
      Thread.Sleep(3000);
      operation = operationsClient.GetOperationAsync(operationReference).Result;
    }

    string lastObjectId = PushInitialContentWithReadMe(ProjectName, TargetWorkspace);

    AppLogger.LogSubstep("Create project operation complete");

    return lastObjectId;
  }
 
  public static TeamProjectReference EnsureProjectExists(string ProjectName) {

    var projects = GetProjects();

    foreach (var project in projects) {
      if (project.Name == ProjectName) return project;
    }

    CreateProject(ProjectName);

    return GetProject(ProjectName);

  }

  public static void DeleteProject(Guid ProjectId) {
    OperationReference operationReference = projectClient.QueueDeleteProject(ProjectId).Result;
    Operation operation = operationsClient.GetOperationAsync(operationReference).Result;
    while (!operation.Completed) {
      Thread.Sleep(3000);
      operation = operationsClient.GetOperationAsync(operationReference).Result;
    }
  }

  public static void DeleteProject(string ProjectName) {
    Guid projectId = GetProjectId(ProjectName);
    DeleteProject(projectId);
  }

  private static void DeleteProjectIfItExists(string ProjectName) {
    var projects = GetProjects();
    foreach (var project in projects) {
      if (project.Name == ProjectName) {
        AppLogger.LogSubstep($"Deleting existing project with same name");
        DeleteProject(project.Id);
        return;
      }
    }
  }

  public static void DisplayProjects() {
    AppLogger.LogStep("All Projects");
    foreach (var project in GetProjects()) {
      AppLogger.LogSubstep(project.Name + " - " + project.Id.ToString());
    }
  }

  public static Guid GetProjectRepoId(string ProjectName) {

    List<GitRepository> repos = gitHttpClient.GetRepositoriesAsync(ProjectName).Result;

    foreach (GitRepository repo in repos) {
      if (repo.Name == ProjectName) {
        return repo.Id;
      }
    }

    throw new ApplicationException("Cannot find project");
  }

  public static void CopyFilesFromGitRepoToLocalFolder(string ProjectName) {

    DeleteAllFilesInLocalGitFolder(ProjectName);

    Guid repoId = GetProjectRepoId(ProjectName);

    var items = gitHttpClient.GetItemsAsync(repoId,
                                            download: true,
                                            recursionLevel: VersionControlRecursionType.Full).Result;

    foreach (var item in items) {
      if (!item.IsFolder) {
        AppLogger.LogStep($"{item.Path.Substring(1)}");
        var contentStream = gitHttpClient.GetItemContentAsync(repoId, item.Path).Result;
        var contentReader = new StreamReader(contentStream);
        var content = contentReader.ReadToEnd();
        WriteGitFileToLocalFolder(ProjectName, item.Path, content, false);
      }
    }

  }

  public static void DeleteAllFilesInLocalGitFolder(string ProjectName) {
    string targetFolder = AppSettings.LocalExportFolder + ProjectName + @"\";
    if (Directory.Exists(targetFolder)) {
      DirectoryInfo di = new DirectoryInfo(targetFolder);
      foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
      foreach (DirectoryInfo dir in di.GetDirectories()) { dir.Delete(true); }
    }
  }

  public static void WriteGitFileToLocalFolder(string RepoName, string FilePath, string FileContent, bool ConvertFromBase64 = true) {

    if (ConvertFromBase64) {
      byte[] bytes = Convert.FromBase64String(FileContent);
      FileContent = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    FilePath = FilePath.Replace("/", @"\");
    string folderPath = AppSettings.LocalExportFolder + RepoName;
    Directory.CreateDirectory(folderPath);

    string fullPath = folderPath + @"\" + FilePath;

    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

    File.WriteAllText(fullPath, FileContent);

  }

  public static List<ItemDefinitonFile> GetItemsFromGitRepo(string ProjectName) {

    var gitItems = new List<ItemDefinitonFile>();

    Guid repoId = GetProjectRepoId(ProjectName);

    var items = gitHttpClient.GetItemsAsync(repoId,
                                            download: true,
                                            recursionLevel: VersionControlRecursionType.Full).Result;

    foreach (var item in items) {
      if (!item.IsFolder && item.Path.Substring(1).Contains("/")) {
        var contentStream = gitHttpClient.GetItemContentAsync(repoId, item.Path).Result;
        var contentReader = new StreamReader(contentStream);
        string content = contentReader.ReadToEnd();
        string path = item.Path;
        gitItems.Add(new ItemDefinitonFile{
          Content = content,
          FullPath = path.Substring(1)
        });
      }
    }

    return gitItems;
  }

  public static DeploymentConfiguration GetDeployConfigFromGitRepo(string ProjectName) {

    var gitItems = new List<ItemDefinitonFile>();

    Guid repoId = GetProjectRepoId(ProjectName);

    var items = gitHttpClient.GetItemsAsync(repoId,
                                            download: true,
                                            recursionLevel: VersionControlRecursionType.OneLevel).Result;

    foreach (var item in items) {
      if (item.Path.Substring(1) == "deploy.config.json") {
        var contentStream = gitHttpClient.GetItemContentAsync(repoId, item.Path).Result;
        var contentReader = new StreamReader(contentStream);
        string content = contentReader.ReadToEnd();
        var x = JsonSerializer.Deserialize<DeploymentConfiguration>(content, jsonSerializerOptions);
       return x;
      }
    }

    return null;
  }


  public static string PushInitialContentWithReadMe(string ProjectName, Workspace TargetWorkspace = null, string LastObjectId = "0000000000000000000000000000000000000000") {

    // update markdown content for ReadMe.md
    string ReadMeContent = string.Empty;

    if(TargetWorkspace == null) {
      ReadMeContent = ItemDefinitionFactory.GetTemplateFile(@"AdoProjectTemplates\AdoReadMe.md");
    }
    else {
      string workspaceName = TargetWorkspace.DisplayName;
      string workspaceId = TargetWorkspace.Id.ToString();
      string workspaceUrl = $"https://app.powerbi.com/groups/{TargetWorkspace.Id.ToString()}";

      ReadMeContent = ItemDefinitionFactory.GetTemplateFile(@"AdoProjectTemplates\AdoReadMeWithWorkspace.md")
                                           .Replace("{WORKSPACE_NAME}", workspaceName)
                                           .Replace("{WORKSPACE_ID}", workspaceId)
                                           .Replace("{WORKSPACE_URL}", workspaceUrl);
    }

    Guid repoId = GetProjectRepoId(ProjectName);

    GitPush pushReadMe = new GitPush {
      RefUpdates = new List<GitRefUpdate>() {
        new GitRefUpdate {
          Name = "refs/heads/main",
          OldObjectId = LastObjectId
        }
      },
      Commits = new List<GitCommit> {
        new GitCommit {
          Comment = "Commit initial ReadMe.md",
          Changes = new List<GitChange> {
            new GitChange {
              ChangeType = VersionControlChangeType.Add,
              Item = new GitItem {
                Path = "/README.md"
              },
              NewContent = new ItemContent {
                Content = ReadMeContent,
                ContentType = ItemContentType.RawText
              }
            }
          }
        }
      }
    };

    GitPush pushResponse = gitHttpClient.CreatePushAsync(pushReadMe, repoId).Result;

    string oldObjectId = pushResponse.RefUpdates.FirstOrDefault().NewObjectId;

    return oldObjectId;

  }

  public static bool DoesFileExistInGitRepo(string ProjectName, string FileName) {

    var gitItems = new List<ItemDefinitonFile>();

    Guid repoId = GetProjectRepoId(ProjectName);

    var items = gitHttpClient.GetItemsAsync(repoId,
                                            download: true,
                                            recursionLevel: VersionControlRecursionType.Full).Result;

    foreach (var item in items) {
      if (!item.IsFolder && item.Path.Substring(1) == FileName) {
        return true;
      }
    }

    return false;


  }


  public static string PushFileToGitRepo(string ProjectName, string FileName, string FileContent) {

    var doesFileExist = DoesFileExistInGitRepo(ProjectName, FileName);

    var repoId = GetProjectRepoId(ProjectName);

    var repositories = gitHttpClient.GetRepositoriesAsync(ProjectName).Result;

    var mainRepository = repositories.First();
    var refs = gitHttpClient.GetRefsAsync(ProjectName, mainRepository.Id, filter: $"heads/main").Result;
    var mainBranchRef = refs.First();

    string mainBranchObjectId = mainBranchRef.ObjectId;

    var changes = new List<GitChange>();
      changes.Add(new GitChange {
        ChangeType = doesFileExist ? VersionControlChangeType.Edit : VersionControlChangeType.Add,
        Item = new GitItem {
          Path = "/" + FileName
        },
        NewContent = new ItemContent {
          Content = Convert.ToBase64String(Encoding.ASCII.GetBytes(FileContent)),
          ContentType = ItemContentType.Base64Encoded
        }
      });
 
    var pushRequest = new GitPush {
      RefUpdates = new List<GitRefUpdate>() {
        new GitRefUpdate {
          Name = "refs/heads/main",
          OldObjectId = mainBranchObjectId
        }
      },
      Commits = new List<GitCommit>() {
        new GitCommit {
        Changes = changes,
        Comment = "Adding source files for import mode solution"
        }
       }
    };

    GitPush pushResponse = gitHttpClient.CreatePushAsync(pushRequest, repoId).Result;

    string oldObjectId = pushResponse.RefUpdates.FirstOrDefault().NewObjectId;

    return oldObjectId;

  }


  public static void CreateBranch(string ProjectName, string BranchName) {

    AppLogger.LogStep($"Creating new branch named {BranchName} in project {ProjectName}");

    var existingProject = GetProject(ProjectName);

    var repositories = gitHttpClient.GetRepositoriesAsync(ProjectName).Result;

    var mainRepository = repositories.First();
    var refs = gitHttpClient.GetRefsAsync(ProjectName, mainRepository.Id, filter: $"heads/main").Result;
    var mainBranchRef = refs.First();

    string mainBranchObjectId = mainBranchRef.ObjectId;

    List<GitRefUpdate> newBranchUpdates = new List<GitRefUpdate>() {
      new GitRefUpdate {
        Name = $"refs/heads/{BranchName}",
        NewObjectId = mainBranchObjectId,
        OldObjectId = "0000000000000000000000000000000000000000"
      }
    };

    gitHttpClient.UpdateRefsAsync(newBranchUpdates, mainRepository.Id);

    AppLogger.LogSubstep("New branch created");

  }

  private static string GetPartPath(string ItemFolderPath, string FilePath) {
    int ItemFolderPathOffset = ItemFolderPath.Length; // + 1;
    return FilePath.Substring(ItemFolderPathOffset).Replace("\\", "/");
  }

}