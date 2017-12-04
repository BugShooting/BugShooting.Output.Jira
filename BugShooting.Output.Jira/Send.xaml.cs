using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace BugShooting.Output.Jira
{
  partial class Send : Window
  {

    JiraRestIssueTypes issueTypes;

    public Send(string url, string lastProjectKey, int lastIssueTypeID, int lastIssueID, List<JiraRestProject> projects, JiraRestIssueTypes issueTypes, string fileName)
    {
      InitializeComponent();

      this.issueTypes = issueTypes;
      
      Url.Text = url;
      NewIssue.IsChecked = true;

      ProjectComboBox.ItemsSource = projects;
      ProjectComboBox.SelectedValue = lastProjectKey;

      if (ProjectComboBox.SelectedValue != null)
      {
        IssueTypeComboBox.SelectedValue = lastIssueTypeID;
      }

      IssueIDTextBox.Text = lastIssueID.ToString();
      FileNameTextBox.Text = fileName;

      ProjectComboBox.SelectionChanged += ValidateData;
      IssueTypeComboBox.SelectionChanged += ValidateData;
      SummaryTextBox.TextChanged += ValidateData;
      DescriptionTextBox.TextChanged += ValidateData;
      IssueIDTextBox.TextChanged += ValidateData;
      FileNameTextBox.TextChanged += ValidateData;
      ValidateData(null, null);

    }

    public bool CreateNewIssue
    {
      get { return NewIssue.IsChecked.Value; }
    }
 
    public string ProjectKey
    {
      get { return (string)ProjectComboBox.SelectedValue; }
    }

    public int IssueTypeID
    {
      get { return (int)IssueTypeComboBox.SelectedValue; }
    }

    public string Summary
    {
      get { return SummaryTextBox.Text; }
    }

    public string Description
    {
      get { return DescriptionTextBox.Text; }
    }

    public int IssueID
    {
      get { return Convert.ToInt32(IssueIDTextBox.Text); }
    }

    public string Comment
    {
      get { return CommentTextBox.Text; }
    }

    public string FileName
    {
      get { return FileNameTextBox.Text; }
    }

    private void NewIssue_CheckedChanged(object sender, EventArgs e)
    {

      if (NewIssue.IsChecked.Value)
      {
        IssueTypeControls.Visibility = Visibility.Visible;
        SummaryControls.Visibility = Visibility.Visible;
        DescriptionControls.Visibility = Visibility.Visible;
        IssueIDControls.Visibility = Visibility.Collapsed;
        CommentControls.Visibility = Visibility.Collapsed;

        SummaryTextBox.SelectAll();
        SummaryTextBox.Focus();
      }
      else
      {
        IssueTypeControls.Visibility = Visibility.Collapsed;
        SummaryControls.Visibility = Visibility.Collapsed;
        DescriptionControls.Visibility = Visibility.Collapsed;
        IssueIDControls.Visibility = Visibility.Visible;
        CommentControls.Visibility = Visibility.Visible;

        IssueIDTextBox.SelectAll();
        IssueIDTextBox.Focus();
      }

      ValidateData(null, null);

    }

    private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    
      if (ProjectComboBox.SelectedItem is null)
      {
        IssueTypeComboBox.ItemsSource = null;
      }
      else
      {

        string projectKey = ((JiraRestProject)ProjectComboBox.SelectedItem).Key;

        foreach (JiraRestIssueTypesProject issueTypesProject in issueTypes.Projects)
        {

          if (issueTypesProject.ProjectKey.Equals(projectKey))
          {
            List<JiraRestIssueType> useIssueTypes = new List<JiraRestIssueType>();

            foreach (JiraRestIssueType issueType in issueTypesProject.IssueTypes)
            {
              if (!issueType.SubTask)
              {
                useIssueTypes.Add(issueType);
              }
            }

            IssueTypeComboBox.ItemsSource = useIssueTypes;
            
            break;
          }
        }

      }

    }

    private void IssueID_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
    }
    
    private void ValidateData(object sender, EventArgs e)
    {
      OK.IsEnabled = Validation.IsValid(ProjectComboBox) && 
                     ((CreateNewIssue && Validation.IsValid(IssueTypeComboBox) && Validation.IsValid(SummaryTextBox) && Validation.IsValid(DescriptionTextBox)) ||
                      (!CreateNewIssue && Validation.IsValid(IssueIDTextBox))) &&
                     Validation.IsValid(FileNameTextBox);
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
    }
       
  }

}
