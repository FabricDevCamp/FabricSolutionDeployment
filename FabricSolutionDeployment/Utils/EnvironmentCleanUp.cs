using Microsoft.Fabric.Api.Core.Models;

public class EnvironmentCleanUp {

  public static void CleanupTestEnvironment_Danger() {
    AppLogger.LogSolution("Cleaning up your environment by deleting all workspaces and connectios");
    DeleteAllWorkspaces();
    DeleteAllConnections();
    DeleteAllAzureDevOpsProjects();
    AppLogger.LogStep("Environment cleanup complete");
  }

  public static void DeleteAllWorkspaces() {

    var workspaces = FabricRestApi.GetWorkspaces();

    if (workspaces.Count > 0) {
      AppLogger.LogStep("Deleting all workspaces");
      foreach (var workspace in workspaces) {
        if (workspace.Type == WorkspaceType.Workspace) {
          AppLogger.LogSubstep($"Deleting {workspace.DisplayName}");
          FabricRestApi.DeleteWorkspace(workspace.Id);
        }
      }
    }

  }

  public static void DeleteAllConnections() {

    var connections = FabricRestApi.GetConnections();

    if(connections.Count > 0) {
      AppLogger.LogOperationStart("Deleting all connections");
      foreach (var connection in FabricRestApi.GetConnections()) {
        AppLogger.LogOperationInProgress();
        FabricRestApi.DeleteConnection(connection.Id);
      }
      AppLogger.LogOperationComplete();
    }

  }

  public static void DeleteAllPersonalCloudConnections() {

    foreach (var connection in FabricRestApi.GetConnections()) {
      if (connection.ConnectivityType == ConnectivityType.PersonalCloud) {
        FabricRestApi.DeleteConnection(connection.Id);
      }
    }

  }

  public static void DeleteAllSqlEndpointConnections() {
    AppLogger.LogOperationStart("Deleting all SQL endpoint connections");
    foreach (var connection in FabricRestApi.GetConnections()) {
      var conn = FabricRestApi.GetConnection(connection.Id);
      if ((conn.ConnectionDetails.Type == "SQL") &&
          conn.ConnectionDetails.Path.Contains("datawarehouse.fabric.microsoft.com")) {
        AppLogger.LogOperationInProgress();
        FabricRestApi.DeleteConnection(connection.Id);
        Thread.Sleep(6000);
      }
    }
    AppLogger.LogOperationComplete();
  }

  public static void DeleteAllAzureDevOpsProjects() {

    var projects = AdoProjectManager.GetProjects();

    if(projects.Count > 0) {
      AppLogger.LogStep("Deleting all ADO projects");
      foreach (var project in projects) {
        AppLogger.LogSubstep("deleting project " + project.Name);
        AdoProjectManager.DeleteProject(project.Id);
        AppLogger.LogSubstep("Project successfully deleted");
      }
    }

  }


}