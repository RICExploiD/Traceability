using System.Windows;
using System.Windows.Media;

namespace Traceability.MTKWorkPage
{
    using Models;
    using Services;

    partial class MTKWorkPage
    {
        private void UpdateMTKData(string componentMaterial, string QuantityOfPlannedComponent)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gMTKDataLabel1.Text = componentMaterial;

                gMTKDataLabel2.Text = $"- {QuantityOfPlannedComponent}шт.";
            });
        }
        private void UpdateMTKDataMatchBackground(bool isMatched)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gMTKDataLabel1.Foreground = isMatched ? Brushes.Green : Brushes.DarkRed;
            });
        }
        private void ClearMTKDataMatchBackground()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gMTKDataLabel1.Foreground = Brushes.White;
            });
        }
        private void UpdateAwaitedMaterials(string materials)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gProductAwaitedMaterials.Text = materials;
            });
        }
        private void UpdateMTKCounters(int saved, int left)
        {
            Dispatcher.InvokeAsync(() =>
            {
                gComponentsSavedTextBlock.Text = saved.ToString();
                gComponentsLeftTextBlock.Text = left.ToString();
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
            UpdateMTKDataMatchBackground(isMatched);
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

                gQueueCounterTextBlock.Text = $"queue count: {WorkPageCore.OperatorNotifyQueue.Count}";
            });
        }
        private void ClearOperatorNotify()
        {
            Dispatcher.InvokeAsync(() =>
            {
                gOperatorNotifyBackgroundLabel.Style = (Style)Application.Current.Resources["OperatorInfoLabel"];
                gOperatorNotifyTextLabel.Style = (Style)Application.Current.Resources["OperatorBlackTextLabel"];

                gOperatorNotifyTextLabel.Content = "Ожидание сканирования";
                gQueueCounterTextBlock.Text = $"queue count: 0";
            });
        }
    }
}
