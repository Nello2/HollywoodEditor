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

    // Добавленны дополнительные конвертеры для хорошей взамиосвязи с формами. -> 0.8.68EA -> 0.8.69EA
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
    public class ZeroToEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            double val = System.Convert.ToDouble(value);
            return val.ToString("0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;
            if (string.IsNullOrEmpty(str)) return 0.0;

            if (double.TryParse(str, out double result))
                return result;
            return 0.0;
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

                if (string.Equals(str, "All", StringComparison.OrdinalIgnoreCase))
                    return MainModel.CurrentLocale == "RUS" ? "Все" : "All";
                if (string.Equals(str, "None", StringComparison.OrdinalIgnoreCase) || string.Equals(str, "Нет", StringComparison.OrdinalIgnoreCase))
                    return MainModel.CurrentLocale == "RUS" ? "Нет" : "None";

                // Прямые переводы для черт характера в зависимости от текущей локали
                if (MainModel.CurrentLocale == "ENG")
                {
                    switch (str)
                    {
                        case "UNTOUCHABLE": return "Untouchable";
                        case "HARDWORKING": return "Hardworking";
                        case "LAZY": return "Lazy";
                        case "DISCIPLINED": return "Disciplined";
                        case "UNDISCIPLINED": return "Undisciplined";
                        case "PERFECTIONIST": return "Perfectionist";
                        case "INDIFFERENT": return "Indifferent";
                        case "HOTHEADED": return "Hotheaded";
                        case "CALM": return "Calm";
                        case "LEADER": return "Leader";
                        case "TEAM_PLAYER": return "Team Player";
                        case "OPEN_MINDED": return "Open Minded";
                        case "RACIST": return "Racist";
                        case "MISOGYNIST": return "Misogynist";
                        case "XENOPHOBE": return "Xenophobe";
                        case "DEMANDING": return "Demanding";
                        case "MODEST": return "Modest";
                        case "ARROGANT": return "Arrogant";
                        case "SIMPLE": return "Simple";
                        case "HEARTBREAKER": return "Heartbreaker";
                        case "CHASTE": return "Chaste";
                        case "CHEERY": return "Cheery";
                        case "MELANCHOLIC": return "Melancholic";
                        case "ALCOHOLIC": return "Alcoholic";
                        case "LUDOMANIAC": return "Ludomaniac";
                        case "JUNKIE": return "Junkie";
                        case "UNWANTED_ACTOR": return "Unwanted Actor";
                        case "IMAGE_VIVID": return "Vivid Image";
                        case "IMAGE_SOPHISTIC": return "Sophisticated Image";
                        case "STERILE": return "Sterile";
                        case "IMMORTAL": return "Immortal";
                        case "SUPER_IMMORTAL": return "Super Immortal";
                    }
                }
                else if (MainModel.CurrentLocale == "RUS")
                {
                    switch (str)
                    {
                        case "UNTOUCHABLE": return "Неприкасаемый";
                        case "HARDWORKING": return "Трудолюбивый";
                        case "LAZY": return "Ленивый";
                        case "DISCIPLINED": return "Дисциплинированный";
                        case "UNDISCIPLINED": return "Недисциплинированный";
                        case "PERFECTIONIST": return "Перфекционист";
                        case "INDIFFERENT": return "Безразличный";
                        case "HOTHEADED": return "Вспыльчивый";
                        case "CALM": return "Спокойный";
                        case "LEADER": return "Лидер";
                        case "TEAM_PLAYER": return "Командный игрок";
                        case "OPEN_MINDED": return "Широких взглядов";
                        case "RACIST": return "Расист";
                        case "MISOGYNIST": return "Мизогин";
                        case "XENOPHOBE": return "Ксенофоб";
                        case "DEMANDING": return "Требовательный";
                        case "MODEST": return "Скромный";
                        case "ARROGANT": return "Высокомерный";
                        case "SIMPLE": return "Простой";
                        case "HEARTBREAKER": return "Сердцеед";
                        case "CHASTE": return "Целомудренный";
                        case "CHEERY": return "Жизнерадостный";
                        case "MELANCHOLIC": return "Меланхолик";
                        case "ALCOHOLIC": return "Алкоголик";
                        case "LUDOMANIAC": return "Лудоман";
                        case "JUNKIE": return "Наркоман";
                        case "UNWANTED_ACTOR": return "Нежелательный актер";
                        case "IMAGE_VIVID": return "Яркий образ";
                        case "IMAGE_SOPHISTIC": return "Изысканный образ";
                        case "STERILE": return "Бесплодный";
                        case "IMMORTAL": return "Бессмертный";
                        case "SUPER_IMMORTAL": return "Супер бессмертный";
                    }
                }

                if (str == "COM" || str == "ART")
                    str = $"STATUS_{str}_SORT";
                if (str == "INDOOR" || str == "OUTDOOR")
                    str = $"SKILL_{str}_SORT";

                string str_out = str;
                if (MainModel.LocaleTranslator != null && MainModel.LocaleTranslator.ContainsKey(str))
                    str_out = MainModel.LocaleTranslator[str];

                if (!string.IsNullOrEmpty(str_out))
                {
                    str_out = str_out.Replace("<nobr>", "").Replace("</nobr>", "");
                    str_out = str_out.Replace(" (DEPRECATED)", "").Replace("(DEPRECATED)", "").Trim();
                }

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