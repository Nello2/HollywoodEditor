using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;


namespace HollywoodEditor.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Character
    {
        // Технические черты для скрытия

        private static readonly HashSet<string> HiddenTraits = new HashSet<string>
        {
            "STERILE",
            "IMMORTAL",
            "SUPER_IMMORTAL"
        };

        public static List<string> Labels => new List<string>()
        {
            "HARDWORKING", "LAZY", "DISCIPLINED", "UNDISCIPLINED", "PERFECTIONIST",
            "INDIFFERENT", "HOTHEADED", "CALM", "LEADER", "TEAM_PLAYER",
            "OPEN_MINDED", "RACIST", "MISOGYNIST", "XENOPHOBE", "DEMANDING",
            "MODEST", "ARROGANT", "SIMPLE", "HEARTBREAKER", "CHASTE",
            "CHEERY", "MELANCHOLIC", "ALCOHOLIC", "LUDOMANIAC", "JUNKIE",
            "UNWANTED_ACTOR", "UNTOUCHABLE", "STERILE", "IMAGE_VIVID",
            "IMAGE_SOPHISTIC", "IMMORTAL", "SUPER_IMMORTAL"
        };

        private int age;
        private string birthDate1;
        private string normalFirst1;
        private string normalLast1;
        private string customName1;
        private bool calcages = false;
        private string myCustomName = null;
        private string studioId1;
        private bool isDead;
        private DateTime CurrNow = new DateTime();
        private double limit1;
        private ObservableCollection<string> _labels;
        private string customPortraitPath;

        [JsonIgnore]
        public bool IsInit { get; set; }

        public double limit
        {
            get => limit1;
            set
            {
                limit1 = value;
                if (professions != null && professions.Value > limit1)
                    professions.Value = limit1;
            }
        }

        public double mood { get; set; }
        public double attitude { get; set; }
        public int id { get; set; }
        public int portraitBaseId { get; set; }
        public string firstNameId { get; set; }
        public string lastNameId { get; set; }

        public string birthDate
        {
            get => birthDate1;
            set => birthDate1 = value;
        }

        public string studioId
        {
            get
            {
                if (studioId1 == null)
                    studioId1 = "NONE";
                return studioId1;
            }
            set
            {
                if (value == "NONE")
                {
                    studioId1 = null;
                    contract = null;
                    state = IsDead ? 16 : 0;
                }
                else
                {
                    if (studioId != null && value != null)
                    {
                        if (value == "PL")
                            state = 1026;
                        else
                            state = 36;
                        if (IsDead)
                            state = 20;
                    }
                    studioId1 = value;
                }
            }
        }

        public int state { get; set; }
        public int gender { get; set; }

        [JsonIgnore]
        public Professions professions { get; set; }
        public Contract contract { get; set; }

        [JsonIgnore]
        public ObservableCollection<WhiteTag> whiteTagsNEW { get; set; }

        public List<string> aSins { get; set; }

        public ObservableCollection<string> labels
        {
            get => _labels;
            set
            {
                _labels = value;
                UpdateFilteredLabels();
            }
        }

        [JsonIgnore]
        public ObservableCollection<string> FilteredLabels { get; private set; } = new ObservableCollection<string>();

        public void UpdateFilteredLabels()
        {
            FilteredLabels.Clear();
            if (_labels != null)
            {
                foreach (var label in _labels)
                {
                    if (!HiddenTraits.Contains(label))
                    {
                        FilteredLabels.Add(label);
                    }
                }
            }
        }

        public string deathDate { get; set; }
        public int causeOfDeath { get; set; }

        [JsonIgnore]
        public bool IsBusyOnJob { get; set; }

        [JsonIgnore]
        public int ContractDaysLeft => contract?.DaysLeft ?? 0;

        [JsonIgnore]
        public int ContractAmount
        {
            get => contract?.amount ?? 0;
            set
            {
                if (contract != null)
                {
                    contract.amount = value;
                }
            }
        }

        public double ContractInitialFee
        {
            get => contract?.initialFee ?? 0;
            set
            {
                if (contract != null)
                {
                    contract.initialFee = value;
                }
            }
        }

        [JsonProperty("monthlySalary")]
        public double ContractMonthlySalary
        {
            get => contract?.monthlySalary ?? 0;
            set
            {
                if (contract != null)
                {
                    contract.monthlySalary = value;
                }
            }
        }

        [JsonProperty("weightToSalary")]
        public double ContractWeightToSalary
        {
            get => contract?.weightToSalary ?? 0;
            set
            {
                if (contract != null)
                {
                    contract.weightToSalary = value;
                }
            }
        }

        [JsonIgnore]
        public DateTime ContractDateOfSigning => contract?.dateOfSigning ?? stateJson.GameStartTime;

        [JsonIgnore]
        public DateTime ContractDateOfEnding => contract?.dateOfEnding ?? stateJson.GameStartTime;

        [JsonIgnore]
        public int ContractType => contract?.contractType ?? 0;

        [JsonIgnore]
        public string CustomPortraitPath
        {
            get => customPortraitPath;
            set
            {
                customPortraitPath = value;
                if (!string.IsNullOrEmpty(value))
                {
                    // Извлекаем portraitBaseId из пути файла

                    string fileName = Path.GetFileNameWithoutExtension(value);
                    string[] parts = fileName.Split('_');
                    if (parts.Length >= 5 && int.TryParse(parts[4], out int newId))
                    {
                        portraitBaseId = newId;
                    }
                }
            }
        }

        public Character()
        {
            IsInit = true;
            whiteTagsNEW = new ObservableCollection<WhiteTag>();
            labels = new ObservableCollection<string>();
            aSins = new List<string>();
            AvalibaleSkills = new List<string>();
            AvalibaleTraits = new List<string>();
            FilteredLabels = new ObservableCollection<string>();
        }

        #region custom
        public string JsonString { get; set; }

        public string normalFirst
        {
            get
            {
                if (string.IsNullOrWhiteSpace(normalFirst1))
                    return firstNameId;
                return normalFirst1;
            }
            set => normalFirst1 = value;
        }

        public string normalLast
        {
            get
            {
                if (string.IsNullOrWhiteSpace(normalLast1))
                    return lastNameId;
                return normalLast1;
            }
            set => normalLast1 = value;
        }

        public string MyCustomName
        {
            get
            {
                if (myCustomName == null)
                    return customName;
                return myCustomName;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    myCustomName = value;
            }
        }

        public string customName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(customName1))
                    return $"{normalFirst} {normalLast}";
                return customName1;
            }
            set => customName1 = value;
        }

        public bool CustomNameWasSetted => MyCustomName != customName;

        public DateTime GetBirthDate => DateTime.ParseExact(birthDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);

        public bool IsDead
        {
            get => isDead;
            set
            {
                isDead = value;
                if (!value)
                {
                    deathDate = "01-01-0001";
                    causeOfDeath = 0;
                    state = (ReservState != 16 && ReservState != 20) ? ReservState : (studioId == "PL" ? 1026 : (studioId == "NONE" ? 0 : 36));
                }
                else
                {
                    deathDate = ReservDateOfDeath;
                    causeOfDeath = ReservCauseOfDeath;
                    state = studioId == "NONE" ? 16 : 20;
                }
            }
        }

        public string ReservDateOfDeath = string.Empty;
        public int ReservCauseOfDeath = 0;
        public int ReservState = 64;

        public int Age
        {
            get => age;
            set
            {
                if (!calcages)
                    birthDate = GetBirthDate.AddYears(age - value).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                else
                    calcages = false;
                age = value;
            }
        }

        public void SetFullAge(DateTime now)
        {
            var age = now.Year - GetBirthDate.Year;
            CurrNow = now;
            if (GetBirthDate.Date > now.AddYears(-age)) age--;
            calcages = true;
            Age = age;
        }

        public string ImgPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CustomPortraitPath) && File.Exists(CustomPortraitPath))
                    return CustomPortraitPath;

                string path = Path.Combine(App.PathToExe, "Resources", "Profiles") + Path.DirectorySeparatorChar;
                path += "PRT_";

                switch (professions?.GetProfession)
                {
                    case Professions.Profession.Agent:
                        path += "AGENT_";
                        break;

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
                        path += "LIEUT_";
                        break;

                    default:
                        path += "TALENT_";
                        break;
                }

                path += gender == 1 ? "F_" : "M_";

                if (Age >= 60)
                    path += "OLD_";
                else if (Age > 40 && Age < 60)
                    path += "MID_";
                else
                    path += "YOUNG_";

                path += $"{portraitBaseId}.png";

                // Если файл существует — отдаём именно его. 

                if (File.Exists(path))
                    return path;

                // Иногда Profiles.zip распаковывается с дополнительной вложенной папкой Profiles.
                // Не меняем тип/пол/возраст, а ищем только тот же самый файл рекурсивно.

                string profilesRoot = Path.Combine(App.PathToExe, "Resources", "Profiles");
                if (Directory.Exists(profilesRoot))
                {
                    string wanted = Path.GetFileName(path);
                    string found = Directory.GetFiles(profilesRoot, wanted, SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(found) && File.Exists(found))
                        return found;
                }

                return "pack://application:,,,/Resources/user.png";
            }
        }

        public List<string> AvalibaleSkills { get; set; }

        public void SetAvSkills()
        {
            if (professions == null)
            {
                AvalibaleSkills = new List<string>();
                return;
            }

            var answ = new List<string>
            {
                "ACTION", "DRAMA", "HISTORICAL", "THRILLER",
                "ROMANCE", "DETECTIVE", "COMEDY", "ADVENTURE"
            };

            switch (professions.GetProfession)
            {
                case Professions.Profession.Scriptwriter:
                case Professions.Profession.Producer:
                    break;
                case Professions.Profession.Cinematographer:
                    answ = new List<string> { "INDOOR", "OUTDOOR" };
                    break;
                case Professions.Profession.Director:
                case Professions.Profession.Actor:
                    answ.Add("COM");
                    answ.Add("ART");
                    break;
                default:
                    AvalibaleSkills = new List<string>();
                    return;
            }

            if (whiteTagsNEW != null)
            {
                var existing = whiteTagsNEW.Select(t => t.id).ToList();
                answ = answ.Where(s => !existing.Contains(s)).ToList();
            }

            AvalibaleSkills = answ;
        }

        public List<string> AvalibaleTraits { get; set; }

        [JsonIgnore]
        public List<string> FilteredAvalibaleTraits
        {
            get
            {
                if (AvalibaleTraits == null) return new List<string>();
                return AvalibaleTraits.Where(t => !HiddenTraits.Contains(t)).ToList();
            }
        }

        public void SetAvTraits()
        {
            if (professions == null)
            {
                AvalibaleTraits = new List<string>();
                return;
            }

            var answ = Character.Labels.ToList();

            if (!new[]
            {
                Professions.Profession.Scriptwriter, Professions.Profession.Producer,
                Professions.Profession.FilmEditor, Professions.Profession.Director,
                Professions.Profession.Composer, Professions.Profession.Cinematographer,
                Professions.Profession.Actor
            }.Contains(professions.GetProfession))
            {
                AvalibaleTraits = new List<string>();
                return;
            }

            if (labels != null)
            {
                answ = answ.Where(t => !labels.Contains(t)).ToList();
            }

            AvalibaleTraits = answ;
        }
        #endregion

        public static bool operator ==(Character a, Character b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(Character a, Character b) => !(a == b);

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            Character other = (Character)obj;

            bool basicEquals = limit == other.limit &&
                   mood == other.mood &&
                   attitude == other.attitude &&
                   id == other.id &&
                   deathDate == other.deathDate &&
                   causeOfDeath == other.causeOfDeath &&
                   firstNameId == other.firstNameId &&
                   lastNameId == other.lastNameId &&
                   birthDate == other.birthDate &&
                   gender == other.gender &&
                   studioId == other.studioId &&
                   state == other.state;

            if (!basicEquals) return false;

            if (labels == null && other.labels != null) return false;
            if (labels != null && other.labels == null) return false;
            if (labels != null && other.labels != null)
            {
                if (!labels.SequenceEqual(other.labels)) return false;
            }

            if (contract == null && other.contract != null) return false;
            if (contract != null && other.contract == null) return false;
            if (contract != null && other.contract != null)
            {
                if (contract != other.contract) return false;
            }

            return true;
        }

        public override int GetHashCode() => id.GetHashCode();

        public bool WasChanged(DateTime Now)
        {
            var backup = BuildCharacter(JToken.Parse(JsonString), Now);
            return !Equals(backup);
        }

        public override string ToString() => $"{MyCustomName} {professions?.Name}";

        public static Character BuildCharacter(JToken json, DateTime Now)
        {
            var z = JsonConvert.DeserializeObject<Character>(json.ToString());
            if (z == null) return null;

            z.isDead = z.deathDate != "01-01-0001";
            z.ReservDateOfDeath = z.IsDead ? z.deathDate : Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            z.ReservCauseOfDeath = z.causeOfDeath;
            z.ReservState = z.state;

            var aopm = json.SelectToken("activeOrPlannedMovies");
            z.IsBusyOnJob = aopm != null && aopm.HasValues;

            var profToken = json.SelectToken("professions")?.ToObject<JObject>()?.Properties()?.FirstOrDefault();
            if (profToken != null)
            {
                z.professions = new Professions
                {
                    Name = profToken.Name,
                    Value = profToken.Value.ToObject<double>()
                };
            }

            z.JsonString = json.ToString();
            z.SetFullAge(Now);

            if (z.contract != null)
            {
                z.contract.dateOfNow = Now;
                z.contract.SetCalcDaysLeft();
                z.contract.IsInit = false;
            }

            var tags = json.SelectToken("whiteTagsNEW");
            if (tags?.Children().Count() > 0)
            {
                z.whiteTagsNEW = new ObservableCollection<WhiteTag>();
                foreach (var tag in tags.Children())
                {
                    var in_tag = tag.First();
                    if (in_tag == null) continue;

                    var whiteTag = new WhiteTag();
                    whiteTag.id = in_tag.SelectToken("id")?.Value<string>();
                    if (whiteTag.Tagtype == Skills.ELSE) continue;

                    whiteTag.dateAdded = in_tag.SelectToken("dateAdded")?.Value<DateTime>() ?? stateJson.GameStartTime;
                    whiteTag.movieId = in_tag.SelectToken("movieId")?.Value<int>() ?? 0;
                    whiteTag.Value = in_tag.SelectToken("value")?.Value<double>() ?? 0;
                    whiteTag.IsOverall = in_tag.SelectToken("IsOverall")?.Value<bool>() ?? false;

                    var overallToken = in_tag.SelectToken("overallValues");
                    if (overallToken != null)
                        whiteTag.overallValues = JsonConvert.DeserializeObject<List<OverallValue>>(overallToken.ToString());

                    z.whiteTagsNEW.Add(whiteTag);
                }
            }

            if (z.labels != null)
            {
                z.UpdateFilteredLabels();
            }

            z.SetAvSkills();
            z.SetAvTraits();
            z.IsInit = false;
            return z;
        }
    }
}