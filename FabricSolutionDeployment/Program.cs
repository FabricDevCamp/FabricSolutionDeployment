
class Program {

  const string TestWorkspaceNameName = "Contoso";
  const string IsvLakehouseSolution = "ISV Lakehouse Solution";

  public static void Main() {

    DeploymentManager.ExportSolutionFolderFromWorkspace(IsvLakehouseSolution);

    // Setup_ViewWorkspacesAndCapacities();

    // Demo01_DeploySolutionToWorkspace();
    // Demo02_GetItemDefinitionsFromWorkspace();
    // Demo03_DeployCustomerTenantFromWorkspaceTemplate();
    // Demo04_UpdateCustomerTenantFromWorkspaceTemplate();
    // Demo05_ConnectWorkspaceTemplatesToAzureDevOps();
    // Demo06_DeployCustomerTenantFromProjectTemplate();
    // Demo07_UpdateCustomerTenantFromProjectTemplate();

  }

  public static void Setup_ViewWorkspacesAndCapacities() {
    DeploymentManager.ViewWorkspaces();
    DeploymentManager.ViewCapacities();
  }

  public static void Demo01_DeploySolutionToWorkspace() {
    DeploymentManager.DeployWorkspaceWithLakehouseSolution(TestWorkspaceNameName);
  }

  public static void Demo02_GetItemDefinitionsFromWorkspace() {
    DeploymentManager.ExportItemDefinitionsFromWorkspace(IsvLakehouseSolution);
  }

  public static void Demo03_DeployCustomerTenantFromWorkspaceTemplate() {

    // deploy workspace to play the role of workspace template
    DeploymentManager.DeployWorkspaceWithLakehouseSolution(IsvLakehouseSolution);

    // deploy customer tenant workspaces from workspace templates
    // DeploymentManager.DeploySolutionFromWorkspaceTemplate(IsvLakehouseSolution, "Customer 1");
    // DeploymentManager.DeploySolutionFromWorkspaceTemplate(IsvLakehouseSolution, "Customer 2");
    // DeploymentManager.DeploySolutionFromWorkspaceTemplate(IsvPowerBiSolution, "Customer 3");
  }

  public static void Demo04_UpdateCustomerTenantFromWorkspaceTemplate() {
    DeploymentManager.UpdateSolutionFromWorkspaceTemplate(IsvLakehouseSolution, "Customer 1");
  }

  public static void Demo05_ConnectWorkspaceTemplatesToAzureDevOps() {
    DeploymentManager.ConnectWorkspaceToGit(TestWorkspaceNameName);
    DeploymentManager.PushDeployConfigToGitRepo(IsvLakehouseSolution);
  }


  public static void Demo06_DeployCustomerTenantFromProjectTemplate() {
    DeploymentManager.DeploySolutionFromProjectTemplate(IsvLakehouseSolution, "Customer 1");
  }

  public static void Demo07_UpdateCustomerTenantFromProjectTemplate() {
    DeploymentManager.UpdateSolutionFromProjectTemplate(IsvLakehouseSolution, "Customer 1");
  }

}