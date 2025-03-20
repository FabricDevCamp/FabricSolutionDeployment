using Microsoft.Fabric.Api.Core.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FabricSolutionDeployment;


public class DeploymentManager {

  #region Lab Utility Methods

  public static void ViewWorkspaces() {

    var workspaces = FabricRestApi.GetWorkspaces();

    AppLogger.LogStep("Workspaces List");
    foreach (var workspace in workspaces) {
      AppLogger.LogSubstep($"{workspace.DisplayName} ({workspace.Id})");
    }

    Console.WriteLine();

  }

  public static void ViewCapacities() {

    var capacities = FabricRestApi.GetCapacities();

    AppLogger.LogStep("Capacities List");
    foreach (var capacity in capacities) {
      AppLogger.LogSubstep($"[{capacity.Sku}] {capacity.DisplayName} (ID={capacity.Id})");
    }

  }

  private static void OpenWorkspaceInBrowser(Guid WorkspaceId) {
    OpenWorkspaceInBrowser(WorkspaceId.ToString());
  }

  private static void OpenWorkspaceInBrowser(string WorkspaceId) {

    if (!AppLogger.RunInNonInteractiveBatchMode) {
      string url = "https://app.powerbi.com/groups/" + WorkspaceId;
      string chromeBrowserProfileName = "Profile 7";
      var process = new Process();
      if (File.Exists(@"C:\Program Files\Google\Chrome\Application\chrome.exe")) {
        process.StartInfo = new ProcessStartInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
        process.StartInfo.Arguments = url + $" --profile-directory=\"{chromeBrowserProfileName}\" ";
      }
      else {
        process.StartInfo = new ProcessStartInfo { FileName = url, UseShellExecute = true };
      }
      process.Start();
    }
  }

  public static void DisplayDeploymentParameters(DeploymentPlan Deployment) {
    if ((Deployment.Parameters != null) &&
      (Deployment.Parameters.Count > 0)) {
      AppLogger.LogTableHeader("Loading parameters from deployment plan");
      foreach (var parameter in Deployment.Parameters) {
        AppLogger.LogTableRow(parameter.Key, parameter.Value);
      }
    }
  }

  public static void AddSalesReportToCustomerWorkspace(string TargetWorkspace, string ReportDefinitionFolder) {

    var workspace = FabricRestApi.GetWorkspaceByName(TargetWorkspace);
    var workspaceModels = FabricRestApi.GetWorkspaceItems(workspace.Id, ItemType.SemanticModel);

    List<string> targetSemanticModelNames = new List<string> {
      "Product Sales DirectLake Model",
      "Product Sales Imported Model"
    };

    foreach (var model in workspaceModels) {
      if (targetSemanticModelNames.Contains(model.DisplayName)) {

        var createReportRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(ReportDefinitionFolder);
        AppLogger.LogStep($"Creating [{createReportRequest.DisplayName}.Report]");
        AppLogger.LogSubstep($"Found target sales model [{model.DisplayName}.Semantic]");

        createReportRequest.Definition =
          ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReportRequest.Definition, model.Id.Value);

        var report = FabricRestApi.CreateItem(workspace.Id, createReportRequest);
        AppLogger.LogSubstep($"Report created with Id of [{report.Id.Value.ToString()}]");

        return;
      }
    }

    throw new ApplicationException("Could not find sales semantic model for report");
  }

  public static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  #endregion

  #region Lab 01 - Deploy Solution From Item Definitions

  public static Workspace DeployPowerBiSolution(string TargetWorkspaceName) {

    string semanticModelName = "Product Sales Imported Model";
    string reportName = "Product Sales Summary";

    AppLogger.LogSolution("Deploy Power BI Solution with Imported Semantic Model and Report");

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{workspace.Id}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, TargetWorkspaceName + " v1.0");

    AppLogger.LogStep($"Creating [{semanticModelName}.SemanticModel]");
    var modelCreateRequest = ItemDefinitionFactory.GetSemanticModelCreateRequestFromBim(semanticModelName, "sales_model_import.bim");
    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);
    AppLogger.LogSubstep($"New semantic model created with Id of [{model.Id.Value.ToString()}]");

    AppLogger.LogSubstep($"Creating Web connection for semantic model");
    var url = PowerBiRestApi.GetWebDatasourceUrl(workspace.Id, model.Id.Value);
    var connection = FabricRestApi.CreateAnonymousWebConnection(url, workspace);

    AppLogger.LogSubstep($"Binding connection to semantic model");
    PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, connection.Id);

    AppLogger.LogSubOperationStart($"Refreshing semantic model");
    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep($"Creating [{reportName}.Report]");

    var createRequestReport =
      ItemDefinitionFactory.GetReportCreateRequestFromReportJson(model.Id.Value, reportName, "product_sales_summary.json");

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogSubstep($"New report created with Id of [{report.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(workspace.Id);

    return workspace;
  }

  public static void DeployNotebookSolution(string TargetWorkspaceName) {

    string lakehouseName = "sales";
    string semanticModelName = "Product Sales DirectLake Model";
    string reportName = "Product Sales Summary";

    AppLogger.LogSolution("Deploy Lakehouse Solution with Notebook");

    AppLogger.LogStep($"Creating new workspace [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName, AppSettings.FabricCapacityId);
    AppLogger.LogSubstep($"Workspace created with Id of [{workspace.Id.ToString()}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, TargetWorkspaceName + " v1.0");

    // create connection to track Web Url for redirects
    string defaultWebUrl = "https://fabricdevcamp.blob.core.windows.net/sampledata/ProductSales/Dev";
    var connection = FabricRestApi.CreateAnonymousWebConnection(defaultWebUrl, workspace);

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    // create and run notebook to build bronze layer
    string notebook1Name = "Create Lakehouse Tables";
    AppLogger.LogStep($"Creating [{notebook1Name}.Notebook]");
    var notebook1CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequestFromPy(workspace.Id, lakehouse, notebook1Name, "CreateLakehouseTables.py");
    var notebook1 = FabricRestApi.CreateItem(workspace.Id, notebook1CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook1.Id.Value.ToString()}]");
    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook1);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    FabricRestApi.RefreshLakehouseTableSchema(sqlEndpoint.Id);

    AppLogger.LogStep($"Creating [{semanticModelName}.SemanticModel]");
    var modelCreateRequest =
      ItemDefinitionFactory.GetSemanticDirectLakeModelCreateRequestFromBim(semanticModelName, "sales_model_DirectLake.bim", sqlEndpoint.ConnectionString, sqlEndpoint.Id);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    CreateAndBindSemanticModelConnecton(workspace, model.Id.Value, lakehouse);

    AppLogger.LogStep($"Creating [{reportName}.Report]");

    var createRequestReport =
      ItemDefinitionFactory.GetReportCreateRequestFromReportJson(model.Id.Value, reportName, "product_sales_summary.json");

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);
    AppLogger.LogSubstep($"Report created with Id of [{report.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void DeployShortcutSolution(string TargetWorkspaceName) {

    string lakehouseName = "sales";
    string semanticModelName = "Product Sales DirectLake Model";
    string report1Name = "Product Sales Summary";
    string report2Name = "Product Sales Time Intelligence";

    AppLogger.LogSolution("Deploy Lakehouse Solution with Shortcut");

    AppLogger.LogStep($"Creating new workspace [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName, AppSettings.FabricCapacityId);
    AppLogger.LogSubstep($"Workspace created with Id of [{workspace.Id.ToString()}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, TargetWorkspaceName + " v1.0");

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    AppLogger.LogStep("Creating ADLS connection for shortcut");
    AppLogger.LogSubstep($"Location: {AppSettings.AzureStorageServer}/{AppSettings.AzureStoragePath}");

    var connection = FabricRestApi.CreateAzureStorageConnectionWithAccountKey(AppSettings.AzureStorageServer,
                                                                              AppSettings.AzureStoragePath,
                                                                              workspace);
    AppLogger.LogSubstep($"Connection created with Id of {connection.Id}");

    // get data required to create shortcut
    string name = "sales-data";
    string path = "Files";
    Uri location = new Uri(AppSettings.AzureStorageServer);
    string shortcutSubpath = AppSettings.AzureStoragePath;

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse] with path [{path}/{name}]");
    var shortcut = FabricRestApi.CreateAdlsGen2Shortcut(workspace.Id, lakehouse.Id.Value, name, path, location, shortcutSubpath, connection.Id);
    AppLogger.LogSubstep($"Shortcut created");

    // create and run notebook to build silver layer
    string notebook1Name = "Create 01 Silver Layer";
    AppLogger.LogStep($"Creating [{notebook1Name}.Notebook]");
    var notebook1CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequestFromPy(workspace.Id, lakehouse, notebook1Name, "BuildSilverLayer.py");
    var notebook1 = FabricRestApi.CreateItem(workspace.Id, notebook1CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook1.Id.Value.ToString()}]");
    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook1);
    AppLogger.LogOperationComplete();

    // create and run notebook to build gold layer
    string notebook2Name = "Create 02 Gold Layer";
    AppLogger.LogStep($"Creating [{notebook2Name}.Notebook]");
    var notebook2CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequestFromPy(workspace.Id, lakehouse, notebook2Name, "BuildGoldLayer.py");
    var notebook2 = FabricRestApi.CreateItem(workspace.Id, notebook2CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook2.Id.Value.ToString()}]");
    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook2);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    AppLogger.LogSubstep("Refreshing lakehouse table schema");
    FabricRestApi.RefreshLakehouseTableSchema(sqlEndpoint.Id);

    AppLogger.LogStep($"Creating [{semanticModelName}.SemanticModel]");
    var modelCreateRequest = ItemDefinitionFactory.GetSemanticDirectLakeModelCreateRequestFromBim(semanticModelName,
                                                                                                  "sales_model_DirectLake.bim",
                                                                                                  sqlEndpoint.ConnectionString,
                                                                                                  sqlEndpoint.Id);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);
    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    CreateAndBindSemanticModelConnecton(workspace, model.Id.Value, lakehouse);

    AppLogger.LogStep($"Creating [{report1Name}.Report]");
    var createRequestReport1 =
      ItemDefinitionFactory.GetReportCreateRequestFromReportJson(model.Id.Value, report1Name, "product_sales_summary.json");

    var report1 = FabricRestApi.CreateItem(workspace.Id, createRequestReport1);
    AppLogger.LogSubstep($"Report created with Id of [{report1.Id.Value.ToString()}]");

    AppLogger.LogStep($"Creating [{report2Name}.Report]");
    var createRequestReport2 =
      ItemDefinitionFactory.GetReportCreateRequestFromReportJson(model.Id.Value, report2Name, "product_sales_time_intelligence.json");

    var report2 = FabricRestApi.CreateItem(workspace.Id, createRequestReport2);
    AppLogger.LogSubstep($"Report created with Id of [{report2.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void DeployDataPipelineSolution(string TargetWorkspaceName) {

    string lakehouseName = "sales";
    string semanticModelName = "Product Sales DirectLake Model";
    string report1Name = "Product Sales Summary";
    string report2Name = "Product Sales Time Intelligence";
    string report3Name = "Product Sales Top 10 Cities";

    AppLogger.LogSolution("Deploy Lakehouse Solution with Data Pipeline");

    AppLogger.LogStep($"Creating new workspace [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName, AppSettings.FabricCapacityId);
    AppLogger.LogSubstep($"Workspace created with Id of [{workspace.Id.ToString()}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, TargetWorkspaceName + " v1.0");

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    // create and run notebook to build silver layer
    string notebook1Name = "Build 01 Silver Layer";
    AppLogger.LogStep($"Creating [{notebook1Name}.Notebook]");
    var notebook1CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequestFromPy(workspace.Id, lakehouse, notebook1Name, "BuildSilverLayer.py");
    var notebook1 = FabricRestApi.CreateItem(workspace.Id, notebook1CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook1.Id.Value.ToString()}]");

    // create and run notebook to build gold layer
    string notebook2Name = "Build 02 Gold Layer";
    AppLogger.LogStep($"Creating [{notebook2Name}.Notebook]");
    var notebook2CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequestFromPy(workspace.Id, lakehouse, notebook2Name, "BuildGoldLayer.py");
    var notebook2 = FabricRestApi.CreateItem(workspace.Id, notebook2CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook2.Id.Value.ToString()}]");

    AppLogger.LogStep("Creating ADLS connection for data pipeline");
    AppLogger.LogSubstep($"Location: {AppSettings.AzureStorageServer}/{AppSettings.AzureStoragePath}");

    var connection = FabricRestApi.CreateAzureStorageConnectionWithAccountKey(AppSettings.AzureStorageServer,
                                                                              AppSettings.AzureStoragePath,
                                                                              workspace);
    AppLogger.LogSubstep($"Connection created with Id of {connection.Id}");

    string pipelineName = "Create Lakehouse Tables";
    AppLogger.LogStep($"Creating [{pipelineName}.DataPipline]");

    string pipelineDefinitionTemplate = ItemDefinitionFactory.GetTemplateFile(@"DataPipelines\CreateLakehouseTables.json");
    string pipelineDefinition = pipelineDefinitionTemplate.Replace("{WORKSPACE_ID}", workspace.Id.ToString())
                                                          .Replace("{LAKEHOUSE_ID}", lakehouse.Id.Value.ToString())
                                                          .Replace("{CONNECTION_ID}", connection.Id.ToString())
                                                          .Replace("{CONTAINER_NAME}", AppSettings.AzureStorageContainer)
                                                          .Replace("{CONTAINER_PATH}", AppSettings.AzureStorageContainerPath)
                                                          .Replace("{NOTEBOOK_ID_BUILD_SILVER}", notebook1.Id.Value.ToString())
                                                          .Replace("{NOTEBOOK_ID_BUILD_GOLD}", notebook2.Id.Value.ToString());

    var pipelineCreateRequest = ItemDefinitionFactory.GetDataPipelineCreateRequest(pipelineName, pipelineDefinition);
    var pipeline = FabricRestApi.CreateItem(workspace.Id, pipelineCreateRequest);

    AppLogger.LogSubstep($"DataPipline created with Id [{pipeline.Id.Value.ToString()}]");

    AppLogger.LogSubOperationStart($"Running data pipeline");
    FabricRestApi.RunDataPipeline(workspace.Id, pipeline);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    AppLogger.LogSubstep("Refreshing lakehouse table schema");
    FabricRestApi.RefreshLakehouseTableSchema(sqlEndpoint.Id);

    AppLogger.LogStep($"Creating [{semanticModelName}.SemanticModel]");
    var modelCreateRequest = ItemDefinitionFactory.GetSemanticDirectLakeModelCreateRequestFromBim(semanticModelName,
                                                                                                  "sales_model_DirectLake.bim",
                                                                                                  sqlEndpoint.ConnectionString,
                                                                                                  sqlEndpoint.Id);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);
    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    CreateAndBindSemanticModelConnecton(workspace, model.Id.Value, lakehouse);

    AppLogger.LogStep($"Creating [{report1Name}.Report]");
    var createRequestReport1 =
      ItemDefinitionFactory.GetReportCreateRequestFromReportJson(model.Id.Value, report1Name, "product_sales_summary.json");

    var report1 = FabricRestApi.CreateItem(workspace.Id, createRequestReport1);
    AppLogger.LogSubstep($"Report created with Id of [{report1.Id.Value.ToString()}]");

    AppLogger.LogStep($"Creating [{report2Name}.Report]");
    var createRequestReport2 =
      ItemDefinitionFactory.GetReportCreateRequestFromReportJson(model.Id.Value, report2Name, "product_sales_time_intelligence.json");

    var report2 = FabricRestApi.CreateItem(workspace.Id, createRequestReport2);
    AppLogger.LogSubstep($"Report created with Id of [{report2.Id.Value.ToString()}]");

    AppLogger.LogStep($"Creating [{report3Name}.Report]");
    var createRequestReport3 =
      ItemDefinitionFactory.GetReportCreateRequestFromReportJson(model.Id.Value, report3Name, "product_sales_top_ten_cities_report.json");

    var report3 = FabricRestApi.CreateItem(workspace.Id, createRequestReport3);
    AppLogger.LogSubstep($"Report created with Id of [{report3.Id.Value.ToString()}]");


    AppLogger.LogStep("Solution deployment complete");

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CreateAndBindSemanticModelConnecton(Workspace Workspace, Guid SemanticModelId, Item Lakehouse = null) {

    var datasources = PowerBiRestApi.GetDatasourcesForSemanticModel(Workspace.Id, SemanticModelId);

    foreach (var datasource in datasources) {

      if (datasource.DatasourceType.ToLower() == "sql") {

        string sqlEndPointServer = datasource.ConnectionDetails.Server;
        string sqlEndPointDatabase = datasource.ConnectionDetails.Database;

        // you cannot create the connection until your configure a service principal
        if (AppSettings.ServicePrincipalObjectId != "00000000-0000-0000-0000-000000000000") {
          AppLogger.LogSubstep($"Creating connection for semantic model");
          var sqlConnection = FabricRestApi.CreateSqlConnectionWithServicePrincipal(sqlEndPointServer, sqlEndPointDatabase, Workspace, Lakehouse);
          AppLogger.LogSubstep($"Binding connection to semantic model");
          PowerBiRestApi.BindSemanticModelToConnection(Workspace.Id, SemanticModelId, sqlConnection.Id);
        }
        else {
          AppLogger.LogSubstep("Semantic model will use default authentication mode of SSO");
        }

      }

      if (datasource.DatasourceType.ToLower() == "web") {
        string url = datasource.ConnectionDetails.Url;

        AppLogger.LogSubstep($"Creating Web connection for semantic model");
        var webConnection = FabricRestApi.CreateAnonymousWebConnection(url, Workspace);

        AppLogger.LogSubstep($"Binding connection to semantic model");
        PowerBiRestApi.BindSemanticModelToConnection(Workspace.Id, SemanticModelId, webConnection.Id);

        AppLogger.LogSubOperationStart($"Refreshing semantic model");
        PowerBiRestApi.RefreshDataset(Workspace.Id, SemanticModelId);
        AppLogger.LogOperationComplete();

      }

    }
  }

  #endregion

  #region Lab 02 - Export Item Definitions From Workspace

  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {
    ItemDefinitionFactory.ExportItemDefinitionsFromWorkspace(WorkspaceName);
  }

  #endregion

  #region Lab 03 - Parameterize Datasource Paths

  public static ItemDefinition CustomizeReportTitle(CreateItemRequest CreateRequest, DeploymentPlan Deployment) {
    return CustomizeReportTitle(CreateRequest.Definition, CreateRequest.DisplayName, Deployment);
  }

  public static ItemDefinition CustomizeReportTitle(ItemDefinition ReportDefinition, string ReportDisplayName, DeploymentPlan Deployment) {

    string[] stagedDeploymentNames = ["Dev", "Test", "Prod"];

    // don't customize report titles for staged deployments
    if (Deployment.DeploymentType == DeploymentPlanType.CustomerTenantDeployment) {

      var reportTitleRedirect = new Dictionary<string, string>();
      reportTitleRedirect.Add(ReportDisplayName, $"{Deployment.Name} {ReportDisplayName}");

      return ItemDefinitionFactory.UpdateItemDefinitionPart(ReportDefinition,
                                                            "report.json",
                                                            reportTitleRedirect);
    }
    else {
      return ReportDefinition;
    }

  }

  public static Workspace DeployPowerBiSolution(string TargetWorkspaceName, DeploymentPlan Deployment) {

    AppLogger.LogSolution("Deploy Power BI Solution with Deployment Parameters");

    DisplayDeploymentParameters(Deployment);

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{workspace.Id}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, Deployment.Description);

    string modelDefinitionFolder = "Product Sales Imported Model.SemanticModel";
    var createModelRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(modelDefinitionFolder);
    AppLogger.LogStep($"Creating [{createModelRequest.DisplayName}.SemanticModel]");

    if (Deployment.Parameters.ContainsKey(DeploymentPlan.webDatasourcePathParameter)) {

      var webUrl = Deployment.Parameters[DeploymentPlan.webDatasourcePathParameter];
      var semanticModelRedirects = new Dictionary<string, string>() {
        {"{WEB_DATASOURCE_PATH}", webUrl }
      };

      createModelRequest.Definition =
        ItemDefinitionFactory.UpdateItemDefinitionPart(createModelRequest.Definition,
                                                       "definition/expressions.tmdl",
                                                       semanticModelRedirects);
    }

    var model = FabricRestApi.CreateItem(workspace.Id, createModelRequest);
    AppLogger.LogSubstep($"New semantic model created with Id of [{model.Id.Value.ToString()}]");

    CreateAndBindSemanticModelConnecton(workspace, model.Id.Value);

    string reportDefinitionFolder = "Product Sales Summary.Report";
    var createReportRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(reportDefinitionFolder);
    AppLogger.LogStep($"Creating [{createReportRequest.DisplayName}.Report]");

    AppLogger.LogSubstep("Customizing report title header with customer name");
    createReportRequest.Definition = CustomizeReportTitle(createReportRequest, Deployment);

    createReportRequest.Definition = ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReportRequest.Definition, model.Id.Value);

    var report = FabricRestApi.CreateItem(workspace.Id, createReportRequest);
    AppLogger.LogSubstep($"Report created with Id of [{report.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    return workspace;

  }

  public static Workspace DeployNotebookSolution(string TargetWorkspaceName, DeploymentPlan Deployment) {

    string lakehouseName = "sales";

    AppLogger.LogSolution("Deploy Notebook Solution with Deployment Parameters");

    DisplayDeploymentParameters(Deployment);
    var parameters = Deployment.Parameters;

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{workspace.Id}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, Deployment.Description);

    // create connection to track Web Url for redirects
    string defaultWebUrl = "https://fabricdevcamp.blob.core.windows.net/sampledata/ProductSales/Dev";
    var connection = FabricRestApi.CreateAnonymousWebConnection(defaultWebUrl, workspace);

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    // create and run notebook to build bronze layer
    string notebook1Name = "Create Lakehouse Tables";
    AppLogger.LogStep($"Creating [{notebook1Name}.Notebook]");

    var notebookCreateRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder("Create Lakehouse Tables.Notebook");

    var notebookRedirects = new Dictionary<string, string>() {
        {"{WORKSPACE_ID}", workspace.Id.ToString()},
        {"{LAKEHOUSE_ID}", lakehouse.Id.Value.ToString() },
        {"{LAKEHOUSE_NAME}", lakehouse.DisplayName }
      };

    if (Deployment.Parameters.ContainsKey(DeploymentPlan.webDatasourcePathParameter)) {

      AppLogger.LogSubstep($"Updating Web URL in notebook definition");
      var webUrl = Deployment.Parameters[DeploymentPlan.webDatasourcePathParameter];
      notebookRedirects.Add("{WEB_DATASOURCE_PATH}", webUrl);

      notebookCreateRequest.Definition =
        ItemDefinitionFactory.UpdateItemDefinitionPart(notebookCreateRequest.Definition,
                                                       "notebook-content.py",
                                                       notebookRedirects);
    }

    var notebook = FabricRestApi.CreateItem(workspace.Id, notebookCreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook.Id.Value.ToString()}]");

    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    FabricRestApi.RefreshLakehouseTableSchema(sqlEndpoint.Id);

    string modelDefinitionFolder = "Product Sales DirectLake Model.SemanticModel";
    var createModelRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(modelDefinitionFolder);
    AppLogger.LogStep($"Creating [{createModelRequest.DisplayName}.SemanticModel]");

    var semanticModelRedirects = new Dictionary<string, string>() {
        {"{SQL_ENDPOINT_SERVER}", sqlEndpoint.ConnectionString },
        {"{SQL_ENDPOINT_DATABASE}", sqlEndpoint.Id },
      };

    createModelRequest.Definition =
      ItemDefinitionFactory.UpdateItemDefinitionPart(createModelRequest.Definition,
                                                     "definition/expressions.tmdl",
                                                     semanticModelRedirects);

    var model = FabricRestApi.CreateItem(workspace.Id, createModelRequest);
    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    CreateAndBindSemanticModelConnecton(workspace, model.Id.Value, lakehouse);

    string reportDefinitionFolder = "Product Sales Summary.Report";
    var createReportRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(reportDefinitionFolder);
    AppLogger.LogStep($"Creating [{createReportRequest.DisplayName}.Report]");

    createReportRequest.Definition = ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReportRequest.Definition, model.Id.Value);

    AppLogger.LogSubstep("Customizing report title header with customer name");
    createReportRequest.Definition = CustomizeReportTitle(createReportRequest, Deployment);

    var report = FabricRestApi.CreateItem(workspace.Id, createReportRequest);
    AppLogger.LogSubstep($"Report created with Id of [{report.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    return workspace;

  }

  public static Workspace DeployShortcutSolution(string TargetWorkspaceName, DeploymentPlan Deployment) {

    string lakehouseName = "sales";

    AppLogger.LogSolution("Deploy Shortcut Solution with Deployment Parameters");

    DisplayDeploymentParameters(Deployment);

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{workspace.Id}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, Deployment.Description);

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    string adlsServer = AppSettings.AzureStorageServer;
    string adlsPath = AppSettings.AzureStoragePath;

    if ((Deployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
        (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
        (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

      string adlsContainerName = Deployment.Parameters[DeploymentPlan.adlsContainerNameParameter];
      string adlsContainerPath = Deployment.Parameters[DeploymentPlan.adlsContainerPathParameter];
      adlsServer = Deployment.Parameters[DeploymentPlan.adlsServerPathParameter];

      adlsPath = $@"/{adlsContainerName}{adlsContainerPath}";

    }

    AppLogger.LogStep($"Creating ADLS connection");
    AppLogger.LogSubstep($"Path: {adlsServer}/{adlsPath}");
    var connection = FabricRestApi.CreateAzureStorageConnectionWithAccountKey(adlsServer,
                                                                              adlsPath,
                                                                              workspace);

    AppLogger.LogSubstep($"Connection created with Id of [{connection.Id}]");

    // get data required to create shortcut
    string name = "sales-data";
    string path = "Files";
    Uri location = new Uri(adlsServer);
    string shortcutSubpath = adlsPath;

    AppLogger.LogStep("Creating OneLake Shortcut with ADLS connection to provide access to bonze layer data files");
    var shortcut = FabricRestApi.CreateAdlsGen2Shortcut(workspace.Id, lakehouse.Id.Value, name, path, location, shortcutSubpath, connection.Id);
    AppLogger.LogSubstep($"Shortcut created successfully");

    // set up redirect for all notebooks
    var notebookRedirects = new Dictionary<string, string>() {
        {"{WORKSPACE_ID}", workspace.Id.ToString()},
        {"{LAKEHOUSE_ID}", lakehouse.Id.Value.ToString() },
        {"{LAKEHOUSE_NAME}", lakehouse.DisplayName }
      };

    // create notebook to build silver layer
    string notebook1DefinitionFolder = "Create 01 Silver Layer.Notebook";
    var createNotebook1Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(notebook1DefinitionFolder);
    createNotebook1Request.Definition =
        ItemDefinitionFactory.UpdateItemDefinitionPart(createNotebook1Request.Definition,
                                                       "notebook-content.py",
                                                       notebookRedirects);

    AppLogger.LogStep($"Creating [{createNotebook1Request.DisplayName}.Notebook]");
    var notebook1 = FabricRestApi.CreateItem(workspace.Id, createNotebook1Request);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook1.Id.Value.ToString()}]");

    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook1);
    AppLogger.LogOperationComplete();

    // create notebook to build gold layer
    string notebook2DefinitionFolder = "Create 02 Gold Layer.Notebook";
    var createNotebook2Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(notebook2DefinitionFolder);
    createNotebook2Request.Definition =
        ItemDefinitionFactory.UpdateItemDefinitionPart(createNotebook2Request.Definition,
                                                       "notebook-content.py",
                                                       notebookRedirects);

    AppLogger.LogStep($"Creating [{createNotebook2Request.DisplayName}.Notebook]");
    var notebook2 = FabricRestApi.CreateItem(workspace.Id, createNotebook2Request);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook2.Id.Value.ToString()}]");

    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook2);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    AppLogger.LogSubstep("Refreshing lakehouse table schema");
    FabricRestApi.RefreshLakehouseTableSchema(sqlEndpoint.Id);

    string modelDefinitionFolder = "Product Sales DirectLake Model.SemanticModel";
    var createModelRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(modelDefinitionFolder);
    AppLogger.LogStep($"Creating [{createModelRequest.DisplayName}.SemanticModel]");

    var semanticModelRedirects = new Dictionary<string, string>() {
        {"{SQL_ENDPOINT_SERVER}", sqlEndpoint.ConnectionString },
        {"{SQL_ENDPOINT_DATABASE}", sqlEndpoint.Id },
      };

    createModelRequest.Definition =
      ItemDefinitionFactory.UpdateItemDefinitionPart(createModelRequest.Definition,
                                                     "definition/expressions.tmdl",
                                                     semanticModelRedirects);

    var model = FabricRestApi.CreateItem(workspace.Id, createModelRequest);
    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    CreateAndBindSemanticModelConnecton(workspace, model.Id.Value, lakehouse);

    string report1DefinitionFolder = "Product Sales Summary.Report";
    var createReport1Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(report1DefinitionFolder);
    AppLogger.LogStep($"Creating [{createReport1Request.DisplayName}.Report]");
    createReport1Request.Definition =
      ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReport1Request.Definition, model.Id.Value);
    AppLogger.LogSubstep("Customizing report title header with customer name");
    createReport1Request.Definition = CustomizeReportTitle(createReport1Request, Deployment);
    var report1 = FabricRestApi.CreateItem(workspace.Id, createReport1Request);
    AppLogger.LogSubstep($"Report created with Id of [{report1.Id.Value.ToString()}]");

    string report2DefinitionFolder = "Product Sales Time Intelligence.Report";
    var createReport2Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(report2DefinitionFolder);
    AppLogger.LogStep($"Creating [{createReport2Request.DisplayName}.Report]");
    createReport2Request.Definition =
      ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReport2Request.Definition, model.Id.Value);
    AppLogger.LogSubstep("Customizing report title header with customer name");
    createReport2Request.Definition = CustomizeReportTitle(createReport2Request, Deployment);
    var report2 = FabricRestApi.CreateItem(workspace.Id, createReport2Request);
    AppLogger.LogSubstep($"Report created with Id of [{report2.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    return workspace;

  }

  public static Workspace DeployDataPipelineSolution(string TargetWorkspaceName, DeploymentPlan Deployment) {

    string lakehouseName = "sales";

    AppLogger.LogSolution("Deploy Data Pipeline Solution with Deployment Parameters");

    DisplayDeploymentParameters(Deployment);

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{workspace.Id}]");

    FabricRestApi.UpdateWorkspaceDescription(workspace.Id, Deployment.Description);

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    // set up redirect for all notebooks
    var notebookRedirects = new Dictionary<string, string>() {
        {"{WORKSPACE_ID}", workspace.Id.ToString()},
        {"{LAKEHOUSE_ID}", lakehouse.Id.Value.ToString() },
        {"{LAKEHOUSE_NAME}", lakehouse.DisplayName }
      };

    // create notebook to build silver layer
    string notebook1DefinitionFolder = "Build 01 Silver Layer.Notebook";
    var createNotebook1Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(notebook1DefinitionFolder);
    createNotebook1Request.Definition =
        ItemDefinitionFactory.UpdateItemDefinitionPart(createNotebook1Request.Definition,
                                                       "notebook-content.py",
                                                       notebookRedirects);

    AppLogger.LogStep($"Creating [{createNotebook1Request.DisplayName}.Notebook]");
    var notebook1 = FabricRestApi.CreateItem(workspace.Id, createNotebook1Request);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook1.Id.Value.ToString()}]");

    // create notebook to build gold layer
    string notebook2DefinitionFolder = "Build 02 Gold Layer.Notebook";
    var createNotebook2Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(notebook2DefinitionFolder);
    createNotebook2Request.Definition =
        ItemDefinitionFactory.UpdateItemDefinitionPart(createNotebook2Request.Definition,
                                                       "notebook-content.py",
                                                       notebookRedirects);

    AppLogger.LogStep($"Creating [{createNotebook2Request.DisplayName}.Notebook]");
    var notebook2 = FabricRestApi.CreateItem(workspace.Id, createNotebook2Request);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook2.Id.Value.ToString()}]");

    // create connection for data pipeline
    string adlsServer = AppSettings.AzureStorageServer;
    string adlsPath = AppSettings.AzureStoragePath;
    string adlsContainerName = AppSettings.AzureStorageContainer;
    string adlsContainerPath = AppSettings.AzureStorageContainerPath;

    if ((Deployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
        (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
        (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

      adlsContainerName = Deployment.Parameters[DeploymentPlan.adlsContainerNameParameter];
      adlsContainerPath = Deployment.Parameters[DeploymentPlan.adlsContainerPathParameter];

      adlsServer = Deployment.Parameters[DeploymentPlan.adlsServerPathParameter];
      adlsPath = adlsContainerName + adlsContainerPath;

    }

    AppLogger.LogStep($"Creating ADLS connection");
    AppLogger.LogSubstep($"Path: {adlsServer}/{adlsPath}");

    var connection = FabricRestApi.CreateAzureStorageConnectionWithAccountKey(adlsServer,
                                                                              adlsPath,
                                                                              workspace);

    AppLogger.LogSubstep($"Connection created with Id of {connection.Id}");

    string pipelineDefinitionFolder = "Create Lakehouse Tables.DataPipeline";
    var createPipelineRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(pipelineDefinitionFolder);
    AppLogger.LogStep($"Creating [{createPipelineRequest.DisplayName}.DataPipeline]");

    // set up data pipeline redirects
    var pipelineRedirects = new Dictionary<string, string>() {
      { "{WORKSPACE_ID}", workspace.Id.ToString() },
      { "{LAKEHOUSE_ID}", lakehouse.Id.Value.ToString() },
      { "{CONNECTION_ID}", connection.Id.ToString() },
      { "{CONTAINER_NAME}", adlsContainerName},
      { "{CONTAINER_PATH}", adlsContainerPath },
      { "{NOTEBOOK_ID_BUILD_SILVER}", notebook1.Id.Value.ToString() },
      { "{NOTEBOOK_ID_BUILD_GOLD}", notebook2.Id.Value.ToString() },
    };

    AppLogger.LogSubstep("Updating ADLS connection data in data pipeline definition");
    createPipelineRequest.Definition =
        ItemDefinitionFactory.UpdateItemDefinitionPart(createPipelineRequest.Definition,
                                                       "pipeline-content.json",
                                                       pipelineRedirects);

    var pipeline = FabricRestApi.CreateItem(workspace.Id, createPipelineRequest);
    AppLogger.LogSubstep($"Data Pipeline created with Id of [{pipeline.Id.Value.ToString()}]");

    AppLogger.LogSubOperationStart($"Running data pipeline");
    FabricRestApi.RunDataPipeline(workspace.Id, pipeline);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    AppLogger.LogSubstep("Refreshing lakehouse table schema");
    FabricRestApi.RefreshLakehouseTableSchema(sqlEndpoint.Id);

    string modelDefinitionFolder = "Product Sales DirectLake Model.SemanticModel";
    var createModelRequest = ItemDefinitionFactory.GetCreateItemRequestFromFolder(modelDefinitionFolder);
    AppLogger.LogStep($"Creating [{createModelRequest.DisplayName}.SemanticModel]");

    var semanticModelRedirects = new Dictionary<string, string>() {
        {"{SQL_ENDPOINT_SERVER}", sqlEndpoint.ConnectionString },
        {"{SQL_ENDPOINT_DATABASE}", sqlEndpoint.Id },
      };

    createModelRequest.Definition =
      ItemDefinitionFactory.UpdateItemDefinitionPart(createModelRequest.Definition,
                                                     "definition/expressions.tmdl",
                                                     semanticModelRedirects);

    var model = FabricRestApi.CreateItem(workspace.Id, createModelRequest);
    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    CreateAndBindSemanticModelConnecton(workspace, model.Id.Value, lakehouse);


    string report1DefinitionFolder = "Product Sales Summary.Report";
    var createReport1Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(report1DefinitionFolder);
    AppLogger.LogStep($"Creating [{createReport1Request.DisplayName}.Report]");
    createReport1Request.Definition =
      ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReport1Request.Definition, model.Id.Value);
    AppLogger.LogSubstep("Customizing report title header with customer name");
    createReport1Request.Definition = CustomizeReportTitle(createReport1Request, Deployment);
    var report1 = FabricRestApi.CreateItem(workspace.Id, createReport1Request);
    AppLogger.LogSubstep($"Report created with Id of [{report1.Id.Value.ToString()}]");

    string report2DefinitionFolder = "Product Sales Time Intelligence.Report";
    var createReport2Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(report2DefinitionFolder);
    AppLogger.LogStep($"Creating [{createReport2Request.DisplayName}.Report]");
    createReport2Request.Definition =
      ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReport2Request.Definition, model.Id.Value);
    AppLogger.LogSubstep("Customizing report title header with customer name");
    createReport2Request.Definition = CustomizeReportTitle(createReport2Request, Deployment);
    var report2 = FabricRestApi.CreateItem(workspace.Id, createReport2Request);
    AppLogger.LogSubstep($"Report created with Id of [{report2.Id.Value.ToString()}]");

    string report3DefinitionFolder = "Product Sales Top 10 Cities.Report";
    var createReport3Request = ItemDefinitionFactory.GetCreateItemRequestFromFolder(report3DefinitionFolder);
    AppLogger.LogStep($"Creating [{createReport3Request.DisplayName}.Report]");
    createReport3Request.Definition =
      ItemDefinitionFactory.UpdateReportDefinitionWithSemanticModelId(createReport3Request.Definition, model.Id.Value);
    AppLogger.LogSubstep("Customizing report title header with customer name");
    createReport3Request.Definition = CustomizeReportTitle(createReport3Request, Deployment);
    var report3 = FabricRestApi.CreateItem(workspace.Id, createReport3Request);
    AppLogger.LogSubstep($"Report created with Id of [{report3.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    return workspace;
  }

  // overloads added for convenience

  public static void DeployPowerBiSolution(DeploymentPlan Deployment) {
    var workspace = DeployPowerBiSolution(Deployment.TargetWorkspaceName, Deployment);
    AppLogger.PromptUserToContinue();
    OpenWorkspaceInBrowser(workspace.Id);
  }

  public static void DeployNotebookSolution(DeploymentPlan Deployment) {
    var workspace = DeployNotebookSolution(Deployment.TargetWorkspaceName, Deployment);
    AppLogger.PromptUserToContinue();
    OpenWorkspaceInBrowser(workspace.Id);
  }

  public static void DeployShortcutSolution(DeploymentPlan Deployment) {
    var workspace = DeployShortcutSolution(Deployment.TargetWorkspaceName, Deployment);
    AppLogger.PromptUserToContinue();
    OpenWorkspaceInBrowser(workspace.Id);
  }

  public static void DeployDataPipelineSolution(DeploymentPlan Deployment) {
    var workspace = DeployDataPipelineSolution(Deployment.TargetWorkspaceName, Deployment);
    AppLogger.PromptUserToContinue();
    OpenWorkspaceInBrowser(workspace.Id);
  }

  #endregion

  #region Lab 04 - Deploy Solution From Source Workspace

  public static Dictionary<string, string> RecreateWorkspaceConnections(Workspace SourceWorkspace, Workspace TargetWorkspace, DeploymentPlan Deployment) {

    var workspaceConnections = FabricRestApi.GetWorkspaceConnections(SourceWorkspace.Id);

    if (workspaceConnections.Where(conn => !conn.DisplayName.Contains("Lakehouse")).ToList().Count > 0) {
      AppLogger.LogStep("Recreating connections found in source workspace");
    }

    var connectionRedirects = new Dictionary<string, string>();

    foreach (var sourceConnection in workspaceConnections) {

      // ignore lakehouse connections
      if (!sourceConnection.DisplayName.Contains("Lakehouse")) {

        Connection targetConnection = null;

        switch (sourceConnection.ConnectionDetails.Type) {

          case "Web":
            string sourceWebUrl = sourceConnection.ConnectionDetails.Path;
            string targetWebUrl = sourceWebUrl;

            if (Deployment.Parameters.ContainsKey(DeploymentPlan.webDatasourcePathParameter)) {
              targetWebUrl = Deployment.Parameters[DeploymentPlan.webDatasourcePathParameter];
            }

            AppLogger.LogSubstep($"Web: {targetWebUrl}");
            targetConnection = FabricRestApi.CreateAnonymousWebConnection(targetWebUrl, TargetWorkspace);

            // redirect connection Id
            connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

            // redirect connection path
            connectionRedirects.Add(sourceWebUrl, targetWebUrl);

            break;

          case "AzureDataLakeStorage":
            string sourceAdlsConnectionPath = sourceConnection.ConnectionDetails.Path;
            string sourceAdlsServer = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[0] + "dfs.core.windows.net";
            string sourceAdlsPath = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[1];

            int pathSlash = sourceAdlsPath.Substring(1).IndexOf("/");
            string sourceAdlsContainerName = sourceAdlsPath.Substring(1, pathSlash);
            string sourceAdlsContainerPath = sourceAdlsPath.Substring(pathSlash + 1);

            string targetAdlsServer = sourceAdlsServer;
            string targetAdlsPath = sourceAdlsPath;
            string targetAdlsContainerName = sourceAdlsContainerName;
            string targetAdlsContainerPath = sourceAdlsContainerPath;

            if ((Deployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
                (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
                (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

              targetAdlsServer = Deployment.Parameters[DeploymentPlan.adlsServerPathParameter];
              targetAdlsContainerName = Deployment.Parameters[DeploymentPlan.adlsContainerNameParameter];
              targetAdlsContainerPath = Deployment.Parameters[DeploymentPlan.adlsContainerPathParameter];
              targetAdlsPath = "/" + targetAdlsContainerName + targetAdlsContainerPath;
            }

            AppLogger.LogSubstep($"ADLS: {targetAdlsServer}{targetAdlsPath}");
            targetConnection = FabricRestApi.CreateAzureStorageConnectionWithAccountKey(targetAdlsServer, targetAdlsPath, TargetWorkspace);

            // redirect connection Id
            connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

            // redirect connection path
            connectionRedirects.Add(sourceAdlsServer, targetAdlsServer);
            connectionRedirects.Add(sourceAdlsContainerName, targetAdlsContainerName);
            connectionRedirects.Add(sourceAdlsContainerPath, targetAdlsContainerPath);

            break;

          default:
            throw new ApplicationException("Unexpected connection type");
        }
      }
    }

    return connectionRedirects;

  }

  public static void DeployFromSourceWorkspace(string SourceWorkspaceName, string TargetWorkspaceName, DeploymentPlan Deployment) {

    AppLogger.LogSolution($"Deploying solution from source workspace [{SourceWorkspaceName}] to [{TargetWorkspaceName}]");

    DisplayDeploymentParameters(Deployment);

    // create data collections to track substitution data
    var connectionRedirects = new Dictionary<string, string>();
    var sqlEndPointIds = new Dictionary<string, Item>();
    var lakehouseNames = new List<string>();
    var shortcutRedirects = new Dictionary<string, string>();
    var notebookRedirects = new Dictionary<string, string>();
    var dataPipelineRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var targetWorkspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{targetWorkspace.Id}]");

    var sourceWorkspaceInfo = FabricRestApi.GetWorkspaceInfo(sourceWorkspace.Id);
    FabricRestApi.UpdateWorkspaceDescription(targetWorkspace.Id, sourceWorkspaceInfo.Description);

    // add connection redirect for deployment pipelines
    connectionRedirects = RecreateWorkspaceConnections(sourceWorkspace, targetWorkspace, Deployment);

    // make deep copy of connectionRedirects for starting point for other redirects
    shortcutRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    notebookRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    semanticModelRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    dataPipelineRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;

    // add redirects for workspace id
    notebookRedirects.Add(sourceWorkspace.Id.ToString(), targetWorkspace.Id.ToString());
    dataPipelineRedirects.Add(sourceWorkspace.Id.ToString(), targetWorkspace.Id.ToString());

    AppLogger.LogStep($"Deploying Workspace Items");

    var lakehouses = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Lakehouse");
    foreach (var sourceLakehouse in lakehouses) {

      Guid sourceLakehouseId = sourceLakehouse.Id.Value;
      var sourceLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(sourceWorkspace.Id, sourceLakehouse.Id.Value);

      AppLogger.LogSubstep($"Creating [{sourceLakehouse.DisplayName}.Lakehouse]");
      var targetLakehouse = FabricRestApi.CreateLakehouse(targetWorkspace.Id, sourceLakehouse.DisplayName);

      var targetLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(targetWorkspace.Id, targetLakehouse.Id.Value);

      // add lakehouse names and Ids to detect lakehouse default semantic model
      lakehouseNames.Add(targetLakehouse.DisplayName);
      sqlEndPointIds.Add(targetLakehouseSqlEndpoint.Id, targetLakehouse);

      // add redirect for lakehouse id
      notebookRedirects.Add(sourceLakehouse.Id.Value.ToString(), targetLakehouse.Id.Value.ToString());
      dataPipelineRedirects.Add(sourceLakehouse.Id.Value.ToString(), targetLakehouse.Id.Value.ToString());

      // add redirect for sql endpoint database name 
      semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.Id, targetLakehouseSqlEndpoint.Id);

      // add redirect for sql endpoint server location
      if (!semanticModelRedirects.Keys.Contains(sourceLakehouseSqlEndpoint.ConnectionString)) {
        // only add sql endpoint server location once because it has same value for all lakehouses in the same workspace
        semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.ConnectionString, targetLakehouseSqlEndpoint.ConnectionString);
      }

      // copy shortcuts
      var shortcuts = FabricRestApi.GetLakehouseShortcuts(sourceWorkspace.Id, sourceLakehouseId);
      foreach (var shortcut in shortcuts) {

        if (shortcut.Target.Type == Microsoft.Fabric.Api.Core.Models.Type.AdlsGen2) {

          string sourceShortcutName = shortcut.Name;
          string sourceShortcutPath = shortcut.Path;
          string sourceShortcutLocation = shortcut.Target.AdlsGen2.Location.ToString();
          string sourceShortcutSubpath = shortcut.Target.AdlsGen2.Subpath;

          string targetShortcutName = sourceShortcutName;
          string targetShortcutPath = sourceShortcutPath;
          string targetShortcutLocation = sourceShortcutLocation;
          string targetShortcutSubpath = sourceShortcutSubpath;


          if ((Deployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
              (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
              (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

            targetShortcutLocation = Deployment.Parameters[DeploymentPlan.adlsServerPathParameter];

            targetShortcutSubpath = "/" + Deployment.Parameters[DeploymentPlan.adlsContainerNameParameter] +
                                           Deployment.Parameters[DeploymentPlan.adlsContainerPathParameter];

          }

          Guid targetConnectionId = new Guid(connectionRedirects[shortcut.Target.AdlsGen2.ConnectionId.ToString()]);

          //var uriLocation = new Uri(location);

          AppLogger.LogSubstep($"Creating [{targetLakehouse.DisplayName}.{targetLakehouse.Type}.Shortcut] with path of [{targetShortcutPath.Substring(1)}/{targetShortcutName}]");
          FabricRestApi.CreateAdlsGen2Shortcut(targetWorkspace.Id,
                                               targetLakehouse.Id.Value,
                                               targetShortcutName,
                                               targetShortcutPath,
                                               new Uri(targetShortcutLocation),
                                               targetShortcutSubpath,
                                               targetConnectionId);

        }

      }
    }

    var notebooks = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Notebook");
    foreach (var sourceNotebook in notebooks) {
      AppLogger.LogSubstep($"Creating [{sourceNotebook.DisplayName}.Notebook]");
      var createRequest = new CreateItemRequest(sourceNotebook.DisplayName, sourceNotebook.Type);

      var notebookDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceNotebook.Id.Value);

      createRequest.Definition = ItemDefinitionFactory.UpdateItemDefinitionPart(notebookDefinition,
                                                                        "notebook-content.py",
                                                                        notebookRedirects);

      var targetNotebook = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

      dataPipelineRedirects.Add(sourceNotebook.Id.Value.ToString(), targetNotebook.Id.Value.ToString());

      if (createRequest.DisplayName.Contains("Create")) {
        AppLogger.LogSubOperationStart($"Running  [{sourceNotebook.DisplayName}.Notebook]");
        FabricRestApi.RunNotebook(targetWorkspace.Id, targetNotebook);
        AppLogger.LogOperationComplete();
      }

    }

    var pipelines = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "DataPipeline");
    foreach (var sourcePipeline in pipelines) {

      AppLogger.LogSubstep($"Creating [{sourcePipeline.DisplayName}.DataPipeline]");
      var createRequest = new CreateItemRequest(sourcePipeline.DisplayName, sourcePipeline.Type);

      var pipelineDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourcePipeline.Id.Value);

      createRequest.Definition = ItemDefinitionFactory.UpdateItemDefinitionPart(pipelineDefinition,
                                                                       "pipeline-content.json",
                                                                        dataPipelineRedirects);

      var targetPipeline = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

      if (createRequest.DisplayName.Contains("Create")) {
        AppLogger.LogSubOperationStart($"Running  [{sourcePipeline.DisplayName}.DataPipeline]");
        FabricRestApi.RunDataPipeline(targetWorkspace.Id, targetPipeline);
        AppLogger.LogOperationComplete();
      }

    }

    foreach (var sqlEndPointId in sqlEndPointIds.Keys) {
      FabricRestApi.RefreshLakehouseTableSchema(sqlEndPointId);
    }

    var models = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in models) {

      // ignore default semantic models for lakehouses
      if (!lakehouseNames.Contains(sourceModel.DisplayName)) {

        AppLogger.LogSubstep($"Creating [{sourceModel.DisplayName}.SemanticModel]");

        // get model definition from source workspace
        var sourceModelDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceModel.Id.Value);

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourceModelDefinition,
                                                                     "definition/expressions.tmdl",
                                                                     semanticModelRedirects);

        // use item definition to create clone in target workspace
        var createRequest = new CreateItemRequest(sourceModel.DisplayName, sourceModel.Type);
        createRequest.Definition = modelDefinition;
        var targetModel = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

        // track mapping between source semantic model and target semantic model
        reportRedirects.Add(sourceModel.Id.Value.ToString(), targetModel.Id.Value.ToString());

        var semanticModelDatasource = PowerBiRestApi.GetDatasourcesForSemanticModel(targetWorkspace.Id, targetModel.Id.Value).FirstOrDefault();

        Item lakehouse = null;
        if (semanticModelDatasource.DatasourceType.ToLower() == "sql") {
          lakehouse = sqlEndPointIds[semanticModelDatasource.ConnectionDetails.Database];
        }

        CreateAndBindSemanticModelConnecton(targetWorkspace, targetModel.Id.Value, lakehouse);

      }

    }

    var reports = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      // use item definition to create clone in target workspace
      AppLogger.LogSubstep($"Creating [{sourceReport.DisplayName}.Report]");

      // get model definition from source workspace
      var sourceReportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);

      // update definition.pbir to point to correct semantic model
      createRequest.Definition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourceReportDefinition,
                                                                   "definition.pbir",
                                                                   reportRedirects);

      // update report.json with report title customization
      createRequest.Definition = CustomizeReportTitle(createRequest, Deployment);

      var targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

    }

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  // overloads added for convenience

  public static void DeployFromSourceWorkspace(string SourceWorkspaceName, DeploymentPlan Deployment) {
    DeployFromSourceWorkspace(SourceWorkspaceName, Deployment.TargetWorkspaceName, Deployment);
  }

  #endregion

  #region Lab 05 - Update Solution From Source Workspace

  public static Dictionary<string, string> GetWorkspaceConnectionRedirects(Workspace SourceWorkspace, Workspace TargetWorkspace, DeploymentPlan Deployment) {

    var sourceWorkspaceConnections = FabricRestApi.GetWorkspaceConnections(SourceWorkspace.Id);
    var targetWorkspaceConnections = FabricRestApi.GetWorkspaceConnections(TargetWorkspace.Id);

    if (targetWorkspaceConnections.Where(conn => !conn.DisplayName.Contains("Lakehouse")).ToList().Count > 0) {
      AppLogger.LogStep("Discovering connections found in target workspace");
    }

    var connectionRedirects = new Dictionary<string, string>();

    foreach (var sourceConnection in sourceWorkspaceConnections) {
      // ignore lakehouse connections
      if (!sourceConnection.DisplayName.Contains("Lakehouse")) {
        int workspaceNameOffset = 48;
        string sourceConnectionName = sourceConnection.DisplayName.Substring(workspaceNameOffset);
        foreach (var targetConnection in targetWorkspaceConnections) {
          string targetConnectionName = targetConnection.DisplayName.Substring(workspaceNameOffset);
          if (sourceConnectionName == targetConnectionName) {

            switch (sourceConnection.ConnectionDetails.Type) {

              case "Web":
                string sourceWebUrl = sourceConnection.ConnectionDetails.Path;
                string targetWebUrl = sourceWebUrl;

                if (Deployment.Parameters.ContainsKey(DeploymentPlan.webDatasourcePathParameter)) {
                  targetWebUrl = Deployment.Parameters[DeploymentPlan.webDatasourcePathParameter];
                }

                AppLogger.LogSubstep($"Web: {targetWebUrl}");

                // redirect connection Id
                connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

                // redirect connection path
                connectionRedirects.Add(sourceWebUrl, targetWebUrl);

                break;

              case "AzureDataLakeStorage":
                string sourceAdlsConnectionPath = sourceConnection.ConnectionDetails.Path;
                string sourceAdlsServer = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[0] + "dfs.core.windows.net";
                string sourceAdlsPath = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[1];

                int pathSlash = sourceAdlsPath.Substring(1).IndexOf("/");
                string sourceAdlsContainerName = sourceAdlsPath.Substring(1, pathSlash);
                string sourceAdlsContainerPath = sourceAdlsPath.Substring(pathSlash + 1);

                string targetAdlsServer = sourceAdlsServer;
                string targetAdlsPath = sourceAdlsPath;
                string targetAdlsContainerName = sourceAdlsContainerName;
                string targetAdlsContainerPath = sourceAdlsContainerPath;

                if ((Deployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
                    (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
                    (Deployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

                  targetAdlsServer = Deployment.Parameters[DeploymentPlan.adlsServerPathParameter];
                  targetAdlsContainerName = Deployment.Parameters[DeploymentPlan.adlsContainerNameParameter];
                  targetAdlsContainerPath = Deployment.Parameters[DeploymentPlan.adlsContainerPathParameter];
                  targetAdlsPath = "/" + targetAdlsContainerName + targetAdlsContainerPath;
                }

                AppLogger.LogSubstep($"ADLS: {targetAdlsServer}{targetAdlsPath}");

                // redirect connection Id
                connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

                // redirect connection path
                connectionRedirects.Add(sourceAdlsServer, targetAdlsServer);
                connectionRedirects.Add(sourceAdlsContainerName, targetAdlsContainerName);
                connectionRedirects.Add(sourceAdlsContainerPath, targetAdlsContainerPath);

                break;

              default:
                throw new ApplicationException("Unexpected connection type");
            }

          }
        }
      }

    }

    return connectionRedirects;
  }

  // FULL UPDATE - all workspace items
  public static void UpdateFromSourceWorkspace(string SourceWorkspaceName, string TargetWorkspaceName, DeploymentPlan Deployment, bool DeleteOrphanedItems = false) {

    AppLogger.LogSolution($"Updating solution from source workspace [{SourceWorkspaceName}] to [{TargetWorkspaceName}]");

    DisplayDeploymentParameters(Deployment);

    // create data collections to track substitution data
    var connectionRedirects = new Dictionary<string, string>();
    var sqlEndPointIds = new List<string>();
    var lakehouseNames = new List<string>();
    var shortcutRedirects = new Dictionary<string, string>();
    var notebookRedirects = new Dictionary<string, string>();
    var dataPipelineRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);
    var sourceWorkspaceItems = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);
    var targetWorkspaceItems = FabricRestApi.GetWorkspaceItems(targetWorkspace.Id);

    // update target workspace description
    var sourceWorkspaceInfo = FabricRestApi.GetWorkspaceInfo(sourceWorkspace.Id);
    FabricRestApi.UpdateWorkspaceDescription(targetWorkspace.Id, sourceWorkspaceInfo.Description);

    // add connection redirect
    connectionRedirects = GetWorkspaceConnectionRedirects(sourceWorkspace, targetWorkspace, Deployment);

    // make deep copy of connectionRedirects for starting point for other redirects
    shortcutRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    notebookRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    semanticModelRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    dataPipelineRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;

    // add redirects for workspace id
    notebookRedirects.Add(sourceWorkspace.Id.ToString(), targetWorkspace.Id.ToString());
    dataPipelineRedirects.Add(sourceWorkspace.Id.ToString(), targetWorkspace.Id.ToString());

    AppLogger.LogStep($"Processing workspace item updates");

    // create lakehouses if they do not exist in target
    var lakehouses = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Lakehouse");
    foreach (var sourceLakehouse in lakehouses) {

      Guid sourceLakehouseId = sourceLakehouse.Id.Value;
      var sourceLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(sourceWorkspace.Id, sourceLakehouse.Id.Value);

      var targetLakehouse = targetWorkspaceItems.Where(item => (item.DisplayName.Equals(sourceLakehouse.DisplayName) &&
                                                               (item.Type == sourceLakehouse.Type))).FirstOrDefault();

      if (targetLakehouse != null) {
        // nothing to do here if lakehouse already exists
      }
      else {
        // create lakehouse if it doesn't exist in target workspace
        AppLogger.LogSubstep($"Creating [{sourceLakehouse.DisplayName}.{sourceLakehouse.Type}]");
        targetLakehouse = FabricRestApi.CreateLakehouse(targetWorkspace.Id, sourceLakehouse.DisplayName);
      }

      var targetLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(targetWorkspace.Id, targetLakehouse.Id.Value);

      // add lakehouse names to detect lakehouse default semantic model
      lakehouseNames.Add(targetLakehouse.DisplayName);

      // add redirect for lakehouse id
      notebookRedirects.Add(sourceLakehouse.Id.Value.ToString(), targetLakehouse.Id.Value.ToString());
      dataPipelineRedirects.Add(sourceLakehouse.Id.Value.ToString(), targetLakehouse.Id.Value.ToString());

      // add redirect for sql endpoint database name 
      semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.Id, targetLakehouseSqlEndpoint.Id);

      // add redirect for sql endpoint server location
      if (!semanticModelRedirects.Keys.Contains(sourceLakehouseSqlEndpoint.ConnectionString)) {
        // only add sql endpoint server location once because it has same value for all lakehouses in the same workspace
        semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.ConnectionString, targetLakehouseSqlEndpoint.ConnectionString);
      }


    }

    // create or update notebooks
    var notebooks = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Notebook");
    foreach (var sourceNotebook in notebooks) {

      var sourceNotebookDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id,
                                                                     sourceNotebook.Id.Value);

      var notebookDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourceNotebookDefinition,
                                                                     "notebook-content.py",
                                                                      notebookRedirects);

      var targetNotebook = targetWorkspaceItems.Where(item => (item.DisplayName.Equals(sourceNotebook.DisplayName) &&
                                                              (item.Type == sourceNotebook.Type))).FirstOrDefault();

      if (targetNotebook != null) {
        // update existing notebook
        AppLogger.LogSubstep($"Updating [{sourceNotebook.DisplayName}.{sourceNotebook.Type}]");
        var updateRequest = new UpdateItemDefinitionRequest(notebookDefinition);
        FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetNotebook.Id.Value, updateRequest);
      }
      else {
        // create new notebook
        AppLogger.LogSubstep($"Creating [{sourceNotebook.DisplayName}.{sourceNotebook.Type}]");
        var createRequest = new CreateItemRequest(sourceNotebook.DisplayName, sourceNotebook.Type);
        createRequest.Definition = notebookDefinition;
        targetNotebook = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

        // run new notebook
        AppLogger.LogSubOperationStart($"Running  [{sourceNotebook.DisplayName}.Notebook]");
        FabricRestApi.RunNotebook(targetWorkspace.Id, targetNotebook);
        AppLogger.LogOperationComplete();

      }

      dataPipelineRedirects.Add(sourceNotebook.Id.Value.ToString(), targetNotebook.Id.Value.ToString());

    }

    // create or update data pipelines
    var pipelines = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "DataPipeline");
    foreach (var sourcePipeline in pipelines) {

      var sourcePipelineDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourcePipeline.Id.Value);

      var pipelineDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourcePipelineDefinition,
                                                                     "pipeline-content.json",
                                                                      dataPipelineRedirects);

      var targetPipeline = targetWorkspaceItems.Where(item => (item.DisplayName.Equals(sourcePipeline.DisplayName) &&
                                                              (item.Type == sourcePipeline.Type))).FirstOrDefault();

      if (targetPipeline != null) {
        // update existing pipeline
        AppLogger.LogSubstep($"Updating [{targetPipeline.DisplayName}.{targetPipeline.Type}]");
        var updateRequest = new UpdateItemDefinitionRequest(pipelineDefinition);
        FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetPipeline.Id.Value, updateRequest);
      }
      else {
        // create new pipeline
        AppLogger.LogSubstep($"Creating [{sourcePipeline.DisplayName}.{sourcePipeline.Type}]");
        var createRequest = new CreateItemRequest(sourcePipeline.DisplayName, sourcePipeline.Type);
        createRequest.Definition = pipelineDefinition;
        targetPipeline = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

        // run pipeline if 'Create' is in its DislayName
        if (createRequest.DisplayName.Contains("Create")) {
          AppLogger.LogSubOperationStart($"Running  [{sourcePipeline.DisplayName}.DataPipeline]");
          FabricRestApi.RunDataPipeline(targetWorkspace.Id, targetPipeline);
          AppLogger.LogOperationComplete();
        }

      }
    }

    // create or update semantic models
    var models = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in models) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(sourceModel.DisplayName)) {

        // get model definition from source workspace
        var sourceModelDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceModel.Id.Value);

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourceModelDefinition,
                                                                     "definition/expressions.tmdl",
                                                                     semanticModelRedirects);

        var targetModel = targetWorkspaceItems.Where(item => (item.Type == sourceModel.Type) &&
                                                             (item.DisplayName == sourceModel.DisplayName)).FirstOrDefault();

        if (targetModel != null) {
          AppLogger.LogSubstep($"Updating [{sourceModel.DisplayName}.{sourceModel.Type}]");
          var updateRequest = new UpdateItemDefinitionRequest(modelDefinition);
          FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetModel.Id.Value, updateRequest);
        }
        else {
          AppLogger.LogSubstep($"Creating [{sourceModel.DisplayName}.{sourceModel.Type}]");
          var createRequest = new CreateItemRequest(sourceModel.DisplayName, sourceModel.Type);
          createRequest.Definition = modelDefinition;
          targetModel = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

          CreateAndBindSemanticModelConnecton(targetWorkspace, targetModel.Id.Value);

        }

        // track mapping between source semantic model and target semantic model
        reportRedirects.Add(sourceModel.Id.Value.ToString(), targetModel.Id.Value.ToString());

      }

    }

    // create or update reports
    var reports = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      // get model definition from source workspace
      var sourceReportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      // update definition.pbir to point to correct semantic model
      var reportDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourceReportDefinition,
                                                                           "definition.pbir",
                                                                           reportRedirects);

      // update report.json with report title customiztions
      reportDefinition = CustomizeReportTitle(reportDefinition, sourceReport.DisplayName, Deployment);

      var targetReport = targetWorkspaceItems.FirstOrDefault(item => (item.Type == "Report") &&
                                                                     (item.DisplayName == sourceReport.DisplayName));

      if (targetReport != null) {
        // update existing report
        AppLogger.LogSubstep($"Updating [{sourceReport.DisplayName}.{sourceReport.Type}]");
        var updateRequest = new UpdateItemDefinitionRequest(reportDefinition);
        FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetReport.Id.Value, updateRequest);
      }
      else {
        // use item definition to create clone in target workspace
        AppLogger.LogSubstep($"Creating [{sourceReport.DisplayName}.{sourceReport.Type}]");
        var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);
        createRequest.Definition = reportDefinition;
        targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);
      }
    }

    // delete orphaned items in target workspace
    if (DeleteOrphanedItems) {

      List<string> sourceWorkspaceItemNames = new List<string>();

      sourceWorkspaceItemNames.AddRange(
        sourceWorkspaceItems.Select(item => $"{item.DisplayName}.{item.Type}")
      );

      var lakehouseNamesInTarget = targetWorkspaceItems
                                     .Where(item => item.Type == "Lakehouse")
                                     .Select(item => item.DisplayName).ToList();

      foreach (var item in targetWorkspaceItems) {
        string itemName = $"{item.DisplayName}.{item.Type}";
        if (!sourceWorkspaceItemNames.Contains(itemName) &&
           (item.Type != "SQLEndpoint") &&
           !(item.Type == "SemanticModel" && lakehouseNamesInTarget.Contains(item.DisplayName))) {
          try {
            AppLogger.LogSubstep($"Deleting [{itemName}]");
            FabricRestApi.DeleteItem(targetWorkspace.Id, item);
          }
          catch {
            AppLogger.LogSubstep($"Could not delete [{itemName}]");

          }
        }
      }


    }

    AppLogger.LogStep("Solution updates complete");

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  // PARTIAL UPDATE - all reports
  public static void UpdateReportsFromFromSourceWorkspace(string SourceWorkspaceName, string TargetWorkspaceName, DeploymentPlan Deployment) {

    AppLogger.LogSolution($"Updating reports from source workspace [{SourceWorkspaceName}] to [{TargetWorkspaceName}]");

    // create data collections to track substitution data
    var reportRedirects = new Dictionary<string, string>();

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);
    var sourceWorkspaceItems = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);
    var targetWorkspaceItems = FabricRestApi.GetWorkspaceItems(targetWorkspace.Id);

    var lakehouseNames = targetWorkspaceItems.Where(item => item.Type == ItemType.Lakehouse)
                                             .Select(item => item.DisplayName);

    AppLogger.LogStep($"Processing workspace item updates");

    // get Id for semantic models in soure workspace and target workspace
    var models = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in models) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(sourceModel.DisplayName)) {


        var targetModel = targetWorkspaceItems.Where(item => (item.Type == ItemType.SemanticModel) &&
                                                             (item.DisplayName == sourceModel.DisplayName)).First();

        // track mapping between source semantic model and target semantic model
        reportRedirects.Add(sourceModel.Id.Value.ToString(), targetModel.Id.Value.ToString());
      }

    }

    // create or update reports
    var reports = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      // get model definition from source workspace
      var sourceReportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
      var reportDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourceReportDefinition,
                                                                   "definition.pbir",
                                                                   reportRedirects);

      // update report.json with report title customiztions
      reportDefinition = CustomizeReportTitle(reportDefinition, sourceReport.DisplayName, Deployment);

      var targetReport = targetWorkspaceItems.FirstOrDefault(item => (item.Type == "Report") &&
                                                                     (item.DisplayName == sourceReport.DisplayName));

      if (targetReport != null) {
        // update existing report
        AppLogger.LogSubstep($"Updating [{sourceReport.DisplayName}.{sourceReport.Type}]");
        var updateRequest = new UpdateItemDefinitionRequest(reportDefinition);
        FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetReport.Id.Value, updateRequest);
      }
      else {
        // use item definition to create clone in target workspace
        AppLogger.LogSubstep($"Creating [{sourceReport.DisplayName}.{sourceReport.Type}]");
        var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);
        createRequest.Definition = reportDefinition;
        targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);
      }

    }

    AppLogger.LogStep("Reports update from source workspace complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  // PARTIAL UPDATE - single report
  public static void UpdateReportFromFromSourceWorkspace(string SourceWorkspaceName, string TargetWorkspaceName, string ReportName, DeploymentPlan Deployment) {

    AppLogger.LogStep($"Updating report [{ReportName}] in workspace [{TargetWorkspaceName}] from [{SourceWorkspaceName}] ");

    // create data collections to track substitution data
    var reportRedirects = new Dictionary<string, string>();

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);
    var sourceWorkspaceItems = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);
    var targetWorkspaceItems = FabricRestApi.GetWorkspaceItems(targetWorkspace.Id);

    var lakehouseNames = targetWorkspaceItems.Where(item => item.Type == ItemType.Lakehouse)
                                             .Select(item => item.DisplayName);

    AppLogger.LogStep($"Processing workspace item updates");

    // get Id for semantic models in soure workspace and target workspace
    var models = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in models) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(sourceModel.DisplayName)) {

        var targetModel = targetWorkspaceItems.Where(item => (item.Type == sourceModel.Type) &&
                                                           (item.DisplayName == sourceModel.DisplayName)).FirstOrDefault();

        // track mapping between source semantic model and target semantic model
        reportRedirects.Add(sourceModel.Id.Value.ToString(), targetModel.Id.Value.ToString());
      }

    }

    // create or update reports
    var reports = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      if (sourceReport.DisplayName == ReportName) {
        // get model definition from source workspace
        var sourceReportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var reportDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(sourceReportDefinition,
                                                                     "definition.pbir",
                                                                     reportRedirects);

        // update report.json with report title customiztions
        reportDefinition = CustomizeReportTitle(reportDefinition, sourceReport.DisplayName, Deployment);

        var targetReport = targetWorkspaceItems.FirstOrDefault(item => (item.Type == "Report") &&
                                                                       (item.DisplayName == sourceReport.DisplayName));

        if (targetReport != null) {
          // update existing report
          AppLogger.LogSubstep($"Updating [{sourceReport.DisplayName}.{sourceReport.Type}]");
          var updateRequest = new UpdateItemDefinitionRequest(reportDefinition);
          FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetReport.Id.Value, updateRequest);
        }
        else {
          // use item definition to create clone in target workspace
          AppLogger.LogSubstep($"Creating [{sourceReport.DisplayName}.{sourceReport.Type}]");
          var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);
          createRequest.Definition = reportDefinition;
          targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);
        }

      }

    }

    AppLogger.LogStep("Report update from source workspace complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  // overloads added for convenience

  public static void UpdateFromSourceWorkspace(string SourceWorkspaceName, DeploymentPlan Deployment) {
    UpdateFromSourceWorkspace(SourceWorkspaceName, Deployment.TargetWorkspaceName, Deployment);
  }

  public static void UpdateReportsFromFromSourceWorkspace(string SourceWorkspaceName, DeploymentPlan Deployment) {
    UpdateReportsFromFromSourceWorkspace(SourceWorkspaceName, Deployment.TargetWorkspaceName, Deployment);
  }

  public static void UpdateReportFromFromSourceWorkspace(string SourceWorkspaceName, string ReportName, DeploymentPlan Deployment) {
    UpdateReportFromFromSourceWorkspace(SourceWorkspaceName, Deployment.TargetWorkspaceName, ReportName, Deployment);
  }


  #endregion

  #region Lab 06 - Export Workspace To Local Solution Folder

  public static void ExportWorkspaceToLocalSolutionFolder(string SourceWorkspace, string SolutionFolderName) {
    ItemDefinitionFactory.ExportWorkspaceToLocalSolutionFolder(SourceWorkspace, SolutionFolderName);
  }

  #endregion

  #region Lab 07 - Deploy and Update From Local Solution Folder

  // Recreate Workspace Connections used for DEPLOY
  public static Dictionary<string, string> RecreateWorkspaceConnections(List<DeploymentSourceConnection> WorkspaceConnections, Workspace TargetWorkspace, SolutionDeploymentPlan SolutionDeployment) {

    var workspaceConnections = SolutionDeployment.DeployConfig.SourceConnections;

    if (workspaceConnections.Where(conn => !conn.DisplayName.Contains("Lakehouse")).ToList().Count > 0) {
      AppLogger.LogStep("Recreating connections found in source workspace");
    }

    var connectionRedirects = new Dictionary<string, string>();

    foreach (var sourceConnection in workspaceConnections) {

      // ignore lakehouse connections
      if (!sourceConnection.DisplayName.Contains("Lakehouse")) {

        Connection targetConnection = null;

        switch (sourceConnection.Type) {

          case "Web":
            string sourceWebUrl = sourceConnection.Path;
            string targetWebUrl = sourceWebUrl;

            if (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.webDatasourcePathParameter)) {
              targetWebUrl = SolutionDeployment.Parameters[DeploymentPlan.webDatasourcePathParameter];
            }

            AppLogger.LogSubstep($"Web: {targetWebUrl}");
            targetConnection = FabricRestApi.CreateAnonymousWebConnection(targetWebUrl, TargetWorkspace);

            // redirect connection Id
            connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

            // redirect connection path
            connectionRedirects.Add(sourceWebUrl, targetWebUrl);

            break;

          case "AzureDataLakeStorage":
            string sourceAdlsConnectionPath = sourceConnection.Path;
            string sourceAdlsServer = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[0] + "dfs.core.windows.net";
            string sourceAdlsPath = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[1];

            int pathSlash = sourceAdlsPath.Substring(1).IndexOf("/");
            string sourceAdlsContainerName = sourceAdlsPath.Substring(1, pathSlash);
            string sourceAdlsContainerPath = sourceAdlsPath.Substring(pathSlash + 1);

            string targetAdlsServer = sourceAdlsServer;
            string targetAdlsPath = sourceAdlsPath;
            string targetAdlsContainerName = sourceAdlsContainerName;
            string targetAdlsContainerPath = sourceAdlsContainerPath;


            if ((SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
                (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
                (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

              targetAdlsServer = SolutionDeployment.Parameters[DeploymentPlan.adlsServerPathParameter];
              targetAdlsContainerName = SolutionDeployment.Parameters[DeploymentPlan.adlsContainerNameParameter];
              targetAdlsContainerPath = SolutionDeployment.Parameters[DeploymentPlan.adlsContainerPathParameter];
              targetAdlsPath = "/" + targetAdlsContainerName + targetAdlsContainerPath;

            }

            AppLogger.LogSubstep($"ADLS: {targetAdlsServer}{targetAdlsPath}");

            targetConnection = FabricRestApi.CreateAzureStorageConnectionWithAccountKey(targetAdlsServer, targetAdlsPath, TargetWorkspace);

            // redirect connection Id
            connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

            // redirect connection path
            connectionRedirects.Add(sourceAdlsServer, targetAdlsServer);
            connectionRedirects.Add(sourceAdlsContainerName, targetAdlsContainerName);
            connectionRedirects.Add(sourceAdlsContainerPath, targetAdlsContainerPath);

            break;

          default:
            throw new ApplicationException("Unexpected connection type");

        }
      }

    }

    return connectionRedirects;

  }

  public static void DeployFromSolutionFolder(string SolutionFolder, string TargetWorkspaceName, SolutionDeploymentPlan SolutionDeployment) {

    // create data collections to track substitution data
    var connectionRedirects = new Dictionary<string, string>();
    var sqlEndPointIds = new Dictionary<string, Item>();
    var lakehouseNames = new List<string>();
    var shortcutRedirects = new Dictionary<string, string>();
    var notebookRedirects = new Dictionary<string, string>();
    var dataPipelineRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var targetWorkspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with id of {targetWorkspace.Id.ToString()}");

    //var targetWorkspaceDescription = SolutionDeployment.DeployConfig.SourceWorkspaceDescription;
    //FabricRestApi.UpdateWorkspaceDescription(targetWorkspace.Id, targetWorkspaceDescription);

    var sourceWorkspaceConnections = SolutionDeployment.DeployConfig.SourceConnections;

    connectionRedirects = RecreateWorkspaceConnections(sourceWorkspaceConnections, targetWorkspace, SolutionDeployment);

    // make deep copy of connectionRedirects for starting point for other redirects
    shortcutRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    notebookRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    semanticModelRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    dataPipelineRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;

    notebookRedirects.Add(SolutionDeployment.GetSourceWorkspaceId(), targetWorkspace.Id.ToString());
    dataPipelineRedirects.Add(SolutionDeployment.GetSourceWorkspaceId(), targetWorkspace.Id.ToString());

    AppLogger.LogStep($"Deploying Workspace Items");

    foreach (var lakehouse in SolutionDeployment.GetLakehouses()) {

      var sourceLakehouse = SolutionDeployment.GetSourceLakehouse(lakehouse.DisplayName);
      Guid sourceLakehouseId = new Guid(sourceLakehouse.Id);

      AppLogger.LogSubstep($"Creating [{lakehouse.ItemName}]");
      var targetLakehouse = FabricRestApi.CreateLakehouse(targetWorkspace.Id, lakehouse.DisplayName);

      var targetLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(targetWorkspace.Id, targetLakehouse.Id.Value);

      // add lakehouse names and Ids to detect lakehouse default semantic model
      lakehouseNames.Add(targetLakehouse.DisplayName);
      sqlEndPointIds.Add(targetLakehouseSqlEndpoint.Id, targetLakehouse);

      // add lakehouse redirect Ids for other workspace items
      notebookRedirects.Add(sourceLakehouse.Id, targetLakehouse.Id.Value.ToString());
      dataPipelineRedirects.Add(sourceLakehouse.Id, targetLakehouse.Id.Value.ToString());

      // add redirect for sql endpoint database name 
      semanticModelRedirects.Add(sourceLakehouse.Database, targetLakehouseSqlEndpoint.Id);

      // add redirect for sql endpoint server location
      if (!semanticModelRedirects.Keys.Contains(sourceLakehouse.Server)) {
        // only add sql endpoint server location once because it has same value for all lakehouses in the same workspace
        semanticModelRedirects.Add(sourceLakehouse.Server, targetLakehouseSqlEndpoint.ConnectionString);
      }

      // copy shortcuts
      var shortcuts = sourceLakehouse.Shortcuts;
      if (shortcuts != null) {
        foreach (var shortcut in shortcuts) {

          if (shortcut.Type.ToLower() == "adlsgen2") {

            string sourceShortcutName = shortcut.Name;
            string sourceShortcutPath = shortcut.Path;
            string sourceShortcutLocation = shortcut.Location;
            string sourceShortcutSubpath = shortcut.Subpath;

            string targetShortcutName = sourceShortcutName;
            string targetShortcutPath = sourceShortcutPath;
            string targetShortcutLocation = sourceShortcutLocation;
            string targetShortcutSubpath = sourceShortcutSubpath;

            if ((SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
                (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
                (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

              targetShortcutLocation = SolutionDeployment.Parameters[DeploymentPlan.adlsServerPathParameter];

              targetShortcutSubpath = "/" + SolutionDeployment.Parameters[DeploymentPlan.adlsContainerNameParameter] +
                                            SolutionDeployment.Parameters[DeploymentPlan.adlsContainerPathParameter];

            }

            Guid targetConnectionId = new Guid(connectionRedirects[shortcut.ConnectionId]);

            AppLogger.LogSubstep($"Creating [{lakehouse.ItemName}.Shortcut] with path of [{targetShortcutPath.Substring(1)}/{targetShortcutName}]");

            FabricRestApi.CreateAdlsGen2Shortcut(targetWorkspace.Id,
                                                 targetLakehouse.Id.Value,
                                                 targetShortcutName,
                                                 targetShortcutPath,
                                                 new Uri(targetShortcutLocation),
                                                 targetShortcutSubpath,
                                                 targetConnectionId);

          }

        }
      }
    }

    foreach (var notebook in SolutionDeployment.GetNotebooks()) {

      var sourceNotebook = SolutionDeployment.GetSourceNotebook(notebook.DisplayName);

      AppLogger.LogSubstep($"Creating [{notebook.ItemName}]");
      var createRequest = new CreateItemRequest(notebook.DisplayName, notebook.Type);
      createRequest.Definition = ItemDefinitionFactory.UpdateItemDefinitionPart(notebook.Definition, "notebook-content.py", notebookRedirects);
      var targetNotebook = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

      dataPipelineRedirects.Add(sourceNotebook.Id, targetNotebook.Id.Value.ToString());

      if (createRequest.DisplayName.Contains("Create")) {
        AppLogger.LogSubOperationStart($"Running  [{notebook.ItemName}]");
        FabricRestApi.RunNotebook(targetWorkspace.Id, targetNotebook);
        AppLogger.LogOperationComplete();
      }

    }

    foreach (var pipeline in SolutionDeployment.GetDataPipelines()) {

      AppLogger.LogSubstep($"Creating [{pipeline.ItemName}]");
      var createRequest = new CreateItemRequest(pipeline.DisplayName, pipeline.Type);
      createRequest.Definition = ItemDefinitionFactory.UpdateItemDefinitionPart(pipeline.Definition,
                                                                                "pipeline-content.json",
                                                                                dataPipelineRedirects);

      var targetPipeline = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

      if (createRequest.DisplayName.Contains("Create")) {
        AppLogger.LogSubOperationStart($"Running  [{createRequest.DisplayName}.DataPipeline]");
        FabricRestApi.RunDataPipeline(targetWorkspace.Id, targetPipeline);
        AppLogger.LogOperationComplete();
      }

    }

    foreach (var sqlEndPointId in sqlEndPointIds.Keys) {
      FabricRestApi.RefreshLakehouseTableSchema(sqlEndPointId);
    }

    foreach (var model in SolutionDeployment.GetSemanticModels()) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(model.DisplayName)) {

        var sourceModel = SolutionDeployment.GetSourceSemanticModel(model.DisplayName);

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(model.Definition, "definition/expressions.tmdl", semanticModelRedirects);

        // use item definition to create clone in target workspace
        AppLogger.LogSubstep($"Creating [{model.ItemName}]");
        var createRequest = new CreateItemRequest(model.DisplayName, model.Type);
        createRequest.Definition = modelDefinition;
        var targetModel = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

        // track mapping between source semantic model and target semantic model
        reportRedirects.Add(sourceModel.Id, targetModel.Id.Value.ToString());

        var semanticModelDatasource = PowerBiRestApi.GetDatasourcesForSemanticModel(targetWorkspace.Id, targetModel.Id.Value).FirstOrDefault();

        Item lakehouse = null;
        if (semanticModelDatasource.DatasourceType.ToLower() == "sql") {
          lakehouse = sqlEndPointIds[semanticModelDatasource.ConnectionDetails.Database];
        }

        CreateAndBindSemanticModelConnecton(targetWorkspace, targetModel.Id.Value, lakehouse);

      }

    }

    foreach (var report in SolutionDeployment.GetReports()) {

      var sourceReport = SolutionDeployment.GetSourceReport(report.DisplayName);

      // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
      var reportDefinition = ItemDefinitionFactory.UpdateReportDefinitionWithRedirection(report.Definition, targetWorkspace.Id, reportRedirects);

      // update report.json with report title customization
      reportDefinition = CustomizeReportTitle(reportDefinition, report.DisplayName, SolutionDeployment);

      // use item definition to create clone in target workspace
      AppLogger.LogSubstep($"Creating [{report.ItemName}]");
      var createRequest = new CreateItemRequest(report.DisplayName, report.Type);
      createRequest.Definition = reportDefinition;
      var targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

    }

    AppLogger.LogStep("Solution deployment complete");

  }

  // Get Workspace Connection Redirects used for UPDATE
  public static Dictionary<string, string> GetWorkspaceConnectionRedirects(List<DeploymentSourceConnection> WorkspaceConnections, Guid TargetWorkspaceId, SolutionDeploymentPlan SolutionDeployment) {

    var sourceWorkspaceConnections = WorkspaceConnections;
    var targetWorkspaceConnections = FabricRestApi.GetWorkspaceConnections(TargetWorkspaceId);

    // make sure there is at least one connection that is not lakehouse connection
    if (targetWorkspaceConnections.Where(conn => !conn.DisplayName.Contains("Lakehouse")).ToList().Count > 0) {
      AppLogger.LogStep("Discovering connections found in target workspace");
    }

    var connectionRedirects = new Dictionary<string, string>();

    foreach (var sourceConnection in WorkspaceConnections) {
      // ignore lakehouse connections
      if (!sourceConnection.DisplayName.Contains("Lakehouse")) {
        int workspaceNameOffset = 48;
        string sourceConnectionName = sourceConnection.DisplayName;
        foreach (var targetConnection in targetWorkspaceConnections) {
          string targetConnectionName = targetConnection.DisplayName.Substring(workspaceNameOffset);
          if (sourceConnectionName == targetConnectionName) {

            switch (sourceConnection.Type) {

              case "Web":
                string sourceWebUrl = sourceConnection.Path;
                string targetWebUrl = sourceWebUrl;

                if (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.webDatasourcePathParameter)) {
                  targetWebUrl = SolutionDeployment.Parameters[DeploymentPlan.webDatasourcePathParameter];
                }

                AppLogger.LogSubstep($"Web: {targetWebUrl}");

                // redirect connection Id
                connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

                // redirect connection path
                connectionRedirects.Add(sourceWebUrl, targetWebUrl);

                break;

              case "AzureDataLakeStorage":
                string sourceAdlsConnectionPath = sourceConnection.Path;
                string sourceAdlsServer = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[0] + "dfs.core.windows.net";
                string sourceAdlsPath = sourceAdlsConnectionPath.Split("dfs.core.windows.net")[1];

                int pathSlash = sourceAdlsPath.Substring(1).IndexOf("/");
                string sourceAdlsContainerName = sourceAdlsPath.Substring(1, pathSlash);
                string sourceAdlsContainerPath = sourceAdlsPath.Substring(pathSlash + 1);

                string targetAdlsServer = sourceAdlsServer;
                string targetAdlsPath = sourceAdlsPath;
                string targetAdlsContainerName = sourceAdlsContainerName;
                string targetAdlsContainerPath = sourceAdlsContainerPath;

                if ((SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsServerPathParameter)) &&
                    (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerNameParameter)) &&
                    (SolutionDeployment.Parameters.ContainsKey(DeploymentPlan.adlsContainerPathParameter))) {

                  targetAdlsServer = SolutionDeployment.Parameters[DeploymentPlan.adlsServerPathParameter];
                  targetAdlsContainerName = SolutionDeployment.Parameters[DeploymentPlan.adlsContainerNameParameter];
                  targetAdlsContainerPath = SolutionDeployment.Parameters[DeploymentPlan.adlsContainerPathParameter];
                  targetAdlsPath = "/" + targetAdlsContainerName + targetAdlsContainerPath;
                }

                AppLogger.LogSubstep($"ADLS: {targetAdlsServer}{targetAdlsPath}");

                // redirect connection Id
                connectionRedirects.Add(sourceConnection.Id.ToString(), targetConnection.Id.ToString());

                // redirect connection path
                connectionRedirects.Add(sourceAdlsServer, targetAdlsServer);
                connectionRedirects.Add(sourceAdlsContainerName, targetAdlsContainerName);
                connectionRedirects.Add(sourceAdlsContainerPath, targetAdlsContainerPath);

                break;

              default:
                throw new ApplicationException("Unexpected connection type");
            }

          }
        }
      }

    }

    return connectionRedirects;
  }

  // UPDATE workflow
  public static void UpdateFromSolutionFolder(string SolutionFolder, string TargetWorkspaceName, SolutionDeploymentPlan SolutionDeployment, bool DeleteOrphanedItems = false) {

    var connectionRedirects = new Dictionary<string, string>();
    var sqlEndPointIds = new Dictionary<string, Item>();
    var lakehouseNames = new List<string>();
    var shortcutRedirects = new Dictionary<string, string>();
    var notebookRedirects = new Dictionary<string, string>();
    var dataPipelineRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    var sourceWorkspaceItems = SolutionDeployment.DeployConfig.SourceItems;

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);
    var targetWorkspaceItems = FabricRestApi.GetWorkspaceItems(targetWorkspace.Id);

    var targetWorkspaceDesciption = SolutionDeployment.DeployConfig.SourceWorkspaceDescription;
    FabricRestApi.UpdateWorkspaceDescription(targetWorkspace.Id, targetWorkspaceDesciption);

    connectionRedirects = GetWorkspaceConnectionRedirects(SolutionDeployment.DeployConfig.SourceConnections, targetWorkspace.Id, SolutionDeployment);

    // copy connections dictionary for shortcuts, notebooks and data pipelines
    // make deep copy of connectionRedirects for starting point for other redirects
    shortcutRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    notebookRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    semanticModelRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;
    dataPipelineRedirects = connectionRedirects.ToDictionary(entry => entry.Key, entry => entry.Value); ;

    notebookRedirects.Add(SolutionDeployment.GetSourceWorkspaceId(), targetWorkspace.Id.ToString());
    dataPipelineRedirects.Add(SolutionDeployment.GetSourceWorkspaceId(), targetWorkspace.Id.ToString());

    AppLogger.LogStep($"Processing workspace item updates");

    var lakehouses = SolutionDeployment.GetLakehouses();
    foreach (var lakehouse in lakehouses) {
      var sourceLakehouse = SolutionDeployment.GetSourceLakehouse(lakehouse.DisplayName);
      var targetLakehouse = targetWorkspaceItems.Where(item => (item.DisplayName.Equals(lakehouse.DisplayName) &&
                                                               (item.Type == lakehouse.Type))).FirstOrDefault();

      if (targetLakehouse != null) {
        // update item - nothing to do for lakehouse        
      }
      else {
        // create item
        AppLogger.LogSubstep($"Creating [{lakehouse.ItemName}]");
        targetLakehouse = FabricRestApi.CreateLakehouse(targetWorkspace.Id, lakehouse.DisplayName);
      }

      var targetLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(targetWorkspace.Id, targetLakehouse.Id.Value);

      // add lakehouse names to detect lakehouse default semantic model
      lakehouseNames.Add(targetLakehouse.DisplayName);

      // add redirects for lakehouse id
      notebookRedirects.Add(sourceLakehouse.Id, targetLakehouse.Id.Value.ToString());
      dataPipelineRedirects.Add(sourceLakehouse.Id, targetLakehouse.Id.Value.ToString());

      // add redirect for sql endpoint database name 
      semanticModelRedirects.Add(sourceLakehouse.Database, targetLakehouseSqlEndpoint.Id);

      // add redirect for sql endpoint server location
      if (!semanticModelRedirects.Keys.Contains(sourceLakehouse.Server)) {
        // only add sql endpoint server location once because it has same value for all lakehouses in the same workspace
        semanticModelRedirects.Add(sourceLakehouse.Server, targetLakehouseSqlEndpoint.ConnectionString);
      }

      // inspect shortcuts - currnently no logic to add missing shortcuts in UPDATE workflow
      var targetShortcuts = FabricRestApi.GetLakehouseShortcuts(targetWorkspace.Id, targetLakehouse.Id.Value);
      var targetShortcutPaths = targetShortcuts.Select(shortcut => shortcut.Path + "/" + shortcut.Name).ToList();
      if (sourceLakehouse.Shortcuts != null) {
        foreach (var shortcut in sourceLakehouse.Shortcuts) {
          string shortcutPath = shortcut.Path + "/" + shortcut.Name;
          if (!targetShortcutPaths.Contains(shortcutPath)) {
            AppLogger.LogSubstep($"New shortcut {shortcutPath}");
          }
        }
      }
    }

    var notebooks = SolutionDeployment.GetNotebooks();
    foreach (var notebook in notebooks) {
      var sourceNotebook = SolutionDeployment.GetSourceNotebook(notebook.DisplayName);
      var targetNotebook = targetWorkspaceItems.Where(item => (item.DisplayName.Equals(sourceNotebook.DisplayName) &&
                                                              (item.Type == sourceNotebook.Type))).FirstOrDefault();

      ItemDefinition notebookDefiniton = ItemDefinitionFactory.UpdateItemDefinitionPart(notebook.Definition,
                                                                                        "notebook-content.py",
                                                                                        notebookRedirects);

      if (targetNotebook != null) {
        // update existing notebook
        AppLogger.LogSubstep($"Updating [{notebook.ItemName}]");
        var updateRequest = new UpdateItemDefinitionRequest(notebookDefiniton);
        FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetNotebook.Id.Value, updateRequest);
      }
      else {
        // create item
        AppLogger.LogSubstep($"Creating [{notebook.ItemName}]");
        var createRequest = new CreateItemRequest(notebook.DisplayName, notebook.Type);
        createRequest.Definition = ItemDefinitionFactory.UpdateItemDefinitionPart(notebook.Definition, "notebook-content.py", notebookRedirects);
        targetNotebook = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);
        if (createRequest.DisplayName.Contains("Create")) {
          AppLogger.LogSubOperationStart($"Running  [{notebook.ItemName}]");
          FabricRestApi.RunNotebook(targetWorkspace.Id, targetNotebook);
          AppLogger.LogOperationComplete();
        }
      }

      dataPipelineRedirects.Add(sourceNotebook.Id, targetNotebook.Id.Value.ToString());

    }

    var pipelines = SolutionDeployment.GetDataPipelines();
    foreach (var pipeline in pipelines) {
      var sourcePipeline = sourceWorkspaceItems.FirstOrDefault(item => (item.Type == "DataPipeline" &&
                                                                         item.DisplayName.Equals(pipeline.DisplayName)));

      var targetPipeline = targetWorkspaceItems.Where(item => (item.DisplayName.Equals(pipeline.DisplayName) &&
                                                              (item.Type == pipeline.Type))).FirstOrDefault();

      ItemDefinition pipelineDefiniton = ItemDefinitionFactory.UpdateItemDefinitionPart(pipeline.Definition,
                                                                                        "pipeline-content.json",
                                                                                        dataPipelineRedirects);

      if (pipelineDefiniton != null) {
        // update existing notebook
        AppLogger.LogSubstep($"Updating [{pipeline.ItemName}]");
        var updateRequest = new UpdateItemDefinitionRequest(pipelineDefiniton);
        FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetPipeline.Id.Value, updateRequest);
      }
      else {
        // create item
        AppLogger.LogSubstep($"Creating [{pipeline.ItemName}]");
        var createRequest = new CreateItemRequest(pipeline.DisplayName, pipeline.Type);
        createRequest.Definition = pipelineDefiniton;
        targetPipeline = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

        // run pipeline if 'Create' is in its DislayName
        if (createRequest.DisplayName.Contains("Create")) {
          AppLogger.LogSubOperationStart($"Running  [{sourcePipeline.DisplayName}.DataPipeline]");
          FabricRestApi.RunDataPipeline(targetWorkspace.Id, targetPipeline);
          AppLogger.LogOperationComplete();
        }

      }
    }

    var models = SolutionDeployment.GetSemanticModels();
    foreach (var model in models) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(model.DisplayName)) {

        var sourceModel = sourceWorkspaceItems.FirstOrDefault(item => (item.Type == "SemanticModel" &&
                                                                       item.DisplayName == model.DisplayName));

        var targetModel = targetWorkspaceItems.Where(item => (item.Type == model.Type) &&
                                                             (item.DisplayName == model.DisplayName)).FirstOrDefault();

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(model.Definition,
                                                                             "definition/expressions.tmdl",
                                                                             semanticModelRedirects);

        if (targetModel != null) {
          AppLogger.LogSubstep($"Updating [{model.ItemName}]");
          // update existing model
          var updateRequest = new UpdateItemDefinitionRequest(modelDefinition);
          FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetModel.Id.Value, updateRequest);
        }
        else {
          AppLogger.LogSubstep($"Creating [{model.ItemName}]");
          var createRequest = new CreateItemRequest(model.DisplayName, model.Type);
          createRequest.Definition = modelDefinition;
          targetModel = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

          CreateAndBindSemanticModelConnecton(targetWorkspace, targetModel.Id.Value);
        }

        // track mapping between source semantic model and target semantic model
        reportRedirects.Add(sourceModel.Id, targetModel.Id.Value.ToString());

      }

    }

    // reports
    var reports = SolutionDeployment.GetReports();
    foreach (var report in reports) {

      var targetReport = targetWorkspaceItems.FirstOrDefault(item => (item.Type == "Report" &&
                                                                      item.DisplayName == report.DisplayName));

      // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
      var reportDefinition = ItemDefinitionFactory.UpdateReportDefinitionWithRedirection(report.Definition,
                                                                                         targetWorkspace.Id,
                                                                                         reportRedirects);

      // update report.json with report title customization
      reportDefinition = CustomizeReportTitle(reportDefinition, report.DisplayName, SolutionDeployment);

      if (targetReport != null) {
        // update existing report
        AppLogger.LogSubstep($"Updating [{report.ItemName}]");
        var updateRequest = new UpdateItemDefinitionRequest(reportDefinition);
        FabricRestApi.UpdateItemDefinition(targetWorkspace.Id, targetReport.Id.Value, updateRequest);
      }
      else {
        // use item definition to create clone in target workspace
        AppLogger.LogSubstep($"Creating [{report.ItemName}]");
        var createRequest = new CreateItemRequest(report.DisplayName, report.Type);
        createRequest.Definition = reportDefinition;
        targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);
      }
    }

    // delete orphaned items
    if (DeleteOrphanedItems) {

      List<string> sourceWorkspaceItemNames = new List<string>();
      sourceWorkspaceItemNames.AddRange(
        sourceWorkspaceItems.Select(item => $"{item.DisplayName}.{item.Type}")
      );

      var lakehouseNamesInTarget = targetWorkspaceItems.Where(item => item.Type == "Lakehouse").Select(item => item.DisplayName).ToList();

      foreach (var item in targetWorkspaceItems) {
        string itemName = $"{item.DisplayName}.{item.Type}";
        if (!sourceWorkspaceItemNames.Contains(itemName) &&
           (item.Type != "SQLEndpoint") &&
           !(item.Type == "SemanticModel" && lakehouseNamesInTarget.Contains(item.DisplayName))) {
          try {
            AppLogger.LogSubstep($"Deleting [{itemName}]");
            FabricRestApi.DeleteItem(targetWorkspace.Id, item);
          }
          catch {
            AppLogger.LogSubstep($"Could not delete [{itemName}]");

          }
        }
      }

      AppLogger.LogStep("Solution update complete");

    }
  }

  // Get Packaged Solution Deployment used for DEPLOY and UPDATE
  public static SolutionDeploymentPlan GetSolutionDeploymentFromLocalFolder(string SolutionFolder, DeploymentPlan Deployment) {

    var solutionDeploymentPlan = new SolutionDeploymentPlan(Deployment);

    AppLogger.LogStep($"Loading item definition files from local solutions folder");

    var itemDefinitionFiles = new List<ItemDefinitonFile>();

    string folderPath = AppSettings.LocalPackagedSolutionFolder + SolutionFolder + @"\";
    List<string> filesInFolder = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).ToList<string>();

    foreach (string file in filesInFolder) {
      string relativeFilePathFromFolder = file.Replace(folderPath, "")
                                              .Replace(@"\", "/");
      if (relativeFilePathFromFolder.Substring(1).Contains("/")) {
        itemDefinitionFiles.Add(new ItemDefinitonFile {
          FullPath = relativeFilePathFromFolder,
          Content = File.ReadAllText(file)
        });
      }
    }

    var items = itemDefinitionFiles.OrderBy(item => item.FullPath);

    DeploymentItem currentItem = null;

    foreach (var item in items) {
      if (item.FileName == ".platform") {
        AppLogger.LogSubstep($"Loading [{item.ItemName}]");
        FabricPlatformFile platformFile = JsonSerializer.Deserialize<FabricPlatformFile>(item.Content);
        PlatformFileMetadata itemMetadata = platformFile.metadata;
        PlatformFileConfig config = platformFile.config;

        currentItem = new DeploymentItem {
          DisplayName = itemMetadata.displayName,
          LogicalId = config.logicalId,
          Type = itemMetadata.type,
          Definition = new ItemDefinition(new List<ItemDefinitionPart>())
        };

        solutionDeploymentPlan.DeploymentItems.Add(currentItem);
      }
      else {
        string encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Content));
        currentItem.Definition.Parts.Add(
          new ItemDefinitionPart(item.Path, encodedContent, PayloadType.InlineBase64)
        );
      }
    }

    AppLogger.LogSubstep($"Loading [deploy.config.json]");
    string deployConfigPath = AppSettings.LocalPackagedSolutionFolder + SolutionFolder + @"\deploy.config.json";
    string deployConfigContent = File.ReadAllText(deployConfigPath);
    DeploymentConfiguration deployConfig = JsonSerializer.Deserialize<DeploymentConfiguration>(deployConfigContent, jsonSerializerOptions);

    solutionDeploymentPlan.DeployConfig = deployConfig;

    return solutionDeploymentPlan;
  }

  // DEPLOY workflow
  public static void DeployFromLocalSolutionFolder(string SolutionFolder, string TargetWorkspaceName, DeploymentPlan Deployment) {

    AppLogger.LogSolution($"Deploying solution package folder [{SolutionFolder}] to workspace [{TargetWorkspaceName}]");

    var solutionDeployment = GetSolutionDeploymentFromLocalFolder(SolutionFolder, Deployment);

    DeployFromSolutionFolder(SolutionFolder, TargetWorkspaceName, solutionDeployment);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }


  // UPDATE workflow
  public static void UpdateFromLocalSolutionFolder(string SolutionFolder, string TargetWorkspaceName, DeploymentPlan Deployment, bool DeleteOrphanedItems = false) {

    AppLogger.LogSolution($"Updating from localsolution folder [{SolutionFolder}] to workspace [{TargetWorkspaceName}]");

    DisplayDeploymentParameters(Deployment);

    var solutionDeployment = GetSolutionDeploymentFromLocalFolder(SolutionFolder, Deployment);

    UpdateFromSolutionFolder(SolutionFolder, TargetWorkspaceName, solutionDeployment);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  // overloads added for convenience

  public static void DeployFromLocalSolutionFolder(string SolutionFolder, DeploymentPlan Deployment) {
    DeployFromLocalSolutionFolder(SolutionFolder, Deployment.TargetWorkspaceName, Deployment);
  }

  public static void UpdateFromLocalSolutionFolder(string SolutionFolder, DeploymentPlan Deployment) {
    UpdateFromLocalSolutionFolder(SolutionFolder, Deployment.TargetWorkspaceName, Deployment);
  }



  #endregion

  #region Lab 08 - Connect Dev Workspace To Azure Dev Ops Repository

  public static void ConnectWorkspaceToGit(string WorkspaceName, string BranchName = "main") {
    AppLogger.LogSolution($"Creating GIT connection to sync [{WorkspaceName}] workspace with Azure Dev Ops repository");
    var workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);

    // create new project in Azure Dev Ops
    AdoProjectManager.EnsureProjectExists(WorkspaceName, workspace);
    var gitConnectRequest = new GitConnectRequest(
      new AzureDevOpsDetails(WorkspaceName, BranchName,
                                            "/",
                                            AppSettings.AzureDevOpsOrganizationName,
                                            WorkspaceName));

    FabricRestApi.ConnectWorkspaceToGitRepository(workspace.Id, gitConnectRequest);
    AppLogger.LogSubstep("Workspace connection to GIT created and synchronized successfully");
  }

  public static void ConnectFeatureWorkspaceToGit(string MainWorkspace, string FeatureWorkspace, string FeatureName) {

    var mainWorkspace = FabricRestApi.GetWorkspaceByName(MainWorkspace);
    var featureWorkspace = FabricRestApi.GetWorkspaceByName(FeatureWorkspace);

    // create new ADO project if it doesn't exist
    AdoProjectManager.EnsureProjectExists(MainWorkspace, mainWorkspace);
    AdoProjectManager.CreateBranch(MainWorkspace, FeatureName);

    var gitConnectRequest = new GitConnectRequest(
      new AzureDevOpsDetails(MainWorkspace,
                             FeatureName,
                             "/",
                             AppSettings.AzureDevOpsOrganizationName,
                             MainWorkspace));

    FabricRestApi.ConnectWorkspaceToGitRepository(featureWorkspace.Id, gitConnectRequest);
    AppLogger.LogSubstep("Workspace connection to GIT created and synchronized successfully");
  }

  public static void BranchOutToFeatureWorkspace(string WorkspaceName, string FeatureName) {

    string featureWorkspaceName = WorkspaceName + " - " + FeatureName;

    AppLogger.LogSolution($"Branching out [{WorkspaceName}] to feature workspace [{featureWorkspaceName}]");

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);

    AppLogger.LogStep($"Creating feature workspace [{featureWorkspaceName}]");
    var featureWorkspace = FabricRestApi.CreateWorkspace(featureWorkspaceName);
    AppLogger.LogSubstep($"Workspace created with Id of [{featureWorkspace.Id.ToString()}]");

    ConnectFeatureWorkspaceToGit(WorkspaceName, featureWorkspaceName, FeatureName);

    AppLogger.LogStep($"Performing Post-GIT-Sync Tasks");

    var sourceWorkspaceItems = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id);
    var featureWorkspaceItems = FabricRestApi.GetWorkspaceItems(featureWorkspace.Id);

    var lakehouseNames = new List<string>();
    var sqlEndPointIds = new Dictionary<string, Item>();
    var notebookRedirects = new Dictionary<string, string>();
    var dataPipelineRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();


    var lakehouses = featureWorkspaceItems.Where(item => item.Type == ItemType.Lakehouse).ToList();
    var sourceLakehouses = sourceWorkspaceItems.Where(item => item.Type == ItemType.Lakehouse).ToList();

    notebookRedirects.Add(sourceWorkspace.Id.ToString(), featureWorkspace.Id.ToString());
    dataPipelineRedirects.Add(sourceWorkspace.Id.ToString(), featureWorkspace.Id.ToString());

    AppLogger.LogSubstep("Processing lakehouses");
    foreach (var lakehouse in lakehouses) {
      var sourceLakehouse = sourceLakehouses.Where(item => item.DisplayName == lakehouse.DisplayName).First();

      lakehouseNames.Add(lakehouse.DisplayName);
      notebookRedirects.Add(sourceLakehouse.Id.Value.ToString(), lakehouse.Id.Value.ToString());

      AppLogger.LogSubstep($"Getting Sql Endpoint Info for [{lakehouse.DisplayName}.Lakehouse]");

      var targetSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(featureWorkspace.Id, lakehouse.Id.Value);

      sqlEndPointIds.Add(targetSqlEndpoint.Id, lakehouse);

      var sourceSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(sourceWorkspace.Id, sourceLakehouse.Id.Value);

      semanticModelRedirects.Add(sourceSqlEndpoint.Id, targetSqlEndpoint.Id);

      // add redirect for sql endpoint server location
      if (!semanticModelRedirects.Keys.Contains(sourceSqlEndpoint.ConnectionString)) {
        // only add sql endpoint server location once because it has same value for all lakehouses in the same workspace
        semanticModelRedirects.Add(sourceSqlEndpoint.ConnectionString, targetSqlEndpoint.ConnectionString);
      }

      var shortcuts = FabricRestApi.GetLakehouseShortcuts(sourceWorkspace.Id, sourceLakehouse.Id.Value);
      foreach (var shortcut in shortcuts) {

        if (shortcut.Target.Type == Microsoft.Fabric.Api.Core.Models.Type.AdlsGen2) {

          string targetShortcutName = shortcut.Name;
          string targetShortcutPath = shortcut.Path;
          string targetShortcutLocation = shortcut.Target.AdlsGen2.Location.ToString();
          string targetShortcutSubpath = shortcut.Target.AdlsGen2.Subpath;
          Guid targetConnectionId = shortcut.Target.AdlsGen2.ConnectionId;

          AppLogger.LogSubstep($"Creating [{lakehouse.DisplayName}.{lakehouse.Type}.Shortcut] with path of [{targetShortcutPath.Substring(1)}/{targetShortcutName}]");
          FabricRestApi.CreateAdlsGen2Shortcut(featureWorkspace.Id,
                                               lakehouse.Id.Value,
                                               targetShortcutName,
                                               targetShortcutPath,
                                               new Uri(targetShortcutLocation),
                                               targetShortcutSubpath,
                                               targetConnectionId);

        }

      }

    }

    AppLogger.LogSubstep("Processing notebooks");
    var notebooks = featureWorkspaceItems.Where(item => item.Type == ItemType.Notebook).ToList();
    var sourceNotebooks = sourceWorkspaceItems.Where(item => item.Type == ItemType.Notebook).ToList();
    foreach (var notebook in notebooks) {
      var sourceNotebook = sourceNotebooks.Where(item => item.DisplayName == notebook.DisplayName).First();

      dataPipelineRedirects.Add(sourceNotebook.Id.Value.ToString(), notebook.Id.Value.ToString());

      var notebookDefinition = FabricRestApi.GetItemDefinition(featureWorkspace.Id, notebook.Id.Value);

      notebookDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(notebookDefinition,
                                                                        "notebook-content.py",
                                                                        notebookRedirects);

      var updaterequest = new UpdateItemDefinitionRequest(notebookDefinition);

      FabricRestApi.UpdateItemDefinition(featureWorkspace.Id, notebook.Id.Value, updaterequest);


      if (notebook.DisplayName.Contains("Create")) {
        AppLogger.LogSubOperationStart($"Running  [{notebook.DisplayName}.Notebook]");
        FabricRestApi.RunNotebook(featureWorkspace.Id, notebook);
        AppLogger.LogOperationComplete();
      }

    }


    var dataPipelines = featureWorkspaceItems.Where(item => item.Type == ItemType.DataPipeline).ToList();
    var sourceDataPipelines = sourceWorkspaceItems.Where(item => item.Type == ItemType.DataPipeline).ToList();
    foreach (var dataPipeline in dataPipelines) {


      var pipelineDefinition = FabricRestApi.GetItemDefinition(featureWorkspace.Id, dataPipeline.Id.Value);

      pipelineDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(pipelineDefinition,
                                                                       "pipeline-content.json",
                                                                        dataPipelineRedirects);

      var updateRequest = new UpdateItemDefinitionRequest(pipelineDefinition);

      FabricRestApi.UpdateItemDefinition(featureWorkspace.Id, dataPipeline.Id.Value, updateRequest);

      if (dataPipeline.DisplayName.Contains("Create")) {
        AppLogger.LogSubOperationStart($"Running  [{dataPipeline.DisplayName}.DataPipeline]");
        FabricRestApi.RunDataPipeline(featureWorkspace.Id, dataPipeline);
        AppLogger.LogOperationComplete();
      }

    }

    foreach (var sqlEndPointId in sqlEndPointIds.Keys) {
      FabricRestApi.RefreshLakehouseTableSchema(sqlEndPointId);
    }

    AppLogger.LogSubstep("Processing semantic models");
    var semanticModels = featureWorkspaceItems.Where(item => item.Type == ItemType.SemanticModel).ToList();
    foreach (var model in semanticModels) {

      if (!lakehouseNames.Contains(model.DisplayName)) {


        var modelDefinition = FabricRestApi.GetItemDefinition(featureWorkspace.Id, model.Id.Value);

        modelDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(modelDefinition,
                                                                         "definition/expressions.tmdl",
                                                                         semanticModelRedirects);

        var updaterequest = new UpdateItemDefinitionRequest(modelDefinition);

        AppLogger.LogSubstep("Update semantic model definition");
        FabricRestApi.UpdateItemDefinition(featureWorkspace.Id, model.Id.Value, updaterequest);

        var semanticModelDatasource = PowerBiRestApi.GetDatasourcesForSemanticModel(featureWorkspace.Id, model.Id.Value).FirstOrDefault();

        Item lakehouse = null;
        if (semanticModelDatasource.DatasourceType.ToLower() == "sql") {
          lakehouse = sqlEndPointIds[semanticModelDatasource.ConnectionDetails.Database];
        }


        AppLogger.LogSubstep("Create connection and bind semantic model");
        CreateAndBindSemanticModelConnecton(featureWorkspace, model.Id.Value, lakehouse);

      }
    }

    Console.WriteLine();
    Console.WriteLine("Customer tenant provisioning complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();

    OpenWorkspaceInBrowser(featureWorkspace.Id);


  }

  #endregion

  #region Lab 09 Setup Staged Deployment for Enterprise Scenario

  public static void ExportWorkspaceToAdoSolutionFolder(string SourceWorkspace, string ProjectName, string BranchName = "") {
    ItemDefinitionFactory.ExportWorkspaceToAzureDevOps(SourceWorkspace, ProjectName, BranchName);
  }

  public static SolutionDeploymentPlan GetSolutionDeploymentFromAdoFolder(string ProjectName, DeploymentPlan Deployment, string BranchName = "") {

    var solutionDeploymentPlan = new SolutionDeploymentPlan(Deployment);

    AppLogger.LogStep($"Loading item definition files from solution folder in Azure Dev Ops");

    if (BranchName == "") {
      BranchName = AdoProjectManager.GetMostRecentDailyBuildBranch(ProjectName);
    }

    AppLogger.LogSubstep($"Loading from most recent build branch [{BranchName}]");

    var itemDefinitionFiles = AdoProjectManager.GetItemDefinitionFilesFromGitRepo(ProjectName, BranchName);

    var items = itemDefinitionFiles.OrderBy(item => item.FullPath);

    DeploymentItem currentItem = null;

    foreach (var item in items) {
      if (item.FileName == ".platform") {
        AppLogger.LogSubstep($"Loading [{item.ItemName}]");
        FabricPlatformFile platformFile = JsonSerializer.Deserialize<FabricPlatformFile>(item.Content);
        PlatformFileMetadata itemMetadata = platformFile.metadata;
        PlatformFileConfig config = platformFile.config;

        currentItem = new DeploymentItem {
          DisplayName = itemMetadata.displayName,
          LogicalId = config.logicalId,
          Type = itemMetadata.type,
          Definition = new ItemDefinition(new List<ItemDefinitionPart>())
        };

        solutionDeploymentPlan.DeploymentItems.Add(currentItem);
      }
      else {
        string encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Content));
        currentItem.Definition.Parts.Add(
          new ItemDefinitionPart(item.Path, encodedContent, PayloadType.InlineBase64)
        );
      }
    }

    AppLogger.LogSubstep($"Loading [deploy.config.json]");
    DeploymentConfiguration deployConfig = AdoProjectManager.GetDeployConfigFromGitRepo(ProjectName, BranchName);

    solutionDeploymentPlan.DeployConfig = deployConfig;

    return solutionDeploymentPlan;
  }

  public static void DeployFromAdoSolutionFolder(string ProjectName, string TargetWorkspaceName, DeploymentPlan Deployment) {

    AppLogger.LogSolution($"Deploying solution from ADO project [{ProjectName}] to workspace [{TargetWorkspaceName}]");

    DisplayDeploymentParameters(Deployment);

    var solutionDeployment = GetSolutionDeploymentFromAdoFolder(ProjectName, Deployment);

    DeployFromSolutionFolder(ProjectName, TargetWorkspaceName, solutionDeployment);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  public static void UpdateFromAdoSolutionFolder(string ProjectName, string TargetWorkspaceName, DeploymentPlan Deployment, string BuildName = "", bool DeleteOrphanedItems = false) {

    AppLogger.LogSolution($"Updating from ADO solution folder [{ProjectName}] to workspace [{TargetWorkspaceName}]");

    DisplayDeploymentParameters(Deployment);

    var solutionDeployment = GetSolutionDeploymentFromAdoFolder(ProjectName, Deployment, BuildName);

    UpdateFromSolutionFolder(ProjectName, TargetWorkspaceName, solutionDeployment, DeleteOrphanedItems);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  #endregion

  #region Lab 10 Setup Staged Deployment for Multitenant Scenario

  public static void DeployFromAdoSolutionFolder(string ProjectName, DeploymentPlan Deployment) {
    DeployFromAdoSolutionFolder(ProjectName, Deployment.TargetWorkspaceName, Deployment);
  }

  public static void UpdateFromAdoSolutionFolder(string ProjectName, DeploymentPlan Deployment, string BuildName = "", bool DeleteOrphanedItems = false) {
    UpdateFromAdoSolutionFolder(ProjectName, Deployment.TargetWorkspaceName, Deployment, BuildName, DeleteOrphanedItems);
  }



  // LOCAL



  #endregion

  #region Other interesting deployment workflow examples

  public static void DisplayConnectionsByWorkspace() {

    AppLogger.LogSolution("Workspace Connections");

    var workspaces = FabricRestApi.GetWorkspaces();

    foreach (var workspace in workspaces) {
      AppLogger.LogStep($"{workspace.DisplayName} [{workspace.Id.ToString()}]");
      var connections = FabricRestApi.GetWorkspaceConnections(workspace.Id);
      foreach (var connection in connections) {
        string connectionName = connection.DisplayName.Substring(48);
        if (!connection.DisplayName.Contains("Lakehouse")) {
          connectionName += $"-{connection.ConnectionDetails.Path}";
        }
        AppLogger.LogSubstep(connectionName);
      }
    }

  }

  public static void DeployFromSourceWorkspaceWithShallowCopy(string SourceWorkspaceName, string TargetWorkspaceName) {

    // NOTE: This function shows an example of what you ARE NOT supposed to do
    AppLogger.LogSolution($"Creating Shallow Copy of source workspace [{SourceWorkspaceName}] to [{TargetWorkspaceName}]");

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);
    var sourceWorkspaceItems = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id);

    AppLogger.LogStep($"Creating target workspace [{TargetWorkspaceName}]");
    var targetWorkspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{targetWorkspace.Id}]");

    List<ItemType> supportedItemTypes = new List<ItemType> {
      ItemType.Notebook,
      ItemType.DataPipeline,
      ItemType.SemanticModel,
      ItemType.Report
    };

    var lakehouseNames = sourceWorkspaceItems.Where(item => item.Type == ItemType.Lakehouse)
                                             .Select(item => item.DisplayName).ToList();

    foreach (var item in sourceWorkspaceItems) {

      if (item.Type == ItemType.Lakehouse) {
        AppLogger.LogStep($"Creating lakehouse named [{item.DisplayName}]");
        FabricRestApi.CreateLakehouse(targetWorkspace.Id, item.DisplayName);
        AppLogger.LogSubstep($"Lakehouse created with Id of [{item.Id.Value.ToString()}]");
      }
      else if (supportedItemTypes.Contains(item.Type)) {

        // ignore item if it is defalt semantic model for lakehouse
        if ((item.Type != ItemType.SemanticModel) ||
            (!lakehouseNames.Contains(item.DisplayName))) {

          AppLogger.LogStep($"Creating {item.Type.ToString()} named [{item.DisplayName}]");
          CreateItemRequest createRequest = new CreateItemRequest(item.DisplayName, item.Type);
          createRequest.Definition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, item.Id.Value);
          FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);
          AppLogger.LogSubstep($"{item.Type.ToString()} created with Id of [{item.Id.Value.ToString()}]");
        }
      }

    }

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.PromptUserToContinue();

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  public static void RebindSemanticModelToLakehouseInSameWorkspace(string TargetWorkspace, string TargetLakehouseName, string TargetSemanticModelName) {

    var workspace = FabricRestApi.GetWorkspaceByName(TargetWorkspace);

    var lakehouse = FabricRestApi.GetLakehouseByName(workspace.Id, TargetLakehouseName);

    var newSqlEndPoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);

    string newSqlEndpointServer = newSqlEndPoint.ConnectionString;
    string newSqlEndpointDatabase = newSqlEndPoint.Id;

    var semanticModel = FabricRestApi.GetSemanticModelByName(workspace.Id, TargetSemanticModelName);

    var oldDatasource = PowerBiRestApi.GetDatasourcesForSemanticModel(workspace.Id, semanticModel.Id.Value).First();

    string oldSqlEndpointServer = oldDatasource.ConnectionDetails.Server;
    string oldSqlEndpointDatabase = oldDatasource.ConnectionDetails.Database;

    var semanticModelRedirects = new Dictionary<string, string>() {
    { oldSqlEndpointServer,newSqlEndpointServer},
    { oldSqlEndpointDatabase, newSqlEndpointDatabase}
  };

    var oldModelDefinition = FabricRestApi.GetItemDefinition(workspace.Id, semanticModel.Id.Value);

    var newModelDefinition = ItemDefinitionFactory.UpdateItemDefinitionPart(oldModelDefinition,
                                                                            "definition/expressions.tmdl",
                                                                            semanticModelRedirects);

    var updateRequest = new UpdateItemDefinitionRequest(newModelDefinition);

    FabricRestApi.UpdateItemDefinition(workspace.Id, semanticModel.Id.Value, updateRequest);

    var sqlConnection = FabricRestApi.CreateSqlConnectionWithServicePrincipal(newSqlEndpointServer, newSqlEndpointDatabase);

    PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, semanticModel.Id.Value, sqlConnection.Id);

  }


  #endregion

}
