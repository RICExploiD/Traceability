using System.Windows;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Traceability.Services
{
    using ObeserverPattern;
    using Models;

    /// <summary>
    /// Implements main functionality to save traceability components, 
    /// connections to component scanners by IP and COM-port, operator notifications.
    /// Must contain interaction with the work page observer <see cref="IWorkPageObserver"/>.
    /// </summary>
    public static class WorkPageCore
    {
        #region Fields

        /// <summary>
        /// Product that needs to trace. Contains product barcode to save.
        /// </summary>
        public static BekoProduct Product { get; set; } = new BekoProduct();
        /// <summary>
        /// Component that needs to trace. Contains component barcode to save.
        /// </summary>
        public static BekoComponent Component { get; set; } = BekoComponent.CreateNew(CurrentPointNumber);
        /// <summary>
        /// Shoud contains parent observer MainWindow object.
        /// </summary>
        public static IWorkPageObserver Observer { get; private set; }
        /// <summary>
        /// Current assembly station number.
        /// </summary>
        public static int CurrentPointNumber { get; set; }
        /// <summary>
        /// Permit to pass further.
        /// </summary>
        public static bool CanGoPermit 
        { 
            get => _canGoPermit || AlwaysCanGoPermit;
            set
            {
                _canGoPermit = value;
                Observer?.ObserveOPCCangoPermit(_canGoPermit);
            }
        }
        private static bool _canGoPermit = default;
        /// <summary>
        /// Production assembly line that needs to validate correct save into database and display data.
        /// </summary>
        public static ProductionLine Line 
        { 
            get => _line;
            set 
            { 
                if (!_line.Equals(value)) ClearAllData();
                _line = value;
            }
        }
        private static ProductionLine _line;
        /// <summary>
        /// Represents does the application have permission to always send next.
        /// </summary>
        public static bool AlwaysCanGoPermit { private get; set; } = default;
        /// <summary>
        /// Washing machine validator varuable.
        /// </summary>
        public static bool IsCurrentLineWM { get => Line is ProductionLine.WashingMachine; }
        /// <summary>
        /// Type-method of saved component.
        /// </summary>
        public static ComponentBarcodeType SaveType { get; set; }
        /// <summary>
        /// Provide access to buffer data.
        /// </summary>
        public static string[] BufferData { get => GetBufferData?.Invoke(); }
        /// <summary>
        /// Value represents does the component material matches a product.
        /// </summary>
        public static bool IsMaterialMatchProduct { get { return !string.IsNullOrEmpty(Component.ComponentMaterial) && (Product.ProductMaterial?.Contains(Component.ComponentMaterial) ?? false); } }
        /// <summary>
        /// Gets configuration value that shows permission to save without match verification.
        /// </summary>
        private static bool SaveWithoutMatchVerification { get { return bool.Parse(ConfigurationManager.AppSettings["SaveWithoutMatchVerification"] ?? "false") ; } }
        /// <summary>
        /// Sick scanner device data access object. Connection by IP adress.
        /// </summary>
        private static BekoSickTCP Sick { get; set; }
        /// <summary>
        /// COM-port data access object. Connection by COM-port adress.
        /// </summary>
        private static BekoCOM COM { get; set; }
        /// <summary>
        /// Queue of operator notifies styles and messages.
        /// </summary>
        public static ConcurrentQueue<OperatorNotifyStyle> OperatorNotifyQueue { get; private set; } = new ConcurrentQueue<OperatorNotifyStyle>();
        public static bool OperatorProcessorIsCanceled { get; private set; } = true;
        #endregion

        static WorkPageCore() 
        { 
            InitConfigData();
            InitOperatorNotifyProcess(); 
        }

        #region Events and delegates

        /// <summary>
        /// Event that implement operator notification.
        /// </summary>
        public static event OperatorNotifyActionDelegate OperatorNotifyAction;
        public delegate void OperatorNotifyActionDelegate(OperatorNotifyStyle popElement);
        /// <summary>
        /// Processor that invoke operator notification event via operator notify queue
        /// </summary>
        private static void InitOperatorNotifyProcess()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Task.Delay(500).GetAwaiter().GetResult();
                    if (OperatorProcessorIsCanceled) continue;
                    if (OperatorNotifyQueue.TryDequeue(out OperatorNotifyStyle popElement))
                    {
                        OperatorNotifyAction?.Invoke(popElement);
                        Task.Delay(1000).GetAwaiter().GetResult();
                    }
                }
            });
        }
        public static void StartOperatorNotifyProcess() { OperatorProcessorIsCanceled = false; }
        public static void StopOperatorNotifyProcess() { OperatorProcessorIsCanceled = true; }

        /// <summary>
        /// Event that collect buffer of saved component barcodes.
        /// </summary>
        public static event BufferShowDataDelegate GetBufferData;
        public delegate string[] BufferShowDataDelegate();

        /// <summary>
        /// Is the scanned component are correct.
        /// </summary>
        public static event IsComponentBarcodeCorrectDelegate IsComponentCorrectEvent;
        public delegate bool IsComponentBarcodeCorrectDelegate();

        /// <summary>
        /// Permit to save component.
        /// </summary>
        public static event CanSavePermitDelegate CanSavePermit;
        public delegate bool CanSavePermitDelegate();

        /// <summary>
        /// Event that must invokes when component data was scanned and received from scanner device.
        /// </summary>
        public static event ComponentBarcodeScannedEvent ComponentScannedEvent;
        public delegate void ComponentBarcodeScannedEvent(string barcode);

        private static void ComponentScanned(string barcode)
        {
            Observer?.ObserveScannedComponent(barcode);

            ComponentScannedEvent?.Invoke(barcode);

            //Observer?.ObserveOPCCangoPermit(CanGoPermit);
        }
        #endregion

        #region Core interaction (public methods)

        /// <summary>
        /// Initialize observer object and restore composition objects.
        /// </summary>
        public static void InitObserver(IWorkPageObserver observer)
        { 
            Observer = observer;
            ClearAllData();
        }
        /// <summary>
        /// Init current assembly station number, assembly line
        /// and "always can go permit" option
        /// from config file.
        /// </summary>
        public static void InitConfigData()
        {
            try
            {
                AlwaysCanGoPermit = bool.Parse(AppSettings.GetOption("AlwaysCanGoPermit"));
                CurrentPointNumber = int.Parse(AppSettings.GetOption("Point"));

                Line = (ProductionLine)System.Enum
                    .Parse(typeof(ProductionLine), AppSettings.GetOption("ProductionLine"));

                Observer?.UpdateSettingsButton(Line);
                Observer?.UpdateWMDetailsView(IsCurrentLineWM);
            }
            catch (ConfigArgumentNullException ex) { ex.ShowErrorMessage(); }
        }

        #region ClearData funcs

        /// <summary>
        /// Restore properties:
        /// <list>
        /// <para><see cref="Product"/></para> 
        /// <para><see cref="Component"/></para>
        /// <para><see cref="OperatorNotifyQueue"/></para>
        /// <para><see cref="CanGoPermit"/></para>
        /// </list>
        /// </summary>
        public static void ClearAllData()
        {
            ClearProductData();
            ClearComponentData();
            ClearOperatorNoitfyQueueData();
        }
        /// <summary>
        /// Restore <see cref="Product"/> and <see cref="CanGoPermit"/> variable.
        /// </summary>
        public static void ClearProductData()
        {
            CanGoPermit = default;

            Product = new BekoProduct();
        }
        /// <summary>
        /// Restore <see cref="Component"/>.
        /// </summary>
        public static void ClearComponentData()
        {
            Component = BekoComponent.CreateNew(CurrentPointNumber);
        }
        /// <summary>
        /// Restore <see cref="OperatorNotifyQueue"/>.
        /// </summary>
        public static void ClearOperatorNoitfyQueueData()
        {
            OperatorNotifyQueue = new ConcurrentQueue<OperatorNotifyStyle>();
        }
        #endregion

        public static void ButtonPressedToSave()
        {
            OperatorNotify("Нажата кнопка сохранения", OperNotifyType.Info);
            SaveComponent();
            //Observer?.ObserveOPCCangoPermit(CanGoPermit);
        }
        public static void ButtonPressedToSkip()
        {
            OperatorNotify("Нажата кнопка отправки без сохранения", OperNotifyType.Attention);
            Observer?.ObserveOPCCangoPermit();
        }
        public static void SaveComponent()
        {
            if (!CanSavePermit.Invoke())
            {
                OperatorNotify("Компонент не будет сохранен", OperNotifyType.Attention);
                CanGoPermit = false;
                return;
            }

            if (!IsComponentCorrectEvent.Invoke())
            {
                OperatorNotify("Попытка сохранения без компонента", OperNotifyType.Attention);
                CanGoPermit = false;
                return;
            }

            if (!BekoBarcode.Barcode22IsOk(Product.ProductBarcode) &&
                !BekoBarcode.BarcodeCSIsOk(Product.ProductBarcode) &&
                !BekoBarcode.BarcodeTSIsOk(Product.ProductBarcode))
            {
                OperatorNotify("Попытка сохранения без продукта", OperNotifyType.Attention);
                CanGoPermit = false;
                return;
            }

            UpdateCanGoPermit();

            if (CanGoPermit)
            {
                OperatorNotify($"{Component.ComponentBarcode} уже сохранен", OperNotifyType.Ok);
                CanGoPermit = true;
                return;
            }

            if (IsCurrentLineWM && !Product.WMDetails.IsCorrectRDummy)
            { 
                OperatorNotify("RDummy не найден", OperNotifyType.Error);
                CanGoPermit = false;
                return;
            }

            if (SaveWithoutMatchVerification || IsMaterialMatchProduct) SaveToSql();

            UpdateCanGoPermit();

            if (CanGoPermit)
                OperatorNotify($"{Component.ComponentBarcode} успешно сохранен", OperNotifyType.Ok);
            else
                OperatorNotify("Компонент не сохранен", OperNotifyType.Error);
        }
        private static void UpdateCanGoPermit()
        {
            switch (Line)
            {
                case ProductionLine.Refrigerator:
                    CanGoPermit =
                        Sql.DoesExistInKKIMatchByBarcode(Product.ProductBarcode, Component.ComponentBarcode);
                    break;
                case ProductionLine.WashingMachine:
                    CanGoPermit =
                        Sql.DoesExistInTkktsFixedBarcodeByBarcode(Component.ComponentBarcode, Product.WMDetails.RDummy) &&
                        Product.WMDetails.IsCorrectRDummy;
                    break;
            }
        }
        private static void SaveToSql()
        {
            switch (Line)
            {
                case ProductionLine.Refrigerator:
                    {
                        var exist = Sql.DoesExistInKKIMatchByComponentCode(
                            Product.ProductBarcode,
                            Component.ComponentCode);

                        if (exist)
                        {
                            Sql.UpdateKKIMatch(Product.ProductBarcode, Component);
                            Observer?.ObserveLogAction($"Данные по {Component.ComponentCode} обновлены");
                            return;
                        }

                        Sql.AddToKKIMatch(Product.ProductBarcode, Component); 
                        
                        break;
                    }
                case ProductionLine.WashingMachine:
                    {
                        switch (Product.BarcodeType)
                        {
                            case ProductBarcodeType.CS:
                                {
                                    var exist = Sql.DoesExistInTkktsFixedBarcodeByLocation(
                                        Product.ProductBarcode,
                                        Product.WMDetails.RDummy,
                                        CurrentPointNumber);

                                    if (exist)
                                    {
                                        Sql.UpdateTkktsFixedBarcode(Product, Component, CurrentPointNumber);
                                        Observer?.ObserveLogAction($"Данные по {Component.ComponentCode} обновлены");
                                        return;
                                    }

                                    Sql.AddToTkktsFixedBarcode(Product, Component, CurrentPointNumber);
                                    break;
                                }
                            case ProductBarcodeType.TS:
                                {

                                    break;
                                }
                        }

                        break;
                    }
            }
        }
        public static void DisconnectFromComponentScanner()
        {
            try
            {
                Sick?.Stop();
                COM?.Stop();
            }
            catch { }
        }
        public static void ReconnectToComponentScanner(ScanType scanTypes)
        {
            DisconnectFromComponentScanner();

            if (Observer is null) return;

            switch (scanTypes)
            {
                case ScanType.SickIP:
                    string ip = ConfigurationManager.AppSettings["ComponentSickIP"];
                    int port = int.Parse(ConfigurationManager.AppSettings["ComponentSickPort"].ToString());
                    Sick = new BekoSickTCP(ip, port, useModeBarcode22: false);
                    Sick.OnLogEvent += Observer.ObserveLogAction;
                    Sick.OnConnectionChange += Observer.ObserveSickConnection;
                    Sick.ReadBarcode += ComponentScanned;
                    break;
                case ScanType.COM:
                    COM = new BekoCOM();
                    COM.OnLog += Observer.ObserveLogAction;
                    COM.OnConnectionChange += Observer.ObserveCOMConnection;
                    COM.OnBarcodeReaded += ComponentScanned;
                    COM.StartFromConfig("Component");
                    break;
                case ScanType.OPC:
                    break;
                default:
                    break;
            }
        }
        public static void OperatorNotify(string message, OperNotifyType notify, bool logging = true)
        {
            if (logging) Observer?.ObserveLogAction(message);

            switch (notify)
            {
                case OperNotifyType.Ok:
                    {
                        OperatorNotifyQueue.Enqueue(new OperatorNotifyStyle()
                        { 
                            Background = (Style)Application.Current.Resources["OperatorOkLabel"],
                            Foreground = (Style)Application.Current.Resources["OperatorWhiteTextLabel"],
                            Message = message
                        });
                        break;
                    }
                case OperNotifyType.Info:
                    {
                        OperatorNotifyQueue.Enqueue(new OperatorNotifyStyle()
                        {
                            Background = (Style)Application.Current.Resources["OperatorInfoLabel"],
                            Foreground = (Style)Application.Current.Resources["OperatorBlackTextLabel"],
                            Message = message
                        });
                        break;
                    }
                case OperNotifyType.Attention:
                    {
                        OperatorNotifyQueue.Enqueue(new OperatorNotifyStyle()
                        {
                            Background = (Style)Application.Current.Resources["OperatorAttentionLabel"],
                            Foreground = (Style)Application.Current.Resources["OperatorBlackTextLabel"],
                            Message = message
                        });
                        break;
                    }
                case OperNotifyType.Error:
                    {
                        OperatorNotifyQueue.Enqueue(new OperatorNotifyStyle()
                        {
                            Background = (Style)Application.Current.Resources["OperatorBadLabel"],
                            Foreground = (Style)Application.Current.Resources["OperatorWhiteTextLabel"],
                            Message = message
                        });
                        break;
                    }
            }
        }
        #endregion
    }
}
