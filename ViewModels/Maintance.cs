using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace HollywoodEditor.ViewModels
{
    public class DateTimeToDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime)value).ToString("dd.MM.yyyy");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    // Добавленны дополнительные конвертеры для хорошей взамиосвязи с формами. 
    public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
    public class LangStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string str = value as string;
                if (string.IsNullOrEmpty(str))
                    return str;

                string originalStr = str;

                if (str == "COM" || str == "ART")
                    str = $"STATUS_{str}_SORT";
                if (str == "INDOOR" || str == "OUTDOOR")
                    str = $"SKILL_{str}_SORT";

                string str_out = str;
                if (MainModel.LocaleTranslator != null && MainModel.LocaleTranslator.ContainsKey(str))
                    str_out = MainModel.LocaleTranslator[str];

                if (str_out != null)
                {
                    if (str_out.Contains("PROFESSION_"))
                        return str_out.Replace("PROFESSION_", "").ToLower();
                    else if (str_out == "PL")
                        return MainModel.MyStudio ?? "PL";
                }

                return string.IsNullOrWhiteSpace(str_out) ? originalStr : str_out;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Converter error: {ex}");
                return value?.ToString() ?? "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class CommandHandler : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public CommandHandler(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
            }
        }
    }
}