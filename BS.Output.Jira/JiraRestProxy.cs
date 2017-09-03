using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BS.Output.Jira
{
  internal class JiraRestProxy
  {
    
    static internal async Task<GetProjectsResult> GetProjects(string url, string userName, string password)
    {

      try
      {
        string requestUrl = GetApiUrl(url, "project");
        string resultData = await GetData(requestUrl, userName, password);
        List<JiraRestProject> projects  = FromJson<List<JiraRestProject>>(resultData);

        return new GetProjectsResult(ResultStatus.Success, null, projects);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        { 

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new GetProjectsResult(ResultStatus.LoginFailed, null, null);

            default:
              return new GetProjectsResult(ResultStatus.Failed, response.StatusDescription, null);
          }

        }

      }

    }

    static internal async Task<GetProjectIssueTypesResult> GetProjectIssueTypes(string url, string userName, string password)
    {
      
      try
      {
        string requestUrl = GetApiUrl(url, "issue/createmeta");
        string resultData = await GetData(requestUrl, userName, password);
        JiraRestIssueTypes issueTypes = FromJson<JiraRestIssueTypes>(resultData);

        return new GetProjectIssueTypesResult(ResultStatus.Success, null, issueTypes);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new GetProjectIssueTypesResult(ResultStatus.LoginFailed, null, null);

            default:
              return new GetProjectIssueTypesResult(ResultStatus.Failed, response.StatusDescription, null);
          }

        }

      }

    }

    static internal async Task<CreateIssueResult> CreateIssue(string url, string userName, string password, string projectKey, int issueTypeID, string summary, string description)
    {

      try
      {
        string requestUrl = GetApiUrl(url, "issue");
        string resultData = await SendData(requestUrl, userName, password, String.Format("{{\"fields\":{{\"project\":{{\"key\":\"{0}\"}},\"summary\":\"{1}\",\"description\":\"{2}\",\"issuetype\": {{\"id\":\"{3}\"}}}}}}\"", HttpUtility.HtmlEncode(projectKey), HttpUtility.HtmlEncode(summary), HttpUtility.HtmlEncode(description), issueTypeID));
        string issueKey =  FromJson<CreateIssueDataResult>(resultData).IssueKey;

        return new CreateIssueResult(ResultStatus.Success, null, issueKey);
        
      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new CreateIssueResult(ResultStatus.LoginFailed, null, null);

            case HttpStatusCode.BadRequest:
              return new CreateIssueResult(ResultStatus.Failed, FromJson<ErrorResult>(response).GetAllErrorMessages(), null);
              
            default:
              return new CreateIssueResult(ResultStatus.Failed, response.StatusDescription, null);
          }

        }

      }

    }
    
    static internal async Task<Result> AddCommentToIssue(string url, string userName, string password, string issueKey, string comment)
    {

      try
      {

        string requestUrl = GetApiUrl(url, String.Format("issue/{0}/comment", issueKey));

        await SendData(requestUrl, userName, password, String.Format("{{\"body\": \"{0}\"}}", HttpUtility.HtmlEncode(comment)));

        return new Result(ResultStatus.Success, null);
        
      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new Result(ResultStatus.LoginFailed, null);

            case HttpStatusCode.BadRequest:
              return new Result(ResultStatus.Failed, FromJson<ErrorResult>(response).GetAllErrorMessages());
             
            default:
              return new Result(ResultStatus.Failed, response.StatusDescription);
          }

        }

      }

    }
    
    static internal async Task<Result> AddAttachmentToIssue(string url, string userName, string password, string issueKey, string fullFileName, byte[] fileBytes, string fileMimeType)
    {

      try
      {
        
        string requestUrl = GetApiUrl(url, string.Format("issue/{0}/attachments", issueKey));

        await SendFile(requestUrl, userName, password, fullFileName, fileBytes, fileMimeType);

        return new Result(ResultStatus.Success, null);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new Result(ResultStatus.LoginFailed, null);

            case HttpStatusCode.Forbidden:
              return new Result(ResultStatus.Failed, FromJson<ErrorResult>(response).GetAllErrorMessages());

            case HttpStatusCode.NotFound:
              return new Result(ResultStatus.Failed, FromJson<ErrorResult>(response).GetAllErrorMessages());

            default:
              return new Result(ResultStatus.Failed, response.StatusDescription);
          }

        }

      }

    }

    private static async Task<string> GetData(string url, string userName, string password)
    {

      WebRequest request = WebRequest.Create(url);
      request.Method = "GET";
      request.ContentType = "application/json";
     
      string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
      request.Headers.Add("Authorization", "Basic " + basicAuth);

      using (WebResponse response = await request.GetResponseAsync())
      {
        using (Stream responseStream = response.GetResponseStream())
        {
          using (StreamReader reader = new StreamReader(responseStream))
          {
            return await reader.ReadToEndAsync();
          }
        }
      }

    }

    private static async Task<string> SendData(string url, string userName, string password, string jsonData)
    {
      
      WebRequest request = WebRequest.Create(url);
      request.Method = "POST";
      request.ContentType = "application/json";

      string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
      request.Headers.Add("Authorization", "Basic " + basicAuth);

      byte[] postData = Encoding.UTF8.GetBytes(jsonData);

      request.ContentLength = postData.Length;

      using (Stream requestStream = await request.GetRequestStreamAsync())
      {
        requestStream.Write(postData, 0, postData.Length);
        requestStream.Close();
      }

      using (WebResponse response = await request.GetResponseAsync())
      {
        using (Stream responseStream = response.GetResponseStream())
        {
          using (StreamReader reader = new StreamReader(responseStream))
          {
            return await reader.ReadToEndAsync();
          }
        }
      }

    }

    private static async Task<string> SendFile(string url, string userName, string password, string fullFileName, byte[] fileBytes, string fileMimeType)
    {

      string boundary = string.Format("----------{0}", DateTime.Now.Ticks.ToString("x"));

      WebRequest request = WebRequest.Create(url);
      request.Method = "POST";
      request.ContentType = "multipart/form-data; boundary=" + boundary;
      
      StringBuilder postData = new StringBuilder();
      postData.AppendFormat("--{0}", boundary);
      postData.AppendLine();
      postData.AppendFormat("Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\n", fullFileName);
      postData.AppendFormat("Content-Type: {0}\r\n", fileMimeType);
      postData.AppendLine();

      byte[] postBytes = Encoding.UTF8.GetBytes(postData.ToString());
      byte[] boundaryBytes = Encoding.ASCII.GetBytes(String.Format("\r\n--{0}--\r\n", boundary));

      request.Headers.Add("X-Atlassian-Token", "no-check");

      string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
      request.Headers.Add("Authorization", "Basic " + basicAuth);
           
      request.ContentLength = postBytes.Length + fileBytes.Length + boundaryBytes.Length;

      using (Stream requestStream = await request.GetRequestStreamAsync())
      {
        requestStream.Write(postBytes, 0, postBytes.Length);
        requestStream.Write(fileBytes, 0, fileBytes.Length);
        requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
        requestStream.Close();
      }

      using (WebResponse response = await request.GetResponseAsync())
      {
        using (Stream responseStream = response.GetResponseStream())
        {
          using (StreamReader reader = new StreamReader(responseStream))
          {
            return await reader.ReadToEndAsync();
          }
        }
      }

    }

    private static string GetApiUrl(string url, string method)
    {

      string apiUrl = url;

      if (!(apiUrl.LastIndexOf("/") == apiUrl.Length - 1))
      {
        apiUrl += "/";
      }

      apiUrl += "rest/api/latest/" + method;

      return apiUrl;

    }

    private static T FromJson<T>(string jsonText)
    {

      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));

      using (MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(jsonText)))
      {
        return (T)serializer.ReadObject(stream);
      }

    }

    private static T FromJson<T>(WebResponse response)
    {

      string responseContent = null;

      using (StreamReader reader = new StreamReader(response.GetResponseStream()))
      {
        responseContent = reader.ReadToEnd();
      }

      DataContractJsonSerializerSettings serializerSettings = new DataContractJsonSerializerSettings();
      serializerSettings.UseSimpleDictionaryFormat = true;

      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), serializerSettings);

      using (MemoryStream memoryStream = new MemoryStream(Encoding.Unicode.GetBytes(responseContent)))
      {
        return (T)serializer.ReadObject(memoryStream);
      }

    }

  }

  internal enum ResultStatus : int
  {
    Success = 1,
    LoginFailed = 2,
    Failed = 3
  }

  class Result
  {

    ResultStatus status;
    string failedMessage;
   
    public Result(ResultStatus status,
                  string failedMessage)
    {
      this.status = status;
      this.failedMessage = failedMessage;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

  }

  class GetProjectsResult
  {

    ResultStatus status;
    string failedMessage;
    List<JiraRestProject> projects;

    public GetProjectsResult(ResultStatus status,
                  string failedMessage,
                  List<JiraRestProject> projects)
    {
      this.status = status;
      this.failedMessage = failedMessage;
      this.projects = projects;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

    public List<JiraRestProject> Projects
    {
      get { return projects; }
    }

  }

  class GetProjectIssueTypesResult
  {

    ResultStatus status;
    string failedMessage;
    JiraRestIssueTypes issueTypes;

    public GetProjectIssueTypesResult(ResultStatus status,
                                      string failedMessage,
                                      JiraRestIssueTypes issueTypes)
    {
      this.status = status;
      this.failedMessage = failedMessage;
      this.issueTypes = issueTypes;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

    public JiraRestIssueTypes IssueTypes
    {
      get { return issueTypes; }
    }

  }

  class CreateIssueResult
  {

    ResultStatus status;
    string failedMessage;
    string issueKey;

    public CreateIssueResult(ResultStatus status,
                             string failedMessage,
                             string issueKey)
    {
      this.status = status;
      this.failedMessage = failedMessage;
      this.issueKey = issueKey;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

    public string IssueKey
    {
      get { return issueKey; }
    }

  }
  
  [DataContract(), System.Reflection.Obfuscation(Exclude = true)]
  class JiraRestProject
  {

    string key;
    string name;

    [DataMember(Name = "key")]
    public string Key
    {
      get { return key; }
      set { key = value; }
    }

    [DataMember(Name = "name")]
    public string Name
    {
      get { return name; }
      set { name = value; }
    }

  }

  [DataContract()]
  class JiraRestIssueTypes
  {
    
    List<JiraRestIssueTypesProject> projects;

    [DataMember(Name = "projects")]
    public List<JiraRestIssueTypesProject> Projects
    {
      get { return projects; }
      set { projects = value; }
    }

  }

  [DataContract()]
  class JiraRestIssueTypesProject
  {

    string projectKey;
    List<JiraRestIssueType> issueTypes;

    [DataMember(Name = "key")]
    public string ProjectKey
    {
      get { return projectKey; }
      set { projectKey = value; }
    }

    [DataMember(Name = "issuetypes")]
    public List<JiraRestIssueType> IssueTypes
    {
      get { return issueTypes; }
      set { issueTypes = value; }
    }

  }

  [DataContract(), System.Reflection.Obfuscation(Exclude = true)]
  class JiraRestIssueType
  {

    int id;
    string name;
    bool subTask;

    [DataMember(Name = "id")]
    public int ID
    {
      get { return id; }
      set { id = value; }
    }

    [DataMember(Name = "name")]
    public string Name
    {
      get { return name; }
      set { name = value; }
    }

    [DataMember(Name = "subtask")]
    public bool SubTask
    {
      get { return subTask; }
      set { subTask = value; }
    }

  }

  [DataContract()]
  class CreateIssueDataResult
  {

    string issueKey;

    [DataMember(Name = "key")]
    public string IssueKey
    {
      get { return issueKey; }
      set { issueKey = value; }
    }

  }

  [DataContract()]
  class ErrorResult
  {

    List<string> errorMessages;
    Dictionary<string, string> errors;

    [DataMember(Name = "errorMessages")]
    public List<string> ErrorMessages
    {
      get { return errorMessages; }
      set { errorMessages = value; }
    }

    [DataMember(Name = "errors")]
    public Dictionary<string, string> Errors
    {
      get { return errors; }
      set { errors = value; }
    }

    public string GetAllErrorMessages()
    {

      List<string> allErrorMessages = new List<string>();
      allErrorMessages.AddRange(errorMessages);
      allErrorMessages.AddRange(errors.Values);

      return string.Join("\r\n", allErrorMessages);

    }

  }

}
