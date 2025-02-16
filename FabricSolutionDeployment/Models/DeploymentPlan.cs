

public class DeploymentParameter {
  public string SourceValue { get; set; }
  public string DeploymentValue { get; set; }
}

public class DeploymentPlan {

  public string CustomerName { get; set; }

  public Dictionary<string, DeploymentParameter> Parameters { get; set; }

  public const string webDatasourcePathParameter = "webDatasourcePath";
  public const string adlsServerPathParameter = "adlsServer";
  public const string adlsContainerNameParameter = "adlsContainerName";
  public const string adlsContainerPathParameter = "adlsContainerPath";
  public const string adlsAccountKey = "adlsAccountKey ";

  // default values
  public const string webDatasourceRootDefault = "https://fabricdevcamp.blob.core.windows.net/sampledata/ProductSales/";
  public const string webDatasourcePathDefault = webDatasourceRootDefault + "Dev/";

  public const string adlsServerPathDefault = AppSettings.AzureStorageServer;
  public const string adlsContainerNameDefault = AppSettings.AzureStorageContainer;
  public const string adlsContainerPathDefault = AppSettings.AzureStorageContainerPath;

  public DeploymentPlan() {
    Parameters = new Dictionary<string, DeploymentParameter>();
  }

  public void AddDeploymentParameter(string ParameterName, string SourceValue, string DeploymentValue) {
    Parameters.Add(ParameterName,
                           new DeploymentParameter {
                             SourceValue = SourceValue,
                             DeploymentValue = DeploymentValue
                           });
  }

}
