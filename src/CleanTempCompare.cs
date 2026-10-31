using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

[assembly: AssemblyTitle("硬件监测中心")]
[assembly: AssemblyDescription("全传感器监测与电脑清灰前后温度对比工具")]
[assembly: AssemblyCompany("Local")]
[assembly: AssemblyProduct("硬件监测中心")]
[assembly: AssemblyVersion("2.3.0.0")]
[assembly: AssemblyFileVersion("2.3.0.0")]

namespace CleanTempCompare
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                ShowFatalError(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                ShowFatalError(e.ExceptionObject as Exception);
            };

            try
            {
                Application.Run(new AdvancedMainForm());
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
        }

        private static void ShowFatalError(Exception ex)
        {
            string message = ex == null ? "发生未知错误。" : ex.Message;
            MessageBox.Show(
                "程序遇到错误：\n\n" + message +
                "\n\n可以重新启动程序；如果反复出现，请检查是否被安全软件拦截了硬件驱动。",
                "清灰温度对比",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(10, 15, 21);
        public static readonly Color Header = Color.FromArgb(12, 18, 25);
        public static readonly Color Card = Color.FromArgb(18, 26, 36);
        public static readonly Color CardAlt = Color.FromArgb(21, 31, 43);
        public static readonly Color Input = Color.FromArgb(13, 20, 28);
        public static readonly Color Border = Color.FromArgb(39, 55, 71);
        public static readonly Color Grid = Color.FromArgb(35, 49, 64);
        public static readonly Color Text = Color.FromArgb(236, 243, 249);
        public static readonly Color Muted = Color.FromArgb(143, 161, 178);
        public static readonly Color Dim = Color.FromArgb(92, 112, 130);
        public static readonly Color Accent = Color.FromArgb(45, 210, 236);
        public static readonly Color AccentDark = Color.FromArgb(16, 111, 128);
        public static readonly Color Orange = Color.FromArgb(255, 160, 92);
        public static readonly Color Green = Color.FromArgb(66, 211, 146);
        public static readonly Color Red = Color.FromArgb(244, 105, 105);
        public static readonly Color Yellow = Color.FromArgb(241, 197, 90);

        public static Font Font(float size, FontStyle style)
        {
            return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
        }
    }

    internal enum MeasurementStage
    {
        Before,
        After
    }

    internal enum TestMode
    {
        Standard,
        Gaming
    }

    [DataContract]
    internal sealed class SensorStat
    {
        [DataMember] public string Identifier;
        [DataMember] public string Name;
        [DataMember] public string Hardware;
        [DataMember] public string Group;
        [DataMember] public string Type;
        [DataMember] public string Unit;
        [DataMember] public double Min;
        [DataMember] public double Avg;
        [DataMember] public double Max;
        [DataMember] public double StdDev;
        [DataMember] public int Samples;
    }

    [DataContract]
    internal sealed class MetricSummary
    {
        [DataMember] public bool HasCpu;
        [DataMember] public bool HasGpu;
        [DataMember] public bool HasFan;
        [DataMember] public bool HasCpuLoad;
        [DataMember] public double AvgCpu;
        [DataMember] public double MaxCpu;
        [DataMember] public double AvgGpu;
        [DataMember] public double MaxGpu;
        [DataMember] public double AvgCpuLoad;
        [DataMember] public double MaxFan;
        [DataMember] public bool HasGpuLoad;
        [DataMember] public bool HasCpuPower;
        [DataMember] public bool HasGpuPower;
        [DataMember] public double AvgGpuLoad;
        [DataMember] public double MaxGpuLoad;
        [DataMember] public double AvgCpuPower;
        [DataMember] public double MaxCpuPower;
        [DataMember] public double AvgGpuPower;
        [DataMember] public double MaxGpuPower;
        [DataMember] public int Samples;
        [DataMember] public List<SensorStat> SensorStats;
    }

    [DataContract]
    internal sealed class SessionRecord
    {
        [DataMember] public string Version;
        [DataMember] public string Stage;
        [DataMember] public string StageLabel;
        [DataMember] public DateTime CreatedAtUtc;
        [DataMember] public string CpuName;
        [DataMember] public string GpuName;
        [DataMember] public string MotherboardName;
        [DataMember] public string CpuSensorName;
        [DataMember] public string GpuSensorName;
        [DataMember] public string Mode;
        [DataMember] public string WorkloadName;
        [DataMember] public int DurationSeconds;
        [DataMember] public MetricSummary Idle;
        [DataMember] public MetricSummary Load;
        [DataMember] public MetricSummary Recovery;
    }

    internal static class SensorFormat
    {
        public static string Unit(SensorType type)
        {
            switch (type)
            {
                case SensorType.Voltage: return "V";
                case SensorType.Current: return "A";
                case SensorType.Power: return "W";
                case SensorType.Clock: return "MHz";
                case SensorType.Temperature: return "°C";
                case SensorType.Load: return "%";
                case SensorType.Frequency: return "Hz";
                case SensorType.Fan: return "RPM";
                case SensorType.Flow: return "L/h";
                case SensorType.Control: return "%";
                case SensorType.Level: return "%";
                case SensorType.Factor: return "×";
                case SensorType.Throughput: return "B/s";
                case SensorType.SmallData: return "MB";
                case SensorType.Data: return "GB";
                default: return "";
            }
        }

        public static string Kind(SensorType type)
        {
            switch (type)
            {
                case SensorType.Voltage: return "电压";
                case SensorType.Current: return "电流";
                case SensorType.Power: return "功耗";
                case SensorType.Clock: return "频率";
                case SensorType.Temperature: return "温度";
                case SensorType.Load: return "负载";
                case SensorType.Frequency: return "频率";
                case SensorType.Fan: return "风扇";
                case SensorType.Flow: return "流量";
                case SensorType.Control: return "控制";
                case SensorType.Level: return "容量";
                case SensorType.Factor: return "系数";
                case SensorType.Throughput: return "吞吐";
                case SensorType.SmallData: return "数据";
                case SensorType.Data: return "数据";
                default: return type.ToString();
            }
        }

        public static string Format(SensorType type, double value)
        {
            string pattern;
            switch (type)
            {
                case SensorType.Voltage:
                case SensorType.Current:
                    pattern = "0.000";
                    break;
                case SensorType.Clock:
                case SensorType.Frequency:
                    pattern = value >= 10000 ? "0" : "0.0";
                    break;
                case SensorType.Fan:
                    pattern = "0";
                    break;
                case SensorType.Data:
                case SensorType.SmallData:
                case SensorType.Throughput:
                    pattern = "0.0";
                    break;
                default:
                    pattern = "0.0";
                    break;
            }
            string unit = Unit(type);
            return value.ToString(pattern, CultureInfo.InvariantCulture) +
                   (string.IsNullOrEmpty(unit) ? "" : " " + unit);
        }
    }

    internal sealed class LiveSensor
    {
        public string HardwareName;
        public string Group;
        public string Name;
        public string Identifier;
        public string Unit;
        public SensorType Type;
        public double Value;
    }

    internal sealed class SensorSnapshot
    {
        public DateTime Timestamp;
        public string CpuName = "未检测到";
        public string GpuName = "未检测到";
        public string MotherboardName = "未检测到";
        public string CpuSensorName = "CPU 温度";
        public string GpuSensorName = "GPU 温度";
        public bool HasCpu;
        public bool HasGpu;
        public bool HasGpuHotspot;
        public bool HasCpuLoad;
        public bool HasGpuLoad;
        public bool HasCpuPower;
        public bool HasGpuPower;
        public bool HasCpuClock;
        public bool HasGpuClock;
        public bool HasFan;
        public double CpuTemp;
        public double GpuTemp;
        public double GpuHotspot;
        public double CpuLoad;
        public double GpuLoad;
        public double CpuPower;
        public double GpuPower;
        public double CpuClock;
        public double GpuClock;
        public double FanRpm;
        public List<LiveSensor> Rows = new List<LiveSensor>();
    }

    internal sealed class SampleEventArgs : EventArgs
    {
        public SensorSnapshot Snapshot;

        public SampleEventArgs(SensorSnapshot snapshot)
        {
            Snapshot = snapshot;
        }
    }

    internal sealed class HardwareSampler : IDisposable
    {
        private readonly object _sync = new object();
        private Computer _computer;
        private Thread _worker;
        private volatile bool _stopping;
        private bool _started;

        public event EventHandler<SampleEventArgs> Sampled;
        public event EventHandler<string> SamplingError;
        public SensorSnapshot Latest { get; private set; }

        public void Start()
        {
            if (_started)
                return;
            _started = true;
            _worker = new Thread(WorkerLoop);
            _worker.IsBackground = true;
            _worker.Name = "Hardware temperature sampler";
            _worker.Priority = ThreadPriority.AboveNormal;
            _worker.Start();
        }

        private void WorkerLoop()
        {
            try
            {
                _computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsStorageEnabled = true
                };
                _computer.Open();
            }
            catch (Exception ex)
            {
                RaiseError("无法连接硬件传感器：" + FriendlySensorError(ex));
                return;
            }

            bool firstRead = true;
            while (!_stopping)
            {
                DateTime began = DateTime.UtcNow;
                try
                {
                    foreach (IHardware hardware in _computer.Hardware)
                        UpdateHardware(hardware);

                    SensorSnapshot snapshot = ReadSnapshot();
                    lock (_sync)
                        Latest = snapshot;

                    EventHandler<SampleEventArgs> handler = Sampled;
                    if (handler != null)
                        handler(this, new SampleEventArgs(snapshot));
                    firstRead = false;
                }
                catch (Exception ex)
                {
                    if (firstRead)
                    {
                        RaiseError("读取传感器失败：" + FriendlySensorError(ex));
                        firstRead = false;
                    }
                }

                int elapsed = (int)(DateTime.UtcNow - began).TotalMilliseconds;
                int delay = Math.Max(250, 1000 - elapsed);
                for (int i = 0; i < delay / 100 && !_stopping; i++)
                    Thread.Sleep(100);
            }

            try
            {
                _computer.Close();
            }
            catch
            {
            }
        }

        private static void UpdateHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
                UpdateHardware(subHardware);
        }

        private static string FriendlySensorError(Exception ex)
        {
            string message = ex == null ? "未知错误" : ex.Message;
            if (message.IndexOf("access", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("denied", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("driver", StringComparison.OrdinalIgnoreCase) >= 0)
                return "权限不足或硬件驱动被拦截。请允许管理员权限，并检查安全软件。";
            return message;
        }

        private SensorSnapshot ReadSnapshot()
        {
            SensorSnapshot result = new SensorSnapshot();
            result.Timestamp = DateTime.Now;

            List<LiveSensor> all = new List<LiveSensor>();
            List<IHardwareGroup> groups = new List<IHardwareGroup>();

            foreach (IHardware hardware in _computer.Hardware)
            {
                string group = GroupName(hardware.HardwareType);
                List<LiveSensor> groupSensors = new List<LiveSensor>();
                CollectSensors(hardware, group, hardware.Name, groupSensors);
                groups.Add(new IHardwareGroup
                {
                    Type = hardware.HardwareType,
                    Name = hardware.Name,
                    Sensors = groupSensors
                });
                all.AddRange(groupSensors);
            }

            IHardwareGroup cpu = groups.FirstOrDefault(g => g.Type == HardwareType.Cpu);
            IHardwareGroup gpu = PickGpu(groups);
            IHardwareGroup board = groups.FirstOrDefault(g => g.Type == HardwareType.Motherboard);

            if (cpu != null)
            {
                result.CpuName = cpu.Name;
                double temp;
                string sensorName;
                if (PickCpuTemperature(cpu.Sensors, out temp, out sensorName))
                {
                    result.CpuTemp = temp;
                    result.CpuSensorName = sensorName;
                    result.HasCpu = temp > 0;
                }

                double load;
                if (PickCpuLoad(cpu.Sensors, out load))
                {
                    result.CpuLoad = load;
                    result.HasCpuLoad = true;
                }

                double power;
                if (PickSensorValue(cpu.Sensors, SensorType.Power, out power,
                    new[] { "package", "cpu package", "package power", "power" }))
                {
                    result.CpuPower = power;
                    result.HasCpuPower = true;
                }

                double clock;
                if (PickSensorValue(cpu.Sensors, SensorType.Clock, out clock,
                    new[] { "cores (avg)", "core average", "effective clock", "core #1" }))
                {
                    result.CpuClock = clock;
                    result.HasCpuClock = true;
                }
            }

            if (gpu != null)
            {
                result.GpuName = gpu.Name;
                double temp;
                string sensorName;
                if (PickGpuTemperature(gpu.Sensors, out temp, out sensorName))
                {
                    result.GpuTemp = temp;
                    result.GpuSensorName = sensorName;
                    result.HasGpu = temp > 0;
                }

                LiveSensor hotspot = FindByNames(gpu.Sensors, SensorType.Temperature,
                    new[] { "hot spot", "hotspot", "junction", "热点" });
                if (hotspot != null)
                {
                    result.GpuHotspot = hotspot.Value;
                    result.HasGpuHotspot = hotspot.Value > 0;
                }

                double gpuLoad;
                if (PickSensorValue(gpu.Sensors, SensorType.Load, out gpuLoad,
                    new[] { "gpu core", "gpu", "d3d 3d", "core" }))
                {
                    result.GpuLoad = gpuLoad;
                    result.HasGpuLoad = true;
                }
                else
                {
                    LiveSensor bestLoad = gpu.Sensors
                        .Where(s => s.Type == SensorType.Load &&
                                    Normalize(s.Name).IndexOf("memory", StringComparison.Ordinal) < 0)
                        .OrderByDescending(s => s.Value)
                        .FirstOrDefault();
                    if (bestLoad != null)
                    {
                        result.GpuLoad = bestLoad.Value;
                        result.HasGpuLoad = true;
                    }
                }

                double gpuPower;
                if (PickSensorValue(gpu.Sensors, SensorType.Power, out gpuPower,
                    new[] { "gpu package", "gpu power", "board power", "power" }))
                {
                    result.GpuPower = gpuPower;
                    result.HasGpuPower = true;
                }

                double gpuClock;
                if (PickSensorValue(gpu.Sensors, SensorType.Clock, out gpuClock,
                    new[] { "gpu core", "graphics", "core" }))
                {
                    result.GpuClock = gpuClock;
                    result.HasGpuClock = true;
                }
            }

            if (board != null)
                result.MotherboardName = board.Name;

            LiveSensor fan = all
                .Where(s => s.Type == SensorType.Fan && s.Value > 0 && s.Value < 20000)
                .OrderByDescending(s => s.Value)
                .FirstOrDefault();
            if (fan != null)
            {
                result.FanRpm = fan.Value;
                result.HasFan = true;
            }

            result.Rows = all
                .OrderBy(s => GroupRank(s.Group))
                .ThenBy(s => SensorTypeRank(s.Type))
                .ThenBy(s => s.Name)
                .ToList();

            return result;
        }

        private static void CollectSensors(IHardware hardware, string group, string rootName, List<LiveSensor> output)
        {
            foreach (ISensor sensor in hardware.Sensors)
            {
                if (!sensor.Value.HasValue)
                    continue;

                double value = sensor.Value.Value;
                if (double.IsNaN(value) || double.IsInfinity(value))
                    continue;

                string hardwareName = hardware.Name == rootName
                    ? rootName
                    : rootName + " / " + hardware.Name;
                output.Add(new LiveSensor
                {
                    HardwareName = hardwareName,
                    Group = group,
                    Name = sensor.Name,
                    Identifier = hardwareName + "|" + sensor.SensorType + "|" + sensor.Name,
                    Unit = SensorFormat.Unit(sensor.SensorType),
                    Type = sensor.SensorType,
                    Value = value
                });
            }

            foreach (IHardware subHardware in hardware.SubHardware)
                CollectSensors(subHardware, group, rootName, output);
        }

        private static string GroupName(HardwareType type)
        {
            if (type == HardwareType.Cpu) return "CPU";
            if (type == HardwareType.GpuNvidia || type == HardwareType.GpuAmd || type == HardwareType.GpuIntel)
                return "GPU";
            if (type == HardwareType.Motherboard) return "主板";
            if (type == HardwareType.SuperIO) return "主板";
            if (type == HardwareType.Storage) return "存储";
            if (type == HardwareType.Memory) return "内存";
            if (type == HardwareType.Cooler) return "散热器";
            if (type == HardwareType.Psu) return "电源";
            return "其他";
        }

        private static int GroupRank(string group)
        {
            if (group == "CPU") return 0;
            if (group == "GPU") return 1;
            if (group == "主板") return 2;
            if (group == "散热器") return 3;
            if (group == "存储") return 4;
            if (group == "内存") return 5;
            if (group == "电源") return 6;
            return 7;
        }

        private static int SensorTypeRank(SensorType type)
        {
            if (type == SensorType.Temperature) return 0;
            if (type == SensorType.Fan) return 1;
            if (type == SensorType.Load) return 2;
            if (type == SensorType.Power) return 3;
            if (type == SensorType.Clock) return 4;
            if (type == SensorType.Voltage) return 5;
            if (type == SensorType.Current) return 6;
            return 10;
        }

        private static IHardwareGroup PickGpu(List<IHardwareGroup> groups)
        {
            List<IHardwareGroup> gpus = groups
                .Where(g => g.Type == HardwareType.GpuNvidia || g.Type == HardwareType.GpuAmd ||
                            g.Type == HardwareType.GpuIntel)
                .ToList();
            if (gpus.Count == 0)
                return null;

            return gpus
                .OrderBy(g => g.Type == HardwareType.GpuIntel ? 1 : 0)
                .ThenByDescending(g => g.Sensors.Count(s => s.Type == SensorType.Temperature))
                .First();
        }

        private static bool PickCpuTemperature(List<LiveSensor> sensors, out double value, out string name)
        {
            string[] preferred =
            {
                "cpu package", "package", "tctl/tdie", "cpu (tctl/tdie)",
                "core max", "core average", "cpu", "temperature"
            };

            for (int p = 0; p < preferred.Length; p++)
            {
                LiveSensor found = sensors
                    .Where(s => s.Type == SensorType.Temperature)
                    .Where(s => Normalize(s.Name).Contains(preferred[p]))
                    .OrderByDescending(s => s.Value)
                    .FirstOrDefault();
                if (found != null)
                {
                    value = found.Value;
                    name = found.Name;
                    return true;
                }
            }

            List<LiveSensor> cores = sensors
                .Where(s => s.Type == SensorType.Temperature &&
                            Normalize(s.Name).Contains("core"))
                .ToList();
            if (cores.Count > 0)
            {
                LiveSensor hottest = cores.OrderByDescending(s => s.Value).First();
                value = hottest.Value;
                name = "最高核心";
                return true;
            }

            LiveSensor any = sensors
                .Where(s => s.Type == SensorType.Temperature)
                .OrderByDescending(s => s.Value)
                .FirstOrDefault();
            value = any == null ? 0 : any.Value;
            name = any == null ? "CPU 温度" : any.Name;
            return any != null;
        }

        private static bool PickGpuTemperature(List<LiveSensor> sensors, out double value, out string name)
        {
            LiveSensor found = FindByNames(sensors, SensorType.Temperature,
                new[] { "gpu core", "gpu temperature", "core temperature", "edge temperature", "gpu" });
            if (found == null)
            {
                found = sensors
                    .Where(s => s.Type == SensorType.Temperature &&
                                Normalize(s.Name).IndexOf("hot", StringComparison.Ordinal) < 0 &&
                                Normalize(s.Name).IndexOf("memory", StringComparison.Ordinal) < 0)
                    .OrderByDescending(s => s.Value)
                    .FirstOrDefault();
            }
            value = found == null ? 0 : found.Value;
            name = found == null ? "GPU 温度" : found.Name;
            return found != null;
        }

        private static bool PickCpuLoad(List<LiveSensor> sensors, out double value)
        {
            LiveSensor total = FindByNames(sensors, SensorType.Load,
                new[] { "cpu total", "total", "cpu", "package" });
            if (total != null)
            {
                value = total.Value;
                return true;
            }

            List<LiveSensor> coreLoads = sensors
                .Where(s => s.Type == SensorType.Load &&
                            Normalize(s.Name).Contains("core") &&
                            Normalize(s.Name).IndexOf("max", StringComparison.Ordinal) < 0)
                .ToList();
            if (coreLoads.Count == 0)
            {
                value = 0;
                return false;
            }
            value = coreLoads.Average(s => s.Value);
            return true;
        }

        private static bool PickSensorValue(
            List<LiveSensor> sensors,
            SensorType type,
            out double value,
            string[] preferredNames)
        {
            LiveSensor found = FindByNames(sensors, type, preferredNames);
            value = found == null ? 0 : found.Value;
            return found != null;
        }

        private static LiveSensor FindByNames(List<LiveSensor> sensors, SensorType type, string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string expected = names[i];
                LiveSensor found = sensors.FirstOrDefault(s =>
                    s.Type == type && Normalize(s.Name).Contains(expected));
                if (found != null)
                    return found;
            }
            return null;
        }

        private static string Normalize(string text)
        {
            return (text ?? string.Empty).Trim().ToLowerInvariant().Replace("_", " ");
        }

        public void Dispose()
        {
            _stopping = true;
            if (_worker != null && _worker.IsAlive)
                _worker.Join(2500);
            _computer = null;
        }

        private void RaiseError(string message)
        {
            EventHandler<string> handler = SamplingError;
            if (handler != null)
                handler(this, message);
        }

        private sealed class IHardwareGroup
        {
            public HardwareType Type;
            public string Name;
            public List<LiveSensor> Sensors;
        }
    }

    internal sealed class MetricAccumulator
    {
        private int _cpuSamples;
        private int _gpuSamples;
        private int _fanSamples;
        private int _loadSamples;
        private double _cpuSum;
        private double _gpuSum;
        private double _fanSum;
        private double _loadSum;
        private double _cpuMax;
        private double _gpuMax;
        private double _fanMax;
        private int _gpuLoadSamples;
        private int _cpuPowerSamples;
        private int _gpuPowerSamples;
        private double _gpuLoadSum;
        private double _gpuLoadMax;
        private double _cpuPowerSum;
        private double _cpuPowerMax;
        private double _gpuPowerSum;
        private double _gpuPowerMax;
        private readonly Dictionary<string, RunningSensor> _sensorStats =
            new Dictionary<string, RunningSensor>();

        public int Samples { get; private set; }

        public void Add(SensorSnapshot snapshot)
        {
            Samples++;
            if (snapshot.HasCpu)
            {
                _cpuSamples++;
                _cpuSum += snapshot.CpuTemp;
                if (snapshot.CpuTemp > _cpuMax) _cpuMax = snapshot.CpuTemp;
            }
            if (snapshot.HasGpu)
            {
                _gpuSamples++;
                _gpuSum += snapshot.GpuTemp;
                if (snapshot.GpuTemp > _gpuMax) _gpuMax = snapshot.GpuTemp;
            }
            if (snapshot.HasCpuLoad)
            {
                _loadSamples++;
                _loadSum += snapshot.CpuLoad;
            }
            if (snapshot.HasFan)
            {
                _fanSamples++;
                _fanSum += snapshot.FanRpm;
                if (snapshot.FanRpm > _fanMax) _fanMax = snapshot.FanRpm;
            }
            if (snapshot.HasGpuLoad)
            {
                _gpuLoadSamples++;
                _gpuLoadSum += snapshot.GpuLoad;
                if (snapshot.GpuLoad > _gpuLoadMax) _gpuLoadMax = snapshot.GpuLoad;
            }
            if (snapshot.HasCpuPower)
            {
                _cpuPowerSamples++;
                _cpuPowerSum += snapshot.CpuPower;
                if (snapshot.CpuPower > _cpuPowerMax) _cpuPowerMax = snapshot.CpuPower;
            }
            if (snapshot.HasGpuPower)
            {
                _gpuPowerSamples++;
                _gpuPowerSum += snapshot.GpuPower;
                if (snapshot.GpuPower > _gpuPowerMax) _gpuPowerMax = snapshot.GpuPower;
            }

            if (snapshot.Rows != null)
            {
                foreach (LiveSensor sensor in snapshot.Rows)
                {
                    if (string.IsNullOrEmpty(sensor.Identifier))
                        continue;
                    RunningSensor running;
                    if (!_sensorStats.TryGetValue(sensor.Identifier, out running))
                    {
                        running = new RunningSensor
                        {
                            Last = sensor,
                            Min = sensor.Value,
                            Max = sensor.Value
                        };
                        _sensorStats[sensor.Identifier] = running;
                    }
                    running.Samples++;
                    running.Sum += sensor.Value;
                    running.SumSquares += sensor.Value * sensor.Value;
                    if (sensor.Value < running.Min) running.Min = sensor.Value;
                    if (sensor.Value > running.Max) running.Max = sensor.Value;
                    running.Last = sensor;
                }
            }
        }

        public MetricSummary Build()
        {
            List<SensorStat> stats = new List<SensorStat>();
            foreach (RunningSensor running in _sensorStats.Values)
            {
                stats.Add(new SensorStat
                {
                    Identifier = running.Last.Identifier,
                    Name = running.Last.Name,
                    Hardware = running.Last.HardwareName,
                    Group = running.Last.Group,
                    Type = SensorFormat.Kind(running.Last.Type),
                    Unit = running.Last.Unit,
                    Min = running.Min,
                    Avg = running.Sum / Math.Max(1, running.Samples),
                    Max = running.Max,
                    StdDev = StandardDeviation(running.Sum, running.SumSquares, running.Samples),
                    Samples = running.Samples
                });
            }
            stats.Sort(delegate(SensorStat a, SensorStat b)
            {
                int group = string.Compare(a.Group, b.Group, StringComparison.Ordinal);
                if (group != 0) return group;
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            return new MetricSummary
            {
                Samples = Samples,
                HasCpu = _cpuSamples > 0,
                HasGpu = _gpuSamples > 0,
                HasFan = _fanSamples > 0,
                HasCpuLoad = _loadSamples > 0,
                AvgCpu = _cpuSamples == 0 ? 0 : _cpuSum / _cpuSamples,
                MaxCpu = _cpuMax,
                AvgGpu = _gpuSamples == 0 ? 0 : _gpuSum / _gpuSamples,
                MaxGpu = _gpuMax,
                AvgCpuLoad = _loadSamples == 0 ? 0 : _loadSum / _loadSamples,
                MaxFan = _fanMax,
                HasGpuLoad = _gpuLoadSamples > 0,
                AvgGpuLoad = _gpuLoadSamples == 0 ? 0 : _gpuLoadSum / _gpuLoadSamples,
                MaxGpuLoad = _gpuLoadMax,
                HasCpuPower = _cpuPowerSamples > 0,
                AvgCpuPower = _cpuPowerSamples == 0 ? 0 : _cpuPowerSum / _cpuPowerSamples,
                MaxCpuPower = _cpuPowerMax,
                HasGpuPower = _gpuPowerSamples > 0,
                AvgGpuPower = _gpuPowerSamples == 0 ? 0 : _gpuPowerSum / _gpuPowerSamples,
                MaxGpuPower = _gpuPowerMax,
                SensorStats = stats
            };
        }

        private sealed class RunningSensor
        {
            public LiveSensor Last;
            public double Min;
            public double Max;
            public double Sum;
            public double SumSquares;
            public int Samples;
        }

        private static double StandardDeviation(double sum, double sumSquares, int count)
        {
            if (count <= 1)
                return 0;
            double mean = sum / count;
            return Math.Sqrt(Math.Max(0, sumSquares / count - mean * mean));
        }
    }

    internal sealed class CpuLoadGenerator : IDisposable
    {
        private volatile bool _running;
        private Thread[] _threads;

        public void Start()
        {
            if (_running)
                return;
            _running = true;
            int count = Math.Max(1, Environment.ProcessorCount);
            _threads = new Thread[count];
            for (int i = 0; i < count; i++)
            {
                Thread thread = new Thread(LoadLoop);
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.Lowest;
                thread.Name = "CPU load " + (i + 1);
                _threads[i] = thread;
                thread.Start();
            }
        }

        private void LoadLoop()
        {
            double value = 1.000001;
            while (_running)
            {
                for (int i = 0; i < 20000; i++)
                {
                    value = Math.Sqrt(value * value + 1.0) - value;
                    if (value < 0.000001 || value > 1000000)
                        value = 1.000001;
                }
                Thread.SpinWait(300);
            }
        }

        public void Stop()
        {
            if (!_running)
                return;
            _running = false;
            if (_threads != null)
            {
                foreach (Thread thread in _threads)
                {
                    try
                    {
                        if (thread.IsAlive)
                            thread.Join(800);
                    }
                    catch
                    {
                    }
                }
            }
            _threads = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }

    internal static class SessionStore
    {
        public static string ReportsDirectory
        {
            get
            {
                string preferred = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                try
                {
                    Directory.CreateDirectory(preferred);
                    return preferred;
                }
                catch
                {
                    string fallback = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CleanTempCompare", "Reports");
                    Directory.CreateDirectory(fallback);
                    return fallback;
                }
            }
        }

        public static string StageKey(MeasurementStage stage)
        {
            return stage == MeasurementStage.Before ? "pre" : "post";
        }

        public static string StageLabel(MeasurementStage stage)
        {
            return stage == MeasurementStage.Before ? "清灰前" : "清灰后";
        }

        public static string ModeKey(TestMode mode)
        {
            return mode == TestMode.Gaming ? "gaming" : "idle";
        }

        public static string ModeLabel(TestMode mode)
        {
            return mode == TestMode.Gaming ? "游戏负载" : "无负载";
        }

        public static void Save(SessionRecord session, MeasurementStage stage)
        {
            Save(session, stage, TestMode.Standard);
        }

        public static void Save(SessionRecord session, MeasurementStage stage, TestMode mode)
        {
            string key = StageKey(stage);
            string modeKey = ModeKey(mode);
            string latest = Path.Combine(ReportsDirectory, key + "_" + modeKey + "_latest.json");
            string archive = Path.Combine(
                ReportsDirectory,
                key + "_" + modeKey + "_" +
                session.CreatedAtUtc.ToLocalTime().ToString("yyyyMMdd_HHmmss") + ".json");
            WriteJson(session, archive);
            WriteJson(session, latest);
        }

        private static void WriteJson(SessionRecord session, string path)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SessionRecord));
            using (FileStream stream = File.Create(path))
            {
                serializer.WriteObject(stream, session);
            }
        }

        public static SessionRecord Load(MeasurementStage stage)
        {
            return Load(stage, TestMode.Standard);
        }

        public static SessionRecord Load(MeasurementStage stage, TestMode mode)
        {
            string path = Path.Combine(
                ReportsDirectory,
                StageKey(stage) + "_" + ModeKey(mode) + "_latest.json");
            if (!File.Exists(path) && mode == TestMode.Standard)
            {
                // Read v2 standard records, then v1 legacy records.
                path = Path.Combine(ReportsDirectory, StageKey(stage) + "_standard_latest.json");
                if (!File.Exists(path))
                    path = Path.Combine(ReportsDirectory, StageKey(stage) + "_latest.json");
            }
            if (!File.Exists(path))
                return null;
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SessionRecord));
                using (FileStream stream = File.OpenRead(path))
                    return (SessionRecord)serializer.ReadObject(stream);
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class DrawingHelpers
    {
        public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            GraphicsPath path = new GraphicsPath();
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawRoundedFill(Graphics graphics, Rectangle bounds, int radius, Color fill)
        {
            using (GraphicsPath path = RoundedRectangle(bounds, radius))
            using (SolidBrush brush = new SolidBrush(fill))
                graphics.FillPath(brush, path);
        }
    }

    internal class CardPanel : Panel
    {
        public Color CardColor = Theme.Card;
        public Color BorderColor = Theme.Border;
        public int CornerRadius = 10;

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
            Padding = new Padding(16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = DrawingHelpers.RoundedRectangle(bounds, CornerRadius))
            using (SolidBrush fill = new SolidBrush(CardColor))
            using (Pen border = new Pen(BorderColor))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
            base.OnPaint(e);
        }
    }

    internal sealed class AccentButton : Button
    {
        public Color NormalColor = Theme.Accent;
        public Color HoverColor = Color.FromArgb(80, 226, 247);
        public Color DisabledColor = Color.FromArgb(50, 67, 80);
        private bool _hover;

        public AccentButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.FromArgb(8, 18, 24);
            Font = Theme.Font(10.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Height = 46;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            Color color = !Enabled ? DisabledColor : (_hover ? HoverColor : NormalColor);
            using (GraphicsPath path = DrawingHelpers.RoundedRectangle(bounds, 8))
            using (SolidBrush brush = new SolidBrush(color))
                pevent.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(
                pevent.Graphics,
                Text,
                Font,
                bounds,
                Enabled ? ForeColor : Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }
    }

    internal sealed class MetricCard : CardPanel
    {
        private readonly Label _title;
        private readonly Label _value;
        private readonly Label _detail;

        public MetricCard(string title, string value, string detail, Color accent)
        {
            _title = new Label
            {
                Text = title,
                ForeColor = Theme.Muted,
                Font = Theme.Font(9.5f, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(18, 14),
                BackColor = Color.Transparent
            };
            _value = new Label
            {
                Text = value,
                ForeColor = accent,
                Font = Theme.Font(29f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(14, 34),
                BackColor = Color.Transparent
            };
            _detail = new Label
            {
                Text = detail,
                ForeColor = Theme.Dim,
                Font = Theme.Font(8.5f, FontStyle.Regular),
                AutoEllipsis = true,
                Location = new Point(18, Height - 31),
                Height = 22,
                Width = Width - 36,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.Transparent
            };
            Controls.Add(_title);
            Controls.Add(_value);
            Controls.Add(_detail);
        }

        public void Set(string value, string detail)
        {
            _value.Text = value;
            _detail.Text = detail;
        }
    }

    internal sealed class LiveChart : Control
    {
        private readonly List<ChartPoint> _points = new List<ChartPoint>();

        public LiveChart()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Padding = new Padding(18);
        }

        public void Add(SensorSnapshot snapshot)
        {
            _points.Add(new ChartPoint
            {
                Time = snapshot.Timestamp,
                Cpu = snapshot.HasCpu ? snapshot.CpuTemp : double.NaN,
                Gpu = snapshot.HasGpu ? snapshot.GpuTemp : double.NaN
            });
            while (_points.Count > 180)
                _points.RemoveAt(0);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath background = DrawingHelpers.RoundedRectangle(bounds, 10))
            using (SolidBrush brush = new SolidBrush(Theme.Card))
                e.Graphics.FillPath(brush, background);

            using (Font titleFont = Theme.Font(10f, FontStyle.Bold))
            using (Font smallFont = Theme.Font(8f, FontStyle.Regular))
            using (SolidBrush white = new SolidBrush(Theme.Text))
            using (SolidBrush muted = new SolidBrush(Theme.Muted))
            using (Pen gridPen = new Pen(Theme.Grid, 1f))
            {
                gridPen.DashStyle = DashStyle.Dot;
                e.Graphics.DrawString("最近 3 分钟温度走势", titleFont, white, 18, 14);
                DrawLegend(e.Graphics, smallFont, 172, 17, Theme.Accent, "CPU");
                DrawLegend(e.Graphics, smallFont, 224, 17, Theme.Orange, "GPU");

                Rectangle plot = new Rectangle(48, 52, Math.Max(100, Width - 68), Height - 80);
                int minTemp = 20;
                int maxTemp = 100;
                for (int t = minTemp; t <= maxTemp; t += 20)
                {
                    int y = plot.Bottom - (t - minTemp) * plot.Height / (maxTemp - minTemp);
                    e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    e.Graphics.DrawString(t + "°", smallFont, muted, 12, y - 7);
                }

                int divisions = 3;
                for (int i = 0; i <= divisions; i++)
                {
                    int x = plot.Left + plot.Width * i / divisions;
                    e.Graphics.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                }

                if (_points.Count == 0)
                {
                    using (StringFormat centered = new StringFormat())
                    {
                        centered.Alignment = StringAlignment.Center;
                        centered.LineAlignment = StringAlignment.Center;
                        e.Graphics.DrawString("正在等待传感器数据…", smallFont, muted, plot, centered);
                    }
                    return;
                }

                DrawSeries(e.Graphics, plot, smallFont, minTemp, maxTemp, true);
                DrawSeries(e.Graphics, plot, smallFont, minTemp, maxTemp, false);
                e.Graphics.DrawString("3 分钟前", smallFont, muted, plot.Left, plot.Bottom + 8);
                string nowText = "现在";
                SizeF size = e.Graphics.MeasureString(nowText, smallFont);
                e.Graphics.DrawString(nowText, smallFont, muted, plot.Right - size.Width, plot.Bottom + 8);
            }
            base.OnPaint(e);
        }

        private void DrawSeries(Graphics graphics, Rectangle plot, Font font, int minTemp, int maxTemp, bool cpu)
        {
            List<PointF> visible = new List<PointF>();
            int count = Math.Max(1, _points.Count);
            for (int i = 0; i < _points.Count; i++)
            {
                double temperature = cpu ? _points[i].Cpu : _points[i].Gpu;
                if (double.IsNaN(temperature))
                    continue;
                double clamped = Math.Max(minTemp, Math.Min(maxTemp, temperature));
                float x = plot.Right - plot.Width * (count - 1 - i) / 179f;
                if (x < plot.Left)
                    continue;
                float y = plot.Bottom - (float)((clamped - minTemp) * plot.Height / (maxTemp - minTemp));
                visible.Add(new PointF(x, y));
            }
            if (visible.Count < 2)
                return;

            using (Pen line = new Pen(cpu ? Theme.Accent : Theme.Orange, 2.2f))
            {
                line.LineJoin = LineJoin.Round;
                graphics.DrawLines(line, visible.ToArray());
            }

            if (cpu)
            {
                PointF last = visible[visible.Count - 1];
                using (SolidBrush dot = new SolidBrush(Theme.Accent))
                    graphics.FillEllipse(dot, last.X - 3.5f, last.Y - 3.5f, 7, 7);
            }
        }

        private static void DrawLegend(Graphics graphics, Font font, int x, int y, Color color, string text)
        {
            using (SolidBrush brush = new SolidBrush(color))
            using (SolidBrush textBrush = new SolidBrush(Theme.Muted))
            {
                graphics.FillEllipse(brush, x, y + 5, 6, 6);
                graphics.DrawString(text, font, textBrush, x + 10, y);
            }
        }

        private sealed class ChartPoint
        {
            public DateTime Time;
            public double Cpu;
            public double Gpu;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly HardwareSampler _sampler = new HardwareSampler();
        private readonly CpuLoadGenerator _loadGenerator = new CpuLoadGenerator();
        private readonly System.Windows.Forms.Timer _testTimer = new System.Windows.Forms.Timer();
        private readonly LiveChart _chart = new LiveChart();
        private readonly MetricCard _cpuCard;
        private readonly MetricCard _gpuCard;
        private readonly MetricCard _loadCard;
        private readonly DataGridView _sensorGrid;
        private readonly Label _statusLabel;
        private readonly Label _cpuInfoLabel;
        private readonly Label _gpuInfoLabel;
        private readonly Label _testHintLabel;
        private readonly Label _phaseLabel;
        private readonly Label _timeLabel;
        private readonly Label _beforeHistoryLabel;
        private readonly Label _afterHistoryLabel;
        private readonly Label _compareHeadline;
        private readonly Label _compareExplanation;
        private readonly CompareRowLabels _cpuAvgCompare;
        private readonly CompareRowLabels _cpuPeakCompare;
        private readonly CompareRowLabels _cpuIdleCompare;
        private readonly CompareRowLabels _gpuCompare;
        private readonly Panel _progressTrack;
        private readonly Panel _progressFill;
        private readonly RadioButton _beforeRadio;
        private readonly RadioButton _afterRadio;
        private readonly AccentButton _startButton;
        private readonly AccentButton _exportButton;
        private readonly ToolTip _toolTip = new ToolTip();
        private readonly int[] _phaseSeconds = { 60, 240, 60 };
        private readonly string[] _phaseNames = { "静置采样", "CPU 负载测试", "恢复采样" };

        private SensorSnapshot _latest;
        private SessionRecord _beforeSession;
        private SessionRecord _afterSession;
        private bool _testActive;
        private int _phaseIndex;
        private DateTime _testStarted;
        private DateTime _phaseEnds;
        private MeasurementStage _testStage;
        private MetricAccumulator _idleAccumulator;
        private MetricAccumulator _loadAccumulator;
        private MetricAccumulator _recoveryAccumulator;
        private StreamWriter _rawCsv;
        private string _rawCsvPath;
        private bool _sensorReady;
        private bool _closingAfterTestPrompt;
        private int _sensorGridTick;

        public MainForm()
        {
            Text = "清灰温度对比";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Font(9f, FontStyle.Regular);
            ClientSize = new Size(1216, 790);
            MinimumSize = new Size(1080, 730);
            DoubleBuffered = true;
            FormClosing += MainForm_FormClosing;

            TableLayoutPanel page = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.Background,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(page);

            Panel header = BuildHeader();
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0);
            page.Controls.Add(header, 0, 0);

            TableLayoutPanel main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(22, 14, 22, 20),
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            page.Controls.Add(main, 0, 1);

            TableLayoutPanel left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 0, 8, 0),
                BackColor = Theme.Background
            };
            left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 116f));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 282f));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            main.Controls.Add(left, 0, 0);

            TableLayoutPanel metricRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Theme.Background
            };
            metricRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            metricRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            metricRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f));
            left.Controls.Add(metricRow, 0, 0);

            _cpuCard = new MetricCard("CPU 温度", "--", "等待传感器数据", Theme.Accent)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
            _gpuCard = new MetricCard("GPU 温度", "--", "等待传感器数据", Theme.Orange)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0)
            };
            _loadCard = new MetricCard("CPU 负载", "--", "风扇转速 -- RPM", Theme.Green)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0)
            };
            metricRow.Controls.Add(_cpuCard, 0, 0);
            metricRow.Controls.Add(_gpuCard, 1, 0);
            metricRow.Controls.Add(_loadCard, 2, 0);

            _chart.Dock = DockStyle.Fill;
            _chart.Margin = new Padding(0, 0, 0, 10);
            left.Controls.Add(_chart, 0, 1);

            CardPanel sensorCard = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(14)
            };
            left.Controls.Add(sensorCard, 0, 2);

            TableLayoutPanel sensorLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            sensorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            sensorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            sensorCard.Controls.Add(sensorLayout);

            Panel sensorHeader = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label sensorTitle = MakeLabel("实时传感器", 10.5f, FontStyle.Bold, Theme.Text);
            sensorTitle.Location = new Point(2, 3);
            Label sensorNote = MakeLabel("温度与风扇转速 · 每秒刷新", 8.2f, FontStyle.Regular, Theme.Dim);
            sensorNote.AutoSize = false;
            sensorNote.TextAlign = ContentAlignment.MiddleRight;
            sensorNote.Dock = DockStyle.Right;
            sensorNote.Width = 210;
            sensorNote.Padding = new Padding(0, 0, 2, 0);
            sensorHeader.Controls.Add(sensorTitle);
            sensorHeader.Controls.Add(sensorNote);
            sensorLayout.Controls.Add(sensorHeader, 0, 0);

            _sensorGrid = BuildSensorGrid();
            sensorLayout.Controls.Add(_sensorGrid, 0, 1);

            TableLayoutPanel right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(8, 0, 0, 0),
                BackColor = Theme.Background
            };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 390f));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            main.Controls.Add(right, 1, 0);

            CardPanel testCard = BuildTestCard(out _testHintLabel, out _phaseLabel, out _timeLabel,
                out _beforeHistoryLabel, out _afterHistoryLabel, out _progressTrack, out _progressFill,
                out _beforeRadio, out _afterRadio, out _startButton);
            testCard.Dock = DockStyle.Fill;
            testCard.Margin = new Padding(0, 0, 0, 10);
            right.Controls.Add(testCard, 0, 0);

            CardPanel compareCard = BuildCompareCard(out _compareHeadline, out _compareExplanation,
                out _cpuAvgCompare, out _cpuPeakCompare, out _cpuIdleCompare, out _gpuCompare);
            compareCard.Dock = DockStyle.Fill;
            compareCard.Margin = new Padding(0, 0, 0, 10);
            right.Controls.Add(compareCard, 0, 1);

            TableLayoutPanel actionRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = Theme.Background
            };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            right.Controls.Add(actionRow, 0, 2);

            _exportButton = new AccentButton
            {
                Text = "导出对比报告",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 5, 0),
                Enabled = false
            };
            _exportButton.Click += delegate { ExportReport(); };
            AccentButton folderButton = new AccentButton
            {
                Text = "记录文件夹",
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 0, 0),
                NormalColor = Theme.CardAlt,
                HoverColor = Color.FromArgb(35, 48, 62),
                ForeColor = Theme.Text
            };
            folderButton.Click += delegate { OpenReportsFolder(); };
            actionRow.Controls.Add(_exportButton, 0, 0);
            actionRow.Controls.Add(folderButton, 1, 0);

            _cpuInfoLabel = MakeLabel("CPU：正在识别…", 8f, FontStyle.Regular, Theme.Dim);
            _gpuInfoLabel = MakeLabel("GPU：正在识别…", 8f, FontStyle.Regular, Theme.Dim);
            _statusLabel = MakeLabel("正在连接硬件传感器…", 8.5f, FontStyle.Regular, Theme.Muted);
            _statusLabel.AutoSize = false;
            _statusLabel.TextAlign = ContentAlignment.MiddleRight;
            _statusLabel.Dock = DockStyle.Right;
            _statusLabel.Width = 310;
            _statusLabel.Padding = new Padding(0, 0, 2, 0);
            header.Controls.Add(_statusLabel);

            Panel infoPanel = new Panel
            {
                BackColor = Color.Transparent,
                Location = new Point(286, 45),
                Size = new Size(430, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _cpuInfoLabel.Dock = DockStyle.Left;
            _cpuInfoLabel.AutoSize = false;
            _cpuInfoLabel.Width = 215;
            _gpuInfoLabel.Dock = DockStyle.Left;
            _gpuInfoLabel.AutoSize = false;
            _gpuInfoLabel.Width = 215;
            infoPanel.Controls.Add(_gpuInfoLabel);
            infoPanel.Controls.Add(_cpuInfoLabel);
            header.Controls.Add(infoPanel);

            _testTimer.Interval = 250;
            _testTimer.Tick += TestTimer_Tick;
            _sampler.Sampled += Sampler_Sampled;
            _sampler.SamplingError += Sampler_SamplingError;
            Shown += MainForm_Shown;
            FormClosed += delegate { _sampler.Dispose(); };

            ApplyStageSelection();
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 86,
                BackColor = Theme.Header,
                Padding = new Padding(22, 12, 22, 8)
            };

            Label mark = new Label
            {
                Text = "°",
                Font = Theme.Font(25f, FontStyle.Bold),
                ForeColor = Theme.Background,
                BackColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(22, 19),
                Size = new Size(46, 46)
            };
            header.Controls.Add(mark);

            Label title = MakeLabel("清灰温度对比", 18f, FontStyle.Bold, Theme.Text);
            title.Location = new Point(81, 13);
            header.Controls.Add(title);

            Label subtitle = MakeLabel("标准化清灰前后散热对比", 9f,
                FontStyle.Regular, Theme.Muted);
            subtitle.Location = new Point(83, 49);
            header.Controls.Add(subtitle);

            Panel divider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Theme.Border
            };
            header.Controls.Add(divider);
            return header;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            LoadSavedSessions();
            if (IsAdministrator() && !IsPawnIoInstalled())
                OfferPawnIoInstallation();
            _sampler.Start();
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPawnIoInstalled()
        {
            try
            {
                Type pawnIoType = typeof(Computer).Assembly.GetType(
                    "LibreHardwareMonitor.PawnIo.PawnIo", false);
                if (pawnIoType == null)
                    return true;
                PropertyInfo installed = pawnIoType.GetProperty(
                    "IsInstalled", BindingFlags.Public | BindingFlags.Static);
                if (installed == null || installed.PropertyType != typeof(bool))
                    return true;
                return (bool)installed.GetValue(null, null);
            }
            catch
            {
                return true;
            }
        }

        private void OfferPawnIoInstallation()
        {
            string installer = Path.Combine(Application.StartupPath, "PawnIO_setup.exe");
            string detail = File.Exists(installer)
                ? "CPU 温度需要 LibreHardwareMonitor 官方使用的 PawnIO 底层驱动。\n\n" +
                  "是否现在打开安装程序？安装完成后回到本窗口即可开始监测。"
                : "未找到 PawnIO 安装器。程序可能只能读取 GPU 温度。";
            DialogResult answer = MessageBox.Show(
                detail,
                "首次使用：安装温度驱动",
                File.Exists(installer) ? MessageBoxButtons.YesNo : MessageBoxButtons.OK,
                File.Exists(installer) ? MessageBoxIcon.Question : MessageBoxIcon.Warning);
            if (!File.Exists(installer) || answer != DialogResult.Yes)
                return;

            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = installer,
                        WorkingDirectory = Application.StartupPath,
                        UseShellExecute = true
                    }))
                {
                    if (process != null)
                        process.WaitForExit();
                }
                if (!IsPawnIoInstalled())
                {
                    MessageBox.Show(
                        "驱动尚未安装完成。请按安装程序提示操作后重新启动本工具。",
                        "需要重启工具",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "无法启动驱动安装程序：\n" + ex.Message +
                    "\n\n你仍可继续查看 GPU 温度，但 CPU 标准测试需要该驱动。",
                    "安装失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private CardPanel BuildTestCard(
            out Label hintLabel,
            out Label phaseLabel,
            out Label timeLabel,
            out Label beforeHistory,
            out Label afterHistory,
            out Panel progressTrack,
            out Panel progressFill,
            out RadioButton beforeRadio,
            out RadioButton afterRadio,
            out AccentButton startButton)
        {
            CardPanel card = new CardPanel { Padding = new Padding(17) };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(layout);

            Panel heading = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label title = MakeLabel("标准对比测试", 10.5f, FontStyle.Bold, Theme.Text);
            title.Location = new Point(0, 3);
            Label duration = MakeLabel("约 6 分钟", 8.5f, FontStyle.Bold, Theme.Accent);
            duration.AutoSize = false;
            duration.TextAlign = ContentAlignment.MiddleRight;
            duration.Dock = DockStyle.Right;
            duration.Width = 90;
            heading.Controls.Add(title);
            heading.Controls.Add(duration);
            layout.Controls.Add(heading, 0, 0);

            hintLabel = MakeLabel(
                "静置 1 分钟 → CPU 负载 4 分钟 → 恢复 1 分钟。清灰前后请使用相同电源模式，并尽量保持室温接近。",
                8.2f, FontStyle.Regular, Theme.Muted);
            hintLabel.AutoSize = false;
            hintLabel.Dock = DockStyle.Fill;
            hintLabel.Padding = new Padding(0, 0, 0, 7);
            layout.Controls.Add(hintLabel, 0, 1);

            TableLayoutPanel stageRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 7)
            };
            stageRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            stageRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            beforeRadio = CreateStageRadio("清灰前", true);
            afterRadio = CreateStageRadio("清灰后", false);
            stageRow.Controls.Add(beforeRadio, 0, 0);
            stageRow.Controls.Add(afterRadio, 1, 0);
            beforeRadio.CheckedChanged += delegate { ApplyStageSelection(); };
            afterRadio.CheckedChanged += delegate { ApplyStageSelection(); };
            layout.Controls.Add(stageRow, 0, 2);

            startButton = new AccentButton
            {
                Text = "开始清灰前测试",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 6)
            };
            startButton.Click += delegate { StartButton_Click(); };
            layout.Controls.Add(startButton, 0, 3);

            Panel progressPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            phaseLabel = MakeLabel("准备就绪", 8.5f, FontStyle.Bold, Theme.Muted);
            phaseLabel.AutoSize = true;
            phaseLabel.Location = new Point(0, 2);
            timeLabel = MakeLabel("", 8.5f, FontStyle.Regular, Theme.Muted);
            timeLabel.AutoSize = false;
            timeLabel.TextAlign = ContentAlignment.MiddleRight;
            timeLabel.Dock = DockStyle.Right;
            timeLabel.Width = 120;
            progressTrack = new Panel
            {
                BackColor = Theme.Input,
                Location = new Point(0, 27),
                Height = 6,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            progressFill = new Panel
            {
                BackColor = Theme.Accent,
                Dock = DockStyle.Left,
                Width = 0
            };
            progressTrack.Controls.Add(progressFill);
            Panel phaseProgressTrack = progressTrack;
            progressPanel.Resize += delegate
            {
                phaseProgressTrack.Width = Math.Max(0, progressPanel.ClientSize.Width);
            };
            progressPanel.Controls.Add(phaseLabel);
            progressPanel.Controls.Add(timeLabel);
            progressPanel.Controls.Add(progressTrack);
            layout.Controls.Add(progressPanel, 0, 4);

            Panel historyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            beforeHistory = MakeLabel("清灰前：暂无记录", 8.2f, FontStyle.Regular, Theme.Dim);
            beforeHistory.AutoSize = false;
            beforeHistory.Dock = DockStyle.Top;
            beforeHistory.Height = 22;
            afterHistory = MakeLabel("清灰后：暂无记录", 8.2f, FontStyle.Regular, Theme.Dim);
            afterHistory.AutoSize = false;
            afterHistory.Dock = DockStyle.Top;
            afterHistory.Height = 22;
            historyPanel.Controls.Add(afterHistory);
            historyPanel.Controls.Add(beforeHistory);
            layout.Controls.Add(historyPanel, 0, 5);

            Panel testNotice = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Input,
                Margin = new Padding(0, 3, 0, 4)
            };
            Label notice = MakeLabel(
                "测试期间风扇加速和温度升高属于正常现象。程序会在 CPU ≥ 105°C 或 GPU 热点 ≥ 115°C 时自动终止。",
                8f, FontStyle.Regular, Theme.Dim);
            notice.AutoSize = false;
            notice.Dock = DockStyle.Fill;
            notice.Padding = new Padding(9, 6, 9, 5);
            testNotice.Controls.Add(notice);
            layout.Controls.Add(testNotice, 0, 6);

            Label note = MakeLabel(
                "提示：清灰后先正常使用 10–20 分钟，再开始测试。",
                7.8f, FontStyle.Regular, Theme.Dim);
            note.AutoSize = false;
            note.Dock = DockStyle.Fill;
            layout.Controls.Add(note, 0, 7);
            return card;
        }

        private CardPanel BuildCompareCard(
            out Label headline,
            out Label explanation,
            out CompareRowLabels cpuAvg,
            out CompareRowLabels cpuPeak,
            out CompareRowLabels cpuIdle,
            out CompareRowLabels gpu)
        {
            CardPanel card = new CardPanel { Padding = new Padding(16) };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 8,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(layout);

            Label title = MakeLabel("清灰前后对比", 10.5f, FontStyle.Bold, Theme.Text);
            title.Dock = DockStyle.Fill;
            title.Padding = new Padding(0, 3, 0, 0);
            layout.SetColumnSpan(title, 3);
            layout.Controls.Add(title, 0, 0);

            headline = MakeLabel("再完成一次清灰后测试即可对比", 11f, FontStyle.Bold, Theme.Muted);
            headline.AutoSize = false;
            headline.Dock = DockStyle.Fill;
            headline.TextAlign = ContentAlignment.MiddleLeft;
            layout.SetColumnSpan(headline, 3);
            layout.Controls.Add(headline, 0, 1);

            AddCompareHeader(layout);
            cpuAvg = AddCompareRow(layout, 3, "CPU 负载平均");
            cpuPeak = AddCompareRow(layout, 4, "CPU 负载峰值");
            cpuIdle = AddCompareRow(layout, 5, "CPU 静置平均");
            gpu = AddCompareRow(layout, 6, "GPU 最高温度");

            explanation = MakeLabel(
                "比较时会优先使用“CPU 负载”阶段的数据，避免只拿一两个瞬时读数作判断。",
                7.3f, FontStyle.Regular, Theme.Dim);
            explanation.AutoSize = false;
            explanation.Dock = DockStyle.Fill;
            explanation.Padding = new Padding(0, 5, 0, 0);
            layout.SetColumnSpan(explanation, 3);
            layout.Controls.Add(explanation, 0, 7);
            return card;
        }

        private static void AddCompareHeader(TableLayoutPanel layout)
        {
            string[] text = { "指标", "清灰前 → 清灰后", "变化" };
            Color[] colors = { Theme.Dim, Theme.Dim, Theme.Dim };
            for (int i = 0; i < 3; i++)
            {
                Label label = MakeLabel(text[i], 7.6f, FontStyle.Bold, colors[i]);
                label.Dock = DockStyle.Fill;
                label.TextAlign = i == 2 ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                layout.Controls.Add(label, i, 2);
            }
        }

        private static CompareRowLabels AddCompareRow(TableLayoutPanel layout, int row, string caption)
        {
            Label name = MakeLabel(caption, 8.2f, FontStyle.Regular, Theme.Muted);
            name.Dock = DockStyle.Fill;
            name.TextAlign = ContentAlignment.MiddleLeft;
            Label value = MakeLabel("--", 8.2f, FontStyle.Bold, Theme.Dim);
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.MiddleLeft;
            Label delta = MakeLabel("--", 8.2f, FontStyle.Bold, Theme.Dim);
            delta.Dock = DockStyle.Fill;
            delta.TextAlign = ContentAlignment.MiddleRight;
            layout.Controls.Add(name, 0, row);
            layout.Controls.Add(value, 1, row);
            layout.Controls.Add(delta, 2, row);
            return new CompareRowLabels { Value = value, Delta = delta };
        }

        private static DataGridView BuildSensorGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Card,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Grid,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                ColumnHeadersHeight = 30,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowTemplate = { Height = 28 },
                EnableHeadersVisualStyles = false,
                Font = Theme.Font(8.2f, FontStyle.Regular)
            };
            grid.DefaultCellStyle.BackColor = Theme.Card;
            grid.DefaultCellStyle.ForeColor = Theme.Text;
            grid.DefaultCellStyle.SelectionBackColor = Theme.CardAlt;
            grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Card;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Muted;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Card;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.Font(7.8f, FontStyle.Bold);
            grid.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            grid.AdvancedCellBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "sensor",
                HeaderText = "传感器",
                FillWeight = 44,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "value",
                HeaderText = "当前值",
                FillWeight = 18,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "device",
                HeaderText = "设备",
                FillWeight = 38,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            return grid;
        }

        private static RadioButton CreateStageRadio(string text, bool selected)
        {
            RadioButton radio = new RadioButton
            {
                Text = text,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                Checked = selected,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = Theme.Font(9f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                BackColor = Theme.Input,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            radio.FlatAppearance.BorderColor = Theme.Border;
            radio.FlatAppearance.CheckedBackColor = Theme.AccentDark;
            radio.FlatAppearance.MouseOverBackColor = Theme.CardAlt;
            radio.FlatAppearance.MouseDownBackColor = Theme.AccentDark;
            return radio;
        }

        private static Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            return new Label
            {
                Text = text,
                Font = Theme.Font(size, style),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }

        private void ApplyStageSelection()
        {
            if (_beforeRadio == null || _afterRadio == null)
                return;
            _beforeRadio.ForeColor = _beforeRadio.Checked ? Theme.Text : Theme.Muted;
            _afterRadio.ForeColor = _afterRadio.Checked ? Theme.Text : Theme.Muted;
            if (!_testActive)
            {
                _startButton.Text = _beforeRadio.Checked ? "开始清灰前测试" : "开始清灰后测试";
            }
        }

        private void LoadSavedSessions()
        {
            _beforeSession = SessionStore.Load(MeasurementStage.Before);
            _afterSession = SessionStore.Load(MeasurementStage.After);
            UpdateHistoryLabels();
            UpdateComparison();
        }

        private void UpdateHistoryLabels()
        {
            string before = _beforeSession == null
                ? "清灰前：暂无记录"
                : "清灰前：" + _beforeSession.CreatedAtUtc.ToLocalTime().ToString("MM-dd HH:mm") +
                  " · CPU 满载均温 " + FormatTemperature(_beforeSession.Load.AvgCpu);
            string after = _afterSession == null
                ? "清灰后：暂无记录"
                : "清灰后：" + _afterSession.CreatedAtUtc.ToLocalTime().ToString("MM-dd HH:mm") +
                  " · CPU 满载均温 " + FormatTemperature(_afterSession.Load.AvgCpu);

            _beforeHistoryLabel.Text = before;
            _beforeHistoryLabel.ForeColor = _beforeSession == null ? Theme.Dim : Theme.Accent;
            _afterHistoryLabel.Text = after;
            _afterHistoryLabel.ForeColor = _afterSession == null ? Theme.Dim : Theme.Green;

            if (_beforeSession == null)
                _beforeRadio.Checked = true;
            else if (_afterSession == null)
                _afterRadio.Checked = true;
        }

        private void Sampler_Sampled(object sender, SampleEventArgs e)
        {
            if (IsDisposed || Disposing)
                return;
            try
            {
                BeginInvoke(new Action<SensorSnapshot>(HandleSnapshot), e.Snapshot);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void Sampler_SamplingError(object sender, string message)
        {
            if (IsDisposed || Disposing)
                return;
            try
            {
                BeginInvoke(new Action<string>(HandleSamplingError), message);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void HandleSamplingError(string message)
        {
            _statusLabel.Text = "传感器异常 · " + message;
            _statusLabel.ForeColor = Theme.Red;
            _startButton.Enabled = false;
        }

        private void HandleSnapshot(SensorSnapshot snapshot)
        {
            _latest = snapshot;
            _sensorReady = snapshot.HasCpu;
            _startButton.Enabled = snapshot.HasCpu || _testActive;

            if (snapshot.HasCpu)
            {
                _statusLabel.Text = snapshot.HasGpu
                    ? "传感器已连接  ·  每秒采样  ·  LibreHardwareMonitor"
                    : "CPU 传感器已连接  ·  未读 GPU 温度";
                _statusLabel.ForeColor = snapshot.HasGpu ? Theme.Green : Theme.Yellow;
            }
            else
            {
                _statusLabel.Text = snapshot.HasGpu
                    ? "未读到 CPU 温度  ·  请确认管理员权限"
                    : "未读到温度传感器  ·  请确认管理员权限";
                _statusLabel.ForeColor = Theme.Red;
            }
            _cpuInfoLabel.Text = "CPU：" + Shorten(snapshot.CpuName, 26);
            _gpuInfoLabel.Text = "GPU：" + Shorten(snapshot.GpuName, 26);
            _cpuInfoLabel.ForeColor = snapshot.HasCpu ? Theme.Muted : Theme.Red;
            _gpuInfoLabel.ForeColor = snapshot.HasGpu ? Theme.Muted : Theme.Red;

            _cpuCard.Set(
                snapshot.HasCpu ? snapshot.CpuTemp.ToString("0.0") + "°" : "--",
                snapshot.HasCpu
                    ? Shorten(snapshot.CpuSensorName, 16) + " · " + Shorten(snapshot.CpuName, 18)
                    : "未检测到 CPU 温度");
            string gpuDetail = snapshot.HasGpu
                ? Shorten(snapshot.GpuSensorName, 16) + " · " +
                  (snapshot.HasGpuHotspot ? "热点 " + snapshot.GpuHotspot.ToString("0") + "°" : Shorten(snapshot.GpuName, 16))
                : "未检测到 GPU 温度";
            _gpuCard.Set(snapshot.HasGpu ? snapshot.GpuTemp.ToString("0.0") + "°" : "--", gpuDetail);
            _loadCard.Set(
                snapshot.HasCpuLoad ? snapshot.CpuLoad.ToString("0") + "%" : "--",
                snapshot.HasFan ? "风扇 " + snapshot.FanRpm.ToString("0") + " RPM" : "未检测到风扇转速");

            _chart.Add(snapshot);
            _sensorGridTick++;
            if (_sensorGridTick % 2 == 0)
                UpdateSensorGrid(snapshot);

            if (_testActive)
            {
                AddTestSample(snapshot);
                if (_phaseIndex == 1 &&
                    ((snapshot.HasCpu && snapshot.CpuTemp >= 105) ||
                     (snapshot.HasGpuHotspot && snapshot.GpuHotspot >= 115) ||
                     (snapshot.HasGpu && snapshot.GpuTemp >= 100)))
                {
                    AbortTestForTemperature();
                }
            }
        }

        private void UpdateSensorGrid(SensorSnapshot snapshot)
        {
            if (snapshot.Rows == null || snapshot.Rows.Count == 0)
                return;
            _sensorGrid.SuspendLayout();
            try
            {
                _sensorGrid.Rows.Clear();
                foreach (LiveSensor sensor in snapshot.Rows)
                {
                    string value = sensor.Type == SensorType.Temperature
                        ? sensor.Value.ToString("0.0") + " °C"
                        : sensor.Value.ToString("0") + " RPM";
                    int index = _sensorGrid.Rows.Add(sensor.Name, value, sensor.HardwareName);
                    DataGridViewRow row = _sensorGrid.Rows[index];
                    row.Cells[0].Style.ForeColor = Theme.Text;
                    row.Cells[1].Style.ForeColor =
                        sensor.Type == SensorType.Fan ? Theme.Green :
                        (sensor.Group == "CPU" ? Theme.Accent :
                         sensor.Group == "GPU" ? Theme.Orange : Theme.Text);
                    row.Cells[2].Style.ForeColor = Theme.Dim;
                    row.Tag = sensor.Group;
                }
            }
            finally
            {
                _sensorGrid.ResumeLayout();
                _sensorGrid.ClearSelection();
                _sensorGrid.CurrentCell = null;
            }
        }

        private void StartButton_Click()
        {
            if (_testActive)
            {
                CancelCurrentTest();
                return;
            }
            if (!_sensorReady || _latest == null || !_latest.HasCpu)
            {
                MessageBox.Show(
                    "当前没有读到 CPU 温度传感器，标准测试无法得到有效结果。\n\n" +
                    "请以管理员身份启动程序，并检查安全软件是否拦截了硬件驱动。",
                    "传感器不可用",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            MeasurementStage stage = _beforeRadio.Checked
                ? MeasurementStage.Before
                : MeasurementStage.After;
            bool hasExisting = stage == MeasurementStage.Before
                ? _beforeSession != null
                : _afterSession != null;
            string overwriteNotice = hasExisting
                ? "\n\n该阶段已有记录，本次完成后会更新为最新结果。"
                : "";
            DialogResult result = MessageBox.Show(
                "将开始约 6 分钟的标准测试：\n\n" +
                "1. 静置采样 1 分钟\n" +
                "2. 低优先级 CPU 满载 4 分钟\n" +
                "3. 恢复采样 1 分钟\n\n" +
                "测试期间请暂时不要运行游戏、渲染或其他高负载程序。风扇变响、温度升高属于正常现象。" +
                overwriteNotice,
                SessionStore.StageLabel(stage) + "标准测试",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            if (result != DialogResult.OK)
                return;

            BeginTest(stage);
        }

        private void BeginTest(MeasurementStage stage)
        {
            _testActive = true;
            _testStage = stage;
            _testStarted = DateTime.Now;
            _phaseIndex = 0;
            _idleAccumulator = new MetricAccumulator();
            _loadAccumulator = new MetricAccumulator();
            _recoveryAccumulator = new MetricAccumulator();

            try
            {
                string key = SessionStore.StageKey(stage);
                string timestamp = _testStarted.ToString("yyyyMMdd_HHmmss");
                _rawCsvPath = Path.Combine(
                    SessionStore.ReportsDirectory,
                    key + "_raw_" + timestamp + "_partial.csv");
                _rawCsv = new StreamWriter(_rawCsvPath, false, new UTF8Encoding(true));
                _rawCsv.WriteLine("时间,阶段,CPU温度,GPU温度,GPU热点,CPU负载,风扇RPM");
                _rawCsv.Flush();
            }
            catch (Exception ex)
            {
                _testActive = false;
                MessageBox.Show("无法创建本次测试的原始记录：\n" + ex.Message,
                    "清灰温度对比", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _beforeRadio.Enabled = false;
            _afterRadio.Enabled = false;
            _exportButton.Enabled = false;
            _startButton.Text = "取消本次测试";
            _startButton.ForeColor = Color.White;
            _startButton.NormalColor = Theme.Red;
            _startButton.HoverColor = Color.FromArgb(255, 130, 130);
            _testHintLabel.Text =
                "测试正在进行。请保持程序运行，不要切换电源模式或启动高负载程序。";
            EnterPhase(0);
            _testTimer.Start();
        }

        private void TestTimer_Tick(object sender, EventArgs e)
        {
            if (!_testActive)
                return;

            DateTime now = DateTime.Now;
            if (now >= _phaseEnds)
            {
                if (_phaseIndex < 2)
                {
                    EnterPhase(_phaseIndex + 1);
                }
                else
                {
                    FinishTest();
                }
                return;
            }

            double phaseRemaining = Math.Max(0, (_phaseEnds - now).TotalSeconds);
            double phaseDuration = _phaseSeconds[_phaseIndex];
            double phaseProgress = Math.Max(0, Math.Min(1, 1.0 - phaseRemaining / phaseDuration));
            double totalRemaining = Math.Max(0, (_testStarted.AddSeconds(360) - now).TotalSeconds);
            _phaseLabel.Text = SessionStore.StageLabel(_testStage) + " · " + _phaseNames[_phaseIndex];
            _timeLabel.Text = "阶段 " + FormatClock(phaseRemaining) +
                              "  ·  总剩余 " + FormatClock(totalRemaining);
            int trackWidth = Math.Max(0, _progressTrack.ClientSize.Width);
            _progressFill.Width = (int)Math.Round(trackWidth * phaseProgress);
        }

        private void AddTestSample(SensorSnapshot snapshot)
        {
            MetricAccumulator accumulator = _phaseIndex == 0
                ? _idleAccumulator
                : (_phaseIndex == 1 ? _loadAccumulator : _recoveryAccumulator);
            accumulator.Add(snapshot);

            if (_rawCsv != null)
            {
                try
                {
                    _rawCsv.WriteLine(
                        snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "," +
                        _phaseNames[_phaseIndex] + "," +
                        CsvNumber(snapshot.HasCpu, snapshot.CpuTemp) + "," +
                        CsvNumber(snapshot.HasGpu, snapshot.GpuTemp) + "," +
                        CsvNumber(snapshot.HasGpuHotspot, snapshot.GpuHotspot) + "," +
                        CsvNumber(snapshot.HasCpuLoad, snapshot.CpuLoad) + "," +
                        CsvNumber(snapshot.HasFan, snapshot.FanRpm));
                    _rawCsv.Flush();
                }
                catch
                {
                }
            }
        }

        private void EnterPhase(int phase)
        {
            _phaseIndex = phase;
            _phaseEnds = DateTime.Now.AddSeconds(_phaseSeconds[phase]);
            _phaseLabel.Text = SessionStore.StageLabel(_testStage) + " · " + _phaseNames[phase];
            _phaseLabel.ForeColor = phase == 1 ? Theme.Orange :
                (phase == 2 ? Theme.Green : Theme.Accent);
            _progressFill.BackColor = phase == 1 ? Theme.Orange :
                (phase == 2 ? Theme.Green : Theme.Accent);
            _progressFill.Width = 0;
            if (phase == 1)
                _loadGenerator.Start();
            else
                _loadGenerator.Stop();
        }

        private void FinishTest()
        {
            _loadGenerator.Stop();
            _testTimer.Stop();
            _testActive = false;
            CloseRawCsv(false);

            SensorSnapshot latest = _latest;
            SessionRecord session = new SessionRecord
            {
                Version = "1.0.0",
                Stage = SessionStore.StageKey(_testStage),
                StageLabel = SessionStore.StageLabel(_testStage),
                CreatedAtUtc = _testStarted.ToUniversalTime(),
                CpuName = latest == null ? "未检测到" : latest.CpuName,
                GpuName = latest == null ? "未检测到" : latest.GpuName,
                MotherboardName = latest == null ? "未检测到" : latest.MotherboardName,
                CpuSensorName = latest == null ? "CPU 温度" : latest.CpuSensorName,
                GpuSensorName = latest == null ? "GPU 温度" : latest.GpuSensorName,
                Idle = _idleAccumulator.Build(),
                Load = _loadAccumulator.Build(),
                Recovery = _recoveryAccumulator.Build()
            };

            try
            {
                SessionStore.Save(session, _testStage);
                if (!string.IsNullOrEmpty(_rawCsvPath) && File.Exists(_rawCsvPath))
                {
                    string finalCsv = _rawCsvPath.Replace("_partial.csv", ".csv");
                    if (File.Exists(finalCsv))
                        File.Delete(finalCsv);
                    File.Move(_rawCsvPath, finalCsv);
                    _rawCsvPath = finalCsv;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "测试已完成，但保存记录时出错：\n" + ex.Message +
                    "\n\n原始采样文件仍保留在记录文件夹中。",
                    "保存失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            ResetTestUi();
            _beforeSession = SessionStore.Load(MeasurementStage.Before);
            _afterSession = SessionStore.Load(MeasurementStage.After);
            UpdateHistoryLabels();
            UpdateComparison();

            string summary = "CPU 负载平均温度：" + FormatTemperature(session.Load.AvgCpu) +
                             "\nCPU 负载最高温度：" + FormatTemperature(session.Load.MaxCpu) +
                             "\n记录已保存到：“" + SessionStore.ReportsDirectory + "”" +
                             "\n\n建议静置散热几分钟后再关闭程序。";
            MessageBox.Show(summary, SessionStore.StageLabel(_testStage) + "测试完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CancelCurrentTest()
        {
            _loadGenerator.Stop();
            _testTimer.Stop();
            _testActive = false;
            CloseRawCsv(true);
            ResetTestUi();
            LoadSavedSessions();
        }

        private void AbortTestForTemperature()
        {
            if (!_testActive)
                return;
            _loadGenerator.Stop();
            _testTimer.Stop();
            _testActive = false;
            CloseRawCsv(true);
            ResetTestUi();
            LoadSavedSessions();
            MessageBox.Show(
                "检测到温度超过安全阈值，本次负载测试已自动终止，结果不会保存。\n\n" +
                "请等待电脑冷却，并检查散热器安装、风扇运转和进出风口是否畅通。",
                "安全保护已触发",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void UpdateComparison()
        {
            bool hasBoth = _beforeSession != null && _afterSession != null;
            _exportButton.Enabled = hasBoth && !_testActive;
            if (!hasBoth)
            {
                _compareHeadline.ForeColor = Theme.Muted;
                _compareHeadline.Text = _beforeSession == null && _afterSession == null
                    ? "再完成一次清灰后测试即可对比"
                    : (_beforeSession == null
                        ? "请先补做清灰前测试"
                        : "清灰前记录已完成，等待清灰后测试");
                _compareExplanation.Text = _beforeSession == null
                    ? "建议在清灰前完成标准测试，清灰后使用完全相同的流程。"
                    : "清灰后建议先正常使用 10–20 分钟，再开始标准测试。";
                SetCompareRow(_cpuAvgCompare, false, 0, false, 0, "°");
                SetCompareRow(_cpuPeakCompare, false, 0, false, 0, "°");
                SetCompareRow(_cpuIdleCompare, false, 0, false, 0, "°");
                SetCompareRow(_gpuCompare, false, 0, false, 0, "°");
                return;
            }

            MetricSummary before = _beforeSession.Load ?? new MetricSummary();
            MetricSummary after = _afterSession.Load ?? new MetricSummary();
            SetCompareRow(_cpuAvgCompare, before.HasCpu, before.AvgCpu, after.HasCpu, after.AvgCpu, "°");
            SetCompareRow(_cpuPeakCompare, before.HasCpu, before.MaxCpu, after.HasCpu, after.MaxCpu, "°");
            MetricSummary beforeIdle = _beforeSession.Idle ?? new MetricSummary();
            MetricSummary afterIdle = _afterSession.Idle ?? new MetricSummary();
            SetCompareRow(_cpuIdleCompare, beforeIdle.HasCpu, beforeIdle.AvgCpu,
                afterIdle.HasCpu, afterIdle.AvgCpu, "°");
            SetCompareRow(_gpuCompare, before.HasGpu, before.MaxGpu, after.HasGpu, after.MaxGpu, "°");

            if (before.HasCpu && after.HasCpu)
            {
                double change = after.AvgCpu - before.AvgCpu;
                string direction = change <= 0 ? "下降" : "上升";
                string amount = Math.Abs(change).ToString("0.0", CultureInfo.InvariantCulture) + "°C";
                if (change <= -5)
                    _compareHeadline.ForeColor = Theme.Green;
                else if (change <= -2)
                    _compareHeadline.ForeColor = Theme.Accent;
                else if (change < 1)
                    _compareHeadline.ForeColor = Theme.Yellow;
                else
                    _compareHeadline.ForeColor = Theme.Red;
                _compareHeadline.Text = "清灰后 CPU 负载均温" + direction + " " + amount;
                _compareExplanation.Text = change <= -2
                    ? "负值表示温度降低；改善结论请同时参考两次室温是否接近。"
                    : "差异较小或升高；请检查室温、摆放位置和后台负载是否一致。";
            }
            else
            {
                double gpuChange = after.MaxGpu - before.MaxGpu;
                _compareHeadline.ForeColor = gpuChange <= -2 ? Theme.Green : Theme.Yellow;
                _compareHeadline.Text = "已生成清灰前后对比";
                _compareExplanation.Text =
                    "CPU 数据不完整，可参考 GPU 温度；不替代显卡满载测试。";
            }
        }

        private void ResetTestUi()
        {
            _beforeRadio.Enabled = true;
            _afterRadio.Enabled = true;
            _startButton.ForeColor = Color.FromArgb(8, 18, 24);
            _startButton.NormalColor = Theme.Accent;
            _startButton.HoverColor = Color.FromArgb(80, 226, 247);
            _testHintLabel.Text =
                "静置 1 分钟 → CPU 负载 4 分钟 → 恢复 1 分钟。清灰前后请使用相同电源模式，并尽量保持室温接近。";
            _phaseLabel.Text = "准备就绪";
            _phaseLabel.ForeColor = Theme.Muted;
            _timeLabel.Text = "";
            _progressFill.Width = 0;
            _progressFill.BackColor = Theme.Accent;
            ApplyStageSelection();
        }

        private void CloseRawCsv(bool deletePartial)
        {
            if (_rawCsv != null)
            {
                try
                {
                    _rawCsv.Flush();
                    _rawCsv.Dispose();
                }
                catch
                {
                }
                _rawCsv = null;
            }
            if (deletePartial && !string.IsNullOrEmpty(_rawCsvPath) && File.Exists(_rawCsvPath))
            {
                try
                {
                    File.Delete(_rawCsvPath);
                }
                catch
                {
                }
            }
        }

        private static string CsvNumber(bool hasValue, double value)
        {
            return hasValue ? value.ToString("0.0", CultureInfo.InvariantCulture) : "";
        }

        private static string FormatClock(double totalSeconds)
        {
            int seconds = Math.Max(0, (int)Math.Ceiling(totalSeconds));
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        private static void SetCompareRow(
            CompareRowLabels row,
            bool beforeHasValue,
            double beforeValue,
            bool afterHasValue,
            double afterValue,
            string suffix)
        {
            if (!beforeHasValue || !afterHasValue)
            {
                row.Value.Text = "--";
                row.Value.ForeColor = Theme.Dim;
                row.Delta.Text = "--";
                row.Delta.ForeColor = Theme.Dim;
                return;
            }

            double difference = afterValue - beforeValue;
            row.Value.Text =
                beforeValue.ToString("0.0", CultureInfo.InvariantCulture) + suffix +
                "  →  " +
                afterValue.ToString("0.0", CultureInfo.InvariantCulture) + suffix;
            row.Delta.Text =
                (difference > 0 ? "+" : "") +
                difference.ToString("0.0", CultureInfo.InvariantCulture) + suffix;
            row.Delta.ForeColor = difference <= -0.05
                ? Theme.Green
                : (difference >= 0.05 ? Theme.Red : Theme.Muted);
            row.Value.ForeColor = Theme.Text;
        }

        private static string FormatTemperature(double value)
        {
            return value <= 0 ? "--" : value.ToString("0.0") + "°C";
        }

        private static string Shorten(string value, int limit)
        {
            if (string.IsNullOrEmpty(value))
                return "未检测到";
            return value.Length <= limit ? value : value.Substring(0, limit - 1) + "…";
        }

        private void ExportReport()
        {
            if (_beforeSession == null || _afterSession == null)
                return;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "导出清灰前后温度对比报告";
                dialog.Filter = "CSV 报告 (*.csv)|*.csv|文本文件 (*.txt)|*.txt";
                dialog.InitialDirectory = SessionStore.ReportsDirectory;
                dialog.FileName = "清灰温度对比_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    StringBuilder report = new StringBuilder();
                    report.AppendLine("电脑清灰前后温度对比");
                    report.AppendLine("导出时间," + EscapeCsv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    report.AppendLine("测试方法," + EscapeCsv("静置1分钟 + CPU负载4分钟 + 恢复1分钟"));
                    report.AppendLine();
                    report.AppendLine("阶段,记录时间,CPU,GPU,主板,CPU传感器,GPU传感器");
                    AppendSessionMetadata(report, _beforeSession);
                    AppendSessionMetadata(report, _afterSession);
                    report.AppendLine();
                    report.AppendLine("指标,清灰前,清灰后,变化(后-前),单位");
                    AppendMetricRow(report, "CPU负载平均温度",
                        _beforeSession.Load, _afterSession.Load, true, true, "°C");
                    AppendMetricRow(report, "CPU负载最高温度",
                        _beforeSession.Load, _afterSession.Load, false, false, "°C");
                    AppendMetricRow(report, "CPU静置平均温度",
                        _beforeSession.Idle, _afterSession.Idle, true, true, "°C");
                    AppendMetricRow(report, "GPU最高温度",
                        _beforeSession.Load, _afterSession.Load, false, false, "°C");
                    AppendMetricRow(report, "风扇最高转速",
                        _beforeSession.Load, _afterSession.Load, false, false, "RPM");
                    report.AppendLine();
                    report.AppendLine("说明," +
                        EscapeCsv("负值表示清灰后更低。CPU 对比以标准负载阶段为准；GPU 仅处于日常轻负载，不能替代显卡满载测试。"));
                    File.WriteAllText(dialog.FileName, report.ToString(), new UTF8Encoding(true));
                    MessageBox.Show(
                        "对比报告已导出：\n" + dialog.FileName,
                        "导出完成",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导出失败：\n" + ex.Message, "清灰温度对比",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void AppendSessionMetadata(StringBuilder report, SessionRecord session)
        {
            report.Append(EscapeCsv(session.StageLabel)).Append(",");
            report.Append(EscapeCsv(session.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))).Append(",");
            report.Append(EscapeCsv(session.CpuName)).Append(",");
            report.Append(EscapeCsv(session.GpuName)).Append(",");
            report.Append(EscapeCsv(session.MotherboardName)).Append(",");
            report.Append(EscapeCsv(session.CpuSensorName)).Append(",");
            report.Append(EscapeCsv(session.GpuSensorName)).AppendLine();
        }

        private static void AppendMetricRow(
            StringBuilder report,
            string name,
            MetricSummary before,
            MetricSummary after,
            bool beforeUseAverage,
            bool afterUseAverage,
            string unit)
        {
            if (before == null || after == null)
                return;
            bool hasBefore = unit == "RPM" ? before.HasFan : before.HasCpu;
            bool hasAfter = unit == "RPM" ? after.HasFan : after.HasCpu;
            if (name.IndexOf("GPU", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hasBefore = before.HasGpu;
                hasAfter = after.HasGpu;
            }
            double beforeValue = unit == "RPM"
                ? before.MaxFan
                : (name.IndexOf("GPU", StringComparison.OrdinalIgnoreCase) >= 0
                    ? before.MaxGpu
                    : (beforeUseAverage ? before.AvgCpu : before.MaxCpu));
            double afterValue = unit == "RPM"
                ? after.MaxFan
                : (name.IndexOf("GPU", StringComparison.OrdinalIgnoreCase) >= 0
                    ? after.MaxGpu
                    : (afterUseAverage ? after.AvgCpu : after.MaxCpu));

            report.Append(EscapeCsv(name)).Append(",");
            report.Append(hasBefore ? beforeValue.ToString("0.0", CultureInfo.InvariantCulture) : "").Append(",");
            report.Append(hasAfter ? afterValue.ToString("0.0", CultureInfo.InvariantCulture) : "").Append(",");
            report.Append(hasBefore && hasAfter
                ? (afterValue - beforeValue).ToString("0.0", CultureInfo.InvariantCulture)
                : "").Append(",");
            report.Append(EscapeCsv(unit)).AppendLine();
        }

        private static string EscapeCsv(string value)
        {
            string text = value ?? string.Empty;
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private void OpenReportsFolder()
        {
            try
            {
                Directory.CreateDirectory(SessionStore.ReportsDirectory);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + SessionStore.ReportsDirectory + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开记录文件夹：\n" + ex.Message, "清灰温度对比",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_testActive || _closingAfterTestPrompt)
                return;
            DialogResult result = MessageBox.Show(
                "标准测试尚未结束。现在退出会取消本次记录，且不会保存这次结果。\n\n确定退出吗？",
                "取消测试",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            _closingAfterTestPrompt = true;
            CancelCurrentTest();
        }

        private sealed class CompareRowLabels
        {
            public Label Value;
            public Label Delta;
        }
    }
}
