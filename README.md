<div align="center">
  <img height="200" src="https://i.imgur.com/WHRggSV.png"  />
</div>
<div align="center">
  <a href="https://github.com/Nello2">
    <img alt="GitHub" src="https://img.shields.io/badge/GitHub-black?style=for-the-badge&logo=github"></a><a href="https://gitverse.ru/Galapogos/HollywoodEditor"><img alt="Static Badge" src="https://img.shields.io/badge/GitVerse-blue?style=for-the-badge&logo=git&logoColor=white"><a href="https://www.playground.ru/hollywood_animal/cheat/hollywood_editor_obnovlennyj_redaktor_sohranenij_0_8_54ea_galapogos_bioyakor-1797123"><img alt="Static Badge" src="https://img.shields.io/badge/Playground-red?style=for-the-badge&logo=precommit&logoColor=white">
</div>
<p align="center">
  <a href="https://www.gnu.org/licenses/agpl-3.0">
    <img src="https://img.shields.io/badge/License-AGPL_v3-blue.svg" alt="License: AGPL v3">
  </a>
  <a href="https://gitverse.ru/Galapogos/HollywoodEditor">
    <img src="https://img.shields.io/badge/version-russian-red" alt="Russian Version">
  </a>
  <a href="https://telegra.ph/Hollywood-Editor-10-10">
    <img src="https://img.shields.io/badge/version-english-green" alt="English Version">
  </a>
</p>

# Hollywood Editor
An updated tool for safely editing save files of the game Hollywood Animal.

## Note
This project is a continuation of the project [HollyJson](https://github.com/BioNoob/HollyJson?ysclid=mgk0zm7rb6682119807), originally developed by [BioNoob (Yakor)](https://github.com/BioNoob).  
After the transfer of the source code and cessation of active development by the original author, the project was taken over by [me](https://github.com/Nello2) for maintenance and further development.

## Features and Functionality

*   **Save File Editing:** Allows users to open and modify save files in JSON format.
*   **Character Management:** Provides functionality to view, filter, and edit character attributes, including:
    *   Basic Information: Name, age, portrait.
    *   Attributes: Mood, attitude, limit.
    *   Contract Details: Modify contract terms, salary, signing date, etc.
    *   Skills and Traits: Add or remove skills and traits.
    *   Studio Affiliation: Change the studio the character is bound to.
*   **Data Validation:** Implements input data validation to ensure integrity for fields such as age, floating-point numbers, and strings.
*   **Localization Support:** Reads localization data from JSON files to display translated names and descriptions.
*   **Resource Management:** Extracts resource files (images, localization) from compressed archives on startup.
*   **Save File Editing:** Allows users to open and modify save files in JSON format. Supports loading saves from the game's standard profile folder (`LocalLow\Weappy\Hollywood Animal\Saves\Profiles`) and saving changes while preserving the complete data structure. During loading, progress is displayed in the status bar, indicating the number of processed characters.
*   **Study Manager:** Provides a tree view of all available and unlocked studies, grouped by departments (TECH, PRODUCTION, LEGAL, HR, PR, SCRIPT, PREPROD, SECURITY, COMFORT, DISTRIBUTION, POST, FINANCE). Each department displays its current progress level. Unlocked studies are highlighted in green, locked ones in red. A dependency system is implemented: attempting to unlock a technology without meeting requirements shows an informational message. Supports mass unlocking of all available studies and locking all unlocked ones, with a notification about the number of changes. Locking a study automatically locks all technologies dependent on it.
*   **Tags Manager:** Provides a two-panel interface for managing unlocked and locked tags. Locked tags are displayed in the left panel with red highlighting, unlocked tags in the right panel with green highlighting. Supports multi-selection for mass unlocking or locking of tags. Quick operation buttons are implemented: "Open All", "Close All", as well as moving selected items between panels.
*   **Portrait Editing (Select Portrait):** Allows changing a character's portrait with automatic category determination based on profession (TALENT, AGENT, LIEUT). Supports filtering by gender and age category (YOUNG, MID, OLD). Pagination is implemented for convenient navigation through a large number of images. When the window loads, the character's current portrait is automatically highlighted. This functionality is based on developments from the special version 0.2.3S.B, adapted for the current game version.
*   **Game Settings Editor:** Allows opening and editing the `GameVariables.json` file, which contains core game settings. Over 250 parameters are available for editing, grouped into thematic categories. Each category is presented as a separate block with a colored border, title, and icon. For each parameter, the corresponding units of measurement (days, $, %, x, months, years, etc.) are indicated. Both single values and ranges with two separate input fields are supported. Automatic search for the configuration file via standard Steam installation paths is implemented, with manual selection as a fallback.
*   **Spawn Dates Viewer:** Displays a list of all professions in a separate window, indicating the date of the next spawn for a character of that profession. Each entry is presented in a two-column format: the localized profession name and the date in `dd.MM.yyyy` format. Dates are highlighted in green for emphasis.
*   **Data Validation:** Implements multi-level validation of input data to ensure integrity:
    *   Numeric fields are checked for allowed characters and value ranges.
    *   Age is limited to reasonable bounds (0–150 years).
    *   Mood, attitude, and limit values are constrained to the range 0 to 1.
    *   Text fields are checked for allowed characters (letters and spaces).
    *   When pasting from the clipboard, additional validation is performed, converting values if necessary.
*   **Localization Support:** Reads localization data from JSON files (`CHARACTER_NAMES.json` and `NON_EVENT.json`) to display translated names for professions, studios, skills, traits, and other UI elements. When localization is loaded, displayed character names and filters are automatically updated. Manual loading of localization files is possible via a button with a user icon.
*   **Resource Management:** Extracts resource files (localization, portrait images) from compressed archives on startup. When the portrait selection window is opened for the first time, the `Profiles.zip` archive is automatically extracted. If necessary archives are missing, informative messages with recommendations are displayed.
*   **Error Handling and Stability:** Global unhandled exception handling is implemented to prevent unexpected application crashes. All file and JSON operations are wrapped in try-catch blocks with informative error messages. When loading a save, progress is displayed in the status bar, and detailed error information with the cause is shown if errors occur.
*   **Saving Changes:** When saving changes, full data synchronization with the original JSON object is performed. All modified fields are updated: character attributes, contracts, skills, traits, portraits, as well as global game settings. Upon successful save, the "Save" button turns green for one second, confirming the successful operation.

## Technology Stack

*   **C#:** Primary programming language.
*   **WPF:** Windows Presentation Foundation for the user interface.
*   **Newtonsoft.Json:** Library for JSON serialization and deserialization.
*   **PropertyChanged.Fody:** Library that automatically implements the `INotifyPropertyChanged` interface.
*   **System.IO.Compression:** Used for extracting resources from archives.

## Prerequisites

*   Windows operating system
*   .NET Framework 4.7.2 or newer (required for WPF) - should be pre-installed on modern versions of Windows.

## Installation Instructions

1.  Download the latest version from the [GitHub repository](https://github.com/Nello2/HollywoodEditor).
2.  Extract the downloaded archive to a directory of your choice.
3.  Run the executable file `HollywoodEditor.exe`.

## Usage Guide

1.  **Opening a Save File:**
    *   Click the "Open File" button.
    *   Select "OFD" to open a save file. Save files are located in `%localappdata%Low\Weappy\Hollywood Animal\Saves\Profiles`.

2.  **Selecting a Locale:**
    *   Click the "Open File" button and select the directory with the locale JSON.
    *   Navigate to the Hollywood Animal installation directory, then to `Hollywood Animal_Data\StreamingAssets\Data\Localization\RUS`.
    *   Select the `RUS` folder containing the localization files.
    *   Click "Select Folder".

3.  **Filtering Characters:**
    *   Use the `Filter_studio` dropdown to filter characters by the studio they belong to.
    *   Use the `Filter_Prof` dropdown to filter characters by their profession.
    *   Enter text in the `Filter_txt` text box to search for characters by name.
    *   Use the `ShowOnlyTalent` checkbox to show only talents (characters).
    *   Use the `ShowOnlyDead` checkbox to show only dead characters.
    *   Use the `ShowWithDead` checkbox to include dead characters in the results.

4.  **Editing Character Attributes:**
    *   Select a character from the list.
    *   Modify attributes in the character details section.
    *   Text fields use the `Tag` property for input validation:
        *   `STR`: Allows string input.
        *   `INT`: Allows integer input.
        *   `AGE`: Allows integer input up to 150.
        *   `DBL`: Allows floating-point number input.
        *   `LMT`: Allows floating-point number input from 0 to 1.

5.  **Adding/Removing Skills and Traits:**
    *   Select a character from the list.
    *   To add a skill, select it from the dropdown and click the "+" button.
    *   To remove a skill, click the "X" button next to the skill in the list.
    *   To add a trait, select it from the dropdown and click the "+" button.
    *   To remove a trait, click the "X" button next to the trait in the list.

6.  **Saving Changes:**
    *   Click the "Save" button.
    *   Choose a location to save the modified save file. It is recommended to create backups of the original save files.

## Contribution Guidelines

1.  Fork the repository.
2.  Create a new branch for your feature or bug fix.
3.  Implement your changes, ensuring code quality and adding appropriate tests.
4.  Submit a pull request with a clear description of your changes.
