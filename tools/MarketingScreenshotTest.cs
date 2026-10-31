using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace MarketingScreenshot
{
    internal static class MarketingProgram
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (CleanTempCompare.AdvancedMainForm form = new CleanTempCompare.AdvancedMainForm())
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-3000, -3000);
                    form.Show();
                    Pump(1500);
                    MethodInfo handle = typeof(CleanTempCompare.AdvancedMainForm).GetMethod(
                        "HandleSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
                    for (int i = 0; i < 35; i++)
                    {
                        double wave = Math.Sin(i / 4.0);
                        handle.Invoke(form, new object[] { BuildSnapshot(i, wave) });
                        Application.DoEvents();
                        Thread.Sleep(30);
                    }
                    Invoke(form, "ShowPage", 0);
                    Application.DoEvents();
                    Capture(form, "showcase-overview.png");

                    Invoke(form, "ShowPage", 1);
                    ComboBox sort = GetField<ComboBox>(form, "_sortCombo");
                    if (sort != null) sort.SelectedIndex = 6;
                    Application.DoEvents();
                    Capture(form, "showcase-sensors.png");

                    SaveDemoSessions();
                    Invoke(form, "RefreshStoredSessions");
                    Invoke(form, "ShowPage", 2);
                    Application.DoEvents();
                    Capture(form, "showcase-compare.png");
                    form.Close();
                }
                Console.WriteLine("MARKETING_SCREENSHOTS_OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static CleanTempCompare.SensorSnapshot BuildSnapshot(int tick, double wave)
        {
            CleanTempCompare.SensorSnapshot s = new CleanTempCompare.SensorSnapshot
            {
                Timestamp = DateTime.Now,
                CpuName = "Intel Core i9-13900HX",
                GpuName = "NVIDIA GeForce RTX 4060 Laptop GPU",
                MotherboardName = "Example Laptop Board",
                CpuSensorName = "CPU Package",
                GpuSensorName = "GPU Core",
                HasCpu = true,
                HasGpu = true,
                HasGpuHotspot = true,
                HasCpuLoad = true,
                HasGpuLoad = true,
                HasCpuPower = true,
                HasGpuPower = true,
                HasCpuClock = true,
                HasGpuClock = true,
                HasFan = true,
                CpuTemp = 64.0 + wave * 1.9,
                GpuTemp = 72.0 + wave * 1.2,
                GpuHotspot = 81.0 + wave * 1.7,
                CpuLoad = 38.0 + wave * 8.0,
                GpuLoad = 94.0 + wave * 2.5,
                CpuPower = 48.0 + wave * 5.0,
                GpuPower = 108.0 + wave * 6.0,
                CpuClock = 4180 + wave * 120,
                GpuClock = 2520 + wave * 30,
                FanRpm = 3400 + wave * 260
            };
            Add(s, "CPU", s.CpuName, "CPU Package", SensorType.Temperature, s.CpuTemp);
            Add(s, "CPU", s.CpuName, "Core Max", SensorType.Temperature, s.CpuTemp + 4.5);
            Add(s, "CPU", s.CpuName, "Core Average", SensorType.Temperature, s.CpuTemp - 1.2);
            Add(s, "CPU", s.CpuName, "CPU Total", SensorType.Load, s.CpuLoad);
            Add(s, "CPU", s.CpuName, "Package", SensorType.Power, s.CpuPower);
            Add(s, "CPU", s.CpuName, "Cores (avg)", SensorType.Clock, s.CpuClock);
            Add(s, "GPU", s.GpuName, "GPU Core", SensorType.Temperature, s.GpuTemp);
            Add(s, "GPU", s.GpuName, "GPU Hot Spot", SensorType.Temperature, s.GpuHotspot);
            Add(s, "GPU", s.GpuName, "GPU Memory Junction", SensorType.Temperature, 76.0 + wave);
            Add(s, "GPU", s.GpuName, "GPU Core", SensorType.Load, s.GpuLoad);
            Add(s, "GPU", s.GpuName, "GPU Package", SensorType.Power, s.GpuPower);
            Add(s, "GPU", s.GpuName, "GPU Core", SensorType.Clock, s.GpuClock);
            Add(s, "散热器", "Laptop Fans", "Fan #1", SensorType.Fan, s.FanRpm);
            Add(s, "主板", s.MotherboardName, "System", SensorType.Temperature, 45.0 + wave);
            Add(s, "存储", "NVMe SSD", "Temperature", SensorType.Temperature, 49.0 + wave * 0.8);
            return s;
        }

        private static void Add(
            CleanTempCompare.SensorSnapshot snapshot,
            string group,
            string hardware,
            string name,
            SensorType type,
            double value)
        {
            snapshot.Rows.Add(new CleanTempCompare.LiveSensor
            {
                Group = group,
                HardwareName = hardware,
                Name = name,
                Identifier = hardware + "|" + type + "|" + name,
                Unit = CleanTempCompare.SensorFormat.Unit(type),
                Type = type,
                Value = value
            });
        }

        private static void SaveDemoSessions()
        {
            CleanTempCompare.SessionStore.Save(BuildSession(false, false, 73.4, 84.0, 61.0, 70.0, 0.9),
                CleanTempCompare.MeasurementStage.Before, CleanTempCompare.TestMode.Standard);
            CleanTempCompare.SessionStore.Save(BuildSession(false, true, 68.2, 78.0, 57.0, 65.0, 0.5),
                CleanTempCompare.MeasurementStage.After, CleanTempCompare.TestMode.Standard);
            CleanTempCompare.SessionStore.Save(BuildSession(true, false, 80.2, 91.0, 76.0, 86.0, 2.4),
                CleanTempCompare.MeasurementStage.Before, CleanTempCompare.TestMode.Gaming);
            CleanTempCompare.SessionStore.Save(BuildSession(true, true, 72.8, 84.0, 70.0, 80.0, 1.2),
                CleanTempCompare.MeasurementStage.After, CleanTempCompare.TestMode.Gaming);
        }

        private static CleanTempCompare.SessionRecord BuildSession(
            bool gaming,
            bool after,
            double cpu,
            double cpuMax,
            double gpu,
            double gpuMax,
            double fluctuation)
        {
            List<CleanTempCompare.SensorStat> stats = new List<CleanTempCompare.SensorStat>
            {
                new CleanTempCompare.SensorStat
                {
                    Identifier = "CPU|Temperature|CPU Package",
                    Name = "CPU Package",
                    Hardware = "Intel Core i9-13900HX",
                    Group = "CPU",
                    Type = "温度",
                    Unit = "°C",
                    Min = cpu - cpu * 0.06,
                    Avg = cpu,
                    Max = cpuMax,
                    StdDev = fluctuation,
                    Samples = gaming ? 600 : 300
                },
                new CleanTempCompare.SensorStat
                {
                    Identifier = "GPU|Temperature|GPU Core",
                    Name = "GPU Core",
                    Hardware = "NVIDIA GeForce RTX 4060 Laptop GPU",
                    Group = "GPU",
                    Type = "温度",
                    Unit = "°C",
                    Min = gpu - 4,
                    Avg = gpu,
                    Max = gpuMax,
                    StdDev = fluctuation * 0.8,
                    Samples = gaming ? 600 : 300
                }
            };
            CleanTempCompare.MetricSummary idle = new CleanTempCompare.MetricSummary
            {
                HasCpu = true,
                HasGpu = true,
                HasFan = true,
                HasCpuPower = true,
                AvgCpu = after ? 49.0 : 54.0,
                MaxCpu = after ? 53.0 : 58.0,
                AvgGpu = after ? 47.0 : 50.0,
                MaxGpu = after ? 49.0 : 53.0,
                AvgCpuPower = 14.0,
                MaxFan = 2300,
                SensorStats = stats
            };
            CleanTempCompare.MetricSummary load = new CleanTempCompare.MetricSummary
            {
                HasCpu = true,
                HasGpu = true,
                HasFan = true,
                HasCpuLoad = true,
                HasGpuLoad = true,
                HasCpuPower = true,
                HasGpuPower = true,
                AvgCpu = cpu,
                MaxCpu = cpuMax,
                AvgGpu = gpu,
                MaxGpu = gpuMax,
                AvgCpuLoad = gaming ? 48.0 : 2.0,
                AvgGpuLoad = gaming ? 94.0 : 1.0,
                AvgCpuPower = gaming ? 56.0 : 16.0,
                AvgGpuPower = gaming ? 108.0 : 8.0,
                MaxFan = gaming ? 4100 : 2500,
                SensorStats = stats
            };
            return new CleanTempCompare.SessionRecord
            {
                Version = "2.3.0",
                Stage = after ? "post" : "pre",
                StageLabel = after ? "清灰后" : "清灰前",
                Mode = gaming ? "gaming" : "idle",
                WorkloadName = gaming ? "极限竞速：地平线 5 · 固定路线" : "无负载静置",
                CreatedAtUtc = DateTime.UtcNow.AddHours(after ? 0 : -2),
                CpuName = "Intel Core i9-13900HX",
                GpuName = "NVIDIA GeForce RTX 4060 Laptop GPU",
                CpuSensorName = "CPU Package",
                GpuSensorName = "GPU Core",
                Idle = idle,
                Load = load,
                Recovery = new CleanTempCompare.MetricSummary()
            };
        }

        private static T GetField<T>(object target, string name) where T : class
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(target) as T;
        }

        private static void Invoke(object target, string method, params object[] args)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, args);
        }

        private static void Pump(int milliseconds)
        {
            DateTime until = DateTime.Now.AddMilliseconds(milliseconds);
            while (DateTime.Now < until)
            {
                Application.DoEvents();
                Thread.Sleep(30);
            }
        }

        private static void Capture(Form form, string name)
        {
            using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                bitmap.Save(
                    Path.Combine(Environment.CurrentDirectory, name),
                    ImageFormat.Png);
            }
        }
    }
}
