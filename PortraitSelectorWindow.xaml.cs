using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HollywoodEditor.Models;
using HollywoodEditor.ViewModels;

namespace HollywoodEditor
{
    public partial class PortraitSelectorWindow : Window
    {
        private Character character;
        private string selectedPortraitPath;
        private List<PortraitItem> allPortraits;
        private List<PortraitItem> currentPagePortraits;
        private int currentPage = 1;
        private int itemsPerPage = 21;

        public string SelectedPortraitPath { get; private set; }

        private bool IsRussianLocale
        {
            get
            {
                string locale = MainModel.CurrentLocale;
                if (!string.IsNullOrWhiteSpace(locale))
                {
                    locale = locale.Trim().ToUpperInvariant();
                    if (locale == "RUS" || locale == "RU" || locale == "RU-RU") return true;
                    if (locale == "ENG" || locale == "EN" || locale == "EN-US") return false;
                }

                if (Application.Current != null && Application.Current.Properties.Contains("Locale"))
                {
                    string appLocale = Convert.ToString(Application.Current.Properties["Locale"]);
                    if (!string.IsNullOrWhiteSpace(appLocale))
                    {
                        appLocale = appLocale.Trim().ToUpperInvariant();
                        if (appLocale == "RUS" || appLocale == "RU" || appLocale == "RU-RU") return true;
                        if (appLocale == "ENG" || appLocale == "EN" || appLocale == "EN-US") return false;
                    }
                }

                try
                {
                    string budgetText = Application.Current?.TryFindResource("Budget") as string;
                    if (budgetText == "Банк") return true;
                    if (budgetText == "Budget") return false;
                }
                catch { }

                return false;
            }
        }

        private string L(string en, string ru)
        {
            return IsRussianLocale ? ru : en;
        }

        private void ApplyLocalization()
        {
            Title = L("Select Portrait", "Изменение портрета");
            if (TitleText != null) TitleText.Text = L("Select Portrait", "Выбор портрета");
            if (PrevButton != null) PrevButton.Content = L("◀ Previous", "◀ Назад");
            if (NextButton != null) NextButton.Content = L("Next ▶", "Далее ▶");
            if (ApplyButton != null) ApplyButton.Content = L("Apply", "Применить");
            if (CancelButton != null) CancelButton.Content = L("Cancel", "Отмена");
        }

        public PortraitSelectorWindow(Character character)
        {
            InitializeComponent();
            ApplyLocalization();
            this.character = character;
            this.Owner = Application.Current.MainWindow;

            string portraitType = GetPortraitType(character);
            // 0.68.69EA -> gender == 1 - женщина (F), gender == 0 - мужчина (M)
            string gender = character.gender == 1 ? "F" : "M";
            string age = GetAgeCategory(character.Age);

            string genderText = character.gender == 1 ? L("Female", "Женщина") : L("Male", "Мужчина");

            CharacterInfoText.Text = $"{character.MyCustomName} | {portraitType} | {genderText} | {age} {L("years", "лет")}";

            LoadAllPortraits(portraitType, gender, age);
        }

        private string GetPortraitType(Character character)
        {
            if (character.professions == null) return "TALENT";

            switch (character.professions.GetProfession)
            {
                // Агенты - отдельная категория
                case Professions.Profession.Agent:
                    return "AGENT";

                case Professions.Profession.LieutScript:
                case Professions.Profession.LieutPrep:
                case Professions.Profession.LieutProd:
                case Professions.Profession.LieutPost:
                case Professions.Profession.LieutRelease:
                case Professions.Profession.LieutSecurity:
                case Professions.Profession.LieutProducers:
                case Professions.Profession.LieutInfrastructure:
                case Professions.Profession.LieutTech:
                case Professions.Profession.LieutMuseum:
                case Professions.Profession.LieutEscort:
                case Professions.Profession.CptHR:
                case Professions.Profession.CptLawyer:
                case Professions.Profession.CptFinancier:
                case Professions.Profession.CptPR:
                    return "LIEUT";

                default:
                    return "TALENT";
            }
        }

        private string GetAgeCategory(int age)
        {
            if (age <= 35) return "YOUNG";
            if (age <= 55) return "MID";
            return "OLD";
        }

        private void LoadAllPortraits(string type, string gender, string age)
        {
            allPortraits = new List<PortraitItem>();

            string profilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Profiles");

            // Распаковка Архива
            if (!Directory.Exists(profilesPath))
            {

                string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Profiles.zip");
                if (File.Exists(zipPath))
                {
                    try
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath,
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error extracting profiles: {ex.Message}\n\nPortraits will not be displayed.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show($"Profiles directory not found: {profilesPath}\n\nPortraits will not be displayed.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string searchPattern = $"PRT_{type}_{gender}_{age}_*.png";
            var files = Directory.GetFiles(profilesPath, searchPattern);

            if (files.Length == 0 && age != "MID")
            {
                searchPattern = $"PRT_{type}_{gender}_MID_*.png";
                files = Directory.GetFiles(profilesPath, searchPattern);
            }

            if (files.Length == 0)
            {
                searchPattern = $"PRT_{type}_{gender}_*.png";
                files = Directory.GetFiles(profilesPath, searchPattern);
            }

            if (files.Length == 0)
            {
                searchPattern = $"PRT_{type}_*.png";
                files = Directory.GetFiles(profilesPath, searchPattern);
            }

            var sortedFiles = files.OrderBy(f =>
            {
                string name = Path.GetFileNameWithoutExtension(f);
                string[] parts = name.Split('_');
                if (parts.Length >= 5)
                {
                    string numberStr = parts[4];
                    if (int.TryParse(numberStr, out int num))
                        return num;
                }
                return 0;
            }).ToList();

            int index = 0;
            foreach (var file in sortedFiles)
            {
                allPortraits.Add(new PortraitItem
                {
                    Path = file,
                    FileName = Path.GetFileName(file),
                    Index = index++
                });
            }

            UpdatePagination();
        }

        private void UpdatePagination()
        {
            if (allPortraits == null) return;

            int totalPages = (int)Math.Ceiling((double)allPortraits.Count / itemsPerPage);
            if (totalPages == 0) totalPages = 1;
            currentPage = Math.Max(1, Math.Min(currentPage, totalPages));

            int startIndex = (currentPage - 1) * itemsPerPage;
            int count = Math.Min(itemsPerPage, allPortraits.Count - startIndex);
            currentPagePortraits = allPortraits.Skip(startIndex).Take(count).ToList();

            PortraitsGrid.ItemsSource = currentPagePortraits;
            PageInfo.Text = $"{L("Page", "Страница")} {currentPage} / {(totalPages > 0 ? totalPages : 1)}";

            PrevButton.IsEnabled = currentPage > 1;
            NextButton.IsEnabled = currentPage < totalPages && allPortraits.Count > 0;

            HighlightCurrentPortrait();
        }

        private void HighlightCurrentPortrait()
        {
            if (character == null || character.portraitBaseId <= 0) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < PortraitsGrid.Items.Count; i++)
                {
                    var item = PortraitsGrid.Items[i];
                    if (item is PortraitItem portrait)
                    {

                        string fileName = Path.GetFileNameWithoutExtension(portrait.FileName);
                        string[] parts = fileName.Split('_');
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int fileNumber))
                        {
                            if (fileNumber == character.portraitBaseId)
                            {
                                var container = PortraitsGrid.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                                if (container != null)
                                {
                                    var border = FindVisualChild<Border>(container);
                                    if (border != null)
                                    {
                                        var innerBorder = FindVisualChild<Border>(border, "SelectionBorder");
                                        if (innerBorder != null)
                                        {
                                            innerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38));
                                            innerBorder.BorderThickness = new Thickness(2);
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private T FindVisualChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (name == null || (child is FrameworkElement fe && fe.Name == name)))
                    return t;

                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                UpdatePagination();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)allPortraits.Count / itemsPerPage);
            if (currentPage < totalPages)
            {
                currentPage++;
                UpdatePagination();
            }
        }

        private void Portrait_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.DataContext is PortraitItem item)
            {
                selectedPortraitPath = item.Path;

                foreach (var container in PortraitsGrid.Items)
                {
                    var contentPresenter = PortraitsGrid.ItemContainerGenerator.ContainerFromItem(container) as ContentPresenter;
                    if (contentPresenter != null)
                    {
                        var outerBorder = FindVisualChild<Border>(contentPresenter);
                        if (outerBorder != null)
                        {
                            var innerBorder = FindVisualChild<Border>(outerBorder, "SelectionBorder");
                            if (innerBorder != null)
                            {
                                innerBorder.BorderBrush = Brushes.Transparent;
                            }
                        }
                    }
                }

                var selectedContainer = PortraitsGrid.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                if (selectedContainer != null)
                {
                    var outerBorder = FindVisualChild<Border>(selectedContainer);
                    if (outerBorder != null)
                    {
                        var innerBorder = FindVisualChild<Border>(outerBorder, "SelectionBorder");
                        if (innerBorder != null)
                        {
                            innerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38));
                            innerBorder.BorderThickness = new Thickness(2);
                        }
                    }
                }
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedPortraitPath))
            {
                SelectedPortraitPath = selectedPortraitPath;

                // Извлекаем номер из имени файла
                string fileName = Path.GetFileNameWithoutExtension(selectedPortraitPath);
                string[] parts = fileName.Split('_');
                if (parts.Length >= 5 && int.TryParse(parts[4], out int newId))
                {
                    character.portraitBaseId = newId;
                    character.CustomPortraitPath = selectedPortraitPath;
                }

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(L("Please select a portrait first.", "Сначала выберите портрет."), L("No Selection", "Портрет не выбран"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
    public class PortraitItem
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public int Index { get; set; }
    }
}
