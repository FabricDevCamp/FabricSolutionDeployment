public class StagingEnvironments {

  public static DeploymentPlan Dev {
    get {
      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Dev";

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourcePathDefault,
                                        DeploymentPlan.webDatasourceRootDefault + "Dev/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        DeploymentPlan.adlsContainerPathDefault,
                                        "/ProductSales/Dev/");

      return Deployment;
    }
  }

  public static DeploymentPlan Test {
    get {
      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Test";

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourcePathDefault,
                                        DeploymentPlan.webDatasourceRootDefault + "Test/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        DeploymentPlan.adlsContainerPathDefault,
                                        "/ProductSales/Test");

      return Deployment;
    }
  }

  public static DeploymentPlan Prod {
    get {
      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Prod";

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourcePathDefault,
                                        DeploymentPlan.webDatasourceRootDefault + "Prod/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        DeploymentPlan.adlsContainerPathDefault,
                                        "/ProductSales/Prod");

      return Deployment;
    }
  }

}
