using System.Text.Json.Serialization;
using Microsoft.Fabric.Api.Core.Models;

public class SolutionDeploymentPlan {
  public List<DeploymentItem> DeploymentItems { get; set; }
  public DeploymentConfiguration DeployConfig { get; set; }
  public List<string> ItemNames { 
    get {
    var itemNames = new List<string>();
      foreach (var item in DeploymentItems) {
        itemNames.Add(item.ItemName);
      }
      return itemNames;
    } 
  }

  public string GetSourceWorkspaceId() {
    return DeployConfig.SourceWorkspaceId;
  }

  public List<DeploymentItem> GetLakehouses() {
    return DeploymentItems.Where(item => item.Type == "Lakehouse").ToList();
  }

  public List<DeploymentItem> GetNotebooks() {
    return DeploymentItems.Where(item => item.Type == "Notebook").ToList();
  }

  public List<DeploymentItem> GetSemanticModels() {
    return DeploymentItems.Where(item => item.Type == "SemanticModel").ToList();
  }

  public List<DeploymentItem> GetReports() {
    return DeploymentItems.Where(item => item.Type == "Report").ToList();
  }

  public DeploymentSourceLakehouse GetSourceLakehouse(string DisplayName) {
    return DeployConfig.SourceLakehouses.FirstOrDefault(item => item.DisplayName == DisplayName);
  }

  public DeploymentSourceItem GetSourceNotebook(string DisplayName) {
    return DeployConfig.SourceItems.FirstOrDefault(item => (item.Type == "Lakehouse") &&
                                                           (item.DisplayName == DisplayName));
  }

  public DeploymentSourceItem GetSourceSemanticModel(string DisplayName) {
    return DeployConfig.SourceItems.FirstOrDefault(item => (item.Type == "SemanticModel") &&
                                                           (item.DisplayName == DisplayName));
  }
  
  public DeploymentSourceItem GetSourceReport(string DisplayName) {
    return DeployConfig.SourceItems.FirstOrDefault(item => (item.Type == "Report") &&
                                                           (item.DisplayName == DisplayName));
  }

}

public class DeploymentItem {
  public string DisplayName { get; set; }
  public string Type { get; set; }
  public string ItemName { get { return $"{DisplayName}.{Type}"; } }
  public ItemDefinition Definition { get; set; }
}

public class DeploymentItemFile {
  public string Path { get; set; }
  public string Content { get; set; }
}

public class ItemDefinitonFile {
  public string FullPath { get; set; }
  public string Content { get; set; }

  public string ItemName { 
    get {
      return FullPath.Contains("/") ? FullPath.Substring(0, FullPath.IndexOf("/")) : FullPath; 
    }
  }
  
  public string Path {
    get {
      int firstSlash = FullPath.IndexOf("/");
      if (firstSlash == -1) {
        return FullPath;
      }
      else {
        int start = firstSlash + 1;
        int length = FullPath.Length - start;
        return FullPath.Substring(start, length);

      }
    }
  }

  public string FileName {
    get {
      return FullPath.Substring(FullPath.LastIndexOf('/') + 1);
    }
  }

}

public class DeploymentConfiguration {
  public string SourceWorkspaceId { get; set; }
  public List<DeploymentSourceItem> SourceItems { get; set; }
  public List<DeploymentSourceLakehouse> SourceLakehouses { get; set; }
  public List<DeploymentSourceConnection> SourceConnections { get; set; }
  public Dictionary<string, string> CustomerData { get; set; }
}

public class DeploymentSourceItem {
  public string Id { get; set; }
  public string DisplayName{ get; set; }
  public string Type { get; set; }
}

public class DeploymentSourceLakehouse {
  public string Id { get; set; }
  public string DisplayName { get; set; }
  public string Server { get; set; }
  public string Database { get; set; }
  public List<DeploymentSourceLakehouseShortcut> Shortcuts { get; set; }
}

public class DeploymentSourceLakehouseShortcut {
  public string ConnectionId { get; set; }
  public string Name { get; set; }
  public string Type { get; set; }
  public string Location { get; set; }
  public string Subpath{ get; set; }
}

public class DeploymentSourceConnection {
  public string Id { get; set; }
  public string DisplayName { get; set; }
  public string Type { get; set; }
  public string Path { get; set; }
  public string CredentialType { get; set; }
}

// types to serailize/deserialize .platform files
public class PlatformFileConfig {
  public string version { get; set; }
  public string logicalId { get; set; }
}

public class PlatformFileMetadata {
  public string type { get; set; }
  public string displayName { get; set; }
}

public class FabricPlatformFile {
  [JsonPropertyName("$schema")]
  public string schema { get; set; }
  public PlatformFileMetadata metadata { get; set; }
  public PlatformFileConfig config { get; set; }
}

// types to serailize/deserialize definition.pbir files

public class ReportDefinitionFile {
  public string version { get; set; }
  public DatasetReference datasetReference { get; set; }
}

public class DatasetReference {
  public ByPathReference byPath { get; set; }
  public ByConnectionReference byConnection { get; set; }
}

public class ByPathReference {
  public string path { get; set; }
}

public class ByConnectionReference {
  public string connectionString { get; set; }
  public object pbiServiceModelId { get; set; }
  public string pbiModelVirtualServerName { get; set; }
  public string pbiModelDatabaseName { get; set; }
  public string name { get; set; }
  public string connectionType { get; set; }
}
