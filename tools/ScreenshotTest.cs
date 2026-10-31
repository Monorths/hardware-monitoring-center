using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace ScreenshotSmoke
{
    internal static class ScreenshotProgram
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
                    Pump(5);
                    Capture(form, "advanced-overview.png");

                    CleanTempCompare.SessionStore.Save(BuildSession(false, false, 79.4, 93.0, 55.8, 68.0),
                        CleanTempCompare.MeasurementStage.Before, CleanTempCompare.TestMode.Standard);
                    CleanTempCompare.SessionStore.Save(BuildSession(false, true, 72.1, 86.0, 51.2, 65.0),
                        CleanTempCompare.MeasurementStage.After, CleanTempCompare.TestMode.Standard);
                    CleanTempCompare.SessionStore.Save(BuildSession(true, false, 76.2, 91.0, 62.0, 91.0),
                        CleanTempCompare.MeasurementStage.Before, CleanTempCompare.TestMode.Gaming);
                    CleanTempCompare.SessionStore.Save(BuildSession(true, true, 69.8, 84.0, 57.0, 87.0),
                        CleanTempCompare.MeasurementStage.After, CleanTempCompare.TestMode.Gaming);

                    Invoke(form, "RefreshStoredSessions");
                    Invoke(form, "ShowPage", 2);
                    Application.DoEvents();
                    Capture(form, "advanced-compare.png");
                    Invoke(form, "ShowPage", 1);
                    Application.DoEvents();
                    Capture(form, "advanced-sensors.png");
                    ComboBox statusFilter = typeof(CleanTempCompare.AdvancedMainForm).GetField(
                        "_statusFilter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form) as ComboBox;
                    ComboBox sortCombo = typeof(CleanTempCompare.AdvancedMainForm).GetField(
                        "_sortCombo", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form) as ComboBox;
                    if (statusFilter != null && statusFilter.Items.Contains("合适"))
                        statusFilter.SelectedItem = "合适";
                    if (sortCombo != null)
                        sortCombo.SelectedIndex = 6;
                    Application.DoEvents();
                    Capture(form, "advanced-sensors-filtered.png");

                    FieldInfo samplerField = typeof(CleanTempCompare.AdvancedMainForm).GetField(
                        "_sampler", BindingFlags.Instance | BindingFlags.NonPublic);
                    CleanTempCompare.HardwareSampler sampler =
                        samplerField == null ? null : samplerField.GetValue(form) as CleanTempCompare.HardwareSampler;
                    CleanTempCompare.SensorSnapshot latest = sampler == null ? null : sampler.Latest;
                    Console.WriteLine(latest == null
                        ? "SAMPLER_NO_SNAPSHOT"
                        : "SAMPLER_OK CPU=" + latest.HasCpu + " GPU=" + latest.HasGpu +
                          " SENSORS=" + (latest.Rows == null ? 0 : latest.Rows.Count));
                    if (CleanTempCompare.SessionStore.Load(
                        CleanTempCompare.MeasurementStage.Before, CleanTempCompare.TestMode.Gaming) == null)
                        throw new InvalidOperationException("Session round trip failed.");
                    CleanTempCompare.SessionRecord roundTrip = CleanTempCompare.SessionStore.Load(
                        CleanTempCompare.MeasurementStage.Before, CleanTempCompare.TestMode.Gaming);
                    if (roundTrip.Load.SensorStats == null || roundTrip.Load.SensorStats.Count == 0)
                        throw new InvalidOperationException("Detailed sensor statistics round trip failed.");
                    Console.WriteLine("SESSION_ROUNDTRIP_OK");
                    form.Close();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static CleanTempCompare.SessionRecord BuildSession(
            bool gaming, bool after, double cpu, double cpuMax, double gpu, double gpuMax)
        {
            CleanTempCompare.MetricSummary idle = new CleanTempCompare.MetricSummary
            {
                HasCpu = true,
                HasGpu = true,
                HasFan = true,
                HasCpuLoad = true,
                HasGpuLoad = true,
                HasCpuPower = true,
                HasGpuPower = true,
                AvgCpu = after ? 51.2 : 55.8,
                MaxCpu = after ? 57.0 : 62.0,
                AvgGpu = after ? 48.0 : 51.0,
                MaxGpu = after ? 52.0 : 55.0,
                AvgCpuLoad = 2.0,
                AvgGpuLoad = 1.0,
                AvgCpuPower = 15.0,
                AvgGpuPower = 8.0,
                MaxFan = 2300,
                SensorStats = new List<CleanTempCompare.SensorStat>
                {
                    new CleanTempCompare.SensorStat
                    {
                        Identifier = "CPU|Temperature|Core Average",
                        Name = "Core Average",
                        Hardware = "Test CPU",
                        Group = "CPU",
                        Type = "温度",
                        Unit = "°C",
                        Min = after ? 50.7 : 55.2,
                        Avg = after ? 51.2 : 55.8,
                        Max = after ? 51.9 : 56.4,
                        StdDev = after ? 0.5 : 0.8,
                        Samples = 300
                    },
                    new CleanTempCompare.SensorStat
                    {
                        Identifier = "GPU|Temperature|GPU Core",
                        Name = "GPU Core",
                        Hardware = "Test GPU",
                        Group = "GPU",
                        Type = "温度",
                        Unit = "°C",
                        Min = after ? 47.2 : 50.2,
                        Avg = after ? 48.0 : 51.0,
                        Max = after ? 48.8 : 51.8,
                        StdDev = after ? 0.6 : 0.9,
                        Samples = 300
                    }
                }
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
                AvgCpuLoad = gaming ? 48.0 : 98.0,
                AvgGpuLoad = gaming ? 93.0 : 20.0,
                AvgCpuPower = gaming ? 55.0 : 92.0,
                AvgGpuPower = gaming ? 104.0 : 22.0,
                MaxFan = gaming ? 4300 : 3900,
                SensorStats = new List<CleanTempCompare.SensorStat>
                {
                    new CleanTempCompare.SensorStat
                    {
                        Identifier = "CPU|Temperature|Core Average",
                        Name = "Core Average",
                        Hardware = "Test CPU",
                        Group = "CPU",
                        Type = "温度",
                        Unit = "°C",
                        Min = cpu - 4,
                        Avg = cpu,
                        Max = cpuMax,
                        StdDev = gaming ? (after ? 1.4 : 2.6) : (after ? 0.5 : 0.8),
                        Samples = 240
                    }
                }
            };
            return new CleanTempCompare.SessionRecord
            {
                Version = "2.0.0",
                Stage = after ? "post" : "pre",
                StageLabel = after ? "清灰后" : "清灰前",
                Mode = gaming ? "gaming" : "idle",
                WorkloadName = gaming ? "赛博朋克 2077 · 固定路线" : "无负载静置",
                CreatedAtUtc = DateTime.UtcNow.AddHours(after ? 0 : -2),
                CpuName = "Intel Core i9-13900H",
                GpuName = "NVIDIA GeForce RTX 4060 Laptop GPU",
                Load = load,
                Idle = idle,
                Recovery = new CleanTempCompare.MetricSummary()
            };
        }

        private static void Invoke(object target, string method, params object[] args)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, args);
        }

        private static void Pump(int seconds)
        {
            DateTime until = DateTime.Now.AddSeconds(seconds);
            while (DateTime.Now < until)
            {
                Application.DoEvents();
                Thread.Sleep(40);
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
