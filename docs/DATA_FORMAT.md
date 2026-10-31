# 数据格式

## 1. 目录

程序优先写入：

```text
<app>\Reports
```

不可写时使用：

```text
%LOCALAPPDATA%\CleanTempCompare\Reports
```

## 2. 最新会话文件

```text
pre_idle_latest.json
post_idle_latest.json
pre_gaming_latest.json
post_gaming_latest.json
```

## 3. 历史会话

```text
pre_idle_YYYYMMDD_HHMMSS.json
post_gaming_YYYYMMDD_HHMMSS.json
```

## 4. SessionRecord

主要字段：

| 字段 | 含义 |
| --- | --- |
| `Version` | 数据或程序版本 |
| `Stage` | `pre` 或 `post` |
| `StageLabel` | 清灰前/清灰后 |
| `Mode` | `idle` 或 `gaming` |
| `WorkloadName` | 无负载静置或游戏场景名称 |
| `CreatedAtUtc` | UTC 记录时间 |
| `CpuName` / `GpuName` | 硬件名称 |
| `Idle` | 无负载阶段统计 |
| `Load` | 游戏负载阶段统计 |
| `Recovery` | 兼容旧记录的恢复阶段 |

## 5. MetricSummary

核心统计：

| 字段 | 含义 |
| --- | --- |
| `AvgCpu` / `MaxCpu` | CPU 平均/最高温度 |
| `AvgGpu` / `MaxGpu` | GPU 平均/最高温度 |
| `AvgCpuLoad` / `AvgGpuLoad` | 平均负载 |
| `AvgCpuPower` / `AvgGpuPower` | 平均功耗 |
| `MaxFan` | 最高风扇 RPM |
| `SensorStats` | 该阶段全部传感器统计 |

## 6. SensorStat

| 字段 | 含义 |
| --- | --- |
| `Identifier` | 设备、类型和名称组成的稳定标识 |
| `Hardware` / `Group` | 硬件名称和分组 |
| `Name` / `Type` / `Unit` | 传感器名称、类型和单位 |
| `Min` / `Avg` / `Max` | 阶段最小值、平均值和最大值 |
| `StdDev` | 阶段标准差 |
| `Samples` | 有效样本数 |

## 7. 原始 CSV

测试期间每秒写入：

```text
时间,阶段,CPU温度,GPU温度,GPU热点,CPU负载,GPU负载,CPU功耗,GPU功耗,风扇RPM
```

文件使用 UTF-8 BOM，便于 Windows Excel 直接打开。

## 8. 隐私

JSON 和 CSV 可能包含 CPU、GPU、主板名称和测试时间。公开分享记录前请先检查并删除不希望公开的硬件或时间信息。
