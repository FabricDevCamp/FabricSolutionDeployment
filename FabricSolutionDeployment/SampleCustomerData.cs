class SampleCustomerData {

  public static DeploymentPlan AdventureWorks {
    get {
      var Deployment = new DeploymentPlan("Adventure Works");
      Deployment.Description = "The ultimate provider for the avid bicycle rider";

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Customers/AdventureWorks/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Customers/AdventureWorks/");

      return Deployment;
    }
  }

  public static DeploymentPlan Contoso {
    get {
      var Deployment = new DeploymentPlan("Contoso");
      Deployment.Description = "Your trusted source for world-famous pharmaceuticals";

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Customers/Contoso/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Customers/Contoso/");

      return Deployment;
    }
  }
  
  public static DeploymentPlan Fabricam {
    get {
      var Deployment = new DeploymentPlan("Fabrikam");
      Deployment.Description = "The Absolute WHY and WHERE for Enterprise Hardware";


      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Customers/Fabrikam/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Customers/Fabrikam/");

      return Deployment;
    }
  }

  public static DeploymentPlan Northwind {
    get {
      var Deployment = new DeploymentPlan("Northwind Traders");
      Deployment.Description = "Microsoft's favorate fictional company";

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Customers/Northwind/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Customers/Northwind/");

      return Deployment;
    }
  }

  public static DeploymentPlan SeamarkFarms {
    get {
      var Deployment = new DeploymentPlan("Seamark Farms");
      Deployment.Description = "Sweet Sheep for Cheap";

      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Customers/SeamarkFarms/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Customers/SeamarkFarms/");

      return Deployment;
    }
  }

  public static DeploymentPlan Wingtip {
    get {
      var Deployment = new DeploymentPlan("Wingtip Toys");
      Deployment.Description = "Retro toys for nostalgic girls and boys";


      // setup Web datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.webDatasourcePathParameter,
                                        DeploymentPlan.webDatasourceRootDefault + "Customers/Wingtip/");

      // setup ADLS datasource path
      Deployment.AddDeploymentParameter(DeploymentPlan.adlsServerPathParameter,
                                        DeploymentPlan.adlsServerPathDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerNameParameter,
                                        DeploymentPlan.adlsContainerNameDefault);

      Deployment.AddDeploymentParameter(DeploymentPlan.adlsContainerPathParameter,
                                        "/ProductSales/Customers/Wingtip/");

      return Deployment;
    }
  }

}
