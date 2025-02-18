
class SampleCustomerData {

  public static DeploymentPlan AdventureWorks {
    get {
      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Adventure Works";

      // setup Web datasource path
      string deploymentWebDatasourcePath = DeploymentPlan.webDatasourceRootDefault + "Customers/AdventureWorks/";

      Deployment.AddDeploymentParameter(
                          DeploymentPlan.webDatasourcePathParameter,
                          DeploymentPlan.webDatasourcePathDefault,
                          deploymentWebDatasourcePath);

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(
                    DeploymentPlan.adlsServerPathParameter,
                    DeploymentPlan.adlsServerPathDefault,
                    DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerNameParameter,
                   DeploymentPlan.adlsContainerNameDefault,
                   DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerPathParameter,
                   DeploymentPlan.adlsContainerPathDefault,
                    "/ProductSales/Customers/AdventureWorks/");

      return Deployment;
    }
  }

  public static DeploymentPlan Contoso {
    get {
      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Contoso";

      // setup Web datasource path
      string deploymentWebDatasourcePath = DeploymentPlan.webDatasourceRootDefault + "Customers/Contoso/";

      Deployment.AddDeploymentParameter(
                          DeploymentPlan.webDatasourcePathParameter,
                          DeploymentPlan.webDatasourcePathDefault,
                          deploymentWebDatasourcePath);

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(
                    DeploymentPlan.adlsServerPathParameter,
                    DeploymentPlan.adlsServerPathDefault,
                    DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerNameParameter,
                   DeploymentPlan.adlsContainerNameDefault,
                   DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerPathParameter,
                   DeploymentPlan.adlsContainerPathDefault,
                    "/ProductSales/Customers/Contoso/");

      return Deployment;
    }
  }

  public static DeploymentPlan Northwind {
    get {
      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Northwind";

      // setup Web datasource path
      string deploymentWebDatasourcePath = DeploymentPlan.webDatasourceRootDefault + "Customers/Northwind/";

      Deployment.AddDeploymentParameter(
                          DeploymentPlan.webDatasourcePathParameter,
                          DeploymentPlan.webDatasourcePathDefault,
                          deploymentWebDatasourcePath);

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(
                    DeploymentPlan.adlsServerPathParameter,
                    DeploymentPlan.adlsServerPathDefault,
                    DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerNameParameter,
                   DeploymentPlan.adlsContainerNameDefault,
                   DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerPathParameter,
                   DeploymentPlan.adlsContainerPathDefault,
                    "/ProductSales/Customers/Northwind/");

      return Deployment;
    }
  }

  public static DeploymentPlan Wingtip {
    get {
      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Wingtip";

      // setup Web datasource path
      string deploymentWebDatasourcePath = DeploymentPlan.webDatasourceRootDefault + "Customers/Wingtip/";

      Deployment.AddDeploymentParameter(
                          DeploymentPlan.webDatasourcePathParameter,
                          DeploymentPlan.webDatasourcePathDefault,
                          deploymentWebDatasourcePath);

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(
                    DeploymentPlan.adlsServerPathParameter,
                    DeploymentPlan.adlsServerPathDefault,
                    DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerNameParameter,
                   DeploymentPlan.adlsContainerNameDefault,
                   DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(
                   DeploymentPlan.adlsContainerPathParameter,
                   DeploymentPlan.adlsContainerPathDefault,
                    "/ProductSales/Customers/Wingtip/");

      return Deployment;
    }
  }
  
}
