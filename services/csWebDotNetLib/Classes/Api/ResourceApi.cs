using System;
using System.IO;
using System.Collections.Generic;
using RestSharp;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace IO.Swagger.Api
{
    
    public interface IResourceApi
    {
        
        /// <summary>
        /// Add resource Adds a single resource
        /// </summary>
        /// <param name="body">Resource object that needs to be added to the datastorage</param>
        /// <returns></returns>
        void AddResource (Resource body);
  
        /// <summary>
        /// Add resource Adds a single resource
        /// </summary>
        /// <param name="body">Resource object that needs to be added to the datastorage</param>
        /// <returns></returns>
        System.Threading.Tasks.Task AddResourceAsync (Resource body);
        
        /// <summary>
        /// Find resource by ID Returns a single resource
        /// </summary>
        /// <param name="resourceId">ID of the resource to be returned</param>
        /// <returns>Resource</returns>
        Resource GetResource (string resourceId);
  
        /// <summary>
        /// Find resource by ID Returns a single resource
        /// </summary>
        /// <param name="resourceId">ID of the resource to be returned</param>
        /// <returns>Resource</returns>
        System.Threading.Tasks.Task<Resource> GetResourceAsync (string resourceId);
        
    }
  
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public class ResourceApi : IResourceApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceApi"/> class.
        /// </summary>
        /// <param name="apiClient"> an instance of ApiClient (optional)</param>
        /// <returns></returns>
        public ResourceApi(ApiClient apiClient = null)
        {
            if (apiClient == null) // use the default one in Configuration
                this.ApiClient = Configuration.DefaultApiClient; 
            else
                this.ApiClient = apiClient;
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceApi"/> class.
        /// </summary>
        /// <returns></returns>
        public ResourceApi(String basePath)
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
        /// Add resource Adds a single resource
        /// </summary>
        /// <param name="body">Resource object that needs to be added to the datastorage</param> 
        /// <returns></returns>            
        public void AddResource (Resource body)
        {
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddResource");
            
    
            var path = "/resources";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddResource: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AddResource: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Add resource Adds a single resource
        /// </summary>
        /// <param name="body">Resource object that needs to be added to the datastorage</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task AddResourceAsync (Resource body)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddResource");
            
    
            var path = "/resources";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddResource: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Find resource by ID Returns a single resource
        /// </summary>
        /// <param name="resourceId">ID of the resource to be returned</param> 
        /// <returns>Resource</returns>            
        public Resource GetResource (string resourceId)
        {
            
            // verify the required parameter 'resourceId' is set
            if (resourceId == null) throw new ApiException(400, "Missing required parameter 'resourceId' when calling GetResource");
            
    
            var path = "/resources/{resourceId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (resourceId != null) pathParams.Add("resourceId", ApiClient.ParameterToString(resourceId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetResource: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling GetResource: " + response.ErrorMessage, response.ErrorMessage);
    
            return (Resource) ApiClient.Deserialize(response.Content, typeof(Resource), response.Headers);
        }
    
        /// <summary>
        /// Find resource by ID Returns a single resource
        /// </summary>
        /// <param name="resourceId">ID of the resource to be returned</param>
        /// <returns>Resource</returns>
        public async System.Threading.Tasks.Task<Resource> GetResourceAsync (string resourceId)
        {
            // verify the required parameter 'resourceId' is set
            if (resourceId == null) throw new ApiException(400, "Missing required parameter 'resourceId' when calling GetResource");
            
    
            var path = "/resources/{resourceId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (resourceId != null) pathParams.Add("resourceId", ApiClient.ParameterToString(resourceId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetResource: " + response.Content, response.Content);

            return (Resource) ApiClient.Deserialize(response.Content, typeof(Resource), response.Headers);
        }
        
    }
    
}
