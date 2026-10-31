# 硬件监测中心 v2.3

Windows 笔记本硬件传感器监测与清灰前后量化对比工具。

## 下载

- `HardwareMonitorCenter-v2.3-Windows-x64.zip`
- 完整解压后运行 `HardwareMonitorCenter.exe`
- 首次运行允许管理员权限，并按提示安装 PawnIO

## 功能

- 完整 CPU、GPU、主板、内存、存储、风扇等实时传感器。
- 按判断、传感器类型和设备分类筛选。
- 按名称、当前值、温度和近期波动排序。
- 结合 CPU/GPU 负载显示温度“合适、偏热、过高、危险”。
- 使用最近约 60 秒标准差与峰谷差分析温度波动。
- 点击任意传感器查看介绍、重要性、高低含义和当前建议。
- 记录无负载与游戏两类负载的清灰前后结果。
- 自动比较平均温度、峰值温度、功耗、GPU 负载、风扇和温度波动。
- 导出全部传感器快照、原始 CSV 和完整对比报告。

## 运行要求

- Windows 10/11 x64
- .NET Framework 4.8
- 管理员权限

## 数据与隐私

所有传感器记录仅保存在本机。程序不上传遥测或硬件数据。

## 第三方组件

- LibreHardwareMonitorLib 0.9.6，MPL-2.0。
- PawnIO Setup 2.1.0，由 LibreHardwareMonitor 官方程序内置使用。

详见仓库中的 `NOTICE.md` 和 `THIRD-PARTY-LICENSE-MPL-2.0.txt`。
