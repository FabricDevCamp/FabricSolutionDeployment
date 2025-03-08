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

    var targetWorkspaceArgument = new Argument<string>(
        name: "workspace",
        description: "Name of workspace to deploy to.");

    var rootCommand = new RootCommand("CLI for Fabric Multitenant Deployment");

    var deployCommand = new Command("deploy", "Deploy a solution to a workspace.")
    {
        powerbiOption,
        notebookOption,
        shortcutOption,
        dataPipelineOption
    };
    deployCommand.AddArgument(targetWorkspaceArgument);
    rootCommand.AddCommand(deployCommand);

    deployCommand.SetHandler((workspace, powerbi, notebook, shortcut, dataPipeline) =>
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
    },
    targetWorkspaceArgument, powerbiOption, notebookOption, shortcutOption, dataPipelineOption);

    var exportCommand = new Command("export", "Export item definitions from a workspace.")
    {
        itemDefinitionsOption
    };
    exportCommand.AddArgument(targetWorkspaceArgument);
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
    targetWorkspaceArgument, itemDefinitionsOption);

    return await rootCommand.InvokeAsync(args);
  }
}
