using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HollywoodEditor
{
    public partial class PerksWindow : Window
    {
        private string configFilePath;
        private bool closeOnLoaded;
        private JObject configData;
        private readonly List<PerkEditorItem> items = new List<PerkEditorItem>();
        private readonly List<Expander> visibleExpanders = new List<Expander>();
        private readonly Dictionary<string, string> localizedPerkNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool filtersReady;

        private bool IsRussianLocale
        {
            get
            {
                try
                {
                    string locale = HollywoodEditor.ViewModels.MainModel.CurrentLocale;
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

                    string budgetText = Application.Current != null ? Application.Current.TryFindResource("Budget") as string : null;
                    if (budgetText == "Банк") return true;
                }
                catch { }

                return false;
            }
        }

        private string L(string en, string ru)
        {
            return IsRussianLocale ? ru : en;
        }

        public PerksWindow()
        {
            InitializeComponent();
            ApplyLocalization();
            Loaded += PerksWindow_Loaded;
            FindAndLoadConfig();
        }

        private void ApplyLocalization()
        {
            Title = L("Perks Editor", "Редактирование Perks.json");
            HeaderText.Text = L("🔬 Perks Editor", "🔬 Редактор перков");
            SaveButton.Content = L("Save", "Сохранить");
            CancelButton.Content = L("Cancel", "Отмена");
            ExpandAllButton.Content = L("Expand all", "Развернуть всё");
            CollapseAllButton.Content = L("Collapse all", "Свернуть всё");
            ReloadButton.Content = L("Restore", "Восстановить");
            BulkTitleText.Text = L("Bulk edit", "Массовое изменение");
            ApplyToAllButton.Content = L("Apply to all", "Вставить во все");
            BulkValueBox.ToolTip = L("Enter a value. Example: 0.5", "Введите значение. Например: 0.5");
            BulkHintText.Text = L("Changes the selected field in every perk loaded from Perks.json.", "Меняет выбранный параметр во всех исследованиях из Perks.json.");
            BuildBulkPropertyList();
            SearchBox.Text = string.Empty;
            SearchBox.ToolTip = L("Search by display name, ID, department, property value", "Поиск по названию, ID, отделу и значениям");
            FooterText.Text = L("A full-featured «Perks.json» editor for changing perk parameters.",
                                "Полноценный редактор «Perks.json» для изменения параметров перков.");
        }

        private string ConfigRelativePath
        {
            get { return Path.Combine("Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", "Perks.json"); }
        }

        private void FindAndLoadConfig()
        {
            try
            {
                string foundPath = FindConfigPath();
                if (!string.IsNullOrWhiteSpace(foundPath))
                {
                    configFilePath = foundPath;
                    LoadConfig(configFilePath);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    L("Perks.json was not found automatically. Select it manually?\n\nExpected location:\n...\\Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Configs\\Perks.json",
                      "Perks.json не найден автоматически. Выбрать его вручную?\n\nОжидаемый путь:\n...\\Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Configs\\Perks.json"),
                    L("File not found", "Файл не найден"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Title = L("Select Perks.json", "Выберите Perks.json");
                    dialog.Filter = "Perks.json|Perks.json|JSON files (*.json)|*.json|All files (*.*)|*.*";
                    dialog.DefaultExt = ".json";

                    if (dialog.ShowDialog() == true)
                    {
                        configFilePath = dialog.FileName;
                        LoadConfig(configFilePath);
                    }
                    else
                    {
                        closeOnLoaded = true;
                        CloseIfLoaded();
                    }
                }
                else
                {
                    closeOnLoaded = true;
                    CloseIfLoaded();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("Error finding Perks.json:\n", "Ошибка поиска Perks.json:\n") + ex.Message,
                    L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
                closeOnLoaded = true;
                CloseIfLoaded();
            }
        }

        private void PerksWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (closeOnLoaded)
                Close();
        }

        private void CloseIfLoaded()
        {
            if (IsLoaded)
                Close();
        }

        private string FindConfigPath()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                string rootName = drive.RootDirectory.FullName;
                string[] possibleRoots = new string[]
                {
                    Path.Combine(rootName, "Steam", "steamapps", "common", "Hollywood Animal"),
                    Path.Combine(rootName, "Program Files", "Steam", "steamapps", "common", "Hollywood Animal"),
                    Path.Combine(rootName, "Program Files (x86)", "Steam", "steamapps", "common", "Hollywood Animal"),
                    Path.Combine(rootName, "Games", "Hollywood Animal"),
                    Path.Combine(rootName, "GAMES", "Hollywood Animal"),
                    Path.Combine(rootName, "games", "Hollywood Animal"),
                    Path.Combine(rootName, "Игры", "Hollywood Animal")
                };

                foreach (string gameRoot in possibleRoots)
                {
                    string file = Path.Combine(gameRoot, ConfigRelativePath);
                    if (File.Exists(file)) return file;
                }
            }

            return null;
        }

        private void LoadConfig(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                configData = JObject.Parse(json);
                LoadPerkLocalization(path);
                items.Clear();

                foreach (JProperty prop in configData.Properties())
                {
                    JObject obj = prop.Value as JObject;
                    if (obj == null) continue;

                    string id = obj["id"] != null ? obj["id"].ToString() : prop.Name;
                    string department = obj["department"] != null && obj["department"].Type != JTokenType.Null ? obj["department"].ToString() : "NONE";
                    string domain = obj["domain"] != null ? obj["domain"].ToString() : "";

                    items.Add(new PerkEditorItem
                    {
                        Id = id,
                        OriginalKey = prop.Name,
                        Object = obj,
                        Department = department,
                        Domain = domain,
                        DisplayName = GetPerkDisplayName(id)
                    });
                }

                PathText.Text = L("Loaded: Perks.json", "Загружен: Perks.json");
                PathText.ToolTip = L("File path is hidden to keep the interface clean.", "Путь к файлу скрыт, чтобы не захламлять интерфейс.");
                BuildFilters();
                BuildUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("Error loading Perks.json:\n", "Ошибка загрузки Perks.json:\n") + ex.Message,
                    L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
                closeOnLoaded = true;
                CloseIfLoaded();
            }
        }

        private void BuildFilters()
        {
            filtersReady = false;

            DepartmentFilterCombo.Items.Clear();
            DepartmentFilterCombo.Items.Add(new FilterItem("", L("All departments", "Все отделы")));
            foreach (string dep in items.Select(i => i.Department ?? "NONE").Distinct().OrderBy(GetDepartmentOrder).ThenBy(d => d))
                DepartmentFilterCombo.Items.Add(new FilterItem(dep, GetDepartmentDisplayName(dep)));
            DepartmentFilterCombo.SelectedIndex = 0;

            DomainFilterCombo.Items.Clear();
            DomainFilterCombo.Items.Add(new FilterItem("", L("All domains", "Все разделы")));
            foreach (string domain in items.Select(i => i.Domain ?? "").Distinct().OrderBy(GetDomainOrder).ThenBy(d => d))
                DomainFilterCombo.Items.Add(new FilterItem(domain, GetDomainDisplayName(domain)));
            DomainFilterCombo.SelectedIndex = 0;

            filtersReady = true;
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (!filtersReady) return;
            BuildUI();
        }

        private void BuildUI()
        {
            MainPanel.Children.Clear();
            visibleExpanders.Clear();

            IEnumerable<PerkEditorItem> filtered = ApplyFilters(items);
            List<PerkEditorItem> filteredList = filtered.ToList();

            SummaryText.Text = L("Shown", "Показано") + ": " + filteredList.Count + " / " + items.Count;

            var domainGroups = filteredList
                .GroupBy(i => i.Domain ?? "")
                .OrderBy(g => GetDomainOrder(g.Key))
                .ThenBy(g => g.Key);

            foreach (var domainGroup in domainGroups)
            {
                Border domainBorder = CreateDomainBorder(domainGroup.Key, domainGroup.Count());
                StackPanel domainStack = new StackPanel();

                TextBlock domainHeader = new TextBlock
                {
                    Text = GetDomainDisplayName(domainGroup.Key) + "  [" + domainGroup.Count() + "]",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(14, 10, 14, 8)
                };
                domainStack.Children.Add(domainHeader);

                var departmentGroups = domainGroup
                    .GroupBy(i => i.Department ?? "NONE")
                    .OrderBy(g => GetDepartmentOrder(g.Key))
                    .ThenBy(g => g.Key);

                foreach (var depGroup in departmentGroups)
                {
                    Border depBorder = CreateDepartmentBorder(depGroup.Key, depGroup.Count());
                    StackPanel depStack = new StackPanel();

                    Border depHeader = new Border
                    {
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB8, 0x3A, 0x3A)),
                        CornerRadius = new CornerRadius(7, 7, 0, 0),
                        Padding = new Thickness(10, 6, 10, 6)
                    };
                    depHeader.Child = new TextBlock
                    {
                        Text = GetDepartmentIcon(depGroup.Key) + "  " + GetDepartmentDisplayName(depGroup.Key) + " [" + depGroup.Count() + "]",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = System.Windows.Media.Brushes.White
                    };
                    depStack.Children.Add(depHeader);

                    WrapPanel wrap = new WrapPanel { Margin = new Thickness(8, 8, 8, 8) };
                    foreach (PerkEditorItem perk in depGroup.OrderBy(i => i.DisplayName).ThenBy(i => i.Id))
                    {
                        wrap.Children.Add(CreatePerkCard(perk));
                    }
                    depStack.Children.Add(wrap);
                    depBorder.Child = depStack;
                    domainStack.Children.Add(depBorder);
                }

                domainBorder.Child = domainStack;
                MainPanel.Children.Add(domainBorder);
            }
        }

        private IEnumerable<PerkEditorItem> ApplyFilters(IEnumerable<PerkEditorItem> source)
        {
            string search = SearchBox.Text == null ? "" : SearchBox.Text.Trim();
            FilterItem dep = DepartmentFilterCombo.SelectedItem as FilterItem;
            FilterItem domain = DomainFilterCombo.SelectedItem as FilterItem;

            foreach (PerkEditorItem item in source)
            {
                if (dep != null && !string.IsNullOrEmpty(dep.Value) && !string.Equals(item.Department, dep.Value, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (domain != null && !string.IsNullOrEmpty(domain.Value) && !string.Equals(item.Domain, domain.Value, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string haystack = (item.DisplayName + " " + item.Id + " " + item.Department + " " + item.Domain + " " + item.Object.ToString(Formatting.None)).ToUpperInvariant();
                    if (!haystack.Contains(search.ToUpperInvariant()))
                        continue;
                }

                yield return item;
            }
        }

        private Border CreateDomainBorder(string domain, int count)
        {
            return new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA8, 0x30, 0x30)),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 14),
                Background = new SolidColorBrush(Color.FromArgb(0x22, 0xAD, 0x38, 0x38))
            };
        }

        private Border CreateDepartmentBorder(string department, int count)
        {
            return new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB8, 0x3A, 0x3A)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(10, 0, 10, 12),
                Background = new SolidColorBrush(Color.FromArgb(0x33, 0x20, 0x08, 0x08))
            };
        }

        private UIElement CreatePerkCard(PerkEditorItem perk)
        {
            Border card = new Border
            {
                Width = 360, // Было 320, увеличил до 360
                MinHeight = 72,
                Margin = new Thickness(6),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7A, 0x2A, 0x2A)),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x13, 0x13))
            };

            Expander expander = new Expander
            {
                IsExpanded = false,
                Margin = new Thickness(0),
                ToolTip = perk.Id
            };
            visibleExpanders.Add(expander);

            StackPanel header = new StackPanel { Margin = new Thickness(4, 4, 8, 4), Width = 310 }; // Было 270
            header.Children.Add(new TextBlock
            {
                Text = perk.DisplayName,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13
            });
            header.Children.Add(new TextBlock
            {
                Text = perk.Id,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC9, 0xB7, 0xB7)),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
            expander.Header = header;

            expander.Content = CreatePerkEditor(perk);
            card.Child = expander;
            return card;
        }

        private UIElement CreatePerkEditor(PerkEditorItem perk)
        {
            Grid grid = new Grid { Margin = new Thickness(12, 6, 10, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Авто ширина для меток
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // Фикс ширина 160 для полей

            int row = 0;
            foreach (JProperty prop in perk.Object.Properties().OrderBy(p => GetPropertyOrder(p.Name)).ThenBy(p => p.Name))
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Label label = new Label
                {
                    Content = GetPropertyDisplayName(prop.Name),
                    ToolTip = prop.Name,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 3, 12, 3), // Отступ справа 12
                    Padding = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontSize = 12
                };
                Grid.SetRow(label, row);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                UIElement editor = CreateEditor(prop, perk);
                Grid.SetRow(editor, row);
                Grid.SetColumn(editor, 1);
                grid.Children.Add(editor);
                row++;
            }

            return grid;
        }

        private UIElement CreateEditor(JProperty prop, PerkEditorItem perk)
        {
            JToken token = prop.Value;

            if (token.Type == JTokenType.Boolean)
            {
                CheckBox check = new CheckBox
                {
                    IsChecked = token.Value<bool>(),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                check.Checked += delegate { prop.Value = true; };
                check.Unchecked += delegate { prop.Value = false; };
                return check;
            }

            if (prop.Name == "department")
            {
                ComboBox combo = new ComboBox
                {
                    Width = 150,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    IsEditable = true,
                    Text = token.Type == JTokenType.Null ? "" : token.ToString(),
                    ItemsSource = new string[] { "NONE", "HR", "FINANCE", "LAW", "PR", "COMFORT", "SECURITY", "PRODUCTION", "PRODUCING", "POSTPRODUCTION", "TECHNOLOGY" },
                    Margin = new Thickness(0, 3, 0, 3)
                };
                combo.SelectionChanged += delegate
                {
                    prop.Value = combo.Text;
                    perk.Department = combo.Text;
                };
                combo.LostFocus += delegate
                {
                    prop.Value = combo.Text;
                    perk.Department = combo.Text;
                };
                return combo;
            }

            if (prop.Name == "id")
            {
                TextBox idBox = MakeTextBox(token, prop);
                idBox.IsReadOnly = true;
                idBox.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0x2E, 0x2E));
                return idBox;
            }

            TextBox box = MakeTextBox(token, prop);
            box.LostFocus += delegate { ApplyTextBoxValue(box, prop, perk); };
            return box;
        }

        private TextBox MakeTextBox(JToken token, JProperty prop)
        {
            bool complex = token.Type == JTokenType.Array || token.Type == JTokenType.Object;
            return new TextBox
            {
                Text = TokenToEditorText(token),
                Width = 150, // Фиксированная ширина
                HorizontalAlignment = HorizontalAlignment.Left,
                AcceptsReturn = complex,
                TextWrapping = complex ? TextWrapping.Wrap : TextWrapping.NoWrap,
                MinHeight = complex ? 58 : 22,
                VerticalScrollBarVisibility = complex ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
                Tag = prop,
                Margin = new Thickness(0, 3, 0, 3),
                FontSize = 11,
                Padding = new Thickness(4, 2, 4, 2)
            };
        }

        private string TokenToEditorText(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token.Type == JTokenType.Array || token.Type == JTokenType.Object)
                return token.ToString(Formatting.None);
            return token.ToString();
        }

        private void ApplyTextBoxValue(TextBox box, JProperty prop, PerkEditorItem item)
        {
            try
            {
                string text = box.Text;
                JToken old = prop.Value;

                if (old.Type == JTokenType.Array || old.Type == JTokenType.Object)
                {
                    if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                        prop.Value = JValue.CreateNull();
                    else
                        prop.Value = JToken.Parse(text);
                    return;
                }

                if (old.Type == JTokenType.Integer)
                {
                    int v;
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) prop.Value = v;
                    return;
                }

                if (old.Type == JTokenType.Float)
                {
                    double v;
                    if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) prop.Value = v;
                    else prop.Value = text;
                    return;
                }

                if (string.IsNullOrWhiteSpace(text) && (prop.Name == "unlockedByPerks" || prop.Name == "dependsOnBuildings"))
                    prop.Value = JValue.CreateNull();
                else
                    prop.Value = text;

                if (prop.Name == "domain") item.Domain = text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("Invalid JSON/value in field ", "Некорректный JSON/значение в поле ") + prop.Name + ":\n" + ex.Message,
                    L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
                box.Text = TokenToEditorText(prop.Value);
            }
        }

        private string GetPerkDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return id;

            string localizedName;
            if (localizedPerkNames.TryGetValue(id, out localizedName) && !string.IsNullOrWhiteSpace(localizedName))
                return localizedName;

            Dictionary<string, string> known = GetKnownPerkNames();
            string knownName;
            if (known.TryGetValue(id, out knownName)) return knownName;

            return HumanizeIdentifier(id);
        }

        private void LoadPerkLocalization(string perksPath)
        {
            localizedPerkNames.Clear();

            try
            {
                string language = IsRussianLocale ? "RUS" : "ENG";
                List<string> roots = GetLocalizationSearchRoots(perksPath);

                foreach (string root in roots)
                {
                    string file = FindLocalizationFile(root, "NON_EVENT.json", language);
                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        if (TryLoadPerkLocalizationFile(file, language))
                            return;
                    }
                }
            }
            catch
            {
                // При отсутствии файлов локализации по-прежнему доступны резервные имена, указанные ниже.
            }
        }

        private List<string> GetLocalizationSearchRoots(string perksPath)
        {
            List<string> roots = new List<string>();

            try
            {
                if (!string.IsNullOrWhiteSpace(perksPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(perksPath));
                    for (int i = 0; i < 6 && dir != null; i++)
                    {
                        if (dir.Exists) roots.Add(dir.FullName);
                        dir = dir.Parent;
                    }
                }

                string appBase = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(appBase) && Directory.Exists(appBase))
                    roots.Add(appBase);
            }
            catch { }

            return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private string FindLocalizationFile(string root, string fileName, string language)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return null;

                string direct = Path.Combine(root, fileName);
                if (File.Exists(direct) && IsLocalizationLanguage(direct, language))
                    return direct;

                string[] files = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    if (IsLocalizationLanguage(file, language))
                        return file;
                }
            }
            catch { }

            return null;
        }

        private bool IsLocalizationLanguage(string file, string language)
        {
            try
            {
                string json = File.ReadAllText(file);
                JObject root = JObject.Parse(json);
                string package = root["packageID"] != null ? root["packageID"].ToString() : "";
                string lang = root["languageID"] != null ? root["languageID"].ToString() : "";

                return string.Equals(package, "NON_EVENT", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(lang, language, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool TryLoadPerkLocalizationFile(string file, string language)
        {
            string json = File.ReadAllText(file);
            JObject root = JObject.Parse(json);

            JObject idMap = root["IdMap"] as JObject;
            JArray locStrings = root["locStrings"] as JArray;
            if (idMap == null || locStrings == null) return false;

            foreach (string id in GetPerkIdsFromConfig())
            {
                JToken indexToken = idMap[id];
                if (indexToken == null || indexToken.Type == JTokenType.Null) continue;

                int index;
                if (!int.TryParse(indexToken.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) continue;
                if (index < 0 || index >= locStrings.Count) continue;

                string value = locStrings[index] != null ? locStrings[index].ToString() : "";
                value = CleanLocalizationText(value);

                if (!string.IsNullOrWhiteSpace(value))
                    localizedPerkNames[id] = value;
            }

            return localizedPerkNames.Count > 0;
        }


        private IEnumerable<string> GetPerkIdsFromConfig()
        {
            if (configData == null) yield break;

            foreach (JProperty prop in configData.Properties())
            {
                JObject obj = prop.Value as JObject;
                string id = null;
                if (obj != null && obj["id"] != null && obj["id"].Type != JTokenType.Null)
                    id = obj["id"].ToString();

                if (string.IsNullOrWhiteSpace(id))
                    id = prop.Name;

                yield return id;
            }
        }

        private string CleanLocalizationText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            text = text.Replace("<nobr>", "").Replace("</nobr>", "");
            text = Regex.Replace(text, "<.*?>", "");
            text = text.Replace("\\n", " ").Replace("\n", " ").Replace("\r", " ");
            text = Regex.Replace(text, "\\s+", " ").Trim();

            return text;
        }

        // Внутренний словарь ..04.05.2026 (0.8.70EA)

        private Dictionary<string, string> GetKnownPerkNames()
        {
            if (IsRussianLocale)
            {
                return new Dictionary<string, string>
                {
                    { "BLDG_ESCORT_DOMINION", "Постройка: эскорт-доминион" },
                    { "ETHNIC_COMPOSITION", "Этнический состав" },
                    { "ILLEGAL_WORKERS", "Нелегальные работники" },
                    { "CHEAP_ILLEGALS", "Снижение оплаты нелегальных работников" },
                    { "STAFF_LARGE1", "Расширение штата I" },
                    { "STAFF_LARGE2", "Расширение штата II" },
                    { "BUILDINGS_CONSERVATION", "Консервация построек" },
                    { "CONSERVATION_COOLDOWN", "Сокращение задержки консервации" },
                    { "SALARY_CUT", "Сокращение зарплат" },
                    { "IMPROVEMENT_0_NO_SADNESS", "Улучшение без ухудшения настроения" },
                    { "HIRING_BONUSES", "Бонусы при найме" },
                    { "NOMINATION_LOSS_NO_SADNESS", "Проигрыш номинации без ухудшения настроения" },
                    { "MOVIE_RELEASE_MOOD_BOOST", "Бонус настроения после релиза фильма" },
                    { "BAD_ATTITUDE_NO_SADNESS", "Плохое отношение без ухудшения настроения" },
                    { "BANK_LOAN", "Банковский кредит" },
                    { "BANK_LOAN_EARLY_REPAYMENT", "Досрочное погашение кредита" },
                    { "BANK_LOAN_INT_RATE_REDUCTION_1", "Снижение ставки по кредиту I" },
                    { "BANK_LOAN_INT_RATE_REDUCTION_2", "Снижение ставки по кредиту II" },
                    { "BANK_LOAN_AMOUNT_1", "Увеличение суммы кредита I" },
                    { "BANK_LOAN_AMOUNT_2", "Увеличение суммы кредита II" },
                    { "BANK_LOAN_TERM_1", "Увеличение срока кредита I" },
                    { "BANK_LOAN_TERM_2", "Увеличение срока кредита II" },
                    { "BANK_LOAN_REFINANCING", "Рефинансирование кредита" },
                    { "BANK_LOAN_MICROLOAN", "Микрокредит" },
                    { "BANK_LOAN_COOLDOWN_REDUCTION", "Сокращение задержки кредита" },
                    { "CASH_FLOW_1", "Денежный поток I" },
                    { "CASH_FLOW_2", "Денежный поток II" },
                    { "QUARTERLY_REPORT_CASH_1", "Квартальный денежный отчёт I" },
                    { "QUARTERLY_REPORT_CASH_2", "Квартальный денежный отчёт II" },
                    { "QUARTERLY_REPORT_CASH_3", "Квартальный денежный отчёт III" },
                    { "TAX_BASE_REDUCTION_1", "Снижение налоговой базы I" },
                    { "TAX_BASE_REDUCTION_2", "Снижение налоговой базы II" },
                    { "TAX_BASE_REDUCTION_3", "Снижение налоговой базы III" },
                    { "LEGAL_DEFENSE_1", "Юридическая защита I" },
                    { "LEGAL_DEFENSE_2", "Юридическая защита II" },
                    { "LEGAL_DEFENSE_3", "Юридическая защита III" },
                    { "CONTRACT_TERMINATION_FEE_1", "Штраф за расторжение контракта I" },
                    { "CONTRACT_TERMINATION_FEE_2", "Штраф за расторжение контракта II" },
                    { "CONTRACT_PAYMENTS_50_50", "Оплата контракта 50/50" },
                    { "CONTRACT_GROSS", "Процент от сборов" },
                    { "CONTRACT_5_YEARS", "Контракты на 5 лет" },
                    { "CONTRACT_10_YEARS", "Контракты на 10 лет" },
                    { "CONTRACT_5_MOVIES", "Контракты на 5 фильмов" },
                    { "CONTRACT_10_MOVIES", "Контракты на 10 фильмов" },
                    { "CHARITY_TO_REP", "Благотворительность в репутацию" },
                    { "PROFITABLE_MOVIE_REP_2", "Репутация за прибыльный фильм" },
                    { "GENERATION_IP_AND_REP", "Генерация влияния и репутации" },
                    { "GENERATION_IP_X2", "Удвоение влияния" },
                    { "GENERATION_REP_X2", "Удвоение репутации" },
                    { "GOOD_ATTITUDE_REP_1", "Репутация за хорошее отношение I" },
                    { "GOOD_ATTITUDE_REP_2", "Репутация за хорошее отношение II" },
                    { "ICON_REP_1", "Репутация за статус иконы I" },
                    { "LEGEND_REP_1", "Репутация за статус легенды I" },
                    { "SKILLED_ACTOR_REP", "Репутация за опытного актёра" },
                    { "PREMIERE_REP_1", "Репутация за премьеру I" },
                    { "SUPER_PREMIERE_REP_1", "Репутация за суперпремьеру I" },
                    { "SUPER_PREMIERE_PP_1", "Влияние за суперпремьеру I" },
                    { "MOVIE_PALACE_PP_1", "Влияние за Дворец кино I" },
                    { "TOP1_TOP3", "Топ-1 и топ-3" },
                    { "TECH_SALE_PP", "Влияние за продажу технологий" },
                    { "INITIATIVE_PP_FREE", "Бесплатное влияние за инициативу" },
                    { "EDITS_ON_GO", "Правки на ходу" },
                    { "SCEN_IDEAS_STORAGE_1", "Хранилище идей" },
                    { "SCEN_IDEAS_GEN_AMT_1", "Больше идей I" },
                    { "SCEN_IDEAS_GEN_AMT_2", "Больше идей II" },
                    { "BLDG_CONSTRUCTOR", "Сценарный конструктор" },
                    { "TAGS_RESEARCH", "Исследование новых тегов" },
                    { "TAGS_RESEARCH_DIRECTION", "Исследование тегов по категориям" },
                    { "TAGS_SLOTS_6", "6 тегов наполнения в синопсисе" },
                    { "TAGS_SLOTS_7", "7 тегов наполнения в синопсисе" },
                    { "TAGS_SLOTS_8", "8 тегов наполнения в синопсисе" },
                    { "TAGS_SLOTS_9", "9 тегов наполнения в синопсисе" },
                    { "TAGS_SLOTS_10", "10 тегов наполнения в синопсисе" },
                    { "NEW_TAG_BY_LT_1", "Исследование тегов лейтенантом I" },
                    { "NEW_TAG_BY_LT_2", "Исследование тегов лейтенантом II" },
                    { "TAGS_RESEARCH_TIME_RED_1", "Сокращение времени исследования тегов I" },
                    { "TAGS_RESEARCH_TIME_RED_2", "Сокращение времени исследования тегов II" },
                    { "TAGS_RESEARCH_TIME_RED_3", "Сокращение времени исследования тегов III" },
                    { "TAGS_NEW_PP_BONUS", "Влияние за новые теги" },
                    { "TAGS_XP_BONUS_1", "Бонус опыта за теги I" },
                    { "TAGS_XP_BONUS_2", "Бонус опыта за теги II" },
                    { "TAGS_XP_BONUS_3", "Бонус опыта за теги III" },
                    { "BLDG_FREELANCE", "Постройка: фриланс" },
                    { "SCREENPLAYS_AMT_1", "Screenplays количество I" },
                    { "SCREENPLAYS_AMT_2", "Screenplays количество II" },
                    { "SCRIPT_DOCTORS", "Сценарные доктора" },
                    { "SCRIPT_DOCTORS_FASTER", "Ускорение работы сценарных докторов" },
                    { "SCRIPT_DOCTORS_CHEAPER", "Снижение стоимости сценарных докторов" },
                    { "SCRIPT_DOCTORS_RANGE", "Расширенный выбор сценарных докторов" },
                    { "SCRIPT_DOCTORS_SCORES", "Оценки сценарных докторов" },
                    { "MOVIE_RELEASE_XP_1", "Опыт за релиз фильма I" },
                    { "MOVIE_RELEASE_XP_2", "Опыт за релиз фильма II" },
                    { "MOVIE_RELEASE_XP_3", "Опыт за релиз фильма III" },
                    { "MOVIE_RELEASE_MOOD_1", "Фильм релиз настроение I" },
                    { "MOVIE_RELEASE_ATTITUDE_1", "Фильм релиз отношение I" },
                    { "MOVIE_RELEASE_TOP10_COM_XP_1", "Опыт за топ-10: коммерческий успех I" },
                    { "MOVIE_RELEASE_TOP10_ART_XP_1", "Опыт за топ-10: художественный успех I" },
                    { "MOVIE_RELEASE_TOP10_AUD_XP_1", "Опыт за топ-10: зрительский успех I" },
                    { "MOVIE_SEQUEL", "Сиквелы" },
                    { "MOVIE_SEQUEL_LEGACY", "Достойный преемник" },
                    { "MOVIE_SEQUEL_ORIGINALITY", "Свежий взгляд" },
                    { "BLDG_COPYRIGHT", "Постройка: авторские права" },
                    { "PRINT_MEDIA", "Печатные произведения" },
                    { "BROADCAST_MEDIA", "Радио и телевидение" },
                    { "PUBLIC_DOMAIN", "Общественное достояние" },
                    { "LITERARY_WORK_RESEARCH_TIME_1", "Сокращение времени исследования литературных произведений I" },
                    { "SCREENPLAY_TIME_RED_1", "Сокращение времени написания сценария I" },
                    { "SCREENPLAY_TIME_RED_2", "Сокращение времени написания сценария II" },
                    { "SCREENPLAY_TIME_RED_3", "Сокращение времени написания сценария III" },
                    { "NEW_SCREENPLAY_PP_BONUS_1", "Бонус влияния за новый сценарий I" },
                    { "NEW_SCREENPLAY_PP_BONUS_2", "Бонус влияния за новый сценарий II" },
                    { "NEW_SCREENPLAY_XP_BONUS_1", "Бонус опыта за новый сценарий I" },
                    { "NEW_SCREENPLAY_XP_BONUS_2", "Бонус опыта за новый сценарий II" },
                    { "NEW_SCREENPLAY_XP_BONUS_3", "Бонус опыта за новый сценарий III" },
                    { "BLDG_SUPPLY", "Постройка: отдел снабжения" },
                    { "BLDG_CASTING", "Постройка: кастинг" },
                    { "PREPROD_PROD_DIR_CIN_XP_1", "Опыт продюсеров, режиссёров и операторов на подготовке I" },
                    { "PREPROD_PROD_DIR_CIN_XP_2", "Опыт продюсеров, режиссёров и операторов на подготовке II" },
                    { "EXTRAS_2", "Массовка II" },
                    { "EXTRAS_3", "Массовка III" },
                    { "EXTRAS_4", "Массовка IV" },
                    { "ADDITIONAL_REHEARSAL_1", "Дополнительная репетиция I" },
                    { "ADDITIONAL_REHEARSAL_2", "Дополнительная репетиция II" },
                    { "BLDG_SCOUT", "Постройка: поиск локаций" },
                    { "LOCATION_SEARCH_TIME_1", "Локации поиск время I" },
                    { "LOCATION_SEARCH_TIME_2", "Локации поиск время II" },
                    { "LOCATION_SEARCH_WORLD", "Мировой поиск локаций" },
                    { "LOCATION_QLT_1", "Качество локаций I" },
                    { "LOCATION_QLT_2", "Качество локаций II" },
                    { "BLDG_WORKSHOP", "Постройка: мастерская" },
                    { "SETS_QLT_2", "Качество декораций II" },
                    { "SETS_QLT_3", "Качество декораций III" },
                    { "PROPS_QLT_2", "Качество реквизита II" },
                    { "PROPS_QLT_3", "Качество реквизита III" },
                    { "SETS_TIME_RED_1", "Сокращение времени создания декораций I" },
                    { "SETS_TIME_RED_2", "Сокращение времени создания декораций II" },
                    { "SETS_TIME_RED_3", "Сокращение времени создания декораций III" },
                    { "PROD_DIR_CIN_ACT_XP_1", "Опыт режиссёров, операторов и актёров на съёмках I" },
                    { "BLDG_LINE_PRODUCTION", "Постройка: линейное производство" },
                    { "SECOND_UNIT", "Вторая съёмочная группа" },
                    { "URGENT_DOUBLE_SEARCH", "Срочный поиск дублёра" },
                    { "URGENT_EXTRAS_SEARCH", "Срочный поиск массовки" },
                    { "URGENT_CREW_SEARCH", "Срочный поиск съёмочной группы" },
                    { "URGENT_LOCATION_SEARCH", "Срочный поиск локации" },
                    { "FLEX_SCHEDULE", "Гибкий график" },
                    { "BLDG_LOGISTICS", "Постройка: логистика" },
                    { "TEAM_SERVICE_1", "Обслуживание команды I" },
                    { "TEAM_SERVICE_2", "Обслуживание команды II" },
                    { "BLDG_PAVILION_II", "Постройка: павильон II" },
                    { "BLDG_PAVILION_III", "Постройка: павильон II" },
                    { "BLDG_PAVILION_IV", "Постройка: павильон IV" },
                    { "PAVILION_RENT_1", "Аренда павильона I" },
                    { "PAVILION_RENT_2", "Аренда павильона II" },
                    { "BLDG_SOUND", "Постройка: звукозапись" },
                    { "SOUND_INHOUSE_IMPROVED", "Улучшенная внутренняя звукозапись" },
                    { "SOUND_INHOUSE_TIME_1", "Сокращение времени звукозаписи I" },
                    { "POST_DIR_MONT_COMP_XP_1", "Пост режиссёр монтаж композитор опыт I" },
                    { "BLDG_CONCERT", "Постройка: концертный зал" },
                    { "CONCERT_INHOUSE_MPROVED", "Улучшенный внутренний оркестр" },
                    { "CONCERT_INHOUSE_TIME_1", "Сокращение времени записи музыки I" },
                    { "FOCUS_OUTSOURCE", "Аутсорс фокус-групп" },
                    { "BLDG_FOCUS", "Постройка: фокус-группы" },
                    { "FOCUS_QLT_1", "Качество фокус-групп I" },
                    { "FOCUS_QLT_2", "Качество фокус-групп II" },
                    { "BLDG_LAB", "Постройка: лаборатория" },
                    { "LAB_INHOUSE_IMPROVED", "Улучшенная внутренняя лаборатория" },
                    { "LAB_INHOUSE_TIME_1", "Сокращение времени лаборатории I" },
                    { "BLDG_DISTRIBUTION", "Постройка: дистрибуция" },
                    { "MOVIE_THEATRE_SLOT_ADD_1", "Дополнительный слот кинотеатра I" },
                    { "MOVIE_THEATRE_SLOT_RENT", "Аренда слота кинотеатра" },
                    { "MOVIE_PALACE", "Дворец кино" },
                    { "MARKET_INTERVIEW", "Маркетинговый опрос" },
                    { "BLDG_ANALYTICS", "Постройка: аналитика" },
                    { "ANALYSIS_GROUPS", "Анализ групп" },
                    { "POSTRELEASE_ANALYSIS", "Послерелизный анализ" },
                    { "ANALYSIS_ENTIRE_CAST", "Анализ всего актёрского состава" },
                    { "ANALYSIS_TAGS", "Анализ тегов" },
                    { "ANALYSIS_BUDGET", "Анализ бюджета" },
                    { "ANALYSIS_SCREENPLAY", "Анализ сценария" },
                    { "BLDG_PRINT", "Постройка: печать" },
                    { "PRINT_INHOUSE_QLT_1", "Качество внутренней печати I" },
                    { "PRINT_INHOUSE_QLT_2", "Качество внутренней печати II" },
                    { "PRINT_EMERGENCY", "Экстренная печать" },
                    { "BLDG_MARKETING", "Постройка: маркетинг" },
                    { "PREMIERE", "Премьера" },
                    { "SUPER_PREMIERE", "Суперпремьера" },
                    { "WM_HOSPICE", "Благотворительность: хоспис" },
                    { "WM_ORPHANAGE", "Благотворительность: приют" },
                    { "WM_WEDDING", "Благотворительность: свадьба" },
                    { "WM_HOMELESS", "Благотворительность: бездомные" },
                    { "WM_DEBT", "Благотворительность: долги" },
                    { "BM_UNLOCK", "Открыть чёрный маркетинг" },
                    { "BM_DROWNING", "Чёрный маркетинг: утопление" },
                    { "BM_DRUNKARD", "Чёрный маркетинг: пьянство" },
                    { "BM_FIGHT", "Чёрный маркетинг: драка" },
                    { "BM_CRIMINAL", "Чёрный маркетинг: криминал" },
                    { "BM_HOUSE_BURN", "Чёрный маркетинг: поджог дома" },
                    { "SCANDAL_COVER_UP_MONEY", "Замять скандал деньгами" },
                    { "SCANDAL_COVER_UP_PP", "Замять скандал влиянием" },
                    { "BLDG_SHENANIGANS", "Постройка: спецоперации" },
                    { "SHENANIGANS_BEATING", "Спецоперация: избиение" },
                    { "SHENANIGANS_KIDNAPPING", "Спецоперация: похищение" },
                    { "SHENANIGANS_MURDER", "Спецоперация: убийство" },
                    { "LEAK_RISK_REDUCE_1", "Снижение риска утечки I" },
                    { "SPYING_SINS", "Шпионаж: пороки" },
                    { "SPYING_ILLEGALPREFERENCES", "Шпионаж: незаконные предпочтения" },
                    { "SPYING_XP_BONUS_1", "Бонус опыта шпионажа I" },
                    { "SPYING_XP_BONUS_2", "Бонус опыта шпионажа II" },
                    { "FAIL_NO_DISCLOSURE", "Провал без раскрытия" },
                    { "SECURITY_SCHOOL", "Школа безопасности" },
                    { "SECURITY_SCHOOL_FAST", "Ускоренная школа безопасности" },
                    { "SECURITY_SCHOOL_STRONG", "Усиленная школа безопасности" },
                    { "BLDG_SPIES", "Постройка: шпионы" },
                    { "ACTIVE_PROTECTION", "Активная защита" },
                    { "ACTIVE_PROTECTION_XP_BONUS_1", "Бонус опыта активной защиты I" },
                    { "ACTIVE_PROTECTION_XP_BONUS_2", "Бонус опыта активной защиты II" },
                    { "SECRETS_HIDE_EFFECT_BOOST", "Усиление сокрытия секретов" },
                    { "FAIL_DISCLOSURE_NO_LEAK", "Раскрытие без утечки" },
                    { "WG_WATCHES", "Подарок: часы" },
                    { "WG_CIGARS", "Подарок: сигары" },
                    { "WG_ALCOHOL", "Подарок: алкоголь" },
                    { "WG_HAUTE_WARDROBE", "Подарок: дорогой гардероб" },
                    { "WG_SPORTCAR", "Подарок: спорткар" },
                    { "BG_UNLOCK", "Открыть массовку" },
                    { "BG_NARCOTICS", "Массовка: наркотики" },
                    { "BG_METH", "Массовка: метамфетамин" },
                    { "BG_NARCOTICS_2", "Массовка: наркотики II" },
                    { "BG_SAFARI", "Массовка: сафари" },
                    { "BG_XXX", "Массовка: для взрослых" },
                    { "BG_BRAINS", "Умная массовка" },
                    { "BG_KILLING", "Массовка: убийства" },
                    { "BG_CANNIBAL", "Массовка: каннибализм" },
                    { "BG_UNDERAGE", "Массовка: несовершеннолетние" },
                    { "BLDG_EVENTS_STAGE", "Постройка: сцена мероприятий" },
                    { "OFFICIAL_RECEPTION_1", "Официальный приём I" },
                    { "OFFICIAL_RECEPTION_2", "Официальный приём II" },
                    { "OFFICIAL_RECEPTION_3", "Официальный приём III" },
                    { "PARTY_1", "Корпоратив I" },
                    { "PARTY_2", "Корпоратив II" },
                    { "PARTY_3", "Корпоратив III" },
                    { "INSURANCE_PLUS", "Расширенная медстраховка" },
                    { "HOUSEMAID", "Горничная" },
                    { "NANNY", "Няня" },
                    { "ASSISTANT", "Ассистент" },
                    { "SPOUSES_ASSISTANT", "Ассистент для супругов" },
                    { "CHEF", "Шеф-повар" },
                    { "BUTLER", "Дворецкий" },
                    { "HOTEL_SUITE", "Номер в отеле" },
                    { "VILLA", "Вилла" },
                    { "PENTHOUSE", "Пентхаус" },
                    { "PERSONAL_DRIVER", "Автомобиль с водителем" },
                    { "PERSONAL_DRIVER_PREMIUM", "Роскошный автомобиль с водителем" },
                    { "TWO_PROJECTS", "Два проекта" },
                    { "PRODUCERS_ON_FILM_2", "Два продюсера на проекте" },
                    { "PRODUCERS_ON_FILM_3", "Три продюсера на проекте" },
                    { "NEGOTIATION_SCALE_50", "Шкала переговоров 50" },
                    { "NEGOTIATION_SCALE_75", "Шкала переговоров 75" },
                    { "CONTRACT_WEIGHT", "Вес контракта" },
                    { "FOCUS_INHOUSE_RED_TIME_1", "Сокращение времени фокус-групп I" },
                    { "FOCUS_INHOUSE_RED_PRICE_1", "Снижение стоимости фокус-групп I" },
                    { "IP_HYPE", "Ажиотаж вокруг прав" },
                    { "IP_HYPE_RED_TIME_1", "Сокращение времени ажиотажа I" },
                    { "IP_HYPE_RED_PRICE_1", "Снижение стоимости ажиотажа I" },
                    { "IP_KEEPER", "Хранитель прав" },
                    { "IP_CONTRACT_WEIGHT", "Вес контракта на права" },
                    { "IP_TALANTS_LNT_BONUS_XP", "Бонус опыта лейтенантов по правам" },
                    { "START_PROD_NO_ACT", "Старт производства без актёров" },
                    { "IP_MOVIE_THEATRE_CHEAP", "Снижение стоимости кинотеатров за права" },
                    { "REPAIR_TEAM_1", "Ремонтная бригада I" },
                    { "BLDG_WATER_TOWER_I", "Постройка: водонапорная башня I" },
                    { "BLDG_WATER_TOWER_II", "Постройка: водонапорная башня II" },
                    { "BLDG_WATER_TOWER_III", "Постройка: водонапорная башня III" },
                    { "WATER_TOWER_AMT_3", "Количество водонапорных башен III" },
                    { "BLDG_POWERPLANT_I", "Постройка: электростанция I" },
                    { "BLDG_POWERPLANT_II", "Постройка: электростанция II" },
                    { "BLDG_POWERPLANT_III", "Постройка: электростанция III" },
                    { "POWERPLANT_AMT_3", "Количество электростанций III" },
                    { "IMPROVEMENT_I", "Улучшение I" },
                    { "IMPROVEMENT_II", "Улучшение II" },
                    { "IMPROVEMENT_III", "Улучшение III" },
                    { "STUDIO_TECH", "Технологии студии" },
                    { "STUDIO_TECH_ADD_RND", "Дополнительная исследовательская группа" },
                    { "STUDIO_TECH_RED_TIME_1", "Сокращение времени технологий I" },
                    { "STUDIO_TECH_RED_TIME_2", "Сокращение времени технологий II" },
                    { "BLDG_RND_I", "Исследовательская группа I" },
                    { "BLDG_RND_II", "Исследовательская группа II" },
                    { "BLDG_RND_III", "Исследовательская группа III" },
                    { "BLDG_RND_IV", "Исследовательская группа IV" },
                };
            }

            return new Dictionary<string, string>
            {
                    { "BLDG_ESCORT_DOMINION", "Building: Escort Dominion" },
                    { "ETHNIC_COMPOSITION", "Ethnic composition" },
                    { "ILLEGAL_WORKERS", "Illegal workers" },
                    { "CHEAP_ILLEGALS", "Lower illegal worker pay" },
                    { "STAFF_LARGE1", "Staff expansion I" },
                    { "STAFF_LARGE2", "Staff expansion II" },
                    { "BUILDINGS_CONSERVATION", "Building conservation" },
                    { "CONSERVATION_COOLDOWN", "Shorter conservation cooldown" },
                    { "SALARY_CUT", "Salary cuts" },
                    { "IMPROVEMENT_0_NO_SADNESS", "Improvement without mood loss" },
                    { "HIRING_BONUSES", "Hiring bonuses" },
                    { "NOMINATION_LOSS_NO_SADNESS", "Nomination loss without mood penalty" },
                    { "MOVIE_RELEASE_MOOD_BOOST", "Movie release mood boost" },
                    { "BAD_ATTITUDE_NO_SADNESS", "Bad attitude without mood penalty" },
                    { "BANK_LOAN", "Bank loan" },
                    { "BANK_LOAN_EARLY_REPAYMENT", "Early loan repayment" },
                    { "BANK_LOAN_INT_RATE_REDUCTION_1", "Loan interest rate reduction I" },
                    { "BANK_LOAN_INT_RATE_REDUCTION_2", "Loan interest rate reduction II" },
                    { "BANK_LOAN_AMOUNT_1", "Loan amount increase I" },
                    { "BANK_LOAN_AMOUNT_2", "Loan amount increase II" },
                    { "BANK_LOAN_TERM_1", "Loan term increase I" },
                    { "BANK_LOAN_TERM_2", "Loan term increase II" },
                    { "BANK_LOAN_REFINANCING", "Loan refinancing" },
                    { "BANK_LOAN_MICROLOAN", "Microloan" },
                    { "BANK_LOAN_COOLDOWN_REDUCTION", "Shorter loan cooldown" },
                    { "CASH_FLOW_1", "Cash Flow 1" },
                    { "CASH_FLOW_2", "Cash Flow 2" },
                    { "QUARTERLY_REPORT_CASH_1", "Quarterly Report Cash 1" },
                    { "QUARTERLY_REPORT_CASH_2", "Quarterly Report Cash 2" },
                    { "QUARTERLY_REPORT_CASH_3", "Quarterly Report Cash 3" },
                    { "TAX_BASE_REDUCTION_1", "Tax base reduction I" },
                    { "TAX_BASE_REDUCTION_2", "Tax base reduction II" },
                    { "TAX_BASE_REDUCTION_3", "Tax base reduction III" },
                    { "LEGAL_DEFENSE_1", "Legal defense I" },
                    { "LEGAL_DEFENSE_2", "Legal defense II" },
                    { "LEGAL_DEFENSE_3", "Legal defense III" },
                    { "CONTRACT_TERMINATION_FEE_1", "Contract termination fee I" },
                    { "CONTRACT_TERMINATION_FEE_2", "Contract termination fee II" },
                    { "CONTRACT_PAYMENTS_50_50", "50/50 contract payments" },
                    { "CONTRACT_GROSS", "Gross revenue share" },
                    { "CONTRACT_5_YEARS", "5-year contracts" },
                    { "CONTRACT_10_YEARS", "10-year contracts" },
                    { "CONTRACT_5_MOVIES", "5-movie contracts" },
                    { "CONTRACT_10_MOVIES", "10-movie contracts" },
                    { "CHARITY_TO_REP", "Convert charity into reputation" },
                    { "PROFITABLE_MOVIE_REP_2", "Reputation for profitable movies" },
                    { "GENERATION_IP_AND_REP", "Influence and reputation generation" },
                    { "GENERATION_IP_X2", "Double influence generation" },
                    { "GENERATION_REP_X2", "Double reputation generation" },
                    { "GOOD_ATTITUDE_REP_1", "Reputation for good attitude I" },
                    { "GOOD_ATTITUDE_REP_2", "Reputation for good attitude II" },
                    { "ICON_REP_1", "Icon Rep 1" },
                    { "LEGEND_REP_1", "Legend Rep 1" },
                    { "SKILLED_ACTOR_REP", "Skilled Actor Rep" },
                    { "PREMIERE_REP_1", "Premiere Rep 1" },
                    { "SUPER_PREMIERE_REP_1", "Super Premiere Rep 1" },
                    { "SUPER_PREMIERE_PP_1", "Super Premiere Influence 1" },
                    { "MOVIE_PALACE_PP_1", "Movie Palace Influence 1" },
                    { "TOP1_TOP3", "Top1 Top3" },
                    { "TECH_SALE_PP", "Tech Sale Influence" },
                    { "INITIATIVE_PP_FREE", "Initiative Influence Free" },
                    { "EDITS_ON_GO", "Edits On Go" },
                    { "SCEN_IDEAS_STORAGE_1", "Scen Ideas Storage 1" },
                    { "SCEN_IDEAS_GEN_AMT_1", "Scen Ideas Gen Amount 1" },
                    { "SCEN_IDEAS_GEN_AMT_2", "Scen Ideas Gen Amount 2" },
                    { "BLDG_CONSTRUCTOR", "Script constructor" },
                    { "TAGS_RESEARCH", "New tag research" },
                    { "TAGS_RESEARCH_DIRECTION", "Tag category research" },
                    { "TAGS_SLOTS_6", "6 synopsis content tags" },
                    { "TAGS_SLOTS_7", "7 synopsis content tags" },
                    { "TAGS_SLOTS_8", "8 synopsis content tags" },
                    { "TAGS_SLOTS_9", "9 synopsis content tags" },
                    { "TAGS_SLOTS_10", "10 synopsis content tags" },
                    { "NEW_TAG_BY_LT_1", "Lieutenant tag research I" },
                    { "NEW_TAG_BY_LT_2", "Lieutenant tag research II" },
                    { "TAGS_RESEARCH_TIME_RED_1", "Shorter tag research time I" },
                    { "TAGS_RESEARCH_TIME_RED_2", "Shorter tag research time II" },
                    { "TAGS_RESEARCH_TIME_RED_3", "Shorter tag research time III" },
                    { "TAGS_NEW_PP_BONUS", "Tags New Influence Bonus" },
                    { "TAGS_XP_BONUS_1", "Tag XP bonus I" },
                    { "TAGS_XP_BONUS_2", "Tag XP bonus II" },
                    { "TAGS_XP_BONUS_3", "Tag XP bonus III" },
                    { "BLDG_FREELANCE", "Building: freelance office" },
                    { "SCREENPLAYS_AMT_1", "Screenplays Amount 1" },
                    { "SCREENPLAYS_AMT_2", "Screenplays Amount 2" },
                    { "SCRIPT_DOCTORS", "Script Doctors" },
                    { "SCRIPT_DOCTORS_FASTER", "Script Doctors Faster" },
                    { "SCRIPT_DOCTORS_CHEAPER", "Script Doctors Cheaper" },
                    { "SCRIPT_DOCTORS_RANGE", "Script Doctors Range" },
                    { "SCRIPT_DOCTORS_SCORES", "Script Doctors Scores" },
                    { "MOVIE_RELEASE_XP_1", "Movie release XP I" },
                    { "MOVIE_RELEASE_XP_2", "Movie release XP II" },
                    { "MOVIE_RELEASE_XP_3", "Movie release XP III" },
                    { "MOVIE_RELEASE_MOOD_1", "Movie Release Mood 1" },
                    { "MOVIE_RELEASE_ATTITUDE_1", "Movie Release Attitude 1" },
                    { "MOVIE_RELEASE_TOP10_COM_XP_1", "Top-10 movie release XP: commercial score I" },
                    { "MOVIE_RELEASE_TOP10_ART_XP_1", "Top-10 movie release XP: art score I" },
                    { "MOVIE_RELEASE_TOP10_AUD_XP_1", "Top-10 movie release XP: audience score I" },
                    { "MOVIE_SEQUEL", "Sequels" },
                    { "MOVIE_SEQUEL_LEGACY", "Worthy successor" },
                    { "MOVIE_SEQUEL_ORIGINALITY", "Fresh perspective" },
                    { "BLDG_COPYRIGHT", "Building: Copyright" },
                    { "PRINT_MEDIA", "Print Media" },
                    { "BROADCAST_MEDIA", "Broadcast Media" },
                    { "PUBLIC_DOMAIN", "Public Domain" },
                    { "LITERARY_WORK_RESEARCH_TIME_1", "Literary Work Research Time 1" },
                    { "SCREENPLAY_TIME_RED_1", "Shorter screenplay writing time I" },
                    { "SCREENPLAY_TIME_RED_2", "Shorter screenplay writing time II" },
                    { "SCREENPLAY_TIME_RED_3", "Shorter screenplay writing time III" },
                    { "NEW_SCREENPLAY_PP_BONUS_1", "New screenplay influence bonus I" },
                    { "NEW_SCREENPLAY_PP_BONUS_2", "New screenplay influence bonus II" },
                    { "NEW_SCREENPLAY_XP_BONUS_1", "New screenplay XP bonus I" },
                    { "NEW_SCREENPLAY_XP_BONUS_2", "New screenplay XP bonus II" },
                    { "NEW_SCREENPLAY_XP_BONUS_3", "New screenplay XP bonus III" },
                    { "BLDG_SUPPLY", "Building: Supply" },
                    { "BLDG_CASTING", "Building: Casting" },
                    { "PREPROD_PROD_DIR_CIN_XP_1", "Pre-production producer/director/cinematographer XP I" },
                    { "PREPROD_PROD_DIR_CIN_XP_2", "Pre-production producer/director/cinematographer XP II" },
                    { "EXTRAS_2", "Extras 2" },
                    { "EXTRAS_3", "Extras 3" },
                    { "EXTRAS_4", "Extras 4" },
                    { "ADDITIONAL_REHEARSAL_1", "Additional Rehearsal 1" },
                    { "ADDITIONAL_REHEARSAL_2", "Additional Rehearsal 2" },
                    { "BLDG_SCOUT", "Building: Scout" },
                    { "LOCATION_SEARCH_TIME_1", "Location Search Time 1" },
                    { "LOCATION_SEARCH_TIME_2", "Location Search Time 2" },
                    { "LOCATION_SEARCH_WORLD", "Location Search World" },
                    { "LOCATION_QLT_1", "Location quality I" },
                    { "LOCATION_QLT_2", "Location quality II" },
                    { "BLDG_WORKSHOP", "Building: Workshop" },
                    { "SETS_QLT_2", "Set quality II" },
                    { "SETS_QLT_3", "Set quality III" },
                    { "PROPS_QLT_2", "Prop quality II" },
                    { "PROPS_QLT_3", "Prop quality III" },
                    { "SETS_TIME_RED_1", "Shorter set construction time I" },
                    { "SETS_TIME_RED_2", "Shorter set construction time II" },
                    { "SETS_TIME_RED_3", "Shorter set construction time III" },
                    { "PROD_DIR_CIN_ACT_XP_1", "Production director/cinematographer/actor XP I" },
                    { "BLDG_LINE_PRODUCTION", "Building: Line Production" },
                    { "SECOND_UNIT", "Second Unit" },
                    { "URGENT_DOUBLE_SEARCH", "Urgent Double Search" },
                    { "URGENT_EXTRAS_SEARCH", "Urgent Extras Search" },
                    { "URGENT_CREW_SEARCH", "Urgent Crew Search" },
                    { "URGENT_LOCATION_SEARCH", "Urgent Location Search" },
                    { "FLEX_SCHEDULE", "Flex Schedule" },
                    { "BLDG_LOGISTICS", "Building: Logistics" },
                    { "TEAM_SERVICE_1", "Team Service 1" },
                    { "TEAM_SERVICE_2", "Team Service 2" },
                    { "BLDG_PAVILION_II", "Building: pavilion II" },
                    { "BLDG_PAVILION_III", "Building: pavilion II" },
                    { "BLDG_PAVILION_IV", "Building: pavilion IV" },
                    { "PAVILION_RENT_1", "Pavilion rent I" },
                    { "PAVILION_RENT_2", "Pavilion rent II" },
                    { "BLDG_SOUND", "Building: Sound" },
                    { "SOUND_INHOUSE_IMPROVED", "Sound In-house Improved" },
                    { "SOUND_INHOUSE_TIME_1", "Sound In-house Time 1" },
                    { "POST_DIR_MONT_COMP_XP_1", "Post Dir Mont Comp XP 1" },
                    { "BLDG_CONCERT", "Building: Concert" },
                    { "CONCERT_INHOUSE_MPROVED", "Concert In-house Mproved" },
                    { "CONCERT_INHOUSE_TIME_1", "Concert In-house Time 1" },
                    { "FOCUS_OUTSOURCE", "Focus Outsource" },
                    { "BLDG_FOCUS", "Building: Focus" },
                    { "FOCUS_QLT_1", "Focus Quality 1" },
                    { "FOCUS_QLT_2", "Focus Quality 2" },
                    { "BLDG_LAB", "Building: Lab" },
                    { "LAB_INHOUSE_IMPROVED", "Lab In-house Improved" },
                    { "LAB_INHOUSE_TIME_1", "Lab In-house Time 1" },
                    { "BLDG_DISTRIBUTION", "Building: Distribution" },
                    { "MOVIE_THEATRE_SLOT_ADD_1", "Movie Theatre Slot Add 1" },
                    { "MOVIE_THEATRE_SLOT_RENT", "Movie Theatre Slot Rent" },
                    { "MOVIE_PALACE", "Movie Palace" },
                    { "MARKET_INTERVIEW", "Market Interview" },
                    { "BLDG_ANALYTICS", "Building: Analytics" },
                    { "ANALYSIS_GROUPS", "Analysis Groups" },
                    { "POSTRELEASE_ANALYSIS", "Postrelease Analysis" },
                    { "ANALYSIS_ENTIRE_CAST", "Analysis Entire Cast" },
                    { "ANALYSIS_TAGS", "Analysis Tags" },
                    { "ANALYSIS_BUDGET", "Analysis Budget" },
                    { "ANALYSIS_SCREENPLAY", "Analysis Screenplay" },
                    { "BLDG_PRINT", "Building: Print" },
                    { "PRINT_INHOUSE_QLT_1", "Print In-house Quality 1" },
                    { "PRINT_INHOUSE_QLT_2", "Print In-house Quality 2" },
                    { "PRINT_EMERGENCY", "Print Emergency" },
                    { "BLDG_MARKETING", "Building: Marketing" },
                    { "PREMIERE", "Premiere" },
                    { "SUPER_PREMIERE", "Super Premiere" },
                    { "WM_HOSPICE", "Wm Hospice" },
                    { "WM_ORPHANAGE", "Wm Orphanage" },
                    { "WM_WEDDING", "Wm Wedding" },
                    { "WM_HOMELESS", "Wm Homeless" },
                    { "WM_DEBT", "Wm Debt" },
                    { "BM_UNLOCK", "Bm Unlock" },
                    { "BM_DROWNING", "Bm Drowning" },
                    { "BM_DRUNKARD", "Bm Drunkard" },
                    { "BM_FIGHT", "Bm Fight" },
                    { "BM_CRIMINAL", "Bm Criminal" },
                    { "BM_HOUSE_BURN", "Bm House Burn" },
                    { "SCANDAL_COVER_UP_MONEY", "Scandal Cover Up Money" },
                    { "SCANDAL_COVER_UP_PP", "Scandal Cover Up Influence" },
                    { "BLDG_SHENANIGANS", "Building: Shenanigans" },
                    { "SHENANIGANS_BEATING", "Shenanigans Beating" },
                    { "SHENANIGANS_KIDNAPPING", "Shenanigans Kidnapping" },
                    { "SHENANIGANS_MURDER", "Shenanigans Murder" },
                    { "LEAK_RISK_REDUCE_1", "Leak Risk Reductionuce 1" },
                    { "SPYING_SINS", "Spying Sins" },
                    { "SPYING_ILLEGALPREFERENCES", "Spying Illegalpreferences" },
                    { "SPYING_XP_BONUS_1", "Spying XP Bonus 1" },
                    { "SPYING_XP_BONUS_2", "Spying XP Bonus 2" },
                    { "FAIL_NO_DISCLOSURE", "Fail No Disclosure" },
                    { "SECURITY_SCHOOL", "Security School" },
                    { "SECURITY_SCHOOL_FAST", "Security School Fast" },
                    { "SECURITY_SCHOOL_STRONG", "Security School Strong" },
                    { "BLDG_SPIES", "Building: Spies" },
                    { "ACTIVE_PROTECTION", "Active Protection" },
                    { "ACTIVE_PROTECTION_XP_BONUS_1", "Active Protection XP Bonus 1" },
                    { "ACTIVE_PROTECTION_XP_BONUS_2", "Active Protection XP Bonus 2" },
                    { "SECRETS_HIDE_EFFECT_BOOST", "Secrets Hide Effect Boost" },
                    { "FAIL_DISCLOSURE_NO_LEAK", "Fail Disclosure No Leak" },
                    { "WG_WATCHES", "Wg Watches" },
                    { "WG_CIGARS", "Wg Cigars" },
                    { "WG_ALCOHOL", "Wg Alcohol" },
                    { "WG_HAUTE_WARDROBE", "Wg Haute Wardrobe" },
                    { "WG_SPORTCAR", "Wg Sportcar" },
                    { "BG_UNLOCK", "Bg Unlock" },
                    { "BG_NARCOTICS", "Bg Narcotics" },
                    { "BG_METH", "Bg Meth" },
                    { "BG_NARCOTICS_2", "Bg Narcotics 2" },
                    { "BG_SAFARI", "Bg Safari" },
                    { "BG_XXX", "Bg Xxx" },
                    { "BG_BRAINS", "Smart extras" },
                    { "BG_KILLING", "Bg Killing" },
                    { "BG_CANNIBAL", "Bg Cannibal" },
                    { "BG_UNDERAGE", "Bg Underage" },
                    { "BLDG_EVENTS_STAGE", "Building: Events Stage" },
                    { "OFFICIAL_RECEPTION_1", "Official reception I" },
                    { "OFFICIAL_RECEPTION_2", "Official reception II" },
                    { "OFFICIAL_RECEPTION_3", "Official reception III" },
                    { "PARTY_1", "Company party I" },
                    { "PARTY_2", "Company party II" },
                    { "PARTY_3", "Company party III" },
                    { "INSURANCE_PLUS", "Expanded health insurance" },
                    { "HOUSEMAID", "Housemaid" },
                    { "NANNY", "Nanny" },
                    { "ASSISTANT", "Assistant" },
                    { "SPOUSES_ASSISTANT", "Spouses Assistant" },
                    { "CHEF", "Chef" },
                    { "BUTLER", "Butler" },
                    { "HOTEL_SUITE", "Hotel Suite" },
                    { "VILLA", "Villa" },
                    { "PENTHOUSE", "Penthouse" },
                    { "PERSONAL_DRIVER", "Car with driver" },
                    { "PERSONAL_DRIVER_PREMIUM", "Luxury car with driver" },
                    { "TWO_PROJECTS", "Two Projects" },
                    { "PRODUCERS_ON_FILM_2", "Producers On Film 2" },
                    { "PRODUCERS_ON_FILM_3", "Producers On Film 3" },
                    { "NEGOTIATION_SCALE_50", "Negotiation Scale 50" },
                    { "NEGOTIATION_SCALE_75", "Negotiation Scale 75" },
                    { "CONTRACT_WEIGHT", "Contract Weight" },
                    { "FOCUS_INHOUSE_RED_TIME_1", "Focus In-house Reduction Time 1" },
                    { "FOCUS_INHOUSE_RED_PRICE_1", "Focus In-house Reduction Price 1" },
                    { "IP_HYPE", "IP Hype" },
                    { "IP_HYPE_RED_TIME_1", "IP Hype Reduction Time 1" },
                    { "IP_HYPE_RED_PRICE_1", "IP Hype Reduction Price 1" },
                    { "IP_KEEPER", "IP Keeper" },
                    { "IP_CONTRACT_WEIGHT", "IP Contract Weight" },
                    { "IP_TALANTS_LNT_BONUS_XP", "IP Talants Lnt Bonus XP" },
                    { "START_PROD_NO_ACT", "Start Prod No Act" },
                    { "IP_MOVIE_THEATRE_CHEAP", "IP Movie Theatre Cheap" },
                    { "REPAIR_TEAM_1", "Repair Team 1" },
                    { "BLDG_WATER_TOWER_I", "Building: Water Tower I" },
                    { "BLDG_WATER_TOWER_II", "Building: Water Tower Ii" },
                    { "BLDG_WATER_TOWER_III", "Building: Water Tower Iii" },
                    { "WATER_TOWER_AMT_3", "Water Tower Amount 3" },
                    { "BLDG_POWERPLANT_I", "Building: Powerplant I" },
                    { "BLDG_POWERPLANT_II", "Building: Powerplant Ii" },
                    { "BLDG_POWERPLANT_III", "Building: Powerplant Iii" },
                    { "POWERPLANT_AMT_3", "Powerplant Amount 3" },
                    { "IMPROVEMENT_I", "Improvement I" },
                    { "IMPROVEMENT_II", "Improvement Ii" },
                    { "IMPROVEMENT_III", "Improvement Iii" },
                    { "STUDIO_TECH", "Studio Tech" },
                    { "STUDIO_TECH_ADD_RND", "Additional research group" },
                    { "STUDIO_TECH_RED_TIME_1", "Studio Tech Reduction Time 1" },
                    { "STUDIO_TECH_RED_TIME_2", "Studio Tech Reduction Time 2" },
                    { "BLDG_RND_I", "Building: Rnd I" },
                    { "BLDG_RND_II", "Building: Rnd Ii" },
                    { "BLDG_RND_III", "Building: Rnd Iii" },
                    { "BLDG_RND_IV", "Building: Rnd Iv" },
            };
        }

        private string HumanizeIdentifier(string id)
        {
            if (!IsRussianLocale)
                return HumanizeEnglish(id);

            string prefix = "";
            string work = id;
            if (work.StartsWith("BLDG_", StringComparison.OrdinalIgnoreCase)) { prefix = "Постройка: "; work = work.Substring(5); }
            else if (work.StartsWith("BG_", StringComparison.OrdinalIgnoreCase)) { prefix = "Массовка: "; work = work.Substring(3); }
            else if (work.StartsWith("BM_", StringComparison.OrdinalIgnoreCase)) { prefix = "Чёрный маркетинг: "; work = work.Substring(3); }
            else if (work.StartsWith("WG_", StringComparison.OrdinalIgnoreCase)) { prefix = "Подарок: "; work = work.Substring(3); }
            else if (work.StartsWith("WM_", StringComparison.OrdinalIgnoreCase)) { prefix = "Благотворительность: "; work = work.Substring(3); }

            work = Regex.Replace(work, "([A-Z]+)([0-9]+)$", "$1_$2");
            string[] raw = work.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();

            Dictionary<string, string> dict = RussianTokenDictionary();
            foreach (string rawToken in raw)
            {
                string token = rawToken.ToUpperInvariant();
                string translated;
                if (dict.TryGetValue(token, out translated)) result.Add(translated);
                else if (IsNumber(token)) result.Add(ToRomanOrNumber(token));
                else result.Add(CapitalizeRu(token.ToLowerInvariant()));
            }

            string text = prefix + string.Join(" ", result);
            text = CleanupRussianName(text);
            return text.Trim();
        }

        private string HumanizeEnglish(string id)
        {
            string work = Regex.Replace(id, "([A-Z]+)([0-9]+)$", "$1_$2");
            string[] raw = work.Replace("_", " ").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> words = new List<string>();
            foreach (string w in raw)
            {
                if (IsNumber(w)) words.Add(ToRomanOrNumber(w));
                else if (w.Length <= 3 && w.ToUpperInvariant() == w) words.Add(w);
                else words.Add(char.ToUpper(w[0]) + w.Substring(1).ToLowerInvariant());
            }
            return string.Join(" ", words).Replace("Bldg", "Building").Replace("Qlt", "Quality").Replace("Amt", "Amount").Replace("Red", "Reduction").Replace("Rnd", "Research");
        }

        private Dictionary<string, string> RussianTokenDictionary()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "ACTIVE", "активная" }, { "PROTECTION", "защита" }, { "BONUS", "бонус" },
                { "ADDITIONAL", "дополнительная" }, { "REHEARSAL", "репетиция" }, { "ANALYSIS", "анализ" },
                { "BUDGET", "бюджета" }, { "ENTIRE", "всего" }, { "CAST", "состава" }, { "GROUPS", "групп" },
                { "SCREENPLAY", "сценария" }, { "SCREENPLAYS", "сценарии" }, { "TAGS", "теги" }, { "TAG", "тег" },
                { "SMART", "умная" }, { "BRAINS", "умная" }, { "CANNIBAL", "каннибализм" }, { "KILLING", "убийства" },
                { "METH", "метамфетамин" }, { "NARCOTICS", "наркотики" }, { "SAFARI", "сафари" }, { "UNDERAGE", "несовершеннолетние" },
                { "XXX", "для взрослых" }, { "UNLOCK", "открытие" }, { "DROWNING", "утопление" }, { "DRUNKARD", "пьянство" },
                { "FIGHT", "драка" }, { "CRIMINAL", "криминал" }, { "HOUSE", "дом" }, { "BURN", "пожар" },
                { "RADIO", "радио" }, { "TELEVISION", "телевидение" }, { "BUTLER", "дворецкий" }, { "CHEF", "шеф-повар" },
                { "CONCERT", "концерт" }, { "INHOUSE", "внутренний" }, { "IMPROVED", "улучшение" }, { "MPROVED", "улучшение" },
                { "TIME", "время" }, { "CONTRACT", "контракт" }, { "WEIGHT", "вес" }, { "EDITS", "правки" }, { "ON", "на" }, { "GO", "ходу" },
                { "EXTRAS", "массовка" }, { "FAIL", "провал" }, { "DISCLOSURE", "разглашение" }, { "NO", "без" }, { "LEAK", "утечки" },
                { "FLEX", "гибкий" }, { "SCHEDULE", "график" }, { "FOCUS", "фокус" }, { "OUTSOURCE", "аутсорс" },
                { "PRICE", "цены" }, { "RED", "сокращение" }, { "QLT", "качество" }, { "QUALITY", "качество" },
                { "HOTEL", "отель" }, { "SUITE", "номер" }, { "HOUSEMAID", "горничная" }, { "IMPROVEMENT", "улучшение" },
                { "INSURANCE", "страховка" }, { "PLUS", "плюс" }, { "IP", "влияние" }, { "HYPE", "ажиотаж" }, { "KEEPER", "хранитель" },
                { "TALANTS", "талантов" }, { "TALENTS", "талантов" }, { "LNT", "лейтенантов" }, { "XP", "опыт" },
                { "LAB", "лаборатория" }, { "RISK", "риск" }, { "REDUCE", "снижение" }, { "LITERARY", "литературные" }, { "WORK", "произведения" },
                { "RESEARCH", "исследование" }, { "LOCATION", "локации" }, { "SEARCH", "поиск" }, { "WORLD", "мировой" },
                { "MARKET", "рынок" }, { "INTERVIEW", "опрос" }, { "MOVIE", "фильм" }, { "THEATRE", "кинотеатр" }, { "CHEAP", "дешевле" },
                { "SLOT", "слот" }, { "ADD", "добавление" }, { "RENT", "аренда" }, { "PALACE", "дворец" },
                { "RELEASE", "релиз" }, { "ATTITUDE", "отношение" }, { "MOOD", "настроение" }, { "TOP10", "топ-10" }, { "ART", "арт" }, { "AUD", "аудитория" }, { "COM", "коммерция" },
                { "SUCCESSOR", "преемник" }, { "FRESH", "свежий" }, { "PERSPECTIVE", "взгляд" }, { "NEW", "новый" }, { "BY", "через" }, { "LT", "лейтенанта" },
                { "OFFICIAL", "официальный" }, { "RECEPTION", "приём" }, { "PARTY", "вечеринка" }, { "PAVILION", "павильон" },
                { "PENTHOUSE", "пентхаус" }, { "PERSONAL", "личный" }, { "DRIVER", "водитель" }, { "PREMIUM", "премиум" },
                { "POST", "пост" }, { "DIR", "режиссёр" }, { "MONT", "монтаж" }, { "COMP", "композитор" },
                { "POSTRELEASE", "послерелизный" }, { "PROD", "продюсер" }, { "CIN", "оператор" }, { "ACT", "актёр" },
                { "PRODUCERS", "продюсеры" }, { "FILM", "фильм" }, { "NEGOTIATION", "переговоры" }, { "SCALE", "шкала" },
                { "START", "старт" }, { "REPAIR", "ремонтная" }, { "TEAM", "команда" }, { "WATER", "водонапорная" }, { "TOWER", "башня" },
                { "AMT", "количество" }, { "POWERPLANT", "электростанция" }, { "STUDIO", "студия" }, { "TECH", "технологии" }, { "RND", "исследовательская группа" },
                { "QUARTERLY", "квартальный" }, { "REPORT", "отчёт" }, { "CASH", "деньги" }, { "FLOW", "поток" },
                { "TAX", "налог" }, { "BASE", "база" }, { "REDUCTION", "снижение" }, { "LOAN", "кредит" }, { "BANK", "банк" },
                { "EARLY", "досрочное" }, { "REPAYMENT", "погашение" }, { "INT", "ставка" }, { "RATE", "ставка" }, { "AMOUNT", "сумма" }, { "TERM", "срок" },
                { "REFINANCING", "рефинансирование" }, { "MICROLOAN", "микрокредит" }, { "COOLDOWN", "задержка" },
                { "LEGAL", "юридическая" }, { "DEFENSE", "защита" }, { "TERMINATION", "расторжение" }, { "FEE", "штраф" },
                { "PAYMENTS", "оплата" }, { "GROSS", "сборы" }, { "YEARS", "лет" }, { "MOVIES", "фильмов" },
                { "CHARITY", "благотворительность" }, { "REP", "репутация" }, { "PROFITABLE", "прибыльный" }, { "GENERATION", "генерация" },
                { "AND", "и" }, { "GOOD", "хорошее" }, { "ICON", "икона" }, { "LEGEND", "легенда" }, { "SKILLED", "умелый" },
                { "ACTOR", "актёр" }, { "PREMIERE", "премьера" }, { "SUPER", "супер" }, { "INITIATIVE", "инициатива" }, { "FREE", "бесплатно" },
                { "SUPPLY", "снабжение" }, { "CASTING", "кастинг" }, { "SCOUT", "скаут" }, { "WORKSHOP", "мастерская" },
                { "SETS", "декорации" }, { "PROPS", "реквизит" }, { "SECOND", "вторая" }, { "UNIT", "группа" }, { "URGENT", "экстренный" },
                { "DOUBLE", "дублёр" }, { "CREW", "команда" }, { "LOGISTICS", "логистика" }, { "SERVICE", "обслуживание" },
                { "SOUND", "звук" }, { "DISTRIBUTION", "дистрибуция" }, { "ANALYTICS", "аналитика" }, { "PRINT", "печать" }, { "EMERGENCY", "экстренная" },
                { "MARKETING", "маркетинг" }, { "HOSPICE", "хоспис" }, { "ORPHANAGE", "приют" }, { "WEDDING", "свадьба" }, { "HOMELESS", "бездомные" }, { "DEBT", "долги" },
                { "SHENANIGANS", "грязные дела" }, { "BEATING", "избиение" }, { "KIDNAPPING", "похищение" }, { "MURDER", "убийство" },
                { "SPYING", "слежка" }, { "SINS", "грехи" }, { "ILLEGALPREFERENCES", "запретные предпочтения" }, { "ILLEGAL", "нелегальные" }, { "PREFERENCES", "предпочтения" },
                { "SECURITY", "безопасность" }, { "SCHOOL", "школа" }, { "FAST", "быстро" }, { "STRONG", "сильно" }, { "SECRETS", "секреты" }, { "HIDE", "скрытие" }, { "EFFECT", "эффект" }, { "BOOST", "усиление" },
                { "ETHNIC", "этнический" }, { "COMPOSITION", "состав" }, { "WORKERS", "работники" }, { "ILLEGALS", "нелегалы" }, { "STAFF", "штат" }, { "LARGE1", "расширение I" }, { "LARGE2", "расширение II" }, { "BUILDINGS", "постройки" }, { "CONSERVATION", "консервация" }, { "SALARY", "зарплата" }, { "CUT", "сокращение" }, { "BAD", "плохое" }, { "SADNESS", "грусть" }, { "HIRING", "найм" }, { "NOMINATION", "номинация" }, { "LOSS", "проигрыш" },
                { "ESCORT", "эскорт" }, { "DOMINION", "доминьон" }
            };
        }

        private string CleanupRussianName(string text)
        {
            text = text.Replace("исследование время сокращение", "сокращение времени исследования");
            text = text.Replace("время сокращение", "сокращение времени");
            text = text.Replace("цены сокращение", "сокращение цены");
            text = text.Replace("качество I", "качество I");
            text = text.Replace("Теги", "теги");
            return CapitalizeRu(text);
        }

        private string CapitalizeRu(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return char.ToUpper(value[0]) + value.Substring(1);
        }

        private bool IsNumber(string value)
        {
            int x;
            return int.TryParse(value, out x);
        }

        private string ToRomanOrNumber(string number)
        {
            switch (number)
            {
                case "1": return "I";
                case "2": return "II";
                case "3": return "III";
                case "4": return "IV";
                case "5": return "V";
                case "6": return "VI";
                case "7": return "VII";
                case "8": return "VIII";
                case "9": return "IX";
                case "10": return "X";
                case "50": return "50";
                case "75": return "75";
                default: return number;
            }
        }

        private int GetDomainOrder(string domain)
        {
            int n;
            if (int.TryParse(domain, out n)) return n;
            return 999;
        }

        private string GetDomainDisplayName(string domain)
        {
            switch ((domain ?? "").Trim())
            {
                case "1": return L("Studio departments", "Отделы студии");
                case "2": return L("Scripts and intellectual property", "Сценарии и права");
                case "3": return L("Pre-production", "Подготовка к съёмкам");
                case "4": return L("Production", "Производство");
                case "5": return L("Post-production", "Постпродакшен");
                case "6": return L("Distribution and marketing", "Прокат и маркетинг");
                case "7": return L("Security and dirty work", "Безопасность и грязные дела");
                case "8": return L("Comfort and events", "Комфорт и мероприятия");
                case "9": return L("Producing", "Продюсирование");
                case "10": return L("Infrastructure", "Инфраструктура");
                case "11": return L("Technology", "Технологии");
                default: return L("Other", "Прочее");
            }
        }

        private int GetDepartmentOrder(string department)
        {
            switch ((department ?? "NONE").ToUpperInvariant())
            {
                case "NONE": return 0;
                case "HR": return 1;
                case "FINANCE": return 2;
                case "LAW": return 3;
                case "PR": return 4;
                case "COMFORT": return 5;
                case "SECURITY": return 6;
                case "PRODUCTION": return 7;
                case "PRODUCING": return 8;
                case "POSTPRODUCTION": return 9;
                case "TECHNOLOGY": return 10;
                default: return 99;
            }
        }

        private string GetDepartmentDisplayName(string department)
        {
            switch ((department ?? "NONE").ToUpperInvariant())
            {
                case "NONE": return L("General", "Общее");
                case "HR": return L("HR Department", "HR-отдел");
                case "FINANCE": return L("Financial Department", "Финансовый отдел");
                case "LAW": return L("Legal Department", "Юридический отдел");
                case "PR": return L("Public Relations Department", "Отдел связей с общественностью");
                case "COMFORT": return L("Comfort Department", "Отдел обеспечения комфорта");
                case "SECURITY": return L("Security Department", "Отдел безопасности");
                case "PRODUCTION": return L("Production Department", "Производственный отдел");
                case "PRODUCING": return L("Producing Department", "Отдел продюсирования");
                case "POSTPRODUCTION": return L("Post-production Department", "Отдел постпродакшена");
                case "TECHNOLOGY": return L("Technology Department", "Отдел технологий");
                default: return department;
            }
        }

        private string GetDepartmentIcon(string department)
        {
            switch ((department ?? "NONE").ToUpperInvariant())
            {
                case "HR": return "👥";
                case "FINANCE": return "💰";
                case "LAW": return "⚖";
                case "PR": return "📣";
                case "COMFORT": return "🎁";
                case "SECURITY": return "🛡";
                case "PRODUCTION": return "🎬";
                case "PRODUCING": return "📋";
                case "POSTPRODUCTION": return "🎞";
                case "TECHNOLOGY": return "⚙";
                default: return "🔬";
            }
        }

        private int GetPropertyOrder(string property)
        {
            switch (property)
            {
                case "id": return 0;
                case "domain": return 1;
                case "department": return 2;
                case "duration": return 3;
                case "staff": return 4;
                case "electricity": return 5;
                case "water": return 6;
                case "unlockedByPerks": return 7;
                case "dependsOnBuildings": return 8;
                case "hasHiddenObjects": return 9;
                case "shapeType": return 10;
                case "behaviour": return 11;
                case "triggers": return 12;
                default: return 100;
            }
        }

        private string GetPropertyDisplayName(string property)
        {
            switch (property)
            {
                case "id": return L("ID", "ID");
                case "domain": return L("Domain", "Раздел");
                case "department": return L("Department", "Отдел");
                case "unlockedByPerks": return L("Unlocked by perks", "Открывается исследованиями");
                case "dependsOnBuildings": return L("Depends on buildings", "Зависит от построек");
                case "hasHiddenObjects": return L("Hidden in tree", "Скрыто в дереве");
                case "duration": return L("Duration", "Длительность");
                case "staff": return L("Staff", "Персонал");
                case "electricity": return L("Electricity", "Электричество");
                case "water": return L("Water", "Вода");
                case "triggers": return L("Triggers", "Триггеры");
                case "shapeType": return L("Shape type", "Тип формы");
                case "behaviour": return L("Behaviour", "Поведение");
                default: return property;
            }
        }

        private void BuildBulkPropertyList()
        {
            if (BulkPropertyCombo == null) return;
            BulkPropertyCombo.Items.Clear();
            BulkPropertyCombo.Items.Add(new FilterItem("duration", L("Duration", "Длительность")));
            BulkPropertyCombo.Items.Add(new FilterItem("staff", L("Staff", "Персонал")));
            BulkPropertyCombo.Items.Add(new FilterItem("electricity", L("Electricity", "Электричество")));
            BulkPropertyCombo.Items.Add(new FilterItem("water", L("Water", "Вода")));
            BulkPropertyCombo.SelectedIndex = 0;
        }

        private void ApplyToAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FilterItem selected = BulkPropertyCombo.SelectedItem as FilterItem;
                if (selected == null || string.IsNullOrWhiteSpace(selected.Value)) return;

                string propertyName = selected.Value;
                string rawValue = BulkValueBox.Text == null ? "" : BulkValueBox.Text.Trim().Replace(',', '.');
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    MessageBox.Show(L("Enter a value first.", "Сначала введите значение."), L("Warning", "Внимание"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int changed = 0;
                foreach (PerkEditorItem item in items)
                {
                    JProperty prop = item.Object.Property(propertyName);
                    if (prop == null)
                    {
                        item.Object[propertyName] = ConvertBulkValue(propertyName, rawValue, null);
                        changed++;
                        continue;
                    }

                    prop.Value = ConvertBulkValue(propertyName, rawValue, prop.Value);
                    changed++;
                }

                BuildUI();
                MessageBox.Show(
                    L("Updated perks: ", "Изменено исследований: ") + changed,
                    L("Done", "Готово"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("Bulk edit failed:\n", "Не удалось выполнить массовое изменение:\n") + ex.Message,
                    L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private JToken ConvertBulkValue(string propertyName, string rawValue, JToken oldValue)
        {
            if (propertyName == "electricity" || propertyName == "water")
                return rawValue;

            int intValue;
            double doubleValue;

            if (oldValue != null && oldValue.Type == JTokenType.Integer && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                return intValue;

            if (double.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
            {
                if (Math.Abs(doubleValue - Math.Round(doubleValue)) < 0.0000001 && !rawValue.Contains("."))
                    return Convert.ToInt64(Math.Round(doubleValue));
                return doubleValue;
            }

            return rawValue;
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Expander expander in visibleExpanders) expander.IsExpanded = true;
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Expander expander in visibleExpanders) expander.IsExpanded = false;
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configFilePath))
                {
                    MessageBox.Show(L("First select Perks.json.", "Сначала выберите Perks.json."), L("Restore", "Восстановление"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Perks.json");
                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show(L($"Perks.json was not found in Resources:\n{sourcePath}", $"Perks.json не найден в Resources:\n{sourcePath}"), L("Restore", "Восстановление"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    L("Replace current Perks.json with the file from Resources?", "Заменить текущий Perks.json файлом из Resources?"),
                    L("Restore", "Восстановление"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                File.Copy(sourcePath, configFilePath, true);
                LoadConfig(configFilePath);
                MessageBox.Show(L("Perks.json restored successfully.", "Perks.json успешно восстановлен."), L("Restore", "Восстановление"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("Restore error:\n", "Ошибка восстановления:\n") + ex.Message, L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string json = configData.ToString(Formatting.Indented);
                File.WriteAllText(configFilePath, json);

                MessageBox.Show(
                    L("Perks.json saved successfully. Restart the game for changes to take effect.",
                      "Perks.json успешно сохранён. Перезапустите игру, чтобы изменения вступили в силу."),
                    L("Success", "Готово"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("Error saving Perks.json:\n", "Ошибка сохранения Perks.json:\n") + ex.Message,
                    L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class PerkEditorItem : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string OriginalKey { get; set; }
        public string Department { get; set; }
        public string Domain { get; set; }
        public string DisplayName { get; set; }
        public JObject Object { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class FilterItem
    {
        public string Value { get; private set; }
        public string Text { get; private set; }

        public FilterItem(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
