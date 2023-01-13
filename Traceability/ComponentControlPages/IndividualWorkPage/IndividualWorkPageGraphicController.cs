using System.Windows;
using System.Windows.Media;

namespace Traceability.IndividualWorkPage
{
    using Models;
    using Services;

    partial class IndividualWorkPage
    {
        private void UpdateComponentBarcode(string componentBarcode)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentBarcodeLabel.Text = componentBarcode;
            });
        }
        private void UpdateComponentMaterial(string componentMaterial)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentMaterialLabel.Text = componentMaterial;
            });
        }
        private void UpdateComponentMaterialMatchBackground(bool isMatched)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentMaterialLabel.Foreground = isMatched ? Brushes.Green : Brushes.DarkRed;
            });
        }
        private void ClearComponentMaterialMatchBackground()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentMaterialLabel.Foreground = Brushes.White;
            });
        }
        private void ClearComponent()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentBarcodeLabel.Text = "---";
                gComponentMaterialLabel.Text = string.Empty;
            });
        }
        private void UpdateAwaitedMaterials(string materials)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductAwaitedMaterials.Text = materials;
            });
        }
        private void ClearAwaitedMaterials()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductAwaitedMaterials.Text = "---";
            });
        }
        private void OperatorNotifyMaterialsMatch(bool isMatched)
        {
            UpdateComponentMaterialMatchBackground(isMatched);
            WorkPageCore.OperatorNotify($"Компонент {(isMatched ? "" : "не")} подходит",
                isMatched ? OperNotifyType.Info : OperNotifyType.Attention);
        }
        private void SetOperatorNotify(OperatorNotifyStyle popElement)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gOperatorNotifyBackgroundLabel.Style = popElement.Background;
                gOperatorNotifyTextLabel.Style = popElement.Foreground;
                gOperatorNotifyTextLabel.Content = popElement.Message;
            });
        }
        private void ClearOperatorNotify()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gOperatorNotifyBackgroundLabel.Style = (Style)Application.Current.Resources["OperatorInfoLabel"];
                gOperatorNotifyTextLabel.Style = (Style)Application.Current.Resources["OperatorBlackTextLabel"];

                gOperatorNotifyTextLabel.Content = "Ожидание сканирования";
            });
        }
    }
}
