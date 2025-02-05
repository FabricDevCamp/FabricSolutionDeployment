# Getting Started

> This setup guide is designed to help you get up and running with the
**FabricSolutionDeployment** sample application.

Once you download the **FabricSolutionDeployment** project, you can open and test it
using any version of Visual Studio 2022 including the free community
version. While this project hasn't been tested using VS Code, you should be able to
use that developer experience instead of Visual Studio as long as you have the .NET/C# extensions
installed.

<img src="./images/GettingStarted/media/image1.png" style="width:25%" />

When you open the project, start by examining **AppSettings.cs**. This
is a settings file with configuration data you need to modify with configuration data for your
Fabric environment. The following screenshot shows you what this file looks like when
you first open it.

<img src="./images/GettingStarted/media/image2.png" style="width:96%" />

You can see some configuration values are initially set to **00000000-0000-0000-0000-000000000000** which 
are empty GUID values. To make various demos run correctly, you will be required to replace these default values with Ids that are unique to your development environment. 

However, you don't need to modify anything in **AppSettings.cs** before you run this
application for the first time. When you start up and run the project
for the first time, you will be prompted to login. Log in using a user
account with access to your Fabric development environment.

<img src="./images/GettingStarted/media/image3.png" style="width:25%" />

>When you login in for the first time, Entra Id might prompt you with a **Permissions request** consent dialog asking you to consent to permissions requested by the application. Click **Accept**
to grant the permission request and continue running the application.

Once you login, the application will execute two Fabric REST API calls to retrieve 
information about the workspaces and capacities in the current Entra Id tenant to which you have access.
The application then displays these workspaces and capacities in the console window as shown in the
following screenshot. 

<img src="./images/GettingStarted/media/image4.png" style="width:70%" />

> The **Capacities List** shows you what capacities that your
user account has permissions to access in your development environment.

Here is where you need to determine which capacity to configure for use
with the application. You need to select a Fabric-enabled capacity and
add its capacity ID to **AppSettings.cs**. This is required so the
application can assign the workspaces it creates to this capacity.

For testing you can use any Fabric capacity created from an **F SKU** or a **P
SKU** or you can also use a Fabric trial capacity. The screenshot above
shows a Fabric trial shown by the code **[FT1]**. If you
do not see a Fabric-enabled capacity in the capacities list, you must
acquire one before continuing with the setup of this application.

> If you are using a Fabric trial account, this makes it more difficult (but not impossible) to test
deployment workflows using a service principal. That's because you cannot grant trial capacity 
permissions to a service principal. You can only execute the Fabric the REST API call to assign a workspace 
to a Fabric trial capacity using a user identity.

Once you determine which capacity you want to use, copy its capacity Id value
into the **FabricCapcityId** constant value in **AppSettings.cs**.

<img src="./images/GettingStarted/media/image5.png" style="width:80%" />

> Now you're ready to start running the **FabricSolutionDeployment** application
under the identity of your user account. This will allow you to test out and experiment 
with CI/CD workflows which have been designed to deploy and update Fabric solutions.

Open the source file named **Program.cs** and locate the function named **Setup_ViewWorkspacesAndCapacities** .  

<img src="./images/GettingStarted/media/image6.png" style="width:80%" />

Comment out the line that calls **Setup_ViewWorkspacesAndCapacities** and uncomment the next line
to call the function named **Demo01_DeploySolutionToWorkspace**. The next time you run the application it should execute the code for **demo 1** which creates and populates a new workspace named **Contoso**. 

<img src="./images/GettingStarted/media/image7.png" style="width:70%" />

> You've now configured what's required to run **demo 1** through **demo 4**. 
It is simply a matter of commenting out one functions and uncommenting the next.
To run through the first four demos, use the instructions in 
**[Automating Fabric Solution Deployment](https://github.com/FabricDevCamp/FabricSolutionDeployment/blob/main/docs/Automating%20Fabric%20Solution%20Deployment.md)**.

If you want to run **demo 5** through **demo 7**, you will need to configure additional support in 
**AppSettings.cs** for an Azure DevOps organiztion. For this, you will need access
to an Azure DevOps organization in the same Entra Id tenant as the development environment where you are creating Fabric workspaces. 

Once you determined which Azure DevOps organization to use, you can configure it in **AppSettings.cs** by updating the value for a constant named **AzureDevOpsOrganizationName** which is shown in the following screenshot.

<img src="./images/GettingStarted/media/image9.png" style="width:85%" />

For example, if you Azure DevOps organization is named **FabricDevCamp**, you should update the configuration value for **AzureDevOpsOrganizationName** in **AppSettings.cs** 
to look like this.  

<img src="./images/GettingStarted/media/image10.png" style="width:85%" />

Note that this sample application contains code that uses the Azure REST API to create new projects
in an Azure DevOps organization. It also has code to push files to an Azure DevOps repository and commit the changes as well as code to retrieve files from a repository. For anyone interested in seeing how this code has been written, you can examine the source code for the class named [**AdoProjectManager**](https://github.com/FabricDevCamp/FabricSolutionDeployment/blob/main/FabricSolutionDeployment/Services/AdoProjectManager.cs) class.

<img src="./images/GettingStarted/media/image11.png" style="width:99%" />

If you don’t already have access to an Azure DevOps organization, you
can set up support for this pretty quickly without having to purchase anything. Once
you log into your Fabric user account, you should be able to activate a
free Azure DevOps account and create an Azure DevOps organization by
following to this link.

- **<https://dev.azure.com/>**  

> Note that Fabric supports using repositories for GIT integration using
either Azure DevOps or GitHub. However, this project currently only has
support for Azure DevOps. There are plans add support for GitHub in a
future update to this project.

## Configuring Authentication Mode

The **FabricSolutionDeployment** application supports three possible 
configure settings for the application's **authentication mode**.
 If you leave the configuration with the default setting
in **AppSettings.cs** (shown below), things should just work out-of-the-box. 
There is no need to create an Entra Id application before you run 
the he **FabricSolutionDeployment** application in the Visual Studio
debugger. 

The reason that the default auuthentication mode setting *just works* 
is because the application is configure to use a pre-installed 
Entra Id application known as **Azure PowerShell** application. 
This application is automatically available in every M365
tenant and you can use it to acquire user access tokens for the Fabric REST
APIs.

The default for the **AuthenticationMode** setting in **AppSettings.cs**
is **UserAuthWithAzurePowershell**.

<img src="./images/GettingStarted/media/image12.png" style="width:90%" />

>As you can see, the default authentication mode setting has been designed to get
you started quickly because it eliminates the need to create a custom Entra Id application. 

If you want to test the **FabricSolutionDeployment** application using a custom 
application you have created in Entra Id , you can configure **AuthenticationMode** to 
a setting of either **UserAuth** and **ServicePrincipalAuth**. 

<img src="./images/GettingStarted/media/image13.png" style="width:40%" />

> The next two sections explain how to create a custom Entra Id applicationsas and how to configure them in the **FabricSolutionDeployment** application to test out deployment workflows that run as either a user or
as a service principal.

### Configuring User Authentication with a Custom Entra Id Application

If you want to use **UserAuth** mode, you must create a Entra Id
application in the same Entra Id tenant where you are creating workspaces.
When you create a new Entra Id application in the
Entra Id portal, you should configure the **Redirect URI** as **Public
client/native** and set the URI value to **http<span>://</span>localhost** as shown
in the following screenshot.

<img src="./images/GettingStarted/media/image14.png" style="width:70%" />

After clicking **Register** to create the new Entra Id application, you
should be able to copy the application’s client Id to the clipboard so
you can paste it into **AppSettings.cs** as shown in the following
screenshot.

<img src="./images/GettingStarted/media/image15.png" style="width:62%" />

After you have created the Entra Id application for user authentication,
you need to make two changes to **AppSettings.cs**. First you need paste
the client Id of the Entra Id application into the value for the
constant named **UserAuthClientId** as shown in the following
screenshot. Second, you must update the value of the constant named
**AuthenticationMode** to **UserAuth**.

<img src="./images/GettingStarted/media/image16.png" style="width:70%" />

When you start the application for the first time after configuring
**UserAuth** mode, you will be prompted by Entra Id to sign in. Once you
have signed in, Entra Id will then prompt you with the **Permissions
request** consent dialog asking you to consent to the delegated
permissions that this application has requested. You should click
**Accept** to continue.

<img src="./images/GettingStarted/media/image17.png" style="width:30%" />

> Now the application will authenticate you with the custom Entra Id application and run under 
the identity of your Entra Id user account.

### Configuring Service Principal Authentication with a Custom Entra Id Application

If you want to run the **FabricSolutionDeployment** application as a service
principal, you must create an Entra Id application that is configured
as a confidential application with a client secret. The service principal must also be configured
with a tenant-level **Service principals can use Fabric APIs** setting in the Fabric Admin portal.
 
 >If the service principal is not configured properly, all Fabric REST API calls will fail with a ***401 Unauthorized*** error.
 
 Here are steps to create the app.

<img src="./images/GettingStarted/media/image17B.png" style="width:80%" />

Grb both client Id and enant ID.

<img src="./images/GettingStarted/media/image17C.png" style="width:60%" />

Create new client secret.

<img src="./images/GettingStarted/media/image17D.png" style="width:70%" />

Create new client secret.

<img src="./images/GettingStarted/media/image17E.png" style="width:70%" />

Create new client secret.

<img src="./images/GettingStarted/media/image17F.png" style="width:70%" />



Create new client secret.

Get value of client secret so you can paste it into **AppSettings.cs**

Once you have created the Entra Id application, you need to copy its
tenant id and client Id as well as the client secret so you can add them
to **AppSettings.cs**. Once you get the configuration information for
the service principal, you must add its into **AppSettings.cs**.

<img src="./images/GettingStarted/media/image18.png" style="width:80%" />

Create new client secret.

<img src="./images/GettingStarted/media/image17G.png" style="width:70%" />

One part that is kind of tricking is getting
the **ServicePrincipalObjectId **value for the service principal. You
need that configuration value so you can demo workflows can add the
service principal as a workspace member. The easiest way I know to get
this value is to go to the Entra Id application **Overview** page and
click the **Managed application in local directly** link.

<img src="./images/GettingStarted/media/image19.png" style="width:80%" />

When you click the **Managed application in local directly** linkyou
will navigate to page from which you can copy the **Object ID** value
which is the service principal object Id. 

<img src="./images/GettingStarted/media/image20.png" style="width:50%" />

Now you have 4 settings which completes configuring the service
principal.

<img src="./images/GettingStarted/media/image21.png" style="width:80%" />

OK, you’re almost done. There is one more step which is to set
the **AdminUserId** value in **AppSettings.cs**. This configuration
value is important when you start running the demo workflows as a service principal.

Let's take a step back and explain the issue that is addressed with the **AdminUserId**
configuration value. If you create a workspace as a service principal,
that service principal will be the only identity that can access that
workspace. That means you will not be able to inspect that workspace in
the Fabric UI under the identity of your user account. Therefore, the
sample application has been designed to add any user account (*which
should be your user account*) as a workspace admin. After a service
principal creates a workspace, you user account will be given full
access to that workspace so you can inspect it in the Fabric UI and
continue to experiment.

If you look at the bottom of the following screenshot, you will see a
constant named **AdminUserId**. You need to configure this constant with
the object id associated with your Entra Id user account.

<img src="./images/GettingStarted/media/image22.png" style="width:90%" />

There are several different ways you can get the Object Id for your Entra Id
User account. The easiest way is to go to the Entra Id admin center.
Next, click **Users** in the left nav. If you click on your user
account, you should navigate to a page from which you can copy
the **Object Id** associated with your user account.

<img src="./images/GettingStarted/media/image23.png" style="width:60%" />

Use that **Object ID** to update the **AdminUserId** constant
in **AppSettings.cs**.

<img src="./images/GettingStarted/media/image24.png" style="width:70%" />

blah

<img src="./images/GettingStarted/media/image25.png" style="width:70%" />

blah

<img src="./images/GettingStarted/media/image26.png" style="width:70%" />

blah

<img src="./images/GettingStarted/media/image27.png" style="width:70%" />

blah
