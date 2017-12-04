using BS.Plugin.V3.Output;

namespace BugShooting.Output.Jira
{

  public class Output: IOutput 
  {
    
    string name;
    string url;
    string userName;
    string password;
    string fileName;
    string fileFormat;
    bool openItemInBrowser;
    string lastProjectKey;
    int lastIssueTypeID;
    int lastIssueID;

    public Output(string name, 
                  string url, 
                  string userName,
                  string password, 
                  string fileName, 
                  string fileFormat,
                  bool openItemInBrowser, 
                  string lastProjectKey,
                  int lastIssueTypeID,
                  int lastIssueID)
    {
      this.name = name;
      this.url = url;
      this.userName = userName;
      this.password = password;
      this.fileName = fileName;
      this.fileFormat = fileFormat;
      this.openItemInBrowser = openItemInBrowser;
      this.lastProjectKey = lastProjectKey;
      this.lastIssueTypeID = lastIssueTypeID;
      this.lastIssueID = lastIssueID;
    }
    
    public string Name
    {
      get { return name; }
    }

    public string Information
    {
      get { return url; }
    }

    public string Url
    {
      get { return url; }
    }
       
    public string UserName
    {
      get { return userName; }
    }

    public string Password
    {
      get { return password; }
    }
          
    public string FileName
    {
      get { return fileName; }
    }

    public string FileFormat
    {
      get { return fileFormat; }
    }

    public bool OpenItemInBrowser
    {
      get { return openItemInBrowser; }
    }
    
    public string LastProjectKey
    {
      get { return lastProjectKey; }
    }

    public int LastIssueTypeID
    {
      get { return lastIssueTypeID; }
    }

    public int LastIssueID
    {
      get { return lastIssueID; }
    }

  }
}
