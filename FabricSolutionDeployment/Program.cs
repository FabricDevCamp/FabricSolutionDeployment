
class Program {

  const string FabricPowerBiSolution = "Fabric Power BI Solution";
  const string FabricNotebookSolution = "Fabric Notebook Solution";
  const string FabricShortcutSolution = "Fabric Shortcut Solution";
  const string FabricDataPipelineSolution = "Fabric Data Pipeline Solution";

  public static void Main() {

    Setup_ViewWorkspacesAndCapacities();

    // Lab01_DeploySolutionsUsingFromItemDefinitions();
    // Lab02_ExportItemDefinitionsFromWorkspace();
    // Lab03_DeployWithParameterizeDatasourcePaths();
    // Lab04_DeploySolutionFromSourceWorkspace();
    // Lab05_UpdateSolutionFromSourceWorkspace();
    // Lab06_SetupStagedDeployment();
    // Lab07_PushSolutionUpdatesToProduction();
    // Lab08_ConnectDevWorkspaceToGitRepository();
    // Lab09_ExportWorkspaceToPackagedSolutionFolder();
    // Lab10_DeployAndUpdateFromSolutionPackageFolder();

  }

  public static void Setup_ViewWorkspacesAndCapacities() {
    DeploymentManager.ViewWorkspaces();
    DeploymentManager.ViewCapacities();
  }

  public static void Lab01_DeploySolutionsUsingFromItemDefinitions() {
    DeploymentManager.DeployPowerBiSolution(FabricPowerBiSolution);
    //DeploymentManager.DeployNotebookSolution(FabricNotebookSolution);
    //DeploymentManager.DeployShortcutSolution(FabricShortcutSolution);
    //DeploymentManager.DeployDataPipelineSolution(FabricDataPipelineSolution);
  }

  public static void Lab02_ExportItemDefinitionsFromWorkspace() {
    DeploymentManager.ExportItemDefinitionsFromWorkspace(FabricPowerBiSolution);
    DeploymentManager.ExportItemDefinitionsFromWorkspace(FabricNotebookSolution);
    DeploymentManager.ExportItemDefinitionsFromWorkspace(FabricShortcutSolution);
    DeploymentManager.ExportItemDefinitionsFromWorkspace(FabricDataPipelineSolution);
  }

  public static void Lab03_DeployWithParameterizeDatasourcePaths() {
    DeploymentManager.DeployPowerBiSolution(SampleCustomerData.Contoso);
    DeploymentManager.DeployNotebookSolution(SampleCustomerData.Contoso);
    DeploymentManager.DeployShortcutSolution(SampleCustomerData.Contoso);
    DeploymentManager.DeployDataPipelineSolution(SampleCustomerData.Contoso);
  }

  const string TenantPrefix = "Tenant - ";
  const string ContosoPowerBiSolution = TenantPrefix + "Contoso Power BI Solution";
  const string ContosoNotebookSolution = TenantPrefix + "Contoso Notebook Solution";
  const string ContosoShortcutSolution = TenantPrefix + "Contoso Shortcut Solution";
  const string ContosoDataPipelineSolution = TenantPrefix + "Contoso Data Pipeline Solution";


  public static void Lab04_DeploySolutionFromSourceWorkspace() {
    DeploymentManager.DeploySolutionFromSourceWorkspace(FabricPowerBiSolution, ContosoPowerBiSolution, SampleCustomerData.Contoso);
    DeploymentManager.DeploySolutionFromSourceWorkspace(FabricNotebookSolution, ContosoNotebookSolution, SampleCustomerData.Contoso);
    DeploymentManager.DeploySolutionFromSourceWorkspace(FabricShortcutSolution, ContosoShortcutSolution, SampleCustomerData.Contoso);
    DeploymentManager.DeploySolutionFromSourceWorkspace(FabricDataPipelineSolution, ContosoDataPipelineSolution, SampleCustomerData.Contoso);
  }

  public static void Lab05_UpdateSolutionFromSourceWorkspace() {
    DeploymentManager.UpdateSolutionFromSourceWorkspace(FabricPowerBiSolution, ContosoPowerBiSolution, SampleCustomerData.Contoso);
    DeploymentManager.UpdateSolutionFromSourceWorkspace(FabricNotebookSolution, ContosoNotebookSolution, SampleCustomerData.Contoso);
    DeploymentManager.UpdateSolutionFromSourceWorkspace(FabricShortcutSolution, ContosoShortcutSolution, SampleCustomerData.Contoso);
    DeploymentManager.UpdateSolutionFromSourceWorkspace(FabricDataPipelineSolution, ContosoDataPipelineSolution, SampleCustomerData.Contoso);
  }

  const string StagedDeploymentName = "Product Sales";
  const string StagedDeploymentDevStage = StagedDeploymentName + " Dev";
  const string StagedDeploymentTestStage = StagedDeploymentName + " Test";
  const string StagedDeploymentProdStage = StagedDeploymentName + " Prod";

  public static void Lab06_SetupStagedDeployment() {

    // set up staged deplpyment with Dev>Test>Prod workflow
    DeploymentManager.SetupDeploymentStages(StagedDeploymentName);

    // create test customer tenant worksace from prod stage workspace
    string workspaceName = $"Tenant - Contoso Solution from [{StagedDeploymentProdStage}]";
    DeploymentManager.DeploySolutionFromSourceWorkspace(StagedDeploymentProdStage, workspaceName, SampleCustomerData.Contoso);

  }

  public static void Lab07_PushSolutionUpdatesToProduction() {
    DeploymentManager.UpdateDeloymentStage(StagedDeploymentName, StagedDeploymentType.UpdateFromDevToTest);
    DeploymentManager.UpdateDeloymentStage(StagedDeploymentName, StagedDeploymentType.UpdateFromTestToProd);
  }

  public static void Lab08_ConnectDevWorkspaceToGitRepository() {
    DeploymentManager.ConnectWorkspaceToGit(StagedDeploymentDevStage);
  }

  public const string ProductSalesSolutionV1 = "Product Sales v1.0";

  public static void Lab09_ExportWorkspaceToPackagedSolutionFolder() {
    DeploymentManager.ExportWorkspaceToPackagedSolutionFolder(StagedDeploymentProdStage, ProductSalesSolutionV1);
  }

  class SolutionGallery {
    public const string FabricPowerBiPackagedSolution_v1 = "Fabric Power BI Solution v1.0";
    public const string FabricNotebookPackagedSolution_v1 = "Fabric Notebook Solution v1.0";
    public const string FabricShortcutPackagedSolution_v1 = "Fabric Shortcut Solution v1.0";
    public const string FabricDataPipelinePackagedSolution_v1 = "Fabric Data Pipeline Solution v1.0";
  }

  public static void Lab10_DeployAndUpdateFromSolutionPackageFolder() {

    string ContosoProductSales = "Tenant - Contoso Product Sales";

    // deploy from packaged solution folder
    DeploymentManager.DeploySolutionFromPackagedSolutionFolder(ProductSalesSolutionV1,
                                                               ContosoProductSales,
                                                               SampleCustomerData.Contoso);

    // update from packaged solution folder
    DeploymentManager.UpdateSolutionFromPackagedSolutionFolder(ProductSalesSolutionV1,
                                                               ContosoProductSales,
                                                               SampleCustomerData.Contoso);

    // deploy solutions from gallery of packaged solution folders
    DeploymentManager.DeploySolutionFromPackagedSolutionFolder(SolutionGallery.FabricPowerBiPackagedSolution_v1,
                                                              ContosoPowerBiSolution,
                                                              SampleCustomerData.Contoso);

    DeploymentManager.DeploySolutionFromPackagedSolutionFolder(SolutionGallery.FabricNotebookPackagedSolution_v1,
                                                              ContosoNotebookSolution,
                                                              SampleCustomerData.Contoso);


    DeploymentManager.DeploySolutionFromPackagedSolutionFolder(SolutionGallery.FabricShortcutPackagedSolution_v1,
                                                               ContosoShortcutSolution,
                                                               SampleCustomerData.Contoso);

    DeploymentManager.DeploySolutionFromPackagedSolutionFolder(SolutionGallery.FabricDataPipelinePackagedSolution_v1,
                                                               ContosoDataPipelineSolution,
                                                               SampleCustomerData.Contoso);

  }

}