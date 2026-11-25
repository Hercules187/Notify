/* Notify - A utility to display the wireless connection speed
    Copyright (C) 2017-2026  Steven Connelly
    GPL-3.0-or-later. See License.txt.
*/
using System.Runtime.InteropServices;
using System.Text;

namespace Notify;

/// <summary>What Windows reports about the current Wi-Fi connection on one adapter.</summary>
public sealed record WifiConnection(
    Guid InterfaceId,
    string Description,
    string? Ssid,           // null when Windows withholds it (location access off)
    int? SignalQuality,     // 0-100, null when withheld
    double RxMbps,          // receive link rate
    double TxMbps,          // transmit link rate
    string PhyName,         // e.g. "Wi-Fi 6 (802.11ax)"
    bool LocationBlocked);  // true = details withheld; rates came from the adapter's link speed instead

/// <summary>
/// Minimal wrapper over the native Wi-Fi API (wlanapi.dll). Gives the real receive/transmit
/// link rates Windows shows in Settings, which WMI's Win32_NetworkAdapter.Speed does not.
/// </summary>
internal static class NativeWifi
{
    private const uint ClientVersion = 2;               // Vista and later
    private const int OpcodeCurrentConnection = 7;      // wlan_intf_opcode_current_connection
    private const int StateConnected = 1;               // wlan_interface_state_connected

    [DllImport("wlanapi.dll")]
    private static extern uint WlanOpenHandle(uint clientVersion, IntPtr reserved, out uint negotiatedVersion, out IntPtr clientHandle);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanCloseHandle(IntPtr clientHandle, IntPtr reserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanEnumInterfaces(IntPtr clientHandle, IntPtr reserved, out IntPtr interfaceList);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanQueryInterface(IntPtr clientHandle, ref Guid interfaceGuid, int opCode, IntPtr reserved,
        out int dataSize, out IntPtr data, IntPtr opcodeValueType);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr memory);

    /// <summary>All Wi-Fi adapters with their current connection (null where not connected).</summary>
    public static IReadOnlyList<(Guid Id, string Description, WifiConnection? Connection)> Query()
    {
        var result = new List<(Guid, string, WifiConnection?)>();
        IntPtr handle = IntPtr.Zero, list = IntPtr.Zero;
        try
        {
            if (WlanOpenHandle(ClientVersion, IntPtr.Zero, out _, out handle) != 0) return result; // no WLAN service
            if (WlanEnumInterfaces(handle, IntPtr.Zero, out list) != 0) return result;

            // WLAN_INTERFACE_INFO_LIST: DWORD count, DWORD index, then WLAN_INTERFACE_INFO[count]
            // WLAN_INTERFACE_INFO: GUID (16) + WCHAR[256] description (512) + state DWORD (4) = 532 bytes
            int count = Marshal.ReadInt32(list, 0);
            for (int i = 0; i < count; i++)
            {
                IntPtr item = list + 8 + i * 532;
                var id = Marshal.PtrToStructure<Guid>(item);
                string description = Marshal.PtrToStringUni(item + 16, 256).TrimEnd('\0');
                int state = Marshal.ReadInt32(item + 528);
                WifiConnection? conn = state == StateConnected
                    ? QueryConnection(handle, id, description) ?? LinkSpeedOnly(id, description)
                    : null;
                result.Add((id, description, conn));
            }
        }
        catch (DllNotFoundException) { /* no Wi-Fi stack (e.g. Server Core) */ }
        finally
        {
            if (list != IntPtr.Zero) WlanFreeMemory(list);
            if (handle != IntPtr.Zero) WlanCloseHandle(handle, IntPtr.Zero);
        }
        return result;
    }

    private static WifiConnection? QueryConnection(IntPtr handle, Guid id, string description)
    {
        if (WlanQueryInterface(handle, ref id, OpcodeCurrentConnection, IntPtr.Zero, out int size, out IntPtr data, IntPtr.Zero) != 0)
            return null;
        try
        {
            if (size < 588) return null;
            // WLAN_CONNECTION_ATTRIBUTES layout (offsets in bytes):
            //   0 isState, 4 connectionMode, 8 WCHAR[256] profileName,
            //   520 WLAN_ASSOCIATION_ATTRIBUTES:
            //     520 DOT11_SSID { ULONG length; UCHAR ssid[32] }, 556 bssType, 560 BSSID[6] (+2 pad),
            //     568 phyType, 572 phyIndex, 576 signalQuality, 580 rxRate (kbps), 584 txRate (kbps)
            int ssidLength = Math.Clamp(Marshal.ReadInt32(data, 520), 0, 32);
            var ssidBytes = new byte[ssidLength];
            Marshal.Copy(data + 524, ssidBytes, 0, ssidLength);
            string ssid = Encoding.UTF8.GetString(ssidBytes);
            int phy = Marshal.ReadInt32(data, 568);
            int quality = Math.Clamp(Marshal.ReadInt32(data, 576), 0, 100);
            uint rx = (uint)Marshal.ReadInt32(data, 580);
            uint tx = (uint)Marshal.ReadInt32(data, 584);
            return new WifiConnection(id, description, ssid, quality, rx / 1000.0, tx / 1000.0, PhyName(phy), false);
        }
        finally
        {
            WlanFreeMemory(data);
        }
    }

    /// <summary>
    /// Windows 11 24H2+ refuses WlanQueryInterface (error 5) unless desktop apps may use location,
    /// because SSID/BSSID can reveal where you are. The adapter's link speed needs no permission.
    /// </summary>
    private static WifiConnection? LinkSpeedOnly(Guid id, string description)
    {
        var nic = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n => Guid.TryParse(n.Id, out var g) && g == id);
        if (nic is null) return null;
        double mbps = nic.Speed > 0 ? nic.Speed / 1_000_000.0 : 0;
        return new WifiConnection(id, description, null, null, mbps, mbps, "Wi-Fi", true);
    }

    // DOT11_PHY_TYPE values
    private static string PhyName(int phy) => phy switch
    {
        4 => "Wi-Fi 2 (802.11a)",
        5 => "Wi-Fi 1 (802.11b)",
        6 => "Wi-Fi 3 (802.11g)",
        7 => "Wi-Fi 4 (802.11n)",
        8 => "Wi-Fi 5 (802.11ac)",
        9 => "WiGig (802.11ad)",
        10 => "Wi-Fi 6 (802.11ax)",
        11 => "Wi-Fi 7 (802.11be)",
        _ => "Wi-Fi"
    };
}
