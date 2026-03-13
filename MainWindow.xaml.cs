using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HollywoodEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Обработчик для отладки ошибок привязки
            Style = (Style)FindResource(typeof(Window));
            System.Windows.Data.BindingOperations.SetBinding(
                this,
                System.Windows.Controls.Primitives.Selector.SelectedItemProperty,
                new System.Windows.Data.Binding());
            //string mi = $"{App.PathToExe}Resources";
            //string local_dir = $"{mi}\\Localization\\";
            //string path_to_loc = $"{mi}\\Localization.yz";
            //bool arch_loc_exist = Path.Exists(path_to_loc);
            //if (arch_loc_exist)
            //    File.Delete(path_to_loc);
            //ZipFile.CreateFromDirectory(local_dir, path_to_loc);
            //ZipFile.CreateFromDirectory("C:\\Users\\bigja\\source\\repos\\HollyJson\\Resources\\Localization", "C:\\Users\\bigja\\source\\repos\\HollyJson\\Resources\\Localization.yz");
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
            //все таки по тагу смотреть... и переключаться на нужные проверки...
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