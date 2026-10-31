using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace CleanTempCompare
{
    internal sealed class SensorAssessment
    {
        public string Status;
        public string Meaning;
        public string Advice;
        public Color Color;
        public int Level;
    }

    internal static class SensorKnowledge
    {
        public static SensorAssessment Evaluate(SensorSnapshot snapshot, LiveSensor sensor)
        {
            if (sensor == null)
                return Neutral("未选择", "选择一个传感器查看说明。", "点击传感器列表中的任意一行。");

            if (sensor.Type == SensorType.Temperature)
                return EvaluateTemperature(snapshot, sensor);
            if (sensor.Type == SensorType.Fan)
                return EvaluateFan(snapshot, sensor);
            if (sensor.Type == SensorType.Load)
                return EvaluateLoad(sensor);
            if (sensor.Type == SensorType.Power)
                return EvaluatePower(sensor);
            if (sensor.Type == SensorType.Clock)
                return EvaluateClock(sensor);
            if (sensor.Type == SensorType.Voltage)
                return Neutral("需参考设备", "电压应在主板或处理器供电的允许范围内。", "没有通用安全阈值；异常波动、过高或过低时才需要重点检查。");
            if (sensor.Type == SensorType.Current)
                return Neutral("需参考设备", "电流大小取决于设备功耗和供电设计。", "应结合功耗、温度和稳定性判断，不宜只比较绝对电流。");
            if (sensor.Type == SensorType.Fan)
                return EvaluateFan(snapshot, sensor);
            return Neutral("可读取", "该值属于设备运行状态数据。", "结合同一项的历史变化观察，比单次读数更有意义。");
        }

        public static string Description(LiveSensor sensor)
        {
            if (sensor == null)
                return "点击任意传感器查看详细介绍。";

            string name = (sensor.Name ?? "").ToLowerInvariant();
            if (sensor.Group == "CPU")
            {
                if (sensor.Type == SensorType.Temperature)
                {
                    if (name.Contains("package") || name.Contains("tctl"))
                        return "CPU 封装温度，综合反映处理器的整体发热情况，是判断笔记本 CPU 散热状态的核心指标。";
                    if (name.Contains("max"))
                        return "所有 CPU 核心中的最高温度，用于判断是否有个别核心局部过热。";
                    if (name.Contains("average"))
                        return "所有 CPU 核心的平均温度，适合观察整体热平衡。";
                    return "CPU 内部温度传感器，反映核心或封装当前的热量水平。";
                }
                if (sensor.Type == SensorType.Load)
                    return "CPU 使用率。温度必须结合实际负载理解，高负载时温度上升通常属于正常现象。";
                if (sensor.Type == SensorType.Power)
                    return "CPU 封装功耗。功耗越高通常意味着更多热量产生，同时受电源模式和性能设置影响。";
                if (sensor.Type == SensorType.Clock)
                    return "CPU 当前工作频率。高频通常代表正在处理任务，也可能因温度或功耗限制而降频。";
                if (sensor.Type == SensorType.Voltage)
                    return "CPU 供电电压，由处理器和主板动态调节。";
            }
            if (sensor.Group == "GPU")
            {
                if (sensor.Type == SensorType.Temperature && (name.Contains("hot") || name.Contains("hotspot") || name.Contains("junction")))
                    return "GPU 热点或显存结温，反映芯片内部最热区域，通常比普通 GPU 核心温度更高。";
                if (sensor.Type == SensorType.Temperature && name.Contains("memory"))
                    return "显存温度，重点影响游戏和显存密集型工作负载下的稳定性。";
                if (sensor.Type == SensorType.Temperature)
                    return "GPU 核心温度，反映显卡主要发热区域，是判断笔记本游戏时散热能力的关键指标。";
                if (sensor.Type == SensorType.Load)
                    return "GPU 使用率，表示显卡当前有多少计算资源被利用。";
                if (sensor.Type == SensorType.Power)
                    return "GPU 当前功耗，受显卡型号、TGP 设置和游戏负载影响。";
                if (sensor.Type == SensorType.Clock)
                    return "GPU 工作频率，会随负载、功耗与温度动态变化。";
            }
            if (sensor.Group == "主板")
            {
                if (sensor.Type == SensorType.Fan)
                    return "主板或散热器风扇转速，用于判断风量是否随温度和负载提高。";
                if (sensor.Type == SensorType.Temperature)
                    return "主板、供电区域或机箱内部温度传感器，反映整机内部热环境。";
                if (sensor.Type == SensorType.Voltage)
                    return "主板供电电压，常见于 CPU 供电、内存供电或电池适配器监测。";
            }
            if (sensor.Group == "存储")
            {
                if (sensor.Type == SensorType.Temperature)
                    return "固态硬盘温度。NVMe 硬盘在大量读写时会明显升温，过高可能触发降速。";
                if ((sensor.Name ?? "").ToLowerInvariant().Contains("used space"))
                    return "存储空间使用比例，接近满盘时可能影响写入性能和寿命管理。";
                if (sensor.Type == SensorType.Throughput)
                    return "存储设备读写吞吐量，反映当前磁盘活动强度。";
            }
            if (sensor.Group == "内存")
                return "内存工作状态或温度。内存温度升高通常来自高频率、持续读写或封闭机身热积聚。";
            if (sensor.Type == SensorType.Temperature)
                return "设备的温度传感器，用于观察当前发热水平。";
            if (sensor.Type == SensorType.Fan)
                return "风扇转速。转速变化应与温度和负载趋势一致。";
            if (sensor.Type == SensorType.Load)
                return "设备资源使用率，需要结合当前任务强度判断。";
            if (sensor.Type == SensorType.Power)
                return "设备功耗。不同机型和设置之间不宜直接比较绝对功率。";
            return "由硬件传感器上报的运行数据，用于观察设备状态和趋势。";
        }

        public static string Importance(LiveSensor sensor)
        {
            if (sensor == null)
                return "了解传感器含义后才能正确判断温度数据。";
            if (sensor.Type == SensorType.Temperature)
                return "高：直接影响性能稳定性、降频风险和长期硬件寿命，是清灰前后对比的核心依据。";
            if (sensor.Type == SensorType.Fan)
                return "高：风扇转速与风量决定热量能否排出；但不同机型转速范围差别很大。";
            if (sensor.Type == SensorType.Load)
                return "高：判断温度是否升高必须有负载背景，游戏温度和静置温度不能直接混用。";
            if (sensor.Type == SensorType.Power)
                return "中高：功耗直接影响发热量，也受性能模式和散热限制影响。";
            if (sensor.Type == SensorType.Clock)
                return "中：用于判断处理器或显卡是否活跃、是否因温度或功耗发生降频。";
            if (sensor.Type == SensorType.Voltage || sensor.Type == SensorType.Current)
                return "中：适合检查供电是否稳定，但绝对范围高度依赖具体硬件设计。";
            return "中低：用于补充设备运行状态和趋势观察。";
        }

        public static string HighLowMeaning(SensorType type)
        {
            if (type == SensorType.Temperature)
                return "不是越高越“性能好”：温度过高会触发降频、卡顿或主动关机；过低通常只代表当前负载较轻。";
            if (type == SensorType.Fan)
                return "转速高说明风扇在主动排热，但声音也会变大；0 RPM 或低速在高负载下需要确认是否停转。";
            if (type == SensorType.Load)
                return "高表示设备正在忙碌，短时高负载正常；长期接近 100% 需要检查任务和散热。低表示设备比较空闲。";
            if (type == SensorType.Power)
                return "功耗高通常发热更高，但同时可能代表性能释放更充分；绝对值没有统一优劣，需结合机型和负载。";
            if (type == SensorType.Clock)
                return "频率高通常表示正在加速运行；过热或功耗受限时，频率可能明显下降。";
            if (type == SensorType.Voltage || type == SensorType.Current)
                return "过高或过低都不一定代表好坏，应看是否稳定以及是否处于设备设计范围内。";
            return "结合变化趋势、当前负载和其他关联传感器一起判断。";
        }

        public static SensorAssessment EvaluateOverall(SensorSnapshot snapshot)
        {
            if (snapshot == null || (!snapshot.HasCpu && !snapshot.HasGpu))
                return new SensorAssessment
                {
                    Status = "温度未知",
                    Meaning = "尚未读取到 CPU 或 GPU 温度。",
                    Advice = "请允许管理员权限，并确认 PawnIO 驱动正常。",
                    Color = Theme.Red,
                    Level = 3
                };

            SensorAssessment cpu = null;
            SensorAssessment gpu = null;
            if (snapshot.HasCpu)
            {
                cpu = EvaluateCpuTemperature(snapshot.CpuTemp, snapshot.HasCpuLoad ? snapshot.CpuLoad : 0, false);
            }
            if (snapshot.HasGpu)
            {
                gpu = EvaluateGpuTemperature(snapshot.GpuTemp, snapshot.HasGpuLoad ? snapshot.GpuLoad : 0, false);
            }
            if (snapshot.HasGpuHotspot)
            {
                SensorAssessment hotspot = EvaluateHotspot(snapshot.GpuHotspot);
                if (gpu == null || hotspot.Level > gpu.Level)
                    gpu = hotspot;
            }

            SensorAssessment worst = cpu != null && gpu != null
                ? (cpu.Level >= gpu.Level ? cpu : gpu)
                : (cpu ?? gpu);
            string readings = "";
            if (snapshot.HasCpu)
                readings += "CPU " + snapshot.CpuTemp.ToString("0.0") + "°C";
            if (snapshot.HasGpu)
                readings += (readings.Length > 0 ? " · " : "") + "GPU " + snapshot.GpuTemp.ToString("0.0") + "°C";
            if (snapshot.HasGpuHotspot)
                readings += " · 热点 " + snapshot.GpuHotspot.ToString("0.0") + "°C";
            string load = snapshot.HasCpuLoad || snapshot.HasGpuLoad
                ? "负载 CPU " + (snapshot.HasCpuLoad ? snapshot.CpuLoad.ToString("0") + "%" : "--") +
                  " / GPU " + (snapshot.HasGpuLoad ? snapshot.GpuLoad.ToString("0") + "%" : "--")
                : "负载数据不可用";
            worst.Meaning = readings + " · " + load;
            return worst;
        }

        private static SensorAssessment EvaluateTemperature(SensorSnapshot snapshot, LiveSensor sensor)
        {
            string name = (sensor.Name ?? "").ToLowerInvariant();
            if (sensor.Group == "CPU")
                return EvaluateCpuTemperature(sensor.Value, snapshot.HasCpuLoad ? snapshot.CpuLoad : 0, false);
            if (sensor.Group == "GPU")
            {
                if (name.Contains("hot") || name.Contains("hotspot") || name.Contains("junction"))
                    return EvaluateHotspot(sensor.Value);
                if (name.Contains("memory"))
                    return EvaluateGpuTemperature(sensor.Value, snapshot.HasGpuLoad ? snapshot.GpuLoad : 0, true);
                return EvaluateGpuTemperature(sensor.Value, snapshot.HasGpuLoad ? snapshot.GpuLoad : 0, false);
            }
            if (sensor.Group == "存储")
            {
                if (sensor.Value >= 80)
                    return Danger("过高", "固态硬盘已达到可能触发降速的温度。", "暂停大文件读写，确认硬盘散热片或机身风道。");
                if (sensor.Value >= 70)
                    return Warn("偏高", "固态硬盘在持续高速读写时温度较高。", "避免长时间大文件拷贝，观察降速或温度回落。");
                if (sensor.Value >= 55)
                    return Mild("略高", "硬盘温度高于静置常见范围，但仍可能正常。", "结束大量读写后观察是否降低。");
                return Good("合适", "固态硬盘温度处于常见正常范围。", "保持风道通畅即可。");
            }
            if (sensor.Group == "内存")
            {
                if (sensor.Value >= 85)
                    return Danger("过高", "内存温度较高，可能影响高频稳定性。", "降低内存超频，检查机身底部散热。");
                if (sensor.Value >= 72)
                    return Warn("偏高", "内存温度较热。", "关注长时间游戏或高负载下的稳定性。");
                return Good("合适", "内存温度处于常见正常范围。", "无需额外处理。");
            }
            if (sensor.Value >= 80)
                return Danger("过高", "设备内部温度较高。", "检查进风口、出风口和风扇是否正常。");
            if (sensor.Value >= 68)
                return Warn("偏高", "设备内部温度高于静置常见范围。", "确认是否有高负载程序在运行。");
            if (sensor.Value >= 55)
                return Mild("略高", "温度处于中等水平，结合负载判断。", "观察变化趋势即可。");
            return Good("合适", "温度处于常见正常范围。", "保持通风并观察趋势。");
        }

        private static SensorAssessment EvaluateCpuTemperature(double temperature, double load, bool secondary)
        {
            bool loaded = load >= 20;
            if (loaded)
            {
                if (temperature >= 100)
                    return Danger("危险", "CPU 温度已接近高温保护区间。", "立即停止压力任务，检查风扇和散热器。");
                if (temperature >= 90)
                    return Danger("过高", "CPU 在高负载下温度过高，可能出现降频。", "降低性能模式，检查出风口和风扇转速。");
                if (temperature >= 80)
                    return Warn("偏热", "CPU 正在负载运行，温度偏高但部分游戏本可能出现。", "确认风扇转速提高；若持续不降需检查散热。");
                return Good("合适", "CPU 在负载下温度较低。", "无需处理。");
            }
            if (temperature >= 90)
                return Danger("过高", "低负载下 CPU 仍然很热。", "关闭后台任务，检查风扇、硅脂或散热器安装。");
            if (temperature >= 75)
                return Warn("偏高", "无负载时 CPU 温度偏高。", "确认没有隐藏高负载，并检查进风口。");
            if (temperature >= 60)
                return Mild("偏热", "静置 CPU 温度稍高，可能由环境温度或电源模式造成。", "等待几分钟后观察是否下降。");
            return Good("合适", "无负载 CPU 温度处于常见静置范围。", "保持通风即可。");
        }

        private static SensorAssessment EvaluateGpuTemperature(double temperature, double load, bool memory)
        {
            bool loaded = load >= 20;
            if (memory)
            {
                if (temperature >= 100)
                    return Danger("过高", "显存温度较高，可能影响稳定性和寿命。", "降低画质或停止游戏，加强显存散热。");
                if (temperature >= 90)
                    return Warn("偏热", "显存温度偏高。", "观察是否出现花屏、报错或降频。");
                return Good("合适", "显存温度在常见范围内。", "保持通风。");
            }
            if (loaded)
            {
                if (temperature >= 95)
                    return Danger("过高", "游戏负载下 GPU 温度过高。", "降低画质、性能模式或停止游戏并检查散热。");
                if (temperature >= 86)
                    return Warn("偏热", "游戏负载下 GPU 温度偏高。", "确认风扇正常并抬高笔记本后部以改善进风。");
                return Good("合适", "游戏负载下 GPU 温度较低。", "无需处理。");
            }
            if (temperature >= 85)
                return Danger("过高", "无负载时 GPU 温度仍较高。", "确认没有后台 GPU 任务，并检查风扇和待机温度。");
            if (temperature >= 72)
                return Warn("偏高", "静置 GPU 温度偏高。", "检查后台应用、外接显示器和风扇状态。");
            if (temperature >= 60)
                return Mild("偏热", "GPU 静置温度稍高，可能与环境温度有关。", "等待几分钟观察是否下降。");
            return Good("合适", "GPU 静置温度处于常见范围。", "保持通风即可。");
        }

        private static SensorAssessment EvaluateHotspot(double temperature)
        {
            if (temperature >= 110)
                return Danger("危险", "GPU 热点温度极高。", "立即停止高负载，检查风扇和散热模组。");
            if (temperature >= 100)
                return Danger("过高", "GPU 热点温度过高。", "降低画质或停止游戏，检查散热。");
            if (temperature >= 90)
                return Warn("偏热", "GPU 热点较高，热点通常高于核心温度。", "观察核心与热点差值是否持续扩大。");
            return Good("合适", "GPU 热点温度在常见范围。", "继续观察趋势。");
        }

        private static SensorAssessment EvaluateFan(SensorSnapshot snapshot, LiveSensor sensor)
        {
            bool thermalPressure =
                (snapshot.HasCpu && snapshot.CpuTemp >= 85) ||
                (snapshot.HasGpu && snapshot.GpuTemp >= 82);
            if (sensor.Value <= 0)
            {
                return thermalPressure
                    ? Danger("未检测到转速", "温度较高但风扇没有转速或传感器未上报。", "检查风扇是否停转、堵转或被系统设为被动散热。")
                    : Neutral("停止/未上报", "当前转速为 0，低负载或被动散热时可能正常。", "若温度升高后仍为 0，检查风扇和线缆。");
            }
            if (thermalPressure && sensor.Value < 800)
                return Warn("风速偏低", "当前温度较高，但风扇转速相对较低。", "检查风扇曲线、性能模式或风扇是否被遮挡。");
            if (sensor.Value >= 5000)
                return Mild("高速", "风扇正在高速排热，噪音增加通常正常。", "确认温度是否随转速提高而下降。");
            return Good("运行中", "风扇正在提供主动散热。", "不同机型转速范围不同，重点看随负载变化。");
        }

        private static SensorAssessment EvaluateLoad(LiveSensor sensor)
        {
            if (sensor.Value >= 95)
                return Mild("高负载", "设备资源接近满载，温度和功耗上升属于预期现象。", "确认温度和风扇能保持稳定。");
            if (sensor.Value >= 40)
                return Good("有负载", "设备正在执行中等或较高强度任务。", "与温度一起判断是否正常。");
            if (sensor.Value > 15)
                return Good("低负载", "设备处于轻度工作状态。", "可直接参考静置温度。");
            return Good("空闲", "当前资源使用率很低。", "适合作为无负载温度参考。");
        }

        private static SensorAssessment EvaluatePower(LiveSensor sensor)
        {
            if (sensor.Value <= 1)
                return Neutral("待机/低功耗", "当前功耗很低，可能处于待机或节能状态。", "与负载和温度一起观察。");
            return Neutral("有功耗", "功耗会随任务强度变化，绝对值不能在不同笔记本之间直接比较。", "重点观察相同游戏负载下的功耗和温度变化。");
        }

        private static SensorAssessment EvaluateClock(LiveSensor sensor)
        {
            if (sensor.Value <= 10)
                return Neutral("低频/休眠", "当前频率较低，设备可能正在空闲或节能。", "结合负载判断是否为正常降频。");
            return Good("运行中", "设备正在以当前频率工作。", "若高负载时频率明显下降，需结合温度判断是否降频。");
        }

        private static SensorAssessment Good(string status, string meaning, string advice)
        {
            return New(status, meaning, advice, Theme.Green, 0);
        }

        private static SensorAssessment Neutral(string status, string meaning, string advice)
        {
            return New(status, meaning, advice, Theme.Accent, 1);
        }

        private static SensorAssessment Mild(string status, string meaning, string advice)
        {
            return New(status, meaning, advice, Theme.Yellow, 2);
        }

        private static SensorAssessment Warn(string status, string meaning, string advice)
        {
            return New(status, meaning, advice, Theme.Orange, 3);
        }

        private static SensorAssessment Danger(string status, string meaning, string advice)
        {
            return New(status, meaning, advice, Theme.Red, 4);
        }

        private static SensorAssessment New(string status, string meaning, string advice, Color color, int level)
        {
            return new SensorAssessment
            {
                Status = status,
                Meaning = meaning,
                Advice = advice,
                Color = color,
                Level = level
            };
        }
    }
}
