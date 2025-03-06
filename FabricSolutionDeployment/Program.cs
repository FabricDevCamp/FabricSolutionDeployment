
namespace FabricSolutionDeployment;

class Program {

  // workspace names
  const string CustomPowerBiSolution = "Custom Power BI Solution";
  const string CustomNotebookSolution = "Custom Notebook Solution";
  const string CustomShortcutSolution = "Custom Shortcut Solution";
  const string CustomDataPipelineSolution = "Custom Data Pipeline Solution";

  public static void Main() {

    Setup_ViewWorkspacesAndCapacities();

    // Lab01_DeploySolutionWithItemDefinitions();
    // Lab02_ExportItemDefinitionsFromWorkspace();
    // Lab03_DeployWithParameterizeDatasourcePaths();
    // Lab04_DeployFromSourceWorkspace();
    // Lab05_UpdateFromSourceWorkspace();
    // Lab06_ExportWorkspceToLocalSolutionFolder();
    // Lab07_DeployFromLocalSolutionFolder();
    // Lab08_ConnectDevWorkspaceToAzureDevOps();
    // Lab09_SetupStagedDeploymentForEnterprise();
    // Lab10_SetupStagedDeploymentForMultitenancy();
  }

  public static void Setup_ViewWorkspacesAndCapacities() {
    DeploymentManager.ViewWorkspaces();
    DeploymentManager.ViewCapacities();
  }

  public static void Lab01_DeploySolutionWithItemDefinitions() {
    DeploymentManager.DeployPowerBiSolution(CustomPowerBiSolution);
    // DeploymentManager.DeployNotebookSolution(CustomNotebookSolution);
    // DeploymentManager.DeployShortcutSolution(CustomShortcutSolution);
    // DeploymentManager.DeployDataPipelineSolution(CustomDataPipelineSolution);
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

  public static void Lab04_DeployFromSourceWorkspace() {
    DeploymentManager.DeployFromSourceWorkspace(CustomPowerBiSolution, SampleCustomerData.AdventureWorks);
    DeploymentManager.DeployFromSourceWorkspace(CustomNotebookSolution, SampleCustomerData.Contoso);
    DeploymentManager.DeployFromSourceWorkspace(CustomShortcutSolution, SampleCustomerData.Fabricam);
    DeploymentManager.DeployFromSourceWorkspace(CustomDataPipelineSolution, SampleCustomerData.Northwind);
  }

  public static void Lab05_UpdateFromSourceWorkspace() {
    // add new reports to source workspace
    string reportFolder1 = "Product Sales Time Intelligence.Report";
    DeploymentManager.AddSalesReportToCustomerWorkspace(CustomPowerBiSolution, reportFolder1);
    string reportFolder2 = "Product Sales Top 10 Cities.Report";
    DeploymentManager.AddSalesReportToCustomerWorkspace(CustomPowerBiSolution, reportFolder2);

    // deploy new reports to customer tenant using Update operation
    DeploymentManager.UpdateFromSourceWorkspace(CustomPowerBiSolution, SampleCustomerData.AdventureWorks);
  }

  public const string ProductSalesSolution = "Product Sales Premium";

  public static void Lab06_ExportWorkspceToLocalSolutionFolder() {
    DeploymentManager.ExportWorkspaceToLocalSolutionFolder(CustomDataPipelineSolution, ProductSalesSolution);
  }

  public static void Lab07_DeployFromLocalSolutionFolder() {
    DeploymentManager.DeployFromLocalSolutionFolder(ProductSalesSolution, SampleCustomerData.AdventureWorks);
    DeploymentManager.DeployFromLocalSolutionFolder(ProductSalesSolution, SampleCustomerData.Contoso);
    DeploymentManager.DeployFromLocalSolutionFolder(ProductSalesSolution, SampleCustomerData.Fabricam);
    DeploymentManager.DeployFromLocalSolutionFolder(ProductSalesSolution, SampleCustomerData.Northwind);
  }

  // staged deployment workspace names
  const string DevWorkspace = "Product Sales Dev";
  const string TestWorkspace = "Product Sales Test";
  const string ProdWorkspace = "Product Sales Prod";

  public static void Lab08_ConnectDevWorkspaceToAzureDevOps() {

    // create dev workspae and connect to GIT
    DeploymentManager.DeployDataPipelineSolution(DevWorkspace, StagingEnvironments.Dev);
    DeploymentManager.ConnectWorkspaceToGit(DevWorkspace);

    // Test branching out to feature workspaces
    // DeploymentManager.BranchOutToFeatureWorkspace(DevWorkspace, "Feature 1");
    // DeploymentManager.BranchOutToFeatureWorkspace(DevWorkspace, "Feature 2");

  }

  public static void Lab09_SetupStagedDeploymentForEnterprise() {
    // set up staged deployment
    DeploymentManager.ExportWorkspaceToAdoSolutionFolder(DevWorkspace, TestWorkspace);
    DeploymentManager.DeployFromAdoSolutionFolder(TestWorkspace, TestWorkspace, StagingEnvironments.Test);
    DeploymentManager.ExportWorkspaceToAdoSolutionFolder(TestWorkspace, ProdWorkspace);
    DeploymentManager.DeployFromAdoSolutionFolder(ProdWorkspace, ProdWorkspace, StagingEnvironments.Prod);
  }

  public static void Lab10_SetupStagedDeploymentForMultitenancy() {
    // use staged deloyment to deploy customer tenant workspaces
    DeploymentManager.DeployFromAdoSolutionFolder(ProdWorkspace, SampleCustomerData.AdventureWorks);
    DeploymentManager.DeployFromAdoSolutionFolder(ProdWorkspace, SampleCustomerData.Contoso);
    DeploymentManager.DeployFromAdoSolutionFolder(ProdWorkspace, SampleCustomerData.Fabricam);
    DeploymentManager.DeployFromAdoSolutionFolder(ProdWorkspace, SampleCustomerData.Northwind);
    DeploymentManager.DeployFromAdoSolutionFolder(ProdWorkspace, SampleCustomerData.Wingtip);
    DeploymentManager.DeployFromAdoSolutionFolder(ProdWorkspace, SampleCustomerData.SeamarkFarms);

    // string first_build = AdoProjectManager.GetFirstDailyBuildBranch(ProdWorkspace);
    // DeploymentManager.UpdateFromAdoSolutionFolder(ProdWorkspace, SampleCustomerData.Northwind, first_build, true);

  }

}