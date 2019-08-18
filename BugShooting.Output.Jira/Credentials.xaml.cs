using System.Windows;
using System.Windows.Controls;

namespace BugShooting.Output.Jira
{
  partial class Credentials : Window
  {

    public Credentials(string url, string userName, string apiToken, bool remember)
    {
      InitializeComponent();

      Url.Text = url;
      UserNameTextBox.Text = userName;
      ApiTokenBox.Password = apiToken;
      RememberCheckBox.IsChecked = remember;

    }
    
    public string UserName
    {
      get { return UserNameTextBox.Text; }
    }
   
    public string ApiToken
    {
      get { return ApiTokenBox.Password; }
    }

    public bool Remember
    {
      get { return RememberCheckBox.IsChecked.Value; }
    }
  
    private void OK_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
    }

  }
}