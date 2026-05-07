using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HollywoodEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
            var model = DataContext as HollywoodEditor.ViewModels.MainModel;
            string startLocale = (HollywoodEditor.ViewModels.MainModel.CurrentLocale == "ENG") ? "en" : "ru";
            SetLocale(startLocale);
        }

        private void BtnEnglish_Click(object sender, RoutedEventArgs e)
        {
            SetLocale("en");
        }

        private void BtnRussian_Click(object sender, RoutedEventArgs e)
        {
            SetLocale("ru");
        }

        private void SetLocale(string locale)
        {
            var model = DataContext as HollywoodEditor.ViewModels.MainModel;
            if (model == null) return;

            // Сначала фиксируем язык в модели и в Application.Properties,
            // чтобы другие окна (SettingsWindow, Tags, Study Manager) сразу видели текущую локаль.

            HollywoodEditor.ViewModels.MainModel.CurrentLocale = locale == "en" ? "ENG" : "RUS";
            Application.Current.Properties["Locale"] = locale == "en" ? "ENG" : "RUS";

            // ВАЖНО: локализация может быть подключена и в Application.Resources,
            // и прямо в Window.Resources. Если менять только Application.Resources,

            ApplyLocaleDictionary(Application.Current.Resources, locale);
            ApplyLocaleDictionary(this.Resources, locale);

            model.UnzipResources();
            model.RefreshAllCharacterLabels();
            model.RefershLocale();

            UpdateFlagButtonsState(locale);
            RefreshVisualTreeResources(this);
        }

        private void Calc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new CalcWindow();
                window.Owner = this;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Calculator", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
private void ApplyLocaleDictionary(ResourceDictionary resources, string locale)
        {
            if (resources == null) return;

            var oldDicts = resources.MergedDictionaries
                .Where(d => d.Source != null &&
                            (d.Source.OriginalString.Contains("Strings.en.xaml") ||
                             d.Source.OriginalString.Contains("Strings.ru.xaml")))
                .ToList();

            foreach (var dict in oldDicts)
                resources.MergedDictionaries.Remove(dict);

            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(locale == "ru"
                    ? "Resources/Strings.ru.xaml"
                    : "Resources/Strings.en.xaml", UriKind.Relative)
            });
        }

        private void RefreshVisualTreeResources(DependencyObject parent)
        {
            if (parent == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var frameworkElement = child as FrameworkElement;
                if (frameworkElement != null)
                {
                    frameworkElement.InvalidateProperty(FrameworkElement.StyleProperty);
                    frameworkElement.UpdateLayout();
                }

                RefreshVisualTreeResources(child);
            }
        }

        // В будущем стоит обратить своё внимание на это
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        private void UpdateFlagButtonsState(string activeLocale)
        {

            BtnEnglish.BorderBrush = Brushes.Transparent;
            BtnRussian.BorderBrush = Brushes.Transparent;
            BtnEnglish.BorderThickness = new Thickness(2);
            BtnRussian.BorderThickness = new Thickness(2);

            if (activeLocale == "en")
            {
                BtnEnglish.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // #4CAF50
            }
            else if (activeLocale == "ru")
            {
                BtnRussian.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        //с точкой
        private void DoubleValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9\.\s]");//(@"^\d+(?:\.\d+)$");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool CheckDouble(string text) => Regex.IsMatch(text, @"^[0-9\.]$");
        private bool CheckInteger(string text) => Regex.IsMatch(text, @"^[0-9]$");
        private bool CheckString(string text) => Regex.IsMatch(text, @"^[\p{L} ]+$");
        private bool CheckDoubleFull(string text) => Regex.IsMatch(text, @"^(\d+(\.\d+)?)$");
        private bool CheckIntegerFull(string text) => Regex.IsMatch(text, @"^([0-9]+)$");
        private bool CheckLimitOneFull(string text) => Regex.IsMatch(text, @"^((0\.\d+)|(1\.0)|([1,0]))$");
        private bool CheckAgeFull(string text) => Regex.IsMatch(text, @"^[0-1]?[0-9][0-9]$");

        private void PastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            if (sender.GetType().Name == "TextBox")
            {
                var z = (TextBox)sender;
                string tags = z.Tag?.ToString();
                string val = (string)e.DataObject.GetData(typeof(string));
                bool valid = false;

                switch (tags)
                {
                    case "STR":
                        if (!string.IsNullOrEmpty(val))
                            valid = CheckString(val);
                        break;
                    case "INT":
                    case "AGE":
                        int intResult;
                        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out intResult))
                        {
                            int ans = intResult;
                            if (tags == "AGE")
                                if (ans > 150)
                                    ans = 90;
                            val = ans.ToString("0");
                            DataObject d = new DataObject();
                            d.SetData(DataFormats.Text, val);
                            e.DataObject = d;
                        }
                        if (tags == "AGE")
                            valid = CheckAgeFull(val);
                        else
                            valid = CheckIntegerFull(val);
                        break;
                    case "DBL":
                        double doubleResult;
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleResult))
                        {
                            valid = CheckDoubleFull(val);
                        }
                        else
                        {
                            valid = false;
                        }
                        break;
                    case "LMT":
                        valid = CheckLimitOneFull(val);
                        break;
                    default:
                        break;
                }
                if (!valid) e.CancelCommand();
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var z = (sender as TextBox);
            string tags = z.Tag?.ToString();
            switch (tags)
            {
                case "STR":
                    e.Handled = !CheckString(e.Text);
                    break;
                case "INT":
                case "AGE":
                    e.Handled = !CheckInteger(e.Text);
                    break;
                case "DBL":
                case "LMT":
                    e.Handled = !CheckDouble(e.Text);
                    break;
                default:
                    break;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var z = (sender as TextBox);
            string tags = z.Tag?.ToString();
            if (z.Text == "∞")
                return;
            switch (tags)
            {
                case "STR":
                    if (!string.IsNullOrEmpty(z.Text))
                        e.Handled = !CheckString(z.Text);
                    break;
                case "INT":
                    if (string.IsNullOrEmpty(z.Text) || z.Text == "0")
                    {
                        e.Handled = false;
                        return;
                    }
                    e.Handled = !CheckIntegerFull(z.Text);
                    break;
                case "AGE":
                    e.Handled = !CheckAgeFull(z.Text);
                    if (e.Handled)
                    {
                        int val = 0;
                        if (int.TryParse(z.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                        {
                            if (val > 150)
                                z.Text = 90.ToString();
                        }
                    }
                    break;
                case "DBL":
                    e.Handled = !CheckDoubleFull(z.Text);
                    break;
                case "LMT":
                    e.Handled = !CheckLimitOneFull(z.Text);
                    if (e.Handled)
                    {
                        double val = 0.0d;
                        if (double.TryParse(z.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                        {
                            if (val > 1.0d)
                                z.Text = 1.0d.ToString("0.00", CultureInfo.InvariantCulture);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void GitHubLogo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/Nello2/HollywoodEditor",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GitVers_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://gitverse.ru/Galapogos/HollywoodEditor",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }
    }
}
