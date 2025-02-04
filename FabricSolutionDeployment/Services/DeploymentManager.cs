using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Fabric.Api.Core.Models;

public class DeploymentManager {

  public static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public static void DeployWorkspaceWithLakehouseSolution(string WorkspaceName) {

    string lakehouseName = "sales";
    string semanticModelName = "Product Sales DirectLake";

    AppLogger.LogSolution("Deploy Solution with Lakehouse, Notebook, DirectLake Semantic Model and Report");

    AppLogger.LogStep($"Creating new workspace [{WorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.FabricCapacityId);
    AppLogger.LogSubstep($"Workspace created with Id of [{workspace.Id.ToString()}]");

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    // create and run notebook to build bronze layer
    string notebook1Name = "Create Lakehouse Tables";
    AppLogger.LogStep($"Creating [{notebook1Name}.Notebook]");
    string notebook1Content = ItemDefinitionFactory.GetTemplateFile(@"Notebooks\CreateLakehouseTables.py");
    var notebook1CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequest(workspace.Id, lakehouse, notebook1Name, notebook1Content);
    var notebook1 = FabricRestApi.CreateItem(workspace.Id, notebook1CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook1.Id.Value.ToString()}]");
    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook1);
    AppLogger.LogOperationComplete();    

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    AppLogger.LogStep($"Creating [{semanticModelName}.SemanticModel]");
    var modelCreateRequest =
      ItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest(semanticModelName, sqlEndpoint.ConnectionString, sqlEndpoint.Id);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    AppLogger.LogSubstep($"Creating SQL connection for semantic model");
    var sqlConnection = FabricRestApi.CreateSqlConnectionWithServicePrincipal(sqlEndpoint.ConnectionString, sqlEndpoint.Id, workspace, lakehouse);

    AppLogger.LogSubstep($"Binding SQL connection to semantic model");
    PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, sqlConnection.Id);

    AppLogger.LogStep($"Creating [{semanticModelName}.Report]");

    var createRequestReport =
      ItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, semanticModelName);

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);
    AppLogger.LogSubstep($"Report created with Id of [{report.Id.Value.ToString()}]");

    AppLogger.LogStep("Workspace deployment complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

       AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

    OpenWorkspaceInBrowser(workspace.Id);


  }

  public static void DeployWorkspaceWithLakehouseSolutionWithMultipleNotebooks(string WorkspaceName) {

    string lakehouseName = "sales";
    string semanticModelName = "Product Sales DirectLake";

    AppLogger.LogSolution("Deploy Solution with Lakehouse, Notebook, DirectLake Semantic Model and Report");

    AppLogger.LogStep($"Creating new workspace [{WorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName, AppSettings.FabricCapacityId);
    AppLogger.LogSubstep($"Workspace created with Id of [{workspace.Id.ToString()}]");

    AppLogger.LogStep($"Creating [{lakehouseName}.Lakehouse]");
    var lakehouse = FabricRestApi.CreateLakehouse(workspace.Id, lakehouseName);
    AppLogger.LogSubstep($"Lakehouse created with Id of [{lakehouse.Id.Value.ToString()}]");

    // create and run notebook to build bronze layer
    string notebook1Name = "Build 01 Bronze Layer";
    AppLogger.LogStep($"Creating [{notebook1Name}.Notebook]");
    string notebook1Content = ItemDefinitionFactory.GetTemplateFile(@"Notebooks\Build01BronzeLayer.py");
    var notebook1CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequest(workspace.Id, lakehouse, notebook1Name, notebook1Content);
    var notebook1 = FabricRestApi.CreateItem(workspace.Id, notebook1CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook1.Id.Value.ToString()}]");
    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook1);
    AppLogger.LogOperationComplete();

    // create and run notebook to build bronze layer
    string notebook2Name = "Build 02 Silver Layer";
    AppLogger.LogStep($"Creating [{notebook2Name}.Notebook]");
    string notebook2Content = ItemDefinitionFactory.GetTemplateFile(@"Notebooks\Build02SilverLayer.py");
    var notebook2CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequest(workspace.Id, lakehouse, notebook2Name, notebook2Content);
    var notebook2 = FabricRestApi.CreateItem(workspace.Id, notebook2CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook2.Id.Value.ToString()}]");
    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook2);
    AppLogger.LogOperationComplete();

    // create and run notebook to build bronze layer
    string notebook3Name = "Build 03 Gold Layer";
    AppLogger.LogStep($"Creating [{notebook3Name}.Notebook]");
    string notebook3Content = ItemDefinitionFactory.GetTemplateFile(@"Notebooks\Build03GoldLayer.py");
    var notebook3CreateRequest = ItemDefinitionFactory.GetCreateNotebookRequest(workspace.Id, lakehouse, notebook3Name, notebook3Content);
    var notebook3 = FabricRestApi.CreateItem(workspace.Id, notebook3CreateRequest);
    AppLogger.LogSubstep($"Notebook created with Id of [{notebook3.Id.Value.ToString()}]");
    AppLogger.LogSubOperationStart($"Running notebook");
    FabricRestApi.RunNotebook(workspace.Id, notebook3);
    AppLogger.LogOperationComplete();

    AppLogger.LogStep("Querying lakehouse properties to get SQL endpoint connection info");
    var sqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(workspace.Id, lakehouse.Id.Value);
    AppLogger.LogSubstep($"Server: {sqlEndpoint.ConnectionString}");
    AppLogger.LogSubstep($"Database: " + sqlEndpoint.Id);

    AppLogger.LogStep($"Creating [{semanticModelName}.SemanticModel]");
    var modelCreateRequest =
      ItemDefinitionFactory.GetDirectLakeSalesModelCreateRequest(semanticModelName, sqlEndpoint.ConnectionString, sqlEndpoint.Id);

    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);

    AppLogger.LogSubstep($"Semantic model created with Id of [{model.Id.Value.ToString()}]");

    AppLogger.LogSubstep($"Creating SQL connection for semantic model");
    var sqlConnection = FabricRestApi.CreateSqlConnectionWithServicePrincipal(sqlEndpoint.ConnectionString, sqlEndpoint.Id, workspace, lakehouse);

    AppLogger.LogSubstep($"Binding SQL connection to semantic model");
    PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, sqlConnection.Id);

    AppLogger.LogStep($"Creating [{semanticModelName}.Report]");

    var createRequestReport =
      ItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, semanticModelName);

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);
    AppLogger.LogSubstep($"Report created with Id of [{report.Id.Value.ToString()}]");

    AppLogger.LogStep("Workspace deployment complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void DeployWorkspaceWithPowerBiSolution(string WorkspaceName) {

    string semanticModelName = "Product Sales Imported";

    AppLogger.LogSolution("Deploy Solution with Imported Sales Model and Report");

    AppLogger.LogStep($"Creating new workspace named [{WorkspaceName}]");
    var workspace = FabricRestApi.CreateWorkspace(WorkspaceName);
    AppLogger.LogSubstep($"New workspace created with Id of [{workspace.Id}]");

    AppLogger.LogStep($"Creating new import-mode semantic model named [{semanticModelName}]");
    var modelCreateRequest =
      ItemDefinitionFactory.GetImportedSalesModelCreateRequest(semanticModelName);
    var model = FabricRestApi.CreateItem(workspace.Id, modelCreateRequest);
    AppLogger.LogSubstep($"New semantic model created with Id of [{model.Id.Value.ToString()}]");

    AppLogger.LogSubstep($"Creating new connection for semantic model");
    var url = PowerBiRestApi.GetWebDatasourceUrl(workspace.Id, model.Id.Value);
    var connection = FabricRestApi.CreateAnonymousWebConnection(url);

    AppLogger.LogSubstep($"Binding connection to semantic model");
    PowerBiRestApi.BindSemanticModelToConnection(workspace.Id, model.Id.Value, connection.Id);

    AppLogger.LogSubstep($"Refreshing semantic model");
    PowerBiRestApi.RefreshDataset(workspace.Id, model.Id.Value);

    AppLogger.LogStep($"Creating new report named [{semanticModelName}]");

    var createRequestReport =
      ItemDefinitionFactory.GetSalesReportCreateRequest(model.Id.Value, semanticModelName);

    var report = FabricRestApi.CreateItem(workspace.Id, createRequestReport);

    AppLogger.LogSubstep($"New report created with Id of [{report.Id.Value.ToString()}]");

    AppLogger.LogStep("Solution deployment complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    Console.ReadLine();
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

    OpenWorkspaceInBrowser(workspace.Id);

  }

  public static void CreateAndBindSemanticModelConnectons(Guid WorkspaceId, Guid SemanticModelId) {

    var datasources = PowerBiRestApi.GetDatasourcesForSemanricModels(WorkspaceId, SemanticModelId);

    foreach (var datasource in datasources) {

      if (datasource.DatasourceType.ToLower() == "sql") {

        string sqlEndPointServer = datasource.ConnectionDetails.Server;
        string sqlEndPointDatabase = datasource.ConnectionDetails.Database;

        // you cannot create the connection until your configure a service principal
        if (AppSettings.ServicePrincipalObjectId != "00000000-0000-0000-0000-000000000000") {
          AppLogger.LogSubstep($"Creating SQL connection for semantic model");
          var sqlConnection = FabricRestApi.CreateSqlConnectionWithServicePrincipal(sqlEndPointServer, sqlEndPointDatabase);
          AppLogger.LogSubstep($"Binding connection to semantic model");
          PowerBiRestApi.BindSemanticModelToConnection(WorkspaceId, SemanticModelId, sqlConnection.Id);
        }

      }

      if (datasource.DatasourceType.ToLower() == "web") {
        string url = datasource.ConnectionDetails.Url;

        AppLogger.LogSubstep($"Creating Web connection for semantic model");
        var webConnection = FabricRestApi.CreateAnonymousWebConnection(url);

        AppLogger.LogSubstep($"Binding connection to semantic model");
        PowerBiRestApi.BindSemanticModelToConnection(WorkspaceId, SemanticModelId, webConnection.Id);

        AppLogger.LogSubstep($"Refreshing semantic model");
        PowerBiRestApi.RefreshDataset(WorkspaceId, SemanticModelId);

      }

    }
  }

  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {
    FabricRestApi.ExportItemDefinitionsFromWorkspace(WorkspaceName);
  }

  public static void DeploySolutionFromWorkspaceTemplate(string SourceWorkspaceName, string TargetWorkspaceName) {

    AppLogger.LogStep($"Deploying Solution from workspace template [{SourceWorkspaceName}] to workspace [{TargetWorkspaceName}]");

    // create data collections to track substitution data
    var lakehouseNames = new List<string>();
    var notebookRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);
    var targetWorkspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);

    // add redirect for workspace id
    notebookRedirects.Add(sourceWorkspace.Id.ToString(), targetWorkspace.Id.ToString());

    AppLogger.LogStep($"Deploying Workspace Items");

    var lakehouses = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Lakehouse");
    foreach (var sourceLakehouse in lakehouses) {

      Guid sourceLakehouseId = sourceLakehouse.Id.Value;
      var sourceLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(sourceWorkspace.Id, sourceLakehouse.Id.Value);

      AppLogger.LogSubstep($"Creating [{sourceLakehouse.DisplayName}.Lakehouse]");
      var targetLakehouse = FabricRestApi.CreateLakehouse(targetWorkspace.Id, sourceLakehouse.DisplayName);

      var targetLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(targetWorkspace.Id, targetLakehouse.Id.Value);

      // add lakehouse names to detect lakehouse default semantic model
      lakehouseNames.Add(targetLakehouse.DisplayName);

      // add redirect for lakehouse id
      notebookRedirects.Add(sourceLakehouse.Id.Value.ToString(), targetLakehouse.Id.Value.ToString());

      // add redirect for sql endpoint database name 
      semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.Id, targetLakehouseSqlEndpoint.Id);

      // add redirect for sql endpoint server location
      if (!semanticModelRedirects.Keys.Contains(sourceLakehouseSqlEndpoint.ConnectionString)) {
        // only add sql endpoint server location once because it has same value for all lakehouses in the same workspace
        semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.ConnectionString, targetLakehouseSqlEndpoint.ConnectionString);
      }

    }

    var notebooks = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Notebook");
    foreach (var sourceNotebook in notebooks) {
      AppLogger.LogSubstep($"Creating [{sourceNotebook.DisplayName}.Notebook]");
      var createRequest = new CreateItemRequest(sourceNotebook.DisplayName, sourceNotebook.Type);

      var notebookDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceNotebook.Id.Value);

      createRequest.Definition = FabricRestApi.UpdateItemDefinitionPart(notebookDefinition,
                                                                        "notebook-content.py",
                                                                        notebookRedirects);

      var targetNotebook = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

      AppLogger.LogSubOperationStart($"Running  [{sourceNotebook.DisplayName}.Notebook]");
      FabricRestApi.RunNotebook(targetWorkspace.Id, targetNotebook);
      AppLogger.LogOperationComplete();

    }

    var models = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in models) {

      // ignore default semantic models for lakehouses
      if (!lakehouseNames.Contains(sourceModel.DisplayName)) {

        AppLogger.LogSubstep($"Creating [{sourceModel.DisplayName}.SemanticModel]");

        // get model definition from source workspace
        var sourceModelDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceModel.Id.Value);

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = FabricRestApi.UpdateItemDefinitionPart(sourceModelDefinition,
                                                                     "definition/expressions.tmdl",
                                                                     semanticModelRedirects);

        // use item definition to create clone in target workspace
        var createRequest = new CreateItemRequest(sourceModel.DisplayName, sourceModel.Type);
        createRequest.Definition = modelDefinition;
        var targetModel = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

        // track mapping between source semantic model and target semantic model
        reportRedirects.Add(sourceModel.Id.Value.ToString(), targetModel.Id.Value.ToString());

        CreateAndBindSemanticModelConnectons(targetWorkspace.Id, targetModel.Id.Value);

      }

    }

    var reports = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "Report");
    foreach (var sourceReport in reports) {

      // get model definition from source workspace
      var sourceReportDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceReport.Id.Value);

      // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
      var reportDefinition = FabricRestApi.UpdateItemDefinitionPart(sourceReportDefinition,
                                                                   "definition.pbir",
                                                                   reportRedirects);

      // use item definition to create clone in target workspace
      AppLogger.LogSubstep($"Creating [{sourceReport.DisplayName}.Report]");
      var createRequest = new CreateItemRequest(sourceReport.DisplayName, sourceReport.Type);
      createRequest.Definition = reportDefinition;
      var targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

    }

    AppLogger.LogStep("Solution deployment from workspace template complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  public static void UpdateSolutionFromWorkspaceTemplate(string SourceWorkspaceName, string TargetWorkspaceName, bool DeleteOrphanedItems = true) {

    AppLogger.LogStep($"Updating Solution from workspace template [{SourceWorkspaceName}] to workspace [{TargetWorkspaceName}]");

    // create data collections to track substitution data
    var lakehouseNames = new List<string>();
    var notebookRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SourceWorkspaceName);
    var sourceWorkspaceItems = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);
    var targetWorkspaceItems = FabricRestApi.GetWorkspaceItems(targetWorkspace.Id);

    // add redirect for workspace id
    notebookRedirects.Add(sourceWorkspace.Id.ToString(), targetWorkspace.Id.ToString());

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

      var sourceNotebookDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceNotebook.Id.Value);

      var notebookDefinition = FabricRestApi.UpdateItemDefinitionPart(sourceNotebookDefinition,
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


    }

    // create or update semantic models
    var models = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id, "SemanticModel");
    foreach (var sourceModel in models) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(sourceModel.DisplayName)) {

        // get model definition from source workspace
        var sourceModelDefinition = FabricRestApi.GetItemDefinition(sourceWorkspace.Id, sourceModel.Id.Value);

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = FabricRestApi.UpdateItemDefinitionPart(sourceModelDefinition,
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

          CreateAndBindSemanticModelConnectons(targetWorkspace.Id, targetModel.Id.Value);

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

      // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
      var reportDefinition = FabricRestApi.UpdateItemDefinitionPart(sourceReportDefinition,
                                                                   "definition.pbir",
                                                                   reportRedirects);

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

    AppLogger.LogStep("Solution update from workspace template complete");

    AppLogger.LogOperationStart("Press ENTER to open workspace in the browser");
    AppLogger.LogOperationComplete();
    AppLogger.LogStep("");

    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  public static void ConnectWorkspaceToGit(string WorkspaceName, string BranchName = "main") {

    var workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);

    // create new project in Azure Dev Ops
    AdoProjectManager.CreateProject(WorkspaceName, workspace);

    var gitConnectRequest = new GitConnectRequest(
      new AzureDevOpsDetails(WorkspaceName, BranchName,
                                            "/",
                                            AppSettings.AzureDevOpsOrganizationName,
                                            WorkspaceName));

    FabricRestApi.ConnectWorkspaceToGitRepository(workspace.Id, gitConnectRequest);

    AdoProjectManager.CreateBranch(WorkspaceName, "dev");

    AppLogger.LogOperationStart("Workspace connection to GIT has been created and synchronized");


  }

  public static string GenerateDeployConfigFile(Workspace Workspace, List<Item> WorkspaceItems) {

    DeploymentConfiguration deployConfig = new DeploymentConfiguration {
      SourceItems = new List<DeploymentSourceItem>(),
      SourceLakehouses = new List<DeploymentSourceLakehouse>(),
      SourceConnections = new List<DeploymentSourceConnection>()
    };

    deployConfig.SourceWorkspaceId = Workspace.Id.ToString();

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
             Location = shortcut.Target.AdlsGen2.Location.ToString(),
              Name = shortcut.Name,
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

  public static void PushDeployConfigToGitRepo(string WorkspaceName) {

    AppLogger.LogStep($"Pushng [deploy.config.json] to project [{WorkspaceName}]");

    DeleteSolutionFolderContents(WorkspaceName);

    var workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);
    var items = FabricRestApi.GetWorkspaceItems(workspace.Id);

    var config = GenerateDeployConfigFile(workspace, items);

    

    AdoProjectManager.PushFileToGitRepo(WorkspaceName, "deploy.config.json",config); 

  }

  public static void ExportSolutionFolderFromWorkspace(string WorkspaceName) {

    AppLogger.LogStep($"Exporting workspace item definitions to solution folder [{WorkspaceName}]");

    DeleteSolutionFolderContents(WorkspaceName);

    var workspace = FabricRestApi.GetWorkspaceByName(WorkspaceName);
    var items = FabricRestApi.GetWorkspaceItems(workspace.Id);

    // list of items types that should be exported
    List<ItemType> itemTypesForExport = new List<ItemType>() {
      ItemType.Notebook, ItemType.SemanticModel, ItemType.Report
    };

    foreach (var item in items) {
      if (itemTypesForExport.Contains(item.Type)) {

        // fetch item definition from workspace
        var definition = FabricRestApi.GetItemDefinition(workspace.Id, item.Id.Value);

        // write item definition files to local folder
        string targetFolder = item.DisplayName + "." + item.Type;

        AppLogger.LogSubstep($"Exporting item definition for [{targetFolder}]");

        foreach (var part in definition.Parts) {
          WriteFileToSolutionFolder(WorkspaceName, targetFolder, part.Path, part.Payload);
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

      WriteFileToSolutionFolder(WorkspaceName, targetFolder, platformFileName, platformFileContent, false);

    }

    AppLogger.LogSubstep($"Exporting [deploy.config.json]");

    var config = GenerateDeployConfigFile(workspace, items);

    WriteFileToSolutionFolder(WorkspaceName, "", "deploy.config.json", config, false);

    AppLogger.LogStep("Workspace item definition exporting process completed");

  }

  public static void DeleteSolutionFolderContents(string WorkspaceName) {
    string targetFolder = AppSettings.LocalSolutionTemplatesFolder + WorkspaceName + @"\";
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
    string folderPath = AppSettings.LocalSolutionTemplatesFolder + WorkspaceFolder + @"\" + ItemFolder;

    Directory.CreateDirectory(folderPath);

    string fullPath = folderPath + @"\" + FilePath;

    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

    File.WriteAllText(fullPath, FileContent);

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
      var reportDefinitionPart = CreateInlineBase64Part("definition.pbir", reportDefinitionPartContent);
      ReportDefinition.Parts.Add(reportDefinitionPart);
      return ReportDefinition;
    }
    else {
      return FabricRestApi.UpdateItemDefinitionPart(ReportDefinition,
                                                    "definition.pbir",
                                                    ReportRedirects);
    }

  }

  public static ItemDefinition UpdateReportDefinitionWithSemanticModelId(ItemDefinition ItemDefinition, Guid WorkspaceId, string TargetModelName) {
    var targetModel = FabricRestApi.GetSemanticModelByName(WorkspaceId, TargetModelName);
    Guid targetModelId = targetModel.Id.Value;

    var partDefinition = ItemDefinition.Parts.Where(part => part.Path == "definition.pbir").First();
    ItemDefinition.Parts.Remove(partDefinition);
    string reportDefinitionPartTemplate = ItemDefinitionFactory.GetTemplateFile(@"Reports\definition.pbir");
    string reportDefinitionPartContent = reportDefinitionPartTemplate.Replace("{SEMANTIC_MODEL_ID}", targetModel.Id.ToString());
    var reportDefinitionPart = CreateInlineBase64Part("definition.pbir", reportDefinitionPartContent);
    ItemDefinition.Parts.Add(reportDefinitionPart);
    return ItemDefinition;


  }

  private static ItemDefinitionPart CreateInlineBase64Part(string Path, string Payload) {
    string base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Payload));
    return new ItemDefinitionPart(Path, base64Payload, PayloadType.InlineBase64);
  }

  // generic
  public static void DeploySolution(string SolutionFolder, string TargetWorkspaceName, SolutionDeploymentPlan DeploymentPlan) {

    AppLogger.LogStep($"Deploying from solution folder [{SolutionFolder}] to new workspace [{TargetWorkspaceName}]");

    AppLogger.LogStep($"Creating new workspace named [{TargetWorkspaceName}]");
    var targetWorkspace = FabricRestApi.CreateWorkspace(TargetWorkspaceName);
    AppLogger.LogSubstep($"Workspace created with id of {targetWorkspace.Id.ToString()}");

    AppLogger.LogStep($"Deploying Workspace Items");

    var lakehouseNames = new List<string>();
    var notebookRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    notebookRedirects.Add(DeploymentPlan.GetSourceWorkspaceId(), targetWorkspace.Id.ToString());

    foreach (var lakehouse in DeploymentPlan.GetLakehouses()) {

      var sourceLakehouse = DeploymentPlan.GetSourceLakehouse(lakehouse.DisplayName);

      AppLogger.LogSubstep($"Creating [{lakehouse.ItemName}]");
      var targetLakehouse = FabricRestApi.CreateLakehouse(targetWorkspace.Id, lakehouse.DisplayName);

      var targetLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(targetWorkspace.Id, targetLakehouse.Id.Value);

      lakehouseNames.Add(targetLakehouse.DisplayName);

      notebookRedirects.Add(sourceLakehouse.Id, targetLakehouse.Id.Value.ToString());

      if (!semanticModelRedirects.Keys.Contains(sourceLakehouse.Server)) {
        semanticModelRedirects.Add(sourceLakehouse.Server, targetLakehouseSqlEndpoint.ConnectionString);
      }

      semanticModelRedirects.Add(sourceLakehouse.Database, targetLakehouseSqlEndpoint.Id);

    }

    foreach (var notebook in DeploymentPlan.GetNotebooks()) {
      AppLogger.LogSubstep($"Creating [{notebook.ItemName}]");
      var createRequest = new CreateItemRequest(notebook.DisplayName, notebook.Type);
      createRequest.Definition = FabricRestApi.UpdateItemDefinitionPart(notebook.Definition, "notebook-content.py", notebookRedirects);
      var targetNotebook = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

      AppLogger.LogSubOperationStart($"Running  [{notebook.ItemName}]");
      FabricRestApi.RunNotebook(targetWorkspace.Id, targetNotebook);
      AppLogger.LogOperationComplete();

    }

    foreach (var model in DeploymentPlan.GetSemanticModels()) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(model.DisplayName)) {

        var sourceModel = DeploymentPlan.GetSourceSemanticModel(model.DisplayName);

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = FabricRestApi.UpdateItemDefinitionPart(model.Definition, "definition/expressions.tmdl", semanticModelRedirects);

        // use item definition to create clone in target workspace
        AppLogger.LogSubstep($"Creating [{model.ItemName}]");
        var createRequest = new CreateItemRequest(model.DisplayName, model.Type);
        createRequest.Definition = modelDefinition;
        var targetModel = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

        // track mapping between source semantic model and target semantic model
        semanticModelRedirects.Add(sourceModel.Id, targetModel.Id.Value.ToString());

        CreateAndBindSemanticModelConnectons(targetWorkspace.Id, targetModel.Id.Value);

      }

    }

    foreach (var report in DeploymentPlan.GetReports()) {

      var sourceReport = DeploymentPlan.GetSourceReport(report.DisplayName);

      // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
      var reportDefinition = UpdateReportDefinitionWithRedirection(report.Definition, targetWorkspace.Id, reportRedirects);

      // use item definition to create clone in target workspace
      AppLogger.LogSubstep($"Creating [{report.ItemName}]");
      var createRequest = new CreateItemRequest(report.DisplayName, report.Type);
      createRequest.Definition = reportDefinition;
      var targetReport = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);

    }

    Console.WriteLine();
    Console.WriteLine("Solution deployment complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();


    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  public static void UpdateSolution(string SolutionFolder, string TargetWorkspaceName, SolutionDeploymentPlan DeploymentPlan) {

    AppLogger.LogStep($"Deploying updates from solution [{SolutionFolder}] to workspace [{TargetWorkspaceName}]");

    AppLogger.LogStep($"Processing workspace item updates");

    var sourceWorkspace = FabricRestApi.GetWorkspaceByName(SolutionFolder);
    var sourceWorkspaceItems = FabricRestApi.GetWorkspaceItems(sourceWorkspace.Id);

    var targetWorkspace = FabricRestApi.GetWorkspaceByName(TargetWorkspaceName);
    var targetWorkspaceItems = FabricRestApi.GetWorkspaceItems(targetWorkspace.Id);

    var lakehouseNames = new List<string>();
    var notebookRedirects = new Dictionary<string, string>();
    var semanticModelRedirects = new Dictionary<string, string>();
    var reportRedirects = new Dictionary<string, string>();

    notebookRedirects.Add(sourceWorkspace.Id.ToString(), targetWorkspace.Id.ToString());

    var lakehouses = DeploymentPlan.DeploymentItems.Where(item => item.Type == "Lakehouse");
    foreach (var lakehouse in lakehouses) {

      var sourceLakehouse = sourceWorkspaceItems.FirstOrDefault(item => (item.Type == "Lakehouse" &&
                                                                         item.DisplayName.Equals(lakehouse.DisplayName)));

      Guid sourceLakehouseId = sourceWorkspaceItems.FirstOrDefault(item => item.Type == "Lakehouse").Id.Value;
      var sourceLakehouseSqlEndpoint = FabricRestApi.GetSqlEndpointForLakehouse(sourceWorkspace.Id, sourceLakehouse.Id.Value);

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

      lakehouseNames.Add(targetLakehouse.DisplayName);
      notebookRedirects.Add(sourceLakehouse.Id.Value.ToString(), targetLakehouse.Id.Value.ToString());

      if (!semanticModelRedirects.Keys.Contains(sourceLakehouseSqlEndpoint.ConnectionString)) {
        semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.ConnectionString, targetLakehouseSqlEndpoint.ConnectionString);
      }
      semanticModelRedirects.Add(sourceLakehouseSqlEndpoint.Id, targetLakehouseSqlEndpoint.Id);

    }

    var notebooks = DeploymentPlan.DeploymentItems.Where(item => item.Type == "Notebook");
    foreach (var notebook in notebooks) {
      var sourceNoteboook = sourceWorkspaceItems.FirstOrDefault(item => (item.Type == "Notebook" &&
                                                                         item.DisplayName.Equals(notebook.DisplayName)));

      var targetNotebook = targetWorkspaceItems.Where(item => (item.DisplayName.Equals(notebook.DisplayName) &&
                                                              (item.Type == notebook.Type))).FirstOrDefault();

      ItemDefinition notebookDefiniton = FabricRestApi.UpdateItemDefinitionPart(notebook.Definition, "notebook-content.py", notebookRedirects);

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
        createRequest.Definition = FabricRestApi.UpdateItemDefinitionPart(notebook.Definition, "notebook-content.py", notebookRedirects);
        targetNotebook = FabricRestApi.CreateItem(targetWorkspace.Id, createRequest);
      }

      // do not run notebooks by default when updating - uncomment to change
      // AppLogger.LogSubOperationStart($"Running notebook");
      // FabricRestApi.RunNotebook(targetWorkspace.Id, targetNotebook);
      // AppLogger.LogOperationComplete();

    }

    var models = DeploymentPlan.DeploymentItems.Where(item => item.Type == "SemanticModel");

    foreach (var model in models) {

      // ignore default semantic model for lakehouse
      if (!lakehouseNames.Contains(model.DisplayName)) {

        var sourceModel = sourceWorkspaceItems.FirstOrDefault(item => (item.Type == "SemanticModel" &&
                                                                       item.DisplayName == model.DisplayName));

        var targetModel = targetWorkspaceItems.Where(item => (item.Type == model.Type) &&
                                                             (item.DisplayName == model.DisplayName)).FirstOrDefault();

        // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
        var modelDefinition = FabricRestApi.UpdateItemDefinitionPart(model.Definition, "definition/expressions.tmdl", semanticModelRedirects);

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

          CreateAndBindSemanticModelConnectons(targetWorkspace.Id, targetModel.Id.Value);
        }

        // track mapping between source semantic model and target semantic model
        semanticModelRedirects.Add(sourceModel.Id.Value.ToString(), targetModel.Id.Value.ToString());

      }

    }

    // reports
    var reports = DeploymentPlan.DeploymentItems.Where(item => item.Type == "Report");
    foreach (var report in reports) {

      var sourceReport = sourceWorkspaceItems.FirstOrDefault(item => (item.Type == "Report" &&
                                                                      item.DisplayName.Equals(report.DisplayName)));

      var targetReport = targetWorkspaceItems.FirstOrDefault(item => (item.Type == "Report" &&
                                                                   item.DisplayName.Equals(report.DisplayName)));


      // update expressions.tmdl with SQL endpoint info for lakehouse in feature workspace
      var reportDefinition = UpdateReportDefinitionWithRedirection(report.Definition, targetWorkspace.Id, reportRedirects);


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

    Console.WriteLine();
    Console.WriteLine("Solution deployment update complete");
    Console.WriteLine();

    Console.Write("Press ENTER to open workspace in the browser");
    Console.ReadLine();


    OpenWorkspaceInBrowser(targetWorkspace.Id);

  }

  // from ADO
  public static SolutionDeploymentPlan GetSolutionDeploymentPlanFromGitRepo(string SolutionFolder) {

    AppLogger.LogStep($"Loading item definition files from GIT repository");

    var items = AdoProjectManager.GetItemsFromGitRepo(SolutionFolder);

    var SolutionDeployment = new SolutionDeploymentPlan {
      DeploymentItems = new List<DeploymentItem>()
    };

    DeploymentItem currentItem = null;

    foreach (var item in items) {
      if (item.FileName == ".platform") {
        AppLogger.LogSubstep($"Loading [{item.ItemName}]");
        PlatformFileMetadata itemMetadata = JsonSerializer.Deserialize<FabricPlatformFile>(item.Content).metadata;

        currentItem = new DeploymentItem {
          DisplayName = itemMetadata.displayName,
          Type = itemMetadata.type,
          Definition = new ItemDefinition(new List<ItemDefinitionPart>())
        };

        SolutionDeployment.DeploymentItems.Add(currentItem);
      }
      else {
        string encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Content));
        currentItem.Definition.Parts.Add(
          new ItemDefinitionPart(item.Path, encodedContent, PayloadType.InlineBase64)
        );
      }
    }

    AppLogger.LogSubstep($"Loading [deploy.config.json]");
    SolutionDeployment.DeployConfig = AdoProjectManager.GetDeployConfigFromGitRepo(SolutionFolder);

    return SolutionDeployment;

  }

  public static void DeploySolutionFromProjectTemplate(string SolutionFolder, string TargetWorkspaceName) {

    AppLogger.LogStep($"Deploying ADO Project [{SolutionFolder}] to new workspace [{TargetWorkspaceName}]");

    var SolutionDeployment = GetSolutionDeploymentPlanFromGitRepo(SolutionFolder);

    DeploySolution(SolutionFolder, TargetWorkspaceName, SolutionDeployment);

  }

  public static void UpdateSolutionFromProjectTemplate(string SolutionFolder, string TargetWorkspaceName) {

    AppLogger.LogStep($"Deploying updates from ADO Project [{SolutionFolder}] to workspace [{TargetWorkspaceName}]");

    var SolutionDeployment = GetSolutionDeploymentPlanFromGitRepo(SolutionFolder);

    UpdateSolution(SolutionFolder, TargetWorkspaceName, SolutionDeployment);
  }

  // from local solution folder

  public static SolutionDeploymentPlan GetSolutionDeploymentPlanFromSolutionFolder(string SolutionFolder) {

    var solutionDeployment = new SolutionDeploymentPlan {
      DeploymentItems = new List<DeploymentItem>(),      
   };

    AppLogger.LogStep($"Loading deploy.config.json file from local solutions folder");
    string deployConfigPath = AppSettings.LocalSolutionTemplatesFolder + SolutionFolder + @"\deploy.config.json";
    string deployConfigContent = File.ReadAllText(deployConfigPath);
    DeploymentConfiguration deployConfig = JsonSerializer.Deserialize<DeploymentConfiguration>(deployConfigContent, jsonSerializerOptions);

    solutionDeployment.DeployConfig = deployConfig;

    AppLogger.LogStep($"Loading item definition files from local solutions folder");

    var itemDefinitionFiles = new List<ItemDefinitonFile>();

    string folderPath = AppSettings.LocalSolutionTemplatesFolder + SolutionFolder + @"\";
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
        PlatformFileMetadata itemMetadata = JsonSerializer.Deserialize<FabricPlatformFile>(item.Content).metadata;

        currentItem = new DeploymentItem {
          DisplayName = itemMetadata.displayName,
          Type = itemMetadata.type,
          Definition = new ItemDefinition(new List<ItemDefinitionPart>())
        };

        solutionDeployment.DeploymentItems.Add(currentItem);
      }
      else {
        string encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Content));
        currentItem.Definition.Parts.Add(
          new ItemDefinitionPart(item.Path, encodedContent, PayloadType.InlineBase64)
        );
      }
    }

    return solutionDeployment;
  }

  public static void DeploySolutionFromSolutionFolder(string SolutionFolder, string TargetWorkspaceName) {

    AppLogger.LogStep($"Deploying local solution [{SolutionFolder}] to new workspace [{TargetWorkspaceName}]");

    var SolutionDeployment = GetSolutionDeploymentPlanFromSolutionFolder(SolutionFolder);

    DeploySolution(SolutionFolder, TargetWorkspaceName, SolutionDeployment);

  }  
 
  public static void UpdateSolutionFromSolutionFolder(string SolutionFolder, string TargetWorkspaceName) {

    AppLogger.LogStep($"Updating workspace [{TargetWorkspaceName}] from solution folder [{SolutionFolder}]");

    var SolutionDeployment = GetSolutionDeploymentPlanFromSolutionFolder(SolutionFolder);

    UpdateSolution(SolutionFolder, TargetWorkspaceName, SolutionDeployment);

  }

  // utilities

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

    string url = "https://app.powerbi.com/groups/" + WorkspaceId;

    string chromeBrowserProfileName = "Profile 7";

    var process = new Process();
    process.StartInfo = new ProcessStartInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
    process.StartInfo.Arguments = url + $" --profile-directory=\"{chromeBrowserProfileName}\" ";
    process.Start();

  }

}
