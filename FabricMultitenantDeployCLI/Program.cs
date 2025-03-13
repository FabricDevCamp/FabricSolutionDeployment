using FabricSolutionDeployment;
using System.CommandLine;

namespace FabricMultitenantDeployCLI;

class Program
{
  static async Task<int> Main(string[] args)
  {
    var powerbiOption = new Option<bool>(
        name: "--power-bi",
        description: "Deploy Power BI solution.");

    var notebookOption = new Option<bool>(
        name: "--notebook",
        description: "Deploy Notebook solution.");

    var shortcutOption = new Option<bool>(
        name: "--shortcut",
        description: "Deploy Shortcut solution.");

    var dataPipelineOption = new Option<bool>(
        name: "--data-pipeline",
        description: "Deploy Data Pipeline solution.");

    var itemDefinitionsOption = new Option<bool>(
        name: "--item-definitions",
        description: "Export item definitions from the workspace.");

    var targetWorkspaceOption = new Option<string?>(
        name: "--workspace",
        description: "Name of workspace to deploy to.");

    var deploymentPlanOption = new Option<string?>(
        name: "--deployment-plan",
        description: "Name of the deployment plan.");

    var rootCommand = new RootCommand("CLI for Fabric Multitenant Deployment");

    var deployCommand = new Command("deploy", "Deploy a solution to a workspace.")
    {
        powerbiOption,
        notebookOption,
        shortcutOption,
        dataPipelineOption,
        targetWorkspaceOption,
        deploymentPlanOption
    };
    rootCommand.AddCommand(deployCommand);

    deployCommand.SetHandler((workspace, deploymentPlan, powerbi, notebook, shortcut, dataPipeline) =>
    {
      if (workspace == null && deploymentPlan == null)
      {
        Console.WriteLine("Either --workspace or --deployment-plan must be specified.");
        return;
      }

      if (workspace != null)
      {
        Console.WriteLine($"Deploying to {workspace}.");
        if (powerbi)
        {
          Console.WriteLine("Deploying Power BI solution.");
          DeploymentManager.DeployPowerBiSolution(workspace);
        }
        if (notebook)
        {
          Console.WriteLine("Deploying Notebook solution.");
          DeploymentManager.DeployNotebookSolution(workspace);
        }
        if (shortcut)
        {
          Console.WriteLine("Deploying Shortcut solution.");
          DeploymentManager.DeployShortcutSolution(workspace);
        }
        if (dataPipeline)
        {
          Console.WriteLine("Deploying Data Pipeline solution.");
          DeploymentManager.DeployDataPipelineSolution(workspace);
        }
      }

      else if (deploymentPlan != null)
      {
        Dictionary<string, DeploymentPlan> plans = new Dictionary<string, DeploymentPlan>
        {
          { "AdventureWorks", SampleCustomerData.AdventureWorks },
          { "Contoso", SampleCustomerData.Contoso },
          { "Fabricam", SampleCustomerData.Fabricam },
          { "Northwind", SampleCustomerData.Northwind }
        };
        if (!plans.TryGetValue(deploymentPlan, out DeploymentPlan? plan))
        {
          Console.WriteLine($"Deployment plan {deploymentPlan} not found.");
          return;
        }

        if (powerbi)
        {
          Console.WriteLine($"Deploying Power Bi solution using deployment plan from {deploymentPlan}.");
          DeploymentManager.DeployPowerBiSolution(plan);
        }
        if (notebook)
        {
          Console.WriteLine($"Deploying Notebook solution using deployment plan from {deploymentPlan}.");
          DeploymentManager.DeployNotebookSolution(plan);
        }
        if (shortcut)
        {
          Console.WriteLine($"Deploying Shortcut solution using deployment plan from {deploymentPlan}.");
          DeploymentManager.DeployShortcutSolution(plan);
        }
        if (dataPipeline)
        {
          Console.WriteLine($"Deploying Data Pipeline solution using deployment plan from {deploymentPlan}.");
          DeploymentManager.DeployDataPipelineSolution(plan);
        }
      }
    },
    targetWorkspaceOption, deploymentPlanOption, powerbiOption, notebookOption, shortcutOption, dataPipelineOption);

    var exportCommand = new Command("export", "Export item definitions from a workspace.")
    {
        itemDefinitionsOption,
        targetWorkspaceOption
    };
    rootCommand.AddCommand(exportCommand);

    exportCommand.SetHandler((workspace, itemDefinitions) =>
    {
      Console.WriteLine($"Exporting from {workspace}.");
      if (itemDefinitions)
      {
        Console.WriteLine("Exporting item definitions.");
        DeploymentManager.ExportItemDefinitionsFromWorkspace(workspace);
      }
    },
    targetWorkspaceOption, itemDefinitionsOption);

    return await rootCommand.InvokeAsync(args);
  }
}
