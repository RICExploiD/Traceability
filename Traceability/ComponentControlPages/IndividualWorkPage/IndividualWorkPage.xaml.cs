using System.Linq;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Traceability.IndividualWorkPage
{
    using ObeserverPattern;
    using Services;
    using Models;

    public partial class IndividualWorkPage : Page, IWorkPageObservable
    {
        public IndividualWorkPage() 
        { 
            InitializeComponent();
            WorkPageCore.ComponentScannedEvent += ComponentScanned;
            WorkPageCore.IsComponentCorrectEvent += () => !string.IsNullOrEmpty(WorkPageCore.Component.ComponentBarcode);
            WorkPageCore.CanSavePermit += () => WorkPageCore.IsMaterialMatchProduct;
            WorkPageCore.OperatorNotifyAction += SetOperatorNotify;
        }

        #region Observable Functional (public methods)

        public void UnsubscribeFromEvents()
        {
            WorkPageCore.ComponentScannedEvent -= ComponentScanned;
            WorkPageCore.IsComponentCorrectEvent -= () => !string.IsNullOrEmpty(WorkPageCore.Component.ComponentBarcode);
            WorkPageCore.CanSavePermit -= () => WorkPageCore.IsMaterialMatchProduct;
            WorkPageCore.OperatorNotifyAction -= SetOperatorNotify;
        }
        public void ProductScanned()
        {
            UpdateAwaitedMaterials(WorkPageCore.Product.ProductMaterial);

            OperatorSaveNotify();
        }
        public void ClearTemporaryData()
        {
            ClearComponent();
            ClearOperatorNotify();
            ClearAwaitedMaterials();
            ClearProductAndComponentData();
            ClearComponentMaterialMatchBackground();
        }
        #endregion

        #region WorkPage main encapsulated functional (private methods)

        private void ComponentScanned(string barcode)
        {
            if (BekoBarcode.Barcode22IsOk(barcode) ||
                BekoBarcode.BarcodeCSIsOk(barcode) ||
                BekoBarcode.BarcodeTSIsOk(barcode))
            {
                WorkPageCore.Observer.ObserveLogAction("Продукт отсканирован сканером для компонента");
                WorkPageCore.Observer.EventProductBarcodeReaded(barcode);
                return;
            }

            if (!string.IsNullOrEmpty(barcode))
            {
                WorkPageCore.OperatorNotify("Компонент отсканирован", OperNotifyType.Ok);

                WorkPageCore.Component.ComponentBarcode = barcode.Trim();
                
                var approximateMaterials = GenerateApproximateMaterials(WorkPageCore.Component.ComponentBarcode).ToArray();
                var material = 
                    Sql.SafeQueryInvoke(() => 
                    Sql.GetTrueMaterial(approximateMaterials), 
                    WorkPageCore.Observer.ObserveLogAction, out bool success);
                
                WorkPageCore.Component.ComponentMaterial = success ? material : null ;

                UpdateComponentBarcode(WorkPageCore.Component.ComponentBarcode);
                UpdateComponentMaterial(WorkPageCore.Component.ComponentMaterialModel);

                OperatorNotifyMaterialsMatch(WorkPageCore.IsMaterialMatchProduct);

                if (WorkPageCore.IsMaterialMatchProduct)
                {
                    WorkPageCore.Observer.ObserveLogAction("Попытка сохранить сразу после сканирования компонента");
                    WorkPageCore.SaveComponent();
                }
                else
                {
                    WorkPageCore.CanGoPermit = false;
                }
            }
            else
            {
                WorkPageCore.OperatorNotify("Штрих-код компонента неверный", OperNotifyType.Error);

                UpdateComponentBarcode("Ошибка");
                UpdateComponentMaterial("Ошибка");

                WorkPageCore.CanGoPermit = false;
            }
        }

        private void OperatorSaveNotify()
        {
            if (string.IsNullOrEmpty(WorkPageCore.Component.ComponentBarcode))
                WorkPageCore.OperatorNotify("Отсканируйте компонент!", OperNotifyType.Info);
        }

        private void ClearProductAndComponentData()
        {
            WorkPageCore.Product = new BekoProduct();
            WorkPageCore.Component.ComponentBarcode = null;

            WorkPageCore.CanGoPermit = false;
        }

        private IEnumerable<string> GenerateApproximateMaterials(string componentBarcode)
        {
            var lastIndexOfApproximateMaterial = componentBarcode.Length - 1 - 10;
            var result = new List<string>();

            if (componentBarcode.Length - 1 < 10 || lastIndexOfApproximateMaterial <= 0) 
                return new List<string>() { string.Empty };

            for (var index = 0; index <= lastIndexOfApproximateMaterial; index++)
                result.Add(componentBarcode.Substring(index, 10));

            return result;
        }
        #endregion
    }
}
