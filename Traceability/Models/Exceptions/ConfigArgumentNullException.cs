using System;

namespace Traceability.Models
{
    internal class ConfigArgumentNullException : Exception
    {
        public ConfigArgumentNullException(string optionName) : base(optionName) { }
        public void ShowErrorMessage()
        {
            System.Windows.MessageBox.Show($"Имя {Message} не найдено.\n" +
                $"Проверьте его значение в конфигурационном файле или измените значение в настройках",
                "Ошибка конфигурации", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }
}
