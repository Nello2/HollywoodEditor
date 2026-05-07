using HollywoodEditor.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Path = System.IO.Path;

namespace HollywoodEditor.ViewModels
{
    [AddINotifyPropertyChangedInterface]  // Fody автоматически добавляет PropertyChanged
    public class MainModel
    {
        private CommandHandler _showSpawnWindow;
        private CommandHandler _showTagsWindow;
        private CommandHandler _showTechsWindow;
        private CommandHandler _openSettings;
        private CommandHandler _changePortrait;
        private string opennedfileplace = string.Empty;
        CommandHandler _savefile;
        CommandHandler _openfile;

        CommandHandler _removeTraitUp;
        CommandHandler _addtrait;
        CommandHandler _removetrait;

        CommandHandler _addskill;
        CommandHandler _removeskill;

        CommandHandler _setmoodandatt;
        CommandHandler _setcontrdays;
        CommandHandler _setskilltolimit;
        CommandHandler _setskiiltocap;

        CommandHandler _setagetoyoung;
        CommandHandler _setallskills;

        CommandHandler _showtags;
        CommandHandler _showspawndate;
        CommandHandler _showtechs;
        CommandHandler _unlocktechs;
        CommandHandler _unlocktags;

        private string search_txt;
        JObject jobj = null;
        private Character selectedChar;
        private string filter_Prof;
        private string filter_studio;
        private string _originalJsonString;
        private ObservableCollection<Character> filtered_Obj;
        private bool showOnlyTalent = false;
        private bool showOnlyDead = false;
        private bool showWithDead = true;
        private bool settings_done = false;


        public static Dictionary<string, string> LocaleNames { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> LocaleTranslator { get; set; } = new Dictionary<string, string>();
        public static string MyStudio { get; set; }
        public static string CurrentLocale { get; set; } = "ENG";

        // Список жанровых тегов, которые нужно исключить из Tags Manager
        private static readonly HashSet<string> GenreTags = new HashSet<string>
        {
            "DRAMA",
            "ROMANCE",
            "SCIENCE_FICTION",
            "THRILLER",
            "ACTION",
            "DETECTIVE",
            "ADVENTURE",
            "HISTORICAL",
            "COMEDY",
            "HORROR",
            "WILD_WEST",
            "MUSICAL",
            "SLAPSTICK_COMEDY"
        };

        public event EventHandler LocaleChanged;

        public void NotifyLocaleChanged()
        {
            LocaleChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshAllCharacterLabels()
        {
            if (Info?.characters == null) return;

            foreach (var character in Info.characters)
            {
                character.UpdateFilteredLabels();
            }
        }

        public stateJson Info { get; set; }

        public bool Settings_done
        {
            get => settings_done;
            set => settings_done = value;
        }

        public ObservableCollection<Character> Filtered_Obj
        {
            get => filtered_Obj;
            set
            {
                filtered_Obj = value;
                if (SelectedChar == null)
                {
                    if (value != null && value.Count > 0)
                        SelectedChar = Filtered_Obj[0];
                }
            }
        }

        public Character SelectedChar
        {
            get => selectedChar;
            set
            {
                selectedChar = value;
            }
        }

        public string StatusBarText { get; set; } = "Hello";
        public bool ShowSpawn { get; set; } = false;
        public bool ShowTags { get; set; } = false;
        public bool ShowTechs { get; set; } = false;
        public bool Save_Loaded { get; set; } = false;
        public bool Save_done { get; set; } = false;
        public bool Portrait_done { get; set; } = false;

        public bool ShowOnlyTalent
        {
            get => showOnlyTalent;
            set
            {
                showOnlyTalent = value;
                ProfList = value ? ProfListWithOutNoTallent : ProfListWithNoTallent;
                SetSearched();
            }
        }

        public bool ShowOnlyDead
        {
            get => showOnlyDead;
            set
            {
                showOnlyDead = value;
                if (value && !ShowWithDead)
                    ShowWithDead = true;
                SetSearched();
            }
        }

        public bool ShowWithDead
        {
            get => showWithDead;
            set
            {
                showWithDead = value;
                if (!value && ShowOnlyDead)
                    ShowOnlyDead = false;
                SetSearched();
            }
        }

        public List<string> StudioListForChar { get; set; }
        public List<string> StudioList { get; set; }
        public List<string> ProfList { get; set; }
        private List<string> ProfListWithOutNoTallent { get; set; }
        private List<string> ProfListWithNoTallent { get; set; }

        public string Filter_Prof
        {
            get => filter_Prof;
            set
            {
                filter_Prof = value;
                SetSearched();
            }
        }

        public string Filter_studio
        {
            get => filter_studio;
            set
            {
                filter_studio = value;
                SetSearched();
            }
        }

        public string Filter_txt
        {
            get => search_txt;
            set
            {
                search_txt = value;
                SetSearched();
            }
        }

        public MainModel()
        {
            Filter_txt = "";
            Filter_studio = "";
            Filter_Prof = "";
            StatusBarText = Tr("Prepared to unzip", "Готов к распаковке");
            Filtered_Obj = new ObservableCollection<Character>();
            UnzipResources();
            StatusBarText = Tr("Done", "Готово");
        }

        public void SetSearched()
        {
            if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(SetSearched);
                return;
            }

            try
            {
                if (Info == null) return;
                if (Info.characters == null) return;

                IEnumerable<Character> q = Info.characters;

                if (Filter_studio != "All" && !string.IsNullOrEmpty(Filter_studio))
                {
                    q = q.Where(t => t.studioId == Filter_studio);
                }
                if (Filter_Prof != "All" && !string.IsNullOrEmpty(Filter_Prof))
                {
                    q = q.Where(t => t.professions != null && t.professions.ProfToDecode == Filter_Prof);
                }
                if (!string.IsNullOrWhiteSpace(Filter_txt))
                {
                    q = q.Where(t => t.MyCustomName != null && t.MyCustomName.Contains(Filter_txt));
                }
                if (ShowOnlyTalent)
                    q = q.Where(t => t.professions != null && t.professions.IsTalent);
                if (ShowOnlyDead)
                    q = q.Where(t => t.IsDead);
                if (!ShowWithDead)
                    q = q.Where(t => !t.IsDead);

                q = q.OrderBy(t => t.professions != null ? t.professions.ProfToDecode : "");

                int count = q.Count();
                StatusBarText = Tr("Filtered ", "Отфильтровано ") + count + Tr(" chars", " перс.");
                Filtered_Obj = new ObservableCollection<Character>(q);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SetSearched: {ex.Message}");
                StatusBarText = Tr("Error filtering: ", "Ошибка фильтрации: ") + ex.Message;
                Filtered_Obj = new ObservableCollection<Character>();
            }
        }

        // UnzipResources полностью переписан, так как изначально задумывалось, что локали будут использоваться напрямую через .yz,
        // что соответственное создавало своеобразные сложности. 
        // Пришлось полностью менять логику на распаковку .zip файлов.
        // Улучшена совместимость для 0.8.55EA
        // Вдобавок пришлось отказаться от иконок персонажей, так как занимало очень много место.
        // Update 13.03.2026: Был полностью удален код, который позволял редактору распаковывать изображения персонажей, что пришлось это отдельно переносить в SE;
        // Это было сделано для хорошей оптимизации, потому что формы из-за этого зависали/заедали.
        // Были возвращены портреты из специальной версии 0.2.3B(0.2.3S.B). (20.03.2026)
        // Основной фундамент был взят из 0.2.3B и был переписан под соответствующие субмодули + UI Fix.
        // Update 06.05.2026: Были полностью исправлены многие ошибки, которые были изначально у 0.2.5B из-за которых не работали дополнительные субмодули улучшений.
        // Update 08.05.2026: Была проведена работа над ошибками, которые возникали в 0.2.5B

        public async void UnzipResources()
        {
            try
            {
                await Task.Run(() =>
                {
                    string mi = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

                    if (!Directory.Exists(mi))
                    {
                        Directory.CreateDirectory(mi);
                    }

                    string local_dir = Path.Combine(mi, "Localization");
                    string prof_dir = Path.Combine(mi, "Profiles");

                    string loc_zip = Path.Combine(mi, "Localization.zip");
                    string prof_zip = Path.Combine(mi, "Profiles.zip");

                    bool arch_loc_exist = File.Exists(loc_zip);
                    bool arch_prof_exits = File.Exists(prof_zip);

                    if (arch_loc_exist)
                    {
                        if (!Directory.Exists(local_dir))
                        {
                            StatusBarText = Tr("Start extracting Localization", "Распаковка локализации");
                            ExtractZipFile(loc_zip, local_dir);
                            StatusBarText = Tr("End extracting Localization", "Локализация распакована");
                        }

                        StatusBarText = Tr("Set Localization", "Установка локализации");

                        string localeFolder = CurrentLocale.ToUpper();
                        string localePath = Path.Combine(local_dir, localeFolder);

                        if (Directory.Exists(localePath))
                        {
                            SetLocale(localePath);
                        }
                        else
                        {
                            string fallbackPath = Path.Combine(local_dir, "ENG");
                            if (Directory.Exists(fallbackPath))
                            {
                                SetLocale(fallbackPath);
                            }
                            else
                            {
                                StatusBarText = Tr("Locale folder not found: ", "Папка локали не найдена: ") + localePath;
                            }
                        }
                    }
                    if (arch_prof_exits)
                    {
                        if (!Directory.Exists(prof_dir))
                        {
                            StatusBarText = Tr("Start extracting Profile images", "Распаковка портретов");
                            ExtractZipFile(prof_zip, prof_dir);
                            StatusBarText = Tr("End extracting Profile images", "Портреты распакованы");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExtractZipFile(string zipPath, string extractPath)
        {
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
            }
            catch (Exception)
            {
                ManualExtractZip(zipPath, extractPath);
            }
        }

        private void ManualExtractZip(string zipPath, string extractPath)
        {
            if (!Directory.Exists(extractPath))
            {
                Directory.CreateDirectory(extractPath);
            }
        }


        #region Window Creation Methods

        private Window CreateSpawnWindow()
        {
            var window = new Window
            {
                Title = Tr("Spawn Dates", "Даты появления"),
                Width = 450,
                Height = 550,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x29, 0x10, 0x10)),
                Foreground = Brushes.White,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var listBox = new ListBox
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                BorderThickness = new Thickness(0),
                FontSize = 13
            };

            var items = new List<SpawnItem>();
            if (Info?.NextSpawnDays != null)
            {
                foreach (var item in Info.NextSpawnDays)
                {
                    items.Add(new SpawnItem
                    {
                        Profession = item.Key,
                        Date = item.Value
                    });
                }
            }
            listBox.ItemsSource = items;

            var itemTemplate = new DataTemplate();
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackPanelFactory.SetValue(StackPanel.MarginProperty, new Thickness(5));

            var profTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            profTextFactory.SetBinding(TextBlock.TextProperty, new Binding("Profession") { Converter = new LangStringConverter() });
            profTextFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            profTextFactory.SetValue(TextBlock.WidthProperty, 275.0);
            profTextFactory.SetValue(TextBlock.MarginProperty, new Thickness(5));

            var dateTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            dateTextFactory.SetBinding(TextBlock.TextProperty, new Binding("Date") { Converter = new DateTimeToDateConverter() });
            dateTextFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x4E, 0xA8, 0x0B)));
            dateTextFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            dateTextFactory.SetValue(TextBlock.MarginProperty, new Thickness(5));

            stackPanelFactory.AppendChild(profTextFactory);
            stackPanelFactory.AppendChild(dateTextFactory);
            itemTemplate.VisualTree = stackPanelFactory;
            listBox.ItemTemplate = itemTemplate;

            var closeButton = new Button
            {
                Content = Tr("Close", "Закрыть"),
                Margin = new Thickness(10),
                Padding = new Thickness(20, 10, 20, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, e) => window.Close();

            var scrollViewer = new ScrollViewer
            {
                Content = listBox,
                Background = Brushes.Transparent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10)
            };

            scrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (s is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                    e.Handled = true;
                }
            };

            var mainPanel = new DockPanel();
            DockPanel.SetDock(closeButton, Dock.Bottom);
            mainPanel.Children.Add(closeButton);
            mainPanel.Children.Add(scrollViewer);

            window.Content = mainPanel;
            return window;
        }


        private static string UiLocale
        {
            get
            {
                try
                {
                    string propLocale = Application.Current != null && Application.Current.Properties.Contains("Locale")
                        ? Convert.ToString(Application.Current.Properties["Locale"])
                        : null;

                    if (!string.IsNullOrWhiteSpace(propLocale))
                    {
                        propLocale = propLocale.ToUpperInvariant();
                        if (propLocale == "RUS" || propLocale == "RU") return "ru";
                        if (propLocale == "ENG" || propLocale == "EN") return "en";
                    }

                    if (Application.Current != null)
                    {
                        bool hasRuDictionary = Application.Current.Resources.MergedDictionaries.Any(d =>
                            d.Source != null && d.Source.OriginalString.IndexOf("Strings.ru.xaml", StringComparison.OrdinalIgnoreCase) >= 0);
                        bool hasEnDictionary = Application.Current.Resources.MergedDictionaries.Any(d =>
                            d.Source != null && d.Source.OriginalString.IndexOf("Strings.en.xaml", StringComparison.OrdinalIgnoreCase) >= 0);

                        if (hasRuDictionary && !hasEnDictionary) return "ru";
                    }
                }
                catch
                {

                }

                return CurrentLocale != null && CurrentLocale.ToUpperInvariant() == "RUS" ? "ru" : "en";
            }
        }

        public static string Tr(string en, string ru)
        {
            return UiLocale == "ru" ? ru : en;
        }

        private static string GetDepartmentDisplayName(string departmentKey)
        {
            switch (departmentKey)
            {
                case "TECHNOLOGY DEPARTMENT": return Tr("TECHNOLOGY DEPARTMENT", "ОТДЕЛ ТЕХНОЛОГИЙ");
                case "PRODUCTION DEPARTMENT": return Tr("PRODUCTION DEPARTMENT", "ПРОИЗВОДСТВЕННЫЙ ОТДЕЛ");
                case "PRODUCING DEPARTMENT": return Tr("PRODUCING DEPARTMENT", "ОТДЕЛ ПРОДЮСИРОВАНИЯ");
                case "LEGAL DEPARTMENT": return Tr("LEGAL DEPARTMENT", "ЮРИДИЧЕСКИЙ ОТДЕЛ");
                case "PR DEPARTMENT": return Tr("PR DEPARTMENT", "PR-ОТДЕЛ");
                case "POST-PRODUCTION DEPARTMENT": return Tr("POST-PRODUCTION DEPARTMENT", "ОТДЕЛ ПОСТПРОДАКШЕНА");
                case "RENTAL DEPARTMENT": return Tr("RENTAL DEPARTMENT", "ОТДЕЛ ПРОКАТА");
                case "INFRASTRUCTURE DEPARTMENT": return Tr("INFRASTRUCTURE DEPARTMENT", "ОТДЕЛ ИНФРАСТРУКТУРЫ");
                case "SCRIPT DEPARTMENT": return Tr("SCRIPT DEPARTMENT", "СЦЕНАРНЫЙ ОТДЕЛ");
                case "PRE-PRODUCTION DEPARTMENT": return Tr("PRE-PRODUCTION DEPARTMENT", "ОТДЕЛ ПРЕПРОДАКШНА");
                case "HR DEPARTMENT": return Tr("HR DEPARTMENT", "ОТДЕЛ КАДРОВ");
                case "DEPARTMENT HR": return Tr("DEPARTMENT HR", "ОТДЕЛ КАДРОВ");
                case "COMFORT DEPARTMENT": return Tr("COMFORT DEPARTMENT", "ОТДЕЛ ОБЕСПЕЧЕНИЯ КОМФОРТА");
                case "FINANCIAL DEPARTMENT": return Tr("FINANCIAL DEPARTMENT", "ФИНАНСОВЫЙ ОТДЕЛ");
                case "PUBLIC RELATIONS DEPARTMENT": return Tr("PUBLIC RELATIONS DEPARTMENT", "ОТДЕЛ ПО СВЯЗЯМ С ОБЩЕСТВЕННОСТЬЮ");
                case "SECURITY DEPARTMENT": return Tr("SECURITY DEPARTMENT", "СЛУЖБА БЕЗОПАСНОСТИ");
                default: return departmentKey;
            }
        }

        private static string GetTagCategoryPrefix(string tagId)
        {
            if (tagId.StartsWith("PROTAGONIST_"))
                return UiLocale == "ru" ? "🎭 [Протагонист] " : "🎭 [Protagonist] ";
            if (tagId.StartsWith("ANTAGONIST_"))
                return UiLocale == "ru" ? "👹 [Антагонист] " : "👹 [Antagonist] ";
            if (tagId.StartsWith("SUPPORTINGCHARACTER_"))
                return UiLocale == "ru" ? "👥 [Второстепенный] " : "👥 [Supporting] ";
            if (tagId.StartsWith("WILD_WEST") || tagId.StartsWith("MODERN_AMERICAN") ||
                tagId.StartsWith("AMERICAN_CIVIL_WAR") || tagId.StartsWith("GREAT_WAR") ||
                tagId.StartsWith("WW2") || tagId.StartsWith("SPACE") || tagId.StartsWith("FANTASY_KINGDOM") ||
                tagId.StartsWith("TROPICAL_ISLAND") || tagId.StartsWith("ARTHURIAN_LEGENDS") ||
                tagId.StartsWith("CARIBBEAN") || tagId.StartsWith("MIDDLE_AGES") ||
                tagId.StartsWith("VICTORIAN_ENGLAND") || tagId.StartsWith("MODERN_EUROPEAN") ||
                tagId.StartsWith("ANCIENT_") || tagId.StartsWith("FEUDAL_JAPAN") ||
                tagId.StartsWith("RENAISSANCE") || tagId.StartsWith("DYSTOPIAN_") ||
                tagId.StartsWith("UTOPIAN_") || tagId.StartsWith("FREE_STATES") ||
                tagId.StartsWith("SLAVE_STATES"))
                return UiLocale == "ru" ? "🌍 [Сеттинг] " : "🌍 [Setting] ";
            if (tagId.StartsWith("THEME_") || tagId == "EVENT_CURSED_DEAL")
                return UiLocale == "ru" ? "📖 [Тема] " : "📖 [Theme] ";
            if (tagId.StartsWith("EVENTS_"))
                return UiLocale == "ru" ? "⚡ [Событие] " : "⚡ [Event] ";
            if (tagId.StartsWith("FINALE_"))
                return UiLocale == "ru" ? "🏁 [Финал] " : "🏁 [Finale] ";
            return string.Empty;
        }

        private static string StripTagCategoryPrefix(string displayText)
        {
            var prefixes = new[]
            {
                "🎭 [Protagonist] ", "🎭 [Протагонист] ",
                "👹 [Antagonist] ", "👹 [Антагонист] ",
                "👥 [Supporting] ", "👥 [Второстепенный] ",
                "🌍 [Setting] ", "🌍 [Сеттинг] ",
                "📖 [Theme] ", "📖 [Тема] ",
                "⚡ [Event] ", "⚡ [Событие] ",
                "🏁 [Finale] ", "🏁 [Финал] "
            };

            foreach (var prefix in prefixes)
            {
                if (displayText.StartsWith(prefix))
                    return displayText.Substring(prefix.Length);
            }

            return displayText;
        }

        private Window CreateTagsWindow()
        {
            var window = new Window
            {
                Title = Tr("Manage Tags", "Панель тегов"),
                Width = 450,
                Height = 550,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x29, 0x10, 0x10)),
                Foreground = Brushes.White,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var listBox = new ListBox
            {
                ItemsSource = Info?.tagBank,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                BorderThickness = new Thickness(0),
                FontSize = 13
            };

            var itemTemplate = new DataTemplate();
            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, new Binding(".") { Converter = new LangStringConverter() });
            textFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            textFactory.SetValue(TextBlock.PaddingProperty, new Thickness(10, 8, 10, 8));
            textFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
            itemTemplate.VisualTree = textFactory;
            listBox.ItemTemplate = itemTemplate;

            var closeButton = new Button
            {
                Content = Tr("Close", "Закрыть"),
                Margin = new Thickness(10),
                Padding = new Thickness(20, 10, 20, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, e) => window.Close();

            var scrollViewer = new ScrollViewer
            {
                Content = listBox,
                Background = Brushes.Transparent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10)
            };

            scrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (s is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                    e.Handled = true;
                }
            };

            var mainPanel = new DockPanel();
            DockPanel.SetDock(closeButton, Dock.Bottom);
            mainPanel.Children.Add(closeButton);
            mainPanel.Children.Add(scrollViewer);

            window.Content = mainPanel;
            return window;
        }

        private Window CreateTechsWindow()
        {
            var window = new Window
            {
                Title = "Closed Technologies",
                Width = 450,
                Height = 550,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x29, 0x10, 0x10)),
                Foreground = Brushes.White,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var listBox = new ListBox
            {
                ItemsSource = Info?.AvailablePerks,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                BorderThickness = new Thickness(0),
                FontSize = 13
            };

            var itemTemplate = new DataTemplate();
            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, new Binding(".") { Converter = new LangStringConverter() });
            textFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            textFactory.SetValue(TextBlock.PaddingProperty, new Thickness(10, 8, 10, 8));
            textFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
            itemTemplate.VisualTree = textFactory;
            listBox.ItemTemplate = itemTemplate;

            var closeButton = new Button
            {
                Content = Tr("Close", "Закрыть"),
                Margin = new Thickness(10),
                Padding = new Thickness(20, 10, 20, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, e) => window.Close();

            var scrollViewer = new ScrollViewer
            {
                Content = listBox,
                Background = Brushes.Transparent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10)
            };

            scrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (s is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                    e.Handled = true;
                }
            };

            var mainPanel = new DockPanel();
            DockPanel.SetDock(closeButton, Dock.Bottom);
            mainPanel.Children.Add(closeButton);
            mainPanel.Children.Add(scrollViewer);

            window.Content = mainPanel;
            return window;
        }

        private class SpawnItem
        {
            public string Profession { get; set; }
            public DateTime Date { get; set; }
        }

        private Window CreateTagsManagerWindow()
        {
            var window = new Window
            {
                Title = Tr("Tags Manager", "Панель тегов"),
                Width = 850,
                Height = 600,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.CanResize,
                Background = new SolidColorBrush(Color.FromRgb(0x29, 0x10, 0x10)),
                Foreground = Brushes.White,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            int GetCategoryOrder(string tagId)
            {
                if (tagId.StartsWith("PROTAGONIST_")) return 1;
                if (tagId.StartsWith("ANTAGONIST_")) return 2;
                if (tagId.StartsWith("SUPPORTINGCHARACTER_")) return 3;
                if (tagId.StartsWith("WILD_WEST") || tagId.StartsWith("MODERN_AMERICAN") ||
                    tagId.StartsWith("AMERICAN_CIVIL_WAR") || tagId.StartsWith("GREAT_WAR") ||
                    tagId.StartsWith("WW2") || tagId.StartsWith("SPACE") || tagId.StartsWith("FANTASY_KINGDOM") ||
                    tagId.StartsWith("TROPICAL_ISLAND") || tagId.StartsWith("ARTHURIAN_LEGENDS") ||
                    tagId.StartsWith("CARIBBEAN") || tagId.StartsWith("MIDDLE_AGES") ||
                    tagId.StartsWith("VICTORIAN_ENGLAND") || tagId.StartsWith("MODERN_EUROPEAN") ||
                    tagId.StartsWith("ANCIENT_") || tagId.StartsWith("FEUDAL_JAPAN") ||
                    tagId.StartsWith("RENAISSANCE") || tagId.StartsWith("DYSTOPIAN_") ||
                    tagId.StartsWith("UTOPIAN_") || tagId.StartsWith("FREE_STATES") ||
                    tagId.StartsWith("SLAVE_STATES")) return 4;
                if (tagId.StartsWith("THEME_") || tagId == "EVENT_CURSED_DEAL") return 5;
                if (tagId.StartsWith("EVENTS_")) return 6;
                if (tagId.StartsWith("FINALE_")) return 7;
                return 99;
            }

            string GetLocalizedTagNameWithCategory(string tagId)
            {
                if (string.IsNullOrEmpty(tagId)) return tagId;

                string localized = tagId;
                string category = GetTagCategoryPrefix(tagId);

                if (LocaleTranslator != null && LocaleTranslator.ContainsKey(tagId))
                {
                    localized = LocaleTranslator[tagId];
                    localized = localized?.Replace("<nobr>", "").Replace("</nobr>", "");
                }

                return category + (localized ?? tagId);
            }

            string ResolveTagDisplayToId(string displayText, IEnumerable<string> candidateIds)
            {
                if (string.IsNullOrWhiteSpace(displayText)) return displayText;

                foreach (var id in candidateIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (string.Equals(GetLocalizedTagNameWithCategory(id), displayText, StringComparison.Ordinal))
                        return id;
                }

                string cleanText = StripTagCategoryPrefix(displayText);

                foreach (var id in candidateIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    string localized = id;
                    if (LocaleTranslator != null && LocaleTranslator.TryGetValue(id, out string translated) && !string.IsNullOrWhiteSpace(translated))
                        localized = translated.Replace("<nobr>", "").Replace("</nobr>", "");

                    if (string.Equals(localized, cleanText, StringComparison.Ordinal) ||
                        string.Equals(id, cleanText, StringComparison.Ordinal))
                        return id;
                }

                return cleanText;
            }

            string NormalizeTagIdForSave(string rawTag)
            {
                if (string.IsNullOrWhiteSpace(rawTag)) return rawTag;

                string clean = StripTagCategoryPrefix(rawTag).Replace("<nobr>", "").Replace("</nobr>", "").Trim();

                if (LocaleTranslator != null)
                {
                    if (LocaleTranslator.ContainsKey(clean))
                        return clean;

                    foreach (var pair in LocaleTranslator)
                    {
                        string localized = pair.Value?.Replace("<nobr>", "").Replace("</nobr>", "").Trim();
                        if (string.Equals(localized, clean, StringComparison.Ordinal))
                            return pair.Key;
                    }
                }

                return clean;
            }

            if (Info.tagBank != null)
            {
                var normalizedBank = Info.tagBank
                    .Select(NormalizeTagIdForSave)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                Info.tagBank.Clear();
                foreach (var tag in normalizedBank)
                    Info.tagBank.Add(tag);
            }

            if (Info.tagPool != null)
            {
                foreach (var poolItem in Info.tagPool)
                {
                    if (poolItem != null)
                        poolItem.Item1 = NormalizeTagIdForSave(poolItem.Item1);
                }
            }

            if (Info.tagPool != null)
            {
                var uniqueTags = new Dictionary<string, TagPool>();
                foreach (var tag in Info.tagPool)
                {
                    string pureId = tag.Item1;
                    if (!string.IsNullOrWhiteSpace(pureId) && !uniqueTags.ContainsKey(pureId))
                        uniqueTags[pureId] = tag;
                }
                if (uniqueTags.Count != Info.tagPool.Count)
                {
                    Info.tagPool.Clear();
                    foreach (var tag in uniqueTags.Values)
                        Info.tagPool.Add(tag);
                }
            }

            // Убираем из tagBank те, что уже есть в tagPool

            if (Info.tagBank != null && Info.tagPool != null)
            {
                var openedTagIds = new HashSet<string>(Info.tagPool.Select(t => t.Item1), StringComparer.Ordinal);
                var toRemove = Info.tagBank
                    .Where(t => !GenreTags.Contains(t) && openedTagIds.Contains(t))
                    .ToList();
                foreach (var tag in toRemove)
                    Info.tagBank.Remove(tag);
            }

            var closedListBox = new ListBox
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                FontSize = 12,
                SelectionMode = SelectionMode.Extended
            };

            var openedListBox = new ListBox
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0xCA, 0x4A)),
                FontSize = 12,
                SelectionMode = SelectionMode.Extended
            };

            var openButton = new Button
            {
                Content = Tr("→ Open Selected →", "→ Открыть выбранное →"),
                Margin = new Thickness(5),
                Padding = new Thickness(15, 10, 15, 10),
                Background = new SolidColorBrush(Color.FromRgb(0x4A, 0xCA, 0x4A)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
                Width = 150
            };

            var closeButton = new Button
            {
                Content = Tr("← Close Selected ←", "← Закрыть выбранное ←"),
                Margin = new Thickness(5),
                Padding = new Thickness(15, 10, 15, 10),
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
                Width = 150
            };

            var openAllButton = new Button
            {
                Content = Tr("Open All", "Открыть все"),
                Margin = new Thickness(5),
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(0x4A, 0xCA, 0x4A)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Width = 100
            };

            var closeAllButton = new Button
            {
                Content = Tr("Close All", "Закрыть все"),
                Margin = new Thickness(5),
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Width = 100
            };

            var exitButton = new Button
            {
                Content = Tr("Exit", "Выход"),
                Margin = new Thickness(5),
                Padding = new Thickness(20, 8, 20, 8),
                Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Width = 100
            };

            void UpdateUI()
            {
                if (Info.tagBank != null && Info.tagBank.Count > 0)
                {
                    var sortedClosed = Info.tagBank
                        .Where(t => !GenreTags.Contains(t))
                        .OrderBy(t => GetCategoryOrder(t))
                        .ThenBy(t =>
                        {
                            string localized = t;
                            if (LocaleTranslator != null && LocaleTranslator.ContainsKey(t))
                            {
                                localized = LocaleTranslator[t];
                                localized = localized?.Replace("<nobr>", "").Replace("</nobr>", "");
                            }
                            return localized ?? t;
                        })
                        .Select(t => GetLocalizedTagNameWithCategory(t))
                        .ToList();
                    closedListBox.ItemsSource = sortedClosed;
                    openButton.IsEnabled = true;
                }
                else
                {
                    closedListBox.ItemsSource = new List<string>();
                    openButton.IsEnabled = false;
                }

                if (Info.tagPool != null && Info.tagPool.Count > 0)
                {
                    var sortedOpened = Info.tagPool
                        .Where(t => !GenreTags.Contains(t.Item1))
                        .OrderBy(t => GetCategoryOrder(t.Item1))
                        .ThenBy(t =>
                        {
                            string localized = t.Item1;
                            if (LocaleTranslator != null && LocaleTranslator.ContainsKey(t.Item1))
                            {
                                localized = LocaleTranslator[t.Item1];
                                localized = localized?.Replace("<nobr>", "").Replace("</nobr>", "");
                            }
                            return localized ?? t.Item1;
                        })
                        .Select(t => GetLocalizedTagNameWithCategory(t.Item1))
                        .ToList();
                    openedListBox.ItemsSource = sortedOpened;
                    closeButton.IsEnabled = true;
                }
                else
                {
                    openedListBox.ItemsSource = new List<string>();
                    closeButton.IsEnabled = false;
                }
            }

            openButton.Click += (s, e) =>
            {
                if (Info.tagBank == null) Info.tagBank = new ObservableCollection<string>();
                if (Info.tagPool == null) Info.tagPool = new ObservableCollection<TagPool>();

                var selectedDisplay = closedListBox.SelectedItems.Cast<string>().ToList();
                if (selectedDisplay.Count == 0) return;

                var candidateIds = Info.tagBank.Where(t => !GenreTags.Contains(t)).ToList();
                var selectedIds = selectedDisplay.Select(d => ResolveTagDisplayToId(d, candidateIds)).ToList();
                var openedIds = new HashSet<string>(Info.tagPool.Select(t => t.Item1));

                foreach (var itemId in selectedIds)
                {
                    if (!openedIds.Contains(itemId) && !GenreTags.Contains(itemId))
                    {
                        Info.tagPool.Add(new TagPool(itemId, new DateTime(9999, 12, 31)));
                        openedIds.Add(itemId);
                    }
                    Info.tagBank.Remove(itemId);
                }

                UpdateUI();
            };

            closeButton.Click += (s, e) =>
            {
                if (Info.tagBank == null) Info.tagBank = new ObservableCollection<string>();
                if (Info.tagPool == null) Info.tagPool = new ObservableCollection<TagPool>();

                var selectedDisplay = openedListBox.SelectedItems.Cast<string>().ToList();
                if (selectedDisplay.Count == 0) return;

                var candidateIds = Info.tagPool.Where(t => !GenreTags.Contains(t.Item1)).Select(t => t.Item1).ToList();
                var selectedIds = selectedDisplay.Select(d => ResolveTagDisplayToId(d, candidateIds)).ToList();

                foreach (var itemId in selectedIds)
                {
                    var tagToRemove = Info.tagPool.FirstOrDefault(t => t.Item1 == itemId);
                    if (tagToRemove != null)
                    {
                        if (!Info.tagBank.Contains(itemId) && !GenreTags.Contains(itemId))
                            Info.tagBank.Add(itemId);
                        Info.tagPool.Remove(tagToRemove);
                    }
                }

                UpdateUI();
            };

            openAllButton.Click += (s, e) =>
            {
                if (Info.tagBank == null) Info.tagBank = new ObservableCollection<string>();
                if (Info.tagPool == null) Info.tagPool = new ObservableCollection<TagPool>();

                var allTags = Info.tagBank.Where(t => !GenreTags.Contains(t)).ToList();
                if (allTags.Count == 0) return;

                var openedIds = new HashSet<string>(Info.tagPool.Select(t => t.Item1));

                foreach (var itemId in allTags)
                {
                    if (!openedIds.Contains(itemId))
                    {
                        Info.tagPool.Add(new TagPool(itemId, new DateTime(9999, 12, 31)));
                        openedIds.Add(itemId);
                    }
                    Info.tagBank.Remove(itemId);
                }
                UpdateUI();
            };

            closeAllButton.Click += (s, e) =>
            {
                if (Info.tagBank == null) Info.tagBank = new ObservableCollection<string>();
                if (Info.tagPool == null) Info.tagPool = new ObservableCollection<TagPool>();

                var allTags = Info.tagPool
                    .Where(t => !GenreTags.Contains(t.Item1))
                    .Select(t => t.Item1)
                    .ToList();

                foreach (var itemId in allTags)
                {
                    if (!Info.tagBank.Contains(itemId))
                        Info.tagBank.Add(itemId);

                    var tagToRemove = Info.tagPool.FirstOrDefault(t => t.Item1 == itemId);
                    if (tagToRemove != null)
                        Info.tagPool.Remove(tagToRemove);
                }
                UpdateUI();
            };

            exitButton.Click += (s, e) => window.Close();

            UpdateUI();

            var closedHeader = new TextBlock
            {
                Text = Tr("Closed Tags", "Закрытые теги"),
                Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10, 5, 10, 0)
            };

            var openedHeader = new TextBlock
            {
                Text = Tr("Opened Tags", "Открытые теги"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xCA, 0x4A)),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10, 5, 10, 0)
            };

            var leftScrollViewer = new ScrollViewer
            {
                Content = closedListBox,
                Background = Brushes.Transparent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10, 0, 10, 10)
            };

            leftScrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (s is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                    e.Handled = true;
                }
            };

            var leftPanel = new DockPanel();
            DockPanel.SetDock(closedHeader, Dock.Top);
            leftPanel.Children.Add(closedHeader);
            leftPanel.Children.Add(leftScrollViewer);

            var rightScrollViewer = new ScrollViewer
            {
                Content = openedListBox,
                Background = Brushes.Transparent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10, 0, 10, 10)
            };

            rightScrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (s is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                    e.Handled = true;
                }
            };

            var rightPanel = new DockPanel();
            DockPanel.SetDock(openedHeader, Dock.Top);
            rightPanel.Children.Add(openedHeader);
            rightPanel.Children.Add(rightScrollViewer);

            var centerPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            centerPanel.Children.Add(openButton);
            centerPanel.Children.Add(closeButton);
            centerPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10), Width = 150 });
            centerPanel.Children.Add(openAllButton);
            centerPanel.Children.Add(closeAllButton);

            var bottomPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            bottomPanel.Children.Add(exitButton);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(leftPanel, 0);
            Grid.SetRow(leftPanel, 0);
            grid.Children.Add(leftPanel);

            Grid.SetColumn(centerPanel, 1);
            Grid.SetRow(centerPanel, 0);
            grid.Children.Add(centerPanel);

            Grid.SetColumn(rightPanel, 2);
            Grid.SetRow(rightPanel, 0);
            grid.Children.Add(rightPanel);

            Grid.SetColumnSpan(bottomPanel, 3);
            Grid.SetRow(bottomPanel, 1);
            grid.Children.Add(bottomPanel);

            window.Content = grid;
            return window;
        }

        private Window CreateTechsManagerWindow()
        {
            var window = new Window
            {
                Title = Tr("Study Manager", "Панель исследований"),
                Width = 1000,
                Height = 700,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x29, 0x10, 0x10)),
                Foreground = Brushes.White,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var treeView = new TreeView
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                Margin = new Thickness(10),
                FontSize = 13
            };

            treeView.MouseDoubleClick += (s, e) =>
            {
                var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                if (treeViewItem != null && treeViewItem.Tag != null && treeViewItem.Tag is string tech)
                {
                    if (!string.IsNullOrEmpty(tech) && treeViewItem.IsEnabled)
                    {
                        ToggleTech(tech, treeView);
                        e.Handled = true;
                    }
                }
            };

            RefreshTechsTreeView(treeView);

            var openAllSelectedButton = new Button
            {
                Content = Tr("Open all", "Открыть все"),
                Margin = new Thickness(5),
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(0x4A, 0xCA, 0x4A)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Width = 120,
                ToolTip = Tr("Open all available perks", "Открыть все доступные исследования")
            };

            var allClosedButton = new Button
            {
                Content = Tr("Close all", "Закрыть все"),
                Margin = new Thickness(5),
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Width = 120,
                ToolTip = Tr("Close all opened perks", "Закрыть все открытые исследования")
            };

            var exitButton = new Button
            {
                Content = Tr("Exit", "Выход"),
                Margin = new Thickness(5),
                Padding = new Thickness(20, 8, 20, 8),
                Background = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Width = 100
            };

            openAllSelectedButton.Click += (s, e) =>
            {
                var allAvailable = Info.AvailablePerks.ToList();
                var opened = new List<string>();

                foreach (var tech in allAvailable)
                {
                    Info.openedPerks.Add(tech);
                    Info.AvailablePerks.Remove(tech);
                    opened.Add(tech);
                }

                if (opened.Any())
                {
                    string openedNames = string.Join(", ", opened.Take(5).Select(t =>
                    {
                        if (MainModel.LocaleTranslator.ContainsKey(t))
                            return MainModel.LocaleTranslator[t].Replace("<nobr>", "").Replace("</nobr>", "");
                        return t;
                    }));

                    if (opened.Count > 5)
                        openedNames += Tr(" and ", " и ") + (opened.Count - 5) + Tr(" more...", " ещё...");

                    MessageBox.Show(Tr("Opened researches: ", "Открыто исследований: ") + opened.Count + "\n" + openedNames,
                        Tr("Opening Researches", "Открытие исследований"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Tr("No available researches to open.", "Нет доступных исследований для открытия."),
                        Tr("Opening Researches", "Открытие исследований"), MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RefreshTechsTreeView(treeView);
            };

            allClosedButton.Click += (s, e) =>
            {
                var allOpened = Info.openedPerks.ToList();
                var closed = new List<string>();

                foreach (var tech in allOpened)
                {
                    Info.AvailablePerks.Add(tech);
                    Info.openedPerks.Remove(tech);
                    closed.Add(tech);
                }

                if (closed.Any())
                {
                    string closedNames = string.Join(", ", closed.Take(5).Select(t =>
                    {
                        if (MainModel.LocaleTranslator.ContainsKey(t))
                            return MainModel.LocaleTranslator[t].Replace("<nobr>", "").Replace("</nobr>", "");
                        return t;
                    }));

                    if (closed.Count > 5)
                        closedNames += Tr(" and ", " и ") + (closed.Count - 5) + Tr(" more...", " ещё...");

                    MessageBox.Show(Tr("Closed researches: ", "Закрыто исследований: ") + closed.Count + "\n" + closedNames,
                        Tr("Closing Researches", "Закрытие исследований"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Tr("No opened researches to close.", "Нет открытых исследований для закрытия."),
                        Tr("Closing Researches", "Закрытие исследований"), MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RefreshTechsTreeView(treeView);
            };

            exitButton.Click += (s, e) => window.Close();

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            buttonPanel.Children.Add(openAllSelectedButton);
            buttonPanel.Children.Add(allClosedButton);
            buttonPanel.Children.Add(exitButton);

            var mainScrollViewer = new ScrollViewer
            {
                Content = treeView,
                Background = Brushes.Transparent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10, 0, 10, 10)
            };

            mainScrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (s is ScrollViewer sv)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                    e.Handled = true;
                }
            };

            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(mainScrollViewer);

            window.Content = mainPanel;
            return window;
        }

        private void RefreshTechsTreeView(TreeView treeView)
        {
            treeView.Items.Clear();

            var departments = stateJson.GetDepartments(Info.AvailablePerks, Info.openedPerks);

            for (int i = 0; i < departments.Count; i++)
            {
                var department = departments[i];

                var departmentItem = new TreeViewItem
                {
                    Header = GetDepartmentDisplayName(department.DisplayName) + " [" + department.CurrentLevel + "/" + department.MaxLevel + "]",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                    FontWeight = FontWeights.Bold,
                    IsExpanded = true,
                    Background = Brushes.Transparent
                };

                int techIndex = 0;
                foreach (var tech in department.Techs)
                {
                    techIndex++;
                    bool isOpened = Info.openedPerks.Contains(tech);
                    bool isAvailable = Info.AvailablePerks.Contains(tech);

                    if (isOpened || isAvailable)
                    {
                        var techItem = new TreeViewItem
                        {
                            Background = Brushes.Transparent,
                            IsEnabled = true
                        };

                        var panel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Background = Brushes.Transparent
                        };

                        var stateIcon = new TextBlock
                        {
                            Text = isOpened ? "✅ " : "🔲 ",
                            Margin = new Thickness(0, 0, 5, 0),
                            Background = Brushes.Transparent
                        };
                        panel.Children.Add(stateIcon);

                        var techName = new TextBlock();
                        string cleanTech = RemoveNobrTags(tech);
                        techName.SetBinding(TextBlock.TextProperty,
                            new Binding(".")
                            {
                                Converter = new LangStringConverter(),
                                Source = cleanTech
                            });
                        techName.Foreground = isOpened ?
                            new SolidColorBrush(Color.FromRgb(0x90, 0xEE, 0x90)) :
                            new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
                        techName.Background = Brushes.Transparent;
                        panel.Children.Add(techName);

                        if (!isOpened && stateJson.TechDependencies.ContainsKey(tech))
                        {
                            var required = stateJson.TechDependencies[tech];
                            var missingRequired = required.Where(r => !Info.openedPerks.Contains(r)).ToList();

                            if (missingRequired.Any())
                            {
                                var lockIcon = new TextBlock
                                {
                                    Text = " 🔒",
                                    Foreground = Brushes.Gray,
                                    Background = Brushes.Transparent,
                                    ToolTip = Tr("Required: ", "Требуется: ") + string.Join(", ", missingRequired.Select(t =>
                                    {
                                        if (MainModel.LocaleTranslator.ContainsKey(t))
                                            return MainModel.LocaleTranslator[t].Replace("<nobr>", "").Replace("</nobr>", "");
                                        return t;
                                    }))
                                };
                                panel.Children.Add(lockIcon);
                                techItem.IsEnabled = false;
                            }
                        }

                        techItem.Header = panel;
                        techItem.Tag = tech;
                        techItem.Foreground = isOpened ?
                            new SolidColorBrush(Color.FromRgb(0x90, 0xEE, 0x90)) :
                            new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));

                        departmentItem.Items.Add(techItem);

                        if (department.Name == "TECH" && techIndex == 4)
                        {
                            AddTechSeparator(departmentItem);
                        }

                        if (department.Name == "PRODUCTION")
                        {
                            if (techIndex == 1)
                            {
                                AddProductionSeparator(departmentItem);
                            }
                            else if (techIndex == 4)
                            {
                                AddProductionSeparator(departmentItem);
                            }
                            else if (techIndex == 7)
                            {
                                AddProductionSeparator(departmentItem);
                            }
                        }

                        if (department.Name == "HR")
                        {
                            if (tech == "BAD_ATTITUDE_NO_SADNESS")
                            {
                                AddHRSeparator(departmentItem);
                            }
                        }

                        if (department.Name == "PR")
                        {
                            if (tech == "CHARITY_TO_REP")
                            {
                                AddPRSeparator(departmentItem);
                            }
                            else if (tech == "GENERATION_REP_X2")
                            {
                                AddPRSeparator(departmentItem);
                            }
                            else if (tech == "PROFITABLE_MOVIE_REP_2")
                            {
                                AddPRSeparator(departmentItem);
                            }
                            else if (tech == "TECH_SALE_PP")
                            {
                                AddPRSeparator(departmentItem);
                            }
                        }
                        if (department.Name == "POST")
                        {
                            if (tech == "POST_DIR_MONT_COMP_XP_1")
                            {
                                AddPostSeparator(departmentItem);
                            }
                            else if (tech == "LAB_INHOUSE_TIME_1")
                            {
                                AddPostSeparator(departmentItem);
                            }
                            else if (tech == "SOUND_INHOUSE_TIME_1")
                            {
                                AddPostSeparator(departmentItem);
                            }
                        }
                        if (department.Name == "DISTRIBUTION")
                        {
                            if (tech == "MOVIE_THEATRE_SLOT_RENT")
                            {
                                AddDistributionSeparator(departmentItem);
                            }
                            else if (tech == "ANALYSIS_BUDGET")
                            {
                                AddDistributionSeparator(departmentItem);
                            }
                            else if (tech == "PRINT_INHOUSE_QLT_2")
                            {
                                AddDistributionSeparator(departmentItem);
                            }
                            else if (tech == "SCANDAL_COVER_UP_PP")
                            {
                                AddDistributionSeparator(departmentItem);
                            }
                            else if (tech == "WM_DEBT")
                            {
                                AddDistributionSeparator(departmentItem);
                            }
                        }
                        if (department.Name == "SCRIPT")
                        {
                            if (tech == "EDITS_ON_GO")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "SCREENPLAY_TIME_RED_3")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "NEW_SCREENPLAY_PP_BONUS_2")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "NEW_SCREENPLAY_XP_BONUS_3")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "SCEN_IDEAS_GEN_AMT_2")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "MOVIE_RELEASE_TOP10_ART_XP_1")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "BLDG_COPYRIGHT")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "TAGS_SLOTS_10")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "NEW_TAG_BY_LT_2")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "TAGS_RESEARCH_TIME_RED_3")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "TAGS_XP_BONUS_3")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "TAGS_NEW_PP_BONUS")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "SCRIPT_DOCTORS_SCORES")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                            else if (tech == "BLDG_CONSTRUCTOR")
                            {
                                AddScriptSeparator(departmentItem);
                            }
                        }
                        if (department.Name == "PREPROD")
                        {
                            if (tech == "BLDG_SUPPLY")
                            {
                                AddPreprodSeparator(departmentItem);
                            }
                            else if (tech == "PROPS_QLT_3")
                            {
                                AddPreprodSeparator(departmentItem);
                            }
                            else if (tech == "SETS_TIME_RED_3")
                            {
                                AddPreprodSeparator(departmentItem);
                            }
                            else if (tech == "LOCATION_SEARCH_WORLD")
                            {
                                AddPreprodSeparator(departmentItem);
                            }
                            else if (tech == "PREPROD_PROD_DIR_CIN_XP_2")
                            {
                                AddPreprodSeparator(departmentItem);
                            }
                            else if (tech == "EXTRAS_4")
                            {
                                AddPreprodSeparator(departmentItem);
                            }
                        }
                        if (department.Name == "COMFORT")
                        {
                            if (tech == "WG_SPORTCAR")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                            else if (tech == "BLDG_EVENTS_STAGE")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                            else if (tech == "OFFICIAL_RECEPTION_3")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                            else if (tech == "PARTY_3")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                            else if (tech == "PERSONAL_DRIVER_PREMIUM")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                            else if (tech == "VILLA")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                            else if (tech == "SPOUSES_ASSISTANT")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                            else if (tech == "BG_UNDERAGE")
                            {
                                AddComfortSeparator(departmentItem);
                            }
                        }
                        if (department.Name == "SECURITY")
                        {
                            if (tech == "SECURITY_SCHOOL_STRONG")
                            {
                                AddSecuritySeparator(departmentItem);
                            }
                            else if (tech == "BLDG_SHENANIGANS")
                            {
                                AddSecuritySeparator(departmentItem);
                            }
                            else if (tech == "SPYING_XP_BONUS_2")
                            {
                                AddSecuritySeparator(departmentItem);
                            }
                            else if (tech == "LEAK_RISK_REDUCE_1")
                            {
                                AddSecuritySeparator(departmentItem);
                            }
                            else if (tech == "BLDG_SPIES")
                            {
                                AddSecuritySeparator(departmentItem);
                            }
                            else if (tech == "ACTIVE_PROTECTION_XP_BONUS_2")
                            {
                                AddSecuritySeparator(departmentItem);
                            }
                            else if (tech == "FAIL_DISCLOSURE_NO_LEAK")
                            {
                                AddSecuritySeparator(departmentItem);
                            }
                        }
                    }
                }

                if (departmentItem.Items.Count > 0)
                {
                    treeView.Items.Add(departmentItem);

                    if (i < departments.Count - 1)
                    {
                        var spacerPanel = new StackPanel
                        {
                            Height = 10,
                            Background = Brushes.Transparent
                        };

                        var spacerItem = new TreeViewItem
                        {
                            Header = spacerPanel,
                            IsEnabled = false,
                            Focusable = false,
                            Background = Brushes.Transparent
                        };
                        treeView.Items.Add(spacerItem);
                    }
                }
            }
        }

        private void AddTechSeparator(TreeViewItem departmentItem)
        {
            var separatorPanel = new StackPanel
            {
                Margin = new Thickness(0, 5, 0, 5),
                Background = Brushes.Transparent
            };
            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 0, 20, 0),
                Opacity = 0.5
            };
            separatorPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = separatorPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddProductionSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 8,
                Background = Brushes.Transparent
            };

            var spacerItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(spacerItem);
        }

        private void AddHRSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddPRSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddPostSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddDistributionSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddScriptSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddPreprodSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddComfortSeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void AddSecuritySeparator(TreeViewItem departmentItem)
        {
            var spacerPanel = new StackPanel
            {
                Height = 15,
                Background = Brushes.Transparent
            };

            var line = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(20, 5, 20, 5),
                Opacity = 0.5
            };
            spacerPanel.Children.Add(line);

            var separatorItem = new TreeViewItem
            {
                Header = spacerPanel,
                IsEnabled = false,
                Focusable = false,
                Background = Brushes.Transparent
            };
            departmentItem.Items.Add(separatorItem);
        }

        private void ToggleTech(string tech, TreeView treeView)
        {
            if (string.IsNullOrEmpty(tech)) return;

            if (Info.openedPerks.Contains(tech))
            {
                var alsoClosed = new List<string>();
                var allOpenedTechs = Info.openedPerks.ToList();
                var dependants = new List<string>();
                FindAllDependants(tech, allOpenedTechs, dependants);

                Info.AvailablePerks.Add(tech);
                Info.openedPerks.Remove(tech);

                foreach (var dep in dependants)
                {
                    if (Info.openedPerks.Contains(dep))
                    {
                        Info.AvailablePerks.Add(dep);
                        Info.openedPerks.Remove(dep);
                        alsoClosed.Add(dep);
                    }
                }

                if (alsoClosed.Any())
                {
                    string depNames = string.Join(", ", alsoClosed.Select(t =>
                    {
                        string name = t;
                        if (MainModel.LocaleTranslator.ContainsKey(t))
                        {
                            name = MainModel.LocaleTranslator[t];
                        }
                        return name.Replace("<nobr>", "").Replace("</nobr>", "");
                    }));

                    MessageBox.Show(Tr("Dependent technologies also closed: ", "Также закрыты зависимые технологии: ") + depNames,
                        Tr("Dependencies Closed", "Закрыты зависимости"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (Info.AvailablePerks.Contains(tech))
            {
                if (stateJson.TechDependencies.ContainsKey(tech))
                {
                    var required = stateJson.TechDependencies[tech];
                    var missingRequired = required.Where(r => !Info.openedPerks.Contains(r)).ToList();

                    if (missingRequired.Any())
                    {
                        string techNames = string.Join(", ", missingRequired.Select(t =>
                        {
                            if (MainModel.LocaleTranslator.ContainsKey(t))
                                return MainModel.LocaleTranslator[t].Replace("<nobr>", "").Replace("</nobr>", "");
                            return t;
                        }));

                        MessageBox.Show(Tr("Cannot open: required previous technologies: ", "Нельзя открыть: требуются предыдущие технологии: ") + techNames,
                            Tr("Missing Dependencies", "Отсутствуют зависимости"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                Info.openedPerks.Add(tech);
                Info.AvailablePerks.Remove(tech);
            }

            RefreshTechsTreeView(treeView);
        }

        private void FindAllDependants(string tech, List<string> allOpenedTechs, List<string> result)
        {
            foreach (var kvp in stateJson.TechDependencies)
            {
                if (kvp.Value.Contains(tech) && allOpenedTechs.Contains(kvp.Key))
                {
                    if (!result.Contains(kvp.Key))
                    {
                        result.Add(kvp.Key);
                    }
                    FindAllDependants(kvp.Key, allOpenedTechs, result);
                }
            }
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T t)
                    return t;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private List<string> GetSelectedTechs(TreeView treeView)
        {
            var selected = new List<string>();

            foreach (TreeViewItem category in treeView.Items)
            {
                foreach (TreeViewItem tech in category.Items)
                {
                    if (tech.Tag == null) continue;

                    if (tech.IsSelected && tech.IsEnabled)
                    {
                        if (tech.Tag is string techName)
                        {
                            selected.Add(techName);
                        }
                    }
                }
            }

            return selected;
        }

        private string RemoveNobrTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("<nobr>", "").Replace("</nobr>", "");
        }

        #endregion

        #region Cmd

        public CommandHandler OpenFileCmd
        {
            get
            {
                return _openfile ?? (_openfile = new CommandHandler(async obj =>
                {
                    try
                    {
                        if ((string)obj != "OFD")
                        {
                            var ofd = new OpenFileDialog();
                            ofd.ValidateNames = false;
                            ofd.CheckFileExists = false;
                            ofd.CheckPathExists = true;
                            ofd.FileName = "Select Folder";
                            ofd.Title = "Select DIR with Locale (Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Localization\\RUS\\)";
                            if (ofd.ShowDialog() == true)
                            {
                                string selectedPath = Path.GetDirectoryName(ofd.FileName);
                                lock (_localeLock)
                                {
                                    LocaleNames.Clear();
                                    LocaleTranslator.Clear();
                                }

                                await LoadNamesFromJson(selectedPath);
                                await LoadLocaleFromJson(selectedPath);

                                StatusBarText = Tr("Loaded new locale", "Загружена новая локаль");
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    RefershLocale();
                                    ProfList = ProfList;
                                    StudioList = StudioList;
                                    StudioListForChar = StudioList != null ? StudioList.Where(t => t != "All").ToList() : new List<string>();

                                    if (Filtered_Obj != null && Filtered_Obj.Count > 0)
                                    {
                                        var currentChar = SelectedChar;
                                        SelectedChar = null;
                                        SelectedChar = currentChar;
                                    }

                                    StatusBarText = Tr("Locale changed successfully!", "Язык успешно изменён!");
                                });
                            }
                        }
                        else
                        {
                            var ofdd = new OpenFileDialog();
                            ofdd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low\\Weappy\\Hollywood Animal\\Saves\\Profiles";
                            ofdd.Multiselect = false;
                            ofdd.Title = Tr("Select save file", "Выберите файл сохранения");
                            ofdd.DefaultExt = ".json";
                            ofdd.RestoreDirectory = true;
                            ofdd.Filter = "Json|*.json";

                            if (ofdd.ShowDialog() == true)
                            {
                                await Task.Run(async () =>
                                {
                                    opennedfileplace = Path.GetDirectoryName(ofdd.FileName);
                                    await ParseJson(ofdd.FileName);
                                    GC.Collect();
                                    MyStudio = Info.studioName;
                                    Filtered_Obj = Info.characters;

                                    Save_Loaded = true;
                                    if (Filtered_Obj != null && Filtered_Obj.Count > 0)
                                        SelectedChar = Filtered_Obj[0];
                                    RefershLocale();
                                });
                                GC.Collect();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Save_Loaded = false;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                },
                (obj) => true));
            }
        }

        public CommandHandler OpenSettingsCmd
        {
            get
            {
                return _openSettings ?? (_openSettings = new CommandHandler(async obj =>
                {
                    Settings_done = false;

                    var settingsWindow = new SettingsWindow();
                    settingsWindow.Owner = Application.Current.MainWindow;
                    settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    settingsWindow.ShowDialog();

                    Settings_done = true;
                    await Task.Delay(2000);
                    Settings_done = false;
                },
                (obj) => Save_Loaded));
            }
        }

        public CommandHandler AddTraitCmd
        {
            get
            {
                return _addtrait ?? (_addtrait = new CommandHandler(obj =>
                {
                    if (SelectedChar != null && obj != null)
                    {
                        string trait = obj.ToString();
                        if (!string.IsNullOrEmpty(trait))
                        {
                            SelectedChar.labels.Insert(0, trait);
                            SelectedChar.SetAvTraits();
                            SelectedChar.UpdateFilteredLabels();
                        }
                    }
                }, (obj) => SelectedChar != null && obj != null && !string.IsNullOrEmpty(obj.ToString())));
            }
        }

        public CommandHandler MoveTraitUpCmd
        {
            get
            {
                return _removeTraitUp ?? (_removeTraitUp = new CommandHandler(obj =>
                {
                    if (SelectedChar != null && obj != null)
                    {
                        string trait = obj.ToString();
                        int ind = SelectedChar.labels.IndexOf(trait);
                        if (ind > 0)
                        {
                            SelectedChar.labels.Move(ind, ind - 1);
                            SelectedChar.UpdateFilteredLabels();
                        }
                    }
                }, (obj) => SelectedChar != null && obj != null && SelectedChar.labels.IndexOf(obj.ToString()) > 0));
            }
        }

        public CommandHandler RemoveTraitCmd
        {
            get
            {
                return _removetrait ?? (_removetrait = new CommandHandler(obj =>
                {
                    if (SelectedChar != null && obj != null)
                    {
                        string trait = obj.ToString();
                        SelectedChar.labels.Remove(trait);
                        SelectedChar.SetAvTraits();
                        SelectedChar.UpdateFilteredLabels();
                    }
                }, (obj) => true));
            }
        }

        public CommandHandler AddSkillCmd
        {
            get
            {
                return _addskill ?? (_addskill = new CommandHandler(obj =>
                {
                    if (SelectedChar.whiteTagsNEW.Any(t => t.id == (string)obj))
                        return;
                    SelectedChar.whiteTagsNEW.Insert(0, new WhiteTag((string)obj, 12.0));
                    SelectedChar.SetAvSkills();
                }, (obj) => !string.IsNullOrEmpty((string)obj)));
            }
        }

        public CommandHandler RemoveSkillCmd
        {
            get
            {
                return _removeskill ?? (_removeskill = new CommandHandler(obj =>
                {
                    var a = SelectedChar.whiteTagsNEW.Single(t => t.id == ((WhiteTag)obj).id);
                    SelectedChar.whiteTagsNEW.Remove(a);
                    SelectedChar.SetAvSkills();
                }, (obj) => true));
            }
        }

        public CommandHandler SetMoodAndAttCmd
        {
            get
            {
                return _setmoodandatt ?? (_setmoodandatt = new CommandHandler(obj =>
                {
                    if (filtered_Obj != null && filtered_Obj.Count > 0)
                        foreach (var item in Filtered_Obj)
                        {
                            item.mood = item.attitude = 1.00;
                        }
                }, (obj) => filtered_Obj != null && filtered_Obj.Count > 0));
            }
        }

        public CommandHandler SetMaxContrDaysCmd
        {
            get
            {
                return _setcontrdays ?? (_setcontrdays = new CommandHandler(obj =>
                {
                    if (filtered_Obj != null && filtered_Obj.Count > 0)
                        foreach (var item in Filtered_Obj)
                        {
                            if (item.contract != null)
                            {
                                if (item.contract.contractType != 2)
                                {
                                    item.contract.DaysLeft = item.contract.amount * 365;
                                }
                            }
                        }
                }, (obj) => filtered_Obj != null && filtered_Obj.Count > 0));
            }
        }

        public CommandHandler SetAgeToYoungCmd
        {
            get
            {
                return _setagetoyoung ?? (_setagetoyoung = new CommandHandler(obj =>
                {
                    if (filtered_Obj != null && filtered_Obj.Count > 0)
                        foreach (var item in Filtered_Obj)
                        {
                            item.Age = 18;
                        }
                }, (obj) => filtered_Obj != null && filtered_Obj.Count > 0));
            }
        }

        public CommandHandler SetAllSkillsCmd
        {
            get
            {
                return _setallskills ?? (_setallskills = new CommandHandler(obj =>
                {
                    if (filtered_Obj != null && filtered_Obj.Count > 0)
                        foreach (var item in Filtered_Obj)
                        {
                            foreach (var skill in item.whiteTagsNEW)
                            {
                                if (skill.Value < 12)
                                    skill.Value = 12.0;
                            }
                            foreach (var avsk in item.AvalibaleSkills)
                            {
                                item.whiteTagsNEW.Insert(0, new WhiteTag(avsk, 12.0));
                            }
                            item.SetAvSkills();
                        }
                }, (obj) => filtered_Obj != null && filtered_Obj.Count > 0));
            }
        }

        public CommandHandler SetSkillToLimitCmd
        {
            get
            {
                return _setskilltolimit ?? (_setskilltolimit = new CommandHandler(obj =>
                {
                    if (filtered_Obj != null && filtered_Obj.Count > 0)
                        foreach (var item in Filtered_Obj)
                        {
                            item.professions.Value = item.limit;
                        }
                }, (obj) => filtered_Obj != null && filtered_Obj.Count > 0));
            }
        }

        public CommandHandler SetLimitToMaxCmd
        {
            get
            {
                return _setskiiltocap ?? (_setskiiltocap = new CommandHandler(obj =>
                {
                    if (filtered_Obj != null && filtered_Obj.Count > 0)
                        foreach (var item in Filtered_Obj)
                        {
                            item.limit = 1.00d;
                        }
                }, (obj) => filtered_Obj != null && filtered_Obj.Count > 0));
            }
        }

        public CommandHandler ShowSpawnDateCmd
        {
            get
            {
                return _showspawndate ?? (_showspawndate = new CommandHandler(obj =>
                {
                    ShowSpawn = !ShowSpawn;
                }, (obj) => true));
            }
        }

        public CommandHandler ShowTagsCmd
        {
            get
            {
                return _showtags ?? (_showtags = new CommandHandler(obj =>
                {
                    ShowTags = !ShowTags;
                }, (obj) => true));
            }
        }

        public CommandHandler ShowTechsCmd
        {
            get
            {
                return _showtechs ?? (_showtechs = new CommandHandler(obj =>
                {
                    ShowTechs = !ShowTechs;
                }, (obj) => true));
            }
        }

        public CommandHandler UnlockTechsCmd
        {
            get
            {
                return _unlocktechs ?? (_unlocktechs = new CommandHandler(obj =>
                {
                    if (Info.AvailablePerks.Count > 0)
                    {
                        foreach (var item in Info.AvailablePerks)
                        {
                            Info.openedPerks.Add(item);
                        }
                        Info.AvailablePerks.Clear();
                    }
                }, (obj) => true));
            }
        }

        public CommandHandler UnlockTagsCmd
        {
            get
            {
                return _unlocktags ?? (_unlocktags = new CommandHandler(obj =>
                {
                    if (Info.tagBank.Count > 0)
                    {
                        foreach (string tag in Info.tagBank)
                        {
                            Info.tagPool.Add(new TagPool(tag, Info.Now.AddDays(-1)));
                        }
                        Info.tagBank.Clear();
                    }
                }, (obj) => true));
            }
        }

        public CommandHandler ChangePortraitCmd
        {
            get
            {
                if (_changePortrait == null)
                {
                    _changePortrait = new CommandHandler(
                        execute: obj =>
                        {
                            if (SelectedChar == null)
                            {
                                StatusBarText = Tr("No character selected", "Персонаж не выбран");
                                return;
                            }

                            try
                            {
                                var portraitSelector = new PortraitSelectorWindow(SelectedChar);
                                portraitSelector.Owner = Application.Current.MainWindow;
                                portraitSelector.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                                Portrait_done = false;

                                if (portraitSelector.ShowDialog() == true)
                                {
                                    var currentChar = SelectedChar;
                                    SelectedChar = null;
                                    SelectedChar = currentChar;

                                    Portrait_done = true;
                                    StatusBarText = Tr("Portrait changed for ", "Портрет изменён для ") + SelectedChar.MyCustomName;
                                }
                                else
                                {
                                    Portrait_done = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(Tr("Error: ", "Ошибка: ") + ex.Message, Tr("Portrait Error", "Ошибка портрета"),
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                StatusBarText = Tr("Portrait change failed", "Смена портрета не удалась");
                            }
                        },
                        canExecute: obj => SelectedChar != null && Save_Loaded
                    );
                }
                return _changePortrait;
            }
        }

        #endregion

        #region locale

        private static readonly object _localeLock = new object();

        private async void SetLocale(string path)
        {
            try
            {
                StatusBarText = Tr("Loading locale from: ", "Загрузка локали из: ") + path;

                string namesPath = Path.Combine(path, "CHARACTER_NAMES.json");
                string nonEventPath = Path.Combine(path, "NON_EVENT.json");

                if (!File.Exists(namesPath))
                {
                    StatusBarText = Tr("CHARACTER_NAMES.json not found in ", "Файл CHARACTER_NAMES.json не найден в ") + path;
                    return;
                }

                if (!File.Exists(nonEventPath))
                {
                    StatusBarText = Tr("NON_EVENT.json not found in ", "Файл NON_EVENT.json не найден в ") + path;
                    return;
                }

                await LoadNamesFromJson(path);
                await LoadLocaleFromJson(path);
                StatusBarText = Tr("Loaded jsons", "JSON загружены");
                RefershLocale();  // Теперь это public метод
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefershLocale()
        {
            if (Application.Current != null && Application.Current.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(RefershLocale);
                return;
            }

            try
            {
                StatusBarText = Tr("Refresh locales", "Обновление локализации");

                if (Info != null && Info.characters != null && LocaleNames != null && LocaleNames.Count > 0)
                {
                    foreach (var t in Info.characters)
                    {
                        if (t != null)
                        {
                            if (!string.IsNullOrEmpty(t.lastNameId) && LocaleNames.ContainsKey(t.lastNameId))
                                t.normalLast = LocaleNames[t.lastNameId];
                            if (!string.IsNullOrEmpty(t.firstNameId) && LocaleNames.ContainsKey(t.firstNameId))
                                t.normalFirst = LocaleNames[t.firstNameId];
                        }
                    }
                }

                string currentProfFilter = Filter_Prof;
                string currentStudioFilter = Filter_studio;
                string currentTextFilter = Filter_txt;

                if (Info != null && Info.characters != null)
                {
                    ProfListWithNoTallent = Info.characters
                        .Where(c => c != null && c.professions != null)
                        .Select(t => t.professions?.ProfToDecode)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Distinct()
                        .ToList();

                    ProfListWithOutNoTallent = Info.characters
                        .Where(c => c != null && c.professions != null && c.professions.IsTalent)
                        .Select(t => t.professions.ProfToDecode)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Distinct()
                        .ToList();

                    if (ProfListWithNoTallent.Count > 0 && !ProfListWithNoTallent.Contains("All"))
                        ProfListWithNoTallent.Insert(0, "All");

                    if (ProfListWithOutNoTallent.Count > 0 && !ProfListWithOutNoTallent.Contains("All"))
                        ProfListWithOutNoTallent.Insert(0, "All");

                    ProfList = ShowOnlyTalent ? ProfListWithOutNoTallent : ProfListWithNoTallent;

                    StudioList = Info.characters
                        .Where(c => c != null && !string.IsNullOrEmpty(c.studioId))
                        .Select(t => t.studioId)
                        .Distinct()
                        .ToList();
                    StudioList.Insert(0, "All");
                    StudioListForChar = StudioList.Where(t => t != "All").ToList();
                }
                else
                {
                    ProfList = new List<string> { "All" };
                    StudioList = new List<string> { "All" };
                    StudioListForChar = new List<string>();
                    ProfListWithNoTallent = new List<string>();
                    ProfListWithOutNoTallent = new List<string>();
                }

                if (ProfList != null && ProfList.Count > 0)
                {
                    if (!string.IsNullOrEmpty(currentProfFilter) && ProfList.Contains(currentProfFilter))
                        Filter_Prof = currentProfFilter;
                    else
                        Filter_Prof = ProfList[0];
                }

                if (StudioList != null && StudioList.Count > 0)
                {
                    if (!string.IsNullOrEmpty(currentStudioFilter) && StudioList.Contains(currentStudioFilter))
                        Filter_studio = currentStudioFilter;
                    else
                        Filter_studio = StudioList[0];
                }

                Filter_txt = currentTextFilter ?? "";
                RefreshAllCharacterLabels();

                var tempProf = Filter_Prof;
                Filter_Prof = null;
                Filter_Prof = tempProf;

                // Вызываем поиск через диспетчер, чтобы ComboBox успел обновить свой SelectedItem
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SetSearched();
                    if (SelectedChar != null)
                    {
                        var current = SelectedChar;
                        SelectedChar = null;
                        SelectedChar = current;
                    }
                    StatusBarText = Tr("Refresh locales done", "Локализация обновлена");
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefershLocale: {ex.Message}");
                StatusBarText = Tr($"Localization error: {ex.Message}", $"Ошибка локализации: {ex.Message}");
            }
        }

        public void ResetLocale()
        {
            lock (_localeLock)
            {
                LocaleNames.Clear();
                LocaleTranslator.Clear();
            }
        }

        public async Task LoadLocaleFromJson(string path)
        {
            try
            {
                string fullPath = Path.Combine(path, "NON_EVENT.json");
                string json = await Task.Run(() => File.ReadAllText(fullPath));
                var map = JObject.Parse(json).SelectToken("IdMap");
                var local = JObject.Parse(json).SelectToken("locStrings");
                List<string> getout = JsonConvert.DeserializeObject<List<string>>(local.ToString());

                var newDict = new Dictionary<string, string>();
                foreach (var item in map.Children<JProperty>())
                {
                    string key = item.Name;
                    string value = getout[item.Value.ToObject<int>()];

                    if (!newDict.ContainsKey(key))
                    {
                        newDict.Add(key, value);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Duplicate key found: {key}");
                    }
                }

                LocaleTranslator = newDict;
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading locale JSON", ex);
            }
        }

        public async Task LoadNamesFromJson(string path)
        {
            try
            {
                string fullPath = Path.Combine(path, "CHARACTER_NAMES.json");
                string json = await Task.Run(() => File.ReadAllText(fullPath));
                var dt = JObject.Parse(json).SelectToken("locStrings");
                List<string> names = JsonConvert.DeserializeObject<List<string>>(dt.ToString());

                var newDict = new Dictionary<string, string>();
                int ii = 0;
                foreach (var t in names)
                {
                    string key = ii++.ToString();
                    if (!newDict.ContainsKey(key))
                    {
                        newDict.Add(key, t);
                    }
                }

                LocaleNames = newDict;
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading names JSON", ex);
            }
        }

        #endregion

        public CommandHandler ShowSpawnWindowCmd
        {
            get
            {
                return _showSpawnWindow ?? (_showSpawnWindow = new CommandHandler(obj =>
                {
                    if (Info?.NextSpawnDays == null || Info.NextSpawnDays.Count == 0)
                    {
                        MessageBox.Show(Tr("No spawn data available", "Нет данных о появлении"), Tr("Info", "Информация"), MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var window = CreateSpawnWindow();
                    window.Owner = Application.Current.MainWindow;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog();
                }, obj => true));
            }
        }

        public CommandHandler ShowTagsWindowCmd
        {
            get
            {
                return _showTagsWindow ?? (_showTagsWindow = new CommandHandler(obj =>
                {
                    var window = CreateTagsManagerWindow();
                    window.ShowDialog();
                }, obj => true));
            }
        }

        public CommandHandler ShowTechsWindowCmd
        {
            get
            {
                return _showTechsWindow ?? (_showTechsWindow = new CommandHandler(obj =>
                {
                    var window = CreateTechsManagerWindow();
                    window.ShowDialog();
                }, obj => true));
            }
        }

        public CommandHandler SaveCmd
        {
            get
            {
                return _savefile ?? (_savefile = new CommandHandler(async obj =>
                {
                    Save_done = false;

                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        bool t = await WriteChange();
                        Save_done = true;
                    });
                },
                (obj) => true));
            }
        }

        public async Task ParseJson(string path)
        {
            try
            {
                StatusBarText = Tr("Start parsing save...", "Начало разбора сохранения...");

                string jsonstr = await Task.Run(() => File.ReadAllText(path));
                _originalJsonString = jsonstr;   // сохраняем «нетронутый» JSON для будущего бэкапа

                if (string.IsNullOrEmpty(jsonstr))
                {
                    throw new Exception("File is empty!");
                }

                StatusBarText = Tr("JSON file read successfully", "Файл JSON прочитан успешно");

                using (var str_reader = new StringReader(jsonstr))
                {
                    using (var reader = new JsonTextReader(str_reader))
                    {
                        reader.FloatParseHandling = FloatParseHandling.Decimal;
                        jobj = JObject.Load(reader);
                    }
                }

                if (jobj == null)
                {
                    throw new Exception("Failed to parse JSON - jobj is null");
                }

                StatusBarText = Tr("JSON parsed, extracting stateJson...", "JSON разобран, извлечение stateJson...");

                var aa = jobj.SelectToken("stateJson");
                if (aa == null)
                {
                    throw new Exception("stateJson not found in the save file!");
                }

                StatusBarText = Tr("stateJson found, creating Info object...", "stateJson найден, создание объекта Info...");

                Info = new stateJson();
                Info.budget = aa.SelectToken("budget")?.Value<int>() ?? 0;
                Info.cash = aa.SelectToken("cash")?.Value<int>() ?? 0;
                Info.reputation = aa.SelectToken("reputation")?.Value<double>() ?? 0;
                Info.influence = aa.SelectToken("influence")?.Value<int>() ?? 0;
                Info.studioName = aa.SelectToken("studioName")?.Value<string>();
                Info.timePassed = aa.SelectToken("timePassed")?.Value<string>();

                StatusBarText = Tr("Loading milestones...", "Загрузка вех...");

                List<Milestones> mm = new List<Milestones>();
                var milestonesToken = aa.SelectToken("milestones");
                if (milestonesToken != null)
                {
                    foreach (var item in milestonesToken.Children())
                    {
                        var q = item.ToObject<JProperty>();
                        if (q != null)
                        {
                            Milestones nm = JsonConvert.DeserializeObject<Milestones>(q.Value.ToString());
                            if (nm != null && !nm.id.Contains("POLICY_ENABLE_") && nm.id.Contains("POLICY_"))
                                mm.Add(nm);
                        }
                    }
                }
                Info.milestones = new ObservableCollection<Milestones>(mm);

                StatusBarText = Tr("Loading next gen timers...", "Загрузка таймеров появления...");

                Dictionary<string, DateTime> dt_d = new Dictionary<string, DateTime>();
                var sp_d = aa.SelectToken("nextGenCharacterTimers")?.Children();
                if (sp_d != null)
                {
                    foreach (var item in sp_d)
                    {
                        foreach (var prof in item.Children())
                        {
                            var profObj = prof?.ToObject<JObject>();
                            if (profObj != null)
                            {
                                foreach (var prop in profObj.Properties())
                                {
                                    try
                                    {
                                        dt_d.Add($"PROFESSION_{prop.Name.ToUpper()}", prop.Value.ToObject<DateTime>());
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error adding spawn timer: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                Info.NextSpawnDays = new Dictionary<string, DateTime>(dt_d);

                StatusBarText = Tr("Loading opened perks...", "Загрузка открытых перков...");

                List<string> op_d = new List<string>();
                var op_p = aa.SelectToken("openedPerks")?.Children();
                if (op_p != null)
                {
                    foreach (var item in op_p)
                    {
                        string val = item?.Value<string>();
                        if (val != null)
                            op_d.Add(val);
                    }
                }
                Info.openedPerks = new ObservableCollection<string>(op_d);

                var preGenPerks = stateJson.PreGenPerks ?? new List<string>();
                var openedPerks = Info.openedPerks ?? new ObservableCollection<string>();
                Info.AvailablePerks = new ObservableCollection<string>(preGenPerks.Except(openedPerks).ToList());

                StatusBarText = Tr("Loading closed tags...", "Загрузка закрытых тегов...");

                List<string> closedTags = new List<string>();
                var bankToken = aa.SelectToken("tagBank");

                if (bankToken != null && bankToken.Type == JTokenType.Array)
                {
                    foreach (var item in bankToken.Children())
                    {
                        string tag = item?.Value<string>();
                        if (!string.IsNullOrEmpty(tag))
                        {
                            closedTags.Add(tag);
                        }
                    }
                }

                Info.tagBank = new ObservableCollection<string>(closedTags);

                if (Info.tagBank != null)
                {
                    var uniqueClosed = new HashSet<string>(Info.tagBank);
                    if (uniqueClosed.Count != Info.tagBank.Count)
                        Info.tagBank = new ObservableCollection<string>(uniqueClosed);
                }

                // Удаляем дубликаты из tagBank при загрузке

                if (Info.tagBank != null)
                {
                    var uniqueClosed = new HashSet<string>(Info.tagBank);
                    if (uniqueClosed.Count != Info.tagBank.Count)
                        Info.tagBank = new ObservableCollection<string>(uniqueClosed);
                }

                StatusBarText = Tr("Loading opened tags...", "Загрузка открытых тегов...");

                var tagsToken = aa.SelectToken("tagPool");
                ObservableCollection<TagPool> openedTags = new ObservableCollection<TagPool>();

                if (tagsToken != null && tagsToken.Type == JTokenType.Array)
                {
                    foreach (var item in tagsToken.Children())
                    {
                        try
                        {
                            var tagPoolItem = JsonConvert.DeserializeObject<TagPool>(item.ToString());
                            if (tagPoolItem != null && !string.IsNullOrEmpty(tagPoolItem.Item1))
                            {
                                openedTags.Add(tagPoolItem);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to deserialize tag pool item: {item}");
                            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                        }
                    }
                }
                Info.tagPool = openedTags;

                if (Info.tagPool != null)
                {
                    var uniqueTags = new Dictionary<string, TagPool>();
                    foreach (var tag in Info.tagPool)
                    {
                        if (!uniqueTags.ContainsKey(tag.Item1))
                            uniqueTags[tag.Item1] = tag;
                    }
                    if (uniqueTags.Count != Info.tagPool.Count)
                        Info.tagPool = new ObservableCollection<TagPool>(uniqueTags.Values);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {Info.tagBank.Count} closed tags");
                System.Diagnostics.Debug.WriteLine($"Loaded {Info.tagPool.Count} opened tags");

                StatusBarText = Tr("Loading characters...", "Загрузка персонажей...");

                Info.characters = new ObservableCollection<Character>();
                var charactersToken = aa.SelectToken("characters");

                if (charactersToken == null)
                {
                    throw new Exception("characters token not found in save file!");
                }

                int cnt = charactersToken?.Children().Count() ?? 0;
                StatusBarText = Tr("Loading characters lists... ", "Загрузка списка персонажей... ") + cnt + Tr(" characters found", " персонажей найдено");

                int counter = 0;
                if (charactersToken != null)
                {
                    foreach (var item in charactersToken.Children())
                    {
                        if (item != null)
                        {
                            try
                            {
                                var charct = Character.BuildCharacter(item, Info.Now);
                                if (charct != null)
                                {
                                    Info.characters.Add(charct);
                                }
                                counter++;

                                if (counter % 10 == 0 || counter == cnt)
                                {
                                    double progress = (double)counter / (double)cnt * 100;
                                    StatusBarText = Tr("Loading characters... ", "Загрузка персонажей... ") + $"{progress:F1}% ({counter}/{cnt})";
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error loading character {counter}: {ex.Message}");
                            }
                        }
                    }
                }

                StatusBarText = Tr("Loaded ", "Загружено ") + Info.characters.Count + Tr(" characters successfully!", " персонажей успешно!");

                StudioList = Info.characters?.Select(t => t.studioId).Distinct().ToList() ?? new List<string>();
                StudioList = StudioList.Where(s => !string.IsNullOrEmpty(s)).ToList();
                StudioList.Insert(0, "All");

                ProfListWithNoTallent = Info.characters?
                    .Select(t => t.professions?.ProfToDecode)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .ToList() ?? new List<string>();

                ProfListWithOutNoTallent = Info.characters?
                    .Where(t => t.professions != null && t.professions.IsTalent)
                    .Select(t => t.professions.ProfToDecode)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct()
                    .ToList() ?? new List<string>();

                if (ProfListWithNoTallent.Count > 0 && !ProfListWithNoTallent.Contains("All"))
                    ProfListWithNoTallent.Insert(0, "All");

                if (ProfListWithOutNoTallent.Count > 0 && !ProfListWithOutNoTallent.Contains("All"))
                    ProfListWithOutNoTallent.Insert(0, "All");

                ProfList = ProfListWithNoTallent;

                if (ProfList.Count > 0)
                    Filter_Prof = ProfList[0];

                if (StudioList.Count > 0)
                    Filter_studio = StudioList[0];

                StatusBarText = Tr("Parsing complete!", "Разбор завершён!");
            }
            catch (Exception ex)
            {
                string errorMessage = Tr("Error parsing save file:\n\n", "Ошибка разбора сохранения:\n\n") + ex.Message + "\n\nStack trace:\n" + ex.StackTrace;

                if (ex.InnerException != null)
                {
                    errorMessage += "\n\nInner exception:\n" + ex.InnerException.Message;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(errorMessage, Tr("Parse Error", "Ошибка разбора"), MessageBoxButton.OK, MessageBoxImage.Error);
                });

                StatusBarText = Tr("Parse failed!", "Разбор не удался!");

                throw;
            }
        }


        private static bool HasPerk(HashSet<string> opened, string perk)
        {
            return opened != null && opened.Contains(perk);
        }

        private static void AddUniqueString(JArray array, string value)
        {
            if (array == null || string.IsNullOrEmpty(value)) return;
            foreach (var item in array)
            {
                if (string.Equals(item.Value<string>(), value, StringComparison.Ordinal))
                    return;
            }
            array.Add(value);
        }

        private static void RemoveString(JArray array, string value)
        {
            if (array == null || string.IsNullOrEmpty(value)) return;
            for (int i = array.Count - 1; i >= 0; i--)
            {
                if (string.Equals(array[i].Value<string>(), value, StringComparison.Ordinal))
                    array.RemoveAt(i);
            }
        }

        private static void SyncUsedOption(JArray usedOptions, HashSet<string> opened, string perkId, string optionId)
        {
            if (usedOptions == null) return;

            if (HasPerk(opened, perkId))
                AddUniqueString(usedOptions, optionId);
            else
                RemoveString(usedOptions, optionId);
        }

        private static void SyncUnlockedService(JArray unlockedServicesAndContracts, HashSet<string> opened, string perkId, string serviceId)
        {
            if (unlockedServicesAndContracts == null) return;

            if (HasPerk(opened, perkId))
                AddUniqueString(unlockedServicesAndContracts, serviceId);
            else
                RemoveString(unlockedServicesAndContracts, serviceId);
        }

        private static void EnsureServiceSlot(JObject servicesWithSubscribers, string serviceId)
        {
            if (servicesWithSubscribers == null || string.IsNullOrEmpty(serviceId)) return;

            if (!(servicesWithSubscribers[serviceId] is JArray))
                servicesWithSubscribers[serviceId] = new JArray();
        }

        private static void RemoveServiceSlot(JObject servicesWithSubscribers, string serviceId)
        {
            if (servicesWithSubscribers == null || string.IsNullOrEmpty(serviceId)) return;
            servicesWithSubscribers.Remove(serviceId);
        }

        private static void SyncComfortService(JArray usedOptions, JObject servicesWithSubscribers, HashSet<string> opened, string perkId, string serviceId)
        {
            if (usedOptions == null) return;

            if (HasPerk(opened, perkId))
            {
                AddUniqueString(usedOptions, serviceId);
                EnsureServiceSlot(servicesWithSubscribers, serviceId);
            }
            else
            {
                RemoveString(usedOptions, serviceId);
                RemoveServiceSlot(servicesWithSubscribers, serviceId);
            }
        }

        private void NormalizeResearchCollections(JObject state)
        {
            if (Info == null) return;

            var opened = new HashSet<string>(StringComparer.Ordinal);
            if (Info.openedPerks != null)
            {
                foreach (var item in Info.openedPerks)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                        opened.Add(item);
                }
            }

            Info.openedPerks = new ObservableCollection<string>(opened.ToList());

            var preGen = stateJson.PreGenPerks ?? new List<string>();
            Info.AvailablePerks = new ObservableCollection<string>(preGen.Where(p => !opened.Contains(p)).Distinct().ToList());

            if (state != null)
            {
                var openedArray = state["openedPerks"] as JArray;
                if (openedArray == null)
                {
                    openedArray = new JArray();
                    state["openedPerks"] = openedArray;
                }

                openedArray.Clear();
                foreach (var item in Info.openedPerks)
                    openedArray.Add(item);
            }
        }

        private void SyncResearchEffects(JObject state)
        {
            if (state == null || Info == null) return;

            NormalizeResearchCollections(state);

            var opened = new HashSet<string>(Info.openedPerks ?? new ObservableCollection<string>(), StringComparer.Ordinal);

            JObject persistent = state["persistentVariables"] as JObject;
            if (persistent == null)
            {
                persistent = new JObject();
                state["persistentVariables"] = persistent;
            }

            JArray usedOptions = state["usedOptions"] as JArray;
            if (usedOptions == null)
            {
                usedOptions = new JArray();
                state["usedOptions"] = usedOptions;
            }

            JArray unlockedServicesAndContracts = state["unlockedServicesAndContracts"] as JArray;
            if (unlockedServicesAndContracts == null)
            {
                unlockedServicesAndContracts = new JArray();
                state["unlockedServicesAndContracts"] = unlockedServicesAndContracts;
            }

            JObject servicesWithSubscribers = state["servicesWithSubscribers"] as JObject;
            if (servicesWithSubscribers == null)
            {
                servicesWithSubscribers = new JObject();
                state["servicesWithSubscribers"] = servicesWithSubscribers;
            }

            // ВАЖНО: openedPerks отвечает в основном за галочки/статус исследования.
            // Реальные лимиты игра читает из persistentVariables/usedOptions/unlockedServicesAndContracts.
            // Поэтому при включении исследования добавляем эффект, а при отключении откатываем его.

            persistent["TAG_SLOT_MAX"] = HasPerk(opened, "TAGS_SLOTS_10") ? 10 :
                                         HasPerk(opened, "TAGS_SLOTS_9") ? 9 :
                                         HasPerk(opened, "TAGS_SLOTS_8") ? 8 :
                                         HasPerk(opened, "TAGS_SLOTS_7") ? 7 :
                                         HasPerk(opened, "TAGS_SLOTS_6") ? 6 : 5;

            persistent["CONTRACT_MOVIES_MAX"] = HasPerk(opened, "CONTRACT_10_MOVIES") ? 10 :
                                                HasPerk(opened, "CONTRACT_5_MOVIES") ? 5 : 3;

            persistent["CONTRACT_YEARS_MAX"] = HasPerk(opened, "CONTRACT_10_YEARS") ? 10 :
                                               HasPerk(opened, "CONTRACT_5_YEARS") ? 5 : 3;

            // Лимит исследовательских групп. Игра смотрит именно сюда,
            // поэтому одних BLDG_RND_I/II/III/IV в openedPerks недостаточно.

            persistent["BLDG_RND_MAX"] = HasPerk(opened, "BLDG_RND_IV") ? 4 :
                                         HasPerk(opened, "BLDG_RND_III") ? 3 :
                                         HasPerk(opened, "BLDG_RND_II") ? 2 :
                                         HasPerk(opened, "BLDG_RND_I") ? 1 : 0;

            // Разовые опции, которые игра хранит не только в openedPerks.

            SyncUsedOption(usedOptions, opened, "CONTRACT_WEIGHT", "CONTRACT_WEIGHT");
            SyncUsedOption(usedOptions, opened, "EXTRAS_4", "EXTRAS_MAX4");
            SyncUsedOption(usedOptions, opened, "SETS_QLT_1", "PERK_UNLOCK_SETS_QLT1");
            SyncUsedOption(usedOptions, opened, "SETS_QLT_3", "PERK_UNLOCK_SETS_QLT3");
            SyncUsedOption(usedOptions, opened, "PROPS_QLT_3", "PERK_UNLOCK_PROPS_QLT3");
            SyncUsedOption(usedOptions, opened, "LOCATION_QLT_3", "PERK_UNLOCK_LOCATION_QLT3");
            SyncUsedOption(usedOptions, opened, "TEAM_SERVICE_4", "TEAM_SERVICE_4");

            // Услуги отдела комфорта игра показывает только когда ID есть сразу в двух местах:
            // 1) usedOptions — услуга разблокирована;
            // 2) servicesWithSubscribers — есть слот услуги со списком подписчиков.
            // Если добавить только usedOptions, исследование будет открыто, но карточка услуги не появится.

            AddUniqueString(usedOptions, "INSURANCE"); // базовая медстраховка доступна всегда после открытия отдела
            EnsureServiceSlot(servicesWithSubscribers, "INSURANCE");

            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "INSURANCE_PLUS", "EXPANDED_INSURANCE");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "PERSONAL_DRIVER", "CAR_WITH_DRIVER");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "PERSONAL_DRIVER_PREMIUM", "PREMIUM_CAR");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "HOTEL_SUITE", "HOTEL_ROOM");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "PENTHOUSE", "PENTHOUSE");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "VILLA", "VILLA");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "HOUSEMAID", "MAID");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "NANNY", "NANNY");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "CHEF", "PERSONAL_CHEF");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "BUTLER", "BUTLER");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "ASSISTANT", "ASSISTANT");
            SyncComfortService(usedOptions, servicesWithSubscribers, opened, "SPOUSES_ASSISTANT", "SPOUSES_ASSISTANT");

            // Подарки персонажам тоже должны попадать в usedOptions.
            SyncUsedOption(usedOptions, opened, "WG_WATCHES", "WG_WATCHES");
            SyncUsedOption(usedOptions, opened, "WG_CIGARS", "WG_CIGARS");
            SyncUsedOption(usedOptions, opened, "WG_ALCOHOL", "WG_ALCOHOL");
            SyncUsedOption(usedOptions, opened, "WG_HAUTE_WARDROBE", "WG_HAUTE_WARDROBE");
            SyncUsedOption(usedOptions, opened, "WG_SPORTCAR", "WG_SPORTCAR");

            SyncUnlockedService(unlockedServicesAndContracts, opened, "BOOTLEGGER_AGENTS_TRAINING", "BOOTLEGGER_AGENTS_TRAINING");
        }

        public async Task<bool> WriteChange()
        {
            try
            {
                StatusBarText = Tr("Prepare to save", "Подготовка к сохранению");

                if (Info?.characters != null)
                {
                    foreach (var chr in Info.characters)
                    {
                        try
                        {
                            if (chr.contract != null)
                            {
                                if (chr.contract.dateOfSigning > Info.Now)
                                {
                                    chr.contract.dateOfSigning = Info.Now.AddDays(-1);
                                }
                                chr.contract.SetCalcDaysLeft();
                                if (chr.contract.DaysLeft < 0)
                                {
                                    chr.contract.DaysLeft = 1;
                                }
                            }

                            if (chr.professions != null && chr.professions.Value > chr.limit)
                            {
                                chr.professions.Value = chr.limit;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error validating character {chr?.id}: {ex}");
                        }
                    }
                }

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = Tr("Select where to save", "Выберите куда сохранить");
                sfd.DefaultExt = ".json";
                sfd.InitialDirectory = opennedfileplace;
                sfd.RestoreDirectory = true;
                sfd.Filter = "Json|*.json";

                if (sfd.ShowDialog() == true)
                {
                    MessageBoxResult backupChoice = MessageBox.Show(
                        Tr("Create a backup of the original save before applying changes?",
                           "Создать резервную копию исходного сохранения перед внесением изменений?"),
                        Tr("Backup", "Резервная копия"),
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (backupChoice == MessageBoxResult.Cancel)
                    {
                        StatusBarText = Tr("Save cancelled by user", "Сохранение отменено пользователем");
                        return false;
                    }

                    // Создаём бэкап нетронутого оригинала
                    if (backupChoice == MessageBoxResult.Yes && !string.IsNullOrEmpty(_originalJsonString))
                    {
                        try
                        {
                            string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
                            Directory.CreateDirectory(backupDir);
                            string originalFileName = System.IO.Path.GetFileName(sfd.FileName);
                            string destPath = System.IO.Path.Combine(backupDir, originalFileName);
                            await Task.Run(() => File.WriteAllText(destPath, _originalJsonString));
                            StatusBarText = Tr("Backup created", "Резервная копия создана");
                        }
                        catch (Exception ex)
                        {
                            StatusBarText = Tr("Backup failed: ", "Ошибка резервного копирования: ") + ex.Message;
                        }
                    }
                    else if (backupChoice == MessageBoxResult.Yes && string.IsNullOrEmpty(_originalJsonString))
                    {
                        StatusBarText = Tr("Original JSON unavailable, backup skipped",
                                           "Исходный JSON недоступен, резервная копия пропущена");
                    }

                    // Основное сохранение
                    var z = jobj["stateJson"];

                    z["reputation"] = Info.reputation;
                    z["budget"] = Info.budget;
                    z["cash"] = Info.cash;
                    z["influence"] = Info.influence;

                    if (Info.milestones != null)
                    {
                        foreach (var mil in Info.milestones)
                        {
                            if (z["milestones"]?[mil.id] != null)
                            {
                                z["milestones"][mil.id]["finished"] = mil.finished;
                                z["milestones"][mil.id]["locked"] = mil.locked;
                                z["milestones"][mil.id]["progress"] = mil.progress;
                            }
                        }
                    }

                    if (Info.openedPerks != null && z["openedPerks"] is JArray openedPerksArray)
                    {
                        openedPerksArray.Clear();
                        foreach (var item in Info.openedPerks)
                        {
                            openedPerksArray.Add(item);
                        }
                    }

                    SyncResearchEffects((JObject)z);

                    if (Info.tagPool != null && z["tagPool"] is JArray tagPoolArray)
                    {
                        tagPoolArray.Clear();
                        foreach (var item in Info.tagPool)
                        {
                            var w = JsonConvert.SerializeObject(item);
                            var tt = JObject.Parse(w);
                            tagPoolArray.Add(tt);
                        }
                    }

                    // Сохранение tagBank 
                    if (Info.tagBank != null && z["tagBank"] is JArray tagBankArray)
                    {
                        tagBankArray.Clear();
                        foreach (var item in Info.tagBank)
                        {
                            tagBankArray.Add(item);
                        }
                    }

                    StatusBarText = Tr("Saving characters...", "Сохранение персонажей...");
                    int savedCount = 0;
                    int totalCount = Info.characters?.Count ?? 0;

                    if (Info.characters != null && z["characters"] is JArray charactersArray)
                    {
                        foreach (Character chr in Info.characters)
                        {
                            try
                            {
                                savedCount++;
                                StatusBarText = Tr("Saving characters... ", "Сохранение персонажей... ") + $"{savedCount}/{totalCount}";

                                JToken b = charactersArray.FirstOrDefault(token => token["id"]?.Value<int>() == chr.id);

                                if (b != null)
                                {
                                    b["limit"] = chr.limit;
                                    b["Limit"] = chr.limit;

                                    b["portraitBaseId"] = chr.portraitBaseId;

                                    {
                                        b["mood"] = chr.mood;
                                        b["attitude"] = chr.attitude;
                                        b["birthDate"] = chr.birthDate;
                                        b["studioId"] = chr.studioId == "NONE" ? null : chr.studioId;
                                        b["deathDate"] = chr.deathDate;
                                        b["state"] = chr.state;
                                        b["causeOfDeath"] = chr.causeOfDeath;

                                        if (chr.CustomNameWasSetted && !string.IsNullOrWhiteSpace(chr.MyCustomName))
                                        {
                                            b["customName"] = chr.MyCustomName;
                                        }

                                        var cnt = b["contract"];
                                        if (cnt != null)
                                        {
                                            if (chr.contract == null)
                                            {
                                                cnt.Replace(JValue.CreateNull());
                                            }
                                            else
                                            {
                                                if (!cnt.HasValues)
                                                {
                                                    b["contract"] = JToken.Parse(JsonConvert.SerializeObject(chr.contract));
                                                }
                                                else
                                                {
                                                    cnt["amount"] = chr.contract.amount;
                                                    cnt["startAmount"] = chr.contract.startAmount;
                                                    cnt["initialFee"] = chr.contract.initialFee;
                                                    cnt["monthlySalary"] = chr.contract.monthlySalary;
                                                    cnt["weightToSalary"] = chr.contract.weightToSalary;
                                                    cnt["dateOfSigning"] = chr.contract.dateOfSigning;
                                                    cnt["contractType"] = chr.contract.contractType;
                                                }
                                            }
                                        }

                                        var prof = b["professions"];
                                        if (prof != null && prof.HasValues && chr.professions != null)
                                        {
                                            prof[chr.professions.Name] = chr.professions.Value;
                                        }

                                        var lbl = (JArray)b["labels"];
                                        if (lbl != null && chr.labels != null)
                                        {
                                            foreach (var label in chr.labels)
                                            {
                                                bool exists = false;
                                                foreach (var x in lbl)
                                                {
                                                    if (x.ToString() == label)
                                                    {
                                                        exists = true;
                                                        break;
                                                    }
                                                }
                                                if (!exists)
                                                {
                                                    lbl.Add(label);
                                                }
                                            }

                                            List<JToken> toRemove = new List<JToken>();
                                            foreach (var label in lbl)
                                            {
                                                if (!chr.labels.Contains(label.ToString()))
                                                {
                                                    toRemove.Add(label);
                                                }
                                            }
                                            foreach (var t in toRemove)
                                            {
                                                t.Remove();
                                            }
                                        }

                                        var wtgs = b["whiteTagsNEW"];
                                        if (wtgs != null && chr.whiteTagsNEW != null)
                                        {
                                            var existingTags = new Dictionary<string, JProperty>();
                                            foreach (JProperty prop in wtgs.Children<JProperty>())
                                            {
                                                existingTags[prop.Name] = prop;
                                            }

                                            foreach (var whiteTag in chr.whiteTagsNEW)
                                            {
                                                if (existingTags.TryGetValue(whiteTag.id, out var existingProp))
                                                {
                                                    var tochng = existingProp.Value;
                                                    tochng["id"] = whiteTag.id;
                                                    tochng["dateAdded"] = whiteTag.dateAdded;
                                                    tochng["movieId"] = whiteTag.movieId;
                                                    tochng["value"] = whiteTag.Value;
                                                    tochng["IsOverall"] = whiteTag.IsOverall;

                                                    var overallValues = tochng["overallValues"];
                                                    if (overallValues != null && whiteTag.ZeroPoint != null)
                                                    {
                                                        foreach (var t_over in overallValues.Children())
                                                        {
                                                            if (t_over["movieId"]?.Value<int>() == 0 &&
                                                                t_over["sourceType"]?.Value<int>() == 0)
                                                            {
                                                                t_over["value"] = whiteTag.ZeroPoint.value;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var prop = new JProperty(whiteTag.id);
                                                    prop.Value = JToken.Parse(JsonConvert.SerializeObject(whiteTag));
                                                    ((JObject)wtgs).Add(prop);
                                                }
                                            }

                                            List<JProperty> toRemoveTags = new List<JProperty>();
                                            foreach (JProperty whitetg in wtgs.Children<JProperty>())
                                            {
                                                bool exists = false;
                                                foreach (var t in chr.whiteTagsNEW)
                                                {
                                                    if (t.id == whitetg.Name)
                                                    {
                                                        exists = true;
                                                        break;
                                                    }
                                                }

                                                if (!exists && WhiteTag.GetEnumVal(whitetg.Name) != Skills.ELSE)
                                                {
                                                    toRemoveTags.Add(whitetg);
                                                }
                                            }

                                            foreach (var t in toRemoveTags)
                                            {
                                                t.Remove();
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error saving character {chr?.id}: {ex}");
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("=== DEBUG SAVE ===");
                    if (Info.characters != null)
                    {
                        foreach (var chr in Info.characters)
                        {
                            if (chr.labels != null && chr.labels.Count > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Character {chr.id} - {chr.MyCustomName} has labels: {string.Join(", ", chr.labels)}");
                            }
                            System.Diagnostics.Debug.WriteLine($"Character {chr.id} - {chr.MyCustomName} portraitBaseId: {chr.portraitBaseId}");
                        }
                    }

                    string jsonString = jobj.ToString(Formatting.None);
                    await Task.Run(() => File.WriteAllText(sfd.FileName, jsonString));

                    StatusBarText = Tr("Save completed successfully!", "Сохранение успешно завершено!");
                    Save_done = true;
                    await Task.Delay(2000);
                    Save_done = false;

                    return true;
                }
                else
                {
                    StatusBarText = Tr("Save canceled", "Сохранение отменено");
                    return false;
                }
            }
            catch (Exception ex)
            {
                StatusBarText = Tr("Error: ", "Ошибка: ") + ex.Message;
                MessageBox.Show(Tr("Error saving file:\n", "Ошибка сохранения файла:\n") + ex.Message, Tr("Error", "Ошибка"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}