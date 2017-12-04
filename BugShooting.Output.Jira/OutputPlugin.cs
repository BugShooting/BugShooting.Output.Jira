using BS.Plugin.V3.Common;
using BS.Plugin.V3.Output;
using BS.Plugin.V3.Utilities;
using System;
using System.Drawing;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BugShooting.Output.Jira
{
  public class OutputPlugin: OutputPlugin<Output>
  {

    protected override string Name
    {
      get { return "JIRA"; }
    }

    protected override Image Image64
    {
      get  { return Properties.Resources.logo_64; }
    }

    protected override Image Image16
    {
      get { return Properties.Resources.logo_16 ; }
    }

    protected override bool Editable
    {
      get { return true; }
    }

    protected override string Description
    {
      get { return "Attach screenshots to JIRA issues."; }
    }
    
    protected override Output CreateOutput(IWin32Window Owner)
    {
      
      Output output = new Output(Name, 
                                 String.Empty, 
                                 String.Empty, 
                                 String.Empty, 
                                 "Screenshot",
                                 String.Empty, 
                                 true,
                                 String.Empty,
                                 0,
                                 1);

      return EditOutput(Owner, output);

    }

    protected override Output EditOutput(IWin32Window Owner, Output Output)
    {

      Edit edit = new Edit(Output);

      var ownerHelper = new System.Windows.Interop.WindowInteropHelper(edit);
      ownerHelper.Owner = Owner.Handle;
      
      if (edit.ShowDialog() == true) {

        return new Output(edit.OutputName,
                          edit.Url,
                          edit.UserName,
                          edit.Password,
                          edit.FileName,
                          edit.FileFormat,
                          edit.OpenItemInBrowser,
                          Output.LastProjectKey,
                          Output.LastIssueTypeID,
                          Output.LastIssueID);
      }
      else
      {
        return null; 
      }

    }

    protected override OutputValues SerializeOutput(Output Output)
    {

      OutputValues outputValues = new OutputValues();

      outputValues.Add("Name", Output.Name);
      outputValues.Add("Url", Output.Url);
      outputValues.Add("UserName", Output.UserName);
      outputValues.Add("Password",Output.Password, true);
      outputValues.Add("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser));
      outputValues.Add("FileName", Output.FileName);
      outputValues.Add("FileFormat", Output.FileFormat);
      outputValues.Add("LastProjectKey", Output.LastProjectKey);
      outputValues.Add("LastIssueTypeID", Output.LastIssueTypeID.ToString());
      outputValues.Add("LastIssueID", Output.LastIssueID.ToString());

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValues OutputValues)
    {

      return new Output(OutputValues["Name", this.Name],
                        OutputValues["Url", ""], 
                        OutputValues["UserName", ""],
                        OutputValues["Password", ""], 
                        OutputValues["FileName", "Screenshot"], 
                        OutputValues["FileFormat", ""],
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)]),
                        OutputValues["LastProjectKey", string.Empty],
                        Convert.ToInt32(OutputValues["LastIssueTypeID", "0"]),
                        Convert.ToInt32(OutputValues["LastIssueID", "1"]));

    }

    protected override async Task<SendResult> Send(IWin32Window Owner, Output Output, ImageData ImageData)
    {

      try
      {

        string userName = Output.UserName;
        string password = Output.Password;
        bool showLogin = string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password);
        bool rememberCredentials = false;

        string fileName = FileHelper.GetFileName(Output.FileName,  ImageData);

        while (true)
        {

          if (showLogin)
          {

            // Show credentials window
            Credentials credentials = new Credentials(Output.Url, userName, password, rememberCredentials);

            var ownerHelper = new System.Windows.Interop.WindowInteropHelper(credentials);
            ownerHelper.Owner = Owner.Handle;

            if (credentials.ShowDialog() != true)
            {
              return new SendResult(Result.Canceled);
            }

            userName = credentials.UserName;
            password = credentials.Password;
            rememberCredentials = credentials.Remember;

          }

          try
          {

            GetProjectsResult projectsResult = await JiraRestProxy.GetProjects(Output.Url, userName, password);
            switch (projectsResult.Status)
            {
              case ResultStatus.Success:
                break;
              case ResultStatus.LoginFailed:
                showLogin = true;
                continue;
              case ResultStatus.Failed:
                return new SendResult(Result.Failed, projectsResult.FailedMessage);
            }

            GetProjectIssueTypesResult issueTypesResult = await JiraRestProxy.GetProjectIssueTypes(Output.Url, userName, password);
            switch (issueTypesResult.Status)
            {
              case ResultStatus.Success:
                break;
              case ResultStatus.LoginFailed:
                showLogin = true;
                continue;
              case ResultStatus.Failed:
                return new SendResult(Result.Failed, projectsResult.FailedMessage);
            }

            // Show send window
            Send send = new Send(Output.Url, Output.LastProjectKey, Output.LastIssueTypeID, Output.LastIssueID, projectsResult.Projects, issueTypesResult.IssueTypes, fileName);

            var ownerHelper = new System.Windows.Interop.WindowInteropHelper(send);
            ownerHelper.Owner = Owner.Handle;

            if (!send.ShowDialog() == true)
            {
              return new SendResult(Result.Canceled);
            }

            int issueTypeID;
            string issueKey;

            if (send.CreateNewIssue)
            {

              issueTypeID = send.IssueTypeID;

              // Create issue
              CreateIssueResult createIssueResult = await JiraRestProxy.CreateIssue(Output.Url, userName, password, send.ProjectKey, issueTypeID, send.Summary, send.Description);
              switch (createIssueResult.Status)
              {
                case ResultStatus.Success:
                  break;
                case ResultStatus.LoginFailed:
                  showLogin = true;
                  continue;
                case ResultStatus.Failed:
                  return new SendResult(Result.Failed, createIssueResult.FailedMessage);
              }

              issueKey = createIssueResult.IssueKey;

            }
            else
            {
              issueTypeID = Output.LastIssueTypeID;
              issueKey = String.Format("{0}-{1}", send.ProjectKey, send.IssueID);

              // Add comment to issue
              if (! String.IsNullOrEmpty(send.Comment))
              {
                IssueResult commentResult = await JiraRestProxy.AddCommentToIssue(Output.Url, userName, password, issueKey, send.Comment);
                switch (commentResult.Status)
                {
                  case ResultStatus.Success:
                    break;
                  case ResultStatus.LoginFailed:
                    showLogin = true;
                    continue;
                  case ResultStatus.Failed:
                    return new SendResult(Result.Failed, commentResult.FailedMessage);
                }
              }
              
            }

            string fullFileName = String.Format("{0}.{1}", send.FileName, FileHelper.GetFileExtention(Output.FileFormat));
            string fileMimeType = FileHelper.GetMimeType(Output.FileFormat);
            byte[] fileBytes = FileHelper.GetFileBytes(Output.FileFormat, ImageData);

            // Add attachment to issue
            IssueResult attachmentResult = await JiraRestProxy.AddAttachmentToIssue(Output.Url, userName, password, issueKey, fullFileName, fileBytes, fileMimeType);
            switch (attachmentResult.Status)
            {
              case ResultStatus.Success:
                break;
              case ResultStatus.LoginFailed:
                showLogin = true;
                continue;
              case ResultStatus.Failed:
                return new SendResult(Result.Failed, attachmentResult.FailedMessage);
            }


            // Open issue in browser
            if (Output.OpenItemInBrowser)
            {
              WebHelper.OpenUrl(String.Format("{0}/browse/{1}", Output.Url, issueKey));
            }
            

            int issueID = Convert.ToInt32(issueKey.Split(new Char[]{'-'})[1]);
            return new SendResult(Result.Success,
                                  new Output(Output.Name,
                                             Output.Url,
                                             (rememberCredentials) ? userName : Output.UserName,
                                             (rememberCredentials) ? password : Output.Password,
                                             Output.FileName,
                                             Output.FileFormat,
                                             Output.OpenItemInBrowser,
                                             send.ProjectKey,
                                             issueTypeID,
                                             issueID));

          }
          catch (FaultException ex) when (ex.Reason.ToString() == "Access denied")
          {
            // Login failed
            showLogin = true;
          }

        }

      }
      catch (Exception ex)
      {
        return new SendResult(Result.Failed, ex.Message);
      }

    }

  }
}
