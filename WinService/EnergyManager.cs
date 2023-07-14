using LibreHardwareMonitor.Hardware;

namespace WinService;

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
            IsCpuEnabled = true
        };

        _computer.Open();
    }

    // returns sum of all cpu energy
    public float GetCpuEnergy()
    {
        _computer.Accept(new UpdateVisitor());
        return _computer.Hardware.Where(x => x.HardwareType == HardwareType.Cpu).Aggregate<IHardware?, float?>(0f, (current, hardware) => current + hardware?.Sensors.Where(x => x.SensorType == SensorType.Power).Sum(x => x.Value)) ?? 0;
    }

    public void Dispose()
    {
        _computer.Close();
        GC.SuppressFinalize(this);
    }
}

