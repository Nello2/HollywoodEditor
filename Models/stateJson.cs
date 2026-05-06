using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HollywoodEditor.Models
{
    [AddINotifyPropertyChangedInterface]
    public class TagPool
    {
        public string Item1 { get; set; }
        public DateTime Item2 { get; set; }
        public TagPool()
        {
            Item1 = string.Empty;
            Item2 = new DateTime();
        }
        public TagPool(string item1, DateTime item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    // Класс для отделов
    public class Department
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<string> Techs { get; set; }
        public int CurrentLevel { get; set; }
        public int MaxLevel { get; set; }
    }

    [AddINotifyPropertyChangedInterface]
    public class stateJson
    {
        public static DateTime GameStartTime => new DateTime(1929, 1, 1);
        public int budget { get; set; }
        public int cash { get; set; }
        public double reputation { get; set; }
        public int influence { get; set; }
        public string studioName { get; set; }
        public string timePassed { get; set; }

        public DateTime Now
        {
            get
            {
                int days = 0;
                if (!string.IsNullOrEmpty(timePassed))
                {
                    var parts = timePassed.Split('.');
                    if (parts.Length > 0)
                    {
                        int.TryParse(parts[0], out days);
                    }
                }
                return GameStartTime.AddDays(days);
            }
        }

        public ObservableCollection<Character> characters { get; set; }
        public Dictionary<string, DateTime> NextSpawnDays { get; set; }
        public ObservableCollection<Milestones> milestones { get; set; }

        private int valOfActivePolicy;
        public int ValOfActivePolicy
        {
            get
            {
                return valOfActivePolicy;
            }
            set
            {
                if (milestones == null)
                {
                    valOfActivePolicy = 0;
                }
                else
                {
                    var curr = milestones.Where(t => t.id.Contains(NameOfActivePolicy));
                    if (curr.Count() > 0)
                    {
                        var q = curr.Single(t => t.Inner_id == value);
                        q.finished = true;
                        q.progress = 1.0;

                        if (value < 3)
                        {
                            curr.Single(t => t.Inner_id == value + 1).locked = false;
                            curr.Single(t => t.Inner_id == value + 1).progress = 0.0d;
                            curr.Single(t => t.Inner_id == value + 1).finished = false;
                        }

                        foreach (var item in curr.Where(t => t.Inner_id > value + 1))
                        {
                            item.locked = true;
                            item.progress = 0.0d;
                            item.finished = false;
                        }
                        valOfActivePolicy = value;
                    }
                    else
                        valOfActivePolicy = 0;
                }
            }
        }

        public bool HaveActivePolicy
        {
            get
            {
                if (milestones != null)
                    return milestones.Where(t => t.finished).Count() > 0;
                else
                    return false;
            }
        }

        public string NameOfActivePolicy
        {
            get
            {
                if (milestones != null)
                {
                    if (milestones.Where(t => t.finished).Count() > 0)
                    {
                        var result = MaxBy();
                        return result != null ? result.ToString() : "NONE";
                    }
                    else
                        return "NONE";
                }
                else
                    return "NONE";
            }
        }

        private object MaxBy()
        {
            if (milestones == null || !milestones.Any())
                return null;

            Milestones maxMilestone = null;
            foreach (var milestone in milestones)
            {
                if (milestone.finished)
                {
                    if (maxMilestone == null || milestone.Inner_name.CompareTo(maxMilestone.Inner_name) > 0)
                    {
                        maxMilestone = milestone;
                    }
                }
            }
            return maxMilestone?.Inner_name;
        }

        public ObservableCollection<string> AvailablePerks { get; set; }
        public ObservableCollection<string> openedPerks { get; set; }
        public ObservableCollection<string> tagBank { get; set; }
        public ObservableCollection<TagPool> tagPool { get; set; }

        // Словарь зависимостей технологий <- 0.8.68EA
        public static Dictionary<string, List<string>> TechDependencies { get; } = new Dictionary<string, List<string>>
        {
            // ============ ТЕХНОЛОГИЧЕСКИЙ ОТДЕЛ ============

            ["STUDIO_TECH_RED_TIME_1"] = new List<string> { "STUDIO_TECH" },
            ["STUDIO_TECH_RED_TIME_2"] = new List<string> { "STUDIO_TECH_RED_TIME_1" },
            ["STUDIO_TECH_ADD_RND"] = new List<string> { "STUDIO_TECH" },
            ["BLDG_RND_II"] = new List<string> { "BLDG_RND_I" },
            ["BLDG_RND_III"] = new List<string> { "BLDG_RND_II" },
            ["BLDG_RND_IV"] = new List<string> { "BLDG_RND_III" },

            ["BLDG_PAVILION_II"] = new List<string>(),
            ["BLDG_PAVILION_III"] = new List<string> { "BLDG_PAVILION_II" },
            ["BLDG_PAVILION_IV"] = new List<string> { "BLDG_PAVILION_III" },

            ["BLDG_LOGISTICS"] = new List<string>(),
            ["TEAM_SERVICE_1"] = new List<string> { "BLDG_LOGISTICS" },
            ["TEAM_SERVICE_2"] = new List<string> { "TEAM_SERVICE_1" },

            ["BLDG_LINE_PRODUCTION"] = new List<string>(),
            ["SECOND_UNIT"] = new List<string> { "BLDG_LINE_PRODUCTION" },
            ["URGENT_DOUBLE_SEARCH"] = new List<string> { "SECOND_UNIT" },
            ["URGENT_EXTRAS_SEARCH"] = new List<string> { "URGENT_DOUBLE_SEARCH" },
            ["URGENT_LOCATION_SEARCH"] = new List<string> { "SECOND_UNIT" },
            ["URGENT_CREW_SEARCH"] = new List<string> { "URGENT_LOCATION_SEARCH" },
            ["FLEX_SCHEDULE"] = new List<string> { "URGENT_EXTRAS_SEARCH", "URGENT_CREW_SEARCH" },

            ["PROD_DIR_CIN_ACT_XP_1"] = new List<string>(),

            // ============ ОТДЕЛ ПРОДЮСИРОВАНИЯ ============

            ["NEGOTIATION_SCALE_50"] = new List<string>(), // 50% от запросов
            ["NEGOTIATION_SCALE_75"] = new List<string> { "NEGOTIATION_SCALE_50" }, // 75% от запросов

            ["TWO_PROJECTS"] = new List<string>(), // Один продюсер на двух проектах

            ["PRODUCERS_ON_FILM_2"] = new List<string> { "TWO_PROJECTS" }, // Два продюсера
            ["PRODUCERS_ON_FILM_3"] = new List<string> { "PRODUCERS_ON_FILM_2" }, // Три продюсера
            ["CONTRACT_WEIGHT"] = new List<string> { "TWO_PROJECTS" }, // Влиятельный продюсер

            // ============ ЮРИДИЧЕСКИЙ ОТДЕЛ ============

            ["LEGAL_DEFENSE_1"] = new List<string>(), // Юридическая защита среднего уровня - начало

            ["LEGAL_DEFENSE_2"] = new List<string> { "LEGAL_DEFENSE_1" }, // Юридическая защита высокого уровня
            ["LEGAL_DEFENSE_3"] = new List<string> { "LEGAL_DEFENSE_2" }, // Юридическая защита восхитительного уровня

            ["CONTRACT_TERMINATION_FEE_1"] = new List<string> { "LEGAL_DEFENSE_1" }, // Расторжение с половиной отступных
            ["CONTRACT_TERMINATION_FEE_2"] = new List<string> { "CONTRACT_TERMINATION_FEE_1" }, // В два раза больше отступных

            ["CONTRACT_PAYMENTS_50_50"] = new List<string> { "LEGAL_DEFENSE_1" }, // Отсрочка выплатам по контракту

            ["CONTRACT_5_MOVIES"] = new List<string> { "CONTRACT_PAYMENTS_50_50" }, // Контракт на 5 фильмов
            ["CONTRACT_10_MOVIES"] = new List<string> { "CONTRACT_5_MOVIES" }, // Контракт на 10 фильмов

            ["CONTRACT_5_YEARS"] = new List<string> { "CONTRACT_PAYMENTS_50_50" }, // Контракт на 5 лет
            ["CONTRACT_10_YEARS"] = new List<string> { "CONTRACT_5_YEARS" }, // Контракт на 10 лет

            // ============ ОТДЕЛ ПО ФИНАНСАМ ============

            ["BANK_LOAN"] = new List<string>(), // Кредит на 1.000.000$ 
            ["CASH_FLOW_1"] = new List<string> { "BANK_LOAN" }, // $500 наличные в месяц
            ["CASH_FLOW_2"] = new List<string> { "CASH_FLOW_1" }, // $1.000 наличные в месяц

            ["BANK_LOAN_EARLY_REPAYMENT"] = new List<string> { "BANK_LOAN" }, // Досрочное погашение
            ["BANK_LOAN_INT_RATE_REDUCTION_1"] = new List<string> { "BANK_LOAN_EARLY_REPAYMENT" },
            ["BANK_LOAN_INT_RATE_REDUCTION_2"] = new List<string> { "BANK_LOAN_INT_RATE_REDUCTION_1" },

            ["BANK_LOAN_AMOUNT_1"] = new List<string> { "BANK_LOAN_EARLY_REPAYMENT" },
            ["BANK_LOAN_AMOUNT_2"] = new List<string> { "BANK_LOAN_AMOUNT_1" },
            ["BANK_LOAN_TERM_1"] = new List<string> { "BANK_LOAN_EARLY_REPAYMENT" },
            ["BANK_LOAN_TERM_2"] = new List<string> { "BANK_LOAN_TERM_1" },

            // ============ ОТДЕЛ КАДРОВ(HR) ============

            ["BLDG_ESCORT_DOMINION"] = new List<string>(), // Отдел обеспечения комфорта 
            ["IMPROVEMENT_0_NO_SADNESS"] = new List<string> { "BLDG_ESCORT_DOMINION" }, // Аскетичные сотрудники
            ["HIRING_BONUSES"] = new List<string> { "IMPROVEMENT_0_NO_SADNESS" }, // Восторженные новички
            ["NOMINATION_LOSS_NO_SADNESS"] = new List<string> { "HIRING_BONUSES" }, // Позитивный настрой
            ["MOVIE_RELEASE_MOOD_BOOST"] = new List<string> { "HIRING_BONUSES" }, // Профессиональная реализация
            ["BAD_ATTITUDE_NO_SADNESS"] = new List<string> { "HIRING_BONUSES" }, // Философский подход


            ["ETHNIC_COMPOSITION"] = new List<string>(), // Этнический состав 
            ["ILLEGAL_WORKERS"] = new List<string> { "ETHNIC_COMPOSITION" }, // Нелегалы
            ["CHEAP_ILLEGALS"] = new List<string> { "ILLEGAL_WORKERS" }, // Дешевые нелегалы

            ["BUILDINGS_CONSERVATION"] = new List<string> { "ETHNIC_COMPOSITION" }, // Консервация зданий
            ["CONSERVATION_COOLDOWN"] = new List<string> { "BUILDINGS_CONSERVATION" }, // Дешевый простой
            ["STAFF_LARGE1"] = new List<string> { "BUILDINGS_CONSERVATION" }, // Гибкая консервация 

            // ============ ОТДЕЛ ПО СВЯЗЯМ С ОБЩЕСТВЕННОСТЬЮ ============

            ["CHARITY_TO_REP"] = new List<string>(), // Благотворительность

            ["GENERATION_IP_AND_REP"] = new List<string>(), // Генерация ОВ или репутация вместо изучения улучшений 
            ["GENERATION_IP_X2"] = new List<string> { "GENERATION_IP_AND_REP" }, // Вдвое больше ОВ за генерацию
            ["GENERATION_REP_X2"] = new List<string> { "GENERATION_IP_AND_REP" }, // Вдвое больше репутации за генерацию

            ["PROFITABLE_MOVIE_REP_2"] = new List<string>(), // Двойная репутация за прибыльные фильмы 

            ["TOP1_TOP3"] = new List<string> { "PROFITABLE_MOVIE_REP_2" }, // ОВ и репутация за топ-3
            ["TECH_SALE_PP"] = new List<string> { "TOP1_TOP3" }, // Вдвое больше ОВ за продажу технологий

            ["MOVIE_RELEASE_ATTITUDE_1"] = new List<string> { "PROFITABLE_MOVIE_REP_2" }, // Репутация за хорошее отношение к студии
            ["INITIATIVE_PP_FREE"] = new List<string> { "MOVIE_RELEASE_ATTITUDE_1" }, // Владение инициативой
            ["MOVIE_RELEASE_MOOD_1"] = new List<string> { "MOVIE_RELEASE_ATTITUDE_1" }, // Вдвое больше репутации за хорошее отношение к студии
            ["ICON_REP_1"] = new List<string> { "MOVIE_RELEASE_ATTITUDE_1" }, // Вдвое больше репутации за Икону
            ["LEGEND_REP_1"] = new List<string> { "MOVIE_RELEASE_ATTITUDE_1" }, // Вдвое больше репутации за Легенду
            ["SKILLED_ACTOR_REP"] = new List<string> { "LEGEND_REP_1", "ICON_REP_1" }, // Репутация за найм актеров

            // ============ ОТДЕЛ ПОСТПРОДАКШНА ============

            ["POST_DIR_MONT_COMP_XP_1"] = new List<string>(), // Двойной опыт за постпродакшен

            ["BLDG_LAB"] = new List<string>(), // Кинолаборатория 
            ["LAB_INHOUSE_IMPROVED"] = new List<string> { "BLDG_LAB" }, // Улучшенная проявка
            ["LAB_INHOUSE_TIME_1"] = new List<string> { "LAB_INHOUSE_IMPROVED" }, // Ускоренная проявка

            ["BLDG_SOUND"] = new List<string>(), // Студия звукозаписи 
            ["SOUND_INHOUSE_IMPROVED"] = new List<string> { "BLDG_SOUND" }, // Улучшенная студия звукозаписи
            ["SOUND_INHOUSE_TIME_1"] = new List<string> { "SOUND_INHOUSE_IMPROVED" }, // Ускоренная работа студии звукозаписи

            ["BLDG_CONCERT"] = new List<string>(), // Оркестровый зал 
            ["CONCERT_INHOUSE_MPROVED"] = new List<string> { "BLDG_CONCERT" }, // Улучшенный оркестровый зал
            ["CONCERT_INHOUSE_TIME_1"] = new List<string> { "CONCERT_INHOUSE_MPROVED" }, // Ускоренная работа Оркестрового зала

            // ============ ОТДЕЛ ПРОКАТА ============

            ["BLDG_DISTRIBUTION"] = new List<string>(), // Управление кинотеатрами 
            ["MOVIE_THEATRE_SLOT_ADD_1"] = new List<string> { "BLDG_DISTRIBUTION" }, // Оптимизация кинотеатров
            ["MOVIE_THEATRE_SLOT_RENT"] = new List<string> { "BLDG_DISTRIBUTION" }, // Кинотеатры в аренду

            ["BLDG_ANALYTICS"] = new List<string>(), // Аналитический офис 
            ["ANALYSIS_GROUPS"] = new List<string> { "BLDG_ANALYTICS" }, // Аналитика аудитории
            ["POSTRELEASE_ANALYSIS"] = new List<string> { "BLDG_ANALYTICS" }, // Аналитика после проката
            ["ANALYSIS_ENTIRE_CAST"] = new List<string> { "BLDG_ANALYTICS" }, // Актеры конкурентов
            ["ANALYSIS_SCREENPLAY"] = new List<string> { "ANALYSIS_ENTIRE_CAST" }, // Оценки сценариев конкурентов
            ["ANALYSIS_TAGS"] = new List<string> { "ANALYSIS_ENTIRE_CAST" }, // Теги конкурентов
            ["ANALYSIS_BUDGET"] = new List<string> { "ANALYSIS_ENTIRE_CAST" }, // Бюджеты конкурентов

            ["BLDG_PRINT"] = new List<string>(), // Печатный офис 
            ["PRINT_EMERGENCY"] = new List<string> { "BLDG_PRINT" }, // Экстренная печать фильмокопий
            ["PRINT_INHOUSE_QLT_1"] = new List<string> { "BLDG_PRINT" }, // Печать за 3 недели
            ["PRINT_INHOUSE_QLT_2"] = new List<string> { "PRINT_INHOUSE_QLT_1" }, // Печать за неделю

            ["BLDG_MARKETING"] = new List<string>(), // Офис продвижения 
            ["SCANDAL_COVER_UP_MONEY"] = new List<string> { "BLDG_MARKETING" }, // Замять скандал за деньги
            ["SCANDAL_COVER_UP_PP"] = new List<string> { "SCANDAL_COVER_UP_MONEY" }, // Замять скандал за ОВ

            ["WM_HOSPICE"] = new List<string> { "BLDG_MARKETING" }, // Визит в хоспис
            ["WM_ORPHANAGE"] = new List<string> { "WM_HOSPICE" }, // Визит в детский дом
            ["WM_WEDDING"] = new List<string> { "WM_ORPHANAGE" }, // Внезапное появление на свадьбе
            ["WM_HOMELESS"] = new List<string> { "WM_WEDDING" }, // Помощь бездомным
            ["WM_DEBT"] = new List<string> { "WM_HOMELESS" }, // Оплата долгов

            // Грязные уловки
            ["BM_UNLOCK"] = new List<string> { "BLDG_MARKETING" }, // Спасение утопающего 
            ["BM_DROWNING"] = new List<string> { "BM_UNLOCK" }, // Спасение утопающего
            ["BM_DRUNKARD"] = new List<string> { "BM_DROWNING" }, // Помощь пьянице
            ["BM_FIGHT"] = new List<string> { "BM_DRUNKARD" }, // Защита прохожего
            ["BM_CRIMINAL"] = new List<string> { "BM_FIGHT" }, // Поимка опасного преступника
            ["BM_HOUSE_BURN"] = new List<string> { "BM_CRIMINAL" }, // Помощь погорельцам

            // ============ СЦЕНАРНЫЙ ОТДЕЛ ============

            ["BLDG_CONSTRUCTOR"] = new List<string>(), // Сценарный конструктор
            ["EDITS_ON_GO"] = new List<string>(),

            ["SCREENPLAY_TIME_RED_1"] = new List<string>(), // Сценаристы пишут на 15% быстрее 
            ["SCREENPLAY_TIME_RED_2"] = new List<string> { "SCREENPLAY_TIME_RED_1" }, // Сценаристы пишут на 30% быстрее
            ["SCREENPLAY_TIME_RED_3"] = new List<string> { "SCREENPLAY_TIME_RED_2" }, // Сценаристы пишут в два раза быстрее

            ["NEW_SCREENPLAY_PP_BONUS_1"] = new List<string> { "SCREENPLAY_TIME_RED_2" }, // Вдвое больше ОВ за сценарий
            ["NEW_SCREENPLAY_PP_BONUS_2"] = new List<string> { "NEW_SCREENPLAY_PP_BONUS_1" }, // Втрое больше ОВ за сценарий

            ["NEW_SCREENPLAY_XP_BONUS_1"] = new List<string> { "SCREENPLAY_TIME_RED_1" }, // Дополнительные 15% опыта за сценарий
            ["NEW_SCREENPLAY_XP_BONUS_2"] = new List<string> { "NEW_SCREENPLAY_XP_BONUS_1" }, // Дополнительные 30% опыта за сценарий
            ["NEW_SCREENPLAY_XP_BONUS_3"] = new List<string> { "NEW_SCREENPLAY_XP_BONUS_2" }, // Дополнительные 50% опыта за сценарий

            ["SCEN_IDEAS_STORAGE_1"] = new List<string>(), // Дополнительные 6 месяцев на хранение идей 
            ["SCEN_IDEAS_GEN_AMT_1"] = new List<string> { "SCEN_IDEAS_STORAGE_1" }, // 3-4 идеи в месяц
            ["SCEN_IDEAS_GEN_AMT_2"] = new List<string> { "SCEN_IDEAS_GEN_AMT_1" }, // 5-6 идей в месяц

            ["MOVIE_RELEASE_XP_1"] = new List<string>(), // Дополнительные 25% опыта за релиз 
            ["MOVIE_RELEASE_XP_2"] = new List<string> { "MOVIE_RELEASE_XP_1" }, // Дополнительные 50% опыта за релиз
            ["MOVIE_RELEASE_XP_3"] = new List<string> { "MOVIE_RELEASE_XP_2" }, // Дополнительные 100% опыта за релиз
            ["MOVIE_RELEASE_TOP10_AUD_XP_1"] = new List<string> { "MOVIE_RELEASE_XP_2" }, // Опыт за признание зрителей
            ["MOVIE_RELEASE_TOP10_COM_XP_1"] = new List<string> { "MOVIE_RELEASE_XP_2" }, // Опыт за высокие сборы
            ["MOVIE_RELEASE_TOP10_ART_XP_1"] = new List<string> { "MOVIE_RELEASE_XP_2" }, // Опыт за признание критиков

            ["MOVIE_SEQUEL"] = new List<string> { "BLDG_CONSTRUCTOR" }, // Сиквелы  
            ["MOVIE_SEQUEL_ORIGINALITY"] = new List<string> { "MOVIE_SEQUEL" }, // Свежий взгляд
            ["MOVIE_SEQUEL_LEGACY"] = new List<string> { "MOVIE_SEQUEL" }, // Достойный преемник
            ["BLDG_COPYRIGHT"] = new List<string> { "MOVIE_SEQUEL" }, // Сценарный конструктор 

            ["TAGS_RESEARCH"] = new List<string> { "BLDG_CONSTRUCTOR" }, // Исследование новых тегов
            ["TAGS_RESEARCH_DIRECTION"] = new List<string> { "TAGS_RESEARCH" }, // Исследование новых тегов по категориям
            ["TAGS_SLOTS_6"] = new List<string> { "TAGS_RESEARCH" }, // 6 тегов наполнения в синопсисе
            ["TAGS_SLOTS_7"] = new List<string> { "TAGS_SLOTS_6" }, // 7 тегов наполнения в синопсисе
            ["TAGS_SLOTS_8"] = new List<string> { "TAGS_SLOTS_7" }, // 8 тегов наполнения в синопсисе
            ["TAGS_SLOTS_9"] = new List<string> { "TAGS_SLOTS_8" }, // 9 тегов наполнения в синопсисе
            ["TAGS_SLOTS_10"] = new List<string> { "TAGS_SLOTS_9" }, // 10 тегов наполнения в синопсисе

            ["NEW_TAG_BY_LT_1"] = new List<string> { "TAGS_RESEARCH" }, // Новый тег каждые 6 месяцев
            ["NEW_TAG_BY_LT_2"] = new List<string> { "NEW_TAG_BY_LT_1" }, // Новый тег каждые 3 месяца

            ["TAGS_RESEARCH_TIME_RED_1"] = new List<string> { "TAGS_RESEARCH" }, // Исследование тегов на 15% быстрее
            ["TAGS_RESEARCH_TIME_RED_2"] = new List<string> { "TAGS_RESEARCH_TIME_RED_1" }, // Исследование тегов на 30% быстрее
            ["TAGS_RESEARCH_TIME_RED_3"] = new List<string> { "TAGS_RESEARCH_TIME_RED_2" }, // Исследование тегов в 2 раза быстрее

            ["TAGS_XP_BONUS_1"] = new List<string> { "TAGS_RESEARCH_TIME_RED_1" }, // Дополнительные 25% опыта за исследование тегов
            ["TAGS_XP_BONUS_2"] = new List<string> { "TAGS_XP_BONUS_1" }, // Дополнительные 50% опыта за исследование тегов
            ["TAGS_XP_BONUS_3"] = new List<string> { "TAGS_XP_BONUS_2" }, // В два раза больше опыта за исследование тегов

            ["TAGS_NEW_PP_BONUS"] = new List<string> { "TAGS_RESEARCH_TIME_RED_2" }, // Вдвое больше ОВ за исследованный тег

            ["BLDG_FREELANCE"] = new List<string>(), // Офис по работе с внештатными сценаристами 
            ["SCRIPT_DOCTORS"] = new List<string> { "BLDG_FREELANCE" }, // Скрипт-доктор
            ["SCRIPT_DOCTORS_FASTER"] = new List<string> { "SCRIPT_DOCTORS" }, // Ускоренная работа скрипт-доктора
            ["SCRIPT_DOCTORS_CHEAPER"] = new List<string> { "SCRIPT_DOCTORS" }, // Удешевленная работа скрипт-доктора
            ["SCRIPT_DOCTORS_RANGE"] = new List<string> { "SCRIPT_DOCTORS_FASTER", "SCRIPT_DOCTORS_CHEAPER" }, // Тщательная работа скрипт-доктора
            ["SCRIPT_DOCTORS_SCORES"] = new List<string> { "SCRIPT_DOCTORS_RANGE" }, // Усердная работа скрипт-доктора

            // ============ ОТДЕЛ ПРЕПРОДАКШНА ============

            ["BLDG_SUPPLY"] = new List<string>(), // Офис технического снабжения

            ["BLDG_WORKSHOP"] = new List<string>(), // Художественные мастерские 
            ["SETS_QLT_2"] = new List<string> { "BLDG_WORKSHOP" }, // Декорации высокого качества
            ["SETS_QLT_3"] = new List<string> { "SETS_QLT_2" }, // Декорации восхитительного качества
            ["PROPS_QLT_2"] = new List<string> { "BLDG_WORKSHOP" }, // Костюмы и реквизит высокого качества
            ["PROPS_QLT_3"] = new List<string> { "PROPS_QLT_2" }, // Костюмы и реквизит восхитительного качества

            ["SETS_TIME_RED_1"] = new List<string> { "BLDG_WORKSHOP" }, // Сотрудники работают на 10% быстрее
            ["SETS_TIME_RED_2"] = new List<string> { "SETS_TIME_RED_1" }, // Сотрудники работают на 20% быстрее
            ["SETS_TIME_RED_3"] = new List<string> { "SETS_TIME_RED_2" }, // Сотрудники работают на 30% быстрее

            ["BLDG_SCOUT"] = new List<string>(), // Скаут офис 
            ["LOCATION_QLT_1"] = new List<string> { "BLDG_SCOUT" }, // Локации высокого качества
            ["LOCATION_QLT_2"] = new List<string> { "LOCATION_QLT_1" }, // Локации восхитительного качества
            ["LOCATION_SEARCH_TIME_1"] = new List<string> { "BLDG_SCOUT" }, // Скауты работают на 20% быстрее
            ["LOCATION_SEARCH_TIME_2"] = new List<string> { "LOCATION_SEARCH_TIME_1" }, // Скауты работают на 40% быстрее
            ["LOCATION_SEARCH_WORLD"] = new List<string> { "BLDG_SCOUT" }, // Поиск локаций по всему миру


            ["BLDG_CASTING"] = new List<string>(), // Кастинг офис 
            ["PREPROD_PROD_DIR_CIN_XP_1"] = new List<string> { "BLDG_CASTING" }, // Опыт для режиссеров и операторов
            ["PREPROD_PROD_DIR_CIN_XP_2"] = new List<string> { "PREPROD_PROD_DIR_CIN_XP_1" }, // В два раза больше опыта

            ["EXTRAS_2"] = new List<string> { "BLDG_CASTING" }, // Массовка до 100 человек
            ["EXTRAS_3"] = new List<string> { "EXTRAS_2" }, // Массовка до 500 человек
            ["EXTRAS_4"] = new List<string> { "EXTRAS_3" }, // Массовка более 500 человек

            // ============ ОТДЕЛ ОБЕСПЕЧЕНИЯ КОМФОРТА ============

            ["WG_WATCHES"] = new List<string> { "BLDG_ESCORT_DOMINION" }, // Часы Silver Moon Kronos 
            ["WG_CIGARS"] = new List<string> { "WG_WATCHES" }, // Сигары Alexandre Dumas Siglo VI
            ["WG_ALCOHOL"] = new List<string> { "WG_WATCHES" }, // Виски Glennafola 50 Cask Strength
            ["WG_HAUTE_WARDROBE"] = new List<string> { "WG_CIGARS", "WG_ALCOHOL" }, // Гардероб от Христо Дювалье
            ["WG_SPORTCAR"] = new List<string> { "WG_HAUTE_WARDROBE" }, // Спорткар Lussuria Attlantic

            ["BLDG_EVENTS_STAGE"] = new List<string> { "BLDG_ESCORT_DOMINION" }, // Офис организаций мероприятий 

            ["OFFICIAL_RECEPTION_1"] = new List<string> { "BLDG_EVENTS_STAGE" }, // Банкет
            ["OFFICIAL_RECEPTION_2"] = new List<string> { "OFFICIAL_RECEPTION_1" }, // Роскошный банкет
            ["OFFICIAL_RECEPTION_3"] = new List<string> { "OFFICIAL_RECEPTION_2" }, // Грандиозный банкет

            ["PARTY_1"] = new List<string> { "BLDG_EVENTS_STAGE" }, // Корпоратив
            ["PARTY_2"] = new List<string> { "PARTY_1" }, // Роскошный корпоратив
            ["PARTY_3"] = new List<string> { "PARTY_2" }, // Грандиозный корпоратив

            ["INSURANCE_PLUS"] = new List<string> { "BLDG_ESCORT_DOMINION" }, // Расширенная медстраховка 
            ["PERSONAL_DRIVER"] = new List<string> { "INSURANCE_PLUS" }, // Автомобиль с водителем
            ["PERSONAL_DRIVER_PREMIUM"] = new List<string> { "PERSONAL_DRIVER" }, // Роскошный автомобиль с водителем

            ["HOTEL_SUITE"] = new List<string> { "INSURANCE_PLUS" }, // Номер в отеле
            ["PENTHOUSE"] = new List<string> { "HOTEL_SUITE" }, // Пентхаус
            ["VILLA"] = new List<string> { "HOTEL_SUITE" }, // Вилла

            ["HOUSEMAID"] = new List<string> { "INSURANCE_PLUS" }, // Горничная
            ["NANNY"] = new List<string> { "HOUSEMAID" }, // Няня
            ["CHEF"] = new List<string> { "HOUSEMAID" }, // Шеф повар
            ["BUTLER"] = new List<string> { "CHEF" }, // Дворецкий
            ["ASSISTANT"] = new List<string> { "HOUSEMAID" }, // Ассистент
            ["SPOUSES_ASSISTANT"] = new List<string> { "ASSISTANT" }, // Ассистент для жены или мужа

            // Нелегальные подарки
            ["BG_UNLOCK"] = new List<string> { "BLDG_ESCORT_DOMINION" }, // Нелегальные подарки 
            ["BG_NARCOTICS"] = new List<string> { "BG_UNLOCK" }, // Героин
            ["BG_METH"] = new List<string> { "BG_NARCOTICS" }, // Метамфетамин
            ["BG_NARCOTICS_2"] = new List<string> { "BG_NARCOTICS" }, // Кокаин
            ["BG_SAFARI"] = new List<string> { "BG_NARCOTICS_2" }, // Сафари
            ["BG_KILLING"] = new List<string> { "BG_SAFARI" }, // Охота в стиле пилигримов
            ["BG_XXX"] = new List<string> { "BG_NARCOTICS_2" }, // Пикантная кинопленка
            ["BG_BRAINS"] = new List<string> { "BG_XXX" }, // Обезьяньи мозги
            ["BG_CANNIBAL"] = new List<string> { "BG_XXX", "BG_SAFARI" }, // Блюдо из человечины
            ["BG_UNDERAGE"] = new List<string> { "BG_CANNIBAL" }, // Время с несовершеннолетним

            // ============ ОТДЕЛ БЕЗОПАСНОСТИ ============

            ["SECURITY_SCHOOL"] = new List<string>(), // Тренировка тихарей 
            ["SECURITY_SCHOOL_FAST"] = new List<string> { "SECURITY_SCHOOL" }, // Ускоренная тренировка
            ["SECURITY_SCHOOL_STRONG"] = new List<string> { "SECURITY_SCHOOL_FAST" }, // Эффективная тренировка

            ["BLDG_SHENANIGANS"] = new List<string>(), // Офис нападений 

            ["SPYING_SINS"] = new List<string> { "BLDG_SHENANIGANS" }, // Компромат
            ["SPYING_ILLEGALPREFERENCES"] = new List<string> { "BLDG_SHENANIGANS" }, // Нелегальные предпочтения
            ["SPYING_XP_BONUS_1"] = new List<string> { "SPYING_SINS", "SPYING_ILLEGALPREFERENCES" }, // На 50% опыта за найденную информацию
            ["SPYING_XP_BONUS_2"] = new List<string> { "SPYING_XP_BONUS_1" }, // Вдвое больше опыта за найденную информацию
            ["FAIL_NO_DISCLOSURE"] = new List<string> { "SPYING_XP_BONUS_1" }, // Поиск информации без риска разоблачения

            ["SHENANIGANS_BEATING"] = new List<string> { "BLDG_SHENANIGANS" }, // Избиение
            ["SHENANIGANS_KIDNAPPING"] = new List<string> { "BLDG_SHENANIGANS" }, // Похищение
            ["SHENANIGANS_MURDER"] = new List<string> { "BLDG_SHENANIGANS" }, // Убийства
            ["LEAK_RISK_REDUCE_1"] = new List<string> { "SHENANIGANS_BEATING", "SHENANIGANS_KIDNAPPING", "SHENANIGANS_MURDER" }, // Надежное прикрытие

            ["BLDG_SPIES"] = new List<string>(), // Офис защиты 

            ["ACTIVE_PROTECTION"] = new List<string> { "BLDG_SPIES" }, // Усиленная защита
            ["ACTIVE_PROTECTION_XP_BONUS_1"] = new List<string> { "ACTIVE_PROTECTION" }, // На 50% больше опыта за отраженную атаку
            ["ACTIVE_PROTECTION_XP_BONUS_2"] = new List<string> { "ACTIVE_PROTECTION_XP_BONUS_1" }, // Вдвое больше опыта за отраженную атаку

            ["SECRETS_HIDE_EFFECT_BOOST"] = new List<string> { "BLDG_SPIES" }, // Хранитель секретов
            ["FAIL_DISCLOSURE_NO_LEAK"] = new List<string> { "SECRETS_HIDE_EFFECT_BOOST" } // Заметание следов

        };

        public static List<Department> GetDepartments(ObservableCollection<string> availablePerks, ObservableCollection<string> openedPerks)
        {
            var departments = new List<Department>();

            // ============ ОТДЕЛ ТЕХНОЛОГИЙ ============

            departments.Add(new Department
            {
                Name = "TECH",
                DisplayName = "TECHNOLOGY DEPARTMENT",
                MaxLevel = 8,
                Techs = new List<string>{

                    "STUDIO_TECH",
                    "STUDIO_TECH_RED_TIME_1",
                    "STUDIO_TECH_RED_TIME_2",
                    "STUDIO_TECH_ADD_RND",
                    "BLDG_RND_I",
                    "BLDG_RND_II",
                    "BLDG_RND_III",
                    "BLDG_RND_IV"
                }
            });

            // ============ ОТДЕЛ ПРОДАКШНА ============

            departments.Add(new Department
            {
                Name = "PRODUCTION",
                DisplayName = "PRODUCTION DEPARTMENT",
                MaxLevel = 14,
                Techs = new List<string>{

                    "PROD_DIR_CIN_ACT_XP_1",
                    "BLDG_PAVILION_II",
                    "BLDG_PAVILION_III",
                    "BLDG_PAVILION_IV",
                    "BLDG_LOGISTICS",
                    "TEAM_SERVICE_1",
                    "TEAM_SERVICE_2",
                    "BLDG_LINE_PRODUCTION",
                    "SECOND_UNIT",
                    "URGENT_DOUBLE_SEARCH",
                    "URGENT_EXTRAS_SEARCH",
                    "URGENT_LOCATION_SEARCH",
                    "URGENT_CREW_SEARCH",
                    "FLEX_SCHEDULE"
                }
            });

            // ============ ОТДЕЛ ПРОДЮСИРОВАНИЯ ============

            departments.Add(new Department
            {
                Name = "PRODUCING",
                DisplayName = "PRODUCING DEPARTMENT",
                MaxLevel = 6,
                Techs = new List<string>{

                    "NEGOTIATION_SCALE_50",
                    "NEGOTIATION_SCALE_75",
                    "TWO_PROJECTS",
                    "PRODUCERS_ON_FILM_2",
                    "PRODUCERS_ON_FILM_3",
                    "CONTRACT_WEIGHT"
                }
            });

            // ============ ЮРИДИЧЕСКИЙ ОТДЕЛ ============

            departments.Add(new Department
            {
                Name = "LEGAL",
                DisplayName = "LEGAL DEPARTMENT",
                MaxLevel = 10,
                Techs = new List<string>{

                "LEGAL_DEFENSE_1",
                "LEGAL_DEFENSE_2",
                "LEGAL_DEFENSE_3",
                "CONTRACT_TERMINATION_FEE_1",
                "CONTRACT_TERMINATION_FEE_2",
                "CONTRACT_PAYMENTS_50_50",
                "CONTRACT_5_MOVIES",
                "CONTRACT_10_MOVIES",
                "CONTRACT_5_YEARS",
                "CONTRACT_10_YEARS"
                }
            });

            // ============ ФИНАНСОВЫЙ ОТДЕЛ ============
            departments.Add(new Department
            {
                Name = "FINANCE",
                DisplayName = "FINANCIAL DEPARTMENT",
                MaxLevel = 10,
                Techs = new List<string>{

                "BANK_LOAN",                           // Кредит на 1.000.000$
                "CASH_FLOW_1",                         // $500 наличные в месяц
                "CASH_FLOW_2",                         // $1.000 наличные в месяц
                "BANK_LOAN_EARLY_REPAYMENT",           // Досрочное погашение
                "BANK_LOAN_INT_RATE_REDUCTION_1",      // Кредит под 18%
                "BANK_LOAN_INT_RATE_REDUCTION_2",      // Кредит под 14%
                "BANK_LOAN_AMOUNT_1",                  // Кредит на $2.000.000
                "BANK_LOAN_AMOUNT_2",                  // Кредит на $6.000.000
                "BANK_LOAN_TERM_1",                    // Кредит до 3 лет
                "BANK_LOAN_TERM_2",                    // Кредит до 5 лет
                }
            });

            // ============ ОТДЕЛ HR ============
            departments.Add(new Department
            {
                Name = "HR",
                DisplayName = "DEPARTMENT HR",
                MaxLevel = 12,
                Techs = new List<string>{

                "BLDG_ESCORT_DOMINION",           // Отдел обеспечения комфорта 
                "IMPROVEMENT_0_NO_SADNESS",       // Аскетичные сотрудники
                "HIRING_BONUSES",                 // Восторженные новички
                "NOMINATION_LOSS_NO_SADNESS",     // Позитивный настрой
                "MOVIE_RELEASE_MOOD_BOOST",       // Профессиональная реализация
                "BAD_ATTITUDE_NO_SADNESS",        // Философский подход
       
                "ETHNIC_COMPOSITION",             // Этнический состав 
                "ILLEGAL_WORKERS",                // Нелегалы
                "CHEAP_ILLEGALS",                 // Дешевые нелегалы
                "BUILDINGS_CONSERVATION",         // Консервация зданий
                "CONSERVATION_COOLDOWN",          // Дешевый простой
                "STAFF_LARGE1"                    // Гибкая консервация
                }
            });

            // ============ ОТДЕЛ ПО СВЯЗЯМ С ОБЩЕСТВЕННОСТЬЮ ============

            departments.Add(new Department
            {
                Name = "PR",
                DisplayName = "PUBLIC RELATIONS DEPARTMENT",
                MaxLevel = 13,
                Techs = new List<string>
                {
                    "CHARITY_TO_REP",                      // Благотворительность
        
                    "GENERATION_IP_AND_REP",               // Генерация ОВ или репутация вместо изучения улучшений 
                    "GENERATION_IP_X2",                    // Вдвое больше ОВ за генерацию
                    "GENERATION_REP_X2",                   // Вдвое больше репутации за генерацию
        
                    "PROFITABLE_MOVIE_REP_2",              // Двойная репутация за прибыльные фильмы 
        
                    "TOP1_TOP3",                           // ОВ и репутация за топ-3
                    "TECH_SALE_PP",                        // Вдвое больше ОВ за продажу технологий
        
                    "MOVIE_RELEASE_ATTITUDE_1",            // Репутация за хорошее отношение к студии
                    "INITIATIVE_PP_FREE",                  // Владение инициативой
                    "MOVIE_RELEASE_MOOD_1",                // Вдвое больше репутации за хорошее отношение к студии
                    "ICON_REP_1",                          // Вдвое больше репутации за Икону
                    "LEGEND_REP_1",                        // Вдвое больше репутации за Легенду
                    "SKILLED_ACTOR_REP"                    // Репутация за найм актеров
                }
            });

            // ============ ОТДЕЛ ПОСТПРОДАКШЕНА ============

            departments.Add(new Department
            {
                Name = "POST",
                DisplayName = "POST-PRODUCTION DEPARTMENT",
                MaxLevel = 10,
                Techs = new List<string>{

                    "POST_DIR_MONT_COMP_XP_1",               // Двойной опыт за постпродакшен
        
                    "BLDG_LAB",                              // Кинолаборатория 
                    "LAB_INHOUSE_IMPROVED",                  // Улучшенная проявка
                    "LAB_INHOUSE_TIME_1",                    // Ускоренная проявка
        
                    "BLDG_SOUND",                            // Студия звукозаписи 
                    "SOUND_INHOUSE_IMPROVED",                // Улучшенная студия звукозаписи
                    "SOUND_INHOUSE_TIME_1",                  // Ускоренная работа студии звукозаписи
        
                    "BLDG_CONCERT",                          // Оркестровый зал 
                    "CONCERT_INHOUSE_MPROVED",               // Улучшенный оркестровый зал
                    "CONCERT_INHOUSE_TIME_1"                 // Ускоренная работа оркестрового зала
                }
            });

            // ============ ОТДЕЛ ПРОКАТА ============

            departments.Add(new Department
            {
                Name = "DISTRIBUTION",
                DisplayName = "RENTAL DEPARTMENT",
                MaxLevel = 28,
                Techs = new List<string>{

                    "BLDG_DISTRIBUTION",                     // Управление кинотеатрами 
                    "MOVIE_THEATRE_SLOT_ADD_1",              // Оптимизация кинотеатров
                    "MOVIE_THEATRE_SLOT_RENT",               // Кинотеатры в аренду
        
                    "BLDG_ANALYTICS",                        // Аналитический офис 
                    "ANALYSIS_GROUPS",                       // Аналитика аудитории
                    "POSTRELEASE_ANALYSIS",                  // Аналитика после проката
                    "ANALYSIS_ENTIRE_CAST",                  // Актеры конкурентов
                    "ANALYSIS_SCREENPLAY",                   // Оценки сценариев конкурентов
                    "ANALYSIS_TAGS",                         // Теги конкурентов
                    "ANALYSIS_BUDGET",                       // Бюджеты конкурентов
        
                    "BLDG_PRINT",                            // Печатный офис 
                    "PRINT_EMERGENCY",                       // Экстренная печать фильмокопий
                    "PRINT_INHOUSE_QLT_1",                   // Печать за 3 недели
                    "PRINT_INHOUSE_QLT_2",                   // Печать за неделю
        
                    "BLDG_MARKETING",                        // Офис продвижения 
                    "SCANDAL_COVER_UP_MONEY",                // Замять скандал за деньги
                    "SCANDAL_COVER_UP_PP",                   // Замять скандал за ОВ

                    "WM_HOSPICE",                            // Визит в хоспис
                    "WM_ORPHANAGE",                          // Визит в детский дом
                    "WM_WEDDING",                            // Внезапное появление на свадьбе
                    "WM_HOMELESS",                           // Помощь бездомным
                    "WM_DEBT",                               // Оплата долгов

                    "BM_UNLOCK",                             // Спасение утопающего 
                    "BM_DROWNING",                           // Спасение утопающего
                    "BM_DRUNKARD",                           // Помощь пьянице
                    "BM_FIGHT",                              // Защита прохожего
                    "BM_CRIMINAL",                           // Поимка опасного преступника
                    "BM_HOUSE_BURN"                          // Помощь погорельцам
                }
            });

            // ============ ОТДЕЛ ИНФРАСТРУКТУРЫ ============

            departments.Add(new Department
            {
                Name = "INFRASTRUCTURE",
                DisplayName = "INFRASTRUCTURE DEPARTMENT",
                MaxLevel = 4,
                Techs = new List<string>{

                    "BLDG_WATER_TOWER_I",
                    "BLDG_POWERPLANT_I",
                    "REPAIR_TEAM_1",
                    "IMPROVEMENT_I",
                }
            });

            // ============ СЦЕНАРНЫЙ ОТДЕЛ ============

            departments.Add(new Department
            {
                Name = "SCRIPT",
                DisplayName = "SCRIPT DEPARTMENT",
                MaxLevel = 44,
                Techs = new List<string>{

                    "EDITS_ON_GO",

                    "SCREENPLAY_TIME_RED_1",
                    "SCREENPLAY_TIME_RED_2",
                    "SCREENPLAY_TIME_RED_3",

                    "NEW_SCREENPLAY_PP_BONUS_1",
                    "NEW_SCREENPLAY_PP_BONUS_2",

                    "NEW_SCREENPLAY_XP_BONUS_1",
                    "NEW_SCREENPLAY_XP_BONUS_2",
                    "NEW_SCREENPLAY_XP_BONUS_3",

                    "SCEN_IDEAS_STORAGE_1",
                    "SCEN_IDEAS_GEN_AMT_1",
                    "SCEN_IDEAS_GEN_AMT_2",

                    "MOVIE_RELEASE_XP_1",
                    "MOVIE_RELEASE_XP_2",
                    "MOVIE_RELEASE_XP_3",
                    "MOVIE_RELEASE_TOP10_AUD_XP_1",
                    "MOVIE_RELEASE_TOP10_COM_XP_1",
                    "MOVIE_RELEASE_TOP10_ART_XP_1",

                    "BLDG_CONSTRUCTOR",
                    "MOVIE_SEQUEL",
                    "MOVIE_SEQUEL_ORIGINALITY",
                    "MOVIE_SEQUEL_LEGACY",

                    "TAGS_RESEARCH",
                    "TAGS_RESEARCH_DIRECTION",
                    "TAGS_SLOTS_6",
                    "TAGS_SLOTS_7",
                    "TAGS_SLOTS_8",
                    "TAGS_SLOTS_9",
                    "TAGS_SLOTS_10",

                    "NEW_TAG_BY_LT_1",
                    "NEW_TAG_BY_LT_2",

                    "TAGS_RESEARCH_TIME_RED_1",
                    "TAGS_RESEARCH_TIME_RED_2",
                    "TAGS_RESEARCH_TIME_RED_3",

                    "TAGS_XP_BONUS_1",
                    "TAGS_XP_BONUS_2",
                    "TAGS_XP_BONUS_3",

                    "TAGS_NEW_PP_BONUS",

                    "BLDG_FREELANCE",
                    "SCRIPT_DOCTORS",
                    "SCRIPT_DOCTORS_FASTER",
                    "SCRIPT_DOCTORS_CHEAPER",
                    "SCRIPT_DOCTORS_RANGE",
                    "SCRIPT_DOCTORS_SCORES"
                }
            });

            // ============ ОТДЕЛ ПРЕПРОДАКШНА ============

            departments.Add(new Department
            {
                Name = "PREPROD",
                DisplayName = "PRE-PRODUCTION DEPARTMENT",
                MaxLevel = 21,
                Techs = new List<string>{

                    // Офис технического снабжения
                    "BLDG_SUPPLY",

                    "BLDG_WORKSHOP", // Художественные мастерские
                    "SETS_QLT_2",
                    "SETS_QLT_3",
                    "PROPS_QLT_2",
                    "PROPS_QLT_3",
        
                    // Скорость работы художественных мастерских
                    "SETS_TIME_RED_1",
                    "SETS_TIME_RED_2",
                    "SETS_TIME_RED_3",

                    "BLDG_SCOUT", // Скаут офис
                    "LOCATION_QLT_1",
                    "LOCATION_QLT_2",
                    "LOCATION_SEARCH_TIME_1",
                    "LOCATION_SEARCH_TIME_2",
                    "LOCATION_SEARCH_WORLD",

                    "BLDG_CASTING", // Кастинг офис
                    "PREPROD_PROD_DIR_CIN_XP_1",
                    "PREPROD_PROD_DIR_CIN_XP_2",
        
                    // Массовка
                    "EXTRAS_2",
                    "EXTRAS_3",
                    "EXTRAS_4"
                }
            });

            // ============ ОТДЕЛ ОБЕСПЕЧЕНИЯ КОМФОРТА ============

            departments.Add(new Department
            {
                Name = "COMFORT",
                DisplayName = "COMFORT DEPARTMENT",
                MaxLevel = 34,
                Techs = new List<string>{

                    "WG_WATCHES",
                    "WG_CIGARS",
                    "WG_ALCOHOL",
                    "WG_HAUTE_WARDROBE",
                    "WG_SPORTCAR",

                    "BLDG_EVENTS_STAGE",

                    "OFFICIAL_RECEPTION_1",
                    "OFFICIAL_RECEPTION_2",
                    "OFFICIAL_RECEPTION_3",

                    "PARTY_1",
                    "PARTY_2",
                    "PARTY_3",

                    "INSURANCE_PLUS",
                    "PERSONAL_DRIVER",
                    "PERSONAL_DRIVER_PREMIUM",

                    "HOTEL_SUITE",
                    "PENTHOUSE",
                    "VILLA",

                    "HOUSEMAID",
                    "NANNY",
                    "CHEF",
                    "BUTLER",
                    "ASSISTANT",
                    "SPOUSES_ASSISTANT",
        
                    // Нелегальные подарки
                    "BG_UNLOCK",
                    "BG_NARCOTICS",
                    "BG_METH",
                    "BG_NARCOTICS_2",
                    "BG_SAFARI",
                    "BG_KILLING",
                    "BG_XXX",
                    "BG_BRAINS",
                    "BG_CANNIBAL",
                    "BG_UNDERAGE"
                }
            });

            // ============ ОТДЕЛ БЕЗОПАСНОСТИ ============
            departments.Add(new Department
            {
                Name = "SECURITY",
                DisplayName = "SECURITY DEPARTMENT",
                MaxLevel = 19,
                Techs = new List<string>{

                    "SECURITY_SCHOOL",
                    "SECURITY_SCHOOL_FAST",
                    "SECURITY_SCHOOL_STRONG",
        
                    // Офис нападений
                    "BLDG_SHENANIGANS",


                    "SPYING_SINS",
                    "SPYING_ILLEGALPREFERENCES",
                    "SPYING_XP_BONUS_1",
                    "SPYING_XP_BONUS_2",
                    "FAIL_NO_DISCLOSURE",

                    "SHENANIGANS_BEATING",
                    "SHENANIGANS_KIDNAPPING",
                    "SHENANIGANS_MURDER",
                    "LEAK_RISK_REDUCE_1",
        
                    // Офис защиты
                    "BLDG_SPIES",

                    "ACTIVE_PROTECTION",
                    "ACTIVE_PROTECTION_XP_BONUS_1",
                    "ACTIVE_PROTECTION_XP_BONUS_2",

                    "SECRETS_HIDE_EFFECT_BOOST",
                    "FAIL_DISCLOSURE_NO_LEAK"
    }
            });

            // Подсчет текущего уровня для каждого отдела
            foreach (var dept in departments)
            {
                int openedCount = 0;
                foreach (var tech in dept.Techs)
                {
                    if (openedPerks.Contains(tech))
                    {
                        openedCount++;
                    }
                }
                dept.CurrentLevel = openedCount;
            }

            return departments;
        }

        //Список всех перков, которые присутствуют в игре. Прежде чем с ними взаимодействовать, то сначала
        //надо добавить в этот список. -> 0.8.68EA
        public static List<string> PreGenPerks => new List<string>()
        {
            // ============ ФИНАНСОВЫЙ ОТДЕЛ ============
            "BANK_LOAN",
            "CASH_FLOW_1",
            "CASH_FLOW_2",
            "BANK_LOAN_EARLY_REPAYMENT",
            "BANK_LOAN_INT_RATE_REDUCTION_1",
            "BANK_LOAN_INT_RATE_REDUCTION_2",
            "BANK_LOAN_AMOUNT_1",
            "BANK_LOAN_AMOUNT_2",
            "BANK_LOAN_TERM_1",
            "BANK_LOAN_TERM_2",
    
            // ============ ОТДЕЛ ТЕХНОЛОГИЙ ============
            "STUDIO_TECH",
            "STUDIO_TECH_RED_TIME_1",
            "STUDIO_TECH_RED_TIME_2",
            "STUDIO_TECH_ADD_RND",
            "BLDG_RND_I",
            "BLDG_RND_II",
            "BLDG_RND_III",
            "BLDG_RND_IV",
    
            // ============ ОТДЕЛ ПРОДАКШНА ============
            "PROD_DIR_CIN_ACT_XP_1",
            "BLDG_PAVILION_II",
            "BLDG_PAVILION_III",
            "BLDG_PAVILION_IV",
            "BLDG_LOGISTICS",
            "TEAM_SERVICE_1",
            "TEAM_SERVICE_2",
            "BLDG_LINE_PRODUCTION",
            "SECOND_UNIT",
            "URGENT_DOUBLE_SEARCH",
            "URGENT_EXTRAS_SEARCH",
            "URGENT_LOCATION_SEARCH",
            "URGENT_CREW_SEARCH",
            "FLEX_SCHEDULE",
    
            // ============ ОТДЕЛ ПРОДЮСИРОВАНИЯ ============
            "NEGOTIATION_SCALE_50",
            "NEGOTIATION_SCALE_75",
            "TWO_PROJECTS",
            "PRODUCERS_ON_FILM_2",
            "PRODUCERS_ON_FILM_3",
            "CONTRACT_WEIGHT",
    
            // ============ ЮРИДИЧЕСКИЙ ОТДЕЛ ============
            "LEGAL_DEFENSE_1",
            "LEGAL_DEFENSE_2",
            "LEGAL_DEFENSE_3",
            "CONTRACT_TERMINATION_FEE_1",
            "CONTRACT_TERMINATION_FEE_2",
            "CONTRACT_PAYMENTS_50_50",
            "CONTRACT_5_MOVIES",
            "CONTRACT_10_MOVIES",
            "CONTRACT_5_YEARS",
            "CONTRACT_10_YEARS",
            "CONTRACT_TERMINATION_FREE",
    
            // ============ ОТДЕЛ HR ============
            "BLDG_ESCORT_DOMINION",
            "ETHNIC_COMPOSITION",
            "ILLEGAL_WORKERS",
            "CHEAP_ILLEGALS",
            "STAFF_LARGE1",
            "STAFF_LARGE2",
            "BUILDINGS_CONSERVATION",
            "CONSERVATION_COOLDOWN",
            "SALARY_CUT",
            "IMPROVEMENT_0_NO_SADNESS",
            "HIRING_BONUSES",
            "NOMINATION_LOSS_NO_SADNESS",
            "MOVIE_RELEASE_MOOD_BOOST",
            "BAD_ATTITUDE_NO_SADNESS",
            "PERSONNEL_X2",
    
            // ============ ОТДЕЛ ПО СВЯЗЯМ С ОБЩЕСТВЕННОСТЬЮ ============
            "CHARITY_TO_REP",
            "PROFITABLE_MOVIE_REP_2",
            "GENERATION_IP_AND_REP",
            "GENERATION_IP_X2",
            "GENERATION_REP_X2",
            "GOOD_ATTITUDE_REP_1",
            "GOOD_ATTITUDE_REP_2",
            "ICON_REP_1",
            "LEGEND_REP_1",
            "SKILLED_ACTOR_REP",
            "TOP1_TOP3",
            "TECH_SALE_PP",
            "INITIATIVE_PP_FREE",
            "MOVIE_RELEASE_ATTITUDE_1",
            "MOVIE_RELEASE_MOOD_1",
    
            // ============ ОТДЕЛ ПОСТПРОДАКШНА ============
            "POST_DIR_MONT_COMP_XP_1",
            "BLDG_LAB",
            "LAB_INHOUSE_IMPROVED",
            "LAB_INHOUSE_TIME_1",
            "BLDG_SOUND",
            "SOUND_INHOUSE_IMPROVED",
            "SOUND_INHOUSE_TIME_1",
            "BLDG_CONCERT",
            "CONCERT_INHOUSE_MPROVED",
            "CONCERT_INHOUSE_TIME_1",
    
            // ============ ОТДЕЛ ПРОКАТА ============
            "BLDG_DISTRIBUTION",
            "MOVIE_THEATRE_SLOT_ADD_1",
            "MOVIE_THEATRE_SLOT_RENT",
            "MOVIE_PALACE",
            "BLDG_ANALYTICS",
            "ANALYSIS_GROUPS",
            "POSTRELEASE_ANALYSIS",
            "ANALYSIS_ENTIRE_CAST",
            "ANALYSIS_SCREENPLAY",
            "ANALYSIS_TAGS",
            "ANALYSIS_BUDGET",
            "BLDG_PRINT",
            "PRINT_EMERGENCY",
            "PRINT_INHOUSE_QLT_1",
            "PRINT_INHOUSE_QLT_2",
            "BLDG_MARKETING",
            "SCANDAL_COVER_UP_MONEY",
            "SCANDAL_COVER_UP_PP",
            "WM_HOSPICE",
            "WM_ORPHANAGE",
            "WM_WEDDING",
            "WM_HOMELESS",
            "WM_DEBT",
            "BM_UNLOCK",
            "BM_DROWNING",
            "BM_DRUNKARD",
            "BM_FIGHT",
            "BM_CRIMINAL",
            "BM_HOUSE_BURN",
            "PREMIERE",
            "SUPER_PREMIERE",
            "MARKET_INTERVIEW",
    
            // ============ СЦЕНАРНЫЙ ОТДЕЛ ============
            "EDITS_ON_GO",
            "SCREENPLAY_TIME_RED_1",
            "SCREENPLAY_TIME_RED_2",
            "SCREENPLAY_TIME_RED_3",
            "NEW_SCREENPLAY_PP_BONUS_1",
            "NEW_SCREENPLAY_PP_BONUS_2",
            "NEW_SCREENPLAY_XP_BONUS_1",
            "NEW_SCREENPLAY_XP_BONUS_2",
            "NEW_SCREENPLAY_XP_BONUS_3",
            "SCEN_IDEAS_STORAGE_1",
            "SCEN_IDEAS_GEN_AMT_1",
            "SCEN_IDEAS_GEN_AMT_2",
            "MOVIE_RELEASE_XP_1",
            "MOVIE_RELEASE_XP_2",
            "MOVIE_RELEASE_XP_3",
            "MOVIE_RELEASE_TOP10_AUD_XP_1",
            "MOVIE_RELEASE_TOP10_COM_XP_1",
            "MOVIE_RELEASE_TOP10_ART_XP_1",
            "MOVIE_SEQUEL",
            "MOVIE_SEQUEL_ORIGINALITY",
            "MOVIE_SEQUEL_LEGACY",
            "BLDG_COPYRIGHT",
            "PRINT_MEDIA",
            "BROADCAST_MEDIA",
            "PUBLIC_DOMAIN",
            "LITERARY_WORK_RESEARCH_TIME_1",
            "TAGS_RESEARCH",
            "TAGS_RESEARCH_DIRECTION",
            "TAGS_SLOTS_6",
            "TAGS_SLOTS_7",
            "TAGS_SLOTS_8",
            "TAGS_SLOTS_9",
            "TAGS_SLOTS_10",
            "NEW_TAG_BY_LT_1",
            "NEW_TAG_BY_LT_2",
            "NEW_TAG_BY_LT_3",
            "TAGS_RESEARCH_TIME_RED_1",
            "TAGS_RESEARCH_TIME_RED_2",
            "TAGS_RESEARCH_TIME_RED_3",
            "TAGS_XP_BONUS_1",
            "TAGS_XP_BONUS_2",
            "TAGS_XP_BONUS_3",
            "TAGS_NEW_PP_BONUS",
            "BLDG_FREELANCE",
            "SCREENPLAYS_AMT_1",
            "SCREENPLAYS_AMT_2",
            "SCRIPT_DOCTORS",
            "SCRIPT_DOCTORS_FASTER",
            "SCRIPT_DOCTORS_CHEAPER",
            "SCRIPT_DOCTORS_RANGE",
            "SCRIPT_DOCTORS_SCORES",
    
            // ============ ОТДЕЛ ПРЕПРОДАКШНА ============
            "BLDG_SUPPLY",
            "BLDG_WORKSHOP",
            "SETS_QLT_2",
            "SETS_QLT_3",
            "PROPS_QLT_2",
            "PROPS_QLT_3",
            "SETS_TIME_RED_1",
            "SETS_TIME_RED_2",
            "SETS_TIME_RED_3",
            "BLDG_SCOUT",
            "LOCATION_QLT_1",
            "LOCATION_QLT_2",
            "LOCATION_SEARCH_TIME_1",
            "LOCATION_SEARCH_TIME_2",
            "LOCATION_SEARCH_WORLD",
            "BLDG_CASTING",
            "PREPROD_PROD_DIR_CIN_XP_1",
            "PREPROD_PROD_DIR_CIN_XP_2",
            "EXTRAS_2",
            "EXTRAS_3",
            "EXTRAS_4",
            "ADDITIONAL_REHEARSAL_1",
            "ADDITIONAL_REHEARSAL_2",
    
            // ============ ОТДЕЛ ОБЕСПЕЧЕНИЯ КОМФОРТА ============
            "WG_WATCHES",
            "WG_CIGARS",
            "WG_ALCOHOL",
            "WG_HAUTE_WARDROBE",
            "WG_SPORTCAR",
            "BLDG_EVENTS_STAGE",
            "OFFICIAL_RECEPTION_1",
            "OFFICIAL_RECEPTION_2",
            "OFFICIAL_RECEPTION_3",
            "PARTY_1",
            "PARTY_2",
            "PARTY_3",
            "INSURANCE_PLUS",
            "PERSONAL_DRIVER",
            "PERSONAL_DRIVER_PREMIUM",
            "HOTEL_SUITE",
            "PENTHOUSE",
            "VILLA",
            "HOUSEMAID",
            "NANNY",
            "CHEF",
            "BUTLER",
            "ASSISTANT",
            "SPOUSES_ASSISTANT",
            "BG_UNLOCK",
            "BG_NARCOTICS",
            "BG_METH",
            "BG_NARCOTICS_2",
            "BG_SAFARI",
            "BG_KILLING",
            "BG_XXX",
            "BG_BRAINS",
            "BG_CANNIBAL",
            "BG_UNDERAGE",
    
            // ============ ОТДЕЛ БЕЗОПАСНОСТИ ============
            "SECURITY_SCHOOL",
            "SECURITY_SCHOOL_FAST",
            "SECURITY_SCHOOL_STRONG",
            "BLDG_SHENANIGANS",
            "SPYING_SINS",
            "SPYING_ILLEGALPREFERENCES",
            "SPYING_XP_BONUS_1",
            "SPYING_XP_BONUS_2",
            "FAIL_NO_DISCLOSURE",
            "SHENANIGANS_BEATING",
            "SHENANIGANS_KIDNAPPING",
            "SHENANIGANS_MURDER",
            "LEAK_RISK_REDUCE_1",
            "BLDG_SPIES",
            "ACTIVE_PROTECTION",
            "ACTIVE_PROTECTION_XP_BONUS_1",
            "ACTIVE_PROTECTION_XP_BONUS_2",
            "SECRETS_HIDE_EFFECT_BOOST",
            "FAIL_DISCLOSURE_NO_LEAK",
            "PASSIVE_PROTECTION_1",
            "PASSIVE_PROTECTION_2",
            "PASSIVE_PROTECTION_3",
    
            // ============ ОТДЕЛ ИНФРАСТРУКТУРЫ ============
            "BLDG_WATER_TOWER_I",
            "BLDG_WATER_TOWER_II",
            "BLDG_WATER_TOWER_III",
            "WATER_TOWER_AMT_3",
            "BLDG_POWERPLANT_I",
            "BLDG_POWERPLANT_II",
            "BLDG_POWERPLANT_III",
            "POWERPLANT_AMT_3",
            "IMPROVEMENT_I",
            "IMPROVEMENT_II",
            "IMPROVEMENT_III",
            "REPAIR_TEAM_1",
    
            // ============ ПРОЧИЕ (ВОЗМОЖНО БУДУЮЩИЕ, ЛИБО УДАЛЕННЫЕ) ============
            "BLDG_CONSTRUCTOR",
            "MOVIEGOERS_NUMBER_WIDE",
            "MOVIEGOERS_NUMBER_NARROW",
            "ANALYSIS_VISION_DEPTH_1",
            "ANALYSIS_VISION_DEPTH_2",
            "SPYING_LVL_2"
        };
    };
}
