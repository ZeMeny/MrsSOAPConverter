namespace SoapConverter
{
    public class Configuration
    {
        public string DeviceIP { get; set; }
        public int DevicePort { get; set; }
        public string DeviceNotificationIP { get; set; }
        public int DeviceNotificationPort { get; set; }
        public string RequestorID { get; set; }
        public string ListenIP { get; set; }
        public int ListenPort { get; set; }
        public bool ValidateMessages { get; set; }
    }
}