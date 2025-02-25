
class Program {

  const string CustomPowerBiSolution = "Custom Power BI Solution";
  const string CustomNotebookSolution = "Custom Notebook Solution";
  const string CustomShortcutSolution = "Custom Shortcut Solution";
  const string CustomDataPipelineSolution = "Custom Data Pipeline Solution";

  public static void Main() {

    Setup_ViewWorkspacesAndCapacities();

    // Lab01_DeploySolutionsUsingFromItemDefinitions();
    // Lab02_ExportItemDefinitionsFromWorkspace();
    // Lab03_DeployWithParameterizeDatasourcePaths();
    // Lab04_DeploySolutionFromSourceWorkspace();
    // Lab05_UpdateSolutionFromSourceWorkspace();
    // Lab06_SetupStagedDeployment();
    // Lab07_PushSolutionUpdatesToProduction();
    // Lab08_ExportWorkspaceToPackagedSolutionFolder();
    // Lab09_DeployAndUpdateFromSolutionPackageFolder();
    // Lab10_ConnectDevWorkspaceToGitRepository();
  }

  public static void Setup_ViewWorkspacesAndCapacities() {
    DeploymentManager.ViewWorkspaces();
    DeploymentManager.ViewCapacities();
  }

  public static void Lab01_DeploySolutionsUsingFromItemDefinitions() {
    DeploymentManager.DeployPowerBiSolution(CustomPowerBiSolution);
    DeploymentManager.DeployNotebookSolution(CustomNotebookSolution);
    DeploymentManager.DeployShortcutSolution(CustomShortcutSolution);
    DeploymentManager.DeployDataPipelineSolution(CustomDataPipelineSolution);
  }

  public static void Lab02_ExportItemDefinitionsFromWorkspace() {
    DeploymentManager.ExportItemDefinitionsFromWorkspace(CustomPowerBiSolution);
    DeploymentManager.ExportItemDefinitionsFromWorkspace(CustomNotebookSolution);
    DeploymentManager.ExportItemDefinitionsFromWorkspace(CustomShortcutSolution);
    DeploymentManager.ExportItemDefinitionsFromWorkspace(CustomDataPipelineSolution);
  }

  public static void Lab03_DeployWithParameterizeDatasourcePaths() {
    DeploymentManager.DeployPowerBiSolution(SampleCustomerData.AdventureWorks);
    DeploymentManager.DeployNotebookSolution(SampleCustomerData.Contoso);
    DeploymentManager.DeployShortcutSolution(SampleCustomerData.Fabricam);
    DeploymentManager.DeployDataPipelineSolution(SampleCustomerData.Northwind);
  }

  public static void Lab04_DeploySolutionFromSourceWorkspace() {
    DeploymentManager.DeploySolutionFromSourceWorkspace(CustomPowerBiSolution, SampleCustomerData.AdventureWorks);
    DeploymentManager.DeploySolutionFromSourceWorkspace(CustomNotebookSolution, SampleCustomerData.Contoso);
    DeploymentManager.DeploySolutionFromSourceWorkspace(CustomShortcutSolution, SampleCustomerData.Fabricam);
    DeploymentManager.DeploySolutionFromSourceWorkspace(CustomDataPipelineSolution, SampleCustomerData.Northwind);
  }

  public static void Lab05_UpdateSolutionFromSourceWorkspace() {

    string reportFolder1 = "Product Sales Time Intelligence.Report";
    DeploymentManager.AddSalesReportToCustomerWorkspace(CustomPowerBiSolution, reportFolder1);

    string reportFolder2 = "Product Sales Top 10 Cities.Report";
    DeploymentManager.AddSalesReportToCustomerWorkspace(CustomPowerBiSolution, reportFolder2);

    DeploymentManager.UpdateSolutionFromSourceWorkspace(CustomPowerBiSolution, SampleCustomerData.AdventureWorks);

  }

  // Staged deployment for Enterprise scenario with one target workspace
  const string StagedDeployment1 = "Product Sales Project";
  const string StagedDeployment1Dev = StagedDeployment1 + " Dev";
  const string StagedDeployment1Prod = StagedDeployment1 + " Prod";

  // Staged deployment for multi-tenant scenario with many target workspaces
  const string StagedDeployment2 = "Product Sales Premium";
  const string StagedDeployment2Dev = StagedDeployment2 + " Dev";
  const string StagedDeployment2Prod = StagedDeployment2 + " Prod";

  public static void Lab06_SetupStagedDeployment() {

    // set up staged deployment from Dev > Test > Prod
    DeploymentManager.SetupStagedDeploymentWithNotebookSolution(StagedDeployment1);

    // set up staged deployment from Dev > Test > Prod
    DeploymentManager.SetupStagedDeploymentWithDataPipelineSolution(StagedDeployment2);

    // create tenant workspaces for customers
    DeploymentManager.DeploySolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.AdventureWorks);
  
    //DeploymentManager.DeploySolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Contoso);
    //DeploymentManager.DeploySolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Fabricam);
    //DeploymentManager.DeploySolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Northwind);
    //DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.SeamarkFarms);
    //DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Wingtip);
  }

  public static void Lab07_PushSolutionUpdatesToProduction() {

    // push updates from [Product Sales Project Dev] > [Product Sales Project Test]
    DeploymentManager.UpdateDeloymentStage(StagedDeployment1, StagedDeploymentType.UpdateFromDevToTest);

    // push updates from [Product Sales Project Test] > [Product Sales Project Prod]
    DeploymentManager.UpdateDeloymentStage(StagedDeployment1, StagedDeploymentType.UpdateFromTestToProd);

    // push updates from [Product Sales Premium Dev] > [Product Sales Premium Test]
    DeploymentManager.UpdateDeloymentStage(StagedDeployment2, StagedDeploymentType.UpdateFromDevToTest);

    // push updates from [Product Sales Premium Test] > [Product Sales Premium Prod]
    DeploymentManager.UpdateDeloymentStage(StagedDeployment2, StagedDeploymentType.UpdateFromTestToProd);

    // push updates from [Product Sales Premium Prod] > [Customer Tenants]
    DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.AdventureWorks);

    // DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Contoso);
    // DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Fabricam);
    // DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Northwind);
    // DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.SeamarkFarms);
    // DeploymentManager.UpdateSolutionFromSourceWorkspace(StagedDeployment2Prod, SampleCustomerData.Wingtip);
  }

  public const string ProductSalesSolutionV1 = "Product Sales Premium v1.0";

  public static void Lab08_ExportWorkspaceToPackagedSolutionFolder() {

    // export [Product Sales Premium Prod] workspace to packaged solution folder [Product Sales Premium v1.0]
    DeploymentManager.ExportWorkspaceToPackagedSolutionFolder(StagedDeployment2Prod, ProductSalesSolutionV1);

  }

  public static void Lab09_DeployAndUpdateFromSolutionPackageFolder() {

    // deploy from packaged solution folder [Product Sales v1.0] to [Customer Tenants]
    DeploymentManager.DeploySolutionFromPackagedSolutionFolder(ProductSalesSolutionV1, SampleCustomerData.AdventureWorks);

    // DeploymentManager.DeploySolutionFromPackagedSolutionFolder(ProductSalesSolutionV1, SampleCustomerData.Contoso);
    // DeploymentManager.DeploySolutionFromPackagedSolutionFolder(ProductSalesSolutionV1, SampleCustomerData.Fabricam);
    // DeploymentManager.DeploySolutionFromPackagedSolutionFolder(ProductSalesSolutionV1, SampleCustomerData.Northwind);
    // DeploymentManager.DeploySolutionFromPackagedSolutionFolder(ProductSalesSolutionV1, SampleCustomerData.SeamarkFarms);
    // DeploymentManager.DeploySolutionFromPackagedSolutionFolder(ProductSalesSolutionV1, SampleCustomerData.Wingtip);
  }

  public static void Lab10_ConnectDevWorkspaceToGitRepository() {

    // Use Fabric GIT integration to connect [Product Sales Project Dev] to Azure Dev Ops repository
    DeploymentManager.ConnectWorkspaceToGit(StagedDeployment1Dev);

    // Use Fabric GIT integration to connect [Product Sales Premium Dev] to Azure Dev Ops repository
    DeploymentManager.ConnectWorkspaceToGit(StagedDeployment2Dev);

  }

}