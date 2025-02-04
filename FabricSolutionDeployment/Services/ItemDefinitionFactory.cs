using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Text;
using System.Text.Json;


public class ItemDefinitionFactory {

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

  private static ItemDefinitionPart CreateInlineBase64Part(string Path, string Payload) {
    string base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Payload));
    return new ItemDefinitionPart(Path, base64Payload, PayloadType.InlineBase64);
  }

  public static CreateItemRequest GetImportedSalesModelCreateRequest(string DisplayName) {

    string part1FileContent = GetTemplateFile(@"SemanticModels\definition.pbism");
    string part2FileContent = GetTemplateFile(@"SemanticModels\sales_model_import.bim");

    var createRequest = new CreateItemRequest(DisplayName, ItemType.SemanticModel);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      });

    return createRequest;
  }

  public static UpdateItemDefinitionRequest GetImportedSalesModelUpdateRequest(string DisplayName) {

    string part1FileContent = GetTemplateFile(@"SemanticModels\definition.pbism");
    string part2FileContent = GetTemplateFile(@"SemanticModels\sales_model_import_v2.bim");

    return new UpdateItemDefinitionRequest(
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("definition.pbism", part1FileContent),
        CreateInlineBase64Part("model.bim", part2FileContent)
      }));
  }

  public static CreateItemRequest GetSalesReportCreateRequest(Guid SemanticModelId, string DisplayName) {

    string part1FileContent = GetTemplateFile(@"Reports\definition.pbir").Replace("{SEMANTIC_MODEL_ID}", SemanticModelId.ToString());
    string part2FileContent = GetTemplateFile(@"Reports\sales_report.json");
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

  public static UpdateItemDefinitionRequest GetSalesReportUpdateRequest(Guid SemanticModelId, string DisplayName) {

    string part1FileContent = GetTemplateFile(@"Reports\definition.pbir").Replace("{ SEMANTIC_MODEL_ID}", SemanticModelId.ToString());
    string part2FileContent = GetTemplateFile(@"Reports\sales_report.json");
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

  public static CreateItemRequest GetCreateNotebookRequest(Guid WorkspaceId, Item Lakehouse, string DisplayName, string NotebookContent) {

    NotebookContent = NotebookContent.Replace("{WORKSPACE_ID}", WorkspaceId.ToString())
                                     .Replace("{LAKEHOUSE_ID}", Lakehouse.Id.ToString())
                                     .Replace("{LAKEHOUSE_NAME}", Lakehouse.DisplayName);

    var createRequest = new CreateItemRequest(DisplayName, ItemType.Notebook);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("notebook-content.py", NotebookContent)
      });

    return createRequest;

  }

  public static CreateItemRequest GetCreateNotebookRequestFromIpynb(Guid WorkspaceId, Item Lakehouse, string DisplayName, string NotebookContent) {

    NotebookContent = NotebookContent.Replace("{WORKSPACE_ID}", WorkspaceId.ToString())
                                     .Replace("{LAKEHOUSE_ID}", Lakehouse.Id.ToString())
                                     .Replace("{LAKEHOUSE_NAME}", Lakehouse.DisplayName);

    var createRequest = new CreateItemRequest(DisplayName, ItemType.Notebook);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("notebook-content.ipynb", NotebookContent)
      });

    createRequest.Definition.Format = "ipynb";

    return createRequest;

  }

  public static CreateItemRequest GetDirectLakeSalesModelCreateRequest(string DisplayName, string SqlEndpointServer, string SqlEndpointDatabase) {

    string part1FileContent = GetTemplateFile(@"SemanticModels\definition.pbism");
    string part2FileContent = GetTemplateFile(@"SemanticModels\sales_model_DirectLake.bim")
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

  public static CreateItemRequest GetDataPipelineCreateRequest(string DisplayName, string CodeContent) {

    var createRequest = new CreateItemRequest(DisplayName, ItemType.DataPipeline);

    createRequest.Definition =
      new ItemDefinition(new List<ItemDefinitionPart>() {
        CreateInlineBase64Part("pipeline-content.json", CodeContent)
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


    List<string> ignoredFilePaths = new List<string> {
      //"\\.pbi\\",
      //"diagramLayout.json",
      //"localSettings.json",
      //".platform"
    };

    List<string> FilesInFolder = Directory.GetFiles(ItemFolderPath, "*", SearchOption.AllDirectories).ToList<string>();

    List<string> ItemDefinitionFiles = FilesInFolder.Where(filePath => !ignoredFilePaths.Any(ignoredFilePath => filePath.Contains(ignoredFilePath))).ToList();

    foreach (string ItemDefinitionFile in ItemDefinitionFiles) {

      string fileContentBase64 = Convert.ToBase64String(File.ReadAllBytes(ItemDefinitionFile));

      parts.Add(new ItemDefinitionPart(GetPartPath(ItemFolderPath, ItemDefinitionFile), fileContentBase64, "InlineBase64"));

    }

    itemCreateRequest.Definition = new ItemDefinition(parts);

    return itemCreateRequest;
  }

}