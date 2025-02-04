using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;
using System.Net.Http.Headers;
using System;

public class PowerBiRestApi {

  private static PowerBIClient pbiClient;
  private static string accessToken;

  static PowerBiRestApi() {
    accessToken = EntraIdTokenManager.GetFabricAccessToken();
    string urlPowerBiServiceApiRoot = AppSettings.PowerBiRestApiBaseUrl;
    var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
    pbiClient = new PowerBIClient(new Uri(urlPowerBiServiceApiRoot), tokenCredentials);
  }

  public static void RefreshDataset(Guid WorkspaceId, Guid DatasetId) {

    var refreshRequest = new DatasetRefreshRequest {
      NotifyOption = NotifyOption.NoNotification,
      Type = DatasetRefreshType.Automatic
    };

    var responseStartFresh = pbiClient.Datasets.RefreshDatasetInGroup(WorkspaceId, DatasetId.ToString(), refreshRequest);

    var responseStatusCheck = pbiClient.Datasets.GetRefreshExecutionDetailsInGroup(WorkspaceId, DatasetId, new Guid(responseStartFresh.XMsRequestId));

    while (responseStatusCheck.Status == "Unknown") {
      Thread.Sleep(10000);
      responseStatusCheck = pbiClient.Datasets.GetRefreshExecutionDetailsInGroup(WorkspaceId, DatasetId, new Guid(responseStartFresh.XMsRequestId));
    }

    if (responseStatusCheck.Status == "Failed") {
      //AppLogger.LogSubstep("Refresh failed. Trying again");
      Thread.Sleep(15000);
      responseStartFresh = pbiClient.Datasets.RefreshDatasetInGroup(WorkspaceId, DatasetId.ToString(), refreshRequest);

      responseStatusCheck = pbiClient.Datasets.GetRefreshExecutionDetailsInGroup(WorkspaceId, DatasetId, new Guid(responseStartFresh.XMsRequestId));

      while (responseStatusCheck.Status == "Unknown") {
        Thread.Sleep(10000);
        responseStatusCheck = pbiClient.Datasets.GetRefreshExecutionDetailsInGroup(WorkspaceId, DatasetId, new Guid(responseStartFresh.XMsRequestId));
      }

    }

  }

  public static IList<Datasource> GetDatasourcesForDataset(string WorkspaceId, string DatasetId) {
    return pbiClient.Datasets.GetDatasourcesInGroup(new Guid(WorkspaceId), DatasetId).Value;
  }

  public static IList<Report> GetReportsInWorkspace(Guid WorkspaceId) {
    return pbiClient.Reports.GetReportsInGroup(WorkspaceId).Value;
  }

  public static IList<Dataset> GetDatasetsInWorkspace(Guid WorkspaceId) {
    return pbiClient.Datasets.GetDatasetsInGroup(WorkspaceId).Value;
  }

  public static void ViewDatasources(Guid WorkspaceId, Guid DatasetId) {

    // get datasources for dataset
    var datasources = pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value;

    foreach (var datasource in datasources) {

      Console.WriteLine(" - Connection Name: " + datasource.Name);
      Console.WriteLine("   > DatasourceType: " + datasource.DatasourceType);
      Console.WriteLine("   > DatasourceId: " + datasource.DatasourceId);
      Console.WriteLine("   > GatewayId: " + datasource.GatewayId);
      Console.WriteLine("   > Path: " + datasource.ConnectionDetails.Path);
      Console.WriteLine("   > Server: " + datasource.ConnectionDetails.Server);
      Console.WriteLine("   > Database: " + datasource.ConnectionDetails.Database);
      Console.WriteLine("   > Url: " + datasource.ConnectionDetails.Url);
      Console.WriteLine("   > Domain: " + datasource.ConnectionDetails.Domain);
      Console.WriteLine("   > EmailAddress: " + datasource.ConnectionDetails.EmailAddress);
      Console.WriteLine("   > Kind: " + datasource.ConnectionDetails.Kind);
      Console.WriteLine("   > LoginServer: " + datasource.ConnectionDetails.LoginServer);
      Console.WriteLine("   > ClassInfo: " + datasource.ConnectionDetails.ClassInfo);
      Console.WriteLine();

    }
  }

  public static IList<Datasource> GetDatasourcesForSemanricModels(Guid WorkspaceId, Guid DatasetId) {
    return pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value;
  }

  public static string GetWebDatasourceUrl(Guid WorkspaceId, Guid DatasetId) {
 
    var datasource = pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value.First();
    if (datasource.DatasourceType.Equals("Web")) {
      return datasource.ConnectionDetails.Url;
    }
    else {
      throw new ApplicationException("Error - expecting Web connection");
    }
  }

  public static void BindReportToSemanticModel(Guid WorkspaceId, Guid SemanticModelId, Guid ReportId) {
    RebindReportRequest bindRequest = new RebindReportRequest(SemanticModelId.ToString());
    pbiClient.Reports.RebindReportInGroup(WorkspaceId, ReportId, bindRequest);
  }


  public static void BindSemanticModelToConnection(Guid WorkspaceId, Guid SemanticModelId, Guid ConnectionId) {

    BindToGatewayRequest bindRequest = new BindToGatewayRequest {
      DatasourceObjectIds = new List<Guid?>()
    };

    bindRequest.DatasourceObjectIds.Add(ConnectionId);

    pbiClient.Datasets.BindToGatewayInGroup(WorkspaceId, SemanticModelId.ToString(), bindRequest);

  }

  // workaround methods used until Connections API is released
  public static void PatchAnonymousAccessWebCredentials(Guid WorkspaceId, Guid DatasetId) {

    // get datasources for dataset
    var datasources = pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId.ToString()).Value;

    foreach (var datasource in datasources) {

      // check to ensure datasource use Web connector
      if (datasource.DatasourceType.ToLower() == "web") {

        // get DatasourceId and GatewayId
        var datasourceId = datasource.DatasourceId;
        var gatewayId = datasource.GatewayId;

        // Initialize UpdateDatasourceRequest object with AnonymousCredentials
        UpdateDatasourceRequest req = new UpdateDatasourceRequest {
          CredentialDetails = new CredentialDetails(
            new Microsoft.PowerBI.Api.Models.Credentials.AnonymousCredentials(),
            PrivacyLevel.Organizational,
            EncryptedConnection.NotEncrypted)
        };

        // Update datasource credentials through Gateways - UpdateDatasource
        pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);

      }
    }
  }

}

