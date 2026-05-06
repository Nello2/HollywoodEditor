// Был основан на базе Hollywood Animal Calculator
// Автор: CallOn84  - https://github.com/CallOn84/Hollywood-Animal-Calculator
// Внедрил: Galapogos

using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HollywoodEditor
{
    public partial class CalcWindow : Window
    {
        static CalcWindow()
        {
            EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(GlobalScrollableControl_PreviewMouseWheel), true);
            EventManager.RegisterClassHandler(typeof(ComboBoxItem), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(GlobalScrollableControl_PreviewMouseWheel), true);
            EventManager.RegisterClassHandler(typeof(ListBox), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(GlobalScrollableControl_PreviewMouseWheel), true);
            EventManager.RegisterClassHandler(typeof(ListBoxItem), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(GlobalScrollableControl_PreviewMouseWheel), true);
            EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(GlobalScrollableControl_PreviewMouseWheel), true);
        }

        private readonly List<string> categoryOrder = new List<string>
        {
            "Genre", "Setting", "Protagonist", "Antagonist", "Supporting Character", "Theme & Event", "Finale"
        };

        private readonly Dictionary<string, TagInfo> tags = new Dictionary<string, TagInfo>();
        private JObject compatibility = new JObject();
        private JObject genrePairs = new JObject();
        private JObject locEng = new JObject();
        private JObject locRus = new JObject();
        private readonly Random rng = new Random();

        private readonly List<SelectedTag> synergyTags = new List<SelectedTag>();
        private readonly List<SelectedTag> advertiserTags = new List<SelectedTag>();
        private readonly List<SelectedTag> lockedTags = new List<SelectedTag>();
        private readonly List<SelectedTag> excludedTags = new List<SelectedTag>();
        private readonly List<GeneratedScript> generatedScripts = new List<GeneratedScript>();
        private readonly List<GeneratedScript> pinnedScripts = new List<GeneratedScript>();

        private bool isRussian;
        private string dataStatusKey = string.Empty;

        private readonly List<AdAgent> adAgents = new List<AdAgent>
        {
            new AdAgent { Name = "NBG", Targets = new [] { "AM", "AF" }, Type = 0, Level = 3 },
            new AdAgent { Name = "Ross&Ross Bros.", Targets = new [] { "AM", "AF" }, Type = 0, Level = 2 },
            new AdAgent { Name = "Vien Pascal", Targets = new [] { "TM", "TF", "AM", "AF" }, Type = 1, Level = 2 },
            new AdAgent { Name = "Spark", Targets = new [] { "YM", "YF", "AM", "AF" }, Type = 2, Level = 3 },
            new AdAgent { Name = "Nate Sparrow Press", Targets = new [] { "YM", "YF", "AM", "AF" }, Type = 0, Level = 3 },
            new AdAgent { Name = "Velvet Gloss", Targets = new [] { "TF", "YF", "AF" }, Type = 2, Level = 3 },
            new AdAgent { Name = "Pierre Zola Company", Targets = new [] { "TM", "YM", "AM" }, Type = 0, Level = 2 },
            new AdAgent { Name = "Spice Mice", Targets = new [] { "TM", "TF", "YM", "YF" }, Type = 2, Level = 2 }
        };

        private readonly Dictionary<string, string> demoNamesEng = new Dictionary<string, string>
        {
            ["YM"] = "Young men",
            ["YF"] = "Young women",
            ["TM"] = "Boys",
            ["TF"] = "Girls",
            ["AM"] = "Men",
            ["AF"] = "Women"
        };

        private readonly Dictionary<string, string> demoNamesRus = new Dictionary<string, string>
        {
            ["YM"] = "Молодые мужчины",
            ["YF"] = "Молодые женщины",
            ["TM"] = "Мальчики",
            ["TF"] = "Девочки",
            ["AM"] = "Мужчины",
            ["AF"] = "Женщины"
        };

        private readonly string[] starterWhitelist = new[]
        {
            "ACTION", "COMEDY", "DRAMA", "ROMANCE", "ADVENTURE", "DETECTIVE", "HISTORICAL", "THRILLER",
            "WILD_WEST", "FANTASY_KINGDOM", "MODERN_AMERICAN_CITY", "MODERN_AMERICAN_TOWN", "TROPICAL_ISLAND",
            "PROTAGONIST_CLUMSY_OAF", "PROTAGONIST_COP", "PROTAGONIST_COWBOY", "PROTAGONIST_DARING_ADVENTURER",
            "PROTAGONIST_DETECTIVE", "PROTAGONIST_HOPELESS_ROMANTIC", "PROTAGONIST_KNIGHT", "PROTAGONIST_WORKING_MAN",
            "ANTAGONIST_BANDIT", "ANTAGONIST_CRIMINAL_MASTERMIND", "ANTAGONIST_EVIL_MONSTER", "ANTAGONIST_EVIL_WITCH",
            "ANTAGONIST_MURDERER", "ANTAGONIST_SERIAL_KILLER", "ANTAGONIST_TRIBAL_CHIEF",
            "SUPPORTINGCHARACTER_ANGRY_BOSS", "SUPPORTINGCHARACTER_DAMSEL_IN_DISTRESS", "SUPPORTINGCHARACTER_FEMME_FATALE",
            "SUPPORTINGCHARACTER_LOVE_INTEREST", "SUPPORTINGCHARACTER_MENTOR", "SUPPORTINGCHARACTER_RIVAL",
            "SUPPORTINGCHARACTER_SIDEKICK", "SUPPORTINGCHARACTER_STRICT_PARENT",
            "EVENTS_ANCIENT_PUZZLE", "THEME_AVENGING_LOVED_ONES", "EVENTS_BANK_ROBBERY", "EVENTS_JOUSTING_TOURNAMENT",
            "THEME_LOVE_TRIANGLE", "EVENTS_PRISON_BREAK", "THEME_SEARCH_KILLER", "EVENTS_SHOOTOUT", "THEME_SLAPSTICK_MAYHEM",
            "THEME_STRUGGLE_FOR_BETTER_LIFE", "THEME_TREASURE_HUNT", "THEME_UNREQUITED_LOVE", "THEME_WINNING_THE_BELOVED",
            "FINALE_ANTAGONIST_GETS_KILLED", "FINALE_ANTAGONIST_GETS_PUNISHED", "FINALE_ANTAGONIST_REPENTS",
            "FINALE_PROTAGONIST_DIES_HEROICALLY", "FINALE_PROTAGONIST_FINDS_TREASURE", "FINALE_PROTAGONIST_GETS_CHANCE_FOR_BETTER_LIFE",
            "FINALE_PROTAGONIST_OVERCAME_SELFDOUBT", "FINALE_PROTAGONIST_RESCUES_HOSTAGE", "FINALE_SWEETHEARTS_STAY_TOGETHER"
        };

        public CalcWindow()
        {
            InitializeComponent();
            isRussian = DetectRussianLocale();
            SetLanguageBoxWithoutBreakingInitialization();
            LoadAllData();
            ApplyLocalization();
            InitializeAllSelectors();
            RefreshAllLists();
            CalculateDistributionButton_Click(null, null);
        }

        private void SetLanguageBoxWithoutBreakingInitialization()
        {
            if (LanguageBox == null) return;
            string tag = isRussian ? "RUS" : "ENG";
            foreach (ComboBoxItem item in LanguageBox.Items)
            {
                if (Convert.ToString(item.Tag) == tag)
                {
                    LanguageBox.SelectedItem = item;
                    break;
                }
            }
        }

        private bool DetectRussianLocale()
        {
            try
            {
                string locale = HollywoodEditor.ViewModels.MainModel.CurrentLocale;
                if (!string.IsNullOrWhiteSpace(locale))
                {
                    locale = locale.Trim().ToUpperInvariant();
                    return locale == "RUS" || locale == "RU" || locale == "RU-RU";
                }
            }
            catch { }
            return false;
        }

        private bool IsRussianUi()
        {
            if (LanguageBox != null)
            {
                ComboBoxItem selected = LanguageBox.SelectedItem as ComboBoxItem;
                if (selected != null)
                {
                    string tag = Convert.ToString(selected.Tag);
                    string content = Convert.ToString(selected.Content);
                    if (string.Equals(tag, "RUS", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(tag, "RU", StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(content) && content.IndexOf("Рус", StringComparison.OrdinalIgnoreCase) >= 0))
                        return true;

                    if (string.Equals(tag, "ENG", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(tag, "EN", StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(content) && content.IndexOf("English", StringComparison.OrdinalIgnoreCase) >= 0))
                        return false;
                }
            }

            return isRussian;
        }

        private string T(string en, string ru)
        {
            return IsRussianUi() ? ru : en;
        }

        private void SetDataStatus(string key)
        {
            dataStatusKey = key ?? string.Empty;
            UpdateDataStatusText();
        }

        private void UpdateDataStatusText()
        {
            if (DataStatusText == null) return;

            switch (dataStatusKey)
            {
                case "loaded":
                    DataStatusText.Text = T("Data loaded", "Данные загружены");
                    break;
                case "missing":
                    DataStatusText.Text = T("Some calculator data files were not found", "Некоторые файлы калькулятора не найдены");
                    break;
                case "error":
                    DataStatusText.Text = T("Calculator data load error", "Ошибка загрузки данных калькулятора");
                    break;
                default:
                    DataStatusText.Text = string.Empty;
                    break;
            }
        }

        private string WeekCaption(int weekNumber)
        {
            return (IsRussianUi() ? "Неделя" : "Week") + " " + weekNumber.ToString(CultureInfo.InvariantCulture);
        }

        private void LoadAllData()
        {
            try
            {
                string tagDataPath = FindDataFile("TagData.json");
                string weightsPath = FindDataFile("TagsAudienceWeights.json");
                string compatibilityPath = FindDataFile("TagCompatibilityData.json");
                string genrePairsPath = FindDataFile("GenrePairs.json");
                if (tagDataPath == null || weightsPath == null || compatibilityPath == null || genrePairsPath == null)
                {
                    SetDataStatus("missing");
                    return;
                }

                JObject tagData = JObject.Parse(File.ReadAllText(tagDataPath));
                JObject weightsData = JObject.Parse(File.ReadAllText(weightsPath));
                compatibility = JObject.Parse(File.ReadAllText(compatibilityPath));
                genrePairs = JObject.Parse(File.ReadAllText(genrePairsPath));

                locEng = LoadLocalizationJson("ENG") ?? new JObject();
                locRus = LoadLocalizationJson("RUS") ?? new JObject();

                tags.Clear();
                foreach (JProperty prop in tagData.Properties())
                {
                    JObject obj = prop.Value as JObject;
                    if (obj == null) continue;
                    string id = prop.Name;
                    var tag = new TagInfo
                    {
                        Id = id,
                        Category = NormalizeCategory(Convert.ToString(obj["CategoryID"]), id),
                        Art = ParseDouble(Convert.ToString(obj["artValue"])),
                        Commercial = ParseDouble(Convert.ToString(obj["commercialValue"]))
                    };

                    JObject wObj = weightsData[id] as JObject;
                    JObject ww = wObj != null ? wObj["weights"] as JObject : null;
                    foreach (string d in demoNamesEng.Keys)
                    {
                        tag.Weights[d] = ww != null ? ParseDouble(Convert.ToString(ww[d])) : 0.0;
                    }
                    tags[id] = tag;
                }
                UpdateLocalizedNames();
                SetDataStatus("loaded");
            }
            catch (Exception ex)
            {
                SetDataStatus("error");
                MessageBox.Show(ex.Message, T("Calculator data error", "Ошибка данных калькулятора"));
            }
        }

        private string FindDataFile(string fileName)
        {

            bool gameConfigFile = IsGameConfigFile(fileName);

            if (gameConfigFile)
            {
                string fromGame = FindGameConfigFile(fileName);
                if (!string.IsNullOrWhiteSpace(fromGame) && File.Exists(fromGame))
                    return fromGame;
            }

            var bases = new List<string>();
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            bases.Add(dir);
            bases.Add(Path.Combine(dir, "Resources"));
            bases.Add(Path.Combine(dir, "Data"));
            bases.Add(Path.Combine(dir, "Calculator"));

            DirectoryInfo di = new DirectoryInfo(dir);
            for (int i = 0; i < 5 && di != null; i++, di = di.Parent)
            {
                bases.Add(di.FullName);
                bases.Add(Path.Combine(di.FullName, "Resources"));
                bases.Add(Path.Combine(di.FullName, "Data"));
                bases.Add(Path.Combine(di.FullName, "Calculator"));
            }

            foreach (string b in bases.Distinct())
            {
                try
                {
                    string p = Path.Combine(b, fileName);
                    if (File.Exists(p)) return p;
                }
                catch { }
            }

            if (gameConfigFile)
                return SelectGameConfigManually(fileName);

            return null;
        }

        private bool IsGameConfigFile(string fileName)
        {
            return string.Equals(fileName, "TagData.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "TagCompatibilityData.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "GenrePairs.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "TagsAudienceWeights.json", StringComparison.OrdinalIgnoreCase);
        }

        private string FindGameConfigFile(string fileName)
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
                    Path.Combine(rootName, "Games2", "Hollywood Animal"),
                    Path.Combine(rootName, "GAMES", "Hollywood Animal"),
                    Path.Combine(rootName, "games", "Hollywood Animal"),
                    Path.Combine(rootName, "Игры", "Hollywood Animal")
                };

                foreach (string gameRoot in possibleRoots)
                {
                    string file = Path.Combine(gameRoot, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", fileName);
                    if (File.Exists(file)) return file;
                }

                try
                {
                    string[] foundDirs = Directory.GetDirectories(drive.RootDirectory.FullName, "Hollywood Animal", SearchOption.AllDirectories);
                    foreach (string dir in foundDirs)
                    {
                        string file = Path.Combine(dir, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", fileName);
                        if (File.Exists(file)) return file;
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (PathTooLongException) { }
                catch (IOException) { }
            }

            return null;
        }

        private string SelectGameConfigManually(string fileName)
        {
            try
            {
                string message = T(
                    fileName + " was not found automatically. Select it manually?\n\nExpected location:\n...\\Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Configs\\" + fileName,
                    fileName + " не найден автоматически. Выбрать его вручную?\n\nОжидаемый путь:\n...\\Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Configs\\" + fileName);

                MessageBoxResult result = MessageBox.Show(
                    message,
                    T("Calculator config not found", "Конфиг калькулятора не найден"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return null;

                var dialog = new OpenFileDialog
                {
                    Title = T("Select " + fileName, "Выберите " + fileName),
                    Filter = fileName + "|" + fileName + "|JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                return dialog.ShowDialog() == true ? dialog.FileName : null;
            }
            catch
            {
                return null;
            }
        }

        private void UpdateLocalizedNames()
        {
            foreach (TagInfo tag in tags.Values)
            {
                tag.Name = LocalizedTagName(tag.Id);
                tag.Display = tag.Name;
            }

            foreach (SelectedTag tag in synergyTags) RefreshSelectedTagName(tag);
            foreach (SelectedTag tag in advertiserTags) RefreshSelectedTagName(tag);
            foreach (SelectedTag tag in lockedTags) RefreshSelectedTagName(tag);
            foreach (SelectedTag tag in excludedTags) RefreshSelectedTagName(tag);
            foreach (GeneratedScript script in generatedScripts) RefreshScriptTagNames(script);
            foreach (GeneratedScript script in pinnedScripts) RefreshScriptTagNames(script);
        }

        private void RefreshSelectedTagName(SelectedTag tag)
        {
            if (tag == null || string.IsNullOrWhiteSpace(tag.Id)) return;
            tag.Name = LocalizedTagName(tag.Id);
            if (tags.TryGetValue(tag.Id, out TagInfo info))
                tag.Category = info.Category;
        }

        private void RefreshScriptTagNames(GeneratedScript script)
        {
            if (script == null || script.Tags == null) return;
            foreach (SelectedTag tag in script.Tags) RefreshSelectedTagName(tag);
        }

        private string LocalizedTagName(string id)
        {
            string manual = ManualTagTranslation(id);
            if (!string.IsNullOrWhiteSpace(manual)) return CleanLocString(manual);

            string fromLoc = GetLocString(id);
            if (!string.IsNullOrWhiteSpace(fromLoc)) return CleanLocString(fromLoc);

            string fallback = LocalizedFallbackTagName(id);
            if (!string.IsNullOrWhiteSpace(fallback)) return fallback;

            return Humanize(id);
        }

        private string ManualTagTranslation(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            if (isRussian)
            {
                switch (id)
                {
                    case "SUPPORTINGCHARACTER_IMPOSED_COUPLE": return "Навязанная пара";
                    case "THEME_FAKE_RELATIONSHIP": return "Фальшивые отношения";
                    case "THEME_PUBLIC_LOVE_CONFESSION": return "Публичное признание в любви";
                    case "EVENTS_PUBLIC_LOVE_CONFESSION": return "Публичное признание в любви";
                    case "PUBLIC_LOVE_CONFESSION": return "Публичное признание в любви";
                    case "FAKE_RELATIONSHIP": return "Фальшивые отношения";
                    case "IMPOSED_COUPLE": return "Навязанная пара";
                    case "MUSICAL": return "Мюзикл";
                    case "ANTIUTOPIAN_CITY_OF_FUTURE": return "Антиутопический город будущего";
                    case "DYSTOPIAN_FUTURISTIC_CITY": return "Антиутопический город будущего";
                    case "UTOPIAN_FUTURISTIC_CITY": return "Утопический город будущего";
                    case "MODERN_AMERICAN_CITY": return "Современный американский город";
                    case "MODERN_AMERICAN_TOWN": return "Современный американский городок";
                    case "MODERN_AMERICAN_COUNTRYSIDE": return "Современная американская глубинка";
                    case "MODERN_EUROPEAN_CITY": return "Современный европейский город";
                    case "MODERN_EUROPEAN_TOWN": return "Современный европейский городок";
                    case "MODERN_EUROPEAN_COUNTRYSIDE": return "Современная европейская глубинка";
                }
            }
            else
            {
                switch (id)
                {
                    case "SUPPORTINGCHARACTER_IMPOSED_COUPLE": return "Imposed Couple";
                    case "THEME_FAKE_RELATIONSHIP": return "Fake Relationship";
                    case "THEME_PUBLIC_LOVE_CONFESSION": return "Public Love Confession";
                    case "EVENTS_PUBLIC_LOVE_CONFESSION": return "Public Love Confession";
                    case "PUBLIC_LOVE_CONFESSION": return "Public Love Confession";
                    case "FAKE_RELATIONSHIP": return "Fake Relationship";
                    case "IMPOSED_COUPLE": return "Imposed Couple";
                    case "MUSICAL": return "Musical";
                    case "ANTIUTOPIAN_CITY_OF_FUTURE": return "Dystopian Futuristic City";
                }
            }

            return null;
        }

        private string LocalizedFallbackTagName(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return id;

            string manual = ManualTagTranslation(id);
            if (!string.IsNullOrWhiteSpace(manual)) return manual;

            if (isRussian)
            {
                switch (id)
                {
                    case "DRAMA": return "Драма";
                    case "COMEDY": return "Комедия";
                    case "ACTION": return "Боевик";
                    case "HISTORICAL": return "Исторический фильм";
                    case "ROMANCE": return "Романтика";
                    case "DETECTIVE": return "Детектив";
                    case "ADVENTURE": return "Приключения";
                    case "HORROR": return "Ужасы";
                    case "SCIENCE_FICTION": return "Научная фантастика";
                    case "THRILLER": return "Триллер";
                    case "SLAPSTICK_COMEDY": return "Эксцентрическая комедия";
                    case "MUSICAL": return "Мюзикл";

                    case "WILD_WEST": return "Дикий Запад";
                    case "MODERN_AMERICAN_CITY": return "Современный американский город";
                    case "MODERN_AMERICAN_TOWN": return "Современный американский городок";
                    case "MODERN_AMERICAN_COUNTRYSIDE": return "Современная американская глубинка";
                    case "MODERN_EUROPEAN_CITY": return "Современный европейский город";
                    case "MODERN_EUROPEAN_TOWN": return "Современный европейский городок";
                    case "MODERN_EUROPEAN_COUNTRYSIDE": return "Современная европейская глубинка";
                    case "MIDDLE_AGES": return "Средневековье";
                    case "ARTHURIAN_LEGENDS": return "Легенды о короле Артуре";
                    case "ANCIENT_GREECE": return "Древняя Греция";
                    case "ANCIENT_ROME": return "Древний Рим";
                    case "ANCIENT_EGYPT": return "Древний Египет";
                    case "ANCIENT_CHINA": return "Древний Китай";
                    case "FEUDAL_JAPAN": return "Феодальная Япония";
                    case "RENAISSANCE": return "Эпоха Возрождения";
                    case "FANTASY_KINGDOM": return "Фэнтезийное королевство";
                    case "SPACE": return "Космос";
                    case "UTOPIAN_FUTURISTIC_CITY": return "Утопический город будущего";
                    case "DYSTOPIAN_FUTURISTIC_CITY": return "Антиутопический город будущего";
                    case "TROPICAL_ISLAND": return "Тропический остров";
                    case "VICTORIAN_ENGLAND": return "Викторианская Англия";
                    case "AMERICAN_CIVIL_WAR": return "Гражданская война в США";
                    case "FREE_STATES_IN_SLAVERY-ERA": return "Свободные штаты эпохи рабства";
                    case "SLAVE_STATES_IN_SLAVERY-ERA": return "Рабовладельческие штаты";
                    case "CARIBBEAN": return "Карибы";
                    case "GREAT_WAR": return "Великая война";
                    case "WW2_EUROPE": return "Вторая мировая: Европа";
                    case "WW2_PACIFIC": return "Вторая мировая: Тихий океан";
                    case "WW2_AFRICA": return "Вторая мировая: Африка";
                }
            }
            else
            {
                switch (id)
                {
                    case "SCIENCE_FICTION": return "Science Fiction";
                    case "SLAPSTICK_COMEDY": return "Slapstick Comedy";
                    case "MUSICAL": return "Musical";
                    case "WILD_WEST": return "Wild West";
                    case "MODERN_AMERICAN_CITY": return "Modern American City";
                    case "MODERN_AMERICAN_TOWN": return "Modern American Town";
                    case "MODERN_AMERICAN_COUNTRYSIDE": return "Modern American Countryside";
                    case "MODERN_EUROPEAN_CITY": return "Modern European City";
                    case "MODERN_EUROPEAN_TOWN": return "Modern European Town";
                    case "MODERN_EUROPEAN_COUNTRYSIDE": return "Modern European Countryside";
                    case "MIDDLE_AGES": return "Middle Ages";
                    case "ARTHURIAN_LEGENDS": return "Arthurian Legends";
                    case "ANCIENT_GREECE": return "Ancient Greece";
                    case "ANCIENT_ROME": return "Ancient Rome";
                    case "ANCIENT_EGYPT": return "Ancient Egypt";
                    case "ANCIENT_CHINA": return "Ancient China";
                    case "FEUDAL_JAPAN": return "Feudal Japan";
                    case "FANTASY_KINGDOM": return "Fantasy Kingdom";
                    case "UTOPIAN_FUTURISTIC_CITY": return "Utopian Futuristic City";
                    case "DYSTOPIAN_FUTURISTIC_CITY": return "Dystopian Futuristic City";
                    case "TROPICAL_ISLAND": return "Tropical Island";
                    case "VICTORIAN_ENGLAND": return "Victorian England";
                    case "AMERICAN_CIVIL_WAR": return "American Civil War";
                    case "FREE_STATES_IN_SLAVERY-ERA": return "Free States in the Slavery Era";
                    case "SLAVE_STATES_IN_SLAVERY-ERA": return "Slave States in the Slavery Era";
                    case "GREAT_WAR": return "Great War";
                    case "WW2_EUROPE": return "World War II: Europe";
                    case "WW2_PACIFIC": return "World War II: Pacific";
                    case "WW2_AFRICA": return "World War II: Africa";
                }
            }

            return null;
        }

        private string GetLocString(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            JObject loc = isRussian ? locRus : locEng;
            if (loc == null || !loc.HasValues) return null;
            JObject idMap = loc["IdMap"] as JObject;
            JArray strings = loc["locStrings"] as JArray;
            if (idMap == null || strings == null) return null;
            JToken idxTok = idMap[id];
            if (idxTok == null) return null;
            int idx = idxTok.Value<int>();
            if (idx < 0 || idx >= strings.Count) return null;
            return Convert.ToString(strings[idx]);
        }

        private JObject LoadLocalizationJson(string lang)
        {
            string[] directNames = lang == "RUS"
                ? new[] { "Russian.json", "NON_EVENT_RUS.json", "NON_EVENT.ru.json", "NON_EVENT.json" }
                : new[] { "English.json", "NON_EVENT_ENG.json", "NON_EVENT.en.json", "NON_EVENT.json" };

            foreach (string name in directNames)
            {
                string path = FindDataFile(name);
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                try
                {
                    JObject obj = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
                    string packageId = Convert.ToString(obj["packageID"]);
                    string languageId = Convert.ToString(obj["languageID"]);

                    if (packageId.Equals("NON_EVENT", StringComparison.OrdinalIgnoreCase)
                        && languageId.Equals(lang, StringComparison.OrdinalIgnoreCase))
                        return obj;
                }
                catch { }
            }

            JObject fromZip = LoadLocalizationJsonFromZip(lang);
            if (fromZip != null) return fromZip;

            return null;
        }

        private JObject LoadLocalizationJsonFromZip(string lang)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidates = new List<string>();
                candidates.Add(Path.Combine(baseDir, "Resources", "Localization.zip"));
                candidates.Add(Path.Combine(baseDir, "Localization.zip"));

                DirectoryInfo di = new DirectoryInfo(baseDir);
                for (int i = 0; i < 5 && di != null; i++, di = di.Parent)
                {
                    candidates.Add(Path.Combine(di.FullName, "Resources", "Localization.zip"));
                    candidates.Add(Path.Combine(di.FullName, "Localization.zip"));
                }

                foreach (string zipPath in candidates.Distinct())
                {
                    if (!File.Exists(zipPath)) continue;

                    using (ZipArchive zip = ZipFile.OpenRead(zipPath))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            string name = entry.FullName.Replace('\\', '/');
                            if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                            if (name.IndexOf("NON_EVENT", StringComparison.OrdinalIgnoreCase) < 0
                                && name.IndexOf(lang == "RUS" ? "Russian" : "English", StringComparison.OrdinalIgnoreCase) < 0)
                                continue;

                            using (Stream stream = entry.Open())
                            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                            {
                                JObject obj = JObject.Parse(reader.ReadToEnd());
                                string packageId = Convert.ToString(obj["packageID"]);
                                string languageId = Convert.ToString(obj["languageID"]);
                                if (packageId.Equals("NON_EVENT", StringComparison.OrdinalIgnoreCase)
                                    && languageId.Equals(lang, StringComparison.OrdinalIgnoreCase))
                                    return obj;
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private string CleanLocString(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;

            s = s.Replace("<nobr>", string.Empty)
                 .Replace("</nobr>", string.Empty)
                 .Replace("<br>", " ")
                 .Replace("<br/>", " ")
                 .Replace("<br />", " ")
                 .Replace("\r", " ")
                 .Replace("\n", " ")
                 .Trim();

            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            return s;
        }

        private string Humanize(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return id;

            string value = id.Trim();
            string[] prefixes =
            {
                "PROTAGONIST_", "ANTAGONIST_", "SUPPORTINGCHARACTER_", "SUPPORTING_CHARACTER_",
                "THEME_", "EVENTS_", "EVENT_", "FINALE_"
            };

            foreach (string prefix in prefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(prefix.Length);
                    break;
                }
            }

            value = value.Replace("_", " ").Replace("-", " ").ToLowerInvariant();
            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;
            return ti.ToTitleCase(value);
        }

        private string NormalizeCategory(string cat, string id)
        {
            if (string.IsNullOrWhiteSpace(cat))
            {
                if (id.StartsWith("PROTAGONIST_")) return "Protagonist";
                if (id.StartsWith("ANTAGONIST_")) return "Antagonist";
                if (id.StartsWith("SUPPORTING")) return "Supporting Character";
                if (id.StartsWith("THEME_") || id.StartsWith("EVENT") || id.StartsWith("EVENTS_")) return "Theme & Event";
                if (id.StartsWith("FINALE_")) return "Finale";
                return "Setting";
            }
            cat = cat.Trim();
            if (cat.Equals("SupportingCharacter", StringComparison.OrdinalIgnoreCase)) return "Supporting Character";
            if (cat.Equals("Theme", StringComparison.OrdinalIgnoreCase) || cat.Equals("Event", StringComparison.OrdinalIgnoreCase)) return "Theme & Event";
            return cat;
        }

        private bool IsVisualTreeReadyForLocalization()
        {
            return TitleText != null
                && GeneratorTab != null
                && CompatibilityTab != null
                && AdvertisersTab != null
                && DistributionTab != null;
        }

        private void ApplyLocalization()
        {
            if (!IsVisualTreeReadyForLocalization())
                return;

            isRussian = IsRussianUi();

            Title = T("Hollywood Animal Calculator", "Калькулятор");
            TitleText.Text = T("🎬 Hollywood Animal Calculator", "🎬 Калькулятор Hollywood Animal");
            UpdateDataStatusText();
            GeneratedScript.AvgLabel = T("Avg Comp", "Совместимость");
            GeneratedScript.MovieScoreLabel = T("Movie Score", "Оценка фильма");
            GeneratedScript.ScriptQualityLabel = T("Script Qual", "Кач. сценария");
            GeneratedScript.TagsLabel = T("Tags", "Теги");
            GeneratorTab.Header = T("Script Generator", "Генератор сценариев");
            CompatibilityTab.Header = T("SE Compatibility", "Совместимость элементов");
            AdvertisersTab.Header = T("Best Advertisers", "Лучшие рекламщики");
            DistributionTab.Header = T("Distribution", "Прокат");

            GeneratorSettingsHeader.Text = T("Generator Settings", "Настройки генератора");
            TargetCompatibilityText.Text = T("Target Avg Comp", "Целевая совместимость");
            TargetScoreText.Text = T("Target Movie Score", "Целевая оценка фильма");
            TargetQualityText.Text = T("Target Script Qual", "Целевое кач. сценария");
            TargetTagCountText.Text = T("Tags count", "Кол-во тегов");
            StartingProfileButton.Content = T("Starting Tags", "Начальные теги");
            LockedHeader.Text = T("Required Elements", "Обязательные элементы");
            LockedHintText.Text = T("Select specific tags you MUST have in the script.", "Выберите конкретные теги, которые ОБЯЗАТЕЛЬНО должны быть в сценарии.");
            ExcludedHeader.Text = T("Excluded Elements", "Исключённые элементы");
            ExcludedHintText.Text = T("Select tags to BAN (e.g., due to \"The Code\"). The generator will never pick these.", "Выберите теги, которые нужно ЗАПРЕТИТЬ (например, из-за «Кодекса»). Генератор никогда их не выберет.");
            AddLockedButton.Content = AddExcludedButton.Content = AddSynergyButton.Content = AddAdvertiserTagButton.Content = T("Add", "Добавить");
            RemoveLockedButton.Content = RemoveExcludedButton.Content = RemoveSynergyButton.Content = RemoveAdvertiserTagButton.Content = T("Remove selected", "Удалить выбранное");
            ClearLockedButton.Content = ClearExcludedButton.Content = T("Clear", "Очистить всё");
            GenerateButton.Content = T("Generate Scripts", "Сгенерировать сценарии");
            PinScriptButton.Content = T("Pin selected", "Закрепить выбранное");
            SavePinnedButton.Content = T("Save pinned", "Сохранить закреплённые");
            LoadPinnedButton.Content = T("Load pinned", "Загрузить закреплённые");
            PinnedHeader.Text = T("Pinned Scripts", "Закреплённые сценарии");

            CheckCompatibilityHeader.Text = T("Check Compatibility", "Проверка совместимости");
            ClearSynergyButton.Content = ClearAdvertisersButton.Content = T("Clear", "Очистить");
            CheckCompatibilityButton.Content = T("Check Compatibility", "Проверить совместимость");
            TransferToAdvertisersButton.Content = T("Find Best Advertisers →", "Найти лучших рекламщиков →");
            SynergyResultsHeader.Text = T("Results", "Результаты");

            AdvertisersHeader.Text = T("Best Advertisers", "Лучшие рекламщики");
            CommercialScoreText.Text = T("Commercial", "Коммерция");
            ArtScoreText.Text = T("Artistic", "Художественность");
            AnalyzeAdvertisersButton.Content = T("Analyze", "Рассчитать");
            AudienceHeader.Text = T("Audience and Advertisers", "Аудитория и рекламщики");

            DistributionHeader.Text = T("Distribution Calculator", "Калькулятор проката");
            DistributionCommercialText.Text = T("Commercial score", "Коммерческая оценка");
            AvailableScreeningsText.Text = T("Available screenings", "Доступные сеансы");
            CalculateDistributionButton.Content = T("Calculate", "Рассчитать");

            UpdateLocalizedNames();
            InitializeAllSelectors();
            RefreshAllLists();
            if (DistributionGrid != null && DistributionCommercialBox != null && AvailableScreeningsBox != null)
                CalculateDistributionButton_Click(null, null);
            else
                RefreshDistributionCardsLocalization();
        }

        private void InitializeAllSelectors()
        {
            FillCategoryBox(GenCategoryBox);
            FillCategoryBox(ExCategoryBox);
            FillCategoryBox(SynCategoryBox);
            FillCategoryBox(AdvCategoryBox);
            RefillTagsForCategory(GenCategoryBox, GenTagBox);
            RefillTagsForCategory(ExCategoryBox, ExTagBox);
            RefillTagsForCategory(SynCategoryBox, SynTagBox);
            RefillTagsForCategory(AdvCategoryBox, AdvTagBox);
        }

        private void FillCategoryBox(ComboBox box)
        {
            if (box == null) return;
            string selected = box.SelectedItem as string;
            box.ItemsSource = categoryOrder.Where(c => tags.Values.Any(t => t.Category == c)).Select(CategoryDisplay).ToList();
            if (!string.IsNullOrWhiteSpace(selected) && box.Items.Contains(selected)) box.SelectedItem = selected;
            else if (box.Items.Count > 0) box.SelectedIndex = 0;
        }

        private string CategoryDisplay(string category)
        {
            if (!isRussian) return category;
            switch (category)
            {
                case "Genre": return "Жанр";
                case "Setting": return "Сеттинг";
                case "Protagonist": return "Протагонист";
                case "Antagonist": return "Антагонист";
                case "Supporting Character": return "Второстепенный персонаж";
                case "Theme & Event": return "Тема и событие";
                case "Finale": return "Финал";
                default: return category;
            }
        }

        private string CategoryFromDisplay(string display)
        {
            foreach (string c in categoryOrder) if (CategoryDisplay(c) == display) return c;
            return display;
        }

        private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == GenCategoryBox) RefillTagsForCategory(GenCategoryBox, GenTagBox);
            else if (sender == ExCategoryBox) RefillTagsForCategory(ExCategoryBox, ExTagBox);
            else if (sender == SynCategoryBox) RefillTagsForCategory(SynCategoryBox, SynTagBox);
            else if (sender == AdvCategoryBox) RefillTagsForCategory(AdvCategoryBox, AdvTagBox);
        }

        private void RefillTagsForCategory(ComboBox categoryBox, ComboBox tagBox)
        {
            if (categoryBox == null || tagBox == null || categoryBox.SelectedItem == null) return;
            string category = CategoryFromDisplay(Convert.ToString(categoryBox.SelectedItem));
            tagBox.ItemsSource = tags.Values.Where(t => t.Category == category).OrderBy(t => t.Name).ToList();
            tagBox.DisplayMemberPath = "Display";
            if (tagBox.Items.Count > 0) tagBox.SelectedIndex = 0;
        }

        private double ParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            double d;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d)) return d;
            return 0.0;
        }

        private int ParseInt(string s, int fallback)
        {
            int v;
            return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        private SelectedTag MakeSelected(TagInfo tag, double percent)
        {
            if (tag == null) return null;
            if (tag.Category != "Genre") percent = 1.0;
            if (percent > 1.0) percent /= 100.0;
            if (percent <= 0) percent = 1.0;
            return new SelectedTag { Id = tag.Id, Name = tag.Name, Category = tag.Category, Percent = percent };
        }

        private void AddLockedButton_Click(object sender, RoutedEventArgs e) { AddSelectedFromCombo(GenTagBox, lockedTags, 1.0); }
        private void AddExcludedButton_Click(object sender, RoutedEventArgs e) { AddSelectedFromCombo(ExTagBox, excludedTags, 1.0); }
        private void AddSynergyButton_Click(object sender, RoutedEventArgs e) { AddSelectedFromCombo(SynTagBox, synergyTags, 100); }
        private void AddAdvertiserTagButton_Click(object sender, RoutedEventArgs e) { AddSelectedFromCombo(AdvTagBox, advertiserTags, 100); }

        private void AddSelectedFromCombo(ComboBox combo, List<SelectedTag> list, double percent)
        {
            TagInfo tag = combo != null ? combo.SelectedItem as TagInfo : null;
            SelectedTag selected = MakeSelected(tag, percent);
            if (selected == null) return;
            if (list.Any(x => x.Id == selected.Id)) return;
            list.Add(selected);
            RefreshAllLists();
        }

        private void RemoveLockedButton_Click(object sender, RoutedEventArgs e) { RemoveSelected(LockedList, lockedTags); }
        private void RemoveExcludedButton_Click(object sender, RoutedEventArgs e) { RemoveSelected(ExcludedList, excludedTags); }
        private void RemoveSynergyButton_Click(object sender, RoutedEventArgs e) { RemoveSelected(SynergyList, synergyTags); }
        private void RemoveAdvertiserTagButton_Click(object sender, RoutedEventArgs e) { RemoveSelected(AdvertiserTagsList, advertiserTags); }
        private void ClearLockedButton_Click(object sender, RoutedEventArgs e) { lockedTags.Clear(); RefreshAllLists(); }
        private void ClearExcludedButton_Click(object sender, RoutedEventArgs e) { excludedTags.Clear(); RefreshAllLists(); }
        private void ClearSynergyButton_Click(object sender, RoutedEventArgs e) { synergyTags.Clear(); RefreshAllLists(); }
        private void ClearAdvertisersButton_Click(object sender, RoutedEventArgs e) { advertiserTags.Clear(); RefreshAllLists(); }

        private void RemoveSelected(ListBox box, List<SelectedTag> list)
        {
            SelectedTag item = box != null ? box.SelectedItem as SelectedTag : null;
            if (item != null) list.Remove(item);
            RefreshAllLists();
        }

        private void RefreshAllLists()
        {
            LockedList.ItemsSource = null; LockedList.ItemsSource = lockedTags;
            ExcludedList.ItemsSource = null; ExcludedList.ItemsSource = excludedTags;
            SynergyList.ItemsSource = null; SynergyList.ItemsSource = synergyTags;
            AdvertiserTagsList.ItemsSource = null; AdvertiserTagsList.ItemsSource = advertiserTags;
            GeneratedScriptsList.ItemsSource = null; GeneratedScriptsList.ItemsSource = generatedScripts;
            PinnedScriptsList.ItemsSource = null; PinnedScriptsList.ItemsSource = pinnedScripts;
        }

        private double PairRaw(string a, string b)
        {
            JToken v = compatibility[a] != null ? compatibility[a][b] : null;
            if (v == null) v = compatibility[b] != null ? compatibility[b][a] : null;
            return v != null ? ParseDouble(Convert.ToString(v)) : 3.0;
        }

        private MatrixResult CalculateMatrixScore(List<SelectedTag> selected)
        {
            var result = new MatrixResult();
            double rawSum = 0;
            int pairCount = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                for (int j = i + 1; j < selected.Count; j++)
                {
                    rawSum += PairRaw(selected[i].Id, selected[j].Id);
                    pairCount++;
                }
            }
            result.RawAverage = pairCount > 0 ? rawSum / pairCount : 3.0;

            double totalScore = 0;
            foreach (SelectedTag tagA in selected)
            {
                double rowSum = 0;
                double rowWeight = 0;
                double worstVal = 6.0;
                string worstPartner = null;
                foreach (SelectedTag tagB in selected)
                {
                    if (tagA.Id == tagB.Id) continue;
                    double rawVal = PairRaw(tagA.Id, tagB.Id);
                    double score = (rawVal - 3.0) / 2.0;
                    double weight = 1.0;
                    if (score < 0)
                    {
                        if (tagB.Category == "Genre") { score *= 20.0 * tagB.Percent; weight = 20.0 * tagB.Percent; }
                        else if (tagB.Category == "Setting") { score *= 5.0; weight = 5.0; }
                        else { score *= 3.0; weight = 3.0; }
                    }
                    else
                    {
                        if (tagB.Category == "Genre") { score *= tagB.Percent; weight = tagB.Percent; }
                    }
                    rowSum += score;
                    rowWeight += weight;
                    if (rawVal < worstVal) { worstVal = rawVal; worstPartner = tagB.Id; }
                }
                double rowAverage = rowWeight > 0 ? rowSum / rowWeight : 0;
                double transformedWorst = (worstVal - 3.0) / 2.0;
                double finalRowScore = rowAverage;
                if (worstVal <= 1.0)
                {
                    string partnerName = worstPartner != null && tags.ContainsKey(worstPartner) ? tags[worstPartner].Name : T("another selected tag", "другим выбранным элементом");
                    result.Conflicts.Add(tagA.Name + T(" conflicts with ", " конфликтует с ") + partnerName);
                    finalRowScore = -1.0;
                }
                else if (transformedWorst < rowAverage)
                {
                    finalRowScore = transformedWorst;
                }
                totalScore += finalRowScore * tagA.Percent;
            }
            result.TotalScore = totalScore >= 0 ? totalScore * 0.9 : totalScore * 1.25;
            return result;
        }

        private BonusResult CalculateBonuses(List<SelectedTag> selected)
        {
            var b = new BonusResult();
            BonusResult gp = CalculateGenrePairScore(selected);
            if (gp != null)
            {
                b.Art += gp.Art;
                b.Com += gp.Com;
            }
            else
            {
                SelectedTag topGenre = selected.Where(t => t.Category == "Genre").OrderByDescending(t => t.Percent).FirstOrDefault();
                if (topGenre != null && tags.ContainsKey(topGenre.Id))
                {
                    b.Art += tags[topGenre.Id].Art;
                    b.Com += tags[topGenre.Id].Commercial;
                }
            }
            foreach (SelectedTag s in selected)
            {
                if (s.Category == "Genre") continue;
                if (tags.ContainsKey(s.Id))
                {
                    b.Art += tags[s.Id].Art;
                    b.Com += tags[s.Id].Commercial;
                }
            }
            return b;
        }

        private BonusResult CalculateGenrePairScore(List<SelectedTag> selected)
        {
            var genres = selected.Where(t => t.Category == "Genre").OrderByDescending(t => t.Percent).ToList();
            if (genres.Count < 2) return null;
            SelectedTag g1 = genres[0];
            SelectedTag g2 = genres[1];
            if ((g1.Percent + g2.Percent < 0.7) || (g2.Percent < 0.35)) return null;
            JToken pair = genrePairs[g1.Id] != null ? genrePairs[g1.Id][g2.Id] : null;
            if (pair == null) pair = genrePairs[g2.Id] != null ? genrePairs[g2.Id][g1.Id] : null;
            if (pair == null) return null;
            return new BonusResult
            {
                Com = ParseDouble(Convert.ToString(pair["Item1"])),
                Art = ParseDouble(Convert.ToString(pair["Item2"]))
            };
        }

        private int ScoringElementCount(List<SelectedTag> selected)
        {
            return selected.Count(t => t.Category != "Genre" && t.Category != "Setting");
        }

        private int RequiredScoringElementsForTargets(int targetMovieScore, int targetScriptQuality)
        {
            int byScore = targetMovieScore >= 10 ? 9 : targetMovieScore >= 9 ? 8 : targetMovieScore >= 8 ? 7 : targetMovieScore >= 7 ? 5 : 4;
            int byQuality = targetScriptQuality >= 10 ? 9 : targetScriptQuality >= 9 ? 8 : targetScriptQuality >= 8 ? 7 : targetScriptQuality >= 7 ? 6 : targetScriptQuality >= 6 ? 5 : 4;
            return Math.Max(byScore, byQuality);
        }

        private int ScriptQualityFromScoringElements(int scoringElements)
        {
            if (scoringElements >= 9) return 10;
            if (scoringElements >= 8) return 9;
            if (scoringElements >= 7) return 8;
            if (scoringElements >= 6) return 7;
            if (scoringElements >= 5) return 6;
            return 5;
        }

        private int MovieScoreCapFromScoringElements(int scoringElements)
        {
            if (scoringElements >= 9) return 10;
            if (scoringElements >= 8) return 9;
            if (scoringElements >= 7) return 8;
            if (scoringElements >= 5) return 7;
            return 6;
        }

        private double CalculateMovieScore(List<SelectedTag> selected, MatrixResult matrix, BonusResult bonuses)
        {
            int cap = MovieScoreCapFromScoringElements(ScoringElementCount(selected));
            const double maxGameScore = 9.9;
            double com = Math.Max(0, (matrix.TotalScore + bonuses.Com) * maxGameScore);
            double art = Math.Max(0, (matrix.TotalScore + bonuses.Art) * maxGameScore);
            return Math.Min(cap, Math.Max(com, art));
        }

        private void CheckCompatibilityButton_Click(object sender, RoutedEventArgs e)
        {
            if (synergyTags.Count == 0)
            {
                MessageBox.Show(T("Please select at least one tag.", "Выберите хотя бы один элемент."));
                return;
            }
            MatrixResult m = CalculateMatrixScore(synergyTags);
            BonusResult b = CalculateBonuses(synergyTags);
            int ngCount = ScoringElementCount(synergyTags);
            int tagCap = MovieScoreCapFromScoringElements(ngCount);
            const double maxGameScore = 9.9;
            double com = Math.Min(tagCap, Math.Max(0, (m.TotalScore + b.Com) * maxGameScore));
            double art = Math.Min(tagCap, Math.Max(0, (m.TotalScore + b.Art) * maxGameScore));
            SynergySummaryText.Text =
                T("Average compatibility: ", "Средняя совместимость: ") + m.RawAverage.ToString("0.0", CultureInfo.InvariantCulture) + " / 5.0\n" +
                T("Script synergy: ", "Синергия сценария: ") + FormatScore(m.TotalScore) + "\n" +
                T("Commercial bonus: ", "Коммерческий бонус: ") + FormatScore(b.Com) + "    " + T("Artistic bonus: ", "Художественный бонус: ") + FormatScore(b.Art) + "\n" +
                T("Potential commercial score: ", "Потенциальная коммерческая оценка: ") + com.ToString("0.0", CultureInfo.InvariantCulture) + "\n" +
                T("Potential artistic score: ", "Потенциальная художественная оценка: ") + art.ToString("0.0", CultureInfo.InvariantCulture) + "\n" +
                T("Score cap: ", "Потолок оценки: ") + tagCap + ".0";
            SynergyConflictsText.Text = m.Conflicts.Count == 0 ? T("No severe conflicts found.", "Серьёзных конфликтов не найдено.") : string.Join("\n", m.Conflicts.Distinct());
        }

        private string FormatScore(double v)
        {
            if (Math.Abs(v) < 0.005) return "0";
            return (v > 0 ? "+" : "") + v.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void TransferToAdvertisersButton_Click(object sender, RoutedEventArgs e)
        {
            advertiserTags.Clear();
            foreach (SelectedTag s in synergyTags) advertiserTags.Add(s.Clone());
            RefreshAllLists();
            Tabs.SelectedItem = AdvertisersTab;
            AnalyzeAdvertisersButton_Click(sender, e);
        }

        private void AnalyzeAdvertisersButton_Click(object sender, RoutedEventArgs e)
        {
            if (advertiserTags.Count == 0)
            {
                MessageBox.Show(T("Please select at least one tag.", "Выберите хотя бы один элемент."));
                return;
            }
            double commercial = ParseDouble(CommercialScoreBox.Text);
            double artistic = ParseDouble(ArtScoreBox.Text);
            Dictionary<string, double> audience = CalculateAudience(advertiserTags);
            var names = isRussian ? demoNamesRus : demoNamesEng;
            var sb = new StringBuilder();
            sb.AppendLine(T("Audience profile:", "Профиль аудитории:"));
            foreach (var p in audience.OrderByDescending(x => x.Value))
            {
                sb.Append(names[p.Key]).Append(": ").Append((p.Value * 100).ToString("0.0", CultureInfo.InvariantCulture)).AppendLine("%");
            }
            AudienceSummaryText.Text = sb.ToString();

            var ranked = new List<string>();
            foreach (AdAgent a in adAgents)
            {
                double coverage = a.Targets.Sum(t => audience.ContainsKey(t) ? audience[t] : 0);
                double scoreType = a.Type == 1 ? artistic : a.Type == 2 ? (commercial + artistic) / 2.0 : commercial;
                double final = coverage * (1.0 + a.Level * 0.15) * Math.Max(1, scoreType);
                ranked.Add(a.Name + " — " + T("score", "оценка") + ": " + final.ToString("0.00", CultureInfo.InvariantCulture) + "   " + T("coverage", "охват") + ": " + (coverage * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%");
            }
            AdvertisersResultList.ItemsSource = ranked.OrderByDescending(ParseTrailingScore).ToList();
        }

        private double ParseTrailingScore(string s)
        {
            int idx = s.IndexOf(":", StringComparison.Ordinal);
            if (idx < 0) return 0;
            string rest = s.Substring(idx + 1).Trim();
            string num = rest.Split(' ')[0];
            return ParseDouble(num);
        }

        private Dictionary<string, double> CalculateAudience(List<SelectedTag> selected)
        {
            var aff = demoNamesEng.Keys.ToDictionary(k => k, k => 0.0);
            foreach (SelectedTag item in selected)
            {
                if (!tags.ContainsKey(item.Id)) continue;
                foreach (string d in demoNamesEng.Keys)
                {
                    aff[d] += tags[item.Id].Weights.ContainsKey(d) ? tags[item.Id].Weights[d] * item.Percent : 0.0;
                }
            }
            double min = aff.Values.Min();
            if (min < 1.0)
            {
                double lift = 1.0 - min;
                foreach (string d in demoNamesEng.Keys.ToList()) aff[d] += lift;
            }
            double sum = aff.Values.Sum();
            if (sum <= 0) return demoNamesEng.Keys.ToDictionary(k => k, k => 0.0);
            return aff.ToDictionary(x => x.Key, x => Math.Min(1.0, Math.Max(0, (x.Value / sum) * 3.0)));
        }

        private void RefreshDistributionCardsLocalization()
        {
            if (DistributionGrid == null || DistributionGrid.Children.Count == 0)
                return;

            for (int i = 0; i < DistributionGrid.Children.Count; i++)
            {
                Border border = DistributionGrid.Children[i] as Border;
                StackPanel panel = border != null ? border.Child as StackPanel : null;
                if (panel != null && panel.Children.Count > 0)
                {
                    TextBlock weekText = panel.Children[0] as TextBlock;
                    if (weekText != null)
                        weekText.Text = WeekCaption(i + 1);
                }
            }
        }

        private void CalculateDistributionButton_Click(object sender, RoutedEventArgs e)
        {
            double commercial = ParseDouble(DistributionCommercialBox.Text);
            double screenings = ParseDouble(AvailableScreeningsBox.Text);
            double w1 = Math.Max(0, commercial * 2 * 1000 - screenings);
            double w2 = Math.Max(0, commercial * 1 * 1000 - screenings);
            var values = new List<double> { w1, w2 };
            double current = w2;
            for (int i = 2; i < 8; i++)
            {
                current *= 0.8;
                values.Add(current);
            }
            DistributionGrid.Children.Clear();
            for (int i = 0; i < values.Count; i++)
            {
                double val = i < 4 ? Math.Ceiling(values[i]) : Math.Floor(values[i]);
                Border b = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(69, 22, 22)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(199, 66, 66)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = WeekCaption(i + 1), Foreground = Brushes.White, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = val.ToString("N0", CultureInfo.InvariantCulture), Foreground = val > 0 ? Brushes.LightGreen : Brushes.LightGray, FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center });
                b.Child = sp;
                DistributionGrid.Children.Add(b);
            }
        }

        private void CustomProfileButton_Click(object sender, RoutedEventArgs e)
        {
            excludedTags.Clear();
            RefreshAllLists();
        }

        private void StartingProfileButton_Click(object sender, RoutedEventArgs e)
        {
            lockedTags.Clear();
            excludedTags.Clear();

            foreach (string id in starterWhitelist)
            {
                if (tags.TryGetValue(id, out TagInfo tag) && !lockedTags.Any(x => x.Id == id))
                    lockedTags.Add(MakeSelected(tag, tag.Category == "Genre" ? 1.0 : 1.0));
            }

            NormalizeGenrePercents(lockedTags);
            RefreshAllLists();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            generatedScripts.Clear();
            double targetComp = ClampTargetCompatibility(ParseDouble(GenTargetCompatibilityBox.Text));
            GenTargetCompatibilityBox.Text = targetComp.ToString("0.0", CultureInfo.CurrentCulture);
            int targetScore = Math.Max(0, Math.Min(10, ParseInt(GenTargetScoreBox.Text, 6)));
            int targetQuality = Math.Max(0, Math.Min(10, ParseInt(GenTargetQualityBox.Text, 6)));
            int targetTagCount = Math.Max(5, Math.Min(10, ParseInt(GenTargetTagCountBox.Text, 7)));
            GenTargetScoreBox.Text = targetScore.ToString(CultureInfo.CurrentCulture);
            GenTargetQualityBox.Text = targetQuality.ToString(CultureInfo.CurrentCulture);
            GenTargetTagCountBox.Text = targetTagCount.ToString(CultureInfo.CurrentCulture);
            int targetCount = RequiredScoringElementsForTargets(targetScore, targetQuality);
            HashSet<string> excluded = new HashSet<string>(excludedTags.Select(t => t.Id));

            for (int attemptBatch = 0; attemptBatch < 10; attemptBatch++)
            {
                GeneratedScript best = null;
                for (int attempt = 0; attempt < 3000; attempt++)
                {
                    var scriptTags = BuildRandomScript(targetCount, targetTagCount, lockedTags, excluded);
                    if (scriptTags.Count == 0) continue;
                    MatrixResult m = CalculateMatrixScore(scriptTags);
                    BonusResult b = CalculateBonuses(scriptTags);
                    int scriptQuality = ScriptQualityFromScoringElements(ScoringElementCount(scriptTags));
                    double movieScore = CalculateMovieScore(scriptTags, m, b);
                    double compMiss = Math.Max(0, targetComp - m.RawAverage);
                    double movieMiss = Math.Max(0, targetScore - movieScore);
                    double qualityMiss = Math.Max(0, targetQuality - scriptQuality);
                    double score = 1000.0 - compMiss * 180.0 - movieMiss * 120.0 - qualityMiss * 80.0
                                   + m.RawAverage * 10.0 + movieScore * 8.0 + scriptQuality * 4.0 + Math.Max(0, m.TotalScore) * 20.0;
                    var gs = new GeneratedScript
                    {
                        Name = T("Generated Script", "Сгенерированный сценарий") + " " + (generatedScripts.Count + 1),
                        Tags = scriptTags,
                        AverageCompatibility = m.RawAverage,
                        Synergy = m.TotalScore,
                        MovieScore = movieScore,
                        ScriptQuality = scriptQuality,
                        CommercialBonus = b.Com,
                        ArtisticBonus = b.Art,
                        InternalScore = score
                    };
                    if (best == null || gs.InternalScore > best.InternalScore) best = gs;
                    if (m.RawAverage >= targetComp && movieScore >= targetScore && scriptQuality >= targetQuality) break;
                }
                if (best != null) generatedScripts.Add(best);
            }
            RefreshAllLists();
        }

        private List<SelectedTag> BuildRandomScript(int targetScoringCount, int targetTagCount, List<SelectedTag> fixedTags, HashSet<string> excluded)
        {
            targetTagCount = Math.Max(5, Math.Min(10, targetTagCount));

            var result = fixedTags.Select(t => t.Clone()).ToList();
            HashSet<string> used = new HashSet<string>(result.Select(t => t.Id));

            // Минимальный каркас сценария = 5 тегов. 
            string[] requiredBase = { "Genre", "Setting", "Protagonist", "Antagonist", "Finale" };
            foreach (string cat in requiredBase)
            {
                if (result.Count >= targetTagCount) break;
                if (result.Any(t => t.Category == cat)) continue;
                SelectedTag r = RandomTag(cat, used, excluded);
                if (r != null) { result.Add(r); used.Add(r.Id); }
            }

            string[] optionalPriority = { "Supporting Character", "Theme & Event" };
            foreach (string cat in optionalPriority)
            {
                if (result.Count >= targetTagCount) break;
                if (result.Any(t => t.Category == cat)) continue;
                SelectedTag r = RandomTag(cat, used, excluded);
                if (r != null) { result.Add(r); used.Add(r.Id); }
            }

            while (result.Count < targetTagCount && ScoringElementCount(result) < targetScoringCount)
            {
                string cat = categoryOrder[rng.Next(categoryOrder.Count)];
                SelectedTag r = RandomTag(cat, used, excluded);
                if (r == null) break;
                result.Add(r); used.Add(r.Id);
            }

            while (result.Count < targetTagCount)
            {
                string cat = categoryOrder[rng.Next(categoryOrder.Count)];
                SelectedTag r = RandomTag(cat, used, excluded);
                if (r == null) break;
                result.Add(r); used.Add(r.Id);
            }

            NormalizeGenrePercents(result);
            return result;
        }

        private SelectedTag RandomTag(string category, HashSet<string> used, HashSet<string> excluded)
        {
            var list = tags.Values.Where(t => t.Category == category && !used.Contains(t.Id) && !excluded.Contains(t.Id)).ToList();
            if (list.Count == 0) return null;
            TagInfo tag = list[rng.Next(list.Count)];
            return MakeSelected(tag, category == "Genre" ? 1.0 : 1.0);
        }

        private void NormalizeGenrePercents(List<SelectedTag> result)
        {
            var genres = result.Where(t => t.Category == "Genre").ToList();
            if (genres.Count == 0) return;
            double each = 1.0 / genres.Count;
            foreach (SelectedTag g in genres) g.Percent = each;
        }

        private void PinScriptButton_Click(object sender, RoutedEventArgs e)
        {
            GeneratedScript item = GeneratedScriptsList.SelectedItem as GeneratedScript;
            if (item == null) return;
            pinnedScripts.Add(item.Clone());
            RefreshAllLists();
        }

        private void SavePinnedButton_Click(object sender, RoutedEventArgs e)
        {
            if (pinnedScripts.Count == 0)
            {
                MessageBox.Show(T("No pinned scripts to save.", "Нет закреплённых сценариев для сохранения."));
                return;
            }
            var dlg = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "hollywood_animal_scripts.json" };
            if (dlg.ShowDialog(this) != true) return;
            File.WriteAllText(dlg.FileName, JsonConvert.SerializeObject(pinnedScripts, Formatting.Indented));
        }

        private void LoadPinnedButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
            if (dlg.ShowDialog(this) != true) return;
            var loaded = JsonConvert.DeserializeObject<List<GeneratedScript>>(File.ReadAllText(dlg.FileName));
            pinnedScripts.Clear();
            if (loaded != null) pinnedScripts.AddRange(loaded);
            RefreshAllLists();
        }


        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            DependencyObject src = e.OriginalSource as DependencyObject;

            while (src != null)
            {
                if (src is ComboBox || src is ComboBoxItem)
                    return;

                src = VisualTreeHelper.GetParent(src);
            }

            var scroll = GetActiveScrollViewer();
            if (scroll == null) return;

            scroll.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private ScrollViewer GetActiveScrollViewer()
        {
            return FindChild<ScrollViewer>(this);
        }

        private T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                    return typedChild;

                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private ScrollViewer GetActivePageScrollViewer()
        {
            if (Tabs == null) return null;
            if (Tabs.SelectedItem == GeneratorTab) return GeneratorScroll;
            if (Tabs.SelectedItem == CompatibilityTab) return CompatibilityScroll;
            if (Tabs.SelectedItem == AdvertisersTab) return AdvertisersScroll;
            if (Tabs.SelectedItem == DistributionTab) return DistributionScroll;
            return null;
        }

        private double ClampTargetCompatibility(double value)
        {
            if (value < 1.0) return 1.0;
            if (value > 5.0) return 5.0;
            return value;
        }

        private void GenTargetCompatibilityBox_LostFocus(object sender, RoutedEventArgs e)
        {
            double value = ClampTargetCompatibility(ParseDouble(GenTargetCompatibilityBox.Text));
            GenTargetCompatibilityBox.Text = value.ToString("0.0", CultureInfo.CurrentCulture);
        }


        private void GenTargetScoreBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int value = Math.Max(0, Math.Min(10, ParseInt(GenTargetScoreBox.Text, 6)));
            GenTargetScoreBox.Text = value.ToString(CultureInfo.CurrentCulture);
        }

        private void GenTargetQualityBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int value = Math.Max(0, Math.Min(10, ParseInt(GenTargetQualityBox.Text, 6)));
            GenTargetQualityBox.Text = value.ToString(CultureInfo.CurrentCulture);
        }

        private void GenTargetTagCountBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int value = Math.Max(5, Math.Min(10, ParseInt(GenTargetTagCountBox.Text, 7)));
            GenTargetTagCountBox.Text = value.ToString(CultureInfo.CurrentCulture);
        }

        private void ScrollableControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleMouseWheelScroll(sender, e);
        }

        private static void GlobalScrollableControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleMouseWheelScroll(sender, e);
        }

        private static void HandleMouseWheelScroll(object sender, MouseWheelEventArgs e)
        {
            if (e == null) return;

            DependencyObject source = e.OriginalSource as DependencyObject;
            DependencyObject senderObject = sender as DependencyObject;

            ScrollViewer viewer = FindVisualParent<ScrollViewer>(source)
                                  ?? senderObject as ScrollViewer
                                  ?? FindVisualChild<ScrollViewer>(senderObject);

            if (viewer == null || viewer.ScrollableHeight <= 0)
            {
                e.Handled = false;
                return;
            }

            double offset = viewer.VerticalOffset - e.Delta;
            if (offset < 0) offset = 0;
            if (offset > viewer.ScrollableHeight) offset = viewer.ScrollableHeight;
            viewer.ScrollToVerticalOffset(offset);
            e.Handled = true;
        }

        private static TParent FindVisualParent<TParent>(DependencyObject child) where TParent : DependencyObject
        {
            while (child != null)
            {
                TParent result = child as TParent;
                if (result != null) return result;
                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private static TChild FindVisualChild<TChild>(DependencyObject parent) where TChild : DependencyObject
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                TChild result = child as TChild;
                if (result != null) return result;

                result = FindVisualChild<TChild>(child);
                if (result != null) return result;
            }

            return null;
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageBox == null) return;

            ComboBoxItem item = LanguageBox.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string tag = Convert.ToString(item.Tag);
            string content = Convert.ToString(item.Content);
            isRussian = string.Equals(tag, "RUS", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(tag, "RU", StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(content) && content.IndexOf("Рус", StringComparison.OrdinalIgnoreCase) >= 0);

            // Во время загрузки XAML событие может сработать раньше, чем созданы TabItem

            if (!IsVisualTreeReadyForLocalization())
                return;

            ApplyLocalization();
            InitializeAllSelectors();
            RefreshAllLists();

            // Важно: карточки проката пересоздаются, иначе после переключения языка
            // подписи Week/Неделя могут остаться от предыдущей локали.

            if (DistributionGrid != null && DistributionCommercialBox != null && AvailableScreeningsBox != null)
                CalculateDistributionButton_Click(null, null);
        }

    }

    public class TagInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Display { get; set; }
        public string Category { get; set; }
        public double Art { get; set; }
        public double Commercial { get; set; }
        public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>();
    }

    public class SelectedTag
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Percent { get; set; }
        public string Display
        {
            get
            {
                return Category == "Genre" ? Name + " (" + (Percent * 100).ToString("0", CultureInfo.InvariantCulture) + "%)" : Name;
            }
        }
        public SelectedTag Clone()
        {
            return new SelectedTag { Id = Id, Name = Name, Category = Category, Percent = Percent };
        }
    }

    public class MatrixResult
    {
        public double RawAverage { get; set; }
        public double TotalScore { get; set; }
        public List<string> Conflicts { get; set; } = new List<string>();
    }

    public class BonusResult
    {
        public double Art { get; set; }
        public double Com { get; set; }
    }

    public class GeneratedScript
    {
        public static string AvgLabel { get; set; } = "Avg Comp";
        public static string MovieScoreLabel { get; set; } = "Movie Score";
        public static string ScriptQualityLabel { get; set; } = "Script Qual";
        public static string SynLabel { get; set; } = "Syn";
        public static string TagsLabel { get; set; } = "Tags";

        public string Name { get; set; }
        public List<SelectedTag> Tags { get; set; } = new List<SelectedTag>();
        public double AverageCompatibility { get; set; }
        public double Synergy { get; set; }
        public double MovieScore { get; set; }
        public int ScriptQuality { get; set; }
        public double CommercialBonus { get; set; }
        public double ArtisticBonus { get; set; }
        [JsonIgnore]
        public double InternalScore { get; set; }
        public string Summary
        {
            get
            {
                return Name
                    + " | " + AvgLabel + ": " + AverageCompatibility.ToString("0.0", CultureInfo.InvariantCulture)
                    + " | " + MovieScoreLabel + ": " + MovieScore.ToString("0.0", CultureInfo.InvariantCulture)
                    + " | " + ScriptQualityLabel + ": " + ScriptQuality.ToString(CultureInfo.InvariantCulture)
                    + " | " + TagsLabel + ": " + string.Join(", ", Tags.Select(t => t.Name));
            }
        }
        public override string ToString() { return Summary; }
        public GeneratedScript Clone()
        {
            return new GeneratedScript
            {
                Name = Name,
                Tags = Tags.Select(t => t.Clone()).ToList(),
                AverageCompatibility = AverageCompatibility,
                Synergy = Synergy,
                MovieScore = MovieScore,
                ScriptQuality = ScriptQuality,
                CommercialBonus = CommercialBonus,
                ArtisticBonus = ArtisticBonus,
                InternalScore = InternalScore
            };
        }
    }

    public class AdAgent
    {
        public string Name { get; set; }
        public string[] Targets { get; set; }
        public int Type { get; set; }
        public int Level { get; set; }
    }
}
