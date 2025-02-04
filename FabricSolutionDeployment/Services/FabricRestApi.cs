using Microsoft.Fabric;
using FabricAdmin = Microsoft.Fabric.Api.Admin.Models;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Notebook.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using Microsoft.Fabric.Api.Warehouse.Models;
using Microsoft.Fabric.Api.SemanticModel.Models;
using Microsoft.Fabric.Api.Report.Models;
using Microsoft.Fabric.Api.Utils;
using System.Text;

public class FabricRestApi {

  private static string accessToken;
  private static FabricClient fabricApiClient;

  static FabricRestApi() {
    accessToken = EntraIdTokenManager.GetFabricAccessToken();
    fabricApiClient = new FabricClient(accessToken);
  }

  public static List<Workspace> GetWorkspaces() {

    // get all workspaces (this includes My Workspapce)
    var allWorkspaces = fabricApiClient.Core.Workspaces.ListWorkspaces().ToList();

    // filter out My Workspace
    return allWorkspaces.Where(workspace => workspace.Type == WorkspaceType.Workspace).ToList();
  }

  public static List<Capacity> GetCapacities() {
    return fabricApiClient.Core.Capacities.ListCapacities().ToList();
  }

  public static Workspace GetWorkspaceByName(string WorkspaceName) {
    var workspaces = fabricApiClient.Core.Workspaces.ListWorkspaces().ToList();

    foreach (var workspace in workspaces) {
      if (workspace.DisplayName.Equals(WorkspaceName)) {
        return workspace;
      }
    }

    return null;
  }

  public static Workspace CreateWorkspace(string WorkspaceName, string CapacityId = AppSettings.FabricCapacityId, string Description = null) {

    var workspace = GetWorkspaceByName(WorkspaceName);

    // delete workspace with same name if it exists
    if (workspace != null) {
      DeleteWorkspace(workspace.Id);
      workspace = null;
    }

    var createRequest = new CreateWorkspaceRequest(WorkspaceName);
    createRequest.Description = Description;

    workspace = fabricApiClient.Core.Workspaces.CreateWorkspace(createRequest);


    if (CapacityId != null) {
      var capacityId = new Guid(CapacityId);
      AssignWorkspaceToCapacity(workspace.Id, capacityId);
    }

    if (AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth &&
       (AppSettings.AdminUserId != "00000000-0000-0000-0000-000000000000")) {
      Guid AdminUserId = new Guid(AppSettings.AdminUserId);
      FabricRestApi.AddUserAsWorkspaceMember(workspace.Id, AdminUserId, WorkspaceRole.Admin);
    }
    else {
      if (AppSettings.ServicePrincipalObjectId != "00000000-0000-0000-0000-000000000000") {
        Guid ServicePrincipalObjectId = new Guid(AppSettings.ServicePrincipalObjectId);
        FabricRestApi.AddServicePrincipalAsWorkspaceMember(workspace.Id, ServicePrincipalObjectId, WorkspaceRole.Admin);
      }

    }

    return workspace;
  }

  public static Workspace UpdateWorkspace(Guid WorkspaceId, string WorkspaceName, string Description = null) {

    var updateRequest = new UpdateWorkspaceRequest {
      DisplayName = WorkspaceName,
      Description = Description
    };

    return fabricApiClient.Core.Workspaces.UpdateWorkspace(WorkspaceId, updateRequest).Value;
  }

  public static void DeleteWorkspace(Guid WorkspaceId) {

    DeleteWorkspaceResources(WorkspaceId);

    fabricApiClient.Core.Workspaces.DeleteWorkspace(WorkspaceId);
  }

  public static void DeleteWorkspaceByName(string WorkspaceName) {
    var workspace = GetWorkspaceByName(WorkspaceName);
    DeleteWorkspace(workspace.Id);
  }

  public static void DeleteWorkspaceResources(Guid WorkspaceId) {
    var connections = GetConnections();
    foreach (var connection in connections) {
      if (connection.DisplayName.Contains(WorkspaceId.ToString())) {
        DeleteConnection(connection.Id);
      }
    }
  }

  public static void AssignWorkspaceToCapacity(Guid WorkspaceId, Guid CapacityId) {
    var assignRequest = new AssignWorkspaceToCapacityRequest(CapacityId);
    fabricApiClient.Core.Workspaces.AssignToCapacity(WorkspaceId, assignRequest);
  }

  public static void ProvisionWorkspaceIdentity(Guid WorkspaceId) {
    fabricApiClient.Core.Workspaces.ProvisionIdentity(WorkspaceId);
  }

  public static void AddUserAsWorkspaceMember(Guid WorkspaceId, Guid UserId, WorkspaceRole RoleAssignment) {
    var user = new Principal(UserId, PrincipalType.User);
    var roleAssignment = new AddWorkspaceRoleAssignmentRequest(user, RoleAssignment);
    fabricApiClient.Core.Workspaces.AddWorkspaceRoleAssignment(WorkspaceId, roleAssignment);
  }

  public static void AddGroupAsWorkspaceMember(Guid WorkspaceId, Guid GroupId, WorkspaceRole RoleAssignment) {
    var group = new Principal(GroupId, PrincipalType.Group);
    var roleAssignment = new AddWorkspaceRoleAssignmentRequest(group, RoleAssignment);
    fabricApiClient.Core.Workspaces.AddWorkspaceRoleAssignment(WorkspaceId, roleAssignment);
  }

  public static void AddServicePrincipalAsWorkspaceMember(Guid WorkspaceId, Guid ServicePrincipalObjectId, WorkspaceRole RoleAssignment) {
    var user = new Principal(ServicePrincipalObjectId, PrincipalType.ServicePrincipal);
    var roleAssignment = new AddWorkspaceRoleAssignmentRequest(user, RoleAssignment);
    fabricApiClient.Core.Workspaces.AddWorkspaceRoleAssignment(WorkspaceId, roleAssignment);
  }

  public static void ViewWorkspaceRoleAssignments(Guid WorkspaceId) {

    var roleAssignments = fabricApiClient.Core.Workspaces.ListWorkspaceRoleAssignments(WorkspaceId);

    AppLogger.LogStep("Viewing workspace role assignments");
    foreach (var roleAssignment in roleAssignments) {
      AppLogger.LogSubstep($"{roleAssignment.Principal.DisplayName} ({roleAssignment.Principal.Type}) added in role of {roleAssignment.Role}");
    }

  }

  public static void DeleteWorkspaceRoleAssignments(Guid WorkspaceId, Guid RoleAssignmentId) {
    fabricApiClient.Core.Workspaces.DeleteWorkspaceRoleAssignment(WorkspaceId, RoleAssignmentId);
  }

  public static List<Connection> GetConnections() {    
    return fabricApiClient.Core.Connections.ListConnections().ToList();
  }

  public static List<Connection> GetWorkspaceConnections(Guid WorkspaceId) {

    var allConnections = GetConnections();
    var workspaceConnections = new List<Connection>();

    foreach (var connection in allConnections) {
      if (connection.DisplayName.Contains(WorkspaceId.ToString())) {
        workspaceConnections.Add(connection);
      }
    }

    return workspaceConnections;
  }


  public static Connection GetConnection(Guid ConnectionId) {
    return fabricApiClient.Core.Connections.GetConnection(ConnectionId);
  }

  public static void DisplayConnnections() {
    var connections = GetConnections();

    foreach (var connection in connections) {
      Console.WriteLine($"Connection: {connection.Id}");
      Console.WriteLine($" - Display Name: {connection.DisplayName}");
      Console.WriteLine($" - Connectivity Type: {connection.ConnectivityType}");
      Console.WriteLine($" - Connection type: {connection.ConnectionDetails.Type}");
      Console.WriteLine($" - Connection path: {connection.ConnectionDetails.Path}");
      Console.WriteLine();
    }
  }

  public static void DeleteConnection(Guid ConnectionId) {
    fabricApiClient.Core.Connections.DeleteConnection(ConnectionId);
  }

  public static void DeleteConnectionIfItExists(string ConnectionName) {

    var connections = GetConnections();

    foreach (var connection in connections) {
      if (connection.DisplayName == ConnectionName) {
        DeleteConnection(connection.Id);
      }
    }

  }

  public static Connection GetConnectionByName(string ConnectionName) {

    var connections = GetConnections();

    foreach (var connection in connections) {
      if (connection.DisplayName == ConnectionName) {
        return connection;
      }
    }

    return null;

  }

  public static Connection CreateConnection(CreateConnectionRequest CreateConnectionRequest) {

    var existingConnection = GetConnectionByName(CreateConnectionRequest.DisplayName);
    if (existingConnection != null) {
      return existingConnection;
    }
    else {
      var connection = fabricApiClient.Core.Connections.CreateConnection(CreateConnectionRequest).Value;

      if ((AppSettings.AuthenticationMode == AppAuthenticationMode.ServicePrincipalAuth) &&
          (AppSettings.AdminUserId != "00000000-0000-0000-0000-000000000000")) {
        Guid AdminUserId = new Guid(AppSettings.AdminUserId);
        FabricRestApi.AddConnectionRoleAssignmentForUser(connection.Id, AdminUserId, ConnectionRole.Owner);
      }
      else {
        if (AppSettings.ServicePrincipalObjectId != "00000000-0000-0000-0000-000000000000") {
          Guid ServicePrincipalObjectId = new Guid(AppSettings.ServicePrincipalObjectId);
          FabricRestApi.AddConnectionRoleAssignmentForServicePrincipal(connection.Id, ServicePrincipalObjectId, ConnectionRole.Owner);
        }
      }
      return connection;
    }

  }

  public static void AddConnectionRoleAssignmentForUser(Guid ConnectionId, Guid UserId, ConnectionRole Role) {
    var principal = new Principal(UserId, PrincipalType.User);
    var request = new AddConnectionRoleAssignmentRequest(principal, Role);
    fabricApiClient.Core.Connections.AddConnectionRoleAssignment(ConnectionId, request);
  }

  public static void AddConnectionRoleAssignmentForServicePrincipal(Guid ConnectionId, Guid ServicePrincipalId, ConnectionRole Role) {
    var principal = new Principal(ServicePrincipalId, PrincipalType.ServicePrincipal);
    var request = new AddConnectionRoleAssignmentRequest(principal, Role);
    fabricApiClient.Core.Connections.AddConnectionRoleAssignment(ConnectionId, request);
  }

  public static void GetSupportedConnectionTypes() {

    var connTypes = fabricApiClient.Core.Connections.ListSupportedConnectionTypes();

    foreach (var connType in connTypes) {
      Console.WriteLine(connType.Type);
    }

  }

  public static Connection CreateAnonymousWebConnection(string Url, Workspace TargetWorkspace = null) {

    string displayName = string.Empty;

    if (TargetWorkspace != null) {
      displayName += $"Workspace[{TargetWorkspace.Id.ToString()}]-";
    }

    displayName += $"Web-Anonymous-[{Url}]";

    string connectionType = "Web";
    string creationMethod = "Web";

    var creationMethodParams = new List<ConnectionDetailsParameter> {
      new ConnectionDetailsTextParameter("url", Url)
    };

    var createConnectionDetails = new CreateConnectionDetails(connectionType, creationMethod, creationMethodParams);

    Credentials credentials = new AnonymousCredentials();

    var createCredentialDetails = new CreateCredentialDetails(credentials) {
      SingleSignOnType = SingleSignOnType.None,
      ConnectionEncryption = ConnectionEncryption.NotEncrypted,
      SkipTestConnection = false
    };

    var createConnectionRequest = new CreateCloudConnectionRequest(displayName,
                                                                   createConnectionDetails,
                                                                   PrivacyLevel.Organizational,
                                                                   createCredentialDetails);

    var connection = CreateConnection(createConnectionRequest);

    return connection;
  }

  public static Connection CreateSqlConnectionWithServicePrincipal(string Server, string Database, Workspace TargetWorkspace = null, Item TargetLakehouse = null) {

    string displayName = string.Empty;

    if (TargetWorkspace != null) {
      displayName += $"Workspace[{TargetWorkspace.Id.ToString()}]";
      if (TargetLakehouse != null) {
        displayName += $"-Lakehouse[{TargetLakehouse.DisplayName}]";
      }
      else {
        displayName += $"-SQL-SPN-{Server}:{Database}";
      }
    }
    else {
      displayName += $"SQL-SPN-{Server}:{Database}";
    }

    string connectionType = "SQL";
    string creationMethod = "Sql";

    var creationMethodParams = new List<ConnectionDetailsParameter> {
      new ConnectionDetailsTextParameter("server", Server),
      new ConnectionDetailsTextParameter("database", Database)
    };

    var createConnectionDetails = new CreateConnectionDetails(connectionType, creationMethod, creationMethodParams);

    Credentials credentials = new ServicePrincipalCredentials(new Guid(AppSettings.ServicePrincipalAuthTenantId),
                                                              new Guid(AppSettings.ServicePrincipalAuthClientId),
                                                              AppSettings.ServicePrincipalAuthClientSecret);

    var createCredentialDetails = new CreateCredentialDetails(credentials) {
      SingleSignOnType = SingleSignOnType.None,
      ConnectionEncryption = ConnectionEncryption.NotEncrypted,
      SkipTestConnection = false
    };

    var createConnectionRequest = new CreateCloudConnectionRequest(displayName,
                                                                   createConnectionDetails,
                                                                   PrivacyLevel.Organizational,
                                                                   createCredentialDetails);

    var connection = CreateConnection(createConnectionRequest);

    return connection;

  }

  public static Connection CreateSqlConnectionWithWorkspaceIdentity(string Server, string Database, Workspace TargetWorkspace = null, Item TargetLakehouse = null) {

    string displayName = $"SQL-WorkspaceIdentity-{Server}:{Database}-{DateTime.Now.ToString()}";

    string connectionType = "SQL";
    string creationMethod = "Sql";

    var creationMethodParams = new List<ConnectionDetailsParameter> {
      new ConnectionDetailsTextParameter("server", Server),
      new ConnectionDetailsTextParameter("database", Database)
    };

    var createConnectionDetails = new CreateConnectionDetails(connectionType, creationMethod, creationMethodParams);

    Credentials credentials = new WorkspaceIdentityCredentials();

    var createCredentialDetails = new CreateCredentialDetails(credentials) {
      SingleSignOnType = SingleSignOnType.None,
      ConnectionEncryption = ConnectionEncryption.NotEncrypted,
      SkipTestConnection = false
    };

    var createConnectionRequest = new CreateCloudConnectionRequest(displayName,
                                                                   createConnectionDetails,
                                                                   PrivacyLevel.Organizational,
                                                                   createCredentialDetails);

    var connection = CreateConnection(createConnectionRequest);

    return connection;

  }

  public static Item CreateItem(Guid WorkspaceId, CreateItemRequest CreateRequest) {
    var newItem = fabricApiClient.Core.Items.CreateItemAsync(WorkspaceId, CreateRequest).Result.Value;
    return newItem;
  }

  public static List<Item> GetItems(Guid WorkspaceId, string ItemType = null) {
    return fabricApiClient.Core.Items.ListItems(WorkspaceId, ItemType).ToList();
  }

  public static void DeleteItem(Guid WorkspaceId, Item item) {
    var newItem = fabricApiClient.Core.Items.DeleteItem(WorkspaceId, item.Id.Value);
  }

  public static void DisplayWorkspaceItems(Guid WorkspaceId) {

    List<Item> items = fabricApiClient.Core.Items.ListItems(WorkspaceId).ToList();

    foreach (var item in items) {
      Console.WriteLine($"{item.DisplayName} is a {item.Type} with an id of {item.Id}");
    }

  }

  public static Item UpdateItem(Guid WorkspaceId, Guid ItemId, string ItemName, string Description = null) {

    var updateRequest = new UpdateItemRequest {
      DisplayName = ItemName,
      Description = Description
    };

    var item = fabricApiClient.Core.Items.UpdateItem(WorkspaceId, ItemId, updateRequest).Value;

    return item;

  }

  public static List<Item> GetWorkspaceItems(Guid WorkspaceId, string ItemType = null) {
    return fabricApiClient.Core.Items.ListItems(WorkspaceId, ItemType).ToList();
  }

  public static ItemDefinition GetItemDefinition(Guid WorkspaceId, Guid ItemId, string Format = null) {
    var response = fabricApiClient.Core.Items.GetItemDefinitionAsync(WorkspaceId, ItemId, Format).Result.Value;
    return response.Definition;
  }

  public static void UpdateItemDefinition(Guid WorkspaceId, Guid ItemId, UpdateItemDefinitionRequest UpdateRequest) {
    fabricApiClient.Core.Items.UpdateItemDefinition(WorkspaceId, ItemId, UpdateRequest);
  }

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

  public static SemanticModel GetSemanticModelByName(Guid WorkspaceId, string Name) {
    var models = fabricApiClient.SemanticModel.Items.ListSemanticModels(WorkspaceId);
    foreach (var model in models) {
      if (Name == model.DisplayName) {
        return model;
      }
    }
    return null;
  }

  public static Report GetReportByName(Guid WorkspaceId, string Name) {
    var reports = fabricApiClient.Report.Items.ListReports(WorkspaceId);
    foreach (var report in reports) {
      if (Name == report.DisplayName) {
        return report;
      }
    }
    return null;
  }

  public static Item CreateLakehouse(Guid WorkspaceId, string LakehouseName, bool EnableSchemas = false) {

    // Item create request for lakehouse des not include item definition
    var createRequest = new CreateItemRequest(LakehouseName, ItemType.Lakehouse);

    if (EnableSchemas) {
      createRequest.CreationPayload = new List<KeyValuePair<string, object>>() {
          new KeyValuePair<string, object>("enableSchemas", true)
      };
    }

    // create lakehouse
    return CreateItem(WorkspaceId, createRequest);
  }

  public static Shortcut CreateLakehouseShortcut(Guid WorkspaceId, Guid LakehouseId, CreateShortcutRequest CreateShortcutRequest) {
    return fabricApiClient.Core.OneLakeShortcuts.CreateShortcut(WorkspaceId, LakehouseId, CreateShortcutRequest).Value;
  }

  public static List<Shortcut> GetLakehouseShortcuts(Guid WorkspaceId, Guid LakehouseId) {
    return fabricApiClient.Core.OneLakeShortcuts.ListShortcuts(WorkspaceId, LakehouseId).ToList();
  }


  public static Lakehouse GetLakehouse(Guid WorkspaceId, Guid LakehousId) {
    return fabricApiClient.Lakehouse.Items.GetLakehouse(WorkspaceId, LakehousId).Value;
  }

  public static Lakehouse GetLakehouseByName(Guid WorkspaceId, string LakehouseName) {

    var lakehouses = fabricApiClient.Lakehouse.Items.ListLakehouses(WorkspaceId);

    foreach (var lakehouse in lakehouses) {
      if (lakehouse.DisplayName == LakehouseName) {
        return lakehouse;
      }
    }

    return null;
  }

  public static Notebook GetNotebookByName(Guid WorkspaceId, string NotebookName) {

    var notebooks = fabricApiClient.Notebook.Items.ListNotebooks(WorkspaceId);

    foreach (var notebook in notebooks) {
      if (notebook.DisplayName == NotebookName) {
        return notebook;
      }
    }

    return null;
  }

  public static SqlEndpointProperties GetSqlEndpointForLakehouse(Guid WorkspaceId, Guid LakehouseId) {

    var lakehouse = GetLakehouse(WorkspaceId, LakehouseId);

    while ((lakehouse.Properties.SqlEndpointProperties == null) ||
           (lakehouse.Properties.SqlEndpointProperties.ProvisioningStatus != "Success")) {
      lakehouse = GetLakehouse(WorkspaceId, LakehouseId);
      Thread.Sleep(10000); // wait 10 seconds
    }

    return lakehouse.Properties.SqlEndpointProperties;

  }

  public static Item CreateWarehouse(Guid WorkspaceId, string WarehouseName) {

    // Item create request for lakehouse des not include item definition
    var createRequest = new CreateItemRequest(WarehouseName, ItemType.Warehouse);

    // create lakehouse
    return CreateItem(WorkspaceId, createRequest);
  }

  public static Warehouse GetWareHouseByName(Guid WorkspaceId, string WarehouseName) {

    var warehouses = fabricApiClient.Warehouse.Items.ListWarehouses(WorkspaceId);

    foreach (var warehouse in warehouses) {
      if (warehouse.DisplayName == WarehouseName) {
        return warehouse;
      }
    }

    return null;
  }

  public static Warehouse GetWarehouse(Guid WorkspaceId, Guid WarehouseId) {
    return fabricApiClient.Warehouse.Items.GetWarehouse(WorkspaceId, WarehouseId).Value;
  }

  public static string GetSqlConnectionStringForWarehouse(Guid WorkspaceId, Guid WarehouseId) {
    var warehouse = GetWarehouse(WorkspaceId, WarehouseId);
    return warehouse.Properties.ConnectionString;
  }

  public static void LoadLakehouseTableFromParquet(Guid WorkspaceId, Guid LakehouseId, string SourceFile, string TableName) {

    var loadTableRequest = new LoadTableRequest(SourceFile, PathType.File);
    loadTableRequest.Recursive = false;
    loadTableRequest.Mode = ModeType.Overwrite;
    loadTableRequest.FormatOptions = new Parquet();

    fabricApiClient.Lakehouse.Tables.LoadTableAsync(WorkspaceId, LakehouseId, TableName, loadTableRequest).Wait();

  }

  public static void LoadLakehouseTableFromCsv(Guid WorkspaceId, Guid LakehouseId, string SourceFile, string TableName) {

    var loadTableRequest = new LoadTableRequest(SourceFile, PathType.File);
    loadTableRequest.Recursive = false;
    loadTableRequest.Mode = ModeType.Overwrite;
    loadTableRequest.FormatOptions = new Csv();

    fabricApiClient.Lakehouse.Tables.LoadTableAsync(WorkspaceId, LakehouseId, TableName, loadTableRequest).Wait();
  }

  public static void RunNotebook(Guid WorkspaceId, Item Notebook, RunOnDemandItemJobRequest JobRequest = null) {

    AppLogger.LogOperationInProgress();

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, Notebook.Id.Value, "RunNotebook", JobRequest);

    if (response.Status == 202) {

      string location = response.GetLocationHeader();
      int? retryAfter = 6; // response.GetRetryAfterHeader();
      Guid JobInstanceId = response.GetTriggeredJobId();

      Thread.Sleep(retryAfter.Value * 1000);

      var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;

      while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
        AppLogger.LogOperationInProgress();
        Thread.Sleep(retryAfter.Value * 1000);
        jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, Notebook.Id.Value, JobInstanceId).Value;
      }

      if (jobInstance.Status == Status.Completed) {
        return;
      }

      if (jobInstance.Status == Status.Failed) {
        AppLogger.LogSubstep("Notebook execution failed");
        AppLogger.LogSubstep(jobInstance.FailureReason.Message);
      }

      if (jobInstance.Status == Status.Cancelled) {
        AppLogger.LogSubstep("Notebook execution cancelled");
      }

      if (jobInstance.Status == Status.Deduped) {
        AppLogger.LogSubstep("Notebook execution deduped");
      }
    }
    else {
      AppLogger.LogStep("Notebook execution failed when starting");
    }

  }

  public static void RunDataPipeline(Guid WorkspaceId, Item DataPipeline) {

    var response = fabricApiClient.Core.JobScheduler.RunOnDemandItemJob(WorkspaceId, DataPipeline.Id.Value, "Pipeline");

    if (response.Status == 202) {

      string location = response.GetLocationHeader();
      int? retryAfter = 10; // response.GetRetryAfterHeader();
      Guid JobInstanceId = response.GetTriggeredJobId();

      Thread.Sleep(retryAfter.Value * 1000);

      var jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, DataPipeline.Id.Value, JobInstanceId).Value;

      while (jobInstance.Status == Status.NotStarted || jobInstance.Status == Status.InProgress) {
        Thread.Sleep(retryAfter.Value * 1000);
        jobInstance = fabricApiClient.Core.JobScheduler.GetItemJobInstance(WorkspaceId, DataPipeline.Id.Value, JobInstanceId).Value;
      }

      if (jobInstance.Status == Status.Completed) {
        return;
      }

      if (jobInstance.Status == Status.Failed) {
        AppLogger.LogSubstep("Data pipeline execution failed");
        AppLogger.LogSubstep(jobInstance.FailureReason.Message);
      }

      if (jobInstance.Status == Status.Cancelled) {
        AppLogger.LogSubstep("Data pipeline execution cancelled");
      }

      if (jobInstance.Status == Status.Deduped) {
        AppLogger.LogSubstep("Data pipeline execution deduped");
      }
    }
    else {
      AppLogger.LogStep("Data pipeline execution failed when starting");
    }
  }

  public static void CreateShortcut(Guid WorkspaceId, Guid LakehouseId, CreateShortcutRequest CreateShortcutRequest) {
    var response = fabricApiClient.Core.OneLakeShortcuts.CreateShortcut(WorkspaceId, LakehouseId, CreateShortcutRequest).Value;
  }

  public static void CreateAdlsGen2Shortcut(Guid WorkspaceId, Guid LakehouseId, Uri Location, string Path, string Name, Guid ConnectionId) {

    var target = new CreatableShortcutTarget {
      AdlsGen2 = new AdlsGen2(Location, Name, ConnectionId)
    };

    var createRequest = new CreateShortcutRequest(Path, Name, target);
    var response = fabricApiClient.Core.OneLakeShortcuts.CreateShortcut(WorkspaceId, LakehouseId, createRequest).Value;

  }

  public static void ExportItemDefinitionsFromWorkspace(string WorkspaceName) {

    AppLogger.LogStep($"Exporting workspace item definitions from workspace [{WorkspaceName}]");

    DeleteExportsFolderContents(WorkspaceName);

    var workspace = GetWorkspaceByName(WorkspaceName);
    var items = GetWorkspaceItems(workspace.Id);

    // list of items types that should be exported
    List<ItemType> itemTypesForExport = new List<ItemType>() {
      ItemType.Notebook, ItemType.SemanticModel, ItemType.Report
    };

    foreach (var item in items) {
      if (itemTypesForExport.Contains(item.Type)) {

        // fetch item definition from workspace
        var definition = GetItemDefinition(workspace.Id, item.Id.Value);

        // write item definition files to local folder
        string targetFolder = item.DisplayName + "." + item.Type;

        AppLogger.LogSubstep($"Exporting item definition for [{targetFolder}]");

        foreach (var part in definition.Parts) {
          WriteFileToExportsFolder(WorkspaceName, targetFolder, part.Path, part.Payload);
        }

      }

      // slow up calls so this function doesn't trigger throttling
      Thread.Sleep(7000);
    }

    AppLogger.LogStep("Workspace item definition export process completed");

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


  public static Connection CreateAzureStorageConnectionWithServicePrincipal2(string Server, string Path, bool ReuseExistingConnection = false) {

    string displayName = $"ADLS-SPN-{Server}-{Path}";

    string connectionType = "AzureDataLakeStorage";
    string creationMethod = "AzureDataLakeStorage";

    var creationMethodParams = new List<ConnectionDetailsParameter> {
      new ConnectionDetailsTextParameter("server", Server),
      new ConnectionDetailsTextParameter("path", Path)
    };

    var createConnectionDetails = new CreateConnectionDetails(connectionType, creationMethod, creationMethodParams);

    Credentials creds = new ServicePrincipalCredentials(new Guid(AppSettings.ServicePrincipalAuthTenantId),
                                                        new Guid(AppSettings.ServicePrincipalAuthClientId),
                                                        AppSettings.ServicePrincipalAuthClientSecret);

    var createCredentialDetails = new CreateCredentialDetails(creds) {
      SingleSignOnType = SingleSignOnType.None,
      ConnectionEncryption = ConnectionEncryption.NotEncrypted,
      SkipTestConnection = false
    };

    var createConnectionRequest = new CreateCloudConnectionRequest(displayName,
                                                                   createConnectionDetails,
                                                                   PrivacyLevel.Organizational,
                                                                   createCredentialDetails);

    return CreateConnection(createConnectionRequest);
  }

  public static Connection CreateAzureStorageConnectionWithWorkspaceIdentity(string Server, string Path, bool ReuseExistingConnection = false) {

    string displayName = $"ADLS-AccountKey-{Server}-{Path}";

    string connectionType = "AzureDataLakeStorage";
    string creationMethod = "AzureDataLakeStorage";

    var creationMethodParams = new List<ConnectionDetailsParameter> {
      new ConnectionDetailsTextParameter("server", Server),
      new ConnectionDetailsTextParameter("path", Path)
    };

    var createConnectionDetails = new CreateConnectionDetails(connectionType, creationMethod, creationMethodParams);

    Credentials creds = new WorkspaceIdentityCredentials();

    var createCredentialDetails = new CreateCredentialDetails(creds) {
      SingleSignOnType = SingleSignOnType.None,
      ConnectionEncryption = ConnectionEncryption.NotEncrypted,
      SkipTestConnection = false
    };

    var createConnectionRequest = new CreateCloudConnectionRequest(displayName,
                                                                   createConnectionDetails,
                                                                   PrivacyLevel.Organizational,
                                                                   createCredentialDetails);

    return CreateConnection(createConnectionRequest);

  }

  public static void ConnectWorkspaceToGitRepository(Guid WorkspaceId, GitConnectRequest connectionRequest) {

    AppLogger.LogStep("Connecting workspace to Azure Dev Ops");

    var connectResponse = fabricApiClient.Core.Git.Connect(WorkspaceId, connectionRequest);

    AppLogger.LogSubstep("GIT connection established between workspace and Azure Dev Ops");

    // (2) initialize connection
    var initRequest = new InitializeGitConnectionRequest {
      InitializationStrategy = InitializationStrategy.PreferWorkspace
    };

    var initResponse = fabricApiClient.Core.Git.InitializeConnection(WorkspaceId, initRequest).Value;


    if (initResponse.RequiredAction == RequiredAction.CommitToGit) {
      // (2A) commit workspace changes to GIT
      AppLogger.LogSubstep("Committing changes to GIT repository");

      var commitToGitRequest = new CommitToGitRequest(CommitMode.All) {
        WorkspaceHead = initResponse.WorkspaceHead,
        Comment = "Initial commit to GIT"
      };

      fabricApiClient.Core.Git.CommitToGit(WorkspaceId, commitToGitRequest);

      AppLogger.LogSubstep("Workspace changes committed to GIT");
    }

    if (initResponse.RequiredAction == RequiredAction.UpdateFromGit) {
      // (2B) update workspace from source files in GIT
      AppLogger.LogSubstep("Updating workspace from source files in GIT");

      var updateFromGitRequest = new UpdateFromGitRequest(initResponse.RemoteCommitHash) {
        ConflictResolution = new WorkspaceConflictResolution(
          ConflictResolutionType.Workspace,
          ConflictResolutionPolicy.PreferWorkspace)
      };

      fabricApiClient.Core.Git.UpdateFromGit(WorkspaceId, updateFromGitRequest);
      AppLogger.LogSubstep("Workspace updated from source files in GIT");
    }

    AppLogger.LogSubstep("Workspace connection intialization complete");

  }

  public static void DisconnectWorkspaceFromGitRepository(Guid WorkspaceId) {
    fabricApiClient.Core.Git.Disconnect(WorkspaceId);
  }

  public static GitConnection GetWorkspaceGitConnection(Guid WorkspaceId) {
    return fabricApiClient.Core.Git.GetConnection(WorkspaceId);
  }

  public static GitStatusResponse GetWorkspaceGitStatus(Guid WorkspaceId) {
    return fabricApiClient.Core.Git.GetStatus(WorkspaceId).Value;
  }

  public static void CommitWoGrkspaceToGit(Guid WorkspaceId) {
    AppLogger.LogStep("Committing workspace changes to GIT");

    var gitStatus = GetWorkspaceGitStatus(WorkspaceId);

    var commitRequest = new CommitToGitRequest(CommitMode.All);
    commitRequest.Comment = "Workspaces changes after semantic model refresh";
    commitRequest.WorkspaceHead = gitStatus.WorkspaceHead;

    fabricApiClient.Core.Git.CommitToGit(WorkspaceId, commitRequest);

  }

  public static void UpdateWorkspaceFromGit(Guid WorkspaceId) {

    AppLogger.LogStep("Syncing updates to workspace from GIT");

    var gitStatus = GetWorkspaceGitStatus(WorkspaceId);

    var updateFromGitRequest = new UpdateFromGitRequest(gitStatus.RemoteCommitHash) {
      WorkspaceHead = gitStatus.WorkspaceHead,
      Options = new UpdateOptions { AllowOverrideItems = true },
      ConflictResolution = new WorkspaceConflictResolution(ConflictResolutionType.Workspace,
                                                           ConflictResolutionPolicy.PreferWorkspace)
    };

    fabricApiClient.Core.Git.UpdateFromGit(WorkspaceId, updateFromGitRequest);
  }

}