/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using System.Net.NetworkInformation;

namespace Notify;

/// <summary>One reading for the tray: what to show and why.</summary>
public sealed record SpeedReading(
    bool Connected,
    double RxMbps,
    double TxMbps,
    string AdapterName,
    string? Ssid,
    int? SignalQuality,
    string? PhyName,
    bool LocationBlocked = false)
{
    public static SpeedReading Disconnected(string adapter) => new(false, 0, 0, adapter, null, null, null);
}

/// <summary>An adapter the user can pick in Settings.</summary>
public sealed record AdapterChoice(string Id, string Display)
{
    public override string ToString() => Display;
}

internal static class NetworkReader
{
    public const string Automatic = "auto";

    /// <summary>"Automatic" first, then Wi-Fi and Ethernet adapters that exist on this PC.</summary>
    public static List<AdapterChoice> GetChoices()
    {
        var list = new List<AdapterChoice> { new(Automatic, "Automatic (connected Wi-Fi)") };
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(IsRealAdapter)
                     .OrderByDescending(n => n.OperationalStatus == OperationalStatus.Up)
                     .ThenByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                     .ThenBy(n => n.Name))
        {
            string kind = nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "Wi-Fi" : "Wired";
            string state = nic.OperationalStatus == OperationalStatus.Up ? "connected" : "not connected";
            list.Add(new AdapterChoice(nic.Id, $"{nic.Name}  ·  {kind}, {state}  ·  {nic.Description}"));
        }
        return list;
    }

    /// <summary>Hides unplugged adapters and Windows' internal filter/miniport entries.</summary>
    private static bool IsRealAdapter(NetworkInterface n)
    {
        if (n.NetworkInterfaceType is not (NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet))
            return false;
        if (n.OperationalStatus == OperationalStatus.NotPresent) return false;
        string d = n.Description;
        return !(n.Name.Contains("-0000") || d.Contains("Filter", StringComparison.OrdinalIgnoreCase)
                 || d.Contains("Packet Scheduler", StringComparison.OrdinalIgnoreCase) || d.Contains("Miniport", StringComparison.OrdinalIgnoreCase)
                 || d.Contains("Kernel Debug", StringComparison.OrdinalIgnoreCase) || d.Contains("Switch Extension", StringComparison.OrdinalIgnoreCase));
    }

    public static SpeedReading Read(string adapterId)
    {
        var wifi = NativeWifi.Query();

        if (adapterId == Automatic)
        {
            var connected = wifi.FirstOrDefault(w => w.Connection is not null).Connection;
            if (connected is not null) return FromWifi(connected);
            // No Wi-Fi connection: fall back to the fastest wired adapter that is up, so the icon still means something.
            var wired = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet
                            && !n.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase)
                            && !n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();
            return wired is null ? SpeedReading.Disconnected("No network") : FromNic(wired);
        }

        // A specific adapter was chosen.
        if (Guid.TryParse(adapterId, out var guid))
        {
            var match = wifi.FirstOrDefault(w => w.Id == guid);
            if (match.Description is not null)
                return match.Connection is null ? SpeedReading.Disconnected(match.Description) : FromWifi(match.Connection);
        }
        var nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n => n.Id.Equals(adapterId, StringComparison.OrdinalIgnoreCase));
        if (nic is null) return SpeedReading.Disconnected("Adapter not found");
        return nic.OperationalStatus == OperationalStatus.Up ? FromNic(nic) : SpeedReading.Disconnected(nic.Name);
    }

    private static SpeedReading FromWifi(WifiConnection c) =>
        new(true, c.RxMbps, c.TxMbps, c.Description, c.Ssid, c.SignalQuality, c.PhyName, c.LocationBlocked);

    private static SpeedReading FromNic(NetworkInterface n)
    {
        double mbps = n.Speed > 0 ? n.Speed / 1_000_000.0 : 0;
        return new SpeedReading(true, mbps, mbps, n.Name, null, null, "Wired");
    }

    /// <summary>"866", "1.2G" - short enough for a 16px tray icon.</summary>
    public static string ShortSpeed(double mbps) => mbps switch
    {
        <= 0 => "–",
        < 1000 => Math.Round(mbps).ToString("0"),
        _ => (mbps / 1000).ToString(mbps < 10_000 ? "0.0" : "0") + "G"
    };

    public static string LongSpeed(double mbps) =>
        mbps >= 1000 ? $"{mbps / 1000:0.##} Gbps" : $"{Math.Round(mbps):0} Mbps";
}
