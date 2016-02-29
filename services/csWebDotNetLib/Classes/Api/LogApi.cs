using System;
using System.IO;
using System.Collections.Generic;
using RestSharp;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace IO.Swagger.Api
{
    
    public interface ILogApi
    {
        
        /// <summary>
        /// Find the log file associated with a single feature Returns all logs associated with a feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param>
        /// <param name="layerId">ID of the layer in which the feature is located</param>
        /// <returns>Log</returns>
        Log GetLog (string featureId, string layerId);
  
        /// <summary>
        /// Find the log file associated with a single feature Returns all logs associated with a feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param>
        /// <param name="layerId">ID of the layer in which the feature is located</param>
        /// <returns>Log</returns>
        System.Threading.Tasks.Task<Log> GetLogAsync (string featureId, string layerId);
        
        /// <summary>
        /// Adds a log to a feature by updating it with a log 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <param name="featureId">ID of the feature to update with the log</param>
        /// <returns></returns>
        void AddLog (Log body, string layerId, string featureId);
  
        /// <summary>
        /// Adds a log to a feature by updating it with a log 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <param name="featureId">ID of the feature to update with the log</param>
        /// <returns></returns>
        System.Threading.Tasks.Task AddLogAsync (Log body, string layerId, string featureId);
        
        /// <summary>
        /// Delete a log by supplying timestamp, value, and IDs Deletes a log by supplying the API with values for the TimeStamp, property name, and layer/feature identifiers
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param>
        /// <param name="layerId">ID of the layer where the feature is located</param>
        /// <returns></returns>
        void DeleteLog (string featureId, string layerId);
  
        /// <summary>
        /// Delete a log by supplying timestamp, value, and IDs Deletes a log by supplying the API with values for the TimeStamp, property name, and layer/feature identifiers
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param>
        /// <param name="layerId">ID of the layer where the feature is located</param>
        /// <returns></returns>
        System.Threading.Tasks.Task DeleteLogAsync (string featureId, string layerId);
        
    }
  
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public class LogApi : ILogApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogApi"/> class.
        /// </summary>
        /// <param name="apiClient"> an instance of ApiClient (optional)</param>
        /// <returns></returns>
        public LogApi(ApiClient apiClient = null)
        {
            if (apiClient == null) // use the default one in Configuration
                this.ApiClient = Configuration.DefaultApiClient; 
            else
                this.ApiClient = apiClient;
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="LogApi"/> class.
        /// </summary>
        /// <returns></returns>
        public LogApi(String basePath)
        {
            this.ApiClient = new ApiClient(basePath);
        }
    
        /// <summary>
        /// Sets the base path of the API client.
        /// </summary>
        /// <param name="basePath">The base path</param>
        /// <value>The base path</value>
        public void SetBasePath(String basePath)
        {
            this.ApiClient.BasePath = basePath;
        }
    
        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public String GetBasePath()
        {
            return this.ApiClient.BasePath;
        }
    
        /// <summary>
        /// Gets or sets the API client.
        /// </summary>
        /// <value>An instance of the ApiClient</param>
        public ApiClient ApiClient {get; set;}
    
        
        /// <summary>
        /// Find the log file associated with a single feature Returns all logs associated with a feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param> 
        /// <param name="layerId">ID of the layer in which the feature is located</param> 
        /// <returns>Log</returns>            
        public Log GetLog (string featureId, string layerId)
        {
            
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling GetLog");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling GetLog");
            
    
            var path = "/layers/{layerId}/{featureId}/log";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (featureId != null) pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetLog: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling GetLog: " + response.ErrorMessage, response.ErrorMessage);
    
            return (Log) ApiClient.Deserialize(response.Content, typeof(Log), response.Headers);
        }
    
        /// <summary>
        /// Find the log file associated with a single feature Returns all logs associated with a feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param>
        /// <param name="layerId">ID of the layer in which the feature is located</param>
        /// <returns>Log</returns>
        public async System.Threading.Tasks.Task<Log> GetLogAsync (string featureId, string layerId)
        {
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling GetLog");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling GetLog");
            
    
            var path = "/layers/{layerId}/{featureId}/log";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (featureId != null) pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetLog: " + response.Content, response.Content);

            return (Log) ApiClient.Deserialize(response.Content, typeof(Log), response.Headers);
        }
        
        /// <summary>
        /// Adds a log to a feature by updating it with a log 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param> 
        /// <param name="layerId">ID or Name of the Layer to update</param> 
        /// <param name="featureId">ID of the feature to update with the log</param> 
        /// <returns></returns>            
        public void AddLog (Log body, string layerId, string featureId)
        {
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddLog");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling AddLog");
            
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling AddLog");
            
    
            var path = "/layers/{layerId}/{featureId}/log";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            if (featureId != null) pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddLog: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AddLog: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Adds a log to a feature by updating it with a log 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <param name="featureId">ID of the feature to update with the log</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task AddLogAsync (Log body, string layerId, string featureId)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddLog");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling AddLog");
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling AddLog");
            
    
            var path = "/layers/{layerId}/{featureId}/log";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            if (featureId != null) pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddLog: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Delete a log by supplying timestamp, value, and IDs Deletes a log by supplying the API with values for the TimeStamp, property name, and layer/feature identifiers
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param> 
        /// <param name="layerId">ID of the layer where the feature is located</param> 
        /// <returns></returns>            
        public void DeleteLog (string featureId, string layerId)
        {
            
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling DeleteLog");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling DeleteLog");
            
    
            var path = "/layers/{layerId}/{featureId}/log";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (featureId != null) pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteLog: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteLog: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Delete a log by supplying timestamp, value, and IDs Deletes a log by supplying the API with values for the TimeStamp, property name, and layer/feature identifiers
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param>
        /// <param name="layerId">ID of the layer where the feature is located</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task DeleteLogAsync (string featureId, string layerId)
        {
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling DeleteLog");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling DeleteLog");
            
    
            var path = "/layers/{layerId}/{featureId}/log";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (featureId != null) pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteLog: " + response.Content, response.Content);

            
            return;
        }
        
    }
    
}
