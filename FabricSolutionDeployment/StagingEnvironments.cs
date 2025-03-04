public class StagingEnvironments {

  public static DeploymentPlan Dev {
    get {
      var Deployment = new DeploymentPlan("Dev", DeploymentPlanType.StagedDeployment);

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
      var Deployment = new DeploymentPlan("Test", DeploymentPlanType.StagedDeployment);

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
      var Deployment = new DeploymentPlan("Prod", DeploymentPlanType.StagedDeployment);


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
