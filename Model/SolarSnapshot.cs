namespace SolarTray.Model
{
    public sealed class SolarSnapshot
    {
        public double? PvKw { get; set; }
        public double? LoadKw { get; set; }
        public double? SocPercent { get; set; }
        public double? GridKw { get; set; }
        public double? BatteryVolts { get; set; }
    }
}
