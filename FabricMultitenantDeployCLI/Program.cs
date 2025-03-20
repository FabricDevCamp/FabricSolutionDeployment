using FabricSolutionDeployment;

namespace FabricMultitenantDeployCLI;

class Program
{
  static async Task<int> Main(string[] args)
  {
#if DEBUG
    args = ["deploy", "--power-bi", "--workspace", "Custom Power BI Solution"];
#endif
    if (args.Length == 0)
    {
      Console.WriteLine("No command specified.");
      return 1;
    }

    string command = args[0];
    var options = args.Skip(1).ToArray();

    switch (command)
    {
      case "deploy":
        await HandleDeployCommand(options);
        break;
      case "export":
        await HandleExportCommand(options);
        break;
      default:
        Console.WriteLine($"Unknown command: {command}");
        return 1;
    }

    return 0;
  }

  private static Task HandleDeployCommand(string[] options)
  {
    string? workspace = null;
    string? deploymentPlan = null;
    bool powerbi = false;
    bool notebook = false;
    bool shortcut = false;
    bool dataPipeline = false;

    for (int i = 0; i < options.Length; i++)
    {
      switch (options[i])
      {
        case "--workspace":
          workspace = options[++i];
          break;
        case "--deployment-plan":
          deploymentPlan = options[++i];
          break;
        case "--power-bi":
          powerbi = true;
          break;
        case "--notebook":
          notebook = true;
          break;
        case "--shortcut":
          shortcut = true;
          break;
        case "--data-pipeline":
          dataPipeline = true;
          break;
        default:
          Console.WriteLine($"Unknown option: {options[i]}");
          return Task.CompletedTask;
      }
    }

    if (workspace == null && deploymentPlan == null)
    {
      Console.WriteLine("Either --workspace or --deployment-plan must be specified.");
      return Task.CompletedTask;
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
        return Task.CompletedTask;
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

    return Task.CompletedTask;
  }

  private static Task HandleExportCommand(string[] options)
  {
    string? workspace = null;
    bool itemDefinitions = false;

    for (int i = 0; i < options.Length; i++)
    {
      switch (options[i])
      {
        case "--workspace":
          workspace = options[++i];
          break;
        case "--item-definitions":
          itemDefinitions = true;
          break;
        default:
          Console.WriteLine($"Unknown option: {options[i]}");
          return Task.CompletedTask;
      }
    }

    if (workspace == null)
    {
      Console.WriteLine("Workspace must be specified.");
      return Task.CompletedTask;
    }

    Console.WriteLine($"Exporting from {workspace}.");
    if (itemDefinitions)
    {
      Console.WriteLine("Exporting item definitions.");
      DeploymentManager.ExportItemDefinitionsFromWorkspace(workspace);
    }

    return Task.CompletedTask;
  }
}
