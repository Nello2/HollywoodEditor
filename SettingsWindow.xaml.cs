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
        }

        private string configFilePath;
        private JObject configData;
        private Dictionary<string, GameVariable> variables;
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

        // Словари
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

        // Единицы измерения
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

        // Категории параметров
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
            FindAndLoadConfig();
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

        private void FindAndLoadConfig()
        {
            try
            {
                string foundPath = null;

                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady)
                    {
                        string possiblePath = Path.Combine(drive.RootDirectory.FullName, "Steam", "steamapps", "common", "Hollywood Animal");

                        if (Directory.Exists(possiblePath))
                        {
                            string configPath = Path.Combine(possiblePath, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", "GameVariables.json");
                            if (File.Exists(configPath))
                            {
                                foundPath = configPath;
                                break;
                            }
                        }

                        possiblePath = Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam", "steamapps", "common", "Hollywood Animal");
                        if (Directory.Exists(possiblePath))
                        {
                            string configPath = Path.Combine(possiblePath, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", "GameVariables.json");
                            if (File.Exists(configPath))
                            {
                                foundPath = configPath;
                                break;
                            }
                        }

                        possiblePath = Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam", "steamapps", "common", "Hollywood Animal");
                        if (Directory.Exists(possiblePath))
                        {
                            string configPath = Path.Combine(possiblePath, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", "GameVariables.json");
                            if (File.Exists(configPath))
                            {
                                foundPath = configPath;
                                break;
                            }
                        }

                        string[] gameFolders = new[] { "Games", "Games2", "GAMES", "games", "Игры" };
                        foreach (string gameFolder in gameFolders)
                        {
                            possiblePath = Path.Combine(drive.RootDirectory.FullName, gameFolder, "Hollywood Animal");
                            if (Directory.Exists(possiblePath))
                            {
                                string configPath = Path.Combine(possiblePath, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", "GameVariables.json");
                                if (File.Exists(configPath))
                                {
                                    foundPath = configPath;
                                    break;
                                }
                            }
                        }

                        if (foundPath != null) break;

                        try
                        {
                            var foundDirs = Directory.GetDirectories(drive.RootDirectory.FullName, "Hollywood Animal", SearchOption.AllDirectories);
                            foreach (var dir in foundDirs)
                            {
                                string configPath = Path.Combine(dir, "Hollywood Animal_Data", "StreamingAssets", "Data", "Configs", "GameVariables.json");
                                if (File.Exists(configPath))
                                {
                                    foundPath = configPath;
                                    break;
                                }
                            }
                        }
                        catch (UnauthorizedAccessException) { }
                        catch (PathTooLongException) { }
                    }

                    if (foundPath != null) break;
                }

                if (foundPath != null && File.Exists(foundPath))
                {
                    configFilePath = foundPath;
                    LoadConfig(configFilePath);
                }
                else
                {
                    var result = MessageBox.Show("GameVariables.json not found!\n\nWould you like to select the file manually?\n\nThe file should be located in:\n...\\Hollywood Animal\\Hollywood Animal_Data\\StreamingAssets\\Data\\Configs\\GameVariables.json",
                        "File Not Found", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        var dialog = new OpenFileDialog
                        {
                            Title = L("Select GameVariables.json", "Выберите GameVariables.json"),
                            Filter = L("JSON files (*.json)|*.json", "JSON-файлы (*.json)|*.json"),
                            DefaultExt = ".json"
                        };

                        if (dialog.ShowDialog() == true)
                        {
                            configFilePath = dialog.FileName;
                            LoadConfig(configFilePath);
                        }
                        else
                        {
                            DialogResult = false;
                            Close();
                        }
                    }
                    else
                    {
                        DialogResult = false;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error finding config: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void LoadConfig(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                configData = JObject.Parse(json);
                variables.Clear();

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
                            RawToken = token
                        };
                    }
                }

                BuildUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading config file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private string GetDisplayName(string key)
        {
            var value = displayNames.ContainsKey(key) ? displayNames[key] : key;
            return TranslateDisplayName(value);
        }

        private string GetUnit(string key)
        {
            var value = units.ContainsKey(key) ? units[key] : "";
            return TranslateUnit(value);
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

        private void BuildUI()
        {
            MainPanel.Children.Clear();

            var grouped = variables.Values
                .Where(v => paramCategories.ContainsKey(v.Key))
                .GroupBy(v => paramCategories[v.Key])
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var categoryInfo = categories.ContainsKey(group.Key) ? categories[group.Key] : new CategoryInfo { Title = group.Key, Icon = "📁" };

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

                var paramsGrid = new Grid();
                paramsGrid.Margin = new Thickness(15, 5, 15, 12);
                paramsGrid.HorizontalAlignment = HorizontalAlignment.Stretch;

                paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280, GridUnitType.Pixel) });
                paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                paramsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                int rowIndex = 0;
                foreach (var variable in group.OrderBy(v => v.Key))
                {
                    paramsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    string displayName = GetDisplayName(variable.Key);
                    string unit = GetUnit(variable.Key);
                    UIElement editor = CreateValueEditor(variable);

                    var nameLabel = new Label
                    {
                        Content = displayName,
                        FontSize = 12,
                        FontWeight = FontWeights.Normal,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        ToolTip = variable.Key,
                        Margin = new Thickness(0, 5, 0, 5),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetRow(nameLabel, rowIndex);
                    Grid.SetColumn(nameLabel, 0);

                    Grid.SetRow(editor, rowIndex);
                    Grid.SetColumn(editor, 1);

                    if (!string.IsNullOrEmpty(unit))
                    {
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
                        paramsGrid.Children.Add(unitLabel);
                    }

                    paramsGrid.Children.Add(nameLabel);
                    paramsGrid.Children.Add(editor);

                    rowIndex++;
                }

                Grid.SetRow(paramsGrid, 1);
                innerGrid.Children.Add(paramsGrid);
                border.Child = innerGrid;
                MainPanel.Children.Add(border);
            }
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
                            if (token["Value"] != null)
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

                MessageBox.Show("Settings saved successfully!\n\nRestart the game for changes to take effect.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving config file:\n{ex.Message}", "Error",
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class CategoryInfo
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
    }
}