using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using HollywoodEditor.ViewModels;

namespace HollywoodEditor
{
    public partial class App : Application
    {
        public static string PathToExe { get; private set; }
        public static string GamePath { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Устанавливаем путь к исполняемому файлу
            PathToExe = AppDomain.CurrentDomain.BaseDirectory;

            // Загружаем локализацию
            LoadLocalization();

            // Обработка необработанных исключений
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void LoadLocalization()
        {
            try
            {
                string locPath = Path.Combine(PathToExe, "Resources", "Localization.yz");
                if (File.Exists(locPath))
                {
                    // Распаковываем локализацию
                    string extractPath = Path.Combine(PathToExe, "Resources", "Localization");
                    if (!Directory.Exists(extractPath))
                    {
                        Directory.CreateDirectory(extractPath);
                        System.IO.Compression.ZipFile.ExtractToDirectory(locPath, extractPath);
                    }

                    // Загружаем переводы
                    string langFile = Path.Combine(extractPath, "en.json");
                    if (File.Exists(langFile))
                    {
                        string json = File.ReadAllText(langFile);
                        MainModel.LocaleTranslator = Newtonsoft.Json.JsonConvert
                        .DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading localization: {ex.Message}");
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Unhandled exception: {ex.Message}\n\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}