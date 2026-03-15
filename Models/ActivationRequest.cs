namespace LicenseManager.API.Models
{
    public class ActivationRequest
    {
        public string LicenseKey { get; set; }

        public string MachineId { get; set; }

        public string Hostname { get; set; }

        public string IpAddress { get; set; }
    }
}
