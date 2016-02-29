using System;
using System.Collections.Generic;
using RestSharp;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace IO.Swagger.Api
{
    public interface IFeatureApi
    {
        /// <summary>
        /// Adds a feature 
        /// </summary>
        /// <param name="layerId">ID of layer to add the feature to</param>
        /// <param name="body">JSON that will be used to insert the feature</param>
        /// <returns>ApiResponse</returns>
        ApiResponse AddFeature (string layerId, Feature body);
  
        /// <summary>
        /// Adds a feature 
        /// </summary>
        /// <param name="layerId">ID of layer to add the feature to</param>
        /// <param name="body">JSON that will be used to insert the feature</param>
        /// <returns>ApiResponse</returns>
        System.Threading.Tasks.Task<ApiResponse> AddFeatureAsync (string layerId, Feature body);
        
        /// <summary>
        /// Find feature by ID Returns a single feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param>
        /// <param name="layerId">ID of the layer in which the feature is located</param>
        /// <returns>Feature</returns>
        Feature GetFeature (string featureId, string layerId);
  
        /// <summary>
        /// Find feature by ID Returns a single feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param>
        /// <param name="layerId">ID of the layer in which the feature is located</param>
        /// <returns>Feature</returns>
        System.Threading.Tasks.Task<Feature> GetFeatureAsync (string featureId, string layerId);
        
        /// <summary>
        /// Update a single pre-existing feature 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <param name="featureId">ID of the feature to update</param>
        /// <returns></returns>
        void UpdateFeature (Feature body, string layerId, string featureId);
  
        /// <summary>
        /// Update a single pre-existing feature 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <param name="featureId">ID of the feature to update</param>
        /// <returns></returns>
        System.Threading.Tasks.Task UpdateFeatureAsync (Feature body, string layerId, string featureId);
        
        /// <summary>
        /// Delete a feature by ID Deletes an entire layer
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param>
        /// <param name="layerId">ID of the layer where the feature is located</param>
        /// <returns></returns>
        void DeleteFeature (string featureId, string layerId);
  
        /// <summary>
        /// Delete a feature by ID Deletes an entire layer
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param>
        /// <param name="layerId">ID of the layer where the feature is located</param>
        /// <returns></returns>
        System.Threading.Tasks.Task DeleteFeatureAsync (string featureId, string layerId);
        
    }
  
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public class FeatureApi : IFeatureApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureApi"/> class.
        /// </summary>
        /// <param name="apiClient"> an instance of ApiClient (optional)</param>
        /// <returns></returns>
        public FeatureApi(ApiClient apiClient = null)
        {
            ApiClient = apiClient ?? Configuration.DefaultApiClient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureApi"/> class.
        /// </summary>
        /// <returns></returns>
        public FeatureApi(string basePath)
        {
            ApiClient = new ApiClient(basePath);
        }
    
        /// <summary>
        /// Sets the base path of the API client.
        /// </summary>
        /// <param name="basePath">The base path</param>
        /// <value>The base path</value>
        public void SetBasePath(string basePath)
        {
            ApiClient.BasePath = basePath;
        }
    
        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public string GetBasePath()
        {
            return ApiClient.BasePath;
        }

        /// <summary>
        /// Gets or sets the API client.
        /// </summary>
        /// <param>An instance of the ApiClient</param>
        public ApiClient ApiClient {get; set;}
    
        /// <summary>
        /// Adds a feature 
        /// </summary>
        /// <param name="layerId">ID of layer to add the feature to</param> 
        /// <param name="body">JSON that will be used to insert the feature</param> 
        /// <returns>ApiResponse</returns>            
        public ApiResponse AddFeature (string layerId, Feature body)
        {
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling AddFeature");
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddFeature");
    
            const string path = "/layers/{layerId}/feature";
    
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();

            pathParams.Add("format", "json");
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            var postBody = ApiClient.Serialize(body);
            // authentication setting, if any
            string[] authSettings = { };
    
            // make the HTTP request
            var response = (IRestResponse) ApiClient.CallApi(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddFeature: " + response.Content, response.Content);
            if (response.StatusCode == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AddFeature: " + response.ErrorMessage, response.ErrorMessage);

            //return new ApiResponse() {Code = 200, Message = "OK"};
            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
    
        /// <summary>
        /// Adds a feature 
        /// </summary>
        /// <param name="layerId">ID of layer to add the feature to</param>
        /// <param name="body">JSON that will be used to insert the feature</param>
        /// <returns>ApiResponse</returns>
        public async System.Threading.Tasks.Task<ApiResponse> AddFeatureAsync (string layerId, Feature body)
        {
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling AddFeature");
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddFeature");
            
    
            var path = "/layers/{layerId}/feature";
    
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();

            pathParams.Add("format", "json");
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            var postBody = ApiClient.Serialize(body);
    
            // authentication setting, if any
            string[] authSettings = {  };
    
            // make the HTTP request
            var response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddFeature: " + response.Content, response.Content);

            //return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
            return new ApiResponse() {Code = 200, Message = response.Content};
        }
        
        /// <summary>
        /// Find feature by ID Returns a single feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param> 
        /// <param name="layerId">ID of the layer in which the feature is located</param> 
        /// <returns>Feature</returns>            
        public Feature GetFeature (string featureId, string layerId)
        {
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling GetFeature");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling GetFeature");
    
            const string path = "/layers/{layerId}/feature/{featureId}";
    
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();
            string postBody = null;

            pathParams.Add("format", "json");
            pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            // authentication setting, if any
            string[] authSettings = {  };
    
            // make the HTTP request
            var response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if ((int)response.StatusCode >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetFeature: " + response.Content, response.Content);
            if (response.StatusCode == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling GetFeature: " + response.ErrorMessage, response.ErrorMessage);

            return (Feature) ApiClient.Deserialize(response.Content, typeof(Feature), response.Headers);
        }
    
        /// <summary>
        /// Find feature by ID Returns a single feature
        /// </summary>
        /// <param name="featureId">ID of the feature to be returned</param>
        /// <param name="layerId">ID of the layer in which the feature is located</param>
        /// <returns>Feature</returns>
        public async System.Threading.Tasks.Task<Feature> GetFeatureAsync (string featureId, string layerId)
        {
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling GetFeature");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling GetFeature");
            
    
            var path = "/layers/{layerId}/feature/{featureId}";
    
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();
            string postBody = null;
    
            pathParams.Add("format", "json");
            pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
    
            // authentication setting, if any
            string[] authSettings = {  };
    
            // make the HTTP request
            var response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if ((int)response.StatusCode >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetFeature: " + response.Content, response.Content);

            return (Feature) ApiClient.Deserialize(response.Content, typeof(Feature), response.Headers);
        }
        
        /// <summary>
        /// Update a single pre-existing feature 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param> 
        /// <param name="layerId">ID or Name of the Layer to update</param> 
        /// <param name="featureId">ID of the feature to update</param> 
        /// <returns></returns>            
        public void UpdateFeature (Feature body, string layerId, string featureId)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling UpdateFeature");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling UpdateFeature");
            
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling UpdateFeature");
    
            const string path = "/layers/{layerId}/feature/{featureId}";
    
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();

            pathParams.Add("format", "json");
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            
            var postBody = ApiClient.Serialize(body);
            
            // authentication setting, if any
            string[] authSettings = {  };
    
            // make the HTTP request
            var response = (IRestResponse) ApiClient.CallApi(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if ((int)response.StatusCode >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateFeature: " + response.Content, response.Content);
            if (response.StatusCode == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateFeature: " + response.ErrorMessage, response.ErrorMessage);
        }
    
        /// <summary>
        /// Update a single pre-existing feature 
        /// </summary>
        /// <param name="body">JSON that will be used to update the feature</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <param name="featureId">ID of the feature to update</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task UpdateFeatureAsync (Feature body, string layerId, string featureId)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling UpdateFeature");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling UpdateFeature");
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling UpdateFeature");
    
            const string path = "/layers/{layerId}/feature/{featureId}";
    
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();

            pathParams.Add("format", "json");
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            
            var postBody = ApiClient.Serialize(body);
    
            // authentication setting, if any
            string[] authSettings = {  };
    
            // make the HTTP request
            var response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if ((int)response.StatusCode >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateFeature: " + response.Content, response.Content);
        }
        
        /// <summary>
        /// Delete a feature by ID Deletes an entire layer
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param> 
        /// <param name="layerId">ID of the layer where the feature is located</param> 
        /// <returns></returns>            
        public void DeleteFeature (string featureId, string layerId)
        {
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling DeleteFeature");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling DeleteFeature");
            
            const string path = "/layers/{layerId}/feature/{featureId}";
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();
            string postBody = null;

            pathParams.Add("format", "json");
            pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            // authentication setting, if any
            string[] authSettings = {  };
    
            // make the HTTP request
            var response = (IRestResponse) ApiClient.CallApi(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if ((int)response.StatusCode >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteFeature: " + response.Content, response.Content);
            if (response.StatusCode == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteFeature: " + response.ErrorMessage, response.ErrorMessage);
        }
    
        /// <summary>
        /// Delete a feature by ID Deletes an entire layer
        /// </summary>
        /// <param name="featureId">ID of feature to be deleted</param>
        /// <param name="layerId">ID of the layer where the feature is located</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task DeleteFeatureAsync (string featureId, string layerId)
        {
            // verify the required parameter 'featureId' is set
            if (featureId == null) throw new ApiException(400, "Missing required parameter 'featureId' when calling DeleteFeature");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling DeleteFeature");
    
            const string path = "/layers/{layerId}/feature/{featureId}";
    
            var pathParams = new Dictionary<string, string>();
            var queryParams = new Dictionary<string, string>();
            var headerParams = new Dictionary<string, string>();
            var formParams = new Dictionary<string, string>();
            var fileParams = new Dictionary<string, FileParameter>();
            string postBody = null;
    
            pathParams.Add("format", "json");
            pathParams.Add("featureId", ApiClient.ParameterToString(featureId)); // path parameter
            pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            // authentication setting, if any
            string[] authSettings = {  };
    
            // make the HTTP request
            var response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if ((int)response.StatusCode >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteFeature: " + response.Content, response.Content);
        }
    }
}
