using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.Threading.Tasks;

namespace Traceability
{
    using SmartCard;
    using CCureService;
    using OperatorLoginService;
    using Services;

    partial class MainWindow : Window
    {
        private CardReaderInfo reader;
        private int LogOffPointNumber { get; set; }
        private bool isOperatorLoginOn { get { return bool.Parse(ConfigurationManager.AppSettings["isOperatorLoginOn"]); } }
        private void CardReaderEvent(string cardData)
        {
            if (string.IsNullOrEmpty(cardData))
            {
                WorkPageCore.Observer?.ObserveLogAction("--- Logout ---");
                OperatorLogout();
            }
            else
            {
                WorkPageCore.Observer?.ObserveLogAction("--- Login ---");
                OperatorLogin(cardData);
            }
        }
        private void OperatorLogout()
        {
            Dispatcher.Invoke(() =>
            {
                EnterToLoginPage();
                new BasicHttpBinding_IOperatorLogin().CloseSession(1, true, LogOffPointNumber, true);
            });
        }
        private void OperatorLogin(string cardData)
        {
            if (WorkPageCore.CurrentPointNumber.Equals(0))
            {
                myLog.WriteLog("В конфиге указано недопустимое значение производственной точки");
                MessageBox.Show("Указанная точка не определена!\n" +
                    "Убедитесь в корректности данных конфигурации приложения",
                    "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            LogOffPointNumber = WorkPageCore.CurrentPointNumber;

            Dispatcher.InvokeAsync(() =>
            {
                var pageName = (gTraceabilityComponentFrame.Content as Page).Title;

                var loginPageName = typeof(LoginPage).Name;

                if (pageName.Equals(loginPageName))
                {
                    var OperatorCardData = string.IsNullOrEmpty(cardData) ? null :
                        new BasicHttpBinding_ICcureService().GetEmployeeInfo(cardData).ToDictionary(x => x.Key, x => x.Value);

                    var OpBR = OperatorCardData?["PersonnelNumber"];
                    var OpName = OperatorCardData?["FirstNameRu"];
                    var OpFamily = OperatorCardData?["LastNameRu"];

                    var response = new BasicHttpBinding_IOperatorLogin().StartSession(1, true, WorkPageCore.CurrentPointNumber, true, OpBR);

                    if (string.IsNullOrEmpty(response))
                    {
                        InitWorkPage(GetWorkPageFromConfig());
                        gOperatorLabel.Content = $"{OpFamily} {OpName} - {OpBR}";
                    }
                    else { EnterToLoginPage(response); }
                }
            });
        }
        private void CardReaderInit()
        {
            LogOffPointNumber = WorkPageCore.CurrentPointNumber;

            if (!isOperatorLoginOn)
            {
                InitWorkPage(GetWorkPageFromConfig());
                return;
            }

            Dispatcher.Invoke(() =>
            {
                Task.Run(() =>
                {
                    reader = new CardReaderInfo();

                    reader.Presence = (reader, state, card) => CardReaderEvent(card);

                    reader.StartMonitoring();
                });
            });
        }
    }
}