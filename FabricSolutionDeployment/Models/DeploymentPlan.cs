
public enum DeploymentPlanType {
  StagedDeployment,
  CustomerTenantDeployment
}

public class DeploymentPlan {

  public string Name { get; set; }
  public string Description { get; set; }
  public DeploymentPlanType DeploymentType { get; set; }

  public Dictionary<string, string> Parameters { get; set; }
  
  public void AddDeploymentParameter(string ParameterName, string DeploymentValue) {
    Parameters.Add(ParameterName, DeploymentValue);
  }

  public string TargetWorkspaceName {
    get { return $"Tenant - {Name}"; }
  }

  public const string webDatasourcePathParameter = "webDatasourcePath";
  public const string adlsServerPathParameter = "adlsServer";
  public const string adlsContainerNameParameter = "adlsContainerName";
  public const string adlsContainerPathParameter = "adlsContainerPath";
  public const string adlsAccountKey = "adlsAccountKey ";

  // default values
  public const string webDatasourceRootDefault = "https://fabricdevcamp.blob.core.windows.net/sampledata/ProductSales/";

  public const string adlsServerPathDefault = AppSettings.AzureStorageServer;
  public const string adlsContainerNameDefault = AppSettings.AzureStorageContainer;
  public const string adlsContainerPathDefault = AppSettings.AzureStorageContainerPath;

  public DeploymentPlan(DeploymentPlanType DeploymentType) {
    this.DeploymentType = DeploymentType;
    Parameters = new Dictionary<string, string>();
  }

  public DeploymentPlan(string DeploymentName, DeploymentPlanType DeploymentType) {
    this.DeploymentType = DeploymentType;
    this.Name = DeploymentName;
    Parameters = new Dictionary<string, string>();
  }
 
}
