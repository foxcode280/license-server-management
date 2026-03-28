using LicenseManager.API.Models;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LicenseManager.API.Helpers
{
    public class SystemMachineInfoService
    {
        public OfflineActivationMachineInfo GetCurrentMachineInfo()
        {
            var hostName = Environment.MachineName;
            var osDescription = RuntimeInformation.OSDescription?.Trim() ?? "Unknown OS";
            var osType = ResolveOsType();
            var macAddress = ResolveMacAddress();
            var hardwareFingerprint = ComputeHardwareFingerprint(hostName, osType, osDescription, macAddress);
            var machineId = BuildMachineId(hostName, macAddress, hardwareFingerprint);

            return new OfflineActivationMachineInfo
            {
                MachineId = machineId,
                HostName = hostName,
                OsType = osType,
                OsVersion = osDescription,
                MacAddress = macAddress,
                HardwareFingerprint = hardwareFingerprint
            };
        }

        private static string ResolveOsType()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "macOS";
            }

            return "Unknown";
        }

        private static string ResolveMacAddress()
        {
            var rawAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(x =>
                    x.OperationalStatus == OperationalStatus.Up &&
                    x.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    x.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .Select(x => x.GetPhysicalAddress()?.ToString())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            if (string.IsNullOrWhiteSpace(rawAddress))
            {
                return "UNKNOWN";
            }

            return string.Join("-",
                Enumerable.Range(0, rawAddress.Length / 2)
                    .Select(i => rawAddress.Substring(i * 2, 2)))
                .ToUpperInvariant();
        }

        private static string ComputeHardwareFingerprint(
            string hostName,
            string osType,
            string osVersion,
            string macAddress)
        {
            var source = $"{hostName}|{osType}|{osVersion}|{macAddress}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string BuildMachineId(string hostName, string macAddress, string hardwareFingerprint)
        {
            var hostToken = string.IsNullOrWhiteSpace(hostName)
                ? "HOST"
                : new string(hostName
                    .ToUpperInvariant()
                    .Where(char.IsLetterOrDigit)
                    .Take(12)
                    .ToArray());

            if (string.IsNullOrWhiteSpace(hostToken))
            {
                hostToken = "HOST";
            }

            var macToken = macAddress == "UNKNOWN"
                ? hardwareFingerprint[..6].ToUpperInvariant()
                : new string(macAddress.Where(char.IsLetterOrDigit).TakeLast(6).ToArray()).ToUpperInvariant();

            return $"{hostToken}-{macToken}";
        }
    }
}
