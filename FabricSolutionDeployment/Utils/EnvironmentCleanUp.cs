using Microsoft.Fabric.Api.Core.Models;

public class EnvironmentCleanUp {

  public static void CleanupTestEnvironment_Danger() {
    DeleteAllConnections();
    DeleteAllWorkspaces();
    DeleteAllAzureDevOpsProjects();
  }

  public static void DeleteAllConnections() {
    AppLogger.LogOperationStart("Deleting all connections");
    foreach (var connection in NoSdk.FabricRestApiNoSdk.GetConnections()) {
      AppLogger.LogOperationInProgress();
      FabricRestApi.DeleteConnection(new Guid(connection.id));
      Thread.Sleep(6000);
    }
    AppLogger.LogOperationComplete();
  }

  public static void DeleteAllSqlEndpointConnections() {
    AppLogger.LogOperationStart("Deleting all SQL endpoint connections");
    foreach (var connection in NoSdk.FabricRestApiNoSdk.GetConnections()) {
      var conn = FabricRestApi.GetConnection(new Guid(connection.id));
      if ((conn.ConnectionDetails.Type == "SQL") &&
          conn.ConnectionDetails.Path.Contains("datawarehouse.fabric.microsoft.com")) {
        AppLogger.LogOperationInProgress();
        FabricRestApi.DeleteConnection(new Guid(connection.id));
        Thread.Sleep(6000);
      }
    }
    AppLogger.LogOperationComplete();
  }

  public static void DeleteAllWorkspaces() {

    var workspaces = FabricRestApi.GetWorkspaces();

    foreach (var workspace in workspaces) {
      if (workspace.Type == WorkspaceType.Workspace) {
        FabricRestApi.DeleteWorkspace(workspace.Id);
      }
    }
  }

  public static void DeleteAllAzureDevOpsProjects() {

    var projects = AdoProjectManager.GetProjects();

    AppLogger.LogStep("Deleting all ADO projects");

    foreach (var project in projects) {
      AppLogger.LogSubstep("deleting project " + project.Name);
      AdoProjectManager.DeleteProject(project.Id);
      AppLogger.LogSubstep("Project successfully deleted");
    }

  }


}