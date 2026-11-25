# Notify privacy policy

*Last updated: 25 November 2026*

Notify is a system-tray utility that shows your Wi-Fi link speed.

**Notify does not collect, store remotely, share or sell any personal data.** It has no accounts, no analytics, no telemetry and no adverts, and it makes no network connections of its own.

## What it reads, and where that stays

- **Wi-Fi connection details**: link speed, network name (SSID), signal strength and Wi-Fi standard, read from Windows on your PC so they can be shown in the tray and flyout. They are never written to disk or sent anywhere.
- **Location permission**: Windows treats Wi-Fi network names as location information, so it only gives Notify the network name and signal strength if you allow location access. Notify doesn't read your location itself and works without it (it then shows only the speed).
- **Settings**: your choices (adapter, refresh interval, colour, which rate to show) are saved in a small file on your PC (`%APPDATA%\Notify\settings.json`, or the app's own storage folder for the Store version).

## Contact

Questions: open an issue at https://github.com/Hercules187/Notify/issues.
