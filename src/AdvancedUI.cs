using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace CleanTempCompare
{
    internal sealed class AdvancedMainForm : Form
    {
        private readonly HardwareSampler _sampler = new HardwareSampler();
        private readonly CpuLoadGenerator _loadGenerator = new CpuLoadGenerator();
        private readonly System.Windows.Forms.Timer _testTimer = new System.Windows.Forms.Timer();
        private readonly Dictionary<string, LiveSensorStat> _liveStats =
            new Dictionary<string, LiveSensorStat>();
        private readonly string[] _standardPhaseNames = { "无负载静置" };
        private readonly int[] _standardPhaseSeconds = { 300 };

        private Panel _pageHost;
        private Panel _overviewPage;
        private Panel _sensorsPage;
        private Panel _comparePage;
        private AccentButton _overviewNav;
        private AccentButton _sensorsNav;
        private AccentButton _compareNav;

        private MetricCard _cpuCard;
        private MetricCard _gpuCard;
        private MetricCard _cpuLoadCard;
        private MetricCard _gpuLoadCard;
        private MultiSensorChart _trendChart;
        private ComboBox _primaryChartCombo;
        private ComboBox _secondaryChartCombo;
        private Label[] _keyValueLabels;
        private CardPanel _thermalStatusCard;
        private Label _thermalStatusTitle;
        private Label _thermalStatusDetail;
        private DataGridView _previewGrid;
        private DataGridView _sensorGrid;
        private TextBox _sensorSearch;
        private ComboBox _typeFilter;
        private ComboBox _groupFilter;
        private ComboBox _statusFilter;
        private ComboBox _sortCombo;
        private Label _sensorCountLabel;
        private Label _sensorDetailTitle;
        private Label _sensorDetailStatus;
        private Label _sensorDetailDescription;
        private Label _sensorDetailImportance;
        private Label _sensorDetailMeaning;
        private Label _sensorDetailFluctuation;
        private Label _sensorDetailAdvice;
        private string _selectedSensorId;
        private bool _updatingSensorGrid;
        private Label _statusLabel;
        private Label _cpuInfoLabel;
        private Label _gpuInfoLabel;
        private string _chartFingerprint = "";
        private string _statusFingerprint = "";
        private bool _updatingStatusFilter;
        private SensorSnapshot _latest;
        private int _gridTick;

        private RadioButton _beforeRadio;
        private RadioButton _afterRadio;
        private RadioButton _standardRadio;
        private RadioButton _gamingRadio;
        private TextBox _workloadText;
        private ComboBox _gameDurationCombo;
        private Label _testInstructionLabel;
        private Label _phaseLabel;
        private Label _timeLabel;
        private Label _standardBeforeHistory;
        private Label _standardAfterHistory;
        private Label _gamingBeforeHistory;
        private Label _gamingAfterHistory;
        private Panel _progressTrack;
        private Panel _progressFill;
        private AccentButton _startButton;
        private AccentButton _exportButton;
        private Label _compareHeadline;
        private Label _compareExplanation;
        private DataGridView _comparisonGrid;

        private SessionRecord _standardBefore;
        private SessionRecord _standardAfter;
        private SessionRecord _gamingBefore;
        private SessionRecord _gamingAfter;
        private bool _testActive;
        private TestMode _activeMode;
        private MeasurementStage _activeStage;
        private int _activePhase;
        private DateTime _testStarted;
        private DateTime _phaseEnds;
        private DateTime _testEnds;
        private int _activeTotalSeconds;
        private string _activeWorkloadName;
        private MetricAccumulator _idleAccumulator;
        private MetricAccumulator _loadAccumulator;
        private MetricAccumulator _recoveryAccumulator;
        private StreamWriter _rawCsv;
        private string _rawCsvPath;
        private bool _sensorReady;
        private bool _closingAfterPrompt;

        public AdvancedMainForm()
        {
            Text = "硬件监测中心 · 清灰温度对比";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1320, 850);
            MinimumSize = new Size(1120, 740);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Font(9f, FontStyle.Regular);
            DoubleBuffered = true;

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Theme.Background,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildNavigation(), 0, 1);

            _pageHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(22, 12, 22, 20)
            };
            root.Controls.Add(_pageHost, 0, 2);

            _overviewPage = BuildOverviewPage();
            _sensorsPage = BuildSensorsPage();
            _comparePage = BuildComparePage();
            _pageHost.Controls.Add(_overviewPage);
            _pageHost.Controls.Add(_sensorsPage);
            _pageHost.Controls.Add(_comparePage);
            ShowPage(0);

            _testTimer.Interval = 250;
            _testTimer.Tick += TestTimer_Tick;
            _sampler.Sampled += Sampler_Sampled;
            _sampler.SamplingError += Sampler_SamplingError;
            Shown += MainForm_Shown;
            FormClosing += MainForm_FormClosing;
            FormClosed += delegate { _sampler.Dispose(); };
            ApplyTestSelection();
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Header,
                Margin = new Padding(0),
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
            Label title = MakeLabel("硬件监测中心", 18f, FontStyle.Bold, Theme.Text);
            title.Location = new Point(81, 13);
            Label subtitle = MakeLabel("实时传感器 · 无负载 / 游戏负载 · 清灰前后量化对比",
                9f, FontStyle.Regular, Theme.Muted);
            subtitle.Location = new Point(83, 49);
            header.Controls.Add(mark);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            _statusLabel = MakeLabel("正在连接硬件传感器…", 8.5f, FontStyle.Regular, Theme.Muted);
            _statusLabel.AutoSize = false;
            _statusLabel.TextAlign = ContentAlignment.MiddleRight;
            _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _statusLabel.Location = new Point(930, 11);
            _statusLabel.Size = new Size(345, 24);
            _statusLabel.Padding = new Padding(0, 0, 2, 0);
            header.Controls.Add(_statusLabel);

            Panel systemInfo = new Panel
            {
                BackColor = Color.Transparent,
                Location = new Point(505, 51),
                Size = new Size(420, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _cpuInfoLabel = MakeLabel("CPU：正在识别…", 7.9f, FontStyle.Regular, Theme.Dim);
            _cpuInfoLabel.AutoSize = false;
            _cpuInfoLabel.Width = 205;
            _cpuInfoLabel.Dock = DockStyle.Left;
            _gpuInfoLabel = MakeLabel("GPU：正在识别…", 7.9f, FontStyle.Regular, Theme.Dim);
            _gpuInfoLabel.AutoSize = false;
            _gpuInfoLabel.Width = 210;
            _gpuInfoLabel.Dock = DockStyle.Left;
            systemInfo.Controls.Add(_gpuInfoLabel);
            systemInfo.Controls.Add(_cpuInfoLabel);
            header.Controls.Add(systemInfo);

            Panel divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Theme.Border };
            header.Controls.Add(divider);
            return header;
        }

        private Panel BuildNavigation()
        {
            Panel nav = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(22, 5, 22, 5)
            };
            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 410,
                BackColor = Color.Transparent,
                WrapContents = false,
                Margin = new Padding(0)
            };
            _overviewNav = CreateNavButton("总览");
            _sensorsNav = CreateNavButton("全部传感器");
            _compareNav = CreateNavButton("清灰对比");
            _overviewNav.Click += delegate { ShowPage(0); };
            _sensorsNav.Click += delegate { ShowPage(1); };
            _compareNav.Click += delegate { ShowPage(2); };
            buttons.Controls.Add(_overviewNav);
            buttons.Controls.Add(_sensorsNav);
            buttons.Controls.Add(_compareNav);

            Label hint = MakeLabel("数据仅保存在本机 · 支持 CSV / JSON 原始记录", 8f,
                FontStyle.Regular, Theme.Dim);
            hint.AutoSize = false;
            hint.TextAlign = ContentAlignment.MiddleRight;
            hint.Dock = DockStyle.Right;
            hint.Width = 390;
            nav.Controls.Add(hint);
            nav.Controls.Add(buttons);
            return nav;
        }

        private AccentButton CreateNavButton(string text)
        {
            return new AccentButton
            {
                Text = text,
                Width = 124,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0),
                NormalColor = Theme.CardAlt,
                HoverColor = Color.FromArgb(34, 48, 62),
                ForeColor = Theme.Muted
            };
        }

        private void ShowPage(int index)
        {
            if (_pageHost == null)
                return;
            _overviewPage.Visible = index == 0;
            _sensorsPage.Visible = index == 1;
            _comparePage.Visible = index == 2;
            StyleNav(_overviewNav, index == 0);
            StyleNav(_sensorsNav, index == 1);
            StyleNav(_compareNav, index == 2);
            if (index == 1 && _latest != null)
                UpdateSensorTables();
            if (index == 2)
                RefreshComparison();
        }

        private static void StyleNav(AccentButton button, bool selected)
        {
            button.NormalColor = selected ? Theme.AccentDark : Theme.CardAlt;
            button.HoverColor = selected ? Color.FromArgb(20, 129, 145) : Color.FromArgb(34, 48, 62);
            button.ForeColor = selected ? Theme.Text : Theme.Muted;
            button.Invalidate();
        }

        private Panel BuildOverviewPage()
        {
            Panel page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Theme.Background,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 114f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 294f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            page.Controls.Add(layout);

            TableLayoutPanel cards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Theme.Background,
                Margin = new Padding(0)
            };
            for (int i = 0; i < 4; i++)
                cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            _cpuCard = CardMetric("CPU 温度", "--", "等待传感器", Theme.Accent);
            _gpuCard = CardMetric("GPU 温度", "--", "等待传感器", Theme.Orange);
            _cpuLoadCard = CardMetric("CPU 负载", "--", "功耗 / 频率", Theme.Green);
            _gpuLoadCard = CardMetric("GPU 负载", "--", "功耗 / 频率", Theme.Yellow);
            cards.Controls.Add(_cpuCard, 0, 0);
            cards.Controls.Add(_gpuCard, 1, 0);
            cards.Controls.Add(_cpuLoadCard, 2, 0);
            cards.Controls.Add(_gpuLoadCard, 3, 0);
            layout.Controls.Add(cards, 0, 0);
            layout.Controls.Add(BuildThermalStatusCard(), 0, 1);

            TableLayoutPanel analysis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Background,
                Margin = new Padding(0, 0, 0, 10)
            };
            analysis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 71f));
            analysis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29f));
            analysis.Controls.Add(BuildTrendCard(), 0, 0);
            analysis.Controls.Add(BuildKeyMetricsCard(), 1, 0);
            layout.Controls.Add(analysis, 0, 2);
            layout.Controls.Add(BuildPreviewSensorsCard(), 0, 3);
            return page;
        }

        private CardPanel BuildThermalStatusCard()
        {
            _thermalStatusCard = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(16, 8, 16, 7),
                CardColor = Theme.CardAlt
            };
            _thermalStatusTitle = MakeLabel("当前温度：正在判断…", 10.5f, FontStyle.Bold, Theme.Muted);
            _thermalStatusTitle.Dock = DockStyle.Left;
            _thermalStatusTitle.AutoSize = false;
            _thermalStatusTitle.Width = 210;
            _thermalStatusTitle.TextAlign = ContentAlignment.MiddleLeft;
            _thermalStatusDetail = MakeLabel("读取 CPU、GPU 温度和当前负载后给出建议。", 8f,
                FontStyle.Regular, Theme.Dim);
            _thermalStatusDetail.AutoSize = false;
            _thermalStatusDetail.Dock = DockStyle.Fill;
            _thermalStatusDetail.TextAlign = ContentAlignment.MiddleLeft;
            _thermalStatusDetail.Padding = new Padding(8, 0, 0, 0);
            TableLayoutPanel inner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            inner.Controls.Add(_thermalStatusTitle, 0, 0);
            inner.Controls.Add(_thermalStatusDetail, 1, 0);
            _thermalStatusCard.Controls.Add(inner);
            return _thermalStatusCard;
        }

        private MetricCard CardMetric(string title, string value, string detail, Color color)
        {
            return new MetricCard(title, value, detail, color)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 0)
            };
        }

        private CardPanel BuildTrendCard()
        {
            CardPanel card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(14) };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(layout);

            Panel header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label title = MakeLabel("实时趋势 · 最近 5 分钟", 10f, FontStyle.Bold, Theme.Text);
            title.Location = new Point(2, 4);
            header.Controls.Add(title);

            _secondaryChartCombo = MakeCombo(150);
            _secondaryChartCombo.Dock = DockStyle.Right;
            _secondaryChartCombo.SelectedIndexChanged += delegate { ApplyChartSelections(); };
            _primaryChartCombo = MakeCombo(150);
            _primaryChartCombo.Dock = DockStyle.Right;
            _primaryChartCombo.SelectedIndexChanged += delegate { ApplyChartSelections(); };
            Label second = MakeLabel("副曲线", 7.5f, FontStyle.Regular, Theme.Dim);
            second.AutoSize = false;
            second.Width = 48;
            second.TextAlign = ContentAlignment.MiddleCenter;
            second.Dock = DockStyle.Right;
            Label first = MakeLabel("主曲线", 7.5f, FontStyle.Regular, Theme.Dim);
            first.AutoSize = false;
            first.Width = 48;
            first.TextAlign = ContentAlignment.MiddleCenter;
            first.Dock = DockStyle.Right;
            header.Controls.Add(_secondaryChartCombo);
            header.Controls.Add(second);
            header.Controls.Add(_primaryChartCombo);
            header.Controls.Add(first);
            layout.Controls.Add(header, 0, 0);

            _trendChart = new MultiSensorChart { Dock = DockStyle.Fill, Margin = new Padding(0) };
            layout.Controls.Add(_trendChart, 0, 1);
            return card;
        }

        private CardPanel BuildKeyMetricsCard()
        {
            CardPanel card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Padding = new Padding(15) };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            for (int i = 0; i < 7; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 7f));
            card.Controls.Add(layout);

            Label title = MakeLabel("关键遥测", 10f, FontStyle.Bold, Theme.Text);
            title.Dock = DockStyle.Fill;
            layout.SetColumnSpan(title, 2);
            layout.Controls.Add(title, 0, 0);
            string[] names = { "CPU 功耗", "GPU 功耗", "CPU 频率", "GPU 频率", "GPU 热点", "风扇转速", "最高温度" };
            _keyValueLabels = new Label[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                Label name = MakeLabel(names[i], 8f, FontStyle.Regular, Theme.Muted);
                name.Dock = DockStyle.Fill;
                name.TextAlign = ContentAlignment.MiddleLeft;
                Label value = MakeLabel("--", 8.4f, FontStyle.Bold, Theme.Text);
                value.Dock = DockStyle.Fill;
                value.TextAlign = ContentAlignment.MiddleRight;
                layout.Controls.Add(name, 0, i + 1);
                layout.Controls.Add(value, 1, i + 1);
                _keyValueLabels[i] = value;
            }
            return card;
        }

        private CardPanel BuildPreviewSensorsCard()
        {
            CardPanel card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14) };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(layout);

            Panel header = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label title = MakeLabel("核心传感器速览", 10f, FontStyle.Bold, Theme.Text);
            title.Location = new Point(2, 3);
            Label note = MakeLabel("在“全部传感器”中可搜索、筛选并导出完整数据", 7.8f,
                FontStyle.Regular, Theme.Dim);
            note.AutoSize = false;
            note.TextAlign = ContentAlignment.MiddleRight;
            note.Dock = DockStyle.Right;
            note.Width = 300;
            header.Controls.Add(title);
            header.Controls.Add(note);
            layout.Controls.Add(header, 0, 0);

            _previewGrid = CreateSensorGrid(false);
            layout.Controls.Add(_previewGrid, 0, 1);
            return card;
        }

        private Panel BuildSensorsPage()
        {
            Panel page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.Background,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            page.Controls.Add(layout);

            CardPanel filter = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(12) };
            FlowLayoutPanel row = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            _sensorSearch = new TextBox
            {
                Width = 180,
                Height = 30,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Font = Theme.Font(8.6f, FontStyle.Regular),
                Margin = new Padding(0, 2, 10, 0)
            };
            _sensorSearch.TextChanged += delegate { UpdateSensorTables(); };
            _typeFilter = MakeCombo(95);
            _typeFilter.Items.AddRange(new object[] { "全部类型", "温度", "风扇", "负载", "功耗", "频率", "电压", "电流", "其他" });
            _typeFilter.SelectedIndex = 0;
            _typeFilter.SelectedIndexChanged += delegate { UpdateSensorTables(); };
            _groupFilter = MakeCombo(95);
            _groupFilter.Items.AddRange(new object[] { "全部设备", "CPU", "GPU", "主板", "存储", "内存", "散热器", "其他" });
            _groupFilter.SelectedIndex = 0;
            _groupFilter.SelectedIndexChanged += delegate { UpdateSensorTables(); };
            _statusFilter = MakeCombo(120);
            _statusFilter.Items.Add("全部判断");
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += delegate { UpdateSensorTables(); };
            _sortCombo = MakeCombo(165);
            _sortCombo.Items.AddRange(new object[]
            {
                "默认：设备分类",
                "传感器名称 A–Z",
                "当前值：高 → 低",
                "当前值：低 → 高",
                "温度：高 → 低",
                "温度：低 → 高",
                "温度波动：高 → 低",
                "温度波动：低 → 高"
            });
            _sortCombo.SelectedIndex = 0;
            _sortCombo.SelectedIndexChanged += delegate { UpdateSensorTables(); };
            _sensorCountLabel = MakeLabel("0 个传感器", 8f, FontStyle.Regular, Theme.Dim);
            _sensorCountLabel.AutoSize = false;
            _sensorCountLabel.Width = 82;
            _sensorCountLabel.TextAlign = ContentAlignment.MiddleCenter;
            AccentButton export = new AccentButton
            {
                Text = "导出当前传感器",
                Width = 140,
                Height = 32,
                Font = Theme.Font(8.6f, FontStyle.Bold),
                Margin = new Padding(8, 0, 0, 0)
            };
            export.Click += delegate { ExportCurrentSensors(); };
            row.Controls.Add(_sensorSearch);
            row.Controls.Add(_typeFilter);
            row.Controls.Add(_groupFilter);
            row.Controls.Add(_statusFilter);
            row.Controls.Add(_sortCombo);
            row.Controls.Add(_sensorCountLabel);
            row.Controls.Add(export);
            filter.Controls.Add(row);
            layout.Controls.Add(filter, 0, 0);

            CardPanel gridCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14) };
            TableLayoutPanel gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
            _sensorGrid = CreateSensorGrid(true);
            _sensorGrid.SelectionChanged += delegate { ShowSelectedSensorDetails(); };
            gridLayout.Controls.Add(_sensorGrid, 0, 0);
            gridLayout.Controls.Add(BuildSensorDetailPanel(), 0, 1);
            gridCard.Controls.Add(gridLayout);
            layout.Controls.Add(gridCard, 0, 1);
            return page;
        }

        private Panel BuildSensorDetailPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Input,
                Margin = new Padding(0, 8, 0, 0),
                Padding = new Padding(12, 8, 12, 6)
            };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            panel.Controls.Add(layout);

            Panel titleRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _sensorDetailTitle = MakeLabel("点击一个传感器查看详细说明", 9.7f, FontStyle.Bold, Theme.Text);
            _sensorDetailTitle.Dock = DockStyle.Left;
            _sensorDetailTitle.AutoSize = false;
            _sensorDetailTitle.Width = 610;
            _sensorDetailTitle.TextAlign = ContentAlignment.MiddleLeft;
            _sensorDetailStatus = MakeLabel("", 9.5f, FontStyle.Bold, Theme.Muted);
            _sensorDetailStatus.Dock = DockStyle.Right;
            _sensorDetailStatus.AutoSize = false;
            _sensorDetailStatus.Width = 300;
            _sensorDetailStatus.TextAlign = ContentAlignment.MiddleRight;
            titleRow.Controls.Add(_sensorDetailTitle);
            titleRow.Controls.Add(_sensorDetailStatus);
            layout.Controls.Add(titleRow, 0, 0);

            _sensorDetailDescription = MakeDetailLabel("介绍：选择传感器后会解释它测量什么。");
            _sensorDetailImportance = MakeDetailLabel("重要性：帮助判断清灰前后哪类数据最有参考价值。");
            _sensorDetailMeaning = MakeDetailLabel("高低说明：不同硬件的安全范围不同。");
            _sensorDetailFluctuation = MakeDetailLabel("温度波动：根据最近约 60 秒数据计算标准差。");
            _sensorDetailAdvice = MakeDetailLabel("建议：先看温度和负载，再结合风扇与功耗。");
            layout.Controls.Add(_sensorDetailDescription, 0, 1);
            layout.Controls.Add(_sensorDetailImportance, 0, 2);
            layout.Controls.Add(_sensorDetailMeaning, 0, 3);
            layout.Controls.Add(_sensorDetailFluctuation, 0, 4);
            layout.Controls.Add(_sensorDetailAdvice, 0, 5);
            return panel;
        }

        private static Label MakeDetailLabel(string text)
        {
            Label label = MakeLabel(text, 7.8f, FontStyle.Regular, Theme.Muted);
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private DataGridView CreateSensorGrid(bool full)
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
                RowTemplate = { Height = 27 },
                ColumnHeadersHeight = 30,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                Font = Theme.Font(8f, FontStyle.Regular),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.DefaultCellStyle.BackColor = Theme.Card;
            grid.DefaultCellStyle.ForeColor = Theme.Text;
            grid.DefaultCellStyle.SelectionBackColor = Theme.CardAlt;
            grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Card;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Muted;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Card;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.Font(7.7f, FontStyle.Bold);
            grid.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            grid.AdvancedCellBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;
            AddGridColumn(grid, "type", "类型", 10);
            AddGridColumn(grid, "sensor", "传感器", full ? 25 : 32);
            AddGridColumn(grid, "value", "当前", 13);
            AddGridColumn(grid, "status", "判断", full ? 13 : 14);
            AddGridColumn(grid, "fluctuation", "波动", full ? 10 : 12);
            if (full)
            {
                AddGridColumn(grid, "min", "最小", 9);
                AddGridColumn(grid, "avg", "平均", 9);
                AddGridColumn(grid, "max", "最大", 9);
            }
            AddGridColumn(grid, "device", "设备", full ? 19 : 28);
            return grid;
        }

        private static void AddGridColumn(DataGridView grid, string name, string header, int weight)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private Panel BuildComparePage()
        {
            Panel page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Background,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            page.Controls.Add(layout);
            layout.Controls.Add(BuildTestControlCard(), 0, 0);
            layout.Controls.Add(BuildComparisonCard(), 1, 0);
            return page;
        }

        private CardPanel BuildTestControlCard()
        {
            CardPanel card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(17)
            };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 11,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            card.Controls.Add(layout);

            Label title = MakeLabel("开始对比记录", 10.5f, FontStyle.Bold, Theme.Text);
            title.Dock = DockStyle.Fill;
            layout.Controls.Add(title, 0, 0);
            _testInstructionLabel = MakeLabel(
                "先选清灰阶段与负载类型。清灰前后必须保持相同的测试方式。",
                8f, FontStyle.Regular, Theme.Muted);
            _testInstructionLabel.AutoSize = false;
            _testInstructionLabel.Dock = DockStyle.Fill;
            _testInstructionLabel.Padding = new Padding(0, 2, 0, 4);
            layout.Controls.Add(_testInstructionLabel, 0, 1);

            TableLayoutPanel stageRow = SegmentRow("清灰阶段", out _beforeRadio, "清灰前", out _afterRadio, "清灰后");
            _beforeRadio.CheckedChanged += delegate { ApplyTestSelection(); };
            _afterRadio.CheckedChanged += delegate { ApplyTestSelection(); };
            layout.Controls.Add(stageRow, 0, 2);

            TableLayoutPanel modeRow = SegmentRow("负载类型", out _standardRadio, "无负载", out _gamingRadio, "游戏");
            _standardRadio.CheckedChanged += delegate { ApplyTestSelection(); };
            _gamingRadio.CheckedChanged += delegate { ApplyTestSelection(); };
            layout.Controls.Add(modeRow, 0, 3);

            Panel workloadPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label workloadLabel = MakeLabel("负载名称", 8f, FontStyle.Regular, Theme.Muted);
            workloadLabel.AutoSize = false;
            workloadLabel.Dock = DockStyle.Left;
            workloadLabel.Width = 68;
            workloadLabel.TextAlign = ContentAlignment.MiddleLeft;
            _workloadText = new TextBox
            {
                Text = "游戏名称 / 场景",
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Font = Theme.Font(8.5f, FontStyle.Regular)
            };
            workloadPanel.Controls.Add(_workloadText);
            workloadPanel.Controls.Add(workloadLabel);
            layout.Controls.Add(workloadPanel, 0, 4);

            Panel durationPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label durationLabel = MakeLabel("记录时长", 8f, FontStyle.Regular, Theme.Muted);
            durationLabel.AutoSize = false;
            durationLabel.Dock = DockStyle.Left;
            durationLabel.Width = 68;
            durationLabel.TextAlign = ContentAlignment.MiddleLeft;
            _gameDurationCombo = MakeCombo(140);
            _gameDurationCombo.Items.AddRange(new object[] { "5 分钟", "10 分钟", "15 分钟", "20 分钟" });
            _gameDurationCombo.SelectedIndex = 1;
            _gameDurationCombo.Dock = DockStyle.Left;
            durationPanel.Controls.Add(_gameDurationCombo);
            durationPanel.Controls.Add(durationLabel);
            layout.Controls.Add(durationPanel, 0, 5);

            _startButton = new AccentButton
            {
                Text = "开始记录",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 4)
            };
            _startButton.Click += delegate { StartButton_Click(); };
            layout.Controls.Add(_startButton, 0, 6);

            Panel progress = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _phaseLabel = MakeLabel("准备就绪", 8.4f, FontStyle.Bold, Theme.Muted);
            _phaseLabel.AutoSize = true;
            _phaseLabel.Location = new Point(0, 1);
            _timeLabel = MakeLabel("", 8.3f, FontStyle.Regular, Theme.Muted);
            _timeLabel.AutoSize = false;
            _timeLabel.Dock = DockStyle.Right;
            _timeLabel.TextAlign = ContentAlignment.MiddleRight;
            _timeLabel.Width = 135;
            _progressTrack = new Panel
            {
                BackColor = Theme.Input,
                Location = new Point(0, 27),
                Height = 6,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _progressFill = new Panel { BackColor = Theme.Accent, Dock = DockStyle.Left, Width = 0 };
            _progressTrack.Controls.Add(_progressFill);
            progress.Resize += delegate
            {
                _progressTrack.Width = Math.Max(0, progress.ClientSize.Width);
            };
            progress.Controls.Add(_phaseLabel);
            progress.Controls.Add(_timeLabel);
            progress.Controls.Add(_progressTrack);
            layout.Controls.Add(progress, 0, 7);

            Panel history = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _standardBeforeHistory = MakeHistoryLabel("无负载 · 清灰前：未记录");
            _standardAfterHistory = MakeHistoryLabel("无负载 · 清灰后：未记录");
            _gamingBeforeHistory = MakeHistoryLabel("游戏负载 · 清灰前：未记录");
            _gamingAfterHistory = MakeHistoryLabel("游戏负载 · 清灰后：未记录");
            history.Controls.Add(_gamingAfterHistory);
            history.Controls.Add(_gamingBeforeHistory);
            history.Controls.Add(_standardAfterHistory);
            history.Controls.Add(_standardBeforeHistory);
            layout.Controls.Add(history, 0, 8);

            Panel guide = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Input,
                Margin = new Padding(0, 0, 0, 6)
            };
            Label guideText = MakeLabel(
                "无负载：关闭大型程序后静置记录 5 分钟。\n" +
                "游戏：开始记录后切到游戏，完成相同场景后自动保存。",
                7.8f, FontStyle.Regular, Theme.Dim);
            guideText.AutoSize = false;
            guideText.Dock = DockStyle.Fill;
            guideText.Padding = new Padding(9, 7, 9, 5);
            guide.Controls.Add(guideText);
            layout.Controls.Add(guide, 0, 9);
            return card;
        }

        private static TableLayoutPanel SegmentRow(
            string caption,
            out RadioButton first,
            string firstText,
            out RadioButton second,
            string secondText)
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 5)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            Label label = MakeLabel(caption, 8f, FontStyle.Regular, Theme.Muted);
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            first = CreateSegmentRadio(firstText, true);
            second = CreateSegmentRadio(secondText, false);
            row.Controls.Add(label, 0, 0);
            row.Controls.Add(first, 1, 0);
            row.Controls.Add(second, 2, 0);
            return row;
        }

        private static RadioButton CreateSegmentRadio(string text, bool selected)
        {
            RadioButton radio = new RadioButton
            {
                Text = text,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                Checked = selected,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.Input,
                ForeColor = selected ? Theme.Text : Theme.Muted,
                Font = Theme.Font(8.4f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 3, 0)
            };
            radio.FlatAppearance.BorderColor = Theme.Border;
            radio.FlatAppearance.CheckedBackColor = Theme.AccentDark;
            radio.FlatAppearance.MouseOverBackColor = Theme.CardAlt;
            radio.FlatAppearance.MouseDownBackColor = Theme.AccentDark;
            return radio;
        }

        private static Label MakeHistoryLabel(string text)
        {
            Label label = MakeLabel(text, 7.8f, FontStyle.Regular, Theme.Dim);
            label.AutoSize = false;
            label.Dock = DockStyle.Top;
            label.Height = 21;
            return label;
        }

        private CardPanel BuildComparisonCard()
        {
            CardPanel card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10, 0, 0, 0),
                Padding = new Padding(16)
            };
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            card.Controls.Add(layout);

            Panel head = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label title = MakeLabel("清灰前后量化对比", 10.5f, FontStyle.Bold, Theme.Text);
            title.Location = new Point(2, 3);
            Label legend = MakeLabel("负值 = 清灰后更低", 7.8f, FontStyle.Regular, Theme.Dim);
            legend.AutoSize = false;
            legend.Dock = DockStyle.Right;
            legend.TextAlign = ContentAlignment.MiddleRight;
            legend.Width = 150;
            head.Controls.Add(title);
            head.Controls.Add(legend);
            layout.Controls.Add(head, 0, 0);

            Panel summary = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _compareHeadline = MakeLabel("先记录清灰前的无负载温度", 12f, FontStyle.Bold, Theme.Muted);
            _compareHeadline.AutoSize = false;
            _compareHeadline.Dock = DockStyle.Top;
            _compareHeadline.Height = 30;
            _compareExplanation = MakeLabel(
                "分别记录无负载和游戏负载，清灰前后使用相同条件。",
                7.8f, FontStyle.Regular, Theme.Dim);
            _compareExplanation.AutoSize = false;
            _compareExplanation.Dock = DockStyle.Fill;
            summary.Controls.Add(_compareExplanation);
            summary.Controls.Add(_compareHeadline);
            layout.Controls.Add(summary, 0, 1);

            _comparisonGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Card,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Grid,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowTemplate = { Height = 32 },
                ColumnHeadersHeight = 32,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                Font = Theme.Font(8.2f, FontStyle.Regular),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _comparisonGrid.DefaultCellStyle.BackColor = Theme.Card;
            _comparisonGrid.DefaultCellStyle.ForeColor = Theme.Text;
            _comparisonGrid.DefaultCellStyle.SelectionBackColor = Theme.CardAlt;
            _comparisonGrid.DefaultCellStyle.SelectionForeColor = Theme.Text;
            _comparisonGrid.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
            _comparisonGrid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Card;
            _comparisonGrid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Muted;
            _comparisonGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Card;
            _comparisonGrid.ColumnHeadersDefaultCellStyle.Font = Theme.Font(7.8f, FontStyle.Bold);
            _comparisonGrid.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            _comparisonGrid.AdvancedCellBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;
            AddGridColumn(_comparisonGrid, "metric", "测试项目", 34);
            AddGridColumn(_comparisonGrid, "before", "清灰前", 15);
            AddGridColumn(_comparisonGrid, "after", "清灰后", 15);
            AddGridColumn(_comparisonGrid, "delta", "变化", 14);
            AddGridColumn(_comparisonGrid, "result", "结论", 22);
            layout.Controls.Add(_comparisonGrid, 0, 2);

            Panel actions = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _exportButton = new AccentButton
            {
                Text = "导出完整对比报告",
                Dock = DockStyle.Right,
                Width = 190,
                Height = 38,
                Enabled = false
            };
            _exportButton.Click += delegate { ExportComparisonReport(); };
            AccentButton folder = new AccentButton
            {
                Text = "打开记录文件夹",
                Dock = DockStyle.Left,
                Width = 170,
                Height = 38,
                NormalColor = Theme.CardAlt,
                HoverColor = Color.FromArgb(34, 48, 62),
                ForeColor = Theme.Text
            };
            folder.Click += delegate { OpenReportsFolder(); };
            actions.Controls.Add(_exportButton);
            actions.Controls.Add(folder);
            layout.Controls.Add(actions, 0, 3);
            return card;
        }

        private static ComboBox MakeCombo(int width)
        {
            ComboBox combo = new ComboBox
            {
                Width = width,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Font = Theme.Font(8.2f, FontStyle.Regular),
                Margin = new Padding(0, 2, 8, 0)
            };
            return combo;
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

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (_sensorSearch != null)
                SendMessage(_sensorSearch.Handle, 0x1501, (IntPtr)1, "搜索传感器或设备…");
            RefreshStoredSessions();
            if (IsAdministrator() && !IsPawnIoInstalled())
                OfferPawnIoInstallation();
            _sampler.Start();
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
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
                Type pawnIoType = typeof(LibreHardwareMonitor.Hardware.Computer).Assembly.GetType(
                    "LibreHardwareMonitor.PawnIo.PawnIo", false);
                if (pawnIoType == null)
                    return true;
                PropertyInfo property = pawnIoType.GetProperty("IsInstalled",
                    BindingFlags.Public | BindingFlags.Static);
                return property == null || property.PropertyType != typeof(bool) ||
                       (bool)property.GetValue(null, null);
            }
            catch
            {
                return true;
            }
        }

        private void OfferPawnIoInstallation()
        {
            string installer = Path.Combine(Application.StartupPath, "PawnIO_setup.exe");
            if (!File.Exists(installer))
                return;
            DialogResult result = MessageBox.Show(
                "CPU 温度需要 LibreHardwareMonitor 官方使用的 PawnIO 底层驱动。\n\n" +
                "是否现在打开安装程序？安装完成后回到本窗口即可开始监测。",
                "首次使用：安装温度驱动",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法启动驱动安装程序：\n" + ex.Message, "安装失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
            _sensorReady = snapshot.HasCpu || snapshot.HasGpu;
            UpdateLiveStats(snapshot);
            UpdateDashboard(snapshot);
            UpdateThermalAssessment(snapshot);
            RefreshStatusFilterChoices(snapshot);
            RefreshChartChoices(snapshot);
            _trendChart.Add(snapshot);

            _gridTick++;
            if (_gridTick % 2 == 0)
                UpdateSensorTables();

            if (_testActive)
            {
                AddTestSample(snapshot);
                if ((snapshot.HasCpu && snapshot.CpuTemp >= 105) ||
                    (snapshot.HasGpuHotspot && snapshot.GpuHotspot >= 115) ||
                    (snapshot.HasGpu && snapshot.GpuTemp >= 100))
                    AbortTestForTemperature();
            }
        }

        private void UpdateLiveStats(SensorSnapshot snapshot)
        {
            if (snapshot.Rows == null)
                return;
            foreach (LiveSensor sensor in snapshot.Rows)
            {
                LiveSensorStat stat;
                if (!_liveStats.TryGetValue(sensor.Identifier, out stat))
                {
                    stat = new LiveSensorStat
                    {
                        Last = sensor,
                        Min = sensor.Value,
                        Max = sensor.Value,
                        PreviousValue = sensor.Value,
                        Recent = new List<double>()
                    };
                    _liveStats[sensor.Identifier] = stat;
                }
                else
                {
                    stat.PreviousValue = stat.Last.Value;
                }
                stat.Last = sensor;
                stat.Count++;
                stat.Sum += sensor.Value;
                if (sensor.Value < stat.Min) stat.Min = sensor.Value;
                if (sensor.Value > stat.Max) stat.Max = sensor.Value;
                stat.Recent.Add(sensor.Value);
                while (stat.Recent.Count > 60)
                    stat.Recent.RemoveAt(0);
                stat.RecalculateFluctuation();
            }
        }

        private void UpdateThermalAssessment(SensorSnapshot snapshot)
        {
            SensorAssessment assessment = SensorKnowledge.EvaluateOverall(snapshot);
            double cpuFluctuation = PrimaryTemperatureFluctuation(snapshot, "CPU", snapshot.CpuSensorName);
            double gpuFluctuation = PrimaryTemperatureFluctuation(snapshot, "GPU", snapshot.GpuSensorName);
            double worstFluctuation = Math.Max(cpuFluctuation, gpuFluctuation);
            string fluctuationText = "波动 " +
                (snapshot.HasCpu ? "CPU ±" + cpuFluctuation.ToString("0.0") + "°C" : "") +
                (snapshot.HasCpu && snapshot.HasGpu ? " / " : "") +
                (snapshot.HasGpu ? "GPU ±" + gpuFluctuation.ToString("0.0") + "°C" : "");
            if (fluctuationText == "波动 ")
                fluctuationText = "波动数据等待中";
            string fluctuationAdvice = FluctuationAdvice(worstFluctuation, assessment.Level);
            bool fluctuating = worstFluctuation >= 2.0;
            _thermalStatusTitle.Text = "当前温度：" + assessment.Status +
                                       (fluctuating ? " · 波动明显" : "");
            _thermalStatusTitle.ForeColor = fluctuating && assessment.Level < 3
                ? Theme.Yellow : assessment.Color;
            _thermalStatusDetail.Text = assessment.Meaning + "  ·  " +
                                        fluctuationText + "  ·  " + fluctuationAdvice;
            _thermalStatusDetail.ForeColor =
                fluctuating || assessment.Level >= 3 ? _thermalStatusTitle.ForeColor : Theme.Muted;
            _thermalStatusCard.BorderColor = assessment.Color;
            _thermalStatusCard.Invalidate();
        }

        private double PrimaryTemperatureFluctuation(SensorSnapshot snapshot, string group, string sensorName)
        {
            if (snapshot.Rows == null)
                return 0;
            LiveSensor sensor = snapshot.Rows.FirstOrDefault(s =>
                s.Group == group && s.Type == SensorType.Temperature &&
                s.Name == sensorName);
            if (sensor == null)
            {
                sensor = snapshot.Rows
                    .Where(s => s.Group == group && s.Type == SensorType.Temperature)
                    .OrderByDescending(s => s.Value)
                    .FirstOrDefault();
            }
            if (sensor == null)
                return 0;
            LiveSensorStat stat;
            return _liveStats.TryGetValue(sensor.Identifier, out stat) ? stat.StdDev : 0;
        }

        private static string FluctuationAdvice(double fluctuation, int assessmentLevel)
        {
            if (fluctuation >= 3.5)
                return "温度波动明显；关注风扇启停、后台负载和降频变化。";
            if (fluctuation >= 2.0)
                return "温度波动较大；建议在固定负载下再观察。";
            if (fluctuation >= 0.8)
                return "温度有轻微波动，通常与负载或风扇调节有关。";
            if (assessmentLevel >= 3)
                return "温度偏高但波动平稳，更可能是持续散热不足。";
            return "温度与波动均处于较平稳状态。";
        }

        private void UpdateDashboard(SensorSnapshot snapshot)
        {
            _sensorReady = snapshot.HasCpu || snapshot.HasGpu;
            _startButton.Enabled = _sensorReady || _testActive;
            _statusLabel.Text = snapshot.HasCpu
                ? (snapshot.HasGpu ? "传感器在线 · " + snapshot.Rows.Count + " 项 · 1 秒刷新"
                    : "CPU 传感器在线 · GPU 温度未读取")
                : (snapshot.HasGpu ? "GPU 传感器在线 · CPU 需管理员/PawnIO"
                    : "未读到温度传感器 · 请允许管理员权限");
            _statusLabel.ForeColor = snapshot.HasCpu
                ? (snapshot.HasGpu ? Theme.Green : Theme.Yellow)
                : Theme.Red;
            _cpuInfoLabel.Text = "CPU：" + Shorten(snapshot.CpuName, 22);
            _gpuInfoLabel.Text = "GPU：" + Shorten(snapshot.GpuName, 22);
            _cpuInfoLabel.ForeColor = snapshot.HasCpu ? Theme.Dim : Theme.Red;
            _gpuInfoLabel.ForeColor = snapshot.HasGpu ? Theme.Dim : Theme.Red;

            _cpuCard.Set(snapshot.HasCpu ? snapshot.CpuTemp.ToString("0.0") + "°" : "--",
                snapshot.HasCpu
                    ? Shorten(snapshot.CpuSensorName, 14) + " · " +
                      (snapshot.HasCpuPower ? snapshot.CpuPower.ToString("0") + " W" : "功耗 --")
                    : "CPU 温度不可用");
            _gpuCard.Set(snapshot.HasGpu ? snapshot.GpuTemp.ToString("0.0") + "°" : "--",
                snapshot.HasGpu
                    ? (snapshot.HasGpuHotspot ? "热点 " + snapshot.GpuHotspot.ToString("0.0") + "°" : "GPU Core") +
                      " · " + (snapshot.HasGpuPower ? snapshot.GpuPower.ToString("0") + " W" : "--")
                    : "GPU 温度不可用");
            _cpuLoadCard.Set(snapshot.HasCpuLoad ? snapshot.CpuLoad.ToString("0") + "%" : "--",
                (snapshot.HasCpuPower ? snapshot.CpuPower.ToString("0") + " W" : "功耗 --") +
                " · " + (snapshot.HasCpuClock ? snapshot.CpuClock.ToString("0") + " MHz" : "频率 --"));
            _gpuLoadCard.Set(snapshot.HasGpuLoad ? snapshot.GpuLoad.ToString("0") + "%" : "--",
                (snapshot.HasGpuPower ? snapshot.GpuPower.ToString("0") + " W" : "功耗 --") +
                " · " + (snapshot.HasGpuClock ? snapshot.GpuClock.ToString("0") + " MHz" : "频率 --"));

            _keyValueLabels[0].Text = snapshot.HasCpuPower ? snapshot.CpuPower.ToString("0.0") + " W" : "--";
            _keyValueLabels[1].Text = snapshot.HasGpuPower ? snapshot.GpuPower.ToString("0.0") + " W" : "--";
            _keyValueLabels[2].Text = snapshot.HasCpuClock ? snapshot.CpuClock.ToString("0") + " MHz" : "--";
            _keyValueLabels[3].Text = snapshot.HasGpuClock ? snapshot.GpuClock.ToString("0") + " MHz" : "--";
            _keyValueLabels[4].Text = snapshot.HasGpuHotspot ? snapshot.GpuHotspot.ToString("0.0") + " °C" : "--";
            _keyValueLabels[5].Text = snapshot.HasFan ? snapshot.FanRpm.ToString("0") + " RPM" : "--";
            double hottest = 0;
            if (snapshot.Rows != null)
                hottest = snapshot.Rows.Where(s => s.Type == SensorType.Temperature)
                    .Select(s => s.Value).DefaultIfEmpty(0).Max();
            _keyValueLabels[6].Text = hottest > 0 ? hottest.ToString("0.0") + " °C" : "--";
        }

        private void RefreshChartChoices(SensorSnapshot snapshot)
        {
            if (snapshot.Rows == null)
                return;
            List<LiveSensor> candidates = snapshot.Rows
                .Where(IsChartSensor)
                .GroupBy(s => s.Identifier)
                .Select(g => g.First())
                .OrderBy(s => GroupPriority(s.Group))
                .ThenBy(s => TypePriority(s.Type))
                .ThenBy(s => s.Name)
                .Take(140)
                .ToList();
            string fingerprint = string.Join("|", candidates.Select(s => s.Identifier).ToArray());
            if (fingerprint == _chartFingerprint)
                return;
            _chartFingerprint = fingerprint;

            string oldPrimary = SelectedChartIdentifier(_primaryChartCombo);
            string oldSecondary = SelectedChartIdentifier(_secondaryChartCombo);
            _primaryChartCombo.BeginUpdate();
            _secondaryChartCombo.BeginUpdate();
            _primaryChartCombo.Items.Clear();
            _secondaryChartCombo.Items.Clear();
            int primaryIndex = 0;
            int secondaryIndex = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                LiveSensor sensor = candidates[i];
                ChartChoice choice = new ChartChoice
                {
                    Identifier = sensor.Identifier,
                    Text = sensor.Group + " · " + sensor.Name + " [" + sensor.Unit + "]"
                };
                _primaryChartCombo.Items.Add(choice);
                _secondaryChartCombo.Items.Add(choice);
                if (sensor.Identifier == oldPrimary) primaryIndex = i;
                if (sensor.Identifier == oldSecondary) secondaryIndex = i;
            }
            if (string.IsNullOrEmpty(oldPrimary))
            {
                int index = candidates.FindIndex(s => s.Group == "CPU" && s.Type == SensorType.Temperature);
                if (index >= 0) primaryIndex = index;
            }
            if (string.IsNullOrEmpty(oldSecondary))
            {
                int index = candidates.FindIndex(s => s.Group == "GPU" && s.Type == SensorType.Temperature);
                if (index >= 0) secondaryIndex = index;
                else secondaryIndex = Math.Min(1, Math.Max(0, candidates.Count - 1));
            }
            if (candidates.Count > 0)
            {
                _primaryChartCombo.SelectedIndex = primaryIndex;
                _secondaryChartCombo.SelectedIndex = secondaryIndex;
            }
            _primaryChartCombo.EndUpdate();
            _secondaryChartCombo.EndUpdate();
            ApplyChartSelections();
        }

        private void RefreshStatusFilterChoices(SensorSnapshot snapshot)
        {
            if (_statusFilter == null || snapshot.Rows == null)
                return;
            List<string> statuses = snapshot.Rows
                .Select(sensor => SensorKnowledge.Evaluate(snapshot, sensor).Status)
                .Where(status => !string.IsNullOrEmpty(status))
                .Distinct()
                .OrderBy(StatusPriority)
                .ThenBy(status => status)
                .Take(30)
                .ToList();
            string fingerprint = string.Join("|", statuses.ToArray());
            if (fingerprint == _statusFingerprint)
                return;
            _statusFingerprint = fingerprint;
            string old = Convert.ToString(_statusFilter.SelectedItem);
            _updatingStatusFilter = true;
            _statusFilter.BeginUpdate();
            _statusFilter.Items.Clear();
            _statusFilter.Items.Add("全部判断");
            foreach (string status in statuses)
                _statusFilter.Items.Add(status);
            int index = 0;
            for (int i = 0; i < _statusFilter.Items.Count; i++)
            {
                if (Convert.ToString(_statusFilter.Items[i]) == old)
                {
                    index = i;
                    break;
                }
            }
            _statusFilter.SelectedIndex = index;
            _statusFilter.EndUpdate();
            _updatingStatusFilter = false;
        }

        private static int StatusPriority(string status)
        {
            if (status == "危险") return 0;
            if (status == "过高") return 1;
            if (status == "偏热") return 2;
            if (status == "未检测到转速") return 3;
            if (status == "风速偏低") return 4;
            if (status == "可读取" || status == "需参考设备") return 5;
            if (status == "空闲" || status == "停止/未上报" || status == "低频/休眠") return 9;
            return 6;
        }

        private static bool IsChartSensor(LiveSensor sensor)
        {
            return sensor.Type == SensorType.Temperature ||
                   sensor.Type == SensorType.Load ||
                   sensor.Type == SensorType.Power ||
                   sensor.Type == SensorType.Clock ||
                   sensor.Type == SensorType.Fan;
        }

        private void ApplyChartSelections()
        {
            if (_trendChart == null)
                return;
            ChartChoice primary = _primaryChartCombo.SelectedItem as ChartChoice;
            ChartChoice secondary = _secondaryChartCombo.SelectedItem as ChartChoice;
            _trendChart.SetSensors(
                primary == null ? null : primary.Identifier,
                primary == null ? "" : primary.Text,
                secondary == null ? null : secondary.Identifier,
                secondary == null ? "" : secondary.Text);
        }

        private static string SelectedChartIdentifier(ComboBox combo)
        {
            if (combo == null)
                return null;
            ChartChoice choice = combo.SelectedItem as ChartChoice;
            return choice == null ? null : choice.Identifier;
        }

        private void UpdateSensorTables()
        {
            if (_latest == null || _updatingStatusFilter)
                return;
            FillSensorGrid(_sensorGrid, false);
            FillSensorGrid(_previewGrid, true);
        }

        private void FillSensorGrid(DataGridView grid, bool preview)
        {
            if (grid == null)
                return;
            string selectedId = _selectedSensorId;
            if (!preview && grid.SelectedRows.Count > 0)
                selectedId = Convert.ToString(grid.SelectedRows[0].Tag);
            if (!preview)
                _updatingSensorGrid = true;
            string query = _sensorSearch == null ? "" : _sensorSearch.Text.Trim().ToLowerInvariant();
            string type = _typeFilter == null || _typeFilter.SelectedIndex <= 0
                ? null : Convert.ToString(_typeFilter.SelectedItem);
            string group = _groupFilter == null || _groupFilter.SelectedIndex <= 0
                ? null : Convert.ToString(_groupFilter.SelectedItem);
            string status = _statusFilter == null || _statusFilter.SelectedIndex <= 0
                ? null : Convert.ToString(_statusFilter.SelectedItem);
            int sortMode = _sortCombo == null ? 0 : _sortCombo.SelectedIndex;

            List<LiveSensor> rows = _latest.Rows == null
                ? new List<LiveSensor>()
                : _latest.Rows.ToList();
            IEnumerable<LiveSensor> filtered = rows.Where(delegate(LiveSensor sensor)
            {
                if (preview && !(sensor.Type == SensorType.Temperature ||
                                 sensor.Type == SensorType.Fan ||
                                 sensor.Type == SensorType.Load ||
                                 sensor.Type == SensorType.Power))
                    return false;
                if (preview && sensor.Group == "其他")
                    return false;
                if (!string.IsNullOrEmpty(group) && sensor.Group != group)
                    return false;
                if (!string.IsNullOrEmpty(status) &&
                    SensorKnowledge.Evaluate(_latest, sensor).Status != status)
                    return false;
                if (!string.IsNullOrEmpty(type) && type != "其他" &&
                    SensorFormat.Kind(sensor.Type) != type)
                    return false;
                if (type == "其他" &&
                    (sensor.Type == SensorType.Temperature || sensor.Type == SensorType.Fan ||
                     sensor.Type == SensorType.Load || sensor.Type == SensorType.Power ||
                     sensor.Type == SensorType.Clock || sensor.Type == SensorType.Voltage ||
                     sensor.Type == SensorType.Current))
                    return false;
                if (!string.IsNullOrEmpty(query) &&
                    (sensor.Name ?? "").ToLowerInvariant().IndexOf(query, StringComparison.Ordinal) < 0 &&
                    (sensor.HardwareName ?? "").ToLowerInvariant().IndexOf(query, StringComparison.Ordinal) < 0 &&
                    sensor.Group.ToLowerInvariant().IndexOf(query, StringComparison.Ordinal) < 0)
                    return false;
                return true;
            });
            if (!preview)
            {
                if (sortMode == 1)
                    filtered = filtered.OrderBy(s => s.Name).ThenBy(s => s.Group);
                else if (sortMode == 2)
                    filtered = filtered.OrderByDescending(s => s.Value).ThenBy(s => s.Name);
                else if (sortMode == 3)
                    filtered = filtered.OrderBy(s => s.Value).ThenBy(s => s.Name);
                else if (sortMode == 4)
                    filtered = filtered.OrderBy(s => s.Type == SensorType.Temperature ? 0 : 1)
                        .ThenByDescending(s => s.Value);
                else if (sortMode == 5)
                    filtered = filtered.OrderBy(s => s.Type == SensorType.Temperature ? 0 : 1)
                        .ThenBy(s => s.Value);
                else if (sortMode == 6)
                    filtered = filtered.OrderBy(s => s.Type == SensorType.Temperature ? 0 : 1)
                        .ThenByDescending(s =>
                        {
                            LiveSensorStat sortedStat;
                            return _liveStats.TryGetValue(s.Identifier, out sortedStat)
                                ? sortedStat.StdDev : 0;
                        });
                else if (sortMode == 7)
                    filtered = filtered.OrderBy(s => s.Type == SensorType.Temperature ? 0 : 1)
                        .ThenBy(s =>
                        {
                            LiveSensorStat sortedStat;
                            return _liveStats.TryGetValue(s.Identifier, out sortedStat)
                                ? sortedStat.StdDev : 0;
                        });
            }
            if (preview)
                filtered = filtered.Take(42);
            List<LiveSensor> visible = filtered.ToList();

            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                foreach (LiveSensor sensor in visible)
                {
                    LiveSensorStat stat;
                    if (!_liveStats.TryGetValue(sensor.Identifier, out stat))
                        continue;
                    int index;
                    if (preview)
                    {
                        SensorAssessment previewAssessment = SensorKnowledge.Evaluate(_latest, sensor);
                        index = grid.Rows.Add(
                            SensorFormat.Kind(sensor.Type),
                            sensor.Name,
                            SensorFormat.Format(sensor.Type, sensor.Value),
                            previewAssessment.Status,
                            FormatFluctuation(sensor.Type, stat.StdDev),
                            sensor.HardwareName);
                    }
                    else
                    {
                        SensorAssessment assessment = SensorKnowledge.Evaluate(_latest, sensor);
                        index = grid.Rows.Add(
                            SensorFormat.Kind(sensor.Type),
                            sensor.Name,
                            SensorFormat.Format(sensor.Type, sensor.Value),
                            assessment.Status,
                            FormatFluctuation(sensor.Type, stat.StdDev),
                            SensorFormat.Format(sensor.Type, stat.Min),
                            SensorFormat.Format(sensor.Type, stat.Sum / Math.Max(1, stat.Count)),
                            SensorFormat.Format(sensor.Type, stat.Max),
                            sensor.HardwareName);
                        DataGridViewRow added = grid.Rows[index];
                        added.Tag = sensor.Identifier;
                        added.Cells[3].Style.ForeColor = assessment.Color;
                    }
                    DataGridViewRow dataRow = grid.Rows[index];
                    Color accent = sensor.Group == "CPU" ? Theme.Accent :
                        (sensor.Group == "GPU" ? Theme.Orange : Theme.Text);
                    dataRow.Cells[0].Style.ForeColor = Theme.Dim;
                    dataRow.Cells[1].Style.ForeColor = accent;
                    dataRow.Cells[2].Style.ForeColor = SensorValueColor(sensor, sensor.Value);
                    if (!preview)
                    {
                        dataRow.Cells[4].Style.ForeColor = FluctuationColor(sensor.Type, stat.StdDev);
                        dataRow.Cells[5].Style.ForeColor = Theme.Muted;
                        dataRow.Cells[6].Style.ForeColor = Theme.Muted;
                        dataRow.Cells[7].Style.ForeColor = Theme.Muted;
                        dataRow.Cells[8].Style.ForeColor = Theme.Dim;
                    }
                    else
                    {
                        dataRow.Cells[4].Style.ForeColor = FluctuationColor(sensor.Type, stat.StdDev);
                        dataRow.Cells[5].Style.ForeColor = Theme.Dim;
                    }
                }
            }
            finally
            {
                grid.ResumeLayout();
                if (!preview)
                {
                    DataGridViewRow target = null;
                    if (!string.IsNullOrEmpty(selectedId))
                    {
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (Convert.ToString(row.Tag) == selectedId)
                            {
                                target = row;
                                break;
                            }
                        }
                    }
                    if (target == null && grid.Rows.Count > 0)
                        target = grid.Rows[0];
                    if (target != null)
                    {
                        grid.ClearSelection();
                        target.Selected = true;
                        grid.CurrentCell = target.Cells[0];
                        _selectedSensorId = Convert.ToString(target.Tag);
                    }
                    _updatingSensorGrid = false;
                }
                else
                {
                    grid.ClearSelection();
                    grid.CurrentCell = null;
                }
            }
            if (!preview)
                ShowSelectedSensorDetails();
            if (!preview && _sensorCountLabel != null)
                _sensorCountLabel.Text = visible.Count + " 个传感器";
        }

        private Color SensorValueColor(LiveSensor sensor, double value)
        {
            SensorAssessment assessment = SensorKnowledge.Evaluate(_latest, sensor);
            if (assessment.Level >= 2)
                return assessment.Color;
            if (sensor.Type == SensorType.Temperature)
                return sensor.Group == "GPU" ? Theme.Orange : Theme.Accent;
            if (sensor.Type == SensorType.Fan || sensor.Type == SensorType.Load)
                return Theme.Green;
            if (sensor.Type == SensorType.Power)
                return Theme.Yellow;
            return Theme.Text;
        }

        private static string FormatFluctuation(SensorType type, double standardDeviation)
        {
            return "±" + standardDeviation.ToString("0.0") + " " + SensorFormat.Unit(type);
        }

        private static Color FluctuationColor(SensorType type, double standardDeviation)
        {
            if (type != SensorType.Temperature)
                return Theme.Muted;
            if (standardDeviation >= 3.5) return Theme.Red;
            if (standardDeviation >= 2.0) return Theme.Orange;
            if (standardDeviation >= 0.8) return Theme.Yellow;
            return Theme.Muted;
        }

        private void ShowSelectedSensorDetails()
        {
            if (_updatingSensorGrid || _sensorGrid == null || _latest == null)
                return;
            if (_sensorGrid.SelectedRows.Count == 0)
                return;
            string identifier = Convert.ToString(_sensorGrid.SelectedRows[0].Tag);
            if (string.IsNullOrEmpty(identifier))
                return;
            LiveSensor sensor = _latest.Rows == null
                ? null
                : _latest.Rows.FirstOrDefault(s => s.Identifier == identifier);
            if (sensor == null)
                return;
            _selectedSensorId = identifier;
            SensorAssessment assessment = SensorKnowledge.Evaluate(_latest, sensor);
            _sensorDetailTitle.Text = sensor.Group + " · " + sensor.Name + "  ·  " + Shorten(sensor.HardwareName, 30);
            _sensorDetailStatus.Text =
                SensorFormat.Format(sensor.Type, sensor.Value) + "  |  " + assessment.Status;
            _sensorDetailStatus.ForeColor = assessment.Color;
            _sensorDetailDescription.Text = "介绍：" + SensorKnowledge.Description(sensor);
            _sensorDetailImportance.Text = "重要性：" + SensorKnowledge.Importance(sensor);
            _sensorDetailMeaning.Text =
                "高低含义：" + SensorKnowledge.HighLowMeaning(sensor.Type) +
                "  当前：" + assessment.Meaning;
            LiveSensorStat stat;
            _liveStats.TryGetValue(identifier, out stat);
            double standardDeviation = stat == null ? 0 : stat.StdDev;
            double swing = stat == null ? 0 : stat.Swing;
            _sensorDetailFluctuation.Text = "波动分析：" +
                FluctuationNarrative(sensor.Type, standardDeviation, swing, assessment.Level);
            _sensorDetailFluctuation.ForeColor = FluctuationColor(sensor.Type, standardDeviation);
            string advice = assessment.Advice;
            if (sensor.Type == SensorType.Temperature && standardDeviation >= 2.0)
                advice += " 温度波动较大，建议保持相同负载观察，并检查风扇是否频繁启停。";
            _sensorDetailAdvice.Text = "建议：" + advice;
            _sensorDetailAdvice.ForeColor =
                assessment.Level >= 3 || standardDeviation >= 2.0
                    ? (assessment.Level >= 3 ? assessment.Color : Theme.Orange)
                    : Theme.Muted;
        }

        private static string FluctuationNarrative(
            SensorType type, double standardDeviation, double swing, int assessmentLevel)
        {
            string unit = SensorFormat.Unit(type);
            string subject = type == SensorType.Temperature ? "温度" : "读数";
            string level;
            if (standardDeviation >= 3.5)
                level = subject + "波动明显";
            else if (standardDeviation >= 2.0)
                level = subject + "波动较大";
            else if (standardDeviation >= 0.8)
                level = subject + "有轻微波动";
            else
                level = subject + "较平稳";
            string cause;
            if (standardDeviation >= 2.0)
                cause = "可能与负载变化、风扇调速、功耗限制或后台任务有关。";
            else if (assessmentLevel >= 3)
                cause = "温度偏高但波动不大，更像是持续散热不足。";
            else
                cause = "当前波动不影响判断。";
            return "最近约 60 秒标准差 ±" + standardDeviation.ToString("0.0") + " " + unit +
                   "，峰谷差 " + swing.ToString("0.0") + " " + unit +
                   "；" + level + "。" + cause;
        }

        private static int GroupPriority(string group)
        {
            if (group == "CPU") return 0;
            if (group == "GPU") return 1;
            if (group == "主板") return 2;
            if (group == "散热器") return 3;
            if (group == "存储") return 4;
            if (group == "内存") return 5;
            return 9;
        }

        private static int TypePriority(SensorType type)
        {
            if (type == SensorType.Temperature) return 0;
            if (type == SensorType.Load) return 1;
            if (type == SensorType.Power) return 2;
            if (type == SensorType.Clock) return 3;
            if (type == SensorType.Fan) return 4;
            return 9;
        }

        private void ApplyTestSelection()
        {
            if (_startButton == null)
                return;
            _beforeRadio.ForeColor = _beforeRadio.Checked ? Theme.Text : Theme.Muted;
            _afterRadio.ForeColor = _afterRadio.Checked ? Theme.Text : Theme.Muted;
            _standardRadio.ForeColor = _standardRadio.Checked ? Theme.Text : Theme.Muted;
            _gamingRadio.ForeColor = _gamingRadio.Checked ? Theme.Text : Theme.Muted;
            bool gaming = _gamingRadio.Checked;
            _workloadText.Enabled = gaming && !_testActive;
            _gameDurationCombo.Enabled = gaming && !_testActive;
            if (!_testActive)
            {
                _startButton.Text = (gaming ? "开始游戏负载记录" : "开始无负载记录");
                _testInstructionLabel.Text = gaming
                    ? "选清灰阶段后开始记录，然后切到游戏并保持相同场景和画质。"
                    : "关闭大型程序并静置记录 5 分钟，记录 CPU/GPU 无负载温度。";
            }
        }

        private void RefreshStoredSessions()
        {
            _standardBefore = SessionStore.Load(MeasurementStage.Before, TestMode.Standard);
            _standardAfter = SessionStore.Load(MeasurementStage.After, TestMode.Standard);
            _gamingBefore = SessionStore.Load(MeasurementStage.Before, TestMode.Gaming);
            _gamingAfter = SessionStore.Load(MeasurementStage.After, TestMode.Gaming);
            UpdateHistoryLabels();
            RefreshComparison();
            if (_standardBefore == null)
                _beforeRadio.Checked = true;
            else if (_standardAfter == null)
                _afterRadio.Checked = true;
        }

        private void UpdateHistoryLabels()
        {
            SetHistoryLabel(_standardBeforeHistory, "无负载 · 清灰前", _standardBefore, Theme.Accent);
            SetHistoryLabel(_standardAfterHistory, "无负载 · 清灰后", _standardAfter, Theme.Green);
            SetHistoryLabel(_gamingBeforeHistory, "游戏负载 · 清灰前", _gamingBefore, Theme.Accent);
            SetHistoryLabel(_gamingAfterHistory, "游戏负载 · 清灰后", _gamingAfter, Theme.Green);
        }

        private static void SetHistoryLabel(Label label, string prefix, SessionRecord session, Color color)
        {
            if (label == null)
                return;
            label.Text = session == null
                ? prefix + "：未记录"
                : prefix + "：" + session.CreatedAtUtc.ToLocalTime().ToString("MM-dd HH:mm") +
                  " · " + (string.IsNullOrEmpty(session.WorkloadName) ? "已完成" : session.WorkloadName);
            label.ForeColor = session == null ? Theme.Dim : color;
        }

        private void StartButton_Click()
        {
            if (_testActive)
            {
                if (_activeMode == TestMode.Gaming)
                {
                    DialogResult end = MessageBox.Show(
                        "结束本次游戏负载记录并保存当前结果？",
                        "结束记录",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (end == DialogResult.Yes)
                        FinishTest(true);
                }
                else
                {
                    CancelCurrentTest();
                }
                return;
            }

            TestMode mode = _gamingRadio.Checked ? TestMode.Gaming : TestMode.Standard;
            MeasurementStage stage = _beforeRadio.Checked
                ? MeasurementStage.Before : MeasurementStage.After;
            if (!_sensorReady || _latest == null ||
                (mode == TestMode.Standard && !_latest.HasCpu))
            {
                MessageBox.Show(
                    mode == TestMode.Standard
                        ? "无负载测试需要读取 CPU 温度。请确认管理员权限和 PawnIO 驱动。"
                        : "当前没有可用的温度传感器，请确认管理员权限。",
                    "传感器不可用",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int gameMinutes = SelectedGameMinutes();
            string prompt = mode == TestMode.Gaming
                ? "将记录 " + gameMinutes + " 分钟游戏负载。\n\n" +
                  "开始后请立即切到游戏，保持与另一阶段相同的游戏、画质和场景。"
                : "将开始 5 分钟无负载记录。\n\n" +
                  "请关闭游戏、渲染、下载等高负载程序，并保持电脑处于日常静置状态。";
            DialogResult result = MessageBox.Show(prompt, "开始" + SessionStore.ModeLabel(mode),
                MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
                BeginTest(stage, mode, gameMinutes);
        }

        private int SelectedGameMinutes()
        {
            if (_gameDurationCombo.SelectedIndex < 0)
                return 10;
            switch (_gameDurationCombo.SelectedIndex)
            {
                case 0: return 5;
                case 2: return 15;
                case 3: return 20;
                default: return 10;
            }
        }

        private void BeginTest(MeasurementStage stage, TestMode mode, int gameMinutes)
        {
            _testActive = true;
            _activeStage = stage;
            _activeMode = mode;
            _testStarted = DateTime.Now;
            _activeTotalSeconds = mode == TestMode.Standard ? 300 : gameMinutes * 60;
            _testEnds = _testStarted.AddSeconds(_activeTotalSeconds);
            _activeWorkloadName = mode == TestMode.Standard
                ? "无负载静置"
                : (string.IsNullOrWhiteSpace(_workloadText.Text) ? "游戏负载" : _workloadText.Text.Trim());
            _idleAccumulator = new MetricAccumulator();
            _loadAccumulator = new MetricAccumulator();
            _recoveryAccumulator = new MetricAccumulator();

            try
            {
                string path = Path.Combine(
                    SessionStore.ReportsDirectory,
                    SessionStore.StageKey(stage) + "_" + SessionStore.ModeKey(mode) + "_raw_" +
                    _testStarted.ToString("yyyyMMdd_HHmmss") + "_partial.csv");
                _rawCsvPath = path;
                _rawCsv = new StreamWriter(path, false, new UTF8Encoding(true));
                _rawCsv.WriteLine("时间,阶段,CPU温度,GPU温度,GPU热点,CPU负载,GPU负载,CPU功耗,GPU功耗,风扇RPM");
                _rawCsv.Flush();
            }
            catch (Exception ex)
            {
                _testActive = false;
                MessageBox.Show("无法创建原始记录：\n" + ex.Message, "记录失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _beforeRadio.Enabled = false;
            _afterRadio.Enabled = false;
            _standardRadio.Enabled = false;
            _gamingRadio.Enabled = false;
            _workloadText.Enabled = false;
            _gameDurationCombo.Enabled = false;
            _exportButton.Enabled = false;
            _startButton.Text = mode == TestMode.Gaming ? "结束并保存" : "取消本次测试";
            _startButton.NormalColor = Theme.Red;
            _startButton.HoverColor = Color.FromArgb(255, 130, 130);
            _startButton.ForeColor = Color.White;
            if (mode == TestMode.Standard)
                EnterStandardPhase(0);
            else
            {
                _activePhase = 1;
                _phaseLabel.Text = "游戏负载记录中";
                _phaseLabel.ForeColor = Theme.Orange;
                _progressFill.BackColor = Theme.Orange;
                _progressTrack.Width = Math.Max(0, _progressTrack.Parent.ClientSize.Width);
            }
            _testTimer.Start();
        }

        private void TestTimer_Tick(object sender, EventArgs e)
        {
            if (!_testActive)
                return;
            DateTime now = DateTime.Now;
            if (now >= _testEnds)
            {
                FinishTest(false);
                return;
            }

            double totalRemaining = Math.Max(0, (_testEnds - now).TotalSeconds);
            double progress = Math.Max(0, Math.Min(1, 1 - totalRemaining / _activeTotalSeconds));
            _phaseLabel.Text = SessionStore.StageLabel(_activeStage) + " · " +
                               Shorten(_activeWorkloadName, 20);
            _timeLabel.Text = "剩余 " + FormatClock(totalRemaining);
            _progressFill.Width = (int)Math.Round(_progressTrack.ClientSize.Width * progress);
        }

        private void EnterStandardPhase(int phase)
        {
            _activePhase = phase;
            _phaseEnds = DateTime.Now.AddSeconds(_standardPhaseSeconds[phase]);
            _phaseLabel.Text = SessionStore.StageLabel(_activeStage) + " · " + _standardPhaseNames[phase];
            _phaseLabel.ForeColor = Theme.Accent;
            _progressFill.BackColor = Theme.Accent;
            _progressFill.Width = 0;
            _loadGenerator.Stop();
        }

        private void AddTestSample(SensorSnapshot snapshot)
        {
            if (_activeMode == TestMode.Standard)
            {
                if (_activePhase == 0) _idleAccumulator.Add(snapshot);
                else if (_activePhase == 1) _loadAccumulator.Add(snapshot);
                else _recoveryAccumulator.Add(snapshot);
            }
            else
            {
                _loadAccumulator.Add(snapshot);
            }

            if (_rawCsv == null)
                return;
            try
            {
                string phase = _activeMode == TestMode.Gaming
                    ? "游戏负载" : _standardPhaseNames[_activePhase];
                _rawCsv.WriteLine(
                    snapshot.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "," +
                    phase + "," + CsvNumber(snapshot.HasCpu, snapshot.CpuTemp) + "," +
                    CsvNumber(snapshot.HasGpu, snapshot.GpuTemp) + "," +
                    CsvNumber(snapshot.HasGpuHotspot, snapshot.GpuHotspot) + "," +
                    CsvNumber(snapshot.HasCpuLoad, snapshot.CpuLoad) + "," +
                    CsvNumber(snapshot.HasGpuLoad, snapshot.GpuLoad) + "," +
                    CsvNumber(snapshot.HasCpuPower, snapshot.CpuPower) + "," +
                    CsvNumber(snapshot.HasGpuPower, snapshot.GpuPower) + "," +
                    CsvNumber(snapshot.HasFan, snapshot.FanRpm));
                _rawCsv.Flush();
            }
            catch
            {
            }
        }

        private void FinishTest(bool manual)
        {
            if (!_testActive)
                return;
            _loadGenerator.Stop();
            _testTimer.Stop();
            _testActive = false;
            CloseRawCsv(false);

            SessionRecord session = new SessionRecord
            {
                Version = "2.0.0",
                Stage = SessionStore.StageKey(_activeStage),
                StageLabel = SessionStore.StageLabel(_activeStage),
                CreatedAtUtc = _testStarted.ToUniversalTime(),
                CpuName = _latest == null ? "未检测到" : _latest.CpuName,
                GpuName = _latest == null ? "未检测到" : _latest.GpuName,
                MotherboardName = _latest == null ? "未检测到" : _latest.MotherboardName,
                CpuSensorName = _latest == null ? "CPU 温度" : _latest.CpuSensorName,
                GpuSensorName = _latest == null ? "GPU 温度" : _latest.GpuSensorName,
                Mode = SessionStore.ModeKey(_activeMode),
                WorkloadName = _activeWorkloadName,
                DurationSeconds = (int)(DateTime.Now - _testStarted).TotalSeconds,
                Idle = _idleAccumulator.Build(),
                Load = _loadAccumulator.Build(),
                Recovery = _recoveryAccumulator.Build()
            };

            try
            {
                SessionStore.Save(session, _activeStage, _activeMode);
                if (!string.IsNullOrEmpty(_rawCsvPath) && File.Exists(_rawCsvPath))
                {
                    string finalPath = _rawCsvPath.Replace("_partial.csv", ".csv");
                    if (File.Exists(finalPath))
                        File.Delete(finalPath);
                    File.Move(_rawCsvPath, finalPath);
                    _rawCsvPath = finalPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("记录已完成，但保存时出错：\n" + ex.Message, "保存失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ResetTestUi();
            RefreshStoredSessions();
            MetricSummary resultSummary = _activeMode == TestMode.Standard
                ? session.Idle : session.Load;
            string summary = SessionStore.StageLabel(_activeStage) + " · " +
                             SessionStore.ModeLabel(_activeMode) + "记录完成。\n\n" +
                             (manual ? "已按你的操作提前结束。\n\n" : "") +
                             "CPU 平均温度：" + FormatTemp(resultSummary.AvgCpu) +
                             "\nGPU 平均温度：" + FormatTemp(resultSummary.AvgGpu) +
                             "\n完整数据已保存到 Reports 文件夹。";
            MessageBox.Show(summary, "记录完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CancelCurrentTest()
        {
            if (!_testActive)
                return;
            DialogResult result = MessageBox.Show(
                "取消本次标准测试？未完成的记录不会保存。",
                "取消测试",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
            _loadGenerator.Stop();
            _testTimer.Stop();
            _testActive = false;
            CloseRawCsv(true);
            ResetTestUi();
            RefreshStoredSessions();
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
            RefreshStoredSessions();
            MessageBox.Show(
                "检测到温度超过安全阈值，本次记录已停止且不会保存。\n\n" +
                "请等待电脑冷却，并检查散热器、风扇和进出风口。",
                "安全保护已触发",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ResetTestUi()
        {
            _beforeRadio.Enabled = true;
            _afterRadio.Enabled = true;
            _standardRadio.Enabled = true;
            _gamingRadio.Enabled = true;
            _workloadText.Enabled = _gamingRadio.Checked;
            _gameDurationCombo.Enabled = _gamingRadio.Checked;
            _startButton.NormalColor = Theme.Accent;
            _startButton.HoverColor = Color.FromArgb(80, 226, 247);
            _startButton.ForeColor = Color.FromArgb(8, 18, 24);
            _phaseLabel.Text = "准备就绪";
            _phaseLabel.ForeColor = Theme.Muted;
            _timeLabel.Text = "";
            _progressFill.Width = 0;
            _progressFill.BackColor = Theme.Accent;
            ApplyTestSelection();
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
                try { File.Delete(_rawCsvPath); }
                catch { }
            }
        }

        private void RefreshComparison()
        {
            if (_comparisonGrid == null)
                return;
            _comparisonGrid.SuspendLayout();
            try
            {
                _comparisonGrid.Rows.Clear();
                bool hasStandard = _standardBefore != null && _standardAfter != null;
                bool hasGaming = _gamingBefore != null && _gamingAfter != null;
                _exportButton.Enabled = (hasStandard || hasGaming) && !_testActive;

                AddPendingSection("无负载测试", hasStandard,
                    _standardBefore == null ? "清灰前未记录" :
                    (_standardAfter == null ? "清灰后未记录" : ""));
                AddMetricRow("无负载 · CPU 平均", _standardBefore, _standardAfter, true, false, "AvgCpu", "°C", true);
                AddMetricRow("无负载 · CPU 峰值", _standardBefore, _standardAfter, true, false, "MaxCpu", "°C", true);
                AddMetricRow("无负载 · GPU 平均", _standardBefore, _standardAfter, true, false, "AvgGpu", "°C", true);
                AddMetricRow("无负载 · GPU 峰值", _standardBefore, _standardAfter, true, false, "MaxGpu", "°C", true);
                AddFluctuationRow("无负载 · CPU 温度波动", _standardBefore, _standardAfter,
                    true, "CPU", _standardBefore == null ? "" : _standardBefore.CpuSensorName);
                AddFluctuationRow("无负载 · GPU 温度波动", _standardBefore, _standardAfter,
                    true, "GPU", _standardBefore == null ? "" : _standardBefore.GpuSensorName);

                AddPendingSection("游戏负载", hasGaming,
                    _gamingBefore == null ? "清灰前未记录" :
                    (_gamingAfter == null ? "清灰后未记录" : ""));
                AddMetricRow("游戏 · CPU 平均", _gamingBefore, _gamingAfter, false, false, "AvgCpu", "°C", true);
                AddMetricRow("游戏 · CPU 峰值", _gamingBefore, _gamingAfter, false, false, "MaxCpu", "°C", true);
                AddMetricRow("游戏 · GPU 平均", _gamingBefore, _gamingAfter, false, false, "AvgGpu", "°C", true);
                AddMetricRow("游戏 · GPU 峰值", _gamingBefore, _gamingAfter, false, false, "MaxGpu", "°C", true);
                AddMetricRow("游戏 · GPU 负载", _gamingBefore, _gamingAfter, false, false, "AvgGpuLoad", "%", true);
                AddMetricRow("游戏 · 风扇最高", _gamingBefore, _gamingAfter, false, false, "MaxFan", "RPM", false);
                AddMetricRow("游戏 · CPU 功耗", _gamingBefore, _gamingAfter, false, false, "AvgCpuPower", "W", true);
                AddFluctuationRow("游戏 · CPU 温度波动", _gamingBefore, _gamingAfter,
                    false, "CPU", _gamingBefore == null ? "" : _gamingBefore.CpuSensorName);
                AddFluctuationRow("游戏 · GPU 温度波动", _gamingBefore, _gamingAfter,
                    false, "GPU", _gamingBefore == null ? "" : _gamingBefore.GpuSensorName);
            }
            finally
            {
                _comparisonGrid.ResumeLayout();
                _comparisonGrid.ClearSelection();
                _comparisonGrid.CurrentCell = null;
            }

            bool standard = _standardBefore != null && _standardAfter != null;
            bool gaming = _gamingBefore != null && _gamingAfter != null;
            if (gaming && _gamingBefore.Load.HasCpu && _gamingAfter.Load.HasCpu)
            {
                double delta = _gamingAfter.Load.AvgCpu - _gamingBefore.Load.AvgCpu;
                _compareHeadline.Text = "游戏负载：清灰后 CPU 均温" +
                                        (delta <= 0 ? "下降 " : "上升 ") +
                                        Math.Abs(delta).ToString("0.0") + "°C";
                _compareHeadline.ForeColor = delta <= -2 ? Theme.Green : (delta >= 1 ? Theme.Red : Theme.Yellow);
                _compareExplanation.Text =
                    "已对比 " + Shorten(_gamingAfter.WorkloadName, 20) +
                    "；请确保前后游戏、画质和场景一致。";
            }
            else if (standard && _standardBefore.Idle.HasCpu && _standardAfter.Idle.HasCpu)
            {
                double delta = _standardAfter.Idle.AvgCpu - _standardBefore.Idle.AvgCpu;
                _compareHeadline.Text = "无负载：清灰后 CPU 均温" +
                                        (delta <= 0 ? "下降 " : "上升 ") +
                                        Math.Abs(delta).ToString("0.0") + "°C";
                _compareHeadline.ForeColor = delta <= -2 ? Theme.Green : (delta >= 1 ? Theme.Red : Theme.Yellow);
                _compareExplanation.Text =
                    "无负载记录为静置 5 分钟；负值表示清灰后更低。";
            }
            else
            {
                _compareHeadline.Text = "继续完成清灰另一阶段的记录";
                _compareHeadline.ForeColor = Theme.Muted;
                _compareExplanation.Text =
                    "无负载记录提供静置温度；游戏记录提供实际使用温度。两类记录均按清灰前后对比。";
            }
        }

        private void AddPendingSection(string name, bool complete, string pending)
        {
            int index = _comparisonGrid.Rows.Add(name, complete ? "已完成" : pending, "", "", "");
            DataGridViewRow row = _comparisonGrid.Rows[index];
            row.DefaultCellStyle.BackColor = Theme.CardAlt;
            row.Cells[0].Style.ForeColor = complete ? Theme.Accent : Theme.Yellow;
            row.Cells[1].Style.ForeColor = complete ? Theme.Green : Theme.Dim;
        }

        private void AddMetricRow(
            string label,
            SessionRecord before,
            SessionRecord after,
            bool idle,
            bool unused,
            string field,
            string unit,
            bool lowerIsBetter)
        {
            if (before == null || after == null)
            {
                int pending = _comparisonGrid.Rows.Add(label, "--", "--", "--", "待记录");
                _comparisonGrid.Rows[pending].DefaultCellStyle.ForeColor = Theme.Dim;
                return;
            }
            MetricSummary a = idle ? before.Idle : before.Load;
            MetricSummary b = idle ? after.Idle : after.Load;
            bool hasA = HasMetric(a, field);
            bool hasB = HasMetric(b, field);
            if (!hasA || !hasB)
            {
                int missing = _comparisonGrid.Rows.Add(label, hasA ? "有" : "--", hasB ? "有" : "--", "--", "传感器不可用");
                _comparisonGrid.Rows[missing].DefaultCellStyle.ForeColor = Theme.Dim;
                return;
            }
            double va = GetMetric(a, field);
            double vb = GetMetric(b, field);
            double delta = vb - va;
            string result = Math.Abs(delta) < 0.05 ? "持平" :
                ((lowerIsBetter ? delta < 0 : delta > 0) ? "改善" : "升高");
            int index = _comparisonGrid.Rows.Add(
                label,
                FormatMetric(va, unit),
                FormatMetric(vb, unit),
                (delta > 0 ? "+" : "") + delta.ToString("0.0", CultureInfo.InvariantCulture) + " " + unit,
                result);
            DataGridViewRow row = _comparisonGrid.Rows[index];
            row.Cells[3].Style.ForeColor = Math.Abs(delta) < 0.05
                ? Theme.Muted
                : ((lowerIsBetter ? delta < 0 : delta > 0) ? Theme.Green : Theme.Red);
            row.Cells[4].Style.ForeColor = row.Cells[3].Style.ForeColor;
        }

        private void AddFluctuationRow(
            string label,
            SessionRecord before,
            SessionRecord after,
            bool idle,
            string group,
            string preferredSensor)
        {
            if (before == null || after == null)
            {
                int pending = _comparisonGrid.Rows.Add(label, "--", "--", "--", "待记录");
                _comparisonGrid.Rows[pending].DefaultCellStyle.ForeColor = Theme.Dim;
                return;
            }
            MetricSummary beforeSummary = idle ? before.Idle : before.Load;
            MetricSummary afterSummary = idle ? after.Idle : after.Load;
            SensorStat beforeStat = FindTemperatureStat(beforeSummary, group, preferredSensor);
            SensorStat afterStat = FindTemperatureStat(afterSummary, group, preferredSensor);
            if (beforeStat == null || afterStat == null)
            {
                int missing = _comparisonGrid.Rows.Add(label, "--", "--", "--", "无波动数据");
                _comparisonGrid.Rows[missing].DefaultCellStyle.ForeColor = Theme.Dim;
                return;
            }
            double delta = afterStat.StdDev - beforeStat.StdDev;
            string result = delta <= -0.3 ? "更平稳" :
                (delta >= 0.3 ? "波动增加" : "基本一致");
            int index = _comparisonGrid.Rows.Add(
                label,
                "±" + beforeStat.StdDev.ToString("0.0") + "°C",
                "±" + afterStat.StdDev.ToString("0.0") + "°C",
                (delta > 0 ? "+" : "") + delta.ToString("0.0") + "°C",
                result);
            DataGridViewRow row = _comparisonGrid.Rows[index];
            row.Cells[3].Style.ForeColor = delta <= -0.3
                ? Theme.Green : (delta >= 0.3 ? Theme.Red : Theme.Muted);
            row.Cells[4].Style.ForeColor = row.Cells[3].Style.ForeColor;
        }

        private static SensorStat FindTemperatureStat(
            MetricSummary summary, string group, string preferredSensor)
        {
            if (summary == null || summary.SensorStats == null)
                return null;
            List<SensorStat> temperatures = summary.SensorStats
                .Where(stat => stat.Group == group && stat.Type == "温度")
                .ToList();
            SensorStat preferred = temperatures.FirstOrDefault(stat =>
                string.Equals(stat.Name, preferredSensor, StringComparison.OrdinalIgnoreCase));
            if (preferred != null)
                return preferred;
            return temperatures.OrderByDescending(stat => stat.Avg).FirstOrDefault();
        }

        private static bool HasMetric(MetricSummary summary, string field)
        {
            if (summary == null) return false;
            if (field == "AvgCpuPower") return summary.HasCpuPower;
            if (field == "AvgGpuLoad") return summary.HasGpuLoad;
            if (field == "MaxFan") return summary.HasFan;
            if (field == "AvgGpu" || field == "MaxGpu") return summary.HasGpu;
            return summary.HasCpu;
        }

        private static double GetMetric(MetricSummary summary, string field)
        {
            if (field == "AvgCpu") return summary.AvgCpu;
            if (field == "MaxCpu") return summary.MaxCpu;
            if (field == "AvgGpu") return summary.AvgGpu;
            if (field == "MaxGpu") return summary.MaxGpu;
            if (field == "AvgGpuLoad") return summary.AvgGpuLoad;
            if (field == "AvgCpuPower") return summary.AvgCpuPower;
            if (field == "MaxFan") return summary.MaxFan;
            return 0;
        }

        private static string FormatMetric(double value, string unit)
        {
            string format = unit == "%" || unit == "W" || unit == "RPM" ? "0" : "0.0";
            return value.ToString(format, CultureInfo.InvariantCulture) + " " + unit;
        }

        private void ExportComparisonReport()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "导出完整清灰前后对比报告";
                dialog.Filter = "CSV 报告 (*.csv)|*.csv";
                dialog.InitialDirectory = SessionStore.ReportsDirectory;
                dialog.FileName = "清灰温度完整对比_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    StringBuilder report = new StringBuilder();
                    report.AppendLine("硬件监测中心 - 清灰前后完整对比");
                    report.AppendLine("导出时间," + Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    report.AppendLine();
                    AppendSessionReport(report, "无负载 · 清灰前", _standardBefore);
                    AppendSessionReport(report, "无负载 · 清灰后", _standardAfter);
                    AppendSessionReport(report, "游戏负载 · 清灰前", _gamingBefore);
                    AppendSessionReport(report, "游戏负载 · 清灰后", _gamingAfter);
                    report.AppendLine();
                    report.AppendLine("对比表");
                    foreach (DataGridViewRow row in _comparisonGrid.Rows)
                    {
                        report.Append(Csv(Convert.ToString(row.Cells[0].Value))).Append(",");
                        report.Append(Csv(Convert.ToString(row.Cells[1].Value))).Append(",");
                        report.Append(Csv(Convert.ToString(row.Cells[2].Value))).Append(",");
                        report.Append(Csv(Convert.ToString(row.Cells[3].Value))).Append(",");
                        report.AppendLine(Csv(Convert.ToString(row.Cells[4].Value)));
                    }
                    File.WriteAllText(dialog.FileName, report.ToString(), new UTF8Encoding(true));
                    MessageBox.Show("完整对比报告已导出：\n" + dialog.FileName,
                        "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导出失败：\n" + ex.Message, "导出失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void AppendSessionReport(StringBuilder report, string label, SessionRecord session)
        {
            if (session == null)
            {
                report.AppendLine(label + ",未记录");
                return;
            }
            report.AppendLine();
            report.AppendLine(Csv(label));
            report.AppendLine("记录时间," + Csv(session.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")));
            report.AppendLine("负载名称," + Csv(session.WorkloadName));
            report.AppendLine("CPU," + Csv(session.CpuName) + ",GPU," + Csv(session.GpuName));
            report.AppendLine("阶段,样本数,CPU平均°C,CPU最高°C,GPU平均°C,GPU最高°C,CPU负载%,GPU负载%,CPU功耗W,GPU功耗W,风扇最高RPM");
            AppendMetric(report, "静置", session.Idle);
            AppendMetric(report, "负载", session.Load);
            AppendMetric(report, "恢复", session.Recovery);
            if (session.Load != null && session.Load.SensorStats != null)
            {
                report.AppendLine();
                report.AppendLine("负载阶段全部传感器");
                report.AppendLine("设备,分组,传感器,类型,最小,平均,最大,标准差,单位,样本");
                foreach (SensorStat stat in session.Load.SensorStats)
                {
                    report.Append(Csv(stat.Hardware)).Append(",");
                    report.Append(Csv(stat.Group)).Append(",");
                    report.Append(Csv(stat.Name)).Append(",");
                    report.Append(Csv(stat.Type)).Append(",");
                    report.Append(stat.Min.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                    report.Append(stat.Avg.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                    report.Append(stat.Max.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                    report.Append(stat.StdDev.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                    report.Append(Csv(stat.Unit)).Append(",");
                    report.AppendLine(stat.Samples.ToString());
                }
            }
        }

        private static void AppendMetric(StringBuilder report, string label, MetricSummary summary)
        {
            if (summary == null)
                return;
            report.Append(Csv(label)).Append(",");
            report.Append(summary.Samples).Append(",");
            report.Append(Number(summary.HasCpu, summary.AvgCpu)).Append(",");
            report.Append(Number(summary.HasCpu, summary.MaxCpu)).Append(",");
            report.Append(Number(summary.HasGpu, summary.AvgGpu)).Append(",");
            report.Append(Number(summary.HasGpu, summary.MaxGpu)).Append(",");
            report.Append(Number(summary.HasCpuLoad, summary.AvgCpuLoad)).Append(",");
            report.Append(Number(summary.HasGpuLoad, summary.AvgGpuLoad)).Append(",");
            report.Append(Number(summary.HasCpuPower, summary.AvgCpuPower)).Append(",");
            report.Append(Number(summary.HasGpuPower, summary.AvgGpuPower)).Append(",");
            report.AppendLine(Number(summary.HasFan, summary.MaxFan));
        }

        private void ExportCurrentSensors()
        {
            if (_latest == null)
                return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "导出当前全部传感器";
                dialog.Filter = "CSV 文件 (*.csv)|*.csv";
                dialog.InitialDirectory = SessionStore.ReportsDirectory;
                dialog.FileName = "传感器快照_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    StringBuilder text = new StringBuilder();
                    text.AppendLine("设备,分组,传感器,类型,当前值,最小值,平均值,最大值,近期波动标准差,单位,样本数");
                    foreach (LiveSensor sensor in _latest.Rows)
                    {
                        LiveSensorStat stat;
                        if (!_liveStats.TryGetValue(sensor.Identifier, out stat))
                            continue;
                        text.Append(Csv(sensor.HardwareName)).Append(",");
                        text.Append(Csv(sensor.Group)).Append(",");
                        text.Append(Csv(sensor.Name)).Append(",");
                        text.Append(Csv(SensorFormat.Kind(sensor.Type))).Append(",");
                        text.Append(sensor.Value.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                        text.Append(stat.Min.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                        text.Append((stat.Sum / Math.Max(1, stat.Count)).ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                        text.Append(stat.Max.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                        text.Append(stat.StdDev.ToString("0.000", CultureInfo.InvariantCulture)).Append(",");
                        text.Append(Csv(sensor.Unit)).Append(",");
                        text.AppendLine(stat.Count.ToString());
                    }
                    File.WriteAllText(dialog.FileName, text.ToString(), new UTF8Encoding(true));
                    MessageBox.Show("当前传感器数据已导出：\n" + dialog.FileName, "导出完成",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导出失败：\n" + ex.Message, "导出失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string Number(bool valid, double value)
        {
            return valid ? value.ToString("0.0", CultureInfo.InvariantCulture) : "";
        }

        private static string CsvNumber(bool valid, double value)
        {
            return valid ? value.ToString("0.0", CultureInfo.InvariantCulture) : "";
        }

        private static string Csv(string value)
        {
            string text = value ?? "";
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private static string FormatClock(double seconds)
        {
            int total = Math.Max(0, (int)Math.Ceiling(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static string FormatTemp(double value)
        {
            return value > 0 ? value.ToString("0.0") + " °C" : "--";
        }

        private static string Shorten(string value, int limit)
        {
            if (string.IsNullOrEmpty(value))
                return "未检测到";
            return value.Length <= limit ? value : value.Substring(0, limit - 1) + "…";
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, string lParam);

        private void OpenReportsFolder()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe",
                    "\"" + SessionStore.ReportsDirectory + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开记录文件夹：\n" + ex.Message, "打开失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_testActive || _closingAfterPrompt)
                return;
            DialogResult result = MessageBox.Show(
                "当前记录尚未结束。退出会中止本次记录并删除未完成文件。\n\n确定退出吗？",
                "记录进行中",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            _closingAfterPrompt = true;
            _loadGenerator.Stop();
            _testTimer.Stop();
            _testActive = false;
            CloseRawCsv(true);
        }

        private sealed class ChartChoice
        {
            public string Identifier;
            public string Text;
            public override string ToString()
            {
                return Text;
            }
        }

        private sealed class LiveSensorStat
        {
            public LiveSensor Last;
            public double PreviousValue;
            public double Min;
            public double Max;
            public double Sum;
            public int Count;
            public List<double> Recent;
            public double StdDev;
            public double Swing;

            public void RecalculateFluctuation()
            {
                if (Recent == null || Recent.Count < 2)
                {
                    StdDev = 0;
                    Swing = 0;
                    return;
                }
                double sum = 0;
                double sumSquares = 0;
                double min = double.MaxValue;
                double max = double.MinValue;
                for (int i = 0; i < Recent.Count; i++)
                {
                    double value = Recent[i];
                    sum += value;
                    sumSquares += value * value;
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
                double mean = sum / Recent.Count;
                StdDev = Math.Sqrt(Math.Max(0, sumSquares / Recent.Count - mean * mean));
                Swing = max - min;
            }
        }
    }

    internal sealed class MultiSensorChart : Control
    {
        private string _primaryId;
        private string _primaryLabel = "";
        private string _secondaryId;
        private string _secondaryLabel = "";
        private readonly List<ChartSample> _samples = new List<ChartSample>();

        public MultiSensorChart()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }

        public void SetSensors(string primaryId, string primaryLabel, string secondaryId, string secondaryLabel)
        {
            bool changed = primaryId != _primaryId || secondaryId != _secondaryId;
            _primaryId = primaryId;
            _primaryLabel = primaryLabel;
            _secondaryId = secondaryId;
            _secondaryLabel = secondaryLabel;
            if (changed)
                _samples.Clear();
            Invalidate();
        }

        public void Add(SensorSnapshot snapshot)
        {
            if (_primaryId == null || _secondaryId == null || snapshot.Rows == null)
                return;
            double primary = FindValue(snapshot.Rows, _primaryId);
            double secondary = FindValue(snapshot.Rows, _secondaryId);
            _samples.Add(new ChartSample
            {
                Time = snapshot.Timestamp,
                Primary = primary,
                Secondary = secondary
            });
            while (_samples.Count > 300)
                _samples.RemoveAt(0);
            Invalidate();
        }

        private static double FindValue(List<LiveSensor> rows, string identifier)
        {
            LiveSensor found = rows.FirstOrDefault(s => s.Identifier == identifier);
            return found == null ? double.NaN : found.Value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath background = DrawingHelpers.RoundedRectangle(bounds, 7))
            using (SolidBrush fill = new SolidBrush(Theme.CardAlt))
                e.Graphics.FillPath(fill, background);
            using (Font small = Theme.Font(7.6f, FontStyle.Regular))
            using (SolidBrush text = new SolidBrush(Theme.Text))
            using (SolidBrush muted = new SolidBrush(Theme.Muted))
            using (Pen grid = new Pen(Theme.Grid))
            {
                grid.DashStyle = DashStyle.Dot;
                DrawLegend(e.Graphics, small, text, 12, 11, Theme.Accent, _primaryLabel);
                DrawLegend(e.Graphics, small, text, Math.Min(Width / 2, 220), 11, Theme.Orange, _secondaryLabel);
                Rectangle plot = new Rectangle(52, 43, Math.Max(80, Width - 70), Math.Max(80, Height - 68));
                List<double> values = _samples
                    .SelectMany(s => new[] { s.Primary, s.Secondary })
                    .Where(v => !double.IsNaN(v))
                    .ToList();
                double min = 0;
                double max = 100;
                if (values.Count > 0)
                {
                    double observedMin = values.Min();
                    double observedMax = values.Max();
                    if (observedMax <= 5)
                    {
                        min = 0;
                        max = 5;
                    }
                    else
                    {
                        double padding = Math.Max(2, (observedMax - observedMin) * 0.12);
                        min = Math.Max(0, observedMin - padding);
                        max = observedMax + padding;
                    }
                }
                for (int i = 0; i <= 4; i++)
                {
                    int y = plot.Bottom - plot.Height * i / 4;
                    e.Graphics.DrawLine(grid, plot.Left, y, plot.Right, y);
                    double labelValue = min + (max - min) * i / 4;
                    e.Graphics.DrawString(labelValue.ToString("0.#"), small, muted, 10, y - 7);
                }
                for (int i = 0; i <= 4; i++)
                {
                    int x = plot.Left + plot.Width * i / 4;
                    e.Graphics.DrawLine(grid, x, plot.Top, x, plot.Bottom);
                }
                if (_samples.Count < 2)
                {
                    e.Graphics.DrawString("等待趋势数据…", small, muted,
                        new RectangleF(plot.Left, plot.Top, plot.Width, plot.Height),
                        CenteredFormat());
                    return;
                }
                DrawSeries(e.Graphics, plot, false, min, max, Theme.Accent);
                DrawSeries(e.Graphics, plot, true, min, max, Theme.Orange);
                e.Graphics.DrawString("5 分钟前", small, muted, plot.Left, plot.Bottom + 7);
                e.Graphics.DrawString("现在", small, muted, plot.Right - 24, plot.Bottom + 7);
            }
        }

        private void DrawSeries(Graphics graphics, Rectangle plot, bool secondary, double min, double max, Color color)
        {
            List<PointF> points = new List<PointF>();
            int count = Math.Max(1, _samples.Count);
            for (int i = 0; i < _samples.Count; i++)
            {
                double raw = secondary ? _samples[i].Secondary : _samples[i].Primary;
                if (double.IsNaN(raw))
                    continue;
                float x = plot.Right - plot.Width * (count - 1 - i) / 299f;
                if (x < plot.Left)
                    continue;
                double normalized = Math.Max(0, Math.Min(1, (raw - min) / Math.Max(0.001, max - min)));
                float y = plot.Bottom - (float)(normalized * plot.Height);
                points.Add(new PointF(x, y));
            }
            if (points.Count < 2)
                return;
            using (Pen pen = new Pen(color, 2f))
            {
                pen.LineJoin = LineJoin.Round;
                graphics.DrawLines(pen, points.ToArray());
            }
            using (SolidBrush dot = new SolidBrush(color))
            {
                PointF last = points[points.Count - 1];
                graphics.FillEllipse(dot, last.X - 3, last.Y - 3, 6, 6);
            }
        }

        private static void DrawLegend(Graphics graphics, Font font, Brush textBrush,
            float x, float y, Color color, string label)
        {
            using (SolidBrush dot = new SolidBrush(color))
                graphics.FillEllipse(dot, x, y + 4, 6, 6);
            graphics.DrawString(string.IsNullOrEmpty(label) ? "未选择" : label,
                font, textBrush, x + 10, y);
        }

        private static StringFormat CenteredFormat()
        {
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            return format;
        }

        private sealed class ChartSample
        {
            public DateTime Time;
            public double Primary;
            public double Secondary;
        }
    }
}
