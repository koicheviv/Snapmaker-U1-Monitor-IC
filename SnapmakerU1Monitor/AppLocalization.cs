using System.Collections.Generic;
using Microsoft.Maui.Storage;

namespace SnapmakerU1Monitor;

public static class AppLocalization
{
    private const string LanguagePreferenceKey = "app_language";

    private static string _language =
        Preferences.Get(LanguagePreferenceKey, "en");

    private static readonly Dictionary<string, string> English = new()
    {
        ["app_title"] = "Snapmaker U1 Monitor IC",
        ["settings"] = "SETTINGS",
        ["elapsed_time"] = "ELAPSED TIME",
        ["remaining_time"] = "TIME REMAINING",
        ["expected_finish"] = "ESTIMATED FINISH",
        ["footer"] = "Snapmaker U1 Monitor IC",
        ["no_active_print"] = "No active print",
        ["calculating"] = "Calculating...",
        ["screen_management"] = "Screen management",
        ["screen_management_message"] = "Device administrator permission is required to automatically turn the screen off and wake it when a new print starts.",
        ["continue"] = "Continue",
        ["later"] = "Later",
        ["ip_prompt"] = "Enter the IP address of your Snapmaker U1.\n\nDefault Moonraker port: 7125.\nYou can change it later in Advanced Settings.",
        ["save"] = "Save",
        ["cancel"] = "Cancel",
        ["invalid_address_title"] = "Invalid address",
        ["invalid_address_message"] = "Enter a valid IP address or printer hostname.",
        ["ok"] = "OK",
        ["connecting"] = "Connecting to Snapmaker U1...",
        ["advanced_settings"] = "Advanced Settings",
        ["moonraker_port"] = "Moonraker port:",
        ["invalid_port_title"] = "Invalid port",
        ["invalid_port_message"] = "The port must be a number between 1 and 65535.",
        ["missing_ip_title"] = "Missing IP address",
        ["missing_ip_message"] = "Enter the printer IP address first.",
        ["close"] = "Close",
        ["change_ip"] = "Change printer IP",
        ["advanced_port"] = "Moonraker port (Advanced)",
        ["language"] = "Language",
        ["english"] = "English",
        ["bulgarian"] = "Bulgarian",
        ["language_changed"] = "Language preference saved.",
        ["settings_error"] = "Settings error",
        ["setup_required_message"] = "No printer configured.\n\nOpen SETTINGS and enter the Snapmaker U1 IP address.\n\nDefault Moonraker port: 7125.",
        ["offline_heading"] = "NO CONNECTION TO PRINTER",
        ["offline_check_list"] = "Please check:",
        ["offline_wifi"] = "The phone and printer are on the same Wi-Fi network.",
        ["offline_ip"] = "The printer IP address is correct.",
        ["offline_power"] = "The printer is powered on.",
        ["offline_port"] = "Moonraker is using port 7125 (unless changed in Advanced Settings).",
        ["offline_settings"] = "Open SETTINGS to review the connection settings.",
        ["details"] = "Details",
        ["app_error_message"] = "An application error occurred:",
        ["printer_reported_error"] = "The printer reported an error.",
        ["klipper_shutdown"] = "Klipper is in SHUTDOWN state.",
        ["paused"] = "PAUSED",
        ["finished"] = "FINISHED",
        ["cancelled"] = "CANCELLED",
        ["error"] = "ERROR",
        ["printer_error_prefix"] = "Printer message:",
        ["connection_timeout"] = "The connection timed out after 5 seconds.",
        ["invalid_response"] = "The printer returned data in an unexpected format:",
    };

    private static readonly Dictionary<string, string> Bulgarian = new()
    {
        ["app_title"] = "Snapmaker U1 Monitor IC",
        ["settings"] = "SETTINGS",
        ["elapsed_time"] = "ИЗМИНАЛО ВРЕМЕ",
        ["remaining_time"] = "ОСТАВАЩО ВРЕМЕ",
        ["expected_finish"] = "ОЧАКВАН КРАЙ НА ПЕЧАТА",
        ["footer"] = "Snapmaker U1 Monitor IC",
        ["no_active_print"] = "Няма активен печат",
        ["calculating"] = "Изчисляване...",
        ["screen_management"] = "Управление на дисплея",
        ["screen_management_message"] = "Необходимо е администраторско разрешение за автоматично изключване на дисплея и събуждането му при нов печат.",
        ["continue"] = "Продължи",
        ["later"] = "По-късно",
        ["ip_prompt"] = "Въведи IP адреса на Snapmaker U1.\n\nСтандартен Moonraker порт: 7125.\nМожеш да го промениш по-късно от Разширени настройки.",
        ["save"] = "Save",
        ["cancel"] = "Cancel",
        ["invalid_address_title"] = "Невалиден адрес",
        ["invalid_address_message"] = "Въведи валиден IP адрес или име на принтера.",
        ["ok"] = "OK",
        ["connecting"] = "Свързване със Snapmaker U1...",
        ["advanced_settings"] = "Advanced Settings",
        ["moonraker_port"] = "Moonraker port:",
        ["invalid_port_title"] = "Невалиден порт",
        ["invalid_port_message"] = "Портът трябва да е число между 1 и 65535.",
        ["missing_ip_title"] = "Липсва IP адрес",
        ["missing_ip_message"] = "Първо въведи IP адреса на принтера.",
        ["close"] = "Close",
        ["change_ip"] = "Change printer IP",
        ["advanced_port"] = "Moonraker port (Advanced)",
        ["language"] = "Language",
        ["english"] = "English",
        ["bulgarian"] = "Bulgarian",
        ["language_changed"] = "Language preference saved.",
        ["settings_error"] = "Settings error",
        ["setup_required_message"] = "Няма настроен принтер.\n\nОтвори НАСТРОЙКИ и въведи IP адреса на Snapmaker U1.\n\nСтандартен Moonraker порт: 7125.",
        ["offline_heading"] = "НЯМА ВРЪЗКА С ПРИНТЕРА",
        ["offline_check_list"] = "Провери следното:",
        ["offline_wifi"] = "Телефонът и принтерът са в една Wi-Fi мрежа.",
        ["offline_ip"] = "IP адресът на принтера е правилен.",
        ["offline_power"] = "Принтерът е включен.",
        ["offline_port"] = "Moonraker използва порт 7125 (освен ако не е променен в Разширени настройки).",
        ["offline_settings"] = "Отвори НАСТРОЙКИ, за да провериш връзката.",
        ["details"] = "Технически подробности",
        ["app_error_message"] = "Възникна грешка в приложението:",
        ["printer_reported_error"] = "Принтерът съобщава за грешка.",
        ["klipper_shutdown"] = "Klipper е в състояние SHUTDOWN.",
        ["paused"] = "ПАУЗА",
        ["finished"] = "ЗАВЪРШЕН",
        ["cancelled"] = "ОТКАЗАН",
        ["error"] = "ГРЕШКА",
        ["printer_error_prefix"] = "Съобщение от принтера:",
        ["connection_timeout"] = "Връзката изтече след 5 секунди.",
        ["invalid_response"] = "Принтерът върна данни в неочакван формат:",
    };

    public static string CurrentLanguage => _language;

    public static void SetLanguage(string language)
    {
        _language = string.Equals(
            language,
            "bg",
            System.StringComparison.OrdinalIgnoreCase)
            ? "bg"
            : "en";

        Preferences.Set(LanguagePreferenceKey, _language);
    }

    public static string Get(string key)
    {
        var dictionary = _language == "bg" ? Bulgarian : English;
        return dictionary.TryGetValue(key, out string? value)
            ? value
            : key;
    }

    public static string Status(string rawStatus)
    {
        string status = rawStatus.Trim().ToUpperInvariant();

        if (_language != "bg")
            return status;

        return status switch
        {
            "IDLE" => "ГОТОВ",
            "STANDBY" => "ГОТОВ",
            "READY" => "ГОТОВ",
            "PRINTING" => "ПЕЧАТ",
            "PAUSED" => "НА ПАУЗА",
            "COMPLETE" => "ЗАВЪРШЕН",
            "CANCELLED" => "ОТКАЗАН",
            "ERROR" => "ГРЕШКА",
            "SHUTDOWN" => "АВАРИЙНО СПИРАНЕ",
            "STARTUP" => "СТАРТИРАНЕ",
            "OFFLINE" => "НЯМА ВРЪЗКА",
            "SETUP" => "НАСТРОЙКА",
            "CONNECTING" => "СВЪРЗВАНЕ",
            "UNKNOWN" => "НЕИЗВЕСТНО",
            "APP ERROR" => "ГРЕШКА В ПРИЛОЖЕНИЕТО",
            _ => status
        };
    }
}
