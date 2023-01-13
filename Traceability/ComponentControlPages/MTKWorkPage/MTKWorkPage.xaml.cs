using System;
using System.Linq;
using System.Windows.Controls;

namespace Traceability.MTKWorkPage
{
    using ObeserverPattern;
    using Services;
    using Models;

    public partial class MTKWorkPage : Page, IWorkPageObservable
    {
        /// <summary>
        /// Plan of components to save.
        /// <para>
        /// For example in case:
        /// <para>
        /// MTK contains quantity of components that stocks on the pallet.
        /// </para>
        /// </para>
        /// </summary>
        private int QuantityOfPlannedComponent { get; set; } = 0;
        /// <summary>
        /// Quantity of components that already saved in datatable.
        /// </summary>
        private int QuantityOfSavedComponents { get; set; }
        /// <summary>
        /// Represents difference between <paramref name="QuantityOfPlannedComponent"/> and <paramref name="QuantityOfSavedComponents"/>.
        /// </summary>
        private int QuantityOfLeftComponents { get { return QuantityOfPlannedComponent - QuantityOfSavedComponents; } }
        /// <summary>
        /// Buffer of MTK barcodes.
        /// </summary>
        private string[] MTKBarcodesBuffer { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferSize">
        /// Size of creating MTK buffer storage. Values of buffer size less than 1 shoud changed to 1.
        /// </param>
        public MTKWorkPage(int bufferSize = 1) 
        { 
            InitializeComponent();

            InitilizeMTKBuffer(bufferSize);
            WorkPageCore.ComponentScannedEvent += MTKScanned;
            WorkPageCore.IsComponentCorrectEvent += IsMTKCorrect;
            WorkPageCore.CanSavePermit += CanSaveMTK;
            WorkPageCore.GetBufferData += () => MTKBarcodesBuffer;
            WorkPageCore.OperatorNotifyAction += SetOperatorNotify;
        }

        #region Observable Functional (public methods)

        public void UnsubscribeFromEvents()
        {
            WorkPageCore.ComponentScannedEvent -= MTKScanned;
            WorkPageCore.IsComponentCorrectEvent -= IsMTKCorrect;
            WorkPageCore.CanSavePermit -= CanSaveMTK;
            WorkPageCore.GetBufferData -= () => MTKBarcodesBuffer;
            WorkPageCore.OperatorNotifyAction -= SetOperatorNotify;
        }
        public void ProductScanned()
        {
            UpdateAwaitedMaterials(WorkPageCore.Product.ProductMaterial);

            InitializeCorrespondMTK();

            OperatorNotifyMaterialsMatch(WorkPageCore.IsMaterialMatchProduct);

            WorkPageCore.Observer.ObserveLogAction("Попытка сохранить сразу после сканирования продукта");
            WorkPageCore.SaveComponent();

            CanSaveNotify();

            UpdateCounters();

            WorkPageCore.Observer.ObserveOPCCangoPermit(WorkPageCore.CanGoPermit);
        }
        public void ClearTemporaryData()
        {
            WorkPageCore.ClearProductData();
            ClearOperatorNotify();
            ClearAwaitedMaterials();
            ClearMTKDataMatchBackground();
        }
        #endregion

        #region WorkPage main encapsulated functional (private methods)

        private void MTKScanned(string barcode)
        {
            if (BekoBarcode.Barcode22IsOk(barcode) || 
                BekoBarcode.BarcodeCSIsOk(barcode) || 
                BekoBarcode.BarcodeTSIsOk(barcode))
            {
                WorkPageCore.Observer.ObserveLogAction("Продукт отсканирован сканером для компонента");
                WorkPageCore.Observer.EventProductBarcodeReaded(barcode);
                return;
            }

            if (BekoBarcode.BarcodeMicIsOk(barcode))
            {
                WorkPageCore.OperatorNotify("MTK отсканирован", OperNotifyType.Ok);

                ParseMTK(barcode);

                if (!AddMTKToBuffer())
                {
                    WorkPageCore.OperatorNotify("Буфер полностью заполнен МТК добавлен не будет", OperNotifyType.Error);
                    return;
                }

                InitializeCorrespondMTK();
                
                UpdateCounters();
                

                OperatorNotifyMaterialsMatch(WorkPageCore.IsMaterialMatchProduct);

                WorkPageCore.Observer.ObserveLogAction("Попытка сохранить сразу после сканирования МТК");
                WorkPageCore.SaveComponent();
                
                CanSaveNotify();

                UpdateCounters();
            }
            else
            {
                WorkPageCore.OperatorNotify("MTK штрих-код неверный", OperNotifyType.Error);

                UpdateMTKData("Ошибка", "0");

                WorkPageCore.CanGoPermit = false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void UpdateCounters()
        {
            if (!IsMTKCorrect()) return;

            UpdateMTKData(WorkPageCore.Component.ComponentMaterialModel, QuantityOfPlannedComponent.ToString());

            switch (WorkPageCore.Line)
            {
                case ProductionLine.Refrigerator:
                    QuantityOfSavedComponents = Sql.GetQuantityOfSavedMTKComponentsREF(WorkPageCore.Component.ComponentBarcode);
                    break;
                case ProductionLine.WashingMachine:
                    QuantityOfSavedComponents = Sql.GetQuantityOfSavedMTKComponentsWM(WorkPageCore.Component.ComponentBarcode);
                    break;
            }

            UpdateMTKCounters(QuantityOfSavedComponents, QuantityOfLeftComponents);
        }
        /// <summary>
        /// 
        /// </summary>
        private void CanSaveNotify()
        {
            if (!IsMTKCorrect())
            {
                WorkPageCore.Observer.ObserveLogAction($"МТК не отсканирован");
                WorkPageCore.OperatorNotify("Отсканируйте МТК!", OperNotifyType.Error, false);
            }
            else if (!CanSaveMTK())
            {
                WorkPageCore.Observer.ObserveLogAction($"Компоненты по {WorkPageCore.Component.ComponentBarcode} в количестве {QuantityOfPlannedComponent} полностью сохранены в базе.");
                WorkPageCore.OperatorNotify("Компоненты по МТК закончились - коробка пуста. Отсканируйте другую!", OperNotifyType.Error, false);
                DeleteCurrentMTK();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void DeleteCurrentMTK()
        {
            for (var index = 0; index < MTKBarcodesBuffer.Length; index++)
                if (MTKBarcodesBuffer[index].Equals(WorkPageCore.Component.ComponentBarcode))
                    MTKBarcodesBuffer[index] = string.Empty;

            WorkPageCore.OperatorNotify($"{WorkPageCore.Component.ComponentBarcode} был удален из буфера.", OperNotifyType.Info);
        }
        /// <summary>
        /// Incapsulate <see cref="BekoBarcode.BarcodeMicIsOk(string)">MTK barcode validation method</see>.
        /// </summary>
        private bool IsMTKCorrect() => BekoBarcode.BarcodeMicIsOk(WorkPageCore.Component.ComponentBarcode);
        /// <returns>
        /// Is not MTK completely used up.
        /// </returns>
        private bool CanSaveMTK() => QuantityOfLeftComponents > 0;
        /// <summary>
        /// Parse input MTK barcode to component material and items quantity. 
        /// Set component barcode and component material values into 
        /// <see cref="WorkPageCore.Component"/> (<see cref="BekoComponent"/> object).
        /// </summary>
        private void ParseMTK(string barcode)
        {
            barcode = barcode.Trim();
            WorkPageCore.Component.ComponentBarcode = barcode;

            var barcodeSplited = barcode.Split('+');
            WorkPageCore.Component.ComponentMaterial = barcodeSplited[0];
            QuantityOfPlannedComponent = Int32.Parse(barcodeSplited[2]);
        }
        /// <summary>
        /// Create buffer with buffer size and fill it with empty strings
        /// </summary>
        private void InitilizeMTKBuffer(int bufferSize)
        {
            MTKBarcodesBuffer = new string[bufferSize < 1 ? 1 : bufferSize];

            for (var index = 0; index < MTKBarcodesBuffer.Length; index++) MTKBarcodesBuffer[index] = string.Empty;
        }
        /// <summary>
        /// 
        /// </summary>
        private void InitializeCorrespondMTK()
        {
            if (WorkPageCore.IsMaterialMatchProduct) return;

            for (var index = 0; index < MTKBarcodesBuffer.Length; index++)
            {
                if (string.IsNullOrEmpty(MTKBarcodesBuffer[index])) continue;

                WorkPageCore.Component.ComponentBarcode = MTKBarcodesBuffer[index];
                
                ParseMTK(WorkPageCore.Component.ComponentBarcode);

                if (WorkPageCore.IsMaterialMatchProduct) return;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private bool AddMTKToBuffer()
        {
            if (MTKBarcodesBuffer.Contains(WorkPageCore.Component.ComponentBarcode)) return true;

            if (MTKBarcodesBuffer.Length.Equals(1))
            { 
                MTKBarcodesBuffer[0] = WorkPageCore.Component.ComponentBarcode;
                return true;
            }

            for (var index = 0; index < MTKBarcodesBuffer.Length; index++)
            {
                if (MTKBarcodesBuffer[index].Contains(WorkPageCore.Component.ComponentMaterial))
                {
                    WorkPageCore.OperatorNotify($"МТК c {WorkPageCore.Component.ComponentMaterialModel} был обновлен", OperNotifyType.Ok, false);
                    WorkPageCore.Observer.ObserveLogAction($"МТК {MTKBarcodesBuffer[index]} заменен на {WorkPageCore.Component.ComponentBarcode}");
                    
                    MTKBarcodesBuffer[index] = WorkPageCore.Component.ComponentBarcode;
                    return true;
                }
            }

            for (var index = 0; index < MTKBarcodesBuffer.Length; index++)
            {
                if (string.IsNullOrEmpty(MTKBarcodesBuffer[index]))
                {
                    WorkPageCore.OperatorNotify($"Добавлен MTK {WorkPageCore.Component.ComponentMaterialModel}", OperNotifyType.Ok, false);
                    WorkPageCore.Observer.ObserveLogAction($"Добавлен MTK {WorkPageCore.Component.ComponentBarcode}");

                    MTKBarcodesBuffer[index] = WorkPageCore.Component.ComponentBarcode;
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
