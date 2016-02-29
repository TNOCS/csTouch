using System;
using System.IO;
using System.Collections.Generic;
using RestSharp;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace IO.Swagger.Api
{
    
    public interface ILayerApi
    {
        
        /// <summary>
        /// Returns a list of all layers and their associated storage engine Returns a list of all layers and their associated storage
        /// </summary>
        /// <returns>Dictionary<string, Layer></returns>
        Dictionary<string, Layer> AllLayers ();
  
        /// <summary>
        /// Returns a list of all layers and their associated storage engine Returns a list of all layers and their associated storage
        /// </summary>
        /// <returns>Dictionary<string, Layer></returns>
        System.Threading.Tasks.Task<Dictionary<string, Layer>> AllLayersAsync ();
        
        /// <summary>
        /// Add a new layer to the system Adds a new layer
        /// </summary>
        /// <param name="body">Layer object that needs to be added to the datastorage</param>
        /// <returns></returns>
        void AddLayer (Layer body);
  
        /// <summary>
        /// Add a new layer to the system Adds a new layer
        /// </summary>
        /// <param name="body">Layer object that needs to be added to the datastorage</param>
        /// <returns></returns>
        System.Threading.Tasks.Task AddLayerAsync (Layer body);
        
        /// <summary>
        /// Find layer by ID Returns a single layer
        /// </summary>
        /// <param name="layerId">ID of the layer to be returned</param>
        /// <returns>Layer</returns>
        Layer GetLayer (string layerId);
  
        /// <summary>
        /// Find layer by ID Returns a single layer
        /// </summary>
        /// <param name="layerId">ID of the layer to be returned</param>
        /// <returns>Layer</returns>
        System.Threading.Tasks.Task<Layer> GetLayerAsync (string layerId);
        
        /// <summary>
        /// Update an existing layer 
        /// </summary>
        /// <param name="body">JSON that will be used to update the layer</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <returns></returns>
        void UpdateLayer (Feature body, string layerId);
  
        /// <summary>
        /// Update an existing layer 
        /// </summary>
        /// <param name="body">JSON that will be used to update the layer</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <returns></returns>
        System.Threading.Tasks.Task UpdateLayerAsync (Feature body, string layerId);
        
        /// <summary>
        /// Delete a layer by ID Deletes an entire layer
        /// </summary>
        /// <param name="layerId">ID of layer to be deleted</param>
        /// <returns></returns>
        void DeleteLayer (string layerId);
  
        /// <summary>
        /// Delete a layer by ID Deletes an entire layer
        /// </summary>
        /// <param name="layerId">ID of layer to be deleted</param>
        /// <returns></returns>
        System.Threading.Tasks.Task DeleteLayerAsync (string layerId);
        
    }
  
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public class LayerApi : ILayerApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayerApi"/> class.
        /// </summary>
        /// <param name="apiClient"> an instance of ApiClient (optional)</param>
        /// <returns></returns>
        public LayerApi(ApiClient apiClient = null)
        {
            if (apiClient == null) // use the default one in Configuration
                this.ApiClient = Configuration.DefaultApiClient; 
            else
                this.ApiClient = apiClient;
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="LayerApi"/> class.
        /// </summary>
        /// <returns></returns>
        public LayerApi(String basePath)
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
        /// Returns a list of all layers and their associated storage engine Returns a list of all layers and their associated storage
        /// </summary>
        /// <returns>Dictionary<string, Layer></returns>            
        public Dictionary<string, Layer> AllLayers ()
        {
            
    
            var path = "/layers/";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AllLayers: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AllLayers: " + response.ErrorMessage, response.ErrorMessage);
    
            return (Dictionary<string, Layer>) ApiClient.Deserialize(response.Content, typeof(Dictionary<string, Layer>), response.Headers);
        }
    
        /// <summary>
        /// Returns a list of all layers and their associated storage engine Returns a list of all layers and their associated storage
        /// </summary>
        /// <returns>Dictionary<string, Layer></returns>
        public async System.Threading.Tasks.Task<Dictionary<string, Layer>> AllLayersAsync ()
        {
            
    
            var path = "/layers/";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AllLayers: " + response.Content, response.Content);

            return (Dictionary<string, Layer>) ApiClient.Deserialize(response.Content, typeof(Dictionary<string, Layer>), response.Headers);
        }
        
        /// <summary>
        /// Add a new layer to the system Adds a new layer
        /// </summary>
        /// <param name="body">Layer object that needs to be added to the datastorage</param> 
        /// <returns></returns>            
        public void AddLayer (Layer body)
        {
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddLayer");
            
    
            var path = "/layers/";
    
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
                throw new ApiException ((int)response.StatusCode, "Error calling AddLayer: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AddLayer: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Add a new layer to the system Adds a new layer
        /// </summary>
        /// <param name="body">Layer object that needs to be added to the datastorage</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task AddLayerAsync (Layer body)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddLayer");
            
    
            var path = "/layers/";
    
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
                throw new ApiException ((int)response.StatusCode, "Error calling AddLayer: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Find layer by ID Returns a single layer
        /// </summary>
        /// <param name="layerId">ID of the layer to be returned</param> 
        /// <returns>Layer</returns>            
        public Layer GetLayer (string layerId)
        {
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling GetLayer");
            
    
            var path = "/layers/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetLayer: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling GetLayer: " + response.ErrorMessage, response.ErrorMessage);
    
            return (Layer) ApiClient.Deserialize(response.Content, typeof(Layer), response.Headers);
        }
    
        /// <summary>
        /// Find layer by ID Returns a single layer
        /// </summary>
        /// <param name="layerId">ID of the layer to be returned</param>
        /// <returns>Layer</returns>
        public async System.Threading.Tasks.Task<Layer> GetLayerAsync (string layerId)
        {
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling GetLayer");
            
    
            var path = "/layers/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetLayer: " + response.Content, response.Content);

            return (Layer) ApiClient.Deserialize(response.Content, typeof(Layer), response.Headers);
        }
        
        /// <summary>
        /// Update an existing layer 
        /// </summary>
        /// <param name="body">JSON that will be used to update the layer</param> 
        /// <param name="layerId">ID or Name of the Layer to update</param> 
        /// <returns></returns>            
        public void UpdateLayer (Feature body, string layerId)
        {
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling UpdateLayer");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling UpdateLayer");
            
    
            var path = "/layers/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateLayer: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateLayer: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Update an existing layer 
        /// </summary>
        /// <param name="body">JSON that will be used to update the layer</param>
        /// <param name="layerId">ID or Name of the Layer to update</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task UpdateLayerAsync (Feature body, string layerId)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling UpdateLayer");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling UpdateLayer");
            
    
            var path = "/layers/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateLayer: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Delete a layer by ID Deletes an entire layer
        /// </summary>
        /// <param name="layerId">ID of layer to be deleted</param> 
        /// <returns></returns>            
        public void DeleteLayer (string layerId)
        {
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling DeleteLayer");
            
    
            var path = "/layers/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteLayer: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteLayer: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Delete a layer by ID Deletes an entire layer
        /// </summary>
        /// <param name="layerId">ID of layer to be deleted</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task DeleteLayerAsync (string layerId)
        {
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling DeleteLayer");
            
    
            var path = "/layers/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteLayer: " + response.Content, response.Content);

            
            return;
        }
        
    }
    
}
