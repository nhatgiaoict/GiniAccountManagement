using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace GiniAccountManagement
{
    public static class VpnHelper
    {
        public static bool IsVPNConnected()
        {
            if (OperatingSystem.IsWindows())
                return IsVpnConnectedFromIpconfig();

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
                return IsVpnConnectedFromIfconfig();

            return false;
        }
        public static bool IsVpnConnectedFromIpconfig()
        {
            string output = RunCommand("ipconfig /all");
            var blocks = SplitIntoBlocks(output);

            foreach (var block in blocks)
            {
                string lower = block.ToLower();

                if (!(lower.Contains("vpn") || lower.Contains("nord") || lower.Contains("kaspersky") ||
                      lower.Contains("openvpn") || lower.Contains("wireguard") || lower.Contains("tun") || lower.Contains("tap")))
                    continue;

                if (lower.Contains("media state") && lower.Contains("disconnected"))
                    continue;

                bool hasIPv4 = Regex.IsMatch(block, @"IPv4 Address[.\s]*:\s*(\d{1,3}\.){3}\d{1,3}");
                bool hasIPv6 = Regex.IsMatch(block, @"IPv6 Address[.\s]*:\s*([a-f0-9:]+)");

                // 4. Phải có Default Gateway
                bool hasGateway = Regex.IsMatch(block, @"Default Gateway[.\s]*:\s*(\d{1,3}\.){3}\d{1,3}");
                bool isTransparentVpn = lower.Contains("kaspersky");
                if (hasIPv4 || hasIPv6)
                {
                    if (isTransparentVpn)
                        return true;

                    return hasGateway;
                }
            }

            return false;
        }
        public static bool IsVpnConnectedFromIfconfig()
        {
            string ifconfigOutput = RunCommand("ifconfig");
            string routeOutput = OperatingSystem.IsMacOS() ? RunCommand("netstat -rn") : RunCommand("ip route");

            var blocks = SplitIntoBlocks(ifconfigOutput);

            foreach (var block in blocks)
            {
                string lower = block.ToLower();

                // 1. Adapter tên nghi là VPN
                if (!(lower.Contains("tun") || lower.Contains("tap") || lower.Contains("utun") ||
                      lower.Contains("vpn") || lower.Contains("wg") || lower.Contains("nord") || lower.Contains("openvpn")))
                    continue;

                // 2. Có IP (inet hoặc inet6)
                bool hasIPv4 = Regex.IsMatch(block, @"inet\s+(\d{1,3}\.){3}\d{1,3}(?!\.\d)");
                bool hasIPv6 = Regex.IsMatch(block, @"inet6\s+[a-f0-9:]+");

                if (!hasIPv4 && !hasIPv6)
                    continue;

                // 3. Có default route qua interface này?
                string adapterName = GetAdapterNameFromBlock(block);
                if (string.IsNullOrEmpty(adapterName)) continue;

                if (IsDefaultRouteThroughInterface(routeOutput, adapterName))
                    return true;

                // 4. Nếu là adapter dạng transparent (giống kaspersky) → fallback check IP
                if (lower.Contains("kaspersky") || lower.Contains("hotspotshield") || lower.Contains("anchorfree"))
                    return true;
            }

            return false;
        }
        private static string GetAdapterNameFromBlock(string block)
        {
            var match = Regex.Match(block, @"^([a-zA-Z0-9\-\_\.]+):");
            return match.Success ? match.Groups[1].Value : null;
        }
        private static bool IsDefaultRouteThroughInterface(string routeOutput, string iface)
        {
            var lines = routeOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                string lower = line.ToLower();

                // Linux: default via x.x.x.x dev tun0
                if (OperatingSystem.IsLinux() && lower.StartsWith("default") && lower.Contains($"dev {iface.ToLower()}"))
                    return true;

                // macOS: default x.x.x.x UGSc utun2
                if (OperatingSystem.IsMacOS() && lower.StartsWith("default") && lower.EndsWith(iface.ToLower()))
                    return true;
            }

            return false;
        }
        private static List<string> SplitIntoBlocks(string input)
        {
            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var blocks = new List<string>();
            var current = new List<string>();

            foreach (var line in lines)
            {
                bool isNewBlock =
                    Regex.IsMatch(line, @"^\S.*adapter .*:", RegexOptions.IgnoreCase) ||          // Windows
                    line.StartsWith("Unknown adapter", StringComparison.OrdinalIgnoreCase) ||     // Windows special case
                    Regex.IsMatch(line, @"^\w[\w\d\-]*:\s") ||                                     // macOS/Linux ifconfig (e.g., en0:, utun0:, eth0:)
                    Regex.IsMatch(line, @"^\s*[\w\-]+:\s+flags=");                                // BSD-style format (macOS)

                if (isNewBlock)
                {
                    if (current.Count > 0)
                    {
                        blocks.Add(string.Join("\n", current));
                        current.Clear();
                    }
                }

                current.Add(line);
            }

            if (current.Count > 0)
                blocks.Add(string.Join("\n", current));

            return blocks;
        }
        public static string RunCommand(string command)
        {
            string shell;
            string shellArgs;

            if (OperatingSystem.IsWindows())
            {
                shell = "cmd.exe";
                shellArgs = $"/c {command}";
            }
            else // macOS / Linux
            {
                shell = "/bin/bash"; // hoặc /bin/sh
                shellArgs = $"-c \"{command}\"";
            }

            var psi = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = shellArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return string.IsNullOrWhiteSpace(output) ? error : output;
        }
    }
}
