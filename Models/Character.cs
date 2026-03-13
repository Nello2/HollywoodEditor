using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace HollywoodEditor.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Character
    {
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

        public ObservableCollection<string> labels { get; set; }

        public string deathDate { get; set; }
        public int causeOfDeath { get; set; }

        [JsonIgnore]
        public bool IsBusyOnJob { get; set; }

        [JsonIgnore]
        public int ContractDaysLeft => contract?.DaysLeft ?? 0;

        [JsonIgnore]
        public int ContractAmount => contract?.amount ?? 0;

        [JsonIgnore]
        public double ContractInitialFee => contract?.initialFee ?? 0;

        [JsonIgnore]
        public double ContractMonthlySalary => contract?.monthlySalary ?? 0;

        [JsonIgnore]
        public double ContractWeightToSalary => contract?.weightToSalary ?? 0;

        [JsonIgnore]
        public DateTime ContractDateOfSigning => contract?.dateOfSigning ?? stateJson.GameStartTime;

        [JsonIgnore]
        public DateTime ContractDateOfEnding => contract?.dateOfEnding ?? stateJson.GameStartTime;

        [JsonIgnore]
        public int ContractType => contract?.contractType ?? 0;

        public Character()
        {
            IsInit = true;
            whiteTagsNEW = new ObservableCollection<WhiteTag>();
            labels = new ObservableCollection<string>();
            aSins = new List<string>();
            AvalibaleSkills = new List<string>();
            AvalibaleTraits = new List<string>();
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

            return limit == other.limit &&
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

            z.SetAvSkills();
            z.SetAvTraits();
            z.IsInit = false;
            return z;
        }
    }
}