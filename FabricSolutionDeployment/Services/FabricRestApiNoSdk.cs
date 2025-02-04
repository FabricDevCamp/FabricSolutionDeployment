using Microsoft.Fabric.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Fabric.Api.Admin.Models;
using Newtonsoft.Json.Linq;

namespace NoSdk {

  #region "Serialization Classes"

  public class FabricOperation {
    public string status { get; set; }
    public DateTime createdTimeUtc { get; set; }
    public DateTime lastUpdatedTimeUtc { get; set; }
    public object percentComplete { get; set; }
    public FabricErrorResponse error { get; set; }
  }

  public class FabricErrorResponse {
    public string errorCode { get; set; }
    public string message { get; set; }
    public string requestId { get; set; }
    public object moreDetails { get; set; }
    public object relatedResource { get; set; }

  }

  public class FabricConnectionListResponse {
    public List<FabricConnection> value { get; set; }
    public string continuationToken { get; set; }
    public string continuationUri { get; set; }
  }

  public class FabricConnection {
    public string id { get; set; }
    public string displayName { get; set; }
    public string gatewayId { get; set; }
    public string connectivityType { get; set; }
    public string privacyLevel { get; set; }
  }

  #endregion

  public class FabricRestApiNoSdk {

    #region "Utility methods for executing HTTP requests"

    private static string AccessToken = EntraIdTokenManager.GetFabricAccessToken();

    private static string ExecuteGetRequest(string endpoint) {

      string restUri = AppSettings.FabricRestApiBaseUrl + endpoint;

      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
      client.DefaultRequestHeaders.Add("Accept", "application/json");

      HttpResponseMessage response = client.GetAsync(restUri).Result;

      if (response.IsSuccessStatusCode) {
        return response.Content.ReadAsStringAsync().Result;
      }
      else {
        throw new ApplicationException("ERROR executing HTTP GET request " + response.StatusCode);
      }
    }

    private static string ExecutePostRequest(string endpoint, string postBody = "") {

      string restUri = AppSettings.FabricRestApiBaseUrl + endpoint;

      HttpContent body = new StringContent(postBody);
      body.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Add("Accept", "application/json");
      client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);

      HttpResponseMessage response = client.PostAsync(restUri, body).Result;

      // switch to handle responses with different status codes
      switch (response.StatusCode) {

        // handle case when sync call succeeds with OK (200) or CREATED (201)
        case HttpStatusCode.OK:
        case HttpStatusCode.Created:
          Console.WriteLine();
          // return result to caller
          return response.Content.ReadAsStringAsync().Result;

        // handle case where call started async operation with ACCEPTED (202)
        case HttpStatusCode.Accepted:
          Console.Write(".");

          // get headers in response with URL for operation status and retry interval
          string operationUrl = response.Headers.GetValues("Location").First();
          //int retryAfter = int.Parse(response.Headers.GetValues("Retry-After").First());
          int retryAfter = 10; // hard-coded during testing - use what's above instead 

          // execute GET request with operation url until it returns OK (200)
          string jsonOperation;
          FabricOperation operation;

          do {
            Thread.Sleep(retryAfter * 1000);  // wait for retry interval 
            Console.Write(".");
            response = client.GetAsync(operationUrl).Result;
            jsonOperation = response.Content.ReadAsStringAsync().Result;
            operation = JsonSerializer.Deserialize<FabricOperation>(jsonOperation);

          } while (operation.status != "Succeeded" &&
                   operation.status != "Failed" &&
                   operation.status != "Completed");

          if (response.StatusCode == HttpStatusCode.OK) {
            // handle 2 cases where operation completed successfully
            if (!response.Headers.Contains("Location")) {
              // (1) handle case where operation has no result
              Console.WriteLine();
              return string.Empty;
            }
            else {
              Console.Write(".");
              // (2) handle case where operation has result by retrieving it
              response = client.GetAsync(operationUrl + "/result").Result;
              Console.WriteLine();
              return response.Content.ReadAsStringAsync().Result;
            }
          }
          else {
            // handle case where operation experienced error
            jsonOperation = response.Content.ReadAsStringAsync().Result;
            operation = JsonSerializer.Deserialize<FabricOperation>(jsonOperation);
            string errorMessage = operation.error.errorCode + " - " + operation.error.message;
            throw new ApplicationException(errorMessage);
          }

        default: // handle exeception where HTTP status code indicates failure
          Console.WriteLine();
          throw new ApplicationException("ERROR executing HTTP POST request " + response.StatusCode);
      }

    }

    private static string ExecutePatchRequest(string endpoint, string postBody = "") {

      string restUri = AppSettings.FabricRestApiBaseUrl + endpoint;

      HttpContent body = new StringContent(postBody);
      body.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Add("Accept", "application/json");
      client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);

      HttpResponseMessage response = client.PatchAsync(restUri, body).Result;

      if (response.IsSuccessStatusCode) {
        return response.Content.ReadAsStringAsync().Result;
      }
      else {
        throw new ApplicationException("ERROR executing HTTP PATCH request " + response.StatusCode);
      }
    }

    private static string ExecuteDeleteRequest(string endpoint) {
      string restUri = AppSettings.FabricRestApiBaseUrl + endpoint;

      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Add("Accept", "application/json");
      client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
      HttpResponseMessage response = client.DeleteAsync(restUri).Result;

      if (response.IsSuccessStatusCode) {
        return response.Content.ReadAsStringAsync().Result;
      }
      else {
        throw new ApplicationException("ERROR executing HTTP DELETE request " + response.StatusCode);
      }
    }

    private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    #endregion

    // connections

    public static List<FabricConnection> GetConnections() {
      string jsonResponse = ExecuteGetRequest("/connections");
      return JsonSerializer.Deserialize<FabricConnectionListResponse>(jsonResponse).value;
    }

    public static FabricConnection GetConnection(string ConnectionId) {
      string jsonResponse = ExecuteGetRequest($"/connections/{ConnectionId}");
      return JsonSerializer.Deserialize<FabricConnection>(jsonResponse);
    }

    public static void DeleteConnection(string ConnectionId) {

      try {
        ExecuteDeleteRequest("/connections/" + ConnectionId);
      }
      catch {
        // do nothing - this is logic to handle bug with delete connection returning error
      }
    }

    public static void DeleteConnectionIfItExists(string ConnectionName) {

      var connections = GetConnections();

      foreach (var connection in connections) {
        if (connection.displayName == ConnectionName) {
          Console.WriteLine("Deleting existing connection");
          ExecuteDeleteRequest("/connections/" + connection.id);
        }
      }

    }

  }
}