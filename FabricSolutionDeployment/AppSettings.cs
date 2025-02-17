﻿
public class AppSettings {

  public const string FabricRestApiBaseUrl = "https://api.fabric.microsoft.com/v1";
  public const string PowerBiRestApiBaseUrl = "https://api.powerbi.com";
  public const string OneLakeBaseUrl = "https://onelake.dfs.fabric.microsoft.com";

  // TODO: configure capacity Id for Fabric-enabled capacity
  public const string FabricCapacityId = "00000000-0000-0000-0000-000000000000";

 // TODO: configure location of Azure storage container
  public const string AzureStorageAccountName = "{YOUR_AZURE_STORAGE_ACCOUNT_NAME}"; // e.g. "fabricdevcamp"
  public const string AzureStorageContainer = "{YOUR_AZURE_CONTAINER_NAME}";         // e.g  "sampledata"
  public const string AzureStorageContainerPath = "{YOUR_AZURE_CONTAINER_PATH}";     // e.g. "/ProductSales/Dev"

  // no need to update these two setting
  public const string AzureStorageServer = $"https://{AzureStorageAccountName}.dfs.core.windows.net";
  public const string AzureStoragePath = AzureStorageContainer + AzureStorageContainerPath;

  // TODO: configure account key or SAS token for Azure storage container
  public const string AzureStorageAccountKey = "{YOUR_AZURE_STORAGE_ACCOUNT_KEY}";

 // TODO: configure Azure DevOps organization
  public const string AzureDevOpsOrganizationName = "{YOUR_AZURE_DEVOPS_ORGANIZATION_NAME}";
  public const string AzureDevOpsApiBaseUrl = $"https://dev.azure.com/{AzureDevOpsOrganizationName}";
  // TODO: configure authentication mode
  public static AppAuthenticationMode AuthenticationMode = AppAuthenticationMode.UserAuthWithAzurePowershell;

  // OPTIONAL TODO: configure Entra Id application for user auth
  public const string UserAuthClientId = "00000000-0000-0000-0000-000000000000";
  public const string UserAuthRedirectUri = "http://localhost";

  // OPTIONAL TODO: configure Entra Id application for service principal auth
  public const string ServicePrincipalAuthTenantId = "00000000-0000-0000-0000-000000000000";
  public const string ServicePrincipalAuthClientId = "00000000-0000-0000-0000-000000000000";
  public const string ServicePrincipalAuthClientSecret = "YOUR_CLIENT_SECRET";
  public const string ServicePrincipalObjectId = "00000000-0000-0000-0000-000000000000";

  // OPTIONAL TODO: configure object id of Entra Id user account of user running demo
  public const string AdminUserId = "00000000-0000-0000-0000-000000000000";

  // paths to folders inside this project to read and write files
  public const string LocalExportFolder = @"..\..\..\ItemDefinitionExports\";
  public const string LocalTemplateFilesRoot = @"..\..\..\ItemDefinitions\ItemDefinitionTemplateFiles\";
  public const string LocalItemTemplatesFolder = @"..\..\..\ItemDefinitions\ItemDefinitionTemplateFolders\";
  public const string LocalPackagedSolutionFolder = @"..\..\..\ItemDefinitions\PackagedSolutionFolders\";

}