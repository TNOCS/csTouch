using System;
using System.IO;
using System.Collections.Generic;
using RestSharp;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace IO.Swagger.Api
{
    
    public interface ISpecialApi
    {
        
        /// <summary>
        /// Gets all and any points in a (bounding box) rectangle. Takes two lng/lat pairs as input. These MUST be the south-west (bottom left) and northeast (top right) coordinates expressed as floating point numbers.
        /// </summary>
        /// <param name="swlng">Southwestern longtitude used to calculate the bbox</param>
        /// <param name="swlat">Southwestern latitude used to calculate the bbox</param>
        /// <param name="nelng">Northeastern longtitude used to calculate the bbox</param>
        /// <param name="nelat">Northeastern latitude used to calculate the bbox</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        void Bbox (float? swlng, float? swlat, float? nelng, float? nelat, string layerId);
  
        /// <summary>
        /// Gets all and any points in a (bounding box) rectangle. Takes two lng/lat pairs as input. These MUST be the south-west (bottom left) and northeast (top right) coordinates expressed as floating point numbers.
        /// </summary>
        /// <param name="swlng">Southwestern longtitude used to calculate the bbox</param>
        /// <param name="swlat">Southwestern latitude used to calculate the bbox</param>
        /// <param name="nelng">Northeastern longtitude used to calculate the bbox</param>
        /// <param name="nelat">Northeastern latitude used to calculate the bbox</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        System.Threading.Tasks.Task BboxAsync (float? swlng, float? swlat, float? nelng, float? nelat, string layerId);
        
        /// <summary>
        /// Gets all and any points in a sphere defined by its center coordinate. takes the center coordinate (Lat:lng) and distance (radius) as arguments to provide a list of coordinates starting with the closest coordinates to the center.
        /// </summary>
        /// <param name="lng">Longtitude used to calculate the sphere</param>
        /// <param name="lat">Latitude used to calculate the sphere</param>
        /// <param name="distance">Max distane in meters</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        void Getsphere (float? lng, float? lat, float? distance, string layerId);
  
        /// <summary>
        /// Gets all and any points in a sphere defined by its center coordinate. takes the center coordinate (Lat:lng) and distance (radius) as arguments to provide a list of coordinates starting with the closest coordinates to the center.
        /// </summary>
        /// <param name="lng">Longtitude used to calculate the sphere</param>
        /// <param name="lat">Latitude used to calculate the sphere</param>
        /// <param name="distance">Max distane in meters</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        System.Threading.Tasks.Task GetsphereAsync (float? lng, float? lat, float? distance, string layerId);
        
        /// <summary>
        /// Gets all and any points that are within a user-supplied polygon in a GeoJSON document. Takes a GeoJSON specification compliant document as input. This method can be used to determine the points within a special area, such as municipial borders.
        /// </summary>
        /// <param name="feature">JSON that will be used to query the layer</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        void Getwithinpolygon (Feature feature, string layerId);
  
        /// <summary>
        /// Gets all and any points that are within a user-supplied polygon in a GeoJSON document. Takes a GeoJSON specification compliant document as input. This method can be used to determine the points within a special area, such as municipial borders.
        /// </summary>
        /// <param name="feature">JSON that will be used to query the layer</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        System.Threading.Tasks.Task GetwithinpolygonAsync (Feature feature, string layerId);
        
    }
  
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public class SpecialApi : ISpecialApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialApi"/> class.
        /// </summary>
        /// <param name="apiClient"> an instance of ApiClient (optional)</param>
        /// <returns></returns>
        public SpecialApi(ApiClient apiClient = null)
        {
            if (apiClient == null) // use the default one in Configuration
                this.ApiClient = Configuration.DefaultApiClient; 
            else
                this.ApiClient = apiClient;
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialApi"/> class.
        /// </summary>
        /// <returns></returns>
        public SpecialApi(String basePath)
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
        /// Gets all and any points in a (bounding box) rectangle. Takes two lng/lat pairs as input. These MUST be the south-west (bottom left) and northeast (top right) coordinates expressed as floating point numbers.
        /// </summary>
        /// <param name="swlng">Southwestern longtitude used to calculate the bbox</param> 
        /// <param name="swlat">Southwestern latitude used to calculate the bbox</param> 
        /// <param name="nelng">Northeastern longtitude used to calculate the bbox</param> 
        /// <param name="nelat">Northeastern latitude used to calculate the bbox</param> 
        /// <param name="layerId">ID of the layer</param> 
        /// <returns></returns>            
        public void Bbox (float? swlng, float? swlat, float? nelng, float? nelat, string layerId)
        {
            
            // verify the required parameter 'swlng' is set
            if (swlng == null) throw new ApiException(400, "Missing required parameter 'swlng' when calling Bbox");
            
            // verify the required parameter 'swlat' is set
            if (swlat == null) throw new ApiException(400, "Missing required parameter 'swlat' when calling Bbox");
            
            // verify the required parameter 'nelng' is set
            if (nelng == null) throw new ApiException(400, "Missing required parameter 'nelng' when calling Bbox");
            
            // verify the required parameter 'nelat' is set
            if (nelat == null) throw new ApiException(400, "Missing required parameter 'nelat' when calling Bbox");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling Bbox");
            
    
            var path = "/layers/{layerId}/bbox";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            if (swlng != null) queryParams.Add("swlng", ApiClient.ParameterToString(swlng)); // query parameter
            if (swlat != null) queryParams.Add("swlat", ApiClient.ParameterToString(swlat)); // query parameter
            if (nelng != null) queryParams.Add("nelng", ApiClient.ParameterToString(nelng)); // query parameter
            if (nelat != null) queryParams.Add("nelat", ApiClient.ParameterToString(nelat)); // query parameter
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling Bbox: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling Bbox: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Gets all and any points in a (bounding box) rectangle. Takes two lng/lat pairs as input. These MUST be the south-west (bottom left) and northeast (top right) coordinates expressed as floating point numbers.
        /// </summary>
        /// <param name="swlng">Southwestern longtitude used to calculate the bbox</param>
        /// <param name="swlat">Southwestern latitude used to calculate the bbox</param>
        /// <param name="nelng">Northeastern longtitude used to calculate the bbox</param>
        /// <param name="nelat">Northeastern latitude used to calculate the bbox</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task BboxAsync (float? swlng, float? swlat, float? nelng, float? nelat, string layerId)
        {
            // verify the required parameter 'swlng' is set
            if (swlng == null) throw new ApiException(400, "Missing required parameter 'swlng' when calling Bbox");
            // verify the required parameter 'swlat' is set
            if (swlat == null) throw new ApiException(400, "Missing required parameter 'swlat' when calling Bbox");
            // verify the required parameter 'nelng' is set
            if (nelng == null) throw new ApiException(400, "Missing required parameter 'nelng' when calling Bbox");
            // verify the required parameter 'nelat' is set
            if (nelat == null) throw new ApiException(400, "Missing required parameter 'nelat' when calling Bbox");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling Bbox");
            
    
            var path = "/layers/{layerId}/bbox";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            if (swlng != null) queryParams.Add("swlng", ApiClient.ParameterToString(swlng)); // query parameter
            if (swlat != null) queryParams.Add("swlat", ApiClient.ParameterToString(swlat)); // query parameter
            if (nelng != null) queryParams.Add("nelng", ApiClient.ParameterToString(nelng)); // query parameter
            if (nelat != null) queryParams.Add("nelat", ApiClient.ParameterToString(nelat)); // query parameter
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling Bbox: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Gets all and any points in a sphere defined by its center coordinate. takes the center coordinate (Lat:lng) and distance (radius) as arguments to provide a list of coordinates starting with the closest coordinates to the center.
        /// </summary>
        /// <param name="lng">Longtitude used to calculate the sphere</param> 
        /// <param name="lat">Latitude used to calculate the sphere</param> 
        /// <param name="distance">Max distane in meters</param> 
        /// <param name="layerId">ID of the layer</param> 
        /// <returns></returns>            
        public void Getsphere (float? lng, float? lat, float? distance, string layerId)
        {
            
            // verify the required parameter 'lng' is set
            if (lng == null) throw new ApiException(400, "Missing required parameter 'lng' when calling Getsphere");
            
            // verify the required parameter 'lat' is set
            if (lat == null) throw new ApiException(400, "Missing required parameter 'lat' when calling Getsphere");
            
            // verify the required parameter 'distance' is set
            if (distance == null) throw new ApiException(400, "Missing required parameter 'distance' when calling Getsphere");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling Getsphere");
            
    
            var path = "/layers/{layerId}/getsphere";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            if (lng != null) queryParams.Add("lng", ApiClient.ParameterToString(lng)); // query parameter
            if (lat != null) queryParams.Add("lat", ApiClient.ParameterToString(lat)); // query parameter
            if (distance != null) queryParams.Add("Distance", ApiClient.ParameterToString(distance)); // query parameter
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling Getsphere: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling Getsphere: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Gets all and any points in a sphere defined by its center coordinate. takes the center coordinate (Lat:lng) and distance (radius) as arguments to provide a list of coordinates starting with the closest coordinates to the center.
        /// </summary>
        /// <param name="lng">Longtitude used to calculate the sphere</param>
        /// <param name="lat">Latitude used to calculate the sphere</param>
        /// <param name="distance">Max distane in meters</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task GetsphereAsync (float? lng, float? lat, float? distance, string layerId)
        {
            // verify the required parameter 'lng' is set
            if (lng == null) throw new ApiException(400, "Missing required parameter 'lng' when calling Getsphere");
            // verify the required parameter 'lat' is set
            if (lat == null) throw new ApiException(400, "Missing required parameter 'lat' when calling Getsphere");
            // verify the required parameter 'distance' is set
            if (distance == null) throw new ApiException(400, "Missing required parameter 'distance' when calling Getsphere");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling Getsphere");
            
    
            var path = "/layers/{layerId}/getsphere";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            if (lng != null) queryParams.Add("lng", ApiClient.ParameterToString(lng)); // query parameter
            if (lat != null) queryParams.Add("lat", ApiClient.ParameterToString(lat)); // query parameter
            if (distance != null) queryParams.Add("Distance", ApiClient.ParameterToString(distance)); // query parameter
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling Getsphere: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Gets all and any points that are within a user-supplied polygon in a GeoJSON document. Takes a GeoJSON specification compliant document as input. This method can be used to determine the points within a special area, such as municipial borders.
        /// </summary>
        /// <param name="feature">JSON that will be used to query the layer</param> 
        /// <param name="layerId">ID of the layer</param> 
        /// <returns></returns>            
        public void Getwithinpolygon (Feature feature, string layerId)
        {
            
            // verify the required parameter 'feature' is set
            if (feature == null) throw new ApiException(400, "Missing required parameter 'feature' when calling Getwithinpolygon");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling Getwithinpolygon");
            
    
            var path = "/layers/{layerId}/getwithinpolygon";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(feature); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling Getwithinpolygon: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling Getwithinpolygon: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Gets all and any points that are within a user-supplied polygon in a GeoJSON document. Takes a GeoJSON specification compliant document as input. This method can be used to determine the points within a special area, such as municipial borders.
        /// </summary>
        /// <param name="feature">JSON that will be used to query the layer</param>
        /// <param name="layerId">ID of the layer</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task GetwithinpolygonAsync (Feature feature, string layerId)
        {
            // verify the required parameter 'feature' is set
            if (feature == null) throw new ApiException(400, "Missing required parameter 'feature' when calling Getwithinpolygon");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling Getwithinpolygon");
            
    
            var path = "/layers/{layerId}/getwithinpolygon";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(feature); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling Getwithinpolygon: " + response.Content, response.Content);

            
            return;
        }
        
    }
    
}
