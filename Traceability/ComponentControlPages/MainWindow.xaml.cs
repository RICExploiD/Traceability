using System;
using System.Windows;
using System.Configuration;

namespace Traceability
{
    using ObeserverPattern;
    using Traceability.Services;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWorkPageObserver, ISettingsObserver
    {
        ISettingsObservable SettingsWindow = new SettingsWindow();
        IWorkPageObservable observedWorkPage;
        BekoSickTCP Sick;
        BekoCOM COM;

        /// <summary>
        /// Logs controller
        /// </summary>
        BekoLogFile myLog { get; set; } = new BekoLogFile();
        public MainWindow()
        {
            InitializeComponent();

            // Init this window like the obesrver in WorkPageCore
            WorkPageCore.InitObserver(this);

            // Init settings window
            SettingsWindow.InitObserver(this);

            // Init logs event
            //which displays log-text in textbox-uielement
            myLog.LogEvent += ShowLog;

            // Init sql connection event
            Sql.ConnectionChange += SqlOnConnectionChange;
        }

        public void EventProductBarcodeReaded(string barcode)
        {
            // Readed barcode shoud be shown to user:
            // Immediately after barcode scan
            UpdateProductRowBarcodeLabel(barcode);

            // Clear linebreakers and save product barcode
            WorkPageCore.Product.ProductBarcode = barcode?.Trim();

            // Initilize washing machine details if it needs
            if (WorkPageCore.IsCurrentLineWM)
            {
                WorkPageCore.Product.InitWMDetails();
                ClearWMDetails();
            }

            // Verify does the scanned barcode is correct
            if (!ValidateScannedBarcode())
            {
                WorkPageCore.OperatorNotify("Продукт не отсканирован или отксанирован некорректно", OperNotifyType.Error);
                ClearTemporaryData();
                ClearWMDetails();
                observedWorkPage.ClearTemporaryData();
                UpdateProductBarcodeLabel("Ошибка");
                return;   
            }

            // Verify does the prod line match the product
            if (!IsCorrectProduct(out string productType))
            {
                WorkPageCore.OperatorNotify($"{productType}", OperNotifyType.Error);
                ClearTemporaryData();
                ClearWMDetails();
                observedWorkPage.ClearTemporaryData();
                return;
            }

            // Show product barcode to user
            UpdateProductBarcodeLabel(WorkPageCore.Product.ProductBarcode);

            // Init iot product type 
            WorkPageCore.Product.ProductIOTType = Sql.GetTypeOfIOT(WorkPageCore.Product.ProductNo);

            // Init product materials
            WorkPageCore.Product.ProductMaterial = Sql.GetProductMaterials(WorkPageCore.Product.ProductNo);

            // Data may be shown
            //optional - collapse both of labels
            UpdateModelBrandLabel(Sql.GetProductBrandMarketingCode(WorkPageCore.Product.ProductNo));
            UpdateIOTTypeLabel(WorkPageCore.Product.ProductIOTType);

            //Send to work page scanned barcode
            observedWorkPage?.ProductScanned();
        }
        public void ObserveOPCCangoPermit(bool cangoPermit)
        {
            WriteToOPCUATag("nOPCTagCanGo", cangoPermit);
            ObserveOPCCangoPermitLabel(cangoPermit);
        }
        private bool IsCorrectProduct(out string product)
        {
            product = Sql.GetProductType(WorkPageCore.Product.ProductNo);
            
            switch (product)
            {
                case "Refrigerator": return WorkPageCore.Line.Equals(ProductionLine.Refrigerator);
                case "Washing Machine": return WorkPageCore.Line.Equals(ProductionLine.WashingMachine);
                default: return false;
            }
        }
        public void ShowStopwatch(System.Diagnostics.Stopwatch sw) =>
            Dispatcher.Invoke(() => gExecutionTimeLabel.Content = $"{sw.ElapsedMilliseconds} ms");
        public void UpdateSettingsButton(ProductionLine line) => SetSettingsButton(line);
        public void UpdateWMDetailsView(bool isShown = true) => SetVisivilityWMDetails(isShown);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool ValidateScannedBarcode()
        {
            var isCaseBarcodeCorrect = WorkPageCore.IsCurrentLineWM &&
                BekoBarcode.BarcodeCSIsOk(WorkPageCore.Product.ProductBarcode);
            var isTubBarcodeCorrect = WorkPageCore.IsCurrentLineWM &&
                BekoBarcode.BarcodeTSIsOk(WorkPageCore.Product.ProductBarcode);
            var isStock22Correct = 
                BekoBarcode.Barcode22IsOk(WorkPageCore.Product.ProductBarcode);

            var isWMSpecialBarcodeCorrect = isCaseBarcodeCorrect || isTubBarcodeCorrect;

            var result = isStock22Correct || (WorkPageCore.IsCurrentLineWM && isWMSpecialBarcodeCorrect);

            if (isCaseBarcodeCorrect)
            {
                WorkPageCore.Product.BarcodeType = ProductBarcodeType.CS;

                WorkPageCore.Product.WMDetails.RDummy =
                    Sql.GetRightDummy(WorkPageCore.Product.ProductBarcode);
                WorkPageCore.Product.ProductNo = 
                    Sql.GetProductNoByCS(WorkPageCore.Product.ProductBarcode);

                UpdateRDummyTextBox(WorkPageCore.Product.WMDetails.RDummy);
            }
            else if (isTubBarcodeCorrect)
            {
                WorkPageCore.Product.BarcodeType = ProductBarcodeType.TS;

                WorkPageCore.Product.WMDetails.LDummy =
                    Sql.GetLeftDummyFromDummyCompare(WorkPageCore.Product.ProductBarcode);
                WorkPageCore.Product.ProductNo =
                    Sql.GetProductFromDummyCompare(WorkPageCore.Product.ProductBarcode);
                
                UpdateLDummyTextBox(WorkPageCore.Product.WMDetails.LDummy);
            }

            if (isStock22Correct)
            {
                WorkPageCore.Product.BarcodeType = ProductBarcodeType.Stock22;

                WorkPageCore.Product.ProductNo =
                    WorkPageCore.Product.ProductBarcode.Substring(0, 10);

                if (WorkPageCore.IsCurrentLineWM)
                {
                    WorkPageCore.Product.WMDetails.RDummy =
                        Sql.GetRightDummy(WorkPageCore.Product.ProductBarcode);
                    WorkPageCore.Product.WMDetails.LDummy =
                        Sql.GetLeftDummy(WorkPageCore.Product.ProductBarcode);
                }
            }

            UpdateProductNoTextBox(WorkPageCore.Product.ProductNo);

            return result;
        }
        /// <summary>
        /// Initialize <see cref="IWorkPageObserver"/> object and places it in <see cref="MainWindow"/> frame.
        /// </summary>
        private void InitWorkPage(IWorkPageObservable workPage)
        {
            observedWorkPage?.UnsubscribeFromEvents();

            observedWorkPage = workPage;

            EnterToNavigationWorkPage(observedWorkPage);
        }
        /// <summary>
        /// Inititalize <see cref="IWorkPageObservable"/> object and type-method of save component.
        /// Clear component data and operator notifies queue. 
        /// </summary>
        /// <returns>
        /// <see cref="IWorkPageObservable"/> object generated via config file.
        /// </returns>
        private IWorkPageObservable GetWorkPageFromConfig()
        {
            WorkPageCore.ClearComponentData();
            WorkPageCore.ClearOperatorNoitfyQueueData();

            InitTypeMethodOfSaveComponent();

            switch (WorkPageCore.SaveType)
            {
                case ComponentBarcodeType.MTK: return new MTKWorkPage.MTKWorkPage(GetBufferSizeFromConfig());
                case ComponentBarcodeType.Individual: return new IndividualWorkPage.IndividualWorkPage();
                default:
                    {
                        myLog.WriteLog("В конфиге указан недопустимый тип сохранения компонента после парсинга");
                        MessageBox.Show("Тип сохранения компонента не определен!\n" +
                            "Убедитесь в корректности данных конфигурации приложения",
                            "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return null;
                    }
            }
        }
        /// <summary>
        /// Place <see cref="LoginPage"/> in <see cref="MainWindow"/> frame and diconnect from component scanner.
        /// </summary>
        /// <param name="repsonseMessage">
        /// Needs to show response from operator login remote service.
        /// </param>
        private void EnterToLoginPage(string repsonseMessage = "")
        {
            WorkPageCore.DisconnectFromComponentScanner();
            WorkPageCore.StopOperatorNotifyProcess();

            gTraceabilityComponentFrame.NavigationService.Navigate(new LoginPage(repsonseMessage));
        }
        /// <summary>
        /// Initialization of <see cref="IWorkPageObservable"/> object, 
        /// place in <see cref="MainWindow"/> frame
        /// and diconnect from component scanner.
        /// </summary>
        private void EnterToWorkPage()
        {
            WorkPageCore.DisconnectFromComponentScanner();

            InitWorkPage(GetWorkPageFromConfig());

            EnterToNavigationWorkPage(observedWorkPage);
        }
        /// <summary>
        /// Reload work page of current point number was changed.
        /// </summary>
        private void ReloadWorkPage()
        {
            Enum.TryParse(ConfigurationManager.AppSettings["SaveType"], out ComponentBarcodeType saveType);
            int.TryParse(ConfigurationManager.AppSettings["BufferSize"], out int bufferSize);
            
            //TODO
            var isBufferLengthChanged = !WorkPageCore.BufferData?.Length.Equals(bufferSize) ?? false;
            var isSaveTypeChanged = !WorkPageCore.SaveType.Equals(saveType);
            var isSaveTypeMTK = WorkPageCore.SaveType.Equals(ComponentBarcodeType.MTK);
            var isPointNumberChanged = !WorkPageCore.CurrentPointNumber.Equals(LogOffPointNumber);

            if (isPointNumberChanged || isSaveTypeChanged || (isSaveTypeMTK && isBufferLengthChanged))
            {
                if (isBufferLengthChanged) 
                    myLog.WriteLog($"Изменение размера буфера с {WorkPageCore.BufferData?.Length} на {bufferSize}");
                if (isPointNumberChanged)
                    myLog.WriteLog($"Изменение производственной точки с {LogOffPointNumber} на {WorkPageCore.CurrentPointNumber}");
                if (isSaveTypeMTK && isSaveTypeChanged)
                    myLog.WriteLog($"Изменение типа сохранения с {WorkPageCore.SaveType} на {saveType}");

                myLog.WriteLog("Изменение исполнительного окна");

                if (isOperatorLoginOn) EnterToLoginPage();
                else
                {
                    LogOffPointNumber = WorkPageCore.CurrentPointNumber;
                    EnterToWorkPage();
                }
            }
        }

        #region Config interaction

        /// <summary>
        /// Init type-method of save component from config file.
        /// </summary>
        private void InitTypeMethodOfSaveComponent()
        {
            var parseResult = Enum.TryParse(ConfigurationManager.AppSettings["SaveType"], out ComponentBarcodeType saveType);
            if (!parseResult)
            {
                myLog.WriteLog("В конфиге указан недопустимый тип сохранения компонента");

                MessageBox.Show("Тип сохранения компонента неверно определен в конфигурации!\n" +
                    "Будет использовано значение по умолчанию.",
                    "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Warning);

                WorkPageCore.SaveType = ComponentBarcodeType.Individual;
            }

            WorkPageCore.SaveType = saveType;
        }
        /// <returns>
        /// Buffer size from config file.
        /// </returns>
        private int GetBufferSizeFromConfig()
        {
            var success = int.TryParse(ConfigurationManager.AppSettings["BufferSize"], out int bufferSize);
            if (bufferSize.Equals(0) && success)
            {
                myLog.WriteLog("В конфиге указано неверное значение для размера буфера");

                MessageBox.Show("Размер буфера неверно определен в конфигурации!\n" +
                    "Будет использовано значение по умолчанию.",
                    "Ошибка конфигурации", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return bufferSize;
        }
        #endregion

        #region Scanner interactions

        public void ReconnectToComponentScanner(ScanType scanType)
        {
            WorkPageCore.ReconnectToComponentScanner(scanType);
        }
        public void ReconnectToProductScanner(ScanType scanType)
        {
            DisconnectFromProductScanner();

            switch (scanType)
            {
                case ScanType.SickIP:
                    string ip = ConfigurationManager.AppSettings["ProductSickIP"];
                    int port = int.Parse(ConfigurationManager.AppSettings["ProductSickPort"].ToString());
                    Sick = new BekoSickTCP(ip, port, useModeBarcode22: false);
                    Sick.OnLogEvent += myLog.WriteLog;
                    Sick.OnConnectionChange += EventBarcodeOfProductConnectionChanged;
                    Sick.ReadBarcode += EventProductBarcodeReaded;
                    break;
                case ScanType.COM:
                    COM = new BekoCOM();
                    COM.OnLog += myLog.WriteLog;
                    COM.OnConnectionChange += EventBarcodeOfProductConnectionChanged;
                    COM.OnBarcodeReaded += EventProductBarcodeReaded;
                    COM.StartFromConfig("Product");
                    break;
                case ScanType.OPC:
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Disconnect from all available product scanners.
        /// </summary>
        private void DisconnectFromProductScanner()
        {
            COM?.Stop();
            Sick?.Stop();
        }
        #endregion
    }
}