using LibreHardwareMonitor.Hardware;

namespace WinService.Manager;

public class EnergyManager : IDisposable
{
    private readonly Computer _computer;

    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    public EnergyManager()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true
        };
        
        _computer.Open();
    }

    /// <summary>
    /// Refreshes the values for hardware information
    /// </summary>
    public void UpdateValues()
    {
        _computer.Accept(new UpdateVisitor());
    }

    /// <summary>
    /// Reads the Energy use of the PC
    /// </summary>
    /// <returns>Energy usage in watt</returns>
    public float GetPowerUsage()
    {
        return _computer.Hardware.Sum(hardware => hardware.Sensors.Where(sensor => sensor.SensorType == SensorType.Power).Select(powerSensor => powerSensor.Value.GetValueOrDefault(0)).Sum());
    }

    /// <summary>
    /// Reads all Cpu usages
    /// </summary>
    /// <returns>List of all cpu usages in percent</returns>
    public List<float> GetCpuUsages()
    {
        var cpuList = _computer.Hardware.Where(x => x.HardwareType == HardwareType.Cpu).ToList();

        var cpuUsages = new List<float>();
        foreach (var usageSensors in cpuList.Select(cpu => cpu.Sensors.Where(sensor => sensor.Name.Contains("CPU Total"))))
        {
            cpuUsages.AddRange(usageSensors.Select(usageSensor => usageSensor.Value.GetValueOrDefault(0)));
        }

        return cpuUsages;
    }

    /// <summary>
    /// Reads the Ram usage
    /// </summary>
    /// <returns>Ram usage in percent</returns>
    public float GetRamUsage()
    {
        var ram = _computer.Hardware.FirstOrDefault(x => x.HardwareType == HardwareType.Memory);

        var ramUsageSensor = ram?.Sensors.FirstOrDefault(ramSensor => ramSensor is { SensorType: SensorType.Load, Name: "Memory" });
        if (ramUsageSensor == null)
            return 0;

        return ramUsageSensor.Value.GetValueOrDefault(0);
    }

    /// <summary>
    /// Reads the Disks usages
    /// </summary>
    /// <returns>List of all disks usages in percent</returns>
    public List<float> GetDiskUsages()
    {
        var diskList = _computer.Hardware.Where(x => x.HardwareType == HardwareType.Storage);
        
        var diskUsages = new List<float>();
        foreach (var diskSensors in diskList.Select(diskSensor => diskSensor.Sensors.Where(sensor => sensor is { SensorType: SensorType.Load, Name: "Used Space" })))
        {
            diskUsages.AddRange(diskSensors.Select(usageSensor => usageSensor.Value.GetValueOrDefault(0)));
        }

        return diskUsages;
    }

    /// <summary>
    /// Reads the current ethernet up and download speed
    /// </summary>
    /// <returns>List of all ethernet adapters up and download speed in bps as Dictionary</returns>
    public List<Dictionary<string, float>> GetEthernetUsages()
    {
        var ethernetList = _computer.Hardware.Where(x => x.HardwareType == HardwareType.Network);
        return ethernetList.Select(ethernetSensor => ethernetSensor.Sensors.Where(sensor => sensor.SensorType == SensorType.Throughput)).Select(ethernetSensors => ethernetSensors.ToDictionary(x => x.Name, x => x.Value.GetValueOrDefault(0))).ToList();
    }

    public void Dispose()
    {
        _computer.Close();
        GC.SuppressFinalize(this);
    }
}

