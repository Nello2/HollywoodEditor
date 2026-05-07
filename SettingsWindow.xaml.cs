using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HollywoodEditor
{
    public partial class SettingsWindow : Window
    {
        private bool IsRussianLocale
        {
            get
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
                    var appLocale = Convert.ToString(Application.Current.Properties["Locale"]);
                    if (!string.IsNullOrWhiteSpace(appLocale))
                    {
                        appLocale = appLocale.Trim().ToUpperInvariant();
                        if (appLocale == "RUS" || appLocale == "RU" || appLocale == "RU-RU") return true;
                        if (appLocale == "ENG" || appLocale == "EN" || appLocale == "EN-US") return false;
                    }
                }

                try
                {
                    var budgetText = Application.Current?.TryFindResource("Budget") as string;
                    if (budgetText == "Банк") return true;
                    if (budgetText == "Budget") return false;
                }
                catch { }

                return false;
            }
        }
        private string L(string en, string ru) => IsRussianLocale ? ru : en;


        private void ApplyLocalization()
        {
            Title = L("Game Settings Editor", "Редактор настроек игры");
            if (HeaderText != null) HeaderText.Text = L("⚙️ Game Settings", "⚙️ Настройки игры");
            if (SaveButton != null) SaveButton.Content = L("Save", "Сохранить");
            if (CancelButton != null) CancelButton.Content = L("Cancel", "Отмена");
            RestoreButton.Content = L("Restore", "Восстановить");
        }

        private string configFilePath;
        private string gameVariablesPath;
        private string buildingsPath;
        private string currentConfigName = "GameVariables";
        private string previousConfigName = "GameVariables";
        private bool closeOnLoaded;
        private bool suppressConfigSelectionChanged;
        private JObject configData;
        private Dictionary<string, GameVariable> variables;
        private readonly Dictionary<string, FieldStats> fieldStats = new Dictionary<string, FieldStats>();
        private readonly Dictionary<string, CategoryInfo> categories = new Dictionary<string, CategoryInfo>
        {
            ["beating"] = new CategoryInfo { Title = "Beating", Icon = "👊" },
            ["kidnapping"] = new CategoryInfo { Title = "Kidnapping", Icon = "🔪" },
            ["murder"] = new CategoryInfo { Title = "Murder", Icon = "💀" },
            ["threat"] = new CategoryInfo { Title = "Threats", Icon = "⚠️" },
            ["boutique"] = new CategoryInfo { Title = "Boutique School", Icon = "🎓" },
            ["building"] = new CategoryInfo { Title = "Construction", Icon = "🏗️" },
            ["loan"] = new CategoryInfo { Title = "Loans", Icon = "⚖️" },
            ["tax"] = new CategoryInfo { Title = "Taxes", Icon = "📊" },
            ["tech"] = new CategoryInfo { Title = "Technologies", Icon = "⚙️" },
            ["production"] = new CategoryInfo { Title = "Production Core", Icon = "🎬" },
            ["limits"] = new CategoryInfo { Title = "Technical Limits", Icon = "🔧" },
            ["character"] = new CategoryInfo { Title = "Characters", Icon = "👤" },
            ["contract"] = new CategoryInfo { Title = "Contracts", Icon = "📄" },
            ["script"] = new CategoryInfo { Title = "Scripts", Icon = "📜" },
            ["pollux"] = new CategoryInfo { Title = "Pollux Award", Icon = "🏆" },
            ["reputation"] = new CategoryInfo { Title = "Reputation", Icon = "⭐" },
            ["spying"] = new CategoryInfo { Title = "Espionage", Icon = "👁️" },
            ["protection"] = new CategoryInfo { Title = "Protection", Icon = "🛡️" },
            ["surveillance"] = new CategoryInfo { Title = "Surveillance", Icon = "📹" },
            ["research"] = new CategoryInfo { Title = "Research", Icon = "🔬" },
            ["economy"] = new CategoryInfo { Title = "Economy", Icon = "💰" },
            ["marketing"] = new CategoryInfo { Title = "Marketing", Icon = "📢" },
            ["studio"] = new CategoryInfo { Title = "Studio", Icon = "🎥" },
            ["events"] = new CategoryInfo { Title = "Events", Icon = "🎭" },
            ["hiring"] = new CategoryInfo { Title = "Hiring", Icon = "🤝" },
            ["association"] = new CategoryInfo { Title = "Association", Icon = "🏛️" },
            ["policy"] = new CategoryInfo { Title = "Politics", Icon = "📜" },
            ["nomination"] = new CategoryInfo { Title = "Nominations", Icon = "🏅" },
            ["competitors"] = new CategoryInfo { Title = "Competitors", Icon = "🏢" },
            ["cinema"] = new CategoryInfo { Title = "Cinemas", Icon = "🎪" },
            ["location"] = new CategoryInfo { Title = "Locations", Icon = "🌍" },
            ["extras"] = new CategoryInfo { Title = "Extras", Icon = "👥" },
            ["costumes"] = new CategoryInfo { Title = "Costumes", Icon = "👗" },
            ["sets"] = new CategoryInfo { Title = "Sets", Icon = "🎨" },
            ["voice"] = new CategoryInfo { Title = "Voiceover", Icon = "🎙️" },
            ["editing"] = new CategoryInfo { Title = "Editing", Icon = "✂️" },
            ["effects"] = new CategoryInfo { Title = "Special Effects", Icon = "✨" },
            ["soundtrack"] = new CategoryInfo { Title = "Soundtrack", Icon = "🎵" },
            ["synopsis"] = new CategoryInfo { Title = "Synopsis", Icon = "📝" },
            ["rnd"] = new CategoryInfo { Title = "R&D", Icon = "🔧" },
            ["training"] = new CategoryInfo { Title = "Training", Icon = "📚" },
            ["dismissal"] = new CategoryInfo { Title = "Dismissal", Icon = "🚪" },
            ["lawsuit"] = new CategoryInfo { Title = "Lawsuits", Icon = "⚖️" },
            ["police"] = new CategoryInfo { Title = "Police", Icon = "👮" },
            ["press"] = new CategoryInfo { Title = "Press", Icon = "📰" },
        };

        private readonly Dictionary<string, string> displayNames = new Dictionary<string, string>
        {
            // Избиение
            ["beating_min_agent_skill_duration"] = "Min. agent skill",
            ["beating_max_agent_skill_duration"] = "Max. agent skill",

            // Похищение
            ["kidnapping_min_agent_skill_duration"] = "Min. agent skill",
            ["kidnapping_max_agent_skill_duration"] = "Max. agent skill",
            ["kidnapping_random_duration_offset"] = "Random offset",
            ["ransom_duration_range"] = "Ransom time range",
            ["ransom_family_cash"] = "Ransom from family",
            ["ransom_studio_cash_per_weight"] = "Ransom from studio",

            // Убийство
            ["assasination_min_agent_skill_duration"] = "Min. agent skill",
            ["assasination_max_agent_skill_duration"] = "Max. agent skill",

            // Угрозы
            ["threat_min_agent_skill_duration"] = "Min. agent skill",
            ["threat_max_agent_skill_duration"] = "Max. agent skill",

            // Школа бутика
            ["policy_boutique_school_chance1_months"] = "Training duration (★)",
            ["policy_boutique_school_chance2_months"] = "Training duration (★★)",
            ["policy_boutique_cap_days"] = "Max policy duration",
            ["policy_boutique_prospective_pool_months"] = "Prospective talent pool",

            // Строительство
            ["building_duration_per_point"] = "Construction duration (per point)",
            ["building_cost_per_point"] = "Construction cost (per point)",
            ["building_maintenance_cost_per_point"] = "Maintenance cost (per point)",
            ["building_wear_per_day"] = "Building wear per day",
            ["building_repair_multipliers"] = "Repair multipliers",

            // Кредиты
            ["bank_loan_amount_0"] = "Loan (initial)",
            ["bank_loan_amount_1"] = "Loan (increased)",
            ["bank_loan_amount_2"] = "Loan (maximum)",
            ["bank_loan_term_0"] = "Loan term (initial)",
            ["bank_loan_term_1"] = "Loan term (increased)",
            ["bank_loan_term_2"] = "Loan term (maximum)",
            ["bank_loan_int_rate_0"] = "Interest rate (initial)",
            ["bank_loan_int_rate_1"] = "Interest rate (improved)",
            ["bank_loan_int_rate_2"] = "Interest rate (best)",
            ["bank_loan_cooldown_0"] = "Loan cooldown (initial)",
            ["bank_loan_cooldown_1"] = "Loan cooldown (improved)",

            // Налоги
            ["start_tax_percent"] = "Initial tax rate",
            ["tax_base_reduction_1"] = "Tax reduction (level 1)",
            ["tax_base_reduction_2"] = "Tax reduction (level 2)",

            // Технологии
            ["tech_improvement_days_per_point_average"] = "Tech improvement (average)",
            ["tech_creation_days_per_point_average"] = "Tech creation (average)",
            ["tech_sell_point_cost_base"] = "Base tech sale cost",
            ["tech_improvement_days_per_point_average"] = "Tech improvement (average)",
            ["tech_creation_days_per_point_average"] = "Tech creation (average)",
            ["tech_sell_point_cost_base"] = "Base tech sale cost",
            ["tech_creation_days_per_point_below_average"] = "Days per point (below average)",
            ["tech_creation_days_per_point_above_average"] = "Days per point (above average)",
            ["tech_improvement_total_duration_multiplier"] = "Total duration multiplier",
            ["tech_improvement_days_per_point_below_average"] = "Tech improvement (below average)",
            ["tech_improvement_days_per_point_above_average"] = "Tech improvement (above average)",


            ["scriptwriter_base_duration"] = "Base script writing time",
            ["production_base_duration"] = "Base filming time",
            ["postprod_base_duration_prod_fraction"] = "Post-production fraction of filming",
            ["script_doctor_duration"] = "Script doctor work time",
            ["script_doctor_fee"] = "Script doctor fee",
            ["script_doctor_success_probability"] = "Script improvement chance",

            // Технические лимиты
            ["max_competitor_tags_amount_for_movie"] = "Max competitor tags",
            ["content_tags_in_script_range"] = "Content tags in script range",
            ["max_content_tags_amount"] = "Absolute content tag limit",
            ["weeks_per_release"] = "Release duration in weeks",

            // Персонажи
            ["character_generator_min_skill"] = "Minimum skill",
            ["character_generator_attitude"] = "Attitude range",
            ["character_generator_mood_ranges"] = "Mood ranges",
            ["character_generator_mood_probability_curve"] = "Mood probability",
            ["suicide_mood_threshold"] = "Suicide mood threshold",
            ["suicide_chance"] = "Suicide chance",
            ["old_age_death_male_probability"] = "Old age death (male)",
            ["old_age_death_female_probability"] = "Old age death (female)",
            ["age_gradation_for_death"] = "Age gradations",

            // Контракты
            ["contract_years_range"] = "Contract years range",
            ["contract_movies_range"] = "Contract movies range",
            ["actors_base_duration_range"] = "Actor work duration",
            ["actor_payment_range"] = "Actor payment range",
            ["contract_extension_weight_multiplier"] = "Extension weight multiplier",

            // Сценарии
            ["script_gen_baseline_propability"] = "Script generation probability",
            ["scriptwriter_base_duration"] = "Base writing duration",
            ["generated_ideas_range"] = "Generated ideas range",
            ["ideas_duration_range"] = "Ideas duration range",
            ["script_del_max_amount"] = "Max deletable scripts",
            ["script_gen_max_amount"] = "Max generated scripts",

            // Премия Поллакс
            ["pollux_nomination_ip_and_rep_bonus_best_movie"] = "Nomination: Best Movie",
            ["pollux_win_ip_and_rep_bonus_best_movie"] = "Win: Best Movie",
            ["pollux_nomination_ip_and_rep_bonus_best_directing"] = "Nomination: Best Directing",
            ["pollux_win_ip_and_rep_bonus_best_directing"] = "Win: Best Directing",
            ["pollux_nomination_ip_and_rep_bonus_best_script"] = "Nomination: Best Script",
            ["pollux_win_ip_and_rep_bonus_best_script"] = "Win: Best Script",

            // Репутация
            ["reputation_for_icons"] = "Reputation for Icons",
            ["reputation_for_skilled_actor"] = "Reputation for Skilled Actor",
            ["reputation_for_idols"] = "Reputation for Idols",
            ["reputation_for_top"] = "Reputation for Top 3",
            ["reputation_for_profitable"] = "Reputation for Profitable Movie",
            ["reputation_for_unprofitable"] = "Reputation for Unprofitable Movie",

            // Шпионаж
            ["spying_xp_bonus_1"] = "XP bonus (level 1)",
            ["spying_xp_bonus_2"] = "XP bonus (level 2)",
            ["spying_xp_bonus_1"] = "Espionage XP bonus (25%)",
            ["spying_xp_bonus_2"] = "Espionage XP bonus (50%)",

            // Защита
            ["protection_min_agent_skill_duration"] = "Min. agent skill",
            ["protection_max_agent_skill_duration"] = "Max. agent skill",
            ["active_protection"] = "Active protection",
            ["passive_protection"] = "Passive protection",
            ["protection_success_probability"] = "Protection success chance",

            // Слежка
            ["surveillance_min_agent_skill_duration"] = "Min. agent skill",
            ["surveillance_max_agent_skill_duration"] = "Max. agent skill",
            ["surveillance_random_duration_offset"] = "Random offset",

            // Исследования
            ["tag_research_duration"] = "Tag research duration",
            ["tag_research_success_probability"] = "Success chance",
            ["tag_research_success_max_probability"] = "Max success chance",

            // Экономика
            ["start_tax_percent"] = "Initial tax rate",
            ["inflation_rate"] = "Inflation rate",
            ["inflation_years"] = "Inflation period (years)",
            ["bank_loan_peny"] = "Loan penalty",

            // Маркетинг
            ["ads_efficiency"] = "Ads efficiency",
            ["premiere_bonus"] = "Premiere bonus",
            ["random_review_probability"] = "Review appearance chance",
            ["reviews_max_count"] = "Max reviews",

            // Студия
            ["starting_budget"] = "Starting budget",
            ["starting_cash"] = "Starting cash",
            ["starting_reputation"] = "Starting reputation",
            ["starting_influence"] = "Starting influence",

            // События
            ["event_notification_timeout"] = "Notification timeout (sec)",
            ["scandal_duration"] = "Scandal duration",
            ["charity_effect"] = "Charity effect",
            ["suicide_low_mood_days"] = "Low mood days for suicide",

            // Наём
            ["hiring_bonus_mood"] = "Hiring mood bonus",
            ["hiring_bonus_attitude"] = "Hiring attitude bonus",
            ["staff_salary_range"] = "Salary range",
            ["min_salary"] = "Minimum salary",

            // Ассоциация
            ["association_yearly_fee"] = "Annual fee",
            ["association_join_fee"] = "Joining fee",
            ["association_fine"] = "Fine",
            ["association_surveillance_duration"] = "Surveillance duration",

            // Политика
            ["policy_major_image_chance1_days"] = "Major image chance (1)",
            ["policy_major_image_chance2_days"] = "Major image chance (2)",
            ["policy_major_image_price1"] = "Major image price (1)",
            ["policy_major_image_price2"] = "Major image price (2)",

            // Номинации
            ["pollux_nomination_ip_and_rep_bonus_best_actor"] = "Nomination: Best Actor",
            ["pollux_win_ip_and_rep_bonus_best_actor"] = "Win: Best Actor",
            ["pollux_nomination_ip_and_rep_bonus_best_actress"] = "Nomination: Best Actress",
            ["pollux_win_ip_and_rep_bonus_best_actress"] = "Win: Best Actress",

            // Конкуренты
            ["competitors_hiring_delay"] = "Competitor hiring delay",
            ["competitors_release_team_xp_increase"] = "Competitor team XP",
            ["competitor_movies_limit_releases_per_week"] = "Competitor releases per week limit",

            // Кинотеатры
            ["our_cinemas_total"] = "Our cinemas (total)",
            ["other_slots_total"] = "Rented slots",
            ["one_cinema_cost"] = "Cinema cost",
            ["cinema_sell_cost_modificator"] = "Cinema sell modifier",

            // Локации
            ["location_cost"] = "Location cost",
            ["location_duration"] = "Location search duration",
            ["location_quality_budget_factors"] = "Location budget multipliers",

            // Массовка
            ["extras_cost"] = "Extras cost",
            ["extras_options_amount"] = "Number of extras options",
            ["extras_duration_factors"] = "Extras duration multipliers",

            // Костюмы
            ["costumes_and_props_cost"] = "Costume cost",
            ["costumes_and_props_duration"] = "Costume creation duration",
            ["costumes_and_props_quality_budget"] = "Costume quality budget",

            // Декорации
            ["sets_time_red_1"] = "Set speed (10%)",
            ["sets_time_red_2"] = "Set speed (20%)",
            ["sets_time_red_3"] = "Set speed (30%)",

            // Озвучка
            ["sound_inhouse_improved"] = "Improved voiceover",
            ["sound_inhouse_time_1"] = "Voiceover speed",
            ["other_sound_fraction"] = "Other voiceover fraction",

            // Монтаж
            ["montage_fraction"] = "Editing fraction",
            ["film_editor_bonus_fraction"] = "Editor bonus",
            ["postprod_montage_base_cost"] = "Base editing cost",

            // Спецэффекты
            ["effects_quality_1"] = "Effect quality (1)",
            ["effects_quality_2"] = "Effect quality (2)",
            ["effects_quality_3"] = "Effect quality (3)",

            // Саундтрек
            ["composer_bonus_fraction"] = "Composer bonus",
            ["composer_payment_range"] = "Composer payment range",
            ["music_fraction"] = "Music fraction",

            // Синопсис
            ["content_tags_in_script_range"] = "Content tags range",
            ["script_content_tags_base"] = "Content tags base",
            ["max_content_tags_amount"] = "Max content tags",

            // R&D
            ["tech_improvement_red_time_per_rnd"] = "Improvement time reduction",
            ["tech_creation_red_time_per_rnd"] = "Creation time reduction",
            ["tech_sell_point_cost_base"] = "Base tech sale cost",

            // Обучение
            ["talents_xp_for_level"] = "XP per level (talents)",
            ["lieutenants_xp_for_level"] = "XP per level (lieutenants)",
            ["agents_xp_for_level"] = "XP per level (agents)",

            // Увольнение
            ["contract_termination_fee_1"] = "Termination fee (50%)",
            ["contract_termination_fee_2"] = "Termination fee (100%)",
            ["staff_raise_request_ignored_demanded_salary_increase"] = "Demanded salary increase",

            // Судебные иски
            ["trial_win_chance_by_severity"] = "Trial win chance",
            ["trial_influence_bonus_value"] = "Influence bonus in court",
            ["legal_defence_cost"] = "Legal defense cost",

            // Полиция
            ["police_raid_bribe_cost"] = "Bribe cost",
            ["cash_seizure_ratio_range"] = "Cash seizure range",
            ["penalty_per_illegal_worker"] = "Penalty per illegal worker",

            // Пресса
            ["random_review_probability"] = "Review chance",
            ["reviews_max_count"] = "Max reviews",
            ["good_gay_review_baseline"] = "Baseline review (gay)",
            ["good_woman_review_baseline"] = "Baseline review (woman)"

        };

        private readonly Dictionary<string, string> units = new Dictionary<string, string>
        {
            ["beating_min_agent_skill_duration"] = "days",
            ["beating_max_agent_skill_duration"] = "days",
            ["kidnapping_min_agent_skill_duration"] = "days",
            ["kidnapping_max_agent_skill_duration"] = "days",
            ["kidnapping_random_duration_offset"] = "days",
            ["ransom_duration_range"] = "days",
            ["ransom_family_cash"] = "$",
            ["ransom_studio_cash_per_weight"] = "$",
            ["assasination_min_agent_skill_duration"] = "days",
            ["assasination_max_agent_skill_duration"] = "days",
            ["threat_min_agent_skill_duration"] = "days",
            ["threat_max_agent_skill_duration"] = "days",
            ["policy_boutique_school_chance1_months"] = "mo",
            ["policy_boutique_school_chance2_months"] = "mo",
            ["policy_boutique_cap_days"] = "days",
            ["policy_boutique_prospective_pool_months"] = "mo",
            ["building_duration_per_point"] = "days/point",
            ["building_cost_per_point"] = "$",
            ["building_maintenance_cost_per_point"] = "$",
            ["building_wear_per_day"] = "%",
            ["building_repair_multipliers"] = "x",
            ["bank_loan_amount_0"] = "$",
            ["bank_loan_amount_1"] = "$",
            ["bank_loan_amount_2"] = "$",
            ["bank_loan_term_0"] = "years",
            ["bank_loan_term_1"] = "years",
            ["bank_loan_term_2"] = "years",
            ["bank_loan_int_rate_0"] = "%",
            ["bank_loan_int_rate_1"] = "%",
            ["bank_loan_int_rate_2"] = "%",
            ["bank_loan_cooldown_0"] = "mo",
            ["bank_loan_cooldown_1"] = "mo",
            ["start_tax_percent"] = "%",
            ["tax_base_reduction_1"] = "%",
            ["tax_base_reduction_2"] = "%",
            ["tech_improvement_days_per_point_average"] = "days",
            ["tech_creation_days_per_point_average"] = "days",
            ["tech_sell_point_cost_base"] = "$",
            ["scriptwriter_base_duration"] = "days",
            ["production_base_duration"] = "days",
            ["postprod_base_duration_prod_fraction"] = "%",
            ["script_doctor_duration"] = "days",
            ["script_doctor_fee"] = "$",
            ["script_doctor_success_probability"] = "%",
            ["tech_improvement_days_per_point_average"] = "days",
            ["tech_creation_days_per_point_average"] = "days",
            ["tech_sell_point_cost_base"] = "$",
            ["tech_creation_days_per_point_below_average"] = "days",
            ["tech_creation_days_per_point_above_average"] = "days",
            ["tech_improvement_total_duration_multiplier"] = "x",
            ["tech_improvement_days_per_point_below_average"] = "days",
            ["tech_improvement_days_per_point_above_average"] = "days",
            ["max_competitor_tags_amount_for_movie"] = "tags",
            ["content_tags_in_script_range"] = "tags",
            ["max_content_tags_amount"] = "tags",
            ["weeks_per_release"] = "weeks",
            ["character_generator_min_skill"] = "",
            ["character_generator_attitude"] = "",
            ["character_generator_mood_ranges"] = "",
            ["character_generator_mood_probability_curve"] = "",
            ["suicide_mood_threshold"] = "",
            ["suicide_chance"] = "%",
            ["old_age_death_male_probability"] = "%",
            ["old_age_death_female_probability"] = "%",
            ["age_gradation_for_death"] = "years",
            ["contract_years_range"] = "years",
            ["contract_movies_range"] = "movies",
            ["actors_base_duration_range"] = "days",
            ["actor_payment_range"] = "$",
            ["contract_extension_weight_multiplier"] = "x",
            ["script_gen_baseline_propability"] = "%",
            ["scriptwriter_base_duration"] = "days",
            ["generated_ideas_range"] = "ideas/mo",
            ["ideas_duration_range"] = "days",
            ["script_del_max_amount"] = "pcs",
            ["script_gen_max_amount"] = "pcs",
            ["pollux_nomination_ip_and_rep_bonus_best_movie"] = "IP / Rep",
            ["pollux_win_ip_and_rep_bonus_best_movie"] = "IP / Rep",
            ["pollux_nomination_ip_and_rep_bonus_best_directing"] = "IP / Rep",
            ["pollux_win_ip_and_rep_bonus_best_directing"] = "IP / Rep",
            ["pollux_nomination_ip_and_rep_bonus_best_script"] = "IP / Rep",
            ["pollux_win_ip_and_rep_bonus_best_script"] = "IP / Rep",
            ["reputation_for_icons"] = "",
            ["reputation_for_skilled_actor"] = "",
            ["reputation_for_idols"] = "",
            ["reputation_for_top"] = "",
            ["reputation_for_profitable"] = "",
            ["reputation_for_unprofitable"] = "",
            ["spying_xp_bonus_1"] = "x",
            ["spying_xp_bonus_2"] = "x",
            ["protection_min_agent_skill_duration"] = "days",
            ["protection_max_agent_skill_duration"] = "days",
            ["active_protection"] = "",
            ["passive_protection"] = "",
            ["protection_success_probability"] = "%",
            ["surveillance_min_agent_skill_duration"] = "days",
            ["surveillance_max_agent_skill_duration"] = "days",
            ["surveillance_random_duration_offset"] = "days",
            ["tag_research_duration"] = "days",
            ["tag_research_success_probability"] = "%",
            ["tag_research_success_max_probability"] = "%",
            ["start_tax_percent"] = "%",
            ["inflation_rate"] = "%",
            ["inflation_years"] = "years",
            ["bank_loan_peny"] = "%",
            ["ads_efficiency"] = "%",
            ["premiere_bonus"] = "%",
            ["random_review_probability"] = "%",
            ["reviews_max_count"] = "pcs",
            ["starting_budget"] = "$",
            ["starting_cash"] = "$",
            ["starting_reputation"] = "",
            ["starting_influence"] = "",
            ["event_notification_timeout"] = "sec",
            ["scandal_duration"] = "days",
            ["charity_effect"] = "",
            ["suicide_low_mood_days"] = "days",
            ["hiring_bonus_mood"] = "",
            ["hiring_bonus_attitude"] = "",
            ["staff_salary_range"] = "$",
            ["min_salary"] = "$",
            ["association_yearly_fee"] = "$",
            ["association_join_fee"] = "$",
            ["association_fine"] = "$",
            ["association_surveillance_duration"] = "days",
            ["policy_major_image_chance1_days"] = "days",
            ["policy_major_image_chance2_days"] = "days",
            ["policy_major_image_price1"] = "$",
            ["policy_major_image_price2"] = "$",
            ["pollux_nomination_ip_and_rep_bonus_best_actor"] = "IP / Rep",
            ["pollux_win_ip_and_rep_bonus_best_actor"] = "IP / Rep",
            ["competitors_hiring_delay"] = "days",
            ["competitors_release_team_xp_increase"] = "xp",
            ["competitor_movies_limit_releases_per_week"] = "pcs",
            ["our_cinemas_total"] = "pcs",
            ["other_slots_total"] = "pcs",
            ["one_cinema_cost"] = "$",
            ["location_cost"] = "$",
            ["location_duration"] = "days",
            ["extras_cost"] = "$",
            ["extras_options_amount"] = "pcs",
            ["costumes_and_props_cost"] = "$",
            ["costumes_and_props_duration"] = "days",
            ["montage_fraction"] = "%",
            ["film_editor_bonus_fraction"] = "x",
            ["postprod_montage_base_cost"] = "$",
            ["composer_bonus_fraction"] = "x",
            ["composer_payment_range"] = "$",
            ["music_fraction"] = "%",
            ["tech_improvement_red_time_per_rnd"] = "%",
            ["tech_creation_red_time_per_rnd"] = "%",
            ["tech_sell_point_cost_base"] = "$",
            ["talents_xp_for_level"] = "xp",
            ["lieutenants_xp_for_level"] = "xp",
            ["agents_xp_for_level"] = "xp",
            ["contract_termination_fee_1"] = "%",
            ["contract_termination_fee_2"] = "%",
            ["trial_win_chance_by_severity"] = "%",
            ["trial_influence_bonus_value"] = "x",
            ["legal_defence_cost"] = "$",
            ["police_raid_bribe_cost"] = "$",
            ["cash_seizure_ratio_range"] = "%",
            ["penalty_per_illegal_worker"] = "$",
            ["random_review_probability"] = "%",
            ["reviews_max_count"] = "pcs"
        };

        private readonly Dictionary<string, string> paramCategories = new Dictionary<string, string>
        {
            // Избиение
            ["beating_min_agent_skill_duration"] = "beating",
            ["beating_max_agent_skill_duration"] = "beating",

            // Похищение
            ["kidnapping_min_agent_skill_duration"] = "kidnapping",
            ["kidnapping_max_agent_skill_duration"] = "kidnapping",
            ["kidnapping_random_duration_offset"] = "kidnapping",
            ["ransom_duration_range"] = "kidnapping",
            ["ransom_family_cash"] = "kidnapping",
            ["ransom_studio_cash_per_weight"] = "kidnapping",

            // Убийство
            ["assasination_min_agent_skill_duration"] = "murder",
            ["assasination_max_agent_skill_duration"] = "murder",

            // Угрозы
            ["threat_min_agent_skill_duration"] = "threat",
            ["threat_max_agent_skill_duration"] = "threat",

            // Школа бутика
            ["policy_boutique_school_chance1_months"] = "boutique",
            ["policy_boutique_school_chance2_months"] = "boutique",
            ["policy_boutique_cap_days"] = "boutique",
            ["policy_boutique_prospective_pool_months"] = "boutique",

            // Строительство
            ["building_duration_per_point"] = "building",
            ["building_cost_per_point"] = "building",
            ["building_maintenance_cost_per_point"] = "building",
            ["building_wear_per_day"] = "building",
            ["building_repair_multipliers"] = "building",

            // Кредиты
            ["bank_loan_amount_0"] = "loan",
            ["bank_loan_amount_1"] = "loan",
            ["bank_loan_amount_2"] = "loan",
            ["bank_loan_term_0"] = "loan",
            ["bank_loan_term_1"] = "loan",
            ["bank_loan_term_2"] = "loan",
            ["bank_loan_int_rate_0"] = "loan",
            ["bank_loan_int_rate_1"] = "loan",
            ["bank_loan_int_rate_2"] = "loan",
            ["bank_loan_cooldown_0"] = "loan",
            ["bank_loan_cooldown_1"] = "loan",

            // Налоги
            ["start_tax_percent"] = "tax",
            ["tax_base_reduction_1"] = "tax",
            ["tax_base_reduction_2"] = "tax",

            // Технологии
            ["tech_improvement_days_per_point_average"] = "tech",
            ["tech_creation_days_per_point_average"] = "tech",
            ["tech_sell_point_cost_base"] = "tech",
            ["tech_improvement_days_per_point_average"] = "tech",
            ["tech_creation_days_per_point_average"] = "tech",
            ["tech_sell_point_cost_base"] = "tech",
            ["tech_creation_days_per_point_below_average"] = "tech",
            ["tech_creation_days_per_point_above_average"] = "tech",
            ["tech_improvement_total_duration_multiplier"] = "tech",
            ["tech_improvement_days_per_point_below_average"] = "tech",
            ["tech_improvement_days_per_point_above_average"] = "tech",

            // Ядро производства
            ["scriptwriter_base_duration"] = "production",
            ["production_base_duration"] = "production",
            ["postprod_base_duration_prod_fraction"] = "production",
            ["script_doctor_duration"] = "production",
            ["script_doctor_fee"] = "production",
            ["script_doctor_success_probability"] = "production",

            // Технические лимиты
            ["max_competitor_tags_amount_for_movie"] = "limits",
            ["content_tags_in_script_range"] = "limits",
            ["max_content_tags_amount"] = "limits",
            ["weeks_per_release"] = "limits",

            // Персонажи
            ["character_generator_min_skill"] = "character",
            ["character_generator_attitude"] = "character",
            ["character_generator_mood_ranges"] = "character",
            ["character_generator_mood_probability_curve"] = "character",
            ["suicide_mood_threshold"] = "character",
            ["suicide_chance"] = "character",
            ["old_age_death_male_probability"] = "character",
            ["old_age_death_female_probability"] = "character",
            ["age_gradation_for_death"] = "character",

            // Контракты
            ["contract_years_range"] = "contract",
            ["contract_movies_range"] = "contract",
            ["actors_base_duration_range"] = "contract",
            ["actor_payment_range"] = "contract",
            ["contract_extension_weight_multiplier"] = "contract",

            // Сценарии
            ["script_gen_baseline_propability"] = "script",
            ["scriptwriter_base_duration"] = "script",
            ["generated_ideas_range"] = "script",
            ["ideas_duration_range"] = "script",
            ["script_del_max_amount"] = "script",
            ["script_gen_max_amount"] = "script",

            // Премия Поллакс
            ["pollux_nomination_ip_and_rep_bonus_best_movie"] = "pollux",
            ["pollux_win_ip_and_rep_bonus_best_movie"] = "pollux",
            ["pollux_nomination_ip_and_rep_bonus_best_directing"] = "pollux",
            ["pollux_win_ip_and_rep_bonus_best_directing"] = "pollux",
            ["pollux_nomination_ip_and_rep_bonus_best_script"] = "pollux",
            ["pollux_win_ip_and_rep_bonus_best_script"] = "pollux",

            // Репутация
            ["reputation_for_icons"] = "reputation",
            ["reputation_for_skilled_actor"] = "reputation",
            ["reputation_for_idols"] = "reputation",
            ["reputation_for_top"] = "reputation",
            ["reputation_for_profitable"] = "reputation",
            ["reputation_for_unprofitable"] = "reputation",

            // Шпионаж
            ["spying_xp_bonus_1"] = "spying",
            ["spying_xp_bonus_2"] = "spying",

            // Защита
            ["protection_min_agent_skill_duration"] = "protection",
            ["protection_max_agent_skill_duration"] = "protection",
            ["active_protection"] = "protection",
            ["passive_protection"] = "protection",
            ["protection_success_probability"] = "protection",

            // Слежка
            ["surveillance_min_agent_skill_duration"] = "surveillance",
            ["surveillance_max_agent_skill_duration"] = "surveillance",
            ["surveillance_random_duration_offset"] = "surveillance",

            // Исследования
            ["tag_research_duration"] = "research",
            ["tag_research_success_probability"] = "research",
            ["tag_research_success_max_probability"] = "research",

            // Экономика
            ["start_tax_percent"] = "economy",
            ["inflation_rate"] = "economy",
            ["inflation_years"] = "economy",
            ["bank_loan_peny"] = "economy",

            // Маркетинг
            ["ads_efficiency"] = "marketing",
            ["premiere_bonus"] = "marketing",
            ["random_review_probability"] = "marketing",
            ["reviews_max_count"] = "marketing",

            // Студия
            ["starting_budget"] = "studio",
            ["starting_cash"] = "studio",
            ["starting_reputation"] = "studio",
            ["starting_influence"] = "studio",

            // События
            ["event_notification_timeout"] = "events",
            ["scandal_duration"] = "events",
            ["charity_effect"] = "events",
            ["suicide_low_mood_days"] = "events",

            // Наём
            ["hiring_bonus_mood"] = "hiring",
            ["hiring_bonus_attitude"] = "hiring",
            ["staff_salary_range"] = "hiring",
            ["min_salary"] = "hiring",

            ["association_yearly_fee"] = "association",
            ["association_join_fee"] = "association",
            ["association_fine"] = "association",
            ["association_surveillance_duration"] = "association",
            ["policy_major_image_chance1_days"] = "policy",
            ["policy_major_image_chance2_days"] = "policy",
            ["policy_major_image_price1"] = "policy",
            ["policy_major_image_price2"] = "policy",
            ["pollux_nomination_ip_and_rep_bonus_best_actor"] = "nomination",
            ["pollux_win_ip_and_rep_bonus_best_actor"] = "nomination",
            ["competitors_hiring_delay"] = "competitors",
            ["competitors_release_team_xp_increase"] = "competitors",
            ["competitor_movies_limit_releases_per_week"] = "competitors",
            ["our_cinemas_total"] = "cinema",
            ["other_slots_total"] = "cinema",
            ["one_cinema_cost"] = "cinema",
            ["cinema_sell_cost_modificator"] = "cinema",
            ["location_cost"] = "location",
            ["location_duration"] = "location",
            ["location_quality_budget_factors"] = "location",
            ["extras_cost"] = "extras",
            ["extras_options_amount"] = "extras",
            ["extras_duration_factors"] = "extras",
            ["costumes_and_props_cost"] = "costumes",
            ["costumes_and_props_duration"] = "costumes",
            ["costumes_and_props_quality_budget"] = "costumes",
            ["sets_time_red_1"] = "sets",
            ["sets_time_red_2"] = "sets",
            ["sets_time_red_3"] = "sets",
            ["sound_inhouse_improved"] = "voice",
            ["sound_inhouse_time_1"] = "voice",
            ["other_sound_fraction"] = "voice",
            ["montage_fraction"] = "editing",
            ["film_editor_bonus_fraction"] = "editing",
            ["postprod_montage_base_cost"] = "editing",
            ["effects_quality_1"] = "effects",
            ["effects_quality_2"] = "effects",
            ["effects_quality_3"] = "effects",
            ["composer_bonus_fraction"] = "soundtrack",
            ["composer_payment_range"] = "soundtrack",
            ["music_fraction"] = "soundtrack",
            ["content_tags_in_script_range"] = "synopsis",
            ["script_content_tags_base"] = "synopsis",
            ["max_content_tags_amount"] = "synopsis",
            ["tech_improvement_red_time_per_rnd"] = "rnd",
            ["tech_creation_red_time_per_rnd"] = "rnd",
            ["tech_sell_point_cost_base"] = "rnd",
            ["talents_xp_for_level"] = "training",
            ["lieutenants_xp_for_level"] = "training",
            ["agents_xp_for_level"] = "training",
            ["contract_termination_fee_1"] = "dismissal",
            ["contract_termination_fee_2"] = "dismissal",
            ["staff_raise_request_ignored_demanded_salary_increase"] = "dismissal",
            ["trial_win_chance_by_severity"] = "lawsuit",
            ["trial_influence_bonus_value"] = "lawsuit",
            ["legal_defence_cost"] = "lawsuit",
            ["police_raid_bribe_cost"] = "police",
            ["cash_seizure_ratio_range"] = "police",
            ["penalty_per_illegal_worker"] = "police",
            ["random_review_probability"] = "press",
            ["reviews_max_count"] = "press",
            ["good_gay_review_baseline"] = "press",
            ["good_woman_review_baseline"] = "press"

        };

        public SettingsWindow()
        {
            InitializeComponent();
            ApplyLocalization();
            variables = new Dictionary<string, GameVariable>();
            LocalizeDictionaries();
            Loaded += SettingsWindow_Loaded;
            FindAndLoadConfig();
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (closeOnLoaded)
                Close();
        }

        private void LocalizeDictionaries()
        {
            categories["beating"].Title = L("Beating", "Избиение");
            categories["kidnapping"].Title = L("Kidnapping", "Похищение");
            categories["murder"].Title = L("Murder", "Убийство");
            categories["threat"].Title = L("Threats", "Угрозы");
            categories["boutique"].Title = L("Boutique School", "Школа бутика");
            categories["building"].Title = L("Construction", "Строительство");
            categories["loan"].Title = L("Loans", "Кредиты");
            categories["tax"].Title = L("Taxes", "Налоги");
            categories["tech"].Title = L("Technologies", "Технологии");
            categories["production"].Title = L("Production Core", "Производство");
            categories["limits"].Title = L("Technical Limits", "Технические лимиты");
            categories["character"].Title = L("Characters", "Персонажи");
            categories["contract"].Title = L("Contracts", "Контракты");
            categories["script"].Title = L("Scripts", "Сценарии");
            categories["pollux"].Title = L("Pollux Award", "Премия Поллакс");
            categories["reputation"].Title = L("Reputation", "Репутация");
            categories["spying"].Title = L("Espionage", "Шпионаж");
            categories["protection"].Title = L("Protection", "Защита");
            categories["surveillance"].Title = L("Surveillance", "Слежка");
            categories["research"].Title = L("Research", "Исследования");
            categories["economy"].Title = L("Economy", "Экономика");
            categories["marketing"].Title = L("Marketing", "Маркетинг");
            categories["studio"].Title = L("Studio", "Студия");
            categories["events"].Title = L("Events", "События");
            categories["hiring"].Title = L("Hiring", "Наём");
            categories["association"].Title = L("Association", "Ассоциация");
            categories["policy"].Title = L("Politics", "Политика");
            categories["nomination"].Title = L("Nominations", "Номинации");
            categories["competitors"].Title = L("Competitors", "Конкуренты");
            categories["cinema"].Title = L("Cinemas", "Кинотеатры");
            categories["location"].Title = L("Locations", "Локации");
            categories["extras"].Title = L("Extras", "Массовка");
            categories["costumes"].Title = L("Costumes", "Костюмы");
            categories["sets"].Title = L("Sets", "Декорации");
            categories["voice"].Title = L("Voiceover", "Озвучка");
            categories["editing"].Title = L("Editing", "Монтаж");
            categories["effects"].Title = L("Special Effects", "Спецэффекты");
            categories["soundtrack"].Title = L("Soundtrack", "Саундтрек");
            categories["synopsis"].Title = L("Synopsis", "Синопсис");
            categories["rnd"].Title = L("R&D", "Технический отдел");
            categories["training"].Title = L("Training", "Обучение");
            categories["dismissal"].Title = L("Dismissal", "Увольнение");
            categories["lawsuit"].Title = L("Lawsuits", "Иски");
            categories["police"].Title = L("Police", "Полиция");
            categories["press"].Title = L("Press", "Пресса");

            displayNames["association_yearly_fee"] = L("Annual fee", "Ежегодный взнос");
            displayNames["association_join_fee"] = L("Joining fee", "Вступительный взнос");
            displayNames["association_fine"] = L("Fine", "Штраф");
            displayNames["association_surveillance_duration"] = L("Surveillance duration", "Длительность слежки");
            displayNames["beating_min_agent_skill_duration"] = L("Min. agent skill", "Мин. навык агента");
            displayNames["beating_max_agent_skill_duration"] = L("Max. agent skill", "Макс. навык агента");
            displayNames["policy_boutique_cap_days"] = L("Max policy duration", "Макс. длительность политики");
            displayNames["policy_boutique_prospective_pool_months"] = L("Prospective talent pool", "Пул перспективных талантов");
            displayNames["policy_boutique_school_chance1_months"] = L("Training duration (★)", "Длительность обучения (★)");
            displayNames["policy_boutique_school_chance2_months"] = L("Training duration (★★)", "Длительность обучения (★★)");
            displayNames["starting_budget"] = L("Starting budget", "Стартовый бюджет");
            displayNames["starting_cash"] = L("Starting cash", "Стартовые деньги");
            displayNames["starting_reputation"] = L("Starting reputation", "Стартовая репутация");
            displayNames["starting_influence"] = L("Starting influence", "Стартовое влияние");
            displayNames["production_base_duration"] = L("Base filming time", "Базовое время съёмок");
            displayNames["tag_research_duration"] = L("Tag research duration", "Длительность исследования тега");
            displayNames["tag_research_success_probability"] = L("Success chance", "Шанс успеха");
            displayNames["tag_research_success_max_probability"] = L("Max success chance", "Макс. шанс успеха");
        }

        private void ConfigSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || ConfigSelector == null || ConfigSelector.SelectedItem == null)
                return;

            var item = ConfigSelector.SelectedItem as ComboBoxItem;
            var selected = Convert.ToString(item?.Content);
            if (string.IsNullOrWhiteSpace(selected) || selected == currentConfigName)
                return;

            if (suppressConfigSelectionChanged)
                return;

            if (selected == "Perks")
            {
                try
                {
                    var perksWindow = new PerksWindow();
                    perksWindow.Owner = this;
                    perksWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    perksWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Perks", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    SelectConfigItem(previousConfigName);
                }
                return;
            }

            currentConfigName = selected;
            previousConfigName = selected;
            string path = currentConfigName == "Buildings" ? buildingsPath : gameVariablesPath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                LoadConfig(path);
                return;
            }

            string fileName = currentConfigName == "Buildings" ? "Buildings.json" : "GameVariables.json";
            if (!SelectConfigManually(fileName))
                SelectConfigItem(previousConfigName);
        }

        private void FindAndLoadConfig()
        {
            try
            {
                string foundGameVariables = null;

                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;

                    string[] roots = new[]
                    {
                        Path.Combine(drive.RootDirectory.FullName, "Steam", "steamapps", "common", "Hollywood Animal"),
                        Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam", "steamapps", "common", "Hollywood Animal"),
                        Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam", "steamapps", "common", "Hollywood Animal"),
                        Path.Combine(drive.RootDirectory.FullName, "Games", "Hollywood Animal"),
                        Path.Combine(drive.RootDirectory.FullName, "Games2", "Hollywood Animal"),
                        Path.Combine(drive.RootDirectory.FullName, "GAMES", "Hollywood Animal"),
                        Path.Combine(drive.RootDirectory.FullName, "games", "Hollywood Animal"),
                        Path.Combine(drive.RootDirectory.FullName, "Игры", "Hollywood Animal")
                    };

                    foreach (string root in roots)
                    {
                        string candidate = Path.Combine(root, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", "GameVariables.json");
                        if (File.Exists(candidate))
                        {
                            foundGameVariables = candidate;
                            break;
                        }
                    }

                    if (foundGameVariables != null) break;
                }

                if (foundGameVariables == null)
                {
                    var result = MessageBox.Show(
                        L("GameVariables.json not found!\n\nWould you like to select the file manually?\n\nThe file should be located in:\n...\\Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Configs\\GameVariables.json",
                          "GameVariables.json не найден!\n\nХотите выбрать файл вручную?\n\nФайл должен находиться здесь:\n...\\Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Configs\\GameVariables.json"),
                        L("File Not Found", "Файл не найден"), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes || !SelectConfigManually("GameVariables.json"))
                    {
                        closeOnLoaded = true;
                        CloseIfLoaded();
                        return;
                    }
                }
                else
                {
                    gameVariablesPath = foundGameVariables;
                    string configDir = Path.GetDirectoryName(gameVariablesPath);
                    buildingsPath = Path.Combine(configDir, "Buildings.json");
                    currentConfigName = "GameVariables";
                    LoadConfig(gameVariablesPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(L($"Error finding config:\n{ex.Message}", $"Ошибка поиска конфига:\n{ex.Message}"),
                    L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
                closeOnLoaded = true;
                CloseIfLoaded();
            }
        }

        private void CloseIfLoaded()
        {
            if (IsLoaded)
                Close();
        }

        private void SelectConfigItem(string name)
        {
            if (ConfigSelector == null) return;
            suppressConfigSelectionChanged = true;
            foreach (ComboBoxItem comboItem in ConfigSelector.Items)
            {
                if (Convert.ToString(comboItem.Content) == name)
                {
                    ConfigSelector.SelectedItem = comboItem;
                    break;
                }
            }
            suppressConfigSelectionChanged = false;
        }

        private string GetCurrentFileName()
        {
            return currentConfigName == "Buildings" ? "Buildings.json" : "GameVariables.json";
        }

        private string GetCurrentConfigPath()
        {
            return currentConfigName == "Buildings" ? buildingsPath : gameVariablesPath;
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fileName = GetCurrentFileName();
                string targetPath = GetCurrentConfigPath();
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    MessageBox.Show(L("First select the target config file.", "Сначала выберите целевой файл конфига."), L("Restore", "Восстановление"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);
                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show(L($"File not found in Resources:\n{sourcePath}", $"Файл не найден в Resources:\n{sourcePath}"), L("Restore", "Восстановление"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    L($"Replace current {fileName} with the file from Resources?", $"Заменить текущий {fileName} файлом из Resources?"),
                    L("Restore", "Восстановление"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                File.Copy(sourcePath, targetPath, true);
                LoadConfig(targetPath);
                MessageBox.Show(L("File restored successfully.", "Файл успешно восстановлен."), L("Restore", "Восстановление"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("Restore error:\n", "Ошибка восстановления:\n") + ex.Message, L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool SelectConfigManually(string fileName)
        {
            var dialog = new OpenFileDialog
            {
                Title = L($"Select {fileName}", $"Выберите {fileName}"),
                Filter = L("JSON files (*.json)|*.json", "JSON-файлы (*.json)|*.json"),
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true)
                return false;

            if (fileName == "Buildings.json")
                buildingsPath = dialog.FileName;
            else
            {
                gameVariablesPath = dialog.FileName;
                string configDir = Path.GetDirectoryName(gameVariablesPath);
                buildingsPath = Path.Combine(configDir, "Buildings.json");
            }

            LoadConfig(dialog.FileName);
            return true;
        }

        private void LoadConfig(string path)
        {
            try
            {
                configFilePath = path;
                string json = File.ReadAllText(path);
                configData = JObject.Parse(json);
                variables.Clear();
                fieldStats.Clear();

                string fileName = Path.GetFileName(path);
                currentConfigName = fileName.Equals("Buildings.json", StringComparison.OrdinalIgnoreCase) ? "Buildings" : "GameVariables";

                if (currentConfigName == "Buildings")
                    LoadBuildingsVariables();
                else
                    LoadGameVariables();

                BuildUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(L($"Error loading config file:\n{ex.Message}", $"Ошибка загрузки файла конфигурации:\n{ex.Message}"),
                    L("Error", "Ошибка"), MessageBoxButton.OK, MessageBoxImage.Error);
                closeOnLoaded = true;
                CloseIfLoaded();
            }
        }

        private void LoadGameVariables()
        {
            foreach (var param in paramCategories.Keys)
            {
                var token = configData.SelectToken(param);
                if (token != null)
                {
                    string currentValue = token["Value"]?.ToString() ?? token.ToString();
                    variables[param] = new GameVariable
                    {
                        Key = param,
                        Value = currentValue,
                        OriginalValue = currentValue,
                        RawToken = token,
                        Category = paramCategories.ContainsKey(param) ? paramCategories[param] : "other",
                        FieldName = param
                    };
                }
            }
        }

        private void LoadBuildingsVariables()
        {
            var editableFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "maxAmount", "width", "height", "level", "staff", "auraRadius", "auraTolerance",
                "baseCost", "baseDuration", "baseWater", "baseElectricity", "baseIp", "demolitionCost", "cameraZoom"
            };

            var numericValues = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

            foreach (var buildingProp in configData.Properties())
            {
                if (!(buildingProp.Value is JObject building)) continue;

                foreach (var field in editableFields)
                {
                    var token = building[field];
                    if (token == null) continue;
                    string value = token.ToString();
                    if (TryParseFlexibleDouble(value, out double n))
                    {
                        if (!numericValues.ContainsKey(field)) numericValues[field] = new List<double>();
                        numericValues[field].Add(n);
                    }
                }
            }

            foreach (var pair in numericValues)
            {
                if (pair.Value.Count > 0)
                    fieldStats[pair.Key] = new FieldStats { Min = pair.Value.Min(), Max = pair.Value.Max() };
            }

            foreach (var buildingProp in configData.Properties())
            {
                if (!(buildingProp.Value is JObject building)) continue;

                foreach (var field in editableFields)
                {
                    var token = building[field];
                    if (token == null) continue;

                    string key = buildingProp.Name + "." + field;
                    string value = token.ToString();
                    variables[key] = new GameVariable
                    {
                        Key = key,
                        Value = value,
                        OriginalValue = value,
                        RawToken = token,
                        Category = buildingProp.Name,
                        FieldName = field
                    };
                }
            }
        }

        private string GetDisplayName(string key)
        {
            if (currentConfigName == "Buildings")
            {
                string field = key.Contains(".") ? key.Split('.').Last() : key;
                return GetBuildingFieldName(field);
            }

            var value = displayNames.ContainsKey(key) ? displayNames[key] : key;
            return TranslateDisplayName(value);
        }

        private string GetUnit(string key)
        {
            if (currentConfigName == "Buildings")
            {
                string field = key.Contains(".") ? key.Split('.').Last() : key;
                return TranslateUnit(GetBuildingUnit(field));
            }

            var value = units.ContainsKey(key) ? units[key] : "";
            return TranslateUnit(value);
        }

        private string GetBuildingFieldName(string field)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["maxAmount"] = L("Maximum amount", "Максимальное количество"),
                ["width"] = L("Width", "Ширина"),
                ["height"] = L("Height", "Высота"),
                ["level"] = L("Level", "Уровень"),
                ["staff"] = L("Staff", "Персонал"),
                ["auraRadius"] = L("Building radius", "Радиус постройки"),
                ["auraTolerance"] = L("Placement tolerance", "Допуск постройки"),
                ["baseCost"] = L("Base cost", "Базовая стоимость"),
                ["baseDuration"] = L("Base duration", "Базовая длительность"),
                ["baseWater"] = L("Water consumption", "Потребление воды"),
                ["baseElectricity"] = L("Electricity consumption", "Потребление электричества"),
                ["baseIp"] = L("Influence points", "Очки влияния"),
                ["demolitionCost"] = L("Demolition cost", "Стоимость сноса"),
                ["cameraZoom"] = L("Camera zoom", "Приближение камеры")
            };
            return map.ContainsKey(field) ? map[field] : field;
        }

        private string GetBuildingUnit(string field)
        {
            switch (field)
            {
                case "baseCost":
                case "demolitionCost": return "$";
                case "baseDuration": return "days";
                case "width":
                case "height":
                case "auraRadius": return "m";
                case "staff":
                case "maxAmount": return "pcs";
                case "baseWater": return L("water", "вода");
                case "baseElectricity": return L("electricity", "эл.");
                default: return "";
            }
        }


        private string GetBuildingDisplayTitle(string id)
        {
            var ru = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MAIN_BUILDING"] = "Главное здание",
                ["SCRIPT_DOMINION"] = "Сценарный отдел",
                ["CONSTRUCTOR"] = "Конструкторское бюро",
                ["FREELANCE"] = "Фриланс-бюро",
                ["COPYRIGHT"] = "Авторские права",
                ["PREPRODUCTION_DOMINION"] = "Предпроизводство",
                ["CASTING"] = "Кастинг",
                ["SUPPLY"] = "Снабжение",
                ["WORKSHOP"] = "Мастерская",
                ["SCOUT"] = "Скаутский отдел",
                ["PRODUCTION_DOMINION"] = "Производственный отдел",
                ["PAVILION_I"] = "Павильон I",
                ["PAVILION_II"] = "Павильон II",
                ["PAVILION_III"] = "Павильон III",
                ["PAVILION_IV"] = "Павильон IV",
                ["LINE_PRODUCTION"] = "Линейное производство",
                ["LOGISTICS"] = "Логистика",
                ["POSTPRODUCTION_DOMINION"] = "Постпроизводство",
                ["LAB"] = "Лаборатория",
                ["FOCUS"] = "Фокус-группа",
                ["CONCERT"] = "Концертная площадка",
                ["SOUND"] = "Звуковой отдел",
                ["RELEASE_DOMINION"] = "Отдел релизов",
                ["DISTRIBUTION"] = "Дистрибуция",
                ["PRINT"] = "Печать копий",
                ["MARKETING"] = "Маркетинг",
                ["ANALYTICS"] = "Аналитический отдел",
                ["SECURITY_DOMINION"] = "Служба безопасности",
                ["SPIES"] = "Шпионский отдел",
                ["SHENANIGANS"] = "Тёмные дела",
                ["ESCORT_DOMINION"] = "Сопровождение",
                ["EVENTS_STAGE"] = "Сцена мероприятий",
                ["PRODUCERS_DOMINION"] = "Продюсерский отдел",
                ["AUTOMATION"] = "Автоматизация",
                ["INFRASTRUCTURE_DOMINION"] = "Инфраструктура",
                ["POWERPLANT_I"] = "Электростанция I",
                ["WATER_TOWER_I"] = "Водонапорная башня I",
                ["TECH_DOMINION"] = "Технический отдел",
                ["RND_I"] = "Исслед. группа I",
                ["TRASH_DOMINION"] = "Свалка",
                ["MAJOR_DOMINION"] = "Мэйджор-отдел",
                ["BOUTIQUE_DOMINION"] = "Школа бутика",
                ["CONVEYOR_DOMINION"] = "Конвейерный отдел",
                ["MUSEUM"] = "Музей",
                ["DUVAL_MONUMENT"] = "Памятник Дювалю"
            };
            var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["MAIN_BUILDING"] = "Main Building",
                ["SCRIPT_DOMINION"] = "Script Department",
                ["CONSTRUCTOR"] = "Construction Office",
                ["FREELANCE"] = "Freelance Office",
                ["COPYRIGHT"] = "Copyright Office",
                ["PREPRODUCTION_DOMINION"] = "Pre-production Department",
                ["CASTING"] = "Casting",
                ["SUPPLY"] = "Supply",
                ["WORKSHOP"] = "Workshop",
                ["SCOUT"] = "Scout Office",
                ["PRODUCTION_DOMINION"] = "Production Department",
                ["PAVILION_I"] = "Pavilion I",
                ["PAVILION_II"] = "Pavilion II",
                ["PAVILION_III"] = "Pavilion III",
                ["PAVILION_IV"] = "Pavilion IV",
                ["LINE_PRODUCTION"] = "Line Production",
                ["LOGISTICS"] = "Logistics",
                ["POSTPRODUCTION_DOMINION"] = "Post-production Department",
                ["LAB"] = "Lab",
                ["FOCUS"] = "Focus Group",
                ["CONCERT"] = "Concert Stage",
                ["SOUND"] = "Sound Department",
                ["RELEASE_DOMINION"] = "Release Department",
                ["DISTRIBUTION"] = "Distribution",
                ["PRINT"] = "Print Office",
                ["MARKETING"] = "Marketing",
                ["ANALYTICS"] = "Analytics",
                ["SECURITY_DOMINION"] = "Security Department",
                ["SPIES"] = "Spy Office",
                ["SHENANIGANS"] = "Shenanigans",
                ["ESCORT_DOMINION"] = "Escort Department",
                ["EVENTS_STAGE"] = "Events Stage",
                ["PRODUCERS_DOMINION"] = "Producers Department",
                ["AUTOMATION"] = "Automation",
                ["INFRASTRUCTURE_DOMINION"] = "Infrastructure Department",
                ["POWERPLANT_I"] = "Power Plant I",
                ["WATER_TOWER_I"] = "Water Tower I",
                ["TECH_DOMINION"] = "Tech Department",
                ["RND_I"] = "R&D I",
                ["TRASH_DOMINION"] = "Trash Department",
                ["MAJOR_DOMINION"] = "Major Department",
                ["BOUTIQUE_DOMINION"] = "Boutique School",
                ["CONVEYOR_DOMINION"] = "Conveyor Department",
                ["MUSEUM"] = "Museum",
                ["DUVAL_MONUMENT"] = "Duval Monument"
            };
            if (IsRussianLocale && ru.TryGetValue(id, out var ruName)) return ruName;
            if (!IsRussianLocale && en.TryGetValue(id, out var enName)) return enName;
            return HumanizeBuildingId(id);
        }

        private string HumanizeBuildingId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return id;
            string text = id.Replace("_", " ").ToLowerInvariant();
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
        }

        private string GetBuildingHint(GameVariable variable)
        {
            string field = variable.FieldName ?? variable.Key;
            string value = variable.OriginalValue;
            string category = variable.Category;

            var customHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["maxAmount"] = L($"* How many copies can be placed. 1 = unique.", $"* Сколько зданий можно построить. 1 — только одно."),
                ["width"] = L($"* Map width. Increasing may block nearby construction.", $"* Ширина на карте. Увеличение может помешать строить рядом."),
                ["height"] = L($"* Map height. Increasing may block nearby construction.", $"* Высота на карте. Увеличение может помешать строить рядом."),
                ["level"] = L($"* Building tier. Affects unlock order.", $"* Уровень здания. Влияет на порядок открытия."),
                ["staff"] = L($"* Staff required. More staff = higher salaries.", $"* Требуемый персонал. Больше — выше зарплаты."),
                ["auraRadius"] = L($"* Placement radius.", $"* Радиус размещения."),
                ["auraTolerance"] = L($"* Placement tolerance.", $"* Допуск размещения."),
                ["baseCost"] = L($"* Construction cost. Lower values break economy.", $"* Стоимость строительства. Сильное занижение ломает экономику."),
                ["baseDuration"] = L($"* Build days. 0 = instant build.", $"* Дней на строительство. 0 = моментально."),
                ["baseWater"] = L($"* Water consumption. Higher = more infrastructure strain.", $"* Потребление воды. Больше — выше нагрузка на инфраструктуру."),
                ["baseElectricity"] = L($"* Electricity consumption. Higher = more infrastructure strain.", $"* Потребление электричества. Больше — выше нагрузка на инфраструктуру."),
                ["baseIp"] = L($"* Influence point cost. Above 1 slows early expansion.", $"* Стоимость в очках влияния. Выше 1 замедляет старт."),
                ["demolitionCost"] = L($"* Demolition cost. 0 = free demolition.", $"* Стоимость сноса. 0 = бесплатный снос."),
                ["cameraZoom"] = L($"* Camera zoom when focusing on this building.", $"* Приближение камеры при фокусе на здании."),
            };

            if (customHints.TryGetValue(field, out string hint))
                return hint;

            return L($"* Original value: {value}. Change carefully.", $"* Исходное значение: {value}. Меняйте осторожно.");
        }

        private string TranslateDisplayName(string text)
        {
            if (!IsRussianLocale || string.IsNullOrWhiteSpace(text))
                return text;

            var map = new Dictionary<string, string>
            {
                ["Construction cost (per point)"] = "Стоимость строительства (за пункт)",
                ["Construction duration (per point)"] = "Длительность строительства (за пункт)",
                ["Maintenance cost (per point)"] = "Стоимость обслуживания (за пункт)",
                ["Repair multipliers"] = "Множители ремонта",
                ["Building wear per day"] = "Износ здания в день",

                ["Age gradations"] = "Возрастные градации",
                ["Attitude range"] = "Диапазон отношения",
                ["Minimum skill"] = "Минимальный навык",
                ["Mood probability"] = "Вероятность настроения",
                ["Mood ranges"] = "Диапазоны настроения",
                ["Old age death (female)"] = "Смерть от старости (женщины)",
                ["Old age death (male)"] = "Смерть от старости (мужчины)",
                ["Suicide chance"] = "Шанс суицида",
                ["Suicide mood threshold"] = "Порог настроения для суицида",

                ["Cinema sell modifier"] = "Модификатор продажи кинотеатра",
                ["Cinema sell cost modif"] = "Модификатор продажи кинотеатра",
                ["Cinema sell cost modifier"] = "Модификатор продажи кинотеатра",
                ["Cinema cost"] = "Стоимость кинотеатра",
                ["Rented slots"] = "Арендованные слоты",
                ["Our cinemas (total)"] = "Наши кинотеатры (всего)",

                ["Competitor releases per week limit"] = "Лимит релизов конкурента в неделю",
                ["Competitor hiring delay"] = "Задержка найма у конкурента",
                ["Competitor team XP"] = "Опыт команды конкурента",
                ["Max competitor tags"] = "Макс. число тегов конкурента",
                ["Release duration in weeks"] = "Длительность релиза в неделях",

                ["Actor payment range"] = "Диапазон оплаты актёра",
                ["Actor work duration"] = "Длительность работы актёра",
                ["Extension weight multiplier"] = "Множитель веса продления",
                ["Contract movies range"] = "Диапазон фильмов по контракту",
                ["Contract years range"] = "Диапазон лет контракта",

                ["Costume cost"] = "Стоимость костюма",
                ["Costume creation duration"] = "Длительность создания костюма",
                ["Costume quality budget"] = "Бюджет качества костюма",

                ["Demanded salary increase"] = "Требуемое повышение зарплаты",

                ["Loan penalty"] = "Штраф по кредиту",
                ["Inflation rate"] = "Уровень инфляции",
                ["Inflation period (years)"] = "Период инфляции (лет)",
                ["Initial tax rate"] = "Начальная налоговая ставка",

                ["Editor bonus"] = "Бонус монтажёра",
                ["Editing fraction"] = "Доля монтажа",
                ["Base editing cost"] = "Базовая стоимость монтажа",

                ["Notification timeout (sec)"] = "Таймаут уведомления (сек)",
                ["Low mood days for suicide"] = "Дни низкого настроения для суицида",

                ["Extras cost"] = "Стоимость массовки",
                ["Extras duration multipliers"] = "Множители длительности массовки",
                ["Number of extras options"] = "Количество вариантов массовки",

                ["Hiring attitude bonus"] = "Бонус отношения при найме",
                ["Hiring mood bonus"] = "Бонус настроения при найме",
                ["Minimum salary"] = "Минимальная зарплата",
                ["Salary range"] = "Диапазон зарплаты",

                ["Legal defense cost"] = "Стоимость юридической защиты",
                ["Influence bonus in court"] = "Бонус влияния в суде",
                ["Trial win chance"] = "Шанс победы в суде",

                ["Loan (initial)"] = "Кредит (начальный)",
                ["Loan (increased)"] = "Кредит (увеличенный)",
                ["Loan (maximum)"] = "Кредит (максимальный)",
                ["Loan cooldown (initial)"] = "Откат кредита (начальный)",
                ["Loan cooldown (improved)"] = "Откат кредита (улучшенный)",
                ["Interest rate (initial)"] = "Процентная ставка (начальная)",
                ["Interest rate (improved)"] = "Процентная ставка (улучшенная)",
                ["Interest rate (best)"] = "Процентная ставка (лучшая)",
                ["Loan term (initial)"] = "Срок кредита (начальный)",
                ["Loan term (increased)"] = "Срок кредита (увеличенный)",
                ["Loan term (maximum)"] = "Срок кредита (максимальный)",

                ["Location cost"] = "Стоимость локации",
                ["Location search duration"] = "Длительность поиска локации",
                ["Location budget multipliers"] = "Множители бюджета локации",

                ["Ads efficiency"] = "Эффективность рекламы",
                ["Premiere bonus"] = "Бонус премьеры",
                ["Review appearance chance"] = "Шанс появления рецензии",
                ["Review chance"] = "Шанс рецензии",
                ["Max reviews"] = "Макс. число рецензий",

                ["Max. agent skill"] = "Макс. навык агента",
                ["Min. agent skill"] = "Мин. навык агента",
                ["Random offset"] = "Случайное смещение",
                ["Ransom time range"] = "Диапазон времени выкупа",
                ["Ransom from family"] = "Выкуп от семьи",
                ["Ransom from studio"] = "Выкуп от студии",

                ["Cash seizure range"] = "Диапазон конфискации денег",
                ["Penalty per illegal worker"] = "Штраф за нелегального работника",
                ["Bribe cost"] = "Стоимость взятки",

                ["Major image chance (1)"] = "Шанс крупного имиджа (1)",
                ["Major image chance (2)"] = "Шанс крупного имиджа (2)",
                ["Major image price (1)"] = "Цена крупного имиджа (1)",
                ["Major image price (2)"] = "Цена крупного имиджа (2)",

                ["Nomination: Best Directing"] = "Номинация: лучшая режиссура",
                ["Nomination: Best Movie"] = "Номинация: лучший фильм",
                ["Nomination: Best Script"] = "Номинация: лучший сценарий",
                ["Win: Best Directing"] = "Победа: лучшая режиссура",
                ["Win: Best Movie"] = "Победа: лучший фильм",
                ["Win: Best Script"] = "Победа: лучший сценарий",
                ["Nomination: Best Actor"] = "Номинация: лучший актёр",
                ["Win: Best Actor"] = "Победа: лучший актёр",
                ["Nomination: Best Actress"] = "Номинация: лучшая актриса",
                ["Win: Best Actress"] = "Победа: лучшая актриса",

                ["Baseline review (gay)"] = "Базовая рецензия (гей)",
                ["Baseline review (woman)"] = "Базовая рецензия (женщина)",

                ["Post-production fraction of filming"] = "Доля постпродакшна от съёмок",
                ["Base filming time"] = "Базовое время съёмок",
                ["Script doctor work time"] = "Время работы скрипт-доктора",
                ["Script doctor fee"] = "Гонорар скрипт-доктора",
                ["Script improvement chance"] = "Шанс улучшения сценария",

                ["Passive protection"] = "Пассивная защита",
                ["Active protection"] = "Активная защита",
                ["Protection success chance"] = "Шанс успешной защиты",

                ["Reputation for Icons"] = "Репутация за икон",
                ["Reputation for Idols"] = "Репутация за идолов",
                ["Reputation for Profitable Movie"] = "Репутация за прибыльный фильм",
                ["Reputation for Skilled Actor"] = "Репутация за опытного актёра",
                ["Reputation for Top 3"] = "Репутация за топ-3",
                ["Reputation for Unprofitable Movie"] = "Репутация за убыточный фильм",

                ["Tag research duration"] = "Длительность исследования тега",
                ["Success chance"] = "Шанс успеха",
                ["Max success chance"] = "Макс. шанс успеха",

                ["Improvement time reduction"] = "Снижение времени улучшения",
                ["Creation time reduction"] = "Снижение времени создания",
                ["Base tech sale cost"] = "Базовая стоимость продажи технологии",

                ["Generated ideas range"] = "Диапазон генерируемых идей",
                ["Ideas duration range"] = "Диапазон длительности идей",
                ["Max deletable scripts"] = "Макс. удаляемых сценариев",
                ["Script generation probability"] = "Вероятность генерации сценария",
                ["Max generated scripts"] = "Макс. сгенерированных сценариев",
                ["Base writing duration"] = "Базовая длительность написания",
                ["Base script writing time"] = "Базовое время написания сценария",

                ["Composer bonus"] = "Бонус композитора",
                ["Composer payment range"] = "Диапазон оплаты композитора",
                ["Music fraction"] = "Доля музыки",

                ["Espionage XP bonus (25%)"] = "Бонус опыта шпионажа (25%)",
                ["Espionage XP bonus (50%)"] = "Бонус опыта шпионажа (50%)",
                ["XP bonus (level 1)"] = "Бонус опыта (уровень 1)",
                ["XP bonus (level 2)"] = "Бонус опыта (уровень 2)",

                ["Starting budget"] = "Стартовый бюджет",
                ["Starting cash"] = "Стартовые деньги",
                ["Starting influence"] = "Стартовое влияние",
                ["Starting reputation"] = "Стартовая репутация",

                ["Content tags range"] = "Диапазон тегов контента",
                ["Content tags in script range"] = "Диапазон тегов контента",
                ["Max content tags"] = "Макс. число тегов контента",
                ["Absolute content tag limit"] = "Абсолютный лимит тегов контента",
                ["Content tags base"] = "Базовое число тегов контента",

                ["Days per point (above average)"] = "Дней за пункт (выше среднего)",
                ["Tech creation (average)"] = "Создание технологии (среднее)",
                ["Days per point (below average)"] = "Дней за пункт (ниже среднего)",
                ["Tech improvement (above average)"] = "Улучшение технологии (выше среднего)",
                ["Tech improvement (average)"] = "Улучшение технологии (среднее)",
                ["Tech improvement (below average)"] = "Улучшение технологии (ниже среднего)",
                ["Total duration multiplier"] = "Множитель общей длительности",

                ["XP per level (agents)"] = "Опыт на уровень (агенты)",
                ["XP per level (lieutenants)"] = "Опыт на уровень (лейтенанты)",
                ["XP per level (talents)"] = "Опыт на уровень (таланты)",

                ["Other voiceover fraction"] = "Доля другой озвучки",

                ["Annual fee"] = "Ежегодный взнос",
                ["Joining fee"] = "Вступительный взнос",
                ["Fine"] = "Штраф",
                ["Surveillance duration"] = "Длительность слежки",
                ["Max policy duration"] = "Макс. длительность политики",
                ["Prospective talent pool"] = "Пул перспективных талантов",
                ["Training duration (★)"] = "Длительность обучения (★)",
                ["Training duration (★★)"] = "Длительность обучения (★★)",
            };

            return map.TryGetValue(text, out var translated) ? translated : text;
        }

        private string TranslateUnit(string unit)
        {
            if (!IsRussianLocale || string.IsNullOrWhiteSpace(unit))
                return unit;

            return unit switch
            {
                "days" => "дни",
                "days/point" => "дн./пункт",
                "years" => "лет",
                "mo" => "мес",
                "weeks" => "нед",
                "movies" => "фильмы",
                "ideas/mo" => "идей/мес",
                "pcs" => "шт",
                "tags" => "теги",
                "sec" => "сек",
                "xp" => "оп",
                "IP / Rep" => "Вл / Реп",
                _ => unit
            };
        }

        private UIElement CreateValueEditor(GameVariable variable)
        {
            if (variable.Value.Contains('_') && !variable.Key.Contains("_and_"))
            {
                return CreateRangeEditor(variable);
            }

            var textBox = new TextBox
            {
                Text = variable.Value,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Tag = variable.Key,
                Background = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x3F)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xAB, 0xAD, 0xB3)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            textBox.TextChanged += (s, e) =>
            {
                if (textBox.Tag is string key && variables.ContainsKey(key))
                    variables[key].Value = textBox.Text;
            };
            return textBox;
        }

        private UIElement CreateRangeEditor(GameVariable variable)
        {
            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            string[] parts = variable.Value.Split('_');
            string minVal = parts.Length > 0 ? parts[0] : "";
            string maxVal = parts.Length > 1 ? parts[1] : "";

            var minBox = new TextBox
            {
                Text = minVal,
                Width = 60,
                Margin = new Thickness(0, 0, 3, 0),
                Tag = variable.Key,
                Background = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x3F)),
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var separator = new TextBlock
            {
                Text = "—",
                Margin = new Thickness(3, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                FontSize = 12
            };

            var maxBox = new TextBox
            {
                Text = maxVal,
                Width = 60,
                Tag = variable.Key,
                Background = new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x3F)),
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            void UpdateValue()
            {
                if (variables.ContainsKey(variable.Key))
                    variables[variable.Key].Value = $"{minBox.Text}_{maxBox.Text}";
            }

            minBox.TextChanged += (s, e) => UpdateValue();
            maxBox.TextChanged += (s, e) => UpdateValue();

            wrapPanel.Children.Add(minBox);
            wrapPanel.Children.Add(separator);
            wrapPanel.Children.Add(maxBox);

            return wrapPanel;
        }

        private bool TryParseFlexibleDouble(string text, out double value)
        {
            text = (text ?? "").Trim().Replace(",", ".");
            return double.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private string FormatNumber(double value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string GetHint(GameVariable variable)
        {
            if (currentConfigName == "Buildings")
                return GetBuildingHint(variable);

            string key = variable.Key ?? "";
            string display = GetDisplayName(key);
            string value = variable.OriginalValue;

            var customHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {

                ["starting_budget"] = L($"* Starting budget. Higher removes early challenges.", $"* Стартовая сумма в бюджете. Завышение убирает вызовы в начале игры."),
                ["starting_budget_small"] = L($"* Starting budget for small start.", $"* Стартовый бюджет для малого старта."),
                ["starting_budget_big"] = L($"* Starting budget for big start.", $"* Стартовый бюджет для крупного старта."),
                ["starting_cash"] = L($"* Starting cash. Affects bribes and small expenses.", $"* Стартовые наличные. Влияют на взятки и мелкие траты."),
                ["starting_reputation"] = L($"* Initial reputation. High value speeds up upgrades.", $"* Начальная репутация. Высокое значение ускоряет доступ к улучшениям."),
                ["starting_influence"] = L($"* Initial influence. Affects association and politics.", $"* Начальное влияние. Влияет на возможности в ассоциации и политике."),

                ["production_base_duration"] = L($"* Filming duration in days. Lower = faster releases.", $"* Длительность съёмок в днях. Меньше — быстрее выпускаете фильмы."),
                ["scriptwriter_base_duration"] = L($"* Days to write a script. Affects idea frequency.", $"* Дней на написание сценария. Влияет на частоту новых идей."),
                ["postprod_base_duration_prod_fraction"] = L($"* Post-prod as fraction of filming. 0.33 = third of filming.", $"* Доля времени постпродакшна от съёмок. 0.33 = треть от времени съёмок."),
                ["building_duration_per_point"] = L($"* Build days per building point.", $"* Дней строительства на единицу здания."),
                ["building_cost_per_point"] = L($"* Build cost per point.", $"* Стоимость строительства за единицу."),
                ["building_maintenance_cost_per_point"] = L($"* Maintenance cost per point.", $"* Расходы на обслуживание за единицу."),
                ["building_wear_per_day"] = L($"* Daily building wear. Higher = faster decay.", $"* Износ здания в день. Больше — быстрее ветшает."),
                ["building_repair_multipliers"] = L($"* Repair cost multipliers at different wear stages.", $"* Множители стоимости ремонта на разных стадиях износа."),

                ["max_content_tags_amount"] = L($"* Max tags per script. Above 5 may break generator.", $"* Максимум тегов в сценарии. Больше 5 может сломать генератор."),
                ["max_competitor_tags_amount_for_movie"] = L($"* Max competitor tags per movie.", $"* Лимит тегов у конкурентов на фильм."),
                ["weeks_per_release"] = L($"* Weeks a movie stays in theaters.", $"* Сколько недель фильм в прокате."),
                ["script_del_max_amount"] = L($"* Scripts that can be deleted at once.", $"* Сколько сценариев можно удалить за раз."),
                ["script_gen_max_amount"] = L($"* Scripts generated at once.", $"* Сколько сценариев генерируется за раз."),
                ["reviews_max_count"] = L($"* Max reviews per movie.", $"* Максимум рецензий на фильм."),
                ["our_cinemas_total"] = L($"* Cinemas already owned by studio.", $"* Сколько кинотеатров уже есть у студии."),
                ["other_slots_total"] = L($"* Slots available to rent from others.", $"* Сколько слотов можно арендовать у других."),
                ["competitor_movies_limit_releases_per_week"] = L($"* Competitor releases per week. Above 3 = oversaturation.", $"* Лимит релизов конкурентов в неделю. Выше 3 — перенасыщение рынка."),
                ["max_amount"] = L($"* How many copies of this building can be placed. 1 = unique.", $"* Сколько зданий можно построить. 1 — только одно."),

                ["character_generator_min_skill"] = L($"* Minimum skill of new characters. Higher = stronger hires.", $"* Минимальный навык новых персонажей. Повышение делает всех сильнее."),
                ["character_generator_attitude"] = L($"* Attitude range of new characters toward studio.", $"* Диапазон отношения новых персонажей к студии."),
                ["character_generator_mood_ranges"] = L($"* Mood category boundaries.", $"* Границы категорий настроения персонажей."),
                ["character_generator_mood_probability_curve"] = L($"* Probability of each mood level.", $"* Вероятность каждого уровня настроения."),
                ["suicide_mood_threshold"] = L($"* Mood threshold for suicide risk.", $"* Порог настроения, при котором персонаж может покончить с собой."),
                ["suicide_chance"] = L($"* Suicide chance when mood drops below threshold.", $"* Шанс суицида при падении настроения ниже порога."),
                ["old_age_death_male_probability"] = L($"* Old age death probability for males by age.", $"* Вероятность смерти от старости для мужчин по возрастам."),
                ["old_age_death_female_probability"] = L($"* Old age death probability for females by age.", $"* Вероятность смерти от старости для женщин по возрастам."),
                ["age_gradation_for_death"] = L($"* Age boundaries for old age death calculation.", $"* Возрастные границы для расчёта смерти от старости."),

                ["contract_years_range"] = L($"* Contract length in years. Affects team stability.", $"* Длительность контракта в годах. Влияет на стабильность команды."),
                ["contract_movies_range"] = L($"* Movies required by contract.", $"* Сколько фильмов по контракту обязан сняться."),
                ["actors_base_duration_range"] = L($"* Actor work duration per movie in days.", $"* Длительность работы актёра над фильмом в днях."),
                ["actor_payment_range"] = L($"* Actor salary range.", $"* Диапазон зарплат актёров."),
                ["contract_extension_weight_multiplier"] = L($"* Weight multiplier for contract extension.", $"* Множитель веса при продлении контракта."),

                ["script_gen_baseline_propability"] = L($"* Script generation probability by quality tier.", $"* Вероятность генерации сценария по уровням качества."),
                ["generated_ideas_range"] = L($"* How many ideas generate at once.", $"* Сколько идей генерируется за раз."),
                ["ideas_duration_range"] = L($"* How long an idea stays relevant in days.", $"* Как долго идея остаётся актуальной в днях."),
                ["script_doctor_duration"] = L($"* Script doctor work days.", $"* Дней работы скрипт-доктора."),
                ["script_doctor_fee"] = L($"* Script doctor fee.", $"* Стоимость услуг скрипт-доктора."),
                ["script_doctor_success_probability"] = L($"* Chance script doctor improves the script.", $"* Шанс, что скрипт-доктор улучшит сценарий."),

                ["pollux_nomination_ip_and_rep_bonus_best_movie"] = L($"* Influence + rep bonus for 'Best Movie' nomination.", $"* Бонус к влиянию и репутации за номинацию на «Лучший фильм»."),
                ["pollux_win_ip_and_rep_bonus_best_movie"] = L($"* Influence + rep bonus for 'Best Movie' win.", $"* Бонус к влиянию и репутации за победу в «Лучший фильм»."),
                ["pollux_nomination_ip_and_rep_bonus_best_directing"] = L($"* Bonus for 'Best Directing' nomination.", $"* Бонус за номинацию «Лучшая режиссура»."),
                ["pollux_win_ip_and_rep_bonus_best_directing"] = L($"* Bonus for 'Best Directing' win.", $"* Бонус за победу «Лучшая режиссура»."),
                ["pollux_nomination_ip_and_rep_bonus_best_script"] = L($"* Bonus for 'Best Script' nomination.", $"* Бонус за номинацию «Лучший сценарий»."),
                ["pollux_win_ip_and_rep_bonus_best_script"] = L($"* Bonus for 'Best Script' win.", $"* Бонус за победу «Лучший сценарий»."),
                ["reputation_for_icons"] = L($"* Reputation gain from hiring an icon.", $"* Сколько репутации даёт икона."),
                ["reputation_for_skilled_actor"] = L($"* Reputation gain from hiring a skilled actor.", $"* Сколько репутации даёт опытный актёр."),
                ["reputation_for_idols"] = L($"* Reputation gain from hiring an idol.", $"* Сколько репутации даёт кумир."),
                ["reputation_for_top"] = L($"* Reputation gain from top chart placement.", $"* Сколько репутации даёт попадание в топ."),
                ["reputation_for_profitable"] = L($"* Reputation gain from profitable movie.", $"* Репутация за прибыльный фильм."),
                ["reputation_for_unprofitable"] = L($"* Reputation penalty from unprofitable movie.", $"* Штраф репутации за убыточный фильм."),

                ["spying_xp_bonus_1"] = L($"* Spy XP multiplier. 2.0 = twice as fast.", $"* Множитель опыта для шпионов. 2.0 = вдвое быстрей прокачка."),
                ["spying_xp_bonus_2"] = L($"* Spy XP multiplier (upgraded).", $"* Множитель опыта для шпионов (улучшенный)."),
                ["protection_min_agent_skill_duration"] = L($"* Protection op duration at min skill.", $"* Время операции защиты при минимальном навыке."),
                ["protection_max_agent_skill_duration"] = L($"* Protection op duration at max skill.", $"* Время операции защиты при максимальном навыке."),
                ["protection_success_probability"] = L($"* Protection success chance. 0.9 = 90%.", $"* Шанс успешной защиты от атак. 0.9 = 90% защиты."),
                ["active_protection"] = L($"* Protection bonus in active mode.", $"* Бонус к защите при активном режиме."),
                ["passive_protection"] = L($"* Protection bonus in passive mode.", $"* Бонус к защите в пассивном режиме."),

                ["surveillance_min_agent_skill_duration"] = L($"* Surveillance duration at min skill.", $"* Длительность слежки при минимальном навыке."),
                ["surveillance_max_agent_skill_duration"] = L($"* Surveillance duration at max skill.", $"* Длительность слежки при максимальном навыке."),
                ["surveillance_random_duration_offset"] = L($"* Random duration offset for surveillance.", $"* Случайное отклонение длительности слежки."),

                ["kidnapping_min_agent_skill_duration"] = L($"* Kidnapping duration at min skill.", $"* Длительность похищения при минимальном навыке."),
                ["kidnapping_max_agent_skill_duration"] = L($"* Kidnapping duration at max skill.", $"* Длительность похищения при максимальном навыке."),
                ["kidnapping_random_duration_offset"] = L($"* Random offset for kidnapping duration.", $"* Случайное отклонение длительности похищения."),
                ["ransom_duration_range"] = L($"* Time range until ransom demand.", $"* Диапазон времени до требования выкупа."),
                ["ransom_family_cash"] = L($"* How much family pays for ransom.", $"* Сколько семья готова заплатить за выкуп."),
                ["ransom_studio_cash_per_weight"] = L($"* Studio payment per victim weight unit.", $"* Сколько студия платит за единицу веса жертвы."),

                ["beating_min_agent_skill_duration"] = L($"* Beating duration at min skill.", $"* Длительность избиения при минимальном навыке."),
                ["beating_max_agent_skill_duration"] = L($"* Beating duration at max skill.", $"* Длительность избиения при максимальном навыке."),

                ["assasination_min_agent_skill_duration"] = L($"* Assassination duration at min skill.", $"* Длительность убийства при минимальном навыке."),
                ["assasination_max_agent_skill_duration"] = L($"* Assassination duration at max skill.", $"* Длительность убийства при максимальном навыке."),

                ["threat_min_agent_skill_duration"] = L($"* Threat duration at min skill.", $"* Длительность угрозы при минимальном навыке."),
                ["threat_max_agent_skill_duration"] = L($"* Threat duration at max skill.", $"* Длительность угрозы при максимальном навыке."),

                ["tag_research_duration"] = L($"* Days to research a new tag.", $"* Дней на исследование нового тега."),
                ["tag_research_success_probability"] = L($"* Success chance when researching a tag.", $"* Шанс успеха при исследовании тега."),
                ["tag_research_success_max_probability"] = L($"* Maximum research success chance.", $"* Максимальный шанс успеха исследования."),

                ["start_tax_percent"] = L($"* Initial tax rate. 0.11 = 11%.", $"* Начальная ставка налога. 0.11 = 11%."),
                ["inflation_rate"] = L($"* Annual inflation. 1.05 = +5% yearly.", $"* Годовая инфляция. 1.05 = +5% к ценам ежегодно."),
                ["inflation_years"] = L($"* Years until inflation starts.", $"* Через сколько лет начинает действовать инфляция."),
                ["bank_loan_peny"] = L($"* Late payment penalty (fraction of loan).", $"* Штраф за просрочку кредита (доля от суммы)."),
                ["bank_loan_amount_0"] = L($"* Starting loan amount.", $"* Сумма кредита на старте."),
                ["bank_loan_amount_1"] = L($"* Loan amount after upgrade.", $"* Сумма кредита после улучшения."),
                ["bank_loan_amount_2"] = L($"* Maximum loan amount.", $"* Максимальная сумма кредита."),
                ["bank_loan_term_0"] = L($"* Starting loan term in years.", $"* Срок кредита в годах на старте."),
                ["bank_loan_term_1"] = L($"* Loan term after upgrade.", $"* Срок кредита после улучшения."),
                ["bank_loan_term_2"] = L($"* Maximum loan term.", $"* Максимальный срок кредита."),
                ["bank_loan_int_rate_0"] = L($"* Starting interest rate.", $"* Процентная ставка на старте."),
                ["bank_loan_int_rate_1"] = L($"* Interest rate after upgrade.", $"* Процентная ставка после улучшения."),
                ["bank_loan_int_rate_2"] = L($"* Best interest rate.", $"* Лучшая процентная ставка."),
                ["bank_loan_cooldown_0"] = L($"* Starting loan cooldown in months.", $"* Месяцев отката между кредитами на старте."),
                ["bank_loan_cooldown_1"] = L($"* Loan cooldown after upgrade.", $"* Месяцев отката между кредитами после улучшения."),

                ["ads_efficiency"] = L($"* Ad campaign efficiency. Higher = more viewers.", $"* Эффективность рекламы. Выше — больше зрителей в первую неделю."),
                ["premiere_bonus"] = L($"* Premiere week bonus multiplier.", $"* Бонус к сборам на премьерной неделе."),
                ["random_review_probability"] = L($"* Chance a random review appears.", $"* Шанс появления случайной рецензии."),

                ["event_notification_timeout"] = L($"* Seconds before notification auto-closes.", $"* Секунд до автоматического закрытия уведомления."),
                ["scandal_duration"] = L($"* How long a scandal lasts in days.", $"* Длительность скандала в днях."),
                ["charity_effect"] = L($"* How much charity improves reputation.", $"* Насколько благотворительность повышает репутацию."),
                ["suicide_low_mood_days"] = L($"* Days of low mood before suicide risk.", $"* Дней низкого настроения до риска суицида."),

                ["hiring_bonus_mood"] = L($"* Temporary mood bonus when hiring.", $"* Временный бонус к настроению при найме."),
                ["hiring_bonus_attitude"] = L($"* Temporary attitude bonus when hiring.", $"* Временный бонус к отношению при найме."),
                ["staff_salary_range"] = L($"* Salary range for regular staff.", $"* Диапазон зарплат обычного персонала."),
                ["min_salary"] = L($"* Minimum possible salary.", $"* Минимально возможная зарплата."),

                ["association_yearly_fee"] = L($"* Annual association fee.", $"* Ежегодный взнос в ассоциацию."),
                ["association_join_fee"] = L($"* One-time association joining fee.", $"* Вступительный взнос в ассоциацию."),
                ["association_fine"] = L($"* Fine for violating association rules.", $"* Штраф за нарушение правил ассоциации."),
                ["association_surveillance_duration"] = L($"* How many days surveillance lasts.", $"* Длительность слежки со стороны ассоциации."),

                ["policy_major_image_chance1_days"] = L($"* Days until first major image chance.", $"* Дней до первого шанса крупного имиджа."),
                ["policy_major_image_chance2_days"] = L($"* Days until second major image chance.", $"* Дней до второго шанса крупного имиджа."),
                ["policy_major_image_price1"] = L($"* Cost of level 1 major image.", $"* Стоимость крупного имиджа (1-й уровень)."),
                ["policy_major_image_price2"] = L($"* Cost of level 2 major image.", $"* Стоимость крупного имиджа (2-й уровень)."),

                ["policy_boutique_school_chance1_months"] = L($"* Duration of training at ★-star.", $"* Длительность обучения на ★-звезды"),
                ["policy_boutique_school_chance2_months"] = L($"* Duration of training at ★★-star.", $"* Длительность обучения на ★★-звезды"),
                ["policy_boutique_cap_days"] = L($"* Max duration for boutique policies.", $"* Максимальная длительность политик бутика."),
                ["policy_boutique_prospective_pool_months"] = L($"* How long prospective talents stay in pool.", $"* Как долго перспективные таланты остаются в пуле."),

                ["competitors_hiring_delay"] = L($"* Delay before competitors hire replacements.", $"* Задержка перед наймом замены конкурентами."),
                ["competitors_release_team_xp_increase"] = L($"* XP gain for competitor release teams.", $"* Опыт для команд релиза конкурентов."),

                ["location_cost"] = L($"* Base location scouting cost.", $"* Базовая стоимость поиска локации."),
                ["location_duration"] = L($"* Days to find and secure a location.", $"* Дней на поиск и бронирование локации."),
                ["location_quality_budget_factors"] = L($"* Budget multipliers by location quality.", $"* Множители бюджета в зависимости от качества локации."),

                ["extras_cost"] = L($"* Base cost for hiring extras.", $"* Базовая стоимость найма массовки."),
                ["extras_options_amount"] = L($"* How many extra options appear.", $"* Сколько вариантов массовки появляется."),
                ["extras_duration_factors"] = L($"* Duration multipliers for extras work.", $"* Множители длительности работы массовки."),

                ["costumes_and_props_cost"] = L($"* Base costume and props cost.", $"* Базовая стоимость костюмов и реквизита."),
                ["costumes_and_props_duration"] = L($"* Days to make costumes and props.", $"* Дней на изготовление костюмов и реквизита."),
                ["costumes_and_props_quality_budget"] = L($"* Budget tiers for costume quality.", $"* Бюджетные уровни качества костюмов."),

                ["sets_time_red_1"] = L($"* 10% time reduction for set building.", $"* 10% ускорение строительства декораций."),
                ["sets_time_red_2"] = L($"* 20% time reduction for set building.", $"* 20% ускорение строительства декораций."),
                ["sets_time_red_3"] = L($"* 30% time reduction for set building.", $"* 30% ускорение строительства декораций."),

                ["sound_inhouse_improved"] = L($"* Improved in-house voiceover quality.", $"* Улучшенное качество внутристудийной озвучки."),
                ["sound_inhouse_time_1"] = L($"* Voiceover production speed.", $"* Скорость производства озвучки."),
                ["other_sound_fraction"] = L($"* Fraction of audio outsourced.", $"* Доля аудио, отдаваемая на аутсорс."),

                ["montage_fraction"] = L($"* Editing fraction of post-production.", $"* Доля постпродакшна, уходящая на монтаж."),
                ["film_editor_bonus_fraction"] = L($"* Quality multiplier from film editor.", $"* Множитель качества от режиссёра монтажа."),
                ["postprod_montage_base_cost"] = L($"* Base editing cost.", $"* Базовая стоимость монтажа."),

                ["effects_quality_1"] = L($"* Basic effect quality level.", $"* Базовый уровень качества эффектов."),
                ["effects_quality_2"] = L($"* Intermediate effect quality level.", $"* Средний уровень качества эффектов."),
                ["effects_quality_3"] = L($"* High effect quality level.", $"* Высокий уровень качества эффектов."),

                ["composer_bonus_fraction"] = L($"* Quality multiplier from composer. 0.5 = up to +50%.", $"* Множитель качества от композитора. 0.5 = до +50%."),
                ["composer_payment_range"] = L($"* Composer salary range.", $"* Диапазон зарплат композитора."),
                ["music_fraction"] = L($"* Music fraction of audio budget.", $"* Доля музыки в аудиобюджете."),

                ["tech_improvement_days_per_point_average"] = L($"* Days per point for average tech improvement.", $"* Дней за пункт для среднего улучшения технологии."),
                ["tech_creation_days_per_point_average"] = L($"* Days per point for average tech creation.", $"* Дней за пункт для среднего создания технологии."),
                ["tech_sell_point_cost_base"] = L($"* Base tech sale price per point.", $"* Базовая цена продажи технологии за пункт."),
                ["tech_creation_days_per_point_below_average"] = L($"* Days per point for below-average tech.", $"* Дней за пункт для технологии ниже среднего."),
                ["tech_creation_days_per_point_above_average"] = L($"* Days per point for above-average tech.", $"* Дней за пункт для технологии выше среднего."),
                ["tech_improvement_total_duration_multiplier"] = L($"* Total duration multiplier for tech improvement.", $"* Множитель общей длительности улучшения технологии."),
                ["tech_improvement_days_per_point_below_average"] = L($"* Improvement days for below-average tech.", $"* Дней улучшения для технологии ниже среднего."),
                ["tech_improvement_days_per_point_above_average"] = L($"* Improvement days for above-average tech.", $"* Дней улучшения для технологии выше среднего."),
                ["tech_improvement_red_time_per_rnd"] = L($"* Time reduction per R&D level. 0.1 = -10%.", $"* Снижение времени за уровень. 0.1 = -10%."),
                ["tech_creation_red_time_per_rnd"] = L($"* Creation time reduction per R&D level.", $"* Снижение времени создания за уровень."),

                ["talents_xp_for_level"] = L($"* XP required for talents to level up.", $"* Опыт для повышения уровня талантов."),
                ["lieutenants_xp_for_level"] = L($"* XP required for lieutenants to level up.", $"* Опыт для повышения уровня лейтенантов."),
                ["agents_xp_for_level"] = L($"* XP required for agents to level up.", $"* Опыт для повышения уровня агентов."),

                ["contract_termination_fee_1"] = L($"* Termination fee as % of contract value. 0.5 = 50%.", $"* Штраф за разрыв контракта в % от стоимости. 0.5 = 50%."),
                ["contract_termination_fee_2"] = L($"* Termination fee at higher severity.", $"* Штраф за разрыв при высокой тяжести нарушения."),
                ["staff_raise_request_ignored_demanded_salary_increase"] = L($"* Salary increase demanded if raise ignored.", $"* На сколько повышают зарплату, если проигнорировать просьбу."),

                ["trial_win_chance_by_severity"] = L($"* Base win chance by crime severity.", $"* Базовый шанс выиграть суд по тяжести преступления."),
                ["trial_influence_bonus_value"] = L($"* Influence bonus multiplier in court.", $"* Множитель бонуса влияния в суде."),
                ["legal_defence_cost"] = L($"* Legal defense cost tiers.", $"* Уровни стоимости юридической защиты."),

                ["police_raid_bribe_cost"] = L($"* Bribe cost during police raid.", $"* Стоимость взятки при обыске."),
                ["cash_seizure_ratio_range"] = L($"* Percentage of cash seized during raid.", $"* Процент конфискуемых денег при обыске."),
                ["penalty_per_illegal_worker"] = L($"* Fine per illegal worker found.", $"* Штраф за каждого нелегального работника."),

                ["good_gay_review_baseline"] = L($"* Baseline review score from gay critics. 0.7 = 7/10.", $"* Базовый уровень рецензий от ЛГБТ-критиков. 0.7 = 7/10."),
                ["good_woman_review_baseline"] = L($"* Baseline review score from female critics.", $"* Базовый уровень рецензий от женщин-критиков."),

                ["tax_base_reduction_1"] = L($"* Tax reduction from first upgrade.", $"* Снижение налогов после первого улучшения."),
                ["tax_base_reduction_2"] = L($"* Tax reduction from second upgrade.", $"* Снижение налогов после второго улучшения."),
                ["cinema_sell_cost_modificator"] = L($"* Money returned when selling a cinema. 0.7 = 70%.", $"* Сколько возвращается при продаже кинотеатра. 0.7 = 70%."),
            };

            if (customHints.TryGetValue(key, out string hint))
                return hint;

            if (variable.Value.Contains("_"))
                return L($"* Range value: min_max format. Keep the underscore.", $"* Диапазон: формат мин_макс. Нижнее и верхнее значение через подчёркивание.");

            if (variable.Value.Contains(";"))
                return L($"* List of values. Order usually matters.", $"* Список значений. Порядок обычно важен.");

            return L($"* Original game value is {value}. Change carefully.", $"* Исходное значение: {value}. Меняйте осторожно.");
        }

        private void BuildUI()
        {
            MainPanel.Children.Clear();

            if (currentConfigName == "Buildings")
                AddBuildingsBulkDurationPanel();

            var grouped = variables.Values
                .GroupBy(v => currentConfigName == "Buildings" ? v.Category : (paramCategories.ContainsKey(v.Key) ? paramCategories[v.Key] : "other"))
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var categoryInfo = currentConfigName == "Buildings"
                    ? new CategoryInfo { Title = GetBuildingDisplayTitle(group.Key), Icon = "🏗️" }
                    : (categories.ContainsKey(group.Key) ? categories[group.Key] : new CategoryInfo { Title = group.Key, Icon = "📁" });

                var border = new Border
                {
                    BorderThickness = new Thickness(1.5),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(5, 5, 5, 12),
                    Background = new SolidColorBrush(Color.FromArgb(0x20, 0xAD, 0x38, 0x38))
                };

                var innerGrid = new Grid();
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var headerBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var headerText = new TextBlock
                {
                    Text = $"{categoryInfo.Icon}  {categoryInfo.Title}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(12, 6, 12, 6),
                    Foreground = Brushes.White
                };
                headerBorder.Child = headerText;

                Grid.SetRow(headerBorder, 0);
                innerGrid.Children.Add(headerBorder);

                var paramsGrid = new Grid
                {
                    Margin = new Thickness(15, 5, 15, 12),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270, GridUnitType.Pixel) });
                paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75, GridUnitType.Pixel) });
                paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                int rowIndex = 0;
                foreach (var variable in group.OrderBy(v => currentConfigName == "Buildings" ? v.FieldName : v.Key))
                {
                    paramsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    string displayName = GetDisplayName(variable.Key);
                    string unit = GetUnit(variable.Key);
                    string hint = GetHint(variable);
                    UIElement editor = CreateValueEditor(variable);

                    var nameLabel = new Label
                    {
                        Content = displayName,
                        FontSize = 12,
                        FontWeight = FontWeights.Normal,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        ToolTip = currentConfigName == "Buildings" ? $"{GetBuildingDisplayTitle(variable.Category)} / {displayName}" : variable.Key,
                        Margin = new Thickness(0, 5, 0, 5),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetRow(nameLabel, rowIndex);
                    Grid.SetColumn(nameLabel, 0);

                    Grid.SetRow(editor, rowIndex);
                    Grid.SetColumn(editor, 1);

                    var unitLabel = new Label
                    {
                        Content = unit,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetRow(unitLabel, rowIndex);
                    Grid.SetColumn(unitLabel, 2);

                    var hintText = new TextBlock
                    {
                        Text = hint,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8)),
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10, 0, 0, 0),
                        ToolTip = hint
                    };
                    Grid.SetRow(hintText, rowIndex);
                    Grid.SetColumn(hintText, 3);

                    paramsGrid.Children.Add(nameLabel);
                    paramsGrid.Children.Add(editor);
                    paramsGrid.Children.Add(unitLabel);
                    paramsGrid.Children.Add(hintText);

                    rowIndex++;
                }

                Grid.SetRow(paramsGrid, 1);
                innerGrid.Children.Add(paramsGrid);
                border.Child = innerGrid;
                MainPanel.Children.Add(border);
            }
        }

        private void AddBuildingsBulkDurationPanel()
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1.5),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xAD, 0x38, 0x38)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(5, 5, 5, 12),
                Background = new SolidColorBrush(Color.FromArgb(0x20, 0xAD, 0x38, 0x38))
            };
            var stack = new StackPanel { Margin = new Thickness(15, 12, 15, 12) };
            stack.Children.Add(new TextBlock
            {
                Text = L("🏗️ Base duration for all buildings", "🏗️ Базовая длительность для всех построек"),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            });
            stack.Children.Add(new TextBlock
            {
                Text = L("Sets the same construction time for every building entry below. Handy for quick balance tests: for example, 1–5 days for a fast sandbox or 60+ days for slower development.",
                         "Выставляет одинаковое время строительства для всех построек ниже. Удобно для быстрой проверки баланса: например, 1–5 дней для песочницы или 60+ дней для более медленного развития."),
                Foreground = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var box = new TextBox { Width = 100, Height = 24, Text = "30", Margin = new Thickness(0, 0, 8, 0) };
            var unit = new Label { Content = L("days", "дни"), Margin = new Thickness(0, 0, 12, 0) };
            var btn = new Button
            {
                Content = L("Apply to all", "Применить ко всем"),
                Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
                Foreground = Brushes.White,
                Padding = new Thickness(10, 4, 10, 4),
                MinWidth = 130
            };
            btn.Click += (sender, args) =>
            {
                string newValue = box.Text.Trim();
                if (string.IsNullOrWhiteSpace(newValue)) return;
                int changed = 0;
                foreach (var v in variables.Values.Where(v => string.Equals(v.FieldName, "baseDuration", StringComparison.OrdinalIgnoreCase)))
                {
                    v.Value = newValue;
                    changed++;
                }
                BuildUI();
                MessageBox.Show(
                    L($"Base duration was changed for {changed} buildings. Click Save when you are ready to write it to Buildings.json.",
                      $"Базовая длительность изменена у {changed} построек. Когда всё проверишь, нажми «Сохранить», чтобы записать это в Buildings.json."),
                    L("Done", "Готово"), MessageBoxButton.OK, MessageBoxImage.Information);
            };
            row.Children.Add(box);
            row.Children.Add(unit);
            row.Children.Add(btn);
            stack.Children.Add(row);
            border.Child = stack;
            MainPanel.Children.Add(border);
        }

        private object ConvertValuePreservingType(string text, JTokenType type)
        {
            if (type == JTokenType.Integer && long.TryParse(text, out long i)) return i;
            if (type == JTokenType.Float && TryParseFlexibleDouble(text, out double d)) return d;
            if (type == JTokenType.Boolean && bool.TryParse(text, out bool b)) return b;
            return text;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var variable in variables.Values)
                {
                    if (variable.Value != variable.OriginalValue)
                    {
                        var token = variable.RawToken;
                        if (token != null)
                        {
                            if (token is JValue valueToken)
                            {
                                valueToken.Value = ConvertValuePreservingType(variable.Value, valueToken.Type);
                            }
                            else if (token["Value"] != null)
                            {
                                token["Value"] = variable.Value;
                            }
                            else
                            {
                                var parent = token.Parent;
                                if (parent is JProperty prop)
                                {
                                    prop.Value = variable.Value;
                                }
                            }
                        }
                    }
                }

                string json = configData.ToString(Formatting.Indented);
                File.WriteAllText(configFilePath, json);

                MessageBox.Show(
                    L("Settings saved successfully!\n\nRestart the game for changes to take effect.",
                      "Настройки успешно сохранены!\n\nПерезапустите игру, чтобы изменения вступили в силу."),
                    L("Success", "Успешно"), MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(L($"Error saving config file:\n{ex.Message}", $"Ошибка сохранения файла конфигурации:\n{ex.Message}"), L("Error", "Ошибка"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class GameVariable : INotifyPropertyChanged
    {
        private string _key;
        private string _value;
        private string _originalValue;
        private JToken _rawToken;
        private string _category;
        private string _fieldName;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(nameof(Key)); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        public string OriginalValue
        {
            get => _originalValue;
            set { _originalValue = value; OnPropertyChanged(nameof(OriginalValue)); }
        }

        public JToken RawToken
        {
            get => _rawToken;
            set { _rawToken = value; OnPropertyChanged(nameof(RawToken)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public string FieldName
        {
            get => _fieldName;
            set { _fieldName = value; OnPropertyChanged(nameof(FieldName)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class FieldStats
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }

    public class CategoryInfo
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
    }
}
