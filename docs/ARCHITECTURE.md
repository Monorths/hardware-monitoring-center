# 架构

## 模块

```text
Program
  └─ AdvancedMainForm
       ├─ HardwareSampler
       │    └─ LibreHardwareMonitorLib
       ├─ SensorKnowledge
       ├─ LiveSensorStat / MetricAccumulator
       ├─ MultiSensorChart
       └─ SessionStore
```

## HardwareSampler

- 后台线程每秒读取一次 LibreHardwareMonitor。
- 打开 CPU、GPU、内存、主板、控制器和存储设备。
- 递归更新子硬件。
- 生成标准化 `SensorSnapshot`。
- 统一识别 CPU/GPU 主温度、热点、风扇、负载、功耗和频率。

## 实时 UI

- 采样线程通过 `BeginInvoke` 回到 WinForms UI 线程。
- 实时统计保存每个传感器的运行期间最小/平均/最大值。
- 波动统计使用最近 60 个采样的标准差和峰谷差。
- 表格每两秒重绘一次，避免频繁刷新影响交互。

## 判定

`SensorKnowledge`：

- 根据设备和传感器类型生成介绍。
- 生成重要性和高低含义。
- 根据设备、当前温度和负载生成判断。
- 为温度、风扇、功耗和频率生成建议。

## 测试记录

`MetricAccumulator`：

- 接收测试阶段的每秒样本。
- 聚合核心指标。
- 聚合全部传感器的 Min/Avg/Max/StdDev。
- 序列化为 JSON。

## 保存兼容

`SessionStore`：

- `idle` 和 `gaming` 分开保存。
- 读取新版路径失败时兼容旧版 `standard` 和更早的 `pre_latest`/`post_latest`。

## 构建

- 源码使用 C# 5 兼容语法，目标为 .NET Framework 4.8。
- `scripts/fetch-deps.ps1` 下载并校验第三方依赖。
- `build.ps1` 调用 Windows 自带 C# 编译器生成运行目录。
- GitHub Actions 在 `windows-latest` 上重复构建并上传 ZIP。
