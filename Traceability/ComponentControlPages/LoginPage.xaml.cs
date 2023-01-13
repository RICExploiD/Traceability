using System.Windows.Controls;

namespace Traceability
{
    using Traceability.Services;

    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage(string responseMessage = "")
        {
            InitializeComponent();
            ServiceResponse.Content = responseMessage;
        }
    }
}
