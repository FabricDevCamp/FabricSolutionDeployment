using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Text;
using System.Text.Json;


public class ItemDefinitionFactory {

  public static ItemDefinition UpdateItemDefinitionPart(ItemDefinition ItemDefinition, string PartPath, Dictionary<string, string> SearchReplaceText) {
    var itemPart = ItemDefinition.Parts.Where(part => part.Path == PartPath).FirstOrDefault();
    if (itemPart != null) {
      ItemDefinition.Parts.Remove(itemPart);
      itemPart.Payload = SearchAndReplaceInPayload(itemPart.Payload, SearchReplaceText);
      ItemDefinition.Parts.Add(itemPart);
    }
    return ItemDefinition;
  }

  public static string SearchAndReplaceInPayload(string Payload, Dictionary<string, string> SearchReplaceText) {
    byte[] PayloadBytes = Convert.FromBase64String(Payload);
    string PayloadContent = Encoding.UTF8.GetString(PayloadBytes, 0, PayloadBytes.Length);
    foreach (var entry in SearchReplaceText.Keys) {
      PayloadContent = PayloadContent.Replace(entry, SearchReplaceText[entry]);
    }
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(PayloadContent));
  }

  public static string GetTemplateFile(string Path) {
    return File.ReadAllText(AppSettings.LocalTemplateFilesRoot + Path);
  }

  private static string GetPartPath(string ItemFolderPath, string FilePath) {
    int ItemFolderPathOffset = ItemFolderPath.Length + 1;
    return FilePath.Substring(ItemFolderPathOffset).Replace("\\", "/");
  }

  public static void DeleteAllTemplateFiles(string WorkspaceName) {
    string targetFolder = AppSettings.LocalExportFolder + (string.IsNullOrEmpty(WorkspaceName) ? "" : WorkspaceName + @"\");
    if (Directory.Exists(targetFolder)) {
      DirectoryInfo di = new DirectoryInfo(targetFolder);
      foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
      foreach (DirectoryInfo dir in di.GetDirectories()) { dir.Delete(true); }
    }
  }

  public static void WriteFile(string WorkspaceFolder, string ItemFolder, string FilePath, string FileContent, bool ConvertFromBase64 = true) {

    if (ConvertFromBase64) {
      byte[] bytes = Convert.FromBase64String(FileContent);
      FileContent = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    FilePath = FilePath.Replace("/", @"\");
    string folderPath = AppSettings.LocalExportFolder + WorkspaceFolder + @"\" + ItemFolder;

    Directory.CreateDirectory(folderPath);

    string fullPath = folderPath + @"\" + FilePath;

    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

    File.WriteAllText(fullPath, FileContent);

  }

  public static ItemDefinitionPart CreateInlineBase64Part(string Path, string Payload) {
    string base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Payload));
    return new ItemDefinitionPart(Path, base64Payload, PayloadType.InlineBase64);
  }

  public static CreateItemRequest GetSemanticModelCreateRequestFromBim(string DisplayName, string BimFile) {

    string part1FileContent = GetTemplateFile(@"SemanticModels\definition.pbism");
    string part2FileContent = GetTemplateFile($@"SemanticModels\{BimFile}");

    var createRequest = new CreateItemRequest(DisplayName, ItemType.SemanticModel);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      });

    return createRequest;
  }

  public static UpdateItemDefinitionRequest GetSemanticModelUpdateRequestFromBim(string DisplayName, string BimFile) {

    string part1FileContent = GetTemplateFile(@"SemanticModels\definition.pbism");
    string part2FileContent = GetTemplateFile($@"SemanticModels\{BimFile}");

    return new UpdateItemDefinitionRequest(
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      }));
  }

  public static CreateItemRequest GetSemanticDirectLakeModelCreateRequestFromBim(string DisplayName, string BimFile, string SqlEndpointServer, string SqlEndpointDatabase) {

    string part1FileContent = GetTemplateFile(@"SemanticModels\definition.pbism");
    string part2FileContent = GetTemplateFile($@"SemanticModels\{BimFile}")
                                               .Replace("{SQL_ENDPOINT_SERVER}", SqlEndpointServer)
                                               .Replace("{SQL_ENDPOINT_DATABASE}", SqlEndpointDatabase);

    var createRequest = new CreateItemRequest(DisplayName, ItemType.SemanticModel);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      });

    return createRequest;
  }

  public static CreateItemRequest GetReportCreateRequestFromReportJson(Guid SemanticModelId, string DisplayName, string ReportJson) {

    string part1FileContent = GetTemplateFile(@"Reports\definition.pbir").Replace("{SEMANTIC_MODEL_ID}", SemanticModelId.ToString());
    string part2FileContent = GetTemplateFile($@"Reports\{ReportJson}");
    string part3FileContent = GetTemplateFile(@"Reports\StaticResources\SharedResources\BaseThemes\CY24SU02.json");

    var createRequest = new CreateItemRequest(DisplayName, ItemType.Report);

    createRequest.Definition =
          new ItemDefinition(new List<ItemDefinitionPart>() {
            CreateInlineBase64Part("definition.pbir", part1FileContent),
            CreateInlineBase64Part("report.json", part2FileContent),
            CreateInlineBase64Part("StaticResources/SharedResources/BaseThemes/CY24SU02.json", part3FileContent),
          });

    return createRequest;

  }

  public static UpdateItemDefinitionRequest GetUpdateRequestFromReportJson(Guid SemanticModelId, string DisplayName, string ReportJson) {

    string part1FileContent = GetTemplateFile(@"Reports\definition.pbir").Replace("{ SEMANTIC_MODEL_ID}", SemanticModelId.ToString());
    string part2FileContent = GetTemplateFile($@"Reports\{ReportJson}");
    string part3FileContent = GetTemplateFile(@"Reports\StaticResources\SharedResources\BaseThemes\CY24SU02.json");
    string part4FileContent = GetTemplateFile(@"Reports\StaticResources\SharedResources\BuiltInThemes\NewExecutive.json");

    return new UpdateItemDefinitionRequest(
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbir", part1FileContent),
        CreateInlineBase64Part("report.json", part2FileContent),
        CreateInlineBase64Part("StaticResources/SharedResources/BaseThemes/CY24SU02.json", part3FileContent),
        CreateInlineBase64Part("StaticResources/SharedResources/BuiltInThemes/NewExecutive.json", part4FileContent)
      }));
  }

  public static CreateItemRequest GetCreateNotebookRequestFromPy(Guid WorkspaceId, Item Lakehouse, string DisplayName, string PyFile) {

    var pyContent = GetTemplateFile($@"Notebooks\{PyFile}").Replace("{WORKSPACE_ID}", WorkspaceId.ToString())
                                                           .Replace("{LAKEHOUSE_ID}", Lakehouse.Id.ToString())
                                                           .Replace("{LAKEHOUSE_NAME}", Lakehouse.DisplayName);

    var createRequest = new CreateItemRequest(DisplayName, ItemType.Notebook);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("notebook-content.py", pyContent)
      });

    return createRequest;

  }

  public static CreateItemRequest GetCreateNotebookRequestFromIpynb(Guid WorkspaceId, Item Lakehouse, string DisplayName, string IpynbFile) {

    var ipynbContent = GetTemplateFile($@"Notebooks\{IpynbFile}").Replace("{WORKSPACE_ID}", WorkspaceId.ToString())
                                                                 .Replace("{LAKEHOUSE_ID}", Lakehouse.Id.ToString())
                                                                 .Replace("{LAKEHOUSE_NAME}", Lakehouse.DisplayName);

    var createRequest = new CreateItemRequest(DisplayName, ItemType.Notebook);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("notebook-content.ipynb", ipynbContent)
      });

    createRequest.Definition.Format = "ipynb";

    return createRequest;

  }

  public static CreateItemRequest GetDataPipelineCreateRequest(string DisplayName, string PipelineDefinition) {

    var createRequest = new CreateItemRequest(DisplayName, ItemType.DataPipeline);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("pipeline-content.json", PipelineDefinition)
      });

    return createRequest;
  }

  public static CreateItemRequest GetDataPipelineCreateRequestForLakehouse(string DisplayName, string CodeContent, string WorkspaceId, string LakehouseId, string ConnectionId) {

    var createRequest = new CreateItemRequest(DisplayName, ItemType.DataPipeline);

    CodeContent = CodeContent
      .Replace("{CONNECTION_ID}", ConnectionId)
      .Replace("{WORKSPACE_ID}", WorkspaceId)
      .Replace("{LAKEHOUSE_ID}", LakehouseId);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("pipeline-content.json", CodeContent)
      });

    return createRequest;
  }

  public static CreateItemRequest GetDataPipelineCreateRequestForWarehouse(string DisplayName, string CodeContent, string WorkspaceId, string WarehouseId, string WarehouseConnectString) {

    var createRequest = new CreateItemRequest(DisplayName, ItemType.DataPipeline);

    CodeContent = CodeContent
      .Replace("{WORKSPACE_ID}", WorkspaceId)
      .Replace("{WAREHOUSE_ID}", WarehouseId)
      .Replace("{WAREHOUSE_CONNECT_STRING}", WarehouseConnectString);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("pipeline-content.json", CodeContent)
      });

    return createRequest;
  }

  public static CreateItemRequest GetCreateItemRequestFromFolder(string ItemFolder) {

    string ItemFolderPath = AppSettings.LocalItemTemplatesFolder + ItemFolder;

    string metadataFilePath = ItemFolderPath + @"\.platform";
    string metadataFileContent = File.ReadAllText(metadataFilePath);
    PlatformFileMetadata item = JsonSerializer.Deserialize<FabricPlatformFile>(metadataFileContent).metadata;

    CreateItemRequest itemCreateRequest = new CreateItemRequest(item.displayName, item.type);

    var parts = new List<ItemDefinitionPart>();

    List<string> ItemDefinitionFiles = Directory.GetFiles(ItemFolderPath, "*", SearchOption.AllDirectories).ToList<string>();

    foreach (string ItemDefinitionFile in ItemDefinitionFiles) {

      string fileContentBase64 = Convert.ToBase64String(File.ReadAllBytes(ItemDefinitionFile));

      parts.Add(new ItemDefinitionPart(GetPartPath(ItemFolderPath, ItemDefinitionFile), fileContentBase64, "InlineBase64"));

    }

    itemCreateRequest.Definition = new ItemDefinition(parts);

    return itemCreateRequest;
  }

  public static ItemDefinition UpdateReportDefinitionWithRedirection(ItemDefinition ReportDefinition, Guid WorkspaceId, Dictionary<string, string> ReportRedirects) {

    var pbirDefinitionPart = ReportDefinition.Parts.Where(part => part.Path == "definition.pbir").First();

    byte[] payloadBytes = Convert.FromBase64String(pbirDefinitionPart.Payload);
    string payloadContent = Encoding.UTF8.GetString(payloadBytes, 0, payloadBytes.Length);

    var pbirDefinition = JsonSerializer.Deserialize<ReportDefinitionFile>(payloadContent);

    if ((pbirDefinition.datasetReference.byPath != null) &&
        (pbirDefinition.datasetReference.byPath.path != null) &&
        (pbirDefinition.datasetReference.byPath.path.Length > 0)) {

      string targetModelName = pbirDefinition.datasetReference.byPath.path.Replace("../", "")
                                                                          .Replace(".SemanticModel", "");

      var targetModel = FabricRestApi.GetSemanticModelByName(WorkspaceId, targetModelName);

      ReportDefinition.Parts.Remove(pbirDefinitionPart);

      string reportDefinitionPartTemplate = ItemDefinitionFactory.GetTemplateFile(@"Reports\definition.pbir");
      string reportDefinitionPartContent = reportDefinitionPartTemplate.Replace("{SEMANTIC_MODEL_ID}", targetModel.Id.ToString());
      var reportDefinitionPart = ItemDefinitionFactory.CreateInlineBase64Part("definition.pbir", reportDefinitionPartContent);
      ReportDefinition.Parts.Add(reportDefinitionPart);
      return ReportDefinition;
    }
    else {
      return ItemDefinitionFactory.UpdateItemDefinitionPart(ReportDefinition,
                                                            "definition.pbir",
                                                            ReportRedirects);
    }

  }

  public static ItemDefinition UpdateReportDefinitionWithSemanticModelId(ItemDefinition ItemDefinition, Guid TargetModelId) {
    var partDefinition = ItemDefinition.Parts.Where(part => part.Path == "definition.pbir").First();
    ItemDefinition.Parts.Remove(partDefinition);
    string reportDefinitionPartTemplate = ItemDefinitionFactory.GetTemplateFile(@"Reports\definition.pbir");
    string reportDefinitionPartContent = reportDefinitionPartTemplate.Replace("{SEMANTIC_MODEL_ID}", TargetModelId.ToString());
    var reportDefinitionPart = ItemDefinitionFactory.CreateInlineBase64Part("definition.pbir", reportDefinitionPartContent);
    ItemDefinition.Parts.Add(reportDefinitionPart);
    return ItemDefinition;
  }

  // export item definitions
  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {

    AppLogger.LogSolution($"Exporting workspace item definitions from workspace [{WorkspaceName}]");

    DeleteExportsFolderContents(WorkspaceName);

    AppLogger.LogStep($"Starting export process");


    var workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);
    var items = FabricRestApi.GetWorkspaceItems(workspace.Id);

    // list of items types that should be exported
    List<ItemType> itemTypesForExport = new List<ItemType>() {
      ItemType.Notebook, ItemType.SemanticModel, ItemType.Report,
      ItemType.DataPipeline, ItemType.Environment
    };

    List<string> lakehouseNames = items.Where(item => item.Type == ItemType.Lakehouse)
                                       .Select(item => item.DisplayName).ToList();

    foreach (var item in items) {
      if (itemTypesForExport.Contains(item.Type)) {

        // filter out lakehouse default semantic models
        if(!lakehouseNames.Contains(item.DisplayName) || item.Type != ItemType.SemanticModel) {

          // fetch item definition from workspace
          var definition = FabricRestApi.GetItemDefinition(workspace.Id, item.Id.Value);

          // write item definition files to local folder
          string targetFolder = item.DisplayName + "." + item.Type;

          AppLogger.LogSubstep($"Exporting item definition for [{targetFolder}]");

          foreach (var part in definition.Parts) {
            WriteFileToExportsFolder(WorkspaceName, targetFolder, part.Path, part.Payload);
          }

        }

      }

    }

    AppLogger.LogStep($"Export process for [{WorkspaceName}] complete");

  }

  public static void DeleteExportsFolderContents(string WorkspaceName) {
    string targetFolder = AppSettings.LocalExportFolder + (string.IsNullOrEmpty(WorkspaceName) ? "" : WorkspaceName + @"\");
    if (Directory.Exists(targetFolder)) {
      DirectoryInfo di = new DirectoryInfo(targetFolder);
      foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
      foreach (DirectoryInfo dir in di.GetDirectories()) { dir.Delete(true); }
    }
  }

  public static void WriteFileToExportsFolder(string WorkspaceFolder, string ItemFolder, string FilePath, string FileContent, bool ConvertFromBase64 = true) {

    if (ConvertFromBase64) {
      byte[] bytes = Convert.FromBase64String(FileContent);
      FileContent = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    FilePath = FilePath.Replace("/", @"\");
    string folderPath = AppSettings.LocalExportFolder + WorkspaceFolder + @"\" + ItemFolder;

    Directory.CreateDirectory(folderPath);

    string fullPath = folderPath + @"\" + FilePath;

    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

    File.WriteAllText(fullPath, FileContent);

  }

  public static void ExportWorkspaceToPackagedSolutionFolder(string WorkspaceName, string SolutionFolderName) {

    AppLogger.LogSolution($"Exporting workspace [{WorkspaceName}] to packaged solution folder [{SolutionFolderName}]");

    DeleteSolutionFolderContents(SolutionFolderName);

    var workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);
    var items = FabricRestApi.GetWorkspaceItems(workspace.Id);

    var lakehouseNames = items.Where(item => item.Type == ItemType.Lakehouse).ToList().Select(lakehouse => lakehouse.DisplayName).ToList();

    // list of items types that should be exported
    List<ItemType> itemTypesForExport = new List<ItemType>() {
      ItemType.Notebook, ItemType.DataPipeline, ItemType.SemanticModel, ItemType.Report
    };

    AppLogger.LogStep("Exporting item definitions");

    foreach (var item in items) {

      // only include supported item types
      if (itemTypesForExport.Contains(item.Type)) {

        // filter out lakehouse default semntic models
        if ((item.Type != ItemType.SemanticModel) ||
            (!lakehouseNames.Contains(item.DisplayName))) {

          // fetch item definition from workspace
          var definition = FabricRestApi.GetItemDefinition(workspace.Id, item.Id.Value);

          // write item definition files to local folder
          string targetFolder = item.DisplayName + "." + item.Type;

          AppLogger.LogSubstep($"Exporting item definition for [{targetFolder}]");

          foreach (var part in definition.Parts) {
            WriteFileToSolutionFolder(SolutionFolderName, targetFolder, part.Path, part.Payload);
          }

        }

      }

    }

    var lakehouses = FabricRestApi.GetWorkspaceItems(workspace.Id, "Lakehouse");
    foreach (var lakehouse in lakehouses) {

      // fetch item definition from workspace
      var platformFile = new FabricPlatformFile {
        schema = "https://developer.microsoft.com/json-schemas/fabric/gitIntegration/platformProperties/2.0.0/schema.json",
        config = new PlatformFileConfig {
          logicalId = Guid.Empty.ToString(),
          version = "2.0"
        },
        metadata = new PlatformFileMetadata {
          displayName = lakehouse.DisplayName,
          type = "Lakehouse"
        }
      };

      string platformFileContent = JsonSerializer.Serialize(platformFile);
      string platformFileName = ".platform";
      // write item definition files to local folder
      string targetFolder = lakehouse.DisplayName + "." + lakehouse.Type;
      AppLogger.LogSubstep($"Exporting item definition for [{targetFolder}]");

      WriteFileToSolutionFolder(SolutionFolderName, targetFolder, platformFileName, platformFileContent, false);

    }

    AppLogger.LogSubstep($"Exporting [deploy.config.json]");

    var config = GenerateDeployConfigFile(workspace, items);

    WriteFileToSolutionFolder(SolutionFolderName, "", "deploy.config.json", config, false);

    AppLogger.LogStep("Packaged solution folder export process complete");

  }

  public static void DeleteSolutionFolderContents(string WorkspaceName) {
    string targetFolder = AppSettings.LocalPackagedSolutionFolder + WorkspaceName + @"\";
    if (Directory.Exists(targetFolder)) {
      DirectoryInfo di = new DirectoryInfo(targetFolder);
      foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
      foreach (DirectoryInfo dir in di.GetDirectories()) { dir.Delete(true); }
    }
  }

  public static void WriteFileToSolutionFolder(string WorkspaceFolder, string ItemFolder, string FilePath, string FileContent, bool ConvertFromBase64 = true) {

    if (ConvertFromBase64) {
      byte[] bytes = Convert.FromBase64String(FileContent);
      FileContent = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    FilePath = FilePath.Replace("/", @"\");
    string folderPath = AppSettings.LocalPackagedSolutionFolder + WorkspaceFolder + @"\" + ItemFolder;

    Directory.CreateDirectory(folderPath);

    string fullPath = folderPath + @"\" + FilePath;

    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

    File.WriteAllText(fullPath, FileContent);

  }

  public static string GenerateDeployConfigFile(Workspace Workspace, List<Item> WorkspaceItems) {

    DeploymentConfiguration deployConfig = new DeploymentConfiguration {
      SourceItems = new List<DeploymentSourceItem>(),
      SourceLakehouses = new List<DeploymentSourceLakehouse>(),
      SourceConnections = new List<DeploymentSourceConnection>()
    };

    deployConfig.SourceWorkspaceId = Workspace.Id.ToString();

    var workspaceInfo = FabricRestApi.GetWorkspaceInfo(Workspace.Id);
    deployConfig.SourceWorkspaceDescription = workspaceInfo.Description;

    foreach (var item in WorkspaceItems) {

      // add each items to items collection
      deployConfig.SourceItems.Add(
      new DeploymentSourceItem {
        Id = item.Id.ToString(),
        DisplayName = item.DisplayName,
        Type = item.Type.ToString(),
      });

      // add lakehouses with SQL endpoint info to lakehouses collection
      if (item.Type == "Lakehouse") {
        var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(Workspace.Id, item.Id.Value);
        var lakehouse = new DeploymentSourceLakehouse {
          Id = item.Id.ToString(),
          DisplayName = item.DisplayName,
          Server = sqlEndpoint.ConnectionString,
          Database = sqlEndpoint.Id
        };

        var shortcuts = FabricRestApi.GetLakehouseShortcuts(Workspace.Id, item.Id.Value);
        if (shortcuts.Count() > 0) {
          lakehouse.Shortcuts = new List<DeploymentSourceLakehouseShortcut>();
          foreach (var shortcut in shortcuts) {
            lakehouse.Shortcuts.Add(new DeploymentSourceLakehouseShortcut {
              ConnectionId = shortcut.Target.AdlsGen2.ConnectionId.ToString(),
              Name = shortcut.Name,
              Path = shortcut.Path,
              Location = shortcut.Target.AdlsGen2.Location.ToString(),
              Subpath = shortcut.Target.AdlsGen2.Subpath,
              Type = shortcut.Target.Type.ToString()
            });


          }
        }

        deployConfig.SourceLakehouses.Add(lakehouse);

      }

    }

    string connectionNamePrefix = $"Workspace[{Workspace.Id.ToString()}]-";

    foreach (var connection in FabricRestApi.GetWorkspaceConnections(Workspace.Id)) {
      deployConfig.SourceConnections.Add(new DeploymentSourceConnection {
        Id = connection.Id.ToString(),
        DisplayName = connection.DisplayName.Replace(connectionNamePrefix, ""),
        Type = connection.ConnectionDetails.Type.ToString(),
        Path = connection.ConnectionDetails.Path,
        CredentialType = connection.CredentialDetails.CredentialType.Value.ToString(),
      });


    }

    var config = JsonSerializer.Serialize(deployConfig, jsonSerializerOptions);

    return config;
  }

  public static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

}