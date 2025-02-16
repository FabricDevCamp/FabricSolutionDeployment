using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class SampleCustomerData {

  public static DeploymentPlan Contoso {
    get {

      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "Contoso";

      //  datasourcePath is "https://fabricdevcamp.blob.core.windows.net/sampledata/ProductSales/Customers/Contoso/";

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

  public static DeploymentPlan AutoBoss {
    get {

      var Deployment = new DeploymentPlan();
      Deployment.CustomerName = "AutoBoss";

      // setup Web datasource path
      string deploymentWebDatasourcePath = DeploymentPlan.webDatasourceRootDefault + "Customers/AutoBoss/";

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
                   DeploymentPlan.adlsContainerNameDefault,
                   DeploymentPlan.adlsContainerNameDefault,
                    "/Customers/AutoBoss/");


      return Deployment;
    }
  }


}
