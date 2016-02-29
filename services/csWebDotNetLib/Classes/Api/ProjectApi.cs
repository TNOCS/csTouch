using System;
using System.IO;
using System.Collections.Generic;
using RestSharp;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace IO.Swagger.Api
{
    
    public interface IProjectApi
    {
        
        /// <summary>
        /// Returns all projects Returns a list of projects
        /// </summary>
        /// <returns>Dictionary<string, Project></returns>
        Dictionary<string, Project> AllProjects ();
  
        /// <summary>
        /// Returns all projects Returns a list of projects
        /// </summary>
        /// <returns>Dictionary<string, Project></returns>
        System.Threading.Tasks.Task<Dictionary<string, Project>> AllProjectsAsync ();
        
        /// <summary>
        /// Add a new project to the system Adds a new project
        /// </summary>
        /// <param name="body">Project object that needs to be added to the datastorage</param>
        /// <returns></returns>
        void AddProject (Project body);
  
        /// <summary>
        /// Add a new project to the system Adds a new project
        /// </summary>
        /// <param name="body">Project object that needs to be added to the datastorage</param>
        /// <returns></returns>
        System.Threading.Tasks.Task AddProjectAsync (Project body);
        
        /// <summary>
        /// Find project by ID Returns a single project
        /// </summary>
        /// <param name="projectId">ID of the project to be returned</param>
        /// <returns>Project</returns>
        Project GetProject (string projectId);
  
        /// <summary>
        /// Find project by ID Returns a single project
        /// </summary>
        /// <param name="projectId">ID of the project to be returned</param>
        /// <returns>Project</returns>
        System.Threading.Tasks.Task<Project> GetProjectAsync (string projectId);
        
        /// <summary>
        /// Update an existing project 
        /// </summary>
        /// <param name="body">JSON that will be used to update the project</param>
        /// <param name="projectId">ID or Name of the project to update</param>
        /// <returns></returns>
        void UpdateProject (Project body, string projectId);
  
        /// <summary>
        /// Update an existing project 
        /// </summary>
        /// <param name="body">JSON that will be used to update the project</param>
        /// <param name="projectId">ID or Name of the project to update</param>
        /// <returns></returns>
        System.Threading.Tasks.Task UpdateProjectAsync (Project body, string projectId);
        
        /// <summary>
        /// Delete a project by ID Deletes an entire project
        /// </summary>
        /// <param name="projectId">ID of project to be deleted</param>
        /// <returns></returns>
        void DeleteProject (string projectId);
  
        /// <summary>
        /// Delete a project by ID Deletes an entire project
        /// </summary>
        /// <param name="projectId">ID of project to be deleted</param>
        /// <returns></returns>
        System.Threading.Tasks.Task DeleteProjectAsync (string projectId);
        
        /// <summary>
        /// Returns all groups in a project Returns a list of groupIds in a project
        /// </summary>
        /// <param name="projectId">ID of the project to get the groups from</param>
        /// <returns>List<string></returns>
        List<string> AllGroups (string projectId);
  
        /// <summary>
        /// Returns all groups in a project Returns a list of groupIds in a project
        /// </summary>
        /// <param name="projectId">ID of the project to get the groups from</param>
        /// <returns>List<string></returns>
        System.Threading.Tasks.Task<List<string>> AllGroupsAsync (string projectId);
        
        /// <summary>
        /// Create a group in a project Create a group in a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the group to</param>
        /// <param name="body">JSON that will be used to create the group</param>
        /// <returns>ApiResponse</returns>
        ApiResponse AddGroupToProject (string projectId, Group body);
  
        /// <summary>
        /// Create a group in a project Create a group in a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the group to</param>
        /// <param name="body">JSON that will be used to create the group</param>
        /// <returns>ApiResponse</returns>
        System.Threading.Tasks.Task<ApiResponse> AddGroupToProjectAsync (string projectId, Group body);
        
        /// <summary>
        /// Deletes a group from a project Deletes a group from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param>
        /// <param name="groupId">id of the group that will be removed</param>
        /// <returns>ApiResponse</returns>
        ApiResponse RemoveGroup (string projectId, string groupId);
  
        /// <summary>
        /// Deletes a group from a project Deletes a group from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param>
        /// <param name="groupId">id of the group that will be removed</param>
        /// <returns>ApiResponse</returns>
        System.Threading.Tasks.Task<ApiResponse> RemoveGroupAsync (string projectId, string groupId);
        
        /// <summary>
        /// Adds a layer to a project Adds a layer to a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the layer to</param>
        /// <param name="groupId">id of the group to add the layer to</param>
        /// <param name="layerId">id of the layer that will be added</param>
        /// <returns>ApiResponse</returns>
        ApiResponse AddLayerToProject (string projectId, string groupId, string layerId);
  
        /// <summary>
        /// Adds a layer to a project Adds a layer to a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the layer to</param>
        /// <param name="groupId">id of the group to add the layer to</param>
        /// <param name="layerId">id of the layer that will be added</param>
        /// <returns>ApiResponse</returns>
        System.Threading.Tasks.Task<ApiResponse> AddLayerToProjectAsync (string projectId, string groupId, string layerId);
        
        /// <summary>
        /// Deletes a layer from a project Deletes a layer from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param>
        /// <param name="groupId">id of the group to add the layer to</param>
        /// <param name="layerId">id of the layer that will be removed</param>
        /// <returns>ApiResponse</returns>
        ApiResponse RemoveLayerFromProject (string projectId, string groupId, string layerId);
  
        /// <summary>
        /// Deletes a layer from a project Deletes a layer from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param>
        /// <param name="groupId">id of the group to add the layer to</param>
        /// <param name="layerId">id of the layer that will be removed</param>
        /// <returns>ApiResponse</returns>
        System.Threading.Tasks.Task<ApiResponse> RemoveLayerFromProjectAsync (string projectId, string groupId, string layerId);
        
    }
  
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public class ProjectApi : IProjectApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectApi"/> class.
        /// </summary>
        /// <param name="apiClient"> an instance of ApiClient (optional)</param>
        /// <returns></returns>
        public ProjectApi(ApiClient apiClient = null)
        {
            if (apiClient == null) // use the default one in Configuration
                this.ApiClient = Configuration.DefaultApiClient; 
            else
                this.ApiClient = apiClient;
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectApi"/> class.
        /// </summary>
        /// <returns></returns>
        public ProjectApi(String basePath)
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
        /// Returns all projects Returns a list of projects
        /// </summary>
        /// <returns>Dictionary<string, Project></returns>            
        public Dictionary<string, Project> AllProjects ()
        {
            
    
            var path = "/projects/";
    
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
                throw new ApiException ((int)response.StatusCode, "Error calling AllProjects: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AllProjects: " + response.ErrorMessage, response.ErrorMessage);
    
            return (Dictionary<string, Project>) ApiClient.Deserialize(response.Content, typeof(Dictionary<string, Project>), response.Headers);
        }
    
        /// <summary>
        /// Returns all projects Returns a list of projects
        /// </summary>
        /// <returns>Dictionary<string, Project></returns>
        public async System.Threading.Tasks.Task<Dictionary<string, Project>> AllProjectsAsync ()
        {
            
    
            var path = "/projects/";
    
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
                throw new ApiException ((int)response.StatusCode, "Error calling AllProjects: " + response.Content, response.Content);

            return (Dictionary<string, Project>) ApiClient.Deserialize(response.Content, typeof(Dictionary<string, Project>), response.Headers);
        }
        
        /// <summary>
        /// Add a new project to the system Adds a new project
        /// </summary>
        /// <param name="body">Project object that needs to be added to the datastorage</param> 
        /// <returns></returns>            
        public void AddProject (Project body)
        {
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddProject");
            
    
            var path = "/projects/";
    
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
                throw new ApiException ((int)response.StatusCode, "Error calling AddProject: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AddProject: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Add a new project to the system Adds a new project
        /// </summary>
        /// <param name="body">Project object that needs to be added to the datastorage</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task AddProjectAsync (Project body)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddProject");
            
    
            var path = "/projects/";
    
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
                throw new ApiException ((int)response.StatusCode, "Error calling AddProject: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Find project by ID Returns a single project
        /// </summary>
        /// <param name="projectId">ID of the project to be returned</param> 
        /// <returns>Project</returns>            
        public Project GetProject (string projectId)
        {
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling GetProject");
            
    
            var path = "/projects/{projectId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetProject: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling GetProject: " + response.ErrorMessage, response.ErrorMessage);
    
            return (Project) ApiClient.Deserialize(response.Content, typeof(Project), response.Headers);
        }
    
        /// <summary>
        /// Find project by ID Returns a single project
        /// </summary>
        /// <param name="projectId">ID of the project to be returned</param>
        /// <returns>Project</returns>
        public async System.Threading.Tasks.Task<Project> GetProjectAsync (string projectId)
        {
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling GetProject");
            
    
            var path = "/projects/{projectId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling GetProject: " + response.Content, response.Content);

            return (Project) ApiClient.Deserialize(response.Content, typeof(Project), response.Headers);
        }
        
        /// <summary>
        /// Update an existing project 
        /// </summary>
        /// <param name="body">JSON that will be used to update the project</param> 
        /// <param name="projectId">ID or Name of the project to update</param> 
        /// <returns></returns>            
        public void UpdateProject (Project body, string projectId)
        {
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling UpdateProject");
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling UpdateProject");
            
    
            var path = "/projects/{projectId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateProject: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateProject: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Update an existing project 
        /// </summary>
        /// <param name="body">JSON that will be used to update the project</param>
        /// <param name="projectId">ID or Name of the project to update</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task UpdateProjectAsync (Project body, string projectId)
        {
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling UpdateProject");
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling UpdateProject");
            
    
            var path = "/projects/{projectId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.PUT, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling UpdateProject: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Delete a project by ID Deletes an entire project
        /// </summary>
        /// <param name="projectId">ID of project to be deleted</param> 
        /// <returns></returns>            
        public void DeleteProject (string projectId)
        {
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling DeleteProject");
            
    
            var path = "/projects/{projectId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteProject: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteProject: " + response.ErrorMessage, response.ErrorMessage);
    
            return;
        }
    
        /// <summary>
        /// Delete a project by ID Deletes an entire project
        /// </summary>
        /// <param name="projectId">ID of project to be deleted</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task DeleteProjectAsync (string projectId)
        {
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling DeleteProject");
            
    
            var path = "/projects/{projectId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling DeleteProject: " + response.Content, response.Content);

            
            return;
        }
        
        /// <summary>
        /// Returns all groups in a project Returns a list of groupIds in a project
        /// </summary>
        /// <param name="projectId">ID of the project to get the groups from</param> 
        /// <returns>List<string></returns>            
        public List<string> AllGroups (string projectId)
        {
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling AllGroups");
            
    
            var path = "/projects/{projectId}/group/";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AllGroups: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AllGroups: " + response.ErrorMessage, response.ErrorMessage);
    
            return (List<string>) ApiClient.Deserialize(response.Content, typeof(List<string>), response.Headers);
        }
    
        /// <summary>
        /// Returns all groups in a project Returns a list of groupIds in a project
        /// </summary>
        /// <param name="projectId">ID of the project to get the groups from</param>
        /// <returns>List<string></returns>
        public async System.Threading.Tasks.Task<List<string>> AllGroupsAsync (string projectId)
        {
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling AllGroups");
            
    
            var path = "/projects/{projectId}/group/";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.GET, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AllGroups: " + response.Content, response.Content);

            return (List<string>) ApiClient.Deserialize(response.Content, typeof(List<string>), response.Headers);
        }
        
        /// <summary>
        /// Create a group in a project Create a group in a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the group to</param> 
        /// <param name="body">JSON that will be used to create the group</param> 
        /// <returns>ApiResponse</returns>            
        public ApiResponse AddGroupToProject (string projectId, Group body)
        {
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling AddGroupToProject");
            
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddGroupToProject");
            
    
            var path = "/projects/{projectId}/group/";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddGroupToProject: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AddGroupToProject: " + response.ErrorMessage, response.ErrorMessage);
    
            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
    
        /// <summary>
        /// Create a group in a project Create a group in a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the group to</param>
        /// <param name="body">JSON that will be used to create the group</param>
        /// <returns>ApiResponse</returns>
        public async System.Threading.Tasks.Task<ApiResponse> AddGroupToProjectAsync (string projectId, Group body)
        {
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling AddGroupToProject");
            // verify the required parameter 'body' is set
            if (body == null) throw new ApiException(400, "Missing required parameter 'body' when calling AddGroupToProject");
            
    
            var path = "/projects/{projectId}/group/";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            
            
            
            
            postBody = ApiClient.Serialize(body); // http body (model) parameter
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddGroupToProject: " + response.Content, response.Content);

            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
        
        /// <summary>
        /// Deletes a group from a project Deletes a group from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param> 
        /// <param name="groupId">id of the group that will be removed</param> 
        /// <returns>ApiResponse</returns>            
        public ApiResponse RemoveGroup (string projectId, string groupId)
        {
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling RemoveGroup");
            
            // verify the required parameter 'groupId' is set
            if (groupId == null) throw new ApiException(400, "Missing required parameter 'groupId' when calling RemoveGroup");
            
    
            var path = "/projects/{projectId}/group/{groupId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            if (groupId != null) pathParams.Add("groupId", ApiClient.ParameterToString(groupId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling RemoveGroup: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling RemoveGroup: " + response.ErrorMessage, response.ErrorMessage);
    
            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
    
        /// <summary>
        /// Deletes a group from a project Deletes a group from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param>
        /// <param name="groupId">id of the group that will be removed</param>
        /// <returns>ApiResponse</returns>
        public async System.Threading.Tasks.Task<ApiResponse> RemoveGroupAsync (string projectId, string groupId)
        {
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling RemoveGroup");
            // verify the required parameter 'groupId' is set
            if (groupId == null) throw new ApiException(400, "Missing required parameter 'groupId' when calling RemoveGroup");
            
    
            var path = "/projects/{projectId}/group/{groupId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            if (groupId != null) pathParams.Add("groupId", ApiClient.ParameterToString(groupId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling RemoveGroup: " + response.Content, response.Content);

            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
        
        /// <summary>
        /// Adds a layer to a project Adds a layer to a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the layer to</param> 
        /// <param name="groupId">id of the group to add the layer to</param> 
        /// <param name="layerId">id of the layer that will be added</param> 
        /// <returns>ApiResponse</returns>            
        public ApiResponse AddLayerToProject (string projectId, string groupId, string layerId)
        {
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling AddLayerToProject");
            
            // verify the required parameter 'groupId' is set
            if (groupId == null) throw new ApiException(400, "Missing required parameter 'groupId' when calling AddLayerToProject");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling AddLayerToProject");
            
    
            var path = "/projects/{projectId}/group/{groupId}/layer/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            if (groupId != null) pathParams.Add("groupId", ApiClient.ParameterToString(groupId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddLayerToProject: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling AddLayerToProject: " + response.ErrorMessage, response.ErrorMessage);
    
            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
    
        /// <summary>
        /// Adds a layer to a project Adds a layer to a project
        /// </summary>
        /// <param name="projectId">ID of the project to add the layer to</param>
        /// <param name="groupId">id of the group to add the layer to</param>
        /// <param name="layerId">id of the layer that will be added</param>
        /// <returns>ApiResponse</returns>
        public async System.Threading.Tasks.Task<ApiResponse> AddLayerToProjectAsync (string projectId, string groupId, string layerId)
        {
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling AddLayerToProject");
            // verify the required parameter 'groupId' is set
            if (groupId == null) throw new ApiException(400, "Missing required parameter 'groupId' when calling AddLayerToProject");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling AddLayerToProject");
            
    
            var path = "/projects/{projectId}/group/{groupId}/layer/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            if (groupId != null) pathParams.Add("groupId", ApiClient.ParameterToString(groupId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.POST, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling AddLayerToProject: " + response.Content, response.Content);

            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
        
        /// <summary>
        /// Deletes a layer from a project Deletes a layer from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param> 
        /// <param name="groupId">id of the group to add the layer to</param> 
        /// <param name="layerId">id of the layer that will be removed</param> 
        /// <returns>ApiResponse</returns>            
        public ApiResponse RemoveLayerFromProject (string projectId, string groupId, string layerId)
        {
            
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling RemoveLayerFromProject");
            
            // verify the required parameter 'groupId' is set
            if (groupId == null) throw new ApiException(400, "Missing required parameter 'groupId' when calling RemoveLayerFromProject");
            
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling RemoveLayerFromProject");
            
    
            var path = "/projects/{projectId}/group/{groupId}/layer/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;

            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            if (groupId != null) pathParams.Add("groupId", ApiClient.ParameterToString(groupId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) ApiClient.CallApi(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
    
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling RemoveLayerFromProject: " + response.Content, response.Content);
            else if (((int)response.StatusCode) == 0)
                throw new ApiException ((int)response.StatusCode, "Error calling RemoveLayerFromProject: " + response.ErrorMessage, response.ErrorMessage);
    
            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
    
        /// <summary>
        /// Deletes a layer from a project Deletes a layer from a project
        /// </summary>
        /// <param name="projectId">ID of the project to remove the layer from</param>
        /// <param name="groupId">id of the group to add the layer to</param>
        /// <param name="layerId">id of the layer that will be removed</param>
        /// <returns>ApiResponse</returns>
        public async System.Threading.Tasks.Task<ApiResponse> RemoveLayerFromProjectAsync (string projectId, string groupId, string layerId)
        {
            // verify the required parameter 'projectId' is set
            if (projectId == null) throw new ApiException(400, "Missing required parameter 'projectId' when calling RemoveLayerFromProject");
            // verify the required parameter 'groupId' is set
            if (groupId == null) throw new ApiException(400, "Missing required parameter 'groupId' when calling RemoveLayerFromProject");
            // verify the required parameter 'layerId' is set
            if (layerId == null) throw new ApiException(400, "Missing required parameter 'layerId' when calling RemoveLayerFromProject");
            
    
            var path = "/projects/{projectId}/group/{groupId}/layer/{layerId}";
    
            var pathParams = new Dictionary<String, String>();
            var queryParams = new Dictionary<String, String>();
            var headerParams = new Dictionary<String, String>();
            var formParams = new Dictionary<String, String>();
            var fileParams = new Dictionary<String, FileParameter>();
            String postBody = null;
    
            pathParams.Add("format", "json");
            if (projectId != null) pathParams.Add("projectId", ApiClient.ParameterToString(projectId)); // path parameter
            if (groupId != null) pathParams.Add("groupId", ApiClient.ParameterToString(groupId)); // path parameter
            if (layerId != null) pathParams.Add("layerId", ApiClient.ParameterToString(layerId)); // path parameter
            
            
            
            
            
    
            // authentication setting, if any
            String[] authSettings = new String[] {  };
    
            // make the HTTP request
            IRestResponse response = (IRestResponse) await ApiClient.CallApiAsync(path, Method.DELETE, queryParams, postBody, headerParams, formParams, fileParams, pathParams, authSettings);
            if (((int)response.StatusCode) >= 400)
                throw new ApiException ((int)response.StatusCode, "Error calling RemoveLayerFromProject: " + response.Content, response.Content);

            return (ApiResponse) ApiClient.Deserialize(response.Content, typeof(ApiResponse), response.Headers);
        }
        
    }
    
}
