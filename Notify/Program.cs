/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program. If not, see <https://www.gnu.org/licenses/>.
*/

namespace Notify;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // One copy per user session: a second launch just exits.
        using var mutex = new Mutex(initiallyOwned: true, @"Local\Notify.WifiSpeedTray", out bool isFirst);
        if (!isFirst) return;

        ApplicationConfiguration.Initialize();          // DPI awareness, visual styles, default font (from the .csproj)
        Application.SetColorMode(SystemColorMode.System); // follow Windows light/dark mode
        Application.Run(new TrayContext());
    }
}
