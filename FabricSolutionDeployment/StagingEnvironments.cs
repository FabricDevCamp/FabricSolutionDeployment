public class StagingEnvironments {

  public static DeploymentPlan Dev {
    get {
      var Deployment = new DeploymentPlan(DeploymentPlanType.StagedDeployment, "Dev");

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter, 
                                        DeploymentPlan.webDatasourceRootDefault + "Dev/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Dev/");

      return Deployment;
    }
  }

  public static DeploymentPlan Test {
    get {
      var Deployment = new DeploymentPlan(DeploymentPlanType.StagedDeployment, "Test");

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Test/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Test");

      return Deployment;
    }
  }

  public static DeploymentPlan Prod {
    get {
      var Deployment = new DeploymentPlan(DeploymentPlanType.StagedDeployment, "Prod");

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Prod/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Prod");

      return Deployment;
    }
  }

}
