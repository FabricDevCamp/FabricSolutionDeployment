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

  #region "Internal plumbing details"

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

  #endregion

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

  public static Guid GetProjectRepoId(string ProjectName) {

    List<GitRepository> repos = gitHttpClient.GetRepositoriesAsync(ProjectName).Result;

    foreach (GitRepository repo in repos) {
      if (repo.Name == ProjectName) {
        return repo.Id;
      }
    }

    throw new ApplicationException("Cannot find project");
  }

  public static void GetProjectBranches(string ProjectName) {

    var project = GetProject(ProjectName);


    if (project == null) {
      throw new ApplicationException("Could not find requested project");
    }

    var projectId = project.Id;
    var projectRepoId = GetProjectRepoId(ProjectName);

    var branches = gitHttpClient.GetBranchesAsync(projectRepoId).Result;

    AppLogger.LogStep("Branches:");
    foreach (var branch in branches) {
      AppLogger.LogSubstep(branch.Name);

    }


  }

  public static string GetMostRecentDailyBuildBranch(string ProjectName) {

    var project = GetProject(ProjectName);

    if (project == null) {
      throw new ApplicationException("Could not find requested project");
    }

    var projectRepoId = GetProjectRepoId(ProjectName);
    var branches = gitHttpClient.GetBranchesAsync(projectRepoId).Result;

    List<string> branchesList = new List<string>();

    foreach (var branch in branches) {
      if (branch.Name.Contains("daily-build")) {
        branchesList.Add(branch.Name);
      }
    }

    return branchesList.OrderByDescending(i => i).First();

  }

  public static string GetFirstDailyBuildBranch(string ProjectName) {

    var project = GetProject(ProjectName);

    if (project == null) {
      throw new ApplicationException("Could not find requested project");
    }

    var projectRepoId = GetProjectRepoId(ProjectName);
    var branches = gitHttpClient.GetBranchesAsync(projectRepoId).Result;

    List<string> branchesList = new List<string>();

    foreach (var branch in branches) {
      if (branch.Name.Contains("daily-build")) {
        branchesList.Add(branch.Name);
      }
    }

    return branchesList.OrderBy(i => i).First();

  }


  public static bool BranchAlreadyExist(string ProjectName, string BranchName) {

    var project = GetProject(ProjectName);

    if (project == null) {
      throw new ApplicationException("Could not find requested project");
    }

    var projectRepoId = GetProjectRepoId(ProjectName);
    var branches = gitHttpClient.GetBranchesAsync(projectRepoId).Result;

    foreach (var branch in branches) {
      if (branch.Name == BranchName) {
        return true;
      }
    }

    return false;

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

  public static TeamProjectReference EnsureProjectExists(string ProjectName, Workspace TargetWorkspace = null) {

    var projects = GetProjects();

    foreach (var project in projects) {
      if (project.Name == ProjectName) return project;
    }

    CreateProject(ProjectName, TargetWorkspace);

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

  public static List<GitItem> GetGitItemsFromAdoProject(string ProjectName, string BranchName = "main") {

    Guid repoId = GetProjectRepoId(ProjectName);

    return gitHttpClient.GetItemsAsync(repoId, download: true,
                                       recursionLevel: VersionControlRecursionType.Full).Result;

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

  public static List<ItemDefinitonFile> GetItemDefinitionFilesFromGitRepo(string ProjectName, string BranchName) {

    var gitItems = new List<ItemDefinitonFile>();

    Guid repoId = GetProjectRepoId(ProjectName);

    GitVersionDescriptor gvd = new GitVersionDescriptor {
      VersionType = GitVersionType.Branch,
      Version = BranchName
    };

    var items = gitHttpClient.GetItemsAsync(repoId, versionDescriptor: gvd, download: true,
                                            recursionLevel: VersionControlRecursionType.Full).Result;

    foreach (var item in items) {
      if (!item.IsFolder && item.Path.Substring(1).Contains("/")) {
        var contentStream = gitHttpClient.GetItemContentAsync(repoId, item.Path, versionDescriptor: gvd).Result;
        var contentReader = new StreamReader(contentStream);
        string content = contentReader.ReadToEnd();
        string path = item.Path;
        gitItems.Add(new ItemDefinitonFile {
          Content = content,
          FullPath = path.Substring(1)
        });
      }
    }

    return gitItems;
  }

  public static DeploymentConfiguration GetDeployConfigFromGitRepo(string ProjectName, string BranchName) {

    Guid repoId = GetProjectRepoId(ProjectName);

    GitVersionDescriptor gvd = new GitVersionDescriptor {
      VersionType = GitVersionType.Branch,
      Version = BranchName
    };

    string itemPath = "/deploy.config.json";
    var item = gitHttpClient.GetItemAsync(repoId, itemPath, versionDescriptor: gvd, download: true).Result;

    var contentStream = gitHttpClient.GetItemContentAsync(repoId, item.Path, versionDescriptor: gvd).Result;
    var contentReader = new StreamReader(contentStream);
    string content = contentReader.ReadToEnd();
    return JsonSerializer.Deserialize<DeploymentConfiguration>(content, jsonSerializerOptions);
  }

  public static string PushInitialContentWithReadMe(string ProjectName, Workspace TargetWorkspace = null, string LastObjectId = "0000000000000000000000000000000000000000") {

    // update markdown content for ReadMe.md
    string ReadMeContent = string.Empty;

    if (TargetWorkspace == null) {
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


  public static string PushFileToGitRepo(string ProjectName, string FileName, string FileContent, string BranchName = "main") {

    var doesFileExist = DoesFileExistInGitRepo(ProjectName, FileName);

    var repoId = GetProjectRepoId(ProjectName);

    var repositories = gitHttpClient.GetRepositoriesAsync(ProjectName).Result;

    var mainRepository = repositories.First();
    var refs = gitHttpClient.GetRefsAsync(ProjectName, mainRepository.Id, filter: $"heads/{BranchName}").Result;
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
          Name = $"refs/heads/{BranchName}",
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


  public static string PushChangesToGitRepo(string ProjectName, List<GitChange> Changes, string BranchName = "main") {

    if (BranchName != "main") {
      AdoProjectManager.CreateBranch(ProjectName, BranchName);
    }
    var repoId = GetProjectRepoId(ProjectName);

    var repositories = gitHttpClient.GetRepositoriesAsync(ProjectName).Result;

    var mainRepository = repositories.First();
    var refs = gitHttpClient.GetRefsAsync(ProjectName, mainRepository.Id, filter: $"heads/{BranchName}").Result;
    var branchRef = refs.First();

    string branchObjectId = branchRef.ObjectId;

    var pushRequest = new GitPush {
      RefUpdates = new List<GitRefUpdate>() {
        new GitRefUpdate {
          Name = $"refs/heads/{BranchName}",
          OldObjectId = branchObjectId
        }
      },
      Commits = new List<GitCommit>() {
        new GitCommit {
        Changes = Changes,
        Comment = "Adding files with item definitions"
        }
       }
    };

    GitPush pushResponse = gitHttpClient.CreatePushAsync(pushRequest, repoId).Result;

    string oldObjectId = pushResponse.RefUpdates.FirstOrDefault().NewObjectId;

    return oldObjectId;

  }


  public static void CreateBranch(string ProjectName, string BranchName) {

    var existingProject = GetProject(ProjectName);

    var repositories = gitHttpClient.GetRepositoriesAsync(ProjectName).Result;

    var mainRepository = repositories.First();
    
    var mainBranch = gitHttpClient.GetRefsAsync(ProjectName, mainRepository.Id, filter: $"heads/main").Result.First();
    
    var targetBranch = gitHttpClient.GetRefsAsync(ProjectName, mainRepository.Id, filter: $"heads/{BranchName}").Result.FirstOrDefault();


    string mainBranchObjectId = mainBranch.ObjectId;

    List<GitRefUpdate> newBranchUpdates = new List<GitRefUpdate>() {
      new GitRefUpdate {
        Name = $"refs/heads/{BranchName}",
        NewObjectId = mainBranchObjectId,
        OldObjectId = "0000000000000000000000000000000000000000"
      }
    };

    gitHttpClient.UpdateRefsAsync(newBranchUpdates, mainRepository.Id);

  }

  private static string GetPartPath(string ItemFolderPath, string FilePath) {
    int ItemFolderPathOffset = ItemFolderPath.Length; // + 1;
    return FilePath.Substring(ItemFolderPathOffset).Replace("\\", "/");
  }



  //public static void ExportWorkspaceToAdoPackagedSolutionFolder(string WorkspaceName, string SolutionFolderName) {

  //  AppLogger.LogSolution($"Exporting workspace [{WorkspaceName}] to packaged solution folder [{SolutionFolderName}]");

  //  DeleteAdoExportsFolderContents(SolutionFolderName);

  //  var workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);
  //  var items = FabricRestApi.GetWorkspaceItems(workspace.Id);

  //  var lakehouseNames = items.Where(item => item.Type == ItemType.Lakehouse).ToList().Select(lakehouse => lakehouse.DisplayName).ToList();

  //  // list of items types that should be exported
  //  List<ItemType> itemTypesForExport = new List<ItemType>() {
  //    ItemType.Notebook, ItemType.DataPipeline, ItemType.SemanticModel, ItemType.Report
  //  };

  //  AppLogger.LogStep("Exporting item definitions");

  //  foreach (var item in items) {

  //    // only include supported item types
  //    if (itemTypesForExport.Contains(item.Type)) {

  //      // filter out lakehouse default semntic models
  //      if ((item.Type != ItemType.SemanticModel) ||
  //          (!lakehouseNames.Contains(item.DisplayName))) {

  //        // fetch item definition from workspace
  //        var definition = FabricRestApi.GetItemDefinition(workspace.Id, item.Id.Value);

  //        // write item definition files to local folder
  //        string targetFolder = item.DisplayName + "." + item.Type;

  //        AppLogger.LogSubstep($"Exporting item definition for [{targetFolder}]");

  //        foreach (var part in definition.Parts) {
  //          WriteFileToAdoExportsFolder(SolutionFolderName, targetFolder, part.Path, part.Payload);
  //        }

  //      }

  //    }

  //  }

  //  var lakehouses = FabricRestApi.GetWorkspaceItems(workspace.Id, "Lakehouse");
  //  foreach (var lakehouse in lakehouses) {

  //    // fetch item definition from workspace
  //    var platformFile = new FabricPlatformFile {
  //      schema = "https://developer.microsoft.com/json-schemas/fabric/gitIntegration/platformProperties/2.0.0/schema.json",
  //      config = new PlatformFileConfig {
  //        logicalId = Guid.Empty.ToString(),
  //        version = "2.0"
  //      },
  //      metadata = new PlatformFileMetadata {
  //        displayName = lakehouse.DisplayName,
  //        type = "Lakehouse"
  //      }
  //    };

  //    string platformFileContent = JsonSerializer.Serialize(platformFile);
  //    string platformFileName = ".platform";
  //    // write item definition files to local folder
  //    string targetFolder = lakehouse.DisplayName + "." + lakehouse.Type;
  //    AppLogger.LogSubstep($"Exporting item definition for [{targetFolder}]");

  //    WriteFileToAdoExportsFolder(SolutionFolderName, targetFolder, platformFileName, platformFileContent, false);

  //  }

  //  AppLogger.LogSubstep($"Exporting [deploy.config.json]");

  //  var config = GenerateDeployConfigFile(workspace, items);

  //  WriteFileToSolutionFolder(SolutionFolderName, "", "deploy.config.json", config, false);

  //  AppLogger.LogStep("Packaged solution folder export process complete");

  //}

  //public static void DeleteAdoProjectRepoContents(string WorkspaceName) {
  //  string targetFolder = AppSettings.LocalPackagedSolutionFolder + WorkspaceName + @"\";
  //  if (Directory.Exists(targetFolder)) {
  //    DirectoryInfo di = new DirectoryInfo(targetFolder);
  //    foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
  //    foreach (DirectoryInfo dir in di.GetDirectories()) { dir.Delete(true); }
  //  }
  //}

  //public static void WriteFileToAdoExportFolder(string WorkspaceFolder, string ItemFolder, string FilePath, string FileContent, bool ConvertFromBase64 = true) {

  //  if (ConvertFromBase64) {
  //    byte[] bytes = Convert.FromBase64String(FileContent);
  //    FileContent = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
  //  }

  //  FilePath = FilePath.Replace("/", @"\");
  //  string folderPath = AppSettings.LocalPackagedSolutionFolder + WorkspaceFolder + @"\" + ItemFolder;

  //  Directory.CreateDirectory(folderPath);

  //  string fullPath = folderPath + @"\" + FilePath;

  //  Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

  //  File.WriteAllText(fullPath, FileContent);

  //}





}