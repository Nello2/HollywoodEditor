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
using System.Threading.Tasks;
using System.Windows;

namespace HollywoodEditor.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainModel
    {
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
        // Под 0.8.68EA
        private ObservableCollection<Character> filtered_Obj;
        private bool showOnlyTalent = false;
        private bool showOnlyDead = false;
        private bool showWithDead = true;

        public static Dictionary<string, string> LocaleNames { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> LocaleTranslator { get; set; } = new Dictionary<string, string>();
        public static string MyStudio { get; set; }
        public stateJson Info { get; set; }
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

        //распаковщик
        public MainModel()
        {
            Filter_txt = "";
            Filter_studio = "";
            Filter_Prof = "";
            StatusBarText = "Prepared to unzip";
            Filtered_Obj = new ObservableCollection<Character>();
            UnzipResources();
            StatusBarText = "Done";
        }
        public void SetSearched()
        {
            if (Info == null) return;
            if (Info.characters == null) return;
            IEnumerable<Character> q = Info.characters;
            if (Filter_studio != "All")
            {
                q = q.Where(t => t.studioId == Filter_studio);
            }
            if (Filter_Prof != "All")
            {
                q = q.Where(t => t.professions.ProfToDecode == Filter_Prof);
            }
            if (!string.IsNullOrWhiteSpace(Filter_txt))
            {
                q = q.Where(t => t.MyCustomName.Contains(Filter_txt));
            }
            if (ShowOnlyTalent)
                q = q.Where(t => t.professions.IsTalent);
            if (ShowOnlyDead)
                q = q.Where(t => t.IsDead);
            if (!ShowWithDead)
                q = q.Where(t => !t.IsDead);

            q = q.OrderBy(t => t.professions.ProfToDecode);
            StatusBarText = $"Filtered {q.Count()} chars";
            Filtered_Obj = new ObservableCollection<Character>(q);
        }
        // UnzipResources полностью переписан, так как изначально задумывалось, что локали будут использоваться напрямую через .yz,
        // что соответственное создавало своеобразные сложности. 
        // Пришлось полностью менять логику на распаковку .zip файлов.
        // Улучшена совместимость для 0.8.55EA
        // Вдобавок пришлоось отказаться от иконок персонажей, так как занимало очень много место.
        // Update 13.03.2026: Был полностью удален код, который позволял редактору распаковывать изображения персонажей, что пришлось это отдельно переносить в SE;
        // Это было сделано для хорошей оптимизации, потому что формы из-за этого зависали/заедали.
        public async void UnzipResources()
        {
            try
            {
                await Task.Run(() =>
                {
                    string mi = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                    string local_dir = Path.Combine(mi, "Localization");

                    bool arch_loc_exist = File.Exists(Path.Combine(mi, "Localization.zip"));


                    if (arch_loc_exist)
                    {
                        if (!Directory.Exists(local_dir))
                        {
                            StatusBarText = "Start extracting Localization";
                            ExtractZipFile(Path.Combine(mi, "Localization.zip"), local_dir);
                            StatusBarText = "End extracting Localization";
                        }
                        StatusBarText = "Set Localization";
                        SetLocale(Path.Combine(mi, "Localization", "RUS"));
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
                                SetLocale(selectedPath);
                            }
                        }
                        else
                        {
                            var ofdd = new OpenFileDialog();
                            ofdd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low\\Weappy\\Hollywood Animal\\Saves\\Profiles";
                            ofdd.Multiselect = false;
                            ofdd.Title = "Select save file";
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
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                },
                (obj) => true));
            }
        }

        // В дальнейшем надо это проверить, потому что могут возникнуть ошибки при новой версии!
        public CommandHandler SaveCmd
        {
            get
            {
                return _savefile ?? (_savefile = new CommandHandler(async obj =>
                {
                    Save_done = false;
                    bool t = await WriteChange();
                    Save_done = true;
                },
                (obj) => true));
            }
        }
        public CommandHandler AddTraitCmd
        {
            get
            {
                return _addtrait ?? (_addtrait = new CommandHandler(obj =>
                {
                    SelectedChar.labels.Insert(0, (string)obj);
                    SelectedChar.SetAvTraits();
                }, (obj) => !string.IsNullOrEmpty((string)obj)));
            }
        }
        public CommandHandler MoveTraitUpCmd
        {
            get
            {
                return _removeTraitUp ?? (_removeTraitUp = new CommandHandler(obj =>
                {
                    var ind = SelectedChar.labels.IndexOf((string)obj);
                    SelectedChar.labels.Move(ind, ind - 1);
                }, (obj) => !string.IsNullOrEmpty((string)obj) && SelectedChar != null && SelectedChar.labels.IndexOf((string)obj) > 0));
            }
        }
        public CommandHandler RemoveTraitCmd
        {
            get
            {
                return _removetrait ?? (_removetrait = new CommandHandler(obj =>
                {
                    SelectedChar.labels.Remove((string)obj);
                    SelectedChar.SetAvTraits();
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
        #endregion
        #region locale
        private static readonly object _localeLock = new object();

        private async void SetLocale(string path)
        {
            try
            {
                lock (_localeLock)
                {
                    // Предотвращаем повторную загрузку
                    if (LocaleNames.Count > 0 && LocaleTranslator.Count > 0)
                        return;
                }

                await LoadNamesFromJson(path);
                await LoadLocaleFromJson(path);
                StatusBarText = "Loaded jsons";
                RefershLocale();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        void RefershLocale()
        {
            StatusBarText = "Refresh loacales";
            if (Info != null && Info.characters != null
                && LocaleNames != null && LocaleNames.Count > 0)
                foreach (var t in Info.characters)
                {
                    t.normalLast = LocaleNames[t.lastNameId];
                    t.normalFirst = LocaleNames[t.firstNameId];
                }

            ProfList = ProfList;
            ProfListWithNoTallent = ProfListWithNoTallent;
            ProfListWithOutNoTallent = ProfListWithOutNoTallent;
            StudioList = StudioList;
            StudioListForChar = StudioList != null ? StudioList.Where(t => t != "All").ToList() : new List<string>();
            SelectedChar = null;
            SelectedChar = Filtered_Obj != null && Filtered_Obj.Count > 0 ? Filtered_Obj[0] : null;
            SetSearched();
            StatusBarText = "Refresh loacales done";
        }
        public async Task LoadLocaleFromJson(string path)
        {
            try
            {
                path += "\\NON_EVENT.json";
                string json = await Task.Run(() => File.ReadAllText(path));
                var map = JObject.Parse(json).SelectToken("IdMap");
                var local = JObject.Parse(json).SelectToken("locStrings");
                List<string> getout = JsonConvert.DeserializeObject<List<string>>(local.ToString());

                var newDict = new Dictionary<string, string>();
                foreach (var item in map.Children<JProperty>())
                {
                    string key = item.Name;
                    string value = getout[item.Value.ToObject<int>()];

                    // Проверяем, нет ли уже такого ключа
                    if (!newDict.ContainsKey(key))
                    {
                        newDict.Add(key, value);
                    }
                    else
                    {
                        // Логируем или игнорируем дубликат
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
                path += "\\CHARACTER_NAMES.json";
                string json = await Task.Run(() => File.ReadAllText(path));
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
        public async Task ParseJson(string path)
        {
            try
            {
                StatusBarText = "Start parsing save...";
                string jsonstr = await Task.Run(() => File.ReadAllText(path));
                using (var str_reader = new StringReader(jsonstr))
                {
                    using (var reader = new JsonTextReader(str_reader))
                    {
                        reader.FloatParseHandling = FloatParseHandling.Decimal;
                        jobj = JObject.Load(reader);
                    }
                }
                StatusBarText = "Json save readed... Start parsing";
                var aa = jobj.SelectToken("stateJson");
                Info = new stateJson();
                Info.budget = aa.SelectToken("budget")?.Value<int>() ?? 0;
                Info.cash = aa.SelectToken("cash")?.Value<int>() ?? 0;
                Info.reputation = aa.SelectToken("reputation")?.Value<double>() ?? 0;
                Info.influence = aa.SelectToken("influence")?.Value<int>() ?? 0;
                Info.studioName = aa.SelectToken("studioName")?.Value<string>();
                Info.timePassed = aa.SelectToken("timePassed")?.Value<string>();

                StatusBarText = "Loading milestones...";
                List<Milestones> mm = new List<Milestones>();
                var milestonesToken = aa.SelectToken("milestones");
                if (milestonesToken != null)
                {
                    foreach (var item in milestonesToken.Children())
                    {
                        var q = item.ToObject<JProperty>();
                        Milestones nm = JsonConvert.DeserializeObject<Milestones>(q.Value.ToString());
                        if (!nm.id.Contains("POLICY_ENABLE_") && nm.id.Contains("POLICY_"))
                            mm.Add(nm);
                    }
                }
                Info.milestones = new ObservableCollection<Milestones>(mm);

                StatusBarText = "Loading next gen timers...";
                Dictionary<string, DateTime> dt_d = new Dictionary<string, DateTime>();
                var sp_d = aa.SelectToken("nextGenCharacterTimers")?.Children();
                if (sp_d != null)
                {
                    foreach (var item in sp_d)
                    {
                        foreach (var prof in item.Children())
                        {
                            foreach (var prop in prof?.ToObject<JObject>().Properties())
                            {
                                dt_d.Add($"PROFESSION_{prop.Name.ToUpper()}", prop.Value.ToObject<DateTime>());
                            }
                        }
                    }
                }
                Info.NextSpawnDays = new Dictionary<string, DateTime>(dt_d);

                StatusBarText = "Loading opened perks...";
                List<string> op_d = new List<string>();
                var op_p = aa.SelectToken("openedPerks")?.Children();
                if (op_p != null)
                {
                    foreach (var item in op_p)
                    {
                        op_d.Add(item?.Value<string>());
                    }
                }
                Info.openedPerks = new ObservableCollection<string>(op_d);
                Info.AvailablePerks = new ObservableCollection<string>(stateJson.PreGenPerks.Except(Info.openedPerks).ToList());

                StatusBarText = "Loading closed tags...";
                op_d = new List<string>();
                op_p = aa.SelectToken("tagBank")?.Children();
                if (op_p != null)
                {
                    foreach (var item in op_p)
                    {
                        op_d.Add(item?.Value<string>());
                    }
                }
                Info.tagBank = new ObservableCollection<string>(op_d);

                StatusBarText = "Loading opened tags...";
                var tagsToken = aa.SelectToken("tagPool");
                ObservableCollection<TagPool> tags = tagsToken != null ?
                    JsonConvert.DeserializeObject<ObservableCollection<TagPool>>(tagsToken.ToString()) :
                    new ObservableCollection<TagPool>();
                Info.tagPool = new ObservableCollection<TagPool>(tags);

                Info.characters = new ObservableCollection<Character>();

                var charactersToken = aa.SelectToken("characters");
                int cnt = charactersToken?.Children().Count() ?? 0;
                StatusBarText = $"Loading characters lists... {cnt} char founded";
                int counter = 1;
                if (charactersToken != null)
                {
                    foreach (var item in charactersToken.Children())
                    {
                        if (item != null)
                        {
                            var charct = Character.BuildCharacter(item, Info.Now);
                            if (charct != null)
                                Info.characters.Add(charct);
                            counter++;
                            double progress = (double)counter / (double)cnt * 100;
                            StatusBarText = $"Loading characters lists... {progress.ToString("0.0", CultureInfo.InvariantCulture)}%";
                        }
                    }
                }
                StatusBarText = "Loading characters done!";

                StudioList = Info.characters.Select(t => t.studioId).Distinct().ToList();
                StudioList.Insert(0, "All");
                ProfListWithNoTallent = Info.characters.Select(t => t.professions.ProfToDecode).Distinct().ToList();
                ProfListWithOutNoTallent = Info.characters
                    .Where(t => t.professions.IsTalent)
                    .Select(t => t.professions.ProfToDecode)
                    .Distinct().ToList();
                ProfListWithNoTallent.Insert(0, "All");
                ProfListWithOutNoTallent.Insert(0, "All");
                ProfList = ProfListWithNoTallent;
                Filter_Prof = ProfListWithNoTallent[0];
                Filter_studio = StudioList[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public async Task<bool> WriteChange()
        {
            try
            {
                StatusBarText = "Prepare to save";
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Select where to save";
                sfd.DefaultExt = ".json";
                sfd.InitialDirectory = opennedfileplace;
                sfd.RestoreDirectory = true;
                sfd.Filter = "Json|*.json";

                if (sfd.ShowDialog() == true)
                {
                    var z = jobj["stateJson"];
                    z["reputation"] = Info.reputation;
                    z["budget"] = Info.budget;
                    z["cash"] = Info.cash;
                    z["influence"] = Info.influence;

                    foreach (var mil in Info.milestones)
                    {
                        if (z["milestones"][mil.id] != null)
                        {
                            z["milestones"][mil.id]["finished"] = mil.finished;
                            z["milestones"][mil.id]["locked"] = mil.locked;
                            z["milestones"][mil.id]["progress"] = mil.progress;
                        }
                    }
                    foreach (var item in Info.openedPerks)
                    {
                        var openedPerksArray = (JArray)z["openedPerks"];
                        bool exists = false;
                        foreach (var x in openedPerksArray)
                        {
                            if (x.ToString(Formatting.None).Equals(item))
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            openedPerksArray.Add(item);
                        }
                    }
                    foreach (var item in Info.tagPool)
                    {
                        var w = JsonConvert.SerializeObject(item);
                        var tagPoolArray = (JArray)z["tagPool"];
                        bool exists = false;
                        foreach (var x in tagPoolArray)
                        {
                            if (x.ToString(Formatting.None).Equals(w))
                            {
                                exists = true;
                                break;
                            }
                        }
                        if (!exists)
                        {
                            var tt = JObject.Parse(w);
                            tagPoolArray.Add(tt);
                        }
                    }

                    if (Info.tagBank.Count < 1)
                        ((JArray)z["tagBank"]).Clear();

                    foreach (Character chr in Info.characters)
                    {
                        var a = z["characters"];
                        JToken b = null;
                        foreach (var token in a)
                        {
                            if (token["id"]?.Value<int>() == chr.id)
                            {
                                b = token;
                                break;
                            }
                        }
                        if (b != null)
                        {
                            b["limit"] = chr.limit;
                            b["Limit"] = chr.limit; //С момента версии 0.8.50EA была добавлена дополнительная строка "Limit"

                            if (chr.WasChanged(Info.Now))
                            { //Оптимизировал под новую версию 0.8.534EA
                                b["mood"] = chr.mood;
                                b["attitude"] = chr.attitude;
                                b["birthDate"] = chr.birthDate;
                                b["studioId"] = chr.studioId == "NONE" ? null : chr.studioId;
                                b["deathDate"] = chr.deathDate;
                                b["state"] = chr.state;
                                b["causeOfDeath"] = chr.causeOfDeath;
                                if (chr.CustomNameWasSetted)
                                    b["customName"] = chr.MyCustomName;
                                var cnt = b["contract"];
                                if (cnt != null)
                                {
                                    if (cnt.HasValues)
                                    {
                                        if (chr.contract == null)
                                        {
                                            cnt.Replace(JValue.CreateNull());
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
                                    else
                                    {
                                        b["contract"] = JToken.Parse(JsonConvert.SerializeObject(chr.contract));
                                    }
                                }
                                var prof = b["professions"];
                                if (prof != null && prof.HasValues)
                                {
                                    prof[chr.professions.Name] = chr.professions.Value;
                                }
                                var lbl = (JArray)b["labels"];
                                if (lbl != null)
                                {
                                    if (chr.labels != null)
                                    {
                                        foreach (var lablel in chr.labels)
                                        {
                                            bool exists = false;
                                            foreach (var x in lbl)
                                            {
                                                if (x.ToString().Equals(lablel))
                                                {
                                                    exists = true;
                                                    break;
                                                }
                                            }
                                            if (!exists)
                                            {
                                                lbl.Add(lablel);
                                            }
                                        }
                                        List<JToken> torem = new List<JToken>();
                                        foreach (var lablel in lbl)
                                        {
                                            if (!chr.labels.Contains(lablel.ToString()))
                                            {
                                                torem.Add(lablel);
                                            }
                                        }
                                        foreach (var t in torem)
                                        {
                                            t.Remove();
                                        }
                                    }
                                }
                                var wtgs = b["whiteTagsNEW"];
                                if (wtgs != null)
                                {
                                    if (chr.whiteTagsNEW != null)
                                    {
                                        foreach (var whiteTag in chr.whiteTagsNEW)
                                        {
                                            bool exists = false;
                                            foreach (JProperty prop in wtgs.Children<JProperty>())
                                            {
                                                if (prop.Value["id"]?.Value<string>() == whiteTag.id)
                                                {
                                                    exists = true;
                                                    var tochng = prop.Value;
                                                    tochng["id"] = whiteTag.id;
                                                    tochng["dateAdded"] = whiteTag.dateAdded;
                                                    tochng["movieId"] = whiteTag.movieId;
                                                    tochng["value"] = whiteTag.Value;
                                                    tochng["IsOverall"] = whiteTag.IsOverall;
                                                    var overallValues = tochng["overallValues"];
                                                    if (overallValues != null)
                                                    {
                                                        foreach (var t_over in overallValues.Children())
                                                        {
                                                            if (t_over["movieId"]?.Value<int>() == 0 && t_over["sourceType"]?.Value<int>() == 0)
                                                            {
                                                                t_over["value"] = whiteTag.ZeroPoint.value;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                            if (!exists)
                                            {
                                                var prop = new JProperty(whiteTag.id);
                                                prop.Value = JToken.Parse(JsonConvert.SerializeObject(whiteTag));
                                                ((JObject)wtgs).Add(prop);
                                            }
                                        }
                                        List<JProperty> torem = new List<JProperty>();
                                        foreach (JProperty whitetg in wtgs.Children<JProperty>())
                                        {
                                            if (!chr.whiteTagsNEW.Any(t => t.id == whitetg.Name))
                                            {
                                                if (WhiteTag.GetEnumVal(whitetg.Name) != Skills.ELSE)
                                                    torem.Add(whitetg);
                                            }
                                        }
                                        foreach (var t in torem)
                                        {
                                            t.Remove();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    StatusBarText = "Write file";
                    await Task.Run(() => File.WriteAllText(sfd.FileName, jobj.ToString(Formatting.None)));
                    StatusBarText = "Save done!";
                    return true;
                }
                else
                {
                    StatusBarText = "Save canceled";
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
