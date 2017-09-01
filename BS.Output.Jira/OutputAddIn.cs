using System;
using System.Drawing;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace BS.Output.Jira
{
  public class OutputAddIn: V3.OutputAddIn<Output>
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

    protected override OutputValueCollection SerializeOutput(Output Output)
    {

      OutputValueCollection outputValues = new OutputValueCollection();

      outputValues.Add(new OutputValue("Name", Output.Name));
      outputValues.Add(new OutputValue("Url", Output.Url));
      outputValues.Add(new OutputValue("UserName", Output.UserName));
      outputValues.Add(new OutputValue("Password",Output.Password, true));
      outputValues.Add(new OutputValue("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser)));
      outputValues.Add(new OutputValue("FileName", Output.FileName));
      outputValues.Add(new OutputValue("FileFormat", Output.FileFormat));
      outputValues.Add(new OutputValue("LastProjectKey", Output.LastProjectKey));
      outputValues.Add(new OutputValue("LastIssueTypeID", Output.LastIssueTypeID.ToString()));
      outputValues.Add(new OutputValue("LastIssueID", Output.LastIssueID.ToString()));

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValueCollection OutputValues)
    {

      return new Output(OutputValues["Name", this.Name].Value,
                        OutputValues["Url", ""].Value, 
                        OutputValues["UserName", ""].Value,
                        OutputValues["Password", ""].Value, 
                        OutputValues["FileName", "Screenshot"].Value, 
                        OutputValues["FileFormat", ""].Value,
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)].Value),
                        OutputValues["LastProjectKey", string.Empty].Value,
                        Convert.ToInt32(OutputValues["LastIssueTypeID", "0"].Value),
                        Convert.ToInt32(OutputValues["LastIssueID", "1"].Value));

    }

    protected override async Task<V3.SendResult> Send(IWin32Window Owner, Output Output, V3.ImageData ImageData)
    {

      try
      {

        string userName = Output.UserName;
        string password = Output.Password;
        bool showLogin = string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password);
        bool rememberCredentials = false;

        string fileName = V3.FileHelper.GetFileName(Output.FileName, Output.FileFormat, ImageData);

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
              return new V3.SendResult(V3.Result.Canceled);
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
                return new V3.SendResult(V3.Result.Failed, projectsResult.FailedMessage);
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
                return new V3.SendResult(V3.Result.Failed, projectsResult.FailedMessage);
            }

            // Show send window
            Send send = new Send(Output.Url, Output.LastProjectKey, Output.LastIssueTypeID, Output.LastIssueID, projectsResult.Projects, issueTypesResult.IssueTypes, fileName);

            var ownerHelper = new System.Windows.Interop.WindowInteropHelper(send);
            ownerHelper.Owner = Owner.Handle;

            if (!send.ShowDialog() == true)
            {
              return new V3.SendResult(V3.Result.Canceled);
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
                  return new V3.SendResult(V3.Result.Failed, createIssueResult.FailedMessage);
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
                Result commentResult = await JiraRestProxy.AddCommentToIssue(Output.Url, userName, password, issueKey, send.Comment);
                switch (commentResult.Status)
                {
                  case ResultStatus.Success:
                    break;
                  case ResultStatus.LoginFailed:
                    showLogin = true;
                    continue;
                  case ResultStatus.Failed:
                    return new V3.SendResult(V3.Result.Failed, commentResult.FailedMessage);
                }
              }
              
            }

            string fullFileName = String.Format("{0}.{1}", send.FileName, V3.FileHelper.GetFileExtention(Output.FileFormat));
            string fileMimeType = V3.FileHelper.GetMimeType(Output.FileFormat);
            byte[] fileBytes = V3.FileHelper.GetFileBytes(Output.FileFormat, ImageData);

            // Add attachment to issue
            Result attachmentResult = await JiraRestProxy.AddAttachmentToIssue(Output.Url, userName, password, issueKey, fullFileName, fileBytes, fileMimeType);
            switch (attachmentResult.Status)
            {
              case ResultStatus.Success:
                break;
              case ResultStatus.LoginFailed:
                showLogin = true;
                continue;
              case ResultStatus.Failed:
                return new V3.SendResult(V3.Result.Failed, attachmentResult.FailedMessage);
            }


            // Open issue in browser
            if (Output.OpenItemInBrowser)
            {
              V3.WebHelper.OpenUrl(String.Format("{0}/browse/{1}", Output.Url, issueKey));
            }
            

            int issueID = Convert.ToInt32(issueKey.Split(new Char[]{'-'})[1]);
            return new V3.SendResult(V3.Result.Success,
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
        return new V3.SendResult(V3.Result.Failed, ex.Message);
      }

    }

  }
}
