using FFBoost.Core.Models;
using FFBoost.Core.Rules;
using FFBoost.Core.Services;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FFBoost.UI;

public class MainForm : Form
{
    private const int HotkeyIdToggle = 9001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint WmHotkey = 0x0312;

    private readonly OptimizerService _optimizer;
    private readonly AdminService _adminService;
    private readonly ConfigService _configService;
    private readonly StartupService _startupService;
    private readonly OptimizationCoordinatorService _optimizationCoordinator;
    private readonly SystemMetricsService _metricsService;
    private readonly LogFileService _logFileService;
    private readonly TelemetryService _telemetryService;
    private readonly GameWatcherService _watcherService;
    private readonly ProcessAnalyzerService _processAnalyzerService;
    private NeonPanel _profileFrame = null!;
    private readonly Label _lblStatus;
    private readonly Label _lblAdminStatus;
    private readonly Label _lblSubtitle;
    private readonly Label _lblTagline;
    private readonly Label _lblCpuInfo;
    private readonly Label _lblRamInfo;
    private readonly Label _lblBenchmarkInfo;
    private readonly Label _lblRecommendation;
    private readonly Label _lblModeNotice;
    private readonly Label _lblModeBadge;
    private readonly RamPulseButton _ramPulseButton;
    private readonly ListBox _lstLogs;
    private readonly ComboBox _cmbProfile;
    private readonly CheckBox _chkFreeFireMode;
    private readonly CheckBox _chkTurboMode;
    private readonly Button _btnOptimize;
    private readonly Button _btnRestore;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _trayStartupItem;
    private readonly ToolStripMenuItem _trayFreeFireItem;
    private readonly ToolStripMenuItem _trayTurboItem;
    private readonly PictureBox _logoBox;
    private readonly System.Windows.Forms.Timer _dashboardTimer;
    private Icon? _appIcon;
    private Icon? _trayRamIcon;
    private Image? _logoImage;
    private bool _exitRequested;
    private bool _isUiOperationRunning;
    private bool _ramOptimizationRunning;
    private bool _startupInitializationCompleted;
    private bool _trayTransitionInProgress;
    private bool _isInTrayMode;
    private bool _watcherTriggeredChange;
    private readonly bool _startInTrayRequested;

    public MainForm(
        OptimizerService optimizer,
        AdminService adminService,
        ConfigService configService,
        StartupService startupService,
        SystemMetricsService metricsService,
        LogFileService logFileService,
        TelemetryService telemetryService,
        OptimizationCoordinatorService optimizationCoordinator,
        GameWatcherService watcherService,
        ProcessAnalyzerService processAnalyzerService,
        bool startInTrayRequested = false)
    {
        Text = "FF Boost";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(668, 892);
        MinimumSize = new Size(668, 892);
        BackColor = Color.FromArgb(4, 8, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;
        _appIcon = LoadAppIcon();
        if (_appIcon != null)
            Icon = _appIcon;

        _optimizer = optimizer;
        _adminService = adminService;
        _configService = configService;
        _startupService = startupService;
        _metricsService = metricsService;
        _logFileService = logFileService;
        _telemetryService = telemetryService;
        _optimizationCoordinator = optimizationCoordinator;
        _watcherService = watcherService;
        _processAnalyzerService = processAnalyzerService;
        _startInTrayRequested = startInTrayRequested;

        _logoBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            SizeMode = PictureBoxSizeMode.StretchImage
        };

        _lblSubtitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "MORE FPS. LESS LAG.",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(245, 247, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
            Visible = false
        };

        _lblTagline = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "OPTIMIZATION ENGINE FOR LOW LATENCY",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(111, 207, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            Visible = false
        };

        var btnAbout = CreateGhostButton("Sobre", 88, Color.FromArgb(65, 167, 255));
        btnAbout.Click += BtnAbout_Click;

        var btnAutoProfile = CreateGhostButton("Perfil Auto", 108, Color.FromArgb(255, 146, 72));
        btnAutoProfile.Click += (_, _) => ApplyRecommendedProfile();

        var btnClearLog = CreateGhostButton("Limpar Log", 104, Color.FromArgb(65, 167, 255));

        var btnAllowedApps = CreateFrameButton("Apps Permitidos", 168, 44, Color.FromArgb(43, 184, 255), Color.FromArgb(8, 16, 34));
        btnAllowedApps.Click += BtnAllowedApps_Click;

        var btnTopProcesses = CreateFrameButton("Top Processos", 168, 44, Color.FromArgb(255, 156, 72), Color.FromArgb(24, 16, 8));
        btnTopProcesses.Click += (_, _) => OpenTopProcesses();

        _btnRestore = CreateFrameButton("Restaurar", 168, 44, Color.FromArgb(255, 92, 92), Color.FromArgb(24, 10, 22));
        _btnRestore.Click += async (_, _) => await ExecuteRestoreAsync(manualTrigger: true);

        _btnOptimize = CreatePrimaryButton();
        _btnOptimize.Click += async (_, _) => await ExecuteOptimizeAsync(manualTrigger: true);

        _ramPulseButton = new RamPulseButton
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            AccentColor = Color.FromArgb(60, 235, 126),
            FooterText = "Clique para otimizar RAM"
        };
        _ramPulseButton.Click += async (_, _) => await ExecuteMemoryOptimizeAsync();

        _cmbProfile = new ComboBox
        {
            Width = 172,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(10, 18, 36),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point)
        };
        _cmbProfile.DropDownWidth = 172;
        _cmbProfile.SelectedIndexChanged += CmbProfile_SelectedIndexChanged;
        _cmbProfile.Enter += (_, _) => SetProfileFocusState(true);
        _cmbProfile.Leave += (_, _) => SetProfileFocusState(false);

        _chkFreeFireMode = new CheckBox
        {
            AutoSize = true,
            Text = "Preset Free Fire + BlueStacks",
            ForeColor = Color.FromArgb(255, 166, 113),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand,
            Visible = false
        };
        _chkFreeFireMode.CheckedChanged += ChkFreeFireMode_CheckedChanged;

        _chkTurboMode = new CheckBox
        {
            AutoSize = true,
            Text = "Turbo FPS",
            ForeColor = Color.FromArgb(111, 207, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        _chkTurboMode.CheckedChanged += ChkTurboMode_CheckedChanged;

        _lblAdminStatus = new Label
        {
            AutoSize = false,
            Height = 24,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(255, 209, 110),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        _lblCpuInfo = CreateMetricValueLabel("CPU: -- -> --");
        _lblRamInfo = CreateMetricValueLabel("RAM: -- -> --");

        _lblBenchmarkInfo = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(132, 208, 255),
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "Benchmark: aguardando nova analise."
        };

        _lblModeBadge = new Label
        {
            AutoSize = false,
            Height = 28,
            Width = 220,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(255, 244, 230),
            BackColor = Color.FromArgb(46, 16, 8),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
            Visible = false
        };

        _lblRecommendation = new Label
        {
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = Color.FromArgb(255, 179, 111),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "Recomendacao: analisando historico local."
        };

        _lblModeNotice = new Label
        {
            Dock = DockStyle.Top,
            Height = 42,
            ForeColor = Color.FromArgb(255, 205, 122),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
            Text = string.Empty,
            Visible = false
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(234, 241, 255),
            Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "Pronto para iniciar a otimizacao.",
            TextAlign = ContentAlignment.TopLeft
        };

        _lstLogs = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(7, 10, 22),
            ForeColor = Color.FromArgb(225, 231, 244),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 10.25F, FontStyle.Regular, GraphicsUnit.Point),
            IntegralHeight = false
        };
        btnClearLog.Click += (_, _) => _lstLogs.Items.Clear();

        var root = new SciFiPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 10, 16, 16),
            BorderGlowLeft = Color.FromArgb(40, 88, 255),
            BorderGlowRight = Color.FromArgb(255, 102, 68),
            UseCutCorners = true,
            ShowCornerNotches = true,
            CutTopLeft = 10,
            CutTopRight = 24,
            CutBottomRight = 18,
            CutBottomLeft = 24
        };

        var topBar = BuildTopBar(btnAbout, btnAutoProfile, btnClearLog);
        var heroCard = BuildHeroCard();
        var profileCard = BuildProfileCard();
        var optimizeCard = BuildOptimizeCard();
        var actionCard = BuildActionCard(btnAllowedApps, btnTopProcesses);
        var infoCard = BuildInfoCard();
        var metricsCard = BuildMetricsCard();
        var logsCard = BuildLogsCard();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 182F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));

        layout.Controls.Add(heroCard, 0, 0);
        layout.Controls.Add(profileCard, 0, 1);
        layout.Controls.Add(optimizeCard, 0, 2);
        layout.Controls.Add(actionCard, 0, 3);
        layout.Controls.Add(infoCard, 0, 4);
        layout.Controls.Add(metricsCard, 0, 5);

        var mainStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        mainStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        mainStack.Controls.Add(topBar, 0, 0);
        mainStack.Controls.Add(layout, 0, 1);
        mainStack.Controls.Add(logsCard, 0, 2);

        root.Controls.Add(mainStack);
        Controls.Add(root);

        (_trayIcon, _trayStartupItem, _trayFreeFireItem, _trayTurboItem) = BuildTrayIcon();
        _dashboardTimer = new System.Windows.Forms.Timer { Interval = 1200 };
        _dashboardTimer.Tick += (_, _) => RefreshLiveMetrics();
        HandleCreated += (_, _) => WindowEffects.ApplyPreferredWindowChrome(Handle, preferRoundedCorners: false);
        Shown += MainForm_Shown;
        Resize += MainForm_Resize;
        FormClosing += MainForm_FormClosing;
    }

    private Panel BuildTopBar(Button btnAbout, Button btnAutoProfile, Button btnClearLog)
    {
        var title = new Label
        {
            Text = "FF BOOST CONTROL",
            AutoSize = true,
            ForeColor = Color.FromArgb(238, 243, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(0, 6)
        };

        var subtitle = new Label
        {
            Text = "Perfis, diagnostico e automacao",
            AutoSize = true,
            ForeColor = Color.FromArgb(126, 182, 214),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.75F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(0, 26)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };
        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(btnAbout);
        panel.Controls.Add(btnAutoProfile);
        panel.Controls.Add(btnClearLog);
        panel.Resize += (_, _) =>
        {
            btnAbout.Location = new Point(Math.Max(0, panel.Width - btnAbout.Width), 6);
            btnAutoProfile.Location = new Point(Math.Max(0, btnAbout.Left - btnAutoProfile.Width - 8), 6);
            btnClearLog.Location = new Point(Math.Max(0, btnAutoProfile.Left - btnClearLog.Width - 8), 6);
        };
        return panel;
    }

    private Control BuildHeroCard()
    {
        var card = CreateCard(Color.FromArgb(20, 43, 98), Color.FromArgb(255, 114, 68), new Padding(0));
        card.Margin = new Padding(0, 0, 0, 6);

        var inner = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        inner.Controls.Add(_logoBox);
        inner.Controls.Add(_lblModeBadge);
        inner.Resize += (_, _) =>
        {
            _lblModeBadge.Location = new Point(
                Math.Max(12, inner.Width - _lblModeBadge.Width - 18),
                12);
        };
        card.Controls.Add(inner);
        return card;
    }

    private Control BuildProfileCard()
    {
        _profileFrame = new NeonPanel
        {
            Size = new Size(470, 40),
            BackColor = Color.Transparent,
            BorderColor = Color.FromArgb(65, 167, 255),
            FillTop = Color.FromArgb(11, 18, 38),
            FillBottom = Color.FromArgb(7, 12, 26),
            Padding = new Padding(12, 4, 12, 4)
        };

        var label = new Label
        {
            Text = "Perfil",
            Dock = DockStyle.Left,
            Width = 96,
            ForeColor = Color.FromArgb(88, 205, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 11.5F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var comboHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        comboHost.Controls.Add(_cmbProfile);
        comboHost.Resize += (_, _) =>
        {
            var desiredWidth = Math.Min(172, Math.Max(148, comboHost.Width - 108));
            _cmbProfile.Width = desiredWidth;
            _cmbProfile.DropDownWidth = desiredWidth;
            _cmbProfile.Location = new Point(
                Math.Max(0, comboHost.Width - desiredWidth - 16),
                Math.Max(0, (comboHost.Height - _cmbProfile.Height) / 2));
        };

        _profileFrame.Controls.Add(comboHost);
        _profileFrame.Controls.Add(label);

        var optionsRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        optionsRow.Controls.Add(_chkTurboMode);
        optionsRow.Controls.Add(_chkFreeFireMode);

        var stack = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.Controls.Add(_profileFrame, 0, 0);
        stack.Controls.Add(optionsRow, 0, 1);

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        host.Controls.Add(stack);
        host.Resize += (_, _) =>
        {
            stack.Location = new Point(
                Math.Max(0, (host.Width - stack.Width) / 2),
                2);
        };

        return host;
    }

    private Control BuildOptimizeCard()
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        host.Controls.Add(_btnOptimize);
        host.Resize += (_, _) =>
        {
            _btnOptimize.Location = new Point(
                Math.Max(0, (host.Width - _btnOptimize.Width) / 2),
                6);
        };
        return host;
    }

    private Control BuildActionCard(Button btnAllowedApps, Button btnTopProcesses)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        host.Controls.Add(btnAllowedApps);
        host.Controls.Add(btnTopProcesses);
        host.Controls.Add(_btnRestore);
        host.Resize += (_, _) =>
        {
            const int spacing = 12;
            var totalWidth = btnAllowedApps.Width + btnTopProcesses.Width + _btnRestore.Width + (spacing * 2);
            var startX = Math.Max(0, (host.Width - totalWidth) / 2);
            btnAllowedApps.Location = new Point(startX, 6);
            btnTopProcesses.Location = new Point(startX + btnAllowedApps.Width + spacing, 6);
            _btnRestore.Location = new Point(btnTopProcesses.Right + spacing, 6);
        };
        return host;
    }

    private Control BuildMetricsCard()
    {
        var metricsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8),
            ColumnStyles =
            {
                new ColumnStyle(SizeType.Percent, 30F),
                new ColumnStyle(SizeType.Percent, 40F),
                new ColumnStyle(SizeType.Percent, 30F)
            }
        };

        metricsLayout.Controls.Add(CreateMetricCard("CPU", _lblCpuInfo, Color.FromArgb(54, 196, 255)), 0, 0);
        metricsLayout.Controls.Add(CreateRamPulseCard(), 1, 0);
        metricsLayout.Controls.Add(CreateMetricCard("RAM", _lblRamInfo, Color.FromArgb(90, 194, 255)), 2, 0);
        return metricsLayout;
    }

    private Control BuildInfoCard()
    {
        var card = CreateCard(Color.FromArgb(38, 84, 178), Color.FromArgb(64, 142, 255), new Padding(12));
        var infoGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _lblRecommendation.Dock = DockStyle.Fill;
        _lblModeNotice.Dock = DockStyle.Fill;
        _lblModeNotice.AutoEllipsis = false;
        _lblBenchmarkInfo.Dock = DockStyle.Fill;
        _lblAdminStatus.Dock = DockStyle.Fill;
        _lblStatus.Dock = DockStyle.Fill;

        infoGrid.Controls.Add(_lblAdminStatus, 0, 0);
        infoGrid.Controls.Add(_lblBenchmarkInfo, 0, 1);
        infoGrid.Controls.Add(_lblModeNotice, 0, 2);
        infoGrid.Controls.Add(_lblRecommendation, 0, 3);
        infoGrid.Controls.Add(_lblStatus, 0, 4);

        card.Controls.Add(infoGrid);
        return card;
    }

    private Control BuildLogsCard()
    {
        var card = CreateCard(Color.FromArgb(20, 44, 102), Color.FromArgb(19, 41, 94), new Padding(10, 8, 10, 10));
        card.Margin = new Padding(0, 0, 0, 0);
        
        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Text = "LOG DE EXECUCAO",
            ForeColor = Color.FromArgb(108, 216, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point)
        };

        card.Controls.Add(_lstLogs);
        card.Controls.Add(titleLabel);
        return card;
    }

    private Control CreateMetricCard(string title, Label valueLabel, Color accent)
    {
        var card = CreateCard(accent, accent, new Padding(12, 8, 12, 8));
        card.Margin = new Padding(0, 0, 0, 0);

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = $"{title}:",
            ForeColor = accent,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point)
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;

        content.Controls.Add(titleLabel, 0, 0);
        content.Controls.Add(valueLabel, 0, 1);
        card.Controls.Add(content);
        return card;
    }

    private Control CreateRamPulseCard()
    {
        var card = CreateCard(Color.FromArgb(28, 138, 78), Color.FromArgb(60, 235, 126), new Padding(12, 8, 12, 8));
        card.Margin = new Padding(12, 0, 12, 0);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "AUTO RAM",
            ForeColor = Color.FromArgb(127, 255, 186),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleCenter
        };

        content.Controls.Add(titleLabel, 0, 0);
        content.Controls.Add(_ramPulseButton, 0, 1);
        card.Controls.Add(content);
        return card;
    }

    private static Label CreateMetricValueLabel(string text) =>
        new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = Color.FromArgb(82, 211, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        };

    private static Button CreatePrimaryButton()
    {
        var button = new Button
        {
            Text = "Otimizar Agora",
            Width = 404,
            Height = 50,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(23, 185, 255),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 15.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(149, 232, 255);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 198, 255);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(18, 158, 224);
        return button;
    }

    private static Button CreateFrameButton(string text, int width, int height, Color borderColor, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = Color.FromArgb(236, 242, 255),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = borderColor;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Light(backColor, 0.18F);
        return button;
    }

    private static Button CreateGhostButton(string text, int width, Color borderColor)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(10, 16, 32),
            ForeColor = Color.FromArgb(221, 231, 255),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold, GraphicsUnit.Point)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = borderColor;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 26, 46);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(22, 32, 58);
        return button;
    }

    private static NeonPanel CreateCard(Color borderColor, Color glowColor, Padding padding) =>
        new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = padding,
            BackColor = Color.Transparent,
            BorderColor = borderColor,
            GlowColor = glowColor,
            FillTop = Color.FromArgb(9, 12, 26),
            FillBottom = Color.FromArgb(5, 8, 20)
        };

    private void LoadEmbeddedLogo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FFBoost.UI.Assets.ffboost-logo.png");
        if (stream == null)
            return;

        using var image = Image.FromStream(stream);
        _logoImage = new Bitmap(image);
        _logoBox.Image = _logoImage;
    }

    private Icon? LoadAppIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FFBoost.UI.Assets.ffboost.ico");
        if (stream == null)
            return null;

        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }

    private static Icon CreateRamTrayIcon(double usagePercent, Color accentColor)
    {
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var bodyBrush = new SolidBrush(Color.FromArgb(12, 18, 34));
        using var borderPen = new Pen(accentColor, 1.2F);
        using var darkPen = new Pen(Color.FromArgb(72, 94, 128), 1F);
        graphics.FillRectangle(bodyBrush, 2, 4, 11, 8);
        graphics.DrawRectangle(darkPen, 2, 4, 11, 8);
        graphics.DrawRectangle(borderPen, 2, 4, 11, 8);

        for (var i = 0; i < 4; i++)
        {
            graphics.DrawLine(darkPen, 4 + (i * 2), 2, 4 + (i * 2), 4);
            graphics.DrawLine(darkPen, 4 + (i * 2), 12, 4 + (i * 2), 14);
        }

        var normalized = Math.Max(0, Math.Min(100, usagePercent));
        var barCount = Math.Max(1, (int)Math.Ceiling(normalized / 25d));
        for (var i = 0; i < barCount; i++)
        {
            using var fillBrush = new SolidBrush(Color.FromArgb(220 - (i * 15), accentColor));
            graphics.FillRectangle(fillBrush, 4 + (i * 2), 6, 1, 4);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var source = Icon.FromHandle(handle);
            return (Icon)source.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private (NotifyIcon trayIcon, ToolStripMenuItem startupItem, ToolStripMenuItem freeFireItem, ToolStripMenuItem turboItem) BuildTrayIcon()
    {
        var trayMenu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            BackColor = Color.FromArgb(6, 10, 22),
            ForeColor = Color.FromArgb(233, 240, 255),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
            Padding = new Padding(8),
            Renderer = new GamerMenuRenderer(),
            MinimumSize = new Size(240, 0),
            AutoSize = true
        };

        trayMenu.Items.Add(CreateTrayItem("Otimizar Agora", async (_, _) => await ExecuteOptimizeAsync(manualTrigger: false)));
        trayMenu.Items.Add(CreateTrayItem("Reverter Tudo", async (_, _) => await ExecuteRestoreAsync(manualTrigger: false)));
        trayMenu.Items.Add(new ToolStripSeparator());

        var startupItem = CreateTrayToggleItem("Iniciar com Windows", (_, _) => ToggleWindowsStartup());
        var freeFireItem = CreateTrayToggleItem("Preset Free Fire", (_, _) => ToggleFreeFireFromTray());
        var turboItem = CreateTrayToggleItem("Turbo FPS", (_, _) => ToggleTurboFromTray());

        trayMenu.Items.Add(startupItem);
        trayMenu.Items.Add(freeFireItem);
        trayMenu.Items.Add(turboItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(CreateTrayItem("Mostrar Painel", (_, _) => RestoreFromTray()));
        trayMenu.Items.Add(CreateTrayItem("Sair", (_, _) =>
        {
            _exitRequested = true;
            Close();
        }));

        var trayIcon = new NotifyIcon
        {
            Text = "FF Boost",
            Visible = false,
            ContextMenuStrip = trayMenu,
            Icon = _appIcon ?? Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application
        };
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        return (trayIcon, startupItem, freeFireItem, turboItem);
    }

    private static ToolStripMenuItem CreateTrayItem(string text, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            ForeColor = Color.FromArgb(233, 240, 255),
            BackColor = Color.FromArgb(6, 10, 22),
            AutoSize = true,
            Height = 34,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 2, 0, 2)
        };
        item.Click += onClick;
        return item;
    }

    private static ToolStripMenuItem CreateTrayToggleItem(string text, EventHandler onClick)
    {
        var item = CreateTrayItem(text, onClick);
        item.CheckOnClick = false;
        return item;
    }

    private void LoadAdminStatus()
    {
        var isAdmin = _adminService.IsRunningAsAdministrator();
        _lblAdminStatus.Text = isAdmin
            ? "Executando com privilegios de administrador."
            : "Executando sem privilegios de administrador. Alguns processos podem nao ser controlados.";
        _lblAdminStatus.ForeColor = isAdmin
            ? Color.FromArgb(121, 243, 171)
            : Color.FromArgb(255, 209, 110);

        RefreshModeNotice();
    }

    private void LoadProfile()
    {
        _cmbProfile.Items.Clear();
        _cmbProfile.Items.Add("Auto");
        _cmbProfile.Items.Add("Seguro");
        _cmbProfile.Items.Add("Forte");
        _cmbProfile.Items.Add("Ultra");

        var config = _configService.Load();
        _cmbProfile.SelectedItem = config.SelectedProfile;
        if (_cmbProfile.SelectedItem == null)
            _cmbProfile.SelectedItem = "Seguro";

        _chkFreeFireMode.Checked = config.EnableFreeFireMode;
        _chkTurboMode.Checked = config.EnableTurboMode;
        SyncTrayMenuState();
        RefreshModeNotice();
    }

    private void SyncTrayMenuState()
    {
        _trayStartupItem.Checked = _startupService.IsEnabled();
        _trayFreeFireItem.Checked = _chkFreeFireMode.Checked;
        _trayTurboItem.Checked = _chkTurboMode.Checked;
    }

    private void EnsureStartupDefaultOnFirstRun()
    {
        var config = _configService.Load();
        if (config.StartupPreferenceInitialized)
            return;

        var startupEnabled = _startupService.SetEnabled(true);
        config.StartupPreferenceInitialized = true;
        config.LaunchOnWindowsStartup = startupEnabled;
        config.AutoOptimizeOnStartup = true;
        config.EnableWatcher = true;
        _configService.Save(config);

        SyncTrayMenuState();
        if (startupEnabled)
        {
            AddLogs(new[]
            {
                "Primeira execucao detectada: iniciar com Windows foi ativado por padrao.",
                "Fluxo automatico ativo: inicializacao em bandeja, watcher do emulador e otimizacao automatica."
            });
            return;
        }

        AddLogs(new[]
        {
            "Primeira execucao detectada, mas a inicializacao automatica nao foi registrada com sucesso.",
            "O watcher e a otimizacao automatica permanecem ativos enquanto o app estiver em execucao."
        });
    }

    private void ApplyPresetVisual()
    {
        var freeFireEnabled = _chkFreeFireMode.Checked;
        _btnOptimize.BackColor = freeFireEnabled ? Color.FromArgb(255, 116, 72) : Color.FromArgb(23, 185, 255);
        _btnOptimize.FlatAppearance.BorderColor = freeFireEnabled ? Color.FromArgb(255, 191, 132) : Color.FromArgb(149, 232, 255);

        _lblTagline.Text = freeFireEnabled ? "FREE FIRE PRESET. LOW LATENCY." : "MORE FPS. LESS LAG.";
        _lblSubtitle.Text = freeFireEnabled ? "PRESET FOR FREE FIRE + BLUESTACKS" : "OPTIMIZATION ENGINE FOR LOW LATENCY";
        _lblSubtitle.ForeColor = freeFireEnabled ? Color.FromArgb(255, 177, 104) : Color.FromArgb(111, 207, 255);
        _chkFreeFireMode.ForeColor = freeFireEnabled ? Color.FromArgb(255, 196, 120) : Color.FromArgb(255, 166, 113);
        _chkTurboMode.ForeColor = _chkTurboMode.Checked ? Color.FromArgb(111, 207, 255) : Color.FromArgb(136, 152, 176);
        _lblCpuInfo.ForeColor = freeFireEnabled ? Color.FromArgb(255, 198, 124) : Color.FromArgb(82, 211, 255);
        _lblRamInfo.ForeColor = freeFireEnabled ? Color.FromArgb(255, 198, 124) : Color.FromArgb(82, 211, 255);
        RefreshModeNotice();
    }

    private void RefreshRecommendation()
    {
        var recommendation = _telemetryService.GetRecommendedProfile(_chkFreeFireMode.Checked);
        _lblRecommendation.Text = $"Perfil recomendado: {recommendation.RecommendedProfile} | score {recommendation.Score:0.##} | {recommendation.Reason}";
    }

    private void ConfigureWatcher()
    {
        var config = _configService.Load();
        if (!config.EnableWatcher)
            return;

        _watcherService.EmulatorStarted += OnWatcherEmulatorStarted;
        _watcherService.EmulatorStopped += OnWatcherEmulatorStopped;
        _watcherService.Start();
    }

    private void OnWatcherEmulatorStarted()
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(new Action(() =>
        {
            if (_optimizer.IsGameModeActive() || _watcherTriggeredChange)
                return;

            _watcherTriggeredChange = true;
            AddLogs(new[] { "Watcher: BlueStacks detectado. Iniciando otimizacao automatica." });
            _ = ExecuteOptimizeAsync(manualTrigger: false).ContinueWith(_ =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(() => _watcherTriggeredChange = false));
            });
        }));
    }

    private void OnWatcherEmulatorStopped()
    {
        if (!IsHandleCreated)
            return;

        BeginInvoke(new Action(() =>
        {
            if (!_optimizer.IsGameModeActive() || _watcherTriggeredChange)
                return;

            _watcherTriggeredChange = true;
            AddLogs(new[] { "Watcher: BlueStacks fechado. Restaurando ajustes temporarios." });
            _ = ExecuteRestoreAsync(manualTrigger: false).ContinueWith(_ =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(() => _watcherTriggeredChange = false));
            });
        }));
    }

    private async Task ExecuteOptimizeAsync(bool manualTrigger)
    {
        if (_isUiOperationRunning)
            return;

        SetUiOperationState(true, "Aplicando otimizacao...");
        _lstLogs.Items.Clear();

        try
        {
            var profile = _cmbProfile.SelectedItem?.ToString() ?? "Seguro";
            var result = await Task.Run(() => _optimizationCoordinator.ExecuteOptimize(profile, _chkFreeFireMode.Checked));
            var report = result.Report;

            _lblStatus.Text = result.Status;
            AddLogs(result.Logs);
            _lblCpuInfo.Text = $"CPU: {report.CpuBefore}% -> {report.CpuAfter}%";
            _lblRamInfo.Text = $"RAM: {report.RamBefore} GB -> {report.RamAfter} GB";
            _lblBenchmarkInfo.Text = $"Benchmark: score {report.SessionScore:0.##} | media {report.Benchmark.AvgScore:0.##} | delta {report.Benchmark.LastScoreDelta:+0.##;-0.##;0} | RAM {report.RamUsageBeforePercent:0.#}% -> {report.RamUsageAfterPercent:0.#}%";
            _lblRecommendation.Text = $"Perfil recomendado: {report.Recommendation.RecommendedProfile} | score {report.Recommendation.Score:0.##} | {report.Recommendation.Reason}";
            UpdateOptimizationNotice(report);
            RefreshLiveMetrics();
            if (manualTrigger)
            {
                using var form = new TechnicalReportForm(report);
                form.ShowDialog(this);
            }
        }
        finally
        {
            SetUiOperationState(false);
        }
    }

    private async Task ExecuteRestoreAsync(bool manualTrigger)
    {
        if (_isUiOperationRunning)
            return;

        SetUiOperationState(true, "Restaurando ajustes do sistema...");

        try
        {
            var result = await Task.Run(_optimizationCoordinator.ExecuteRestore);
            _lblStatus.Text = result.Status;
            AddLogs(result.Logs);
            ResetDashboardAfterRestore();
            RefreshLiveMetrics();
        }
        finally
        {
            SetUiOperationState(false);
        }
    }

    private async Task ExecuteMemoryOptimizeAsync()
    {
        if (_ramOptimizationRunning)
            return;

        _ramOptimizationRunning = true;
        _ramPulseButton.IsBusy = true;
        _ramPulseButton.SubtitleText = "Otimizando memoria...";
        _ramPulseButton.FooterText = "Compactando memoria...";

        try
        {
            var profile = _cmbProfile.SelectedItem?.ToString() ?? "Seguro";
            var result = await Task.Run(() => _optimizationCoordinator.ExecuteMemoryOptimize(profile, _chkFreeFireMode.Checked));
            _lblStatus.Text = result.Status;
            _lblRamInfo.Text = $"RAM: {result.Result.RamBeforeGb:0.##} GB -> {result.Result.RamAfterGb:0.##} GB";
            _lblBenchmarkInfo.Text = $"Memoria: {result.Result.TrimmedProcessCount} processo(s) | ~{result.Result.EstimatedFreedMb:0.#} MB | carga {result.Result.RamUsageBeforePercent:0.#}% -> {result.Result.RamUsageAfterPercent:0.#}%";
            AddLogs(result.Logs);

            if ((_cmbProfile.SelectedItem?.ToString() ?? string.Empty).Equals("Ultra", StringComparison.OrdinalIgnoreCase))
            {
                _lblModeNotice.Text = result.Result.Applied
                    ? $"Perfil Ultra: Windows basico e limpeza agressiva de memoria ativos. ~{result.Result.EstimatedFreedMb:0.#} MB foram liberados nesta passada."
                    : "Perfil Ultra: a limpeza de memoria foi executada, mas nao havia sobra relevante para reduzir nesta passada.";
                _lblModeNotice.ForeColor = result.Result.Applied
                    ? Color.FromArgb(122, 255, 174)
                    : Color.FromArgb(255, 205, 122);
                _lblModeNotice.Visible = true;
            }
        }
        finally
        {
            _ramOptimizationRunning = false;
            _ramPulseButton.IsBusy = false;
            RefreshLiveMetrics();
        }
    }

    private void AddLogs(IEnumerable<string> logs)
    {
        foreach (var log in logs)
            _lstLogs.Items.Add($"[{DateTime.Now:HH:mm:ss}] {log}");

        if (_lstLogs.Items.Count > 0)
            _lstLogs.TopIndex = _lstLogs.Items.Count - 1;
    }

    private void BtnAllowedApps_Click(object? sender, EventArgs e)
    {
        using var form = new AllowedAppsForm();
        form.ShowDialog(this);
    }

    private void BtnAbout_Click(object? sender, EventArgs e)
    {
        using var form = new AboutForm();
        form.ShowDialog(this);
    }

    private void CmbProfile_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_cmbProfile.SelectedItem == null)
            return;

        var config = _configService.Load();
        config.SelectedProfile = _cmbProfile.SelectedItem.ToString() ?? "Seguro";
        _configService.Save(config);
        RefreshModeNotice();
        AddLogs(new[] { $"Perfil selecionado: {config.SelectedProfile}" });
        RefreshLiveMetrics();
    }

    private void ChkFreeFireMode_CheckedChanged(object? sender, EventArgs e)
    {
        var config = _configService.Load();
        if (config.EnableFreeFireMode == _chkFreeFireMode.Checked)
        {
            SyncTrayMenuState();
            return;
        }

        config.EnableFreeFireMode = _chkFreeFireMode.Checked;
        _configService.Save(config);
        ApplyPresetVisual();
        RefreshRecommendation();
        SyncTrayMenuState();
        AddLogs(new[]
        {
            _chkFreeFireMode.Checked ? "Modo Free Fire + BlueStacks ativado." : "Modo Free Fire + BlueStacks desativado."
        });
    }

    private void ChkTurboMode_CheckedChanged(object? sender, EventArgs e)
    {
        var config = _configService.Load();
        if (config.EnableTurboMode == _chkTurboMode.Checked)
        {
            SyncTrayMenuState();
            return;
        }

        config.EnableTurboMode = _chkTurboMode.Checked;
        _configService.Save(config);
        ApplyPresetVisual();
        SyncTrayMenuState();
        AddLogs(new[]
        {
            _chkTurboMode.Checked ? "Turbo FPS ativado." : "Turbo FPS desativado."
        });
    }

    private void ToggleWindowsStartup()
    {
        var enable = !_startupService.IsEnabled();
        var applied = _startupService.SetEnabled(enable);
        var config = _configService.Load();
        config.LaunchOnWindowsStartup = applied && enable;
        config.StartupPreferenceInitialized = true;
        _configService.Save(config);
        SyncTrayMenuState();
        AddLogs(new[]
        {
            enable
                ? applied
                    ? "Inicializacao com Windows ativada. O app abrira minimizado na bandeja."
                    : "Nao foi possivel ativar a inicializacao com Windows nesta sessao."
                : applied
                    ? "Inicializacao com Windows desativada."
                    : "Nao foi possivel desativar a inicializacao com Windows nesta sessao."
        });
    }

    private void ToggleFreeFireFromTray()
    {
        _chkFreeFireMode.Checked = !_chkFreeFireMode.Checked;
    }

    private void ToggleTurboFromTray()
    {
        _chkTurboMode.Checked = !_chkTurboMode.Checked;
    }

    private void ApplyRecommendedProfile()
    {
        var recommendation = _telemetryService.GetRecommendedProfile(_chkFreeFireMode.Checked);
        _cmbProfile.SelectedItem = recommendation.RecommendedProfile;
        _lblRecommendation.Text = $"Perfil recomendado aplicado: {recommendation.RecommendedProfile} | score {recommendation.Score:0.##} | {recommendation.Reason}";
        AddLogs(new[] { $"Perfil recomendado aplicado: {recommendation.RecommendedProfile}" });
    }

    private void TryAutoOptimize()
    {
        var config = _configService.Load();
        if (!config.AutoOptimizeOnStartup)
            return;

        if (_optimizer.IsEmulatorRunning())
            _ = ExecuteOptimizeAsync(manualTrigger: false);
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        Shown -= MainForm_Shown;
        RegisterHotKey(Handle, HotkeyIdToggle, ModControl | ModShift, (uint)Keys.F);
        BeginInvoke(new Action(() => _ = InitializeAfterFirstPaintAsync()));
    }

    private async Task InitializeAfterFirstPaintAsync()
    {
        if (_startupInitializationCompleted)
            return;

        _startupInitializationCompleted = true;
        _lblStatus.Text = "Inicializando painel de controle...";
        await Task.Yield();

        LoadEmbeddedLogo();
        LoadAdminStatus();
        LoadProfile();
        EnsureStartupDefaultOnFirstRun();
        RefreshRecommendation();
        ApplyPresetVisual();
        RefreshLiveMetrics();
        ConfigureWatcher();
        RecoverPendingSystemState();
        _dashboardTimer.Start();
        TryAutoOptimize();
        SyncTrayMenuState();

        if (_startInTrayRequested)
        {
            BeginInvoke(new Action(() =>
            {
                WindowState = FormWindowState.Minimized;
            }));
        }

        if (string.Equals(_lblStatus.Text, "Inicializando painel de controle...", StringComparison.Ordinal))
            _lblStatus.Text = "Pronto para iniciar a otimizacao.";
    }

    private void RecoverPendingSystemState()
    {
        var result = _optimizationCoordinator.RecoverPendingSystemState();
        if (result.Logs.Count == 0)
        {
            RefreshModeNotice();
            return;
        }

        _lblStatus.Text = result.Status;
        AddLogs(result.Logs);
        ResetDashboardAfterRestore();
        _lblStatus.Text = result.Status;
    }

    private void RefreshModeNotice()
    {
        var selectedProfile = _cmbProfile.SelectedItem?.ToString() ?? "Seguro";
        if (selectedProfile.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            _lblModeNotice.Text = $"Perfil Auto: o FF Boost escolhe Seguro, Forte ou Ultra com base na carga atual de memoria. Turbo FPS {(_chkTurboMode.Checked ? "ligado" : "desligado")}. A limpeza de RAM acompanha o perfil definido automaticamente.";
            _lblModeNotice.ForeColor = Color.FromArgb(152, 214, 255);
            _lblModeNotice.Visible = true;
            SetModeBadge("AUTO PROFILE", Color.FromArgb(18, 38, 70), Color.FromArgb(152, 214, 255));
            return;
        }

        if (selectedProfile.Equals("Forte", StringComparison.OrdinalIgnoreCase))
        {
            _lblModeNotice.Text = $"Perfil Forte: equilibrio entre limpeza de processos, memoria e estabilidade do sistema. Turbo FPS {(_chkTurboMode.Checked ? "ligado" : "desligado")}.";
            _lblModeNotice.ForeColor = Color.FromArgb(127, 255, 186);
            _lblModeNotice.Visible = true;
            SetModeBadge("FORTE READY", Color.FromArgb(18, 52, 32), Color.FromArgb(127, 255, 186));
            return;
        }

        if (selectedProfile.Equals("Seguro", StringComparison.OrdinalIgnoreCase))
        {
            _lblModeNotice.Text = $"Perfil Seguro: limpeza leve, foco em estabilidade e menor interferencia no desktop. Turbo FPS {(_chkTurboMode.Checked ? "ligado" : "desligado")}.";
            _lblModeNotice.ForeColor = Color.FromArgb(132, 208, 255);
            _lblModeNotice.Visible = true;
            SetModeBadge("SEGURO READY", Color.FromArgb(18, 38, 70), Color.FromArgb(132, 208, 255));
            return;
        }

        _lblModeNotice.Text = $"Perfil Ultra: ao otimizar, o Windows entra temporariamente em modo visual basico e a limpeza de RAM fica agressiva para reduzir memoria ociosa e sobra visual. Turbo FPS {(_chkTurboMode.Checked ? "ligado" : "desligado")}. O botao Restaurar reverte tudo.";
        _lblModeNotice.ForeColor = Color.FromArgb(255, 205, 122);
        _lblModeNotice.Visible = true;
        SetModeBadge("ULTRA READY", Color.FromArgb(56, 32, 10), Color.FromArgb(255, 205, 122));
    }

    private void UpdateOptimizationNotice(FFBoost.Core.Models.TechnicalReport report)
    {
        if (report.Profile.Equals("Seguro", StringComparison.OrdinalIgnoreCase))
        {
            _lblModeNotice.Text = $"Perfil Seguro ativo: limpeza leve aplicada com {report.MemoryOptimizedProcessCount} processo(s) tratados e ~{report.MemoryRecoveredMb:0.#} MB recuperados.";
            _lblModeNotice.ForeColor = Color.FromArgb(132, 208, 255);
            _lblModeNotice.Visible = true;
            SetModeBadge("SEGURO ACTIVE", Color.FromArgb(18, 38, 70), Color.FromArgb(132, 208, 255));
            return;
        }

        if (report.Profile.Equals("Forte", StringComparison.OrdinalIgnoreCase))
        {
            _lblModeNotice.Text = $"Perfil Forte ativo: equilibrio aplicado com Turbo FPS {(report.TurboModeApplied ? "ativo" : "desligado")}, {report.MemoryOptimizedProcessCount} processo(s) tratados e ~{report.MemoryRecoveredMb:0.#} MB recuperados.";
            _lblModeNotice.ForeColor = Color.FromArgb(127, 255, 186);
            _lblModeNotice.Visible = true;
            SetModeBadge("FORTE ACTIVE", Color.FromArgb(18, 52, 32), Color.FromArgb(127, 255, 186));
            return;
        }

        _lblModeNotice.Visible = true;
        if (report.UltraVisualTweaksApplied)
        {
            _lblModeNotice.Text = $"Perfil Ultra ativo: Windows em modo visual basico, animacoes e efeitos extras reduzidos ate voce restaurar. Turbo FPS {(report.TurboModeApplied ? "ativo" : "desligado")} e memoria otimizada em {report.MemoryOptimizedProcessCount} processo(s), ~{report.MemoryRecoveredMb:0.#} MB.";
            _lblModeNotice.ForeColor = Color.FromArgb(255, 166, 113);
            SetModeBadge("ULTRA ACTIVE", Color.FromArgb(72, 24, 8), Color.FromArgb(255, 166, 113));
            return;
        }

        _lblModeNotice.Text = $"Perfil Ultra selecionado, mas os ajustes visuais do Windows nao foram aplicados por completo nesta sessao. Turbo FPS {(report.TurboModeApplied ? "ativo" : "nao aplicado")} e memoria otimizada em {report.MemoryOptimizedProcessCount} processo(s), ~{report.MemoryRecoveredMb:0.#} MB.";
        _lblModeNotice.ForeColor = Color.FromArgb(255, 120, 120);
        SetModeBadge("ULTRA DEGRADED", Color.FromArgb(70, 18, 18), Color.FromArgb(255, 120, 120));
    }

    private void RefreshLiveMetrics()
    {
        var usagePercent = _metricsService.GetRamUsagePercentage();
        var usedGb = _metricsService.GetUsedRamGb();
        var totalGb = _metricsService.GetTotalRamGb();
        var profileInfo = GetRamHubProfileInfo();
        _ramPulseButton.UsagePercent = usagePercent;
        _ramPulseButton.DetailsText = totalGb > 0
            ? $"{usedGb:0.##}/{totalGb:0.##} GB em uso"
            : $"{usedGb:0.##} GB em uso";
        _ramPulseButton.AccentColor = profileInfo.accent;
        _ramPulseButton.SubtitleText = profileInfo.subtitle;
        _ramPulseButton.SubtitleColor = profileInfo.color;
        _ramPulseButton.FooterText = _ramOptimizationRunning
            ? "Otimizando memoria..."
            : "Clique para otimizar RAM";
        UpdateTrayRamVisual(usagePercent, usedGb, totalGb, profileInfo.accent);
    }

    private void UpdateTrayRamVisual(double usagePercent, double usedGb, double totalGb, Color accentColor)
    {
        _trayRamIcon?.Dispose();
        _trayRamIcon = CreateRamTrayIcon(usagePercent, accentColor);
        _trayIcon.Icon = _trayRamIcon ?? _appIcon ?? SystemIcons.Application;

        var profile = _cmbProfile.SelectedItem?.ToString() ?? "Seguro";
        var ramText = totalGb > 0
            ? $"RAM {usagePercent:0}% | {usedGb:0.0}/{totalGb:0.0} GB | {profile}"
            : $"RAM {usagePercent:0}% | {usedGb:0.0} GB | {profile}";
        _trayIcon.Text = ramText.Length > 63 ? ramText[..63] : ramText;
    }

    private (string subtitle, Color color, Color accent) GetRamHubProfileInfo()
    {
        var selectedProfile = _cmbProfile.SelectedItem?.ToString() ?? "Seguro";
        return selectedProfile switch
        {
            "Ultra" => ("Ultra: limpeza agressiva", Color.FromArgb(255, 166, 113), Color.FromArgb(255, 140, 84)),
            "Forte" => ("Forte: limpeza equilibrada", Color.FromArgb(127, 255, 186), Color.FromArgb(60, 235, 126)),
            "Seguro" => ("Seguro: limpeza leve", Color.FromArgb(132, 208, 255), Color.FromArgb(82, 211, 255)),
            "Auto" => ("Auto: ajusta pela carga", Color.FromArgb(152, 214, 255), Color.FromArgb(120, 188, 255)),
            _ => ("RAM inteligente ativa", Color.FromArgb(127, 255, 186), Color.FromArgb(60, 235, 126))
        };
    }

    private void SetProfileFocusState(bool focused)
    {
        _profileFrame.BorderColor = focused ? Color.FromArgb(149, 232, 255) : Color.FromArgb(65, 167, 255);
        _profileFrame.GlowColor = focused ? Color.FromArgb(112, 214, 255) : Color.FromArgb(65, 167, 255);
        _cmbProfile.BackColor = focused ? Color.FromArgb(14, 24, 48) : Color.FromArgb(10, 18, 36);
        _profileFrame.Invalidate();
        _cmbProfile.Invalidate();
    }

    private void SetUiOperationState(bool running, string? statusText = null)
    {
        _isUiOperationRunning = running;
        _btnOptimize.Enabled = !running;
        _btnRestore.Enabled = !running;
        _cmbProfile.Enabled = !running;
        _chkFreeFireMode.Enabled = !running;
        _chkTurboMode.Enabled = !running;
        _ramPulseButton.Enabled = !running && !_ramOptimizationRunning;

        if (running && !string.IsNullOrWhiteSpace(statusText))
            _lblStatus.Text = statusText;

        _btnOptimize.Text = running ? "Processando..." : "Otimizar Agora";
    }

    private void ResetDashboardAfterRestore()
    {
        _lblCpuInfo.Text = "CPU: -- -> --";
        _lblRamInfo.Text = "RAM: -- -> --";
        _lblBenchmarkInfo.Text = "Benchmark: aguardando nova analise.";
        RefreshRecommendation();
        _lblModeNotice.Text = "Sistema restaurado: prioridades, efeitos visuais e ajustes temporarios foram revertidos.";
        _lblModeNotice.ForeColor = Color.FromArgb(121, 243, 171);
        _lblModeNotice.Visible = true;
        SetModeBadge("SYSTEM RESTORED", Color.FromArgb(18, 58, 34), Color.FromArgb(121, 243, 171));
    }

    private void SetModeBadge(string text, Color backColor, Color foreColor, bool visible = true)
    {
        _lblModeBadge.Text = text;
        _lblModeBadge.BackColor = backColor;
        _lblModeBadge.ForeColor = foreColor;
        _lblModeBadge.Visible = visible;
    }

    private void OpenTopProcesses()
    {
        using var form = new TopProcessesForm(_processAnalyzerService);
        form.ShowDialog(this);
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (_trayTransitionInProgress)
            return;

        if (WindowState != FormWindowState.Minimized)
            return;

        if (_isInTrayMode)
            return;

        _trayTransitionInProgress = true;
        try
        {
            SuspendLayout();
            _isInTrayMode = true;
            ShowInTaskbar = false;
            Hide();
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(1500, "FF Boost", "Executando em segundo plano. Ctrl+Shift+F alterna entre otimizar e restaurar.", ToolTipIcon.Info);
        }
        finally
        {
            ResumeLayout(performLayout: false);
            BeginInvoke(new Action(() => _trayTransitionInProgress = false));
        }
    }

    private void RestoreFromTray()
    {
        if (_trayTransitionInProgress)
            return;

        _trayTransitionInProgress = true;
        SuspendLayout();

        try
        {
            _trayIcon.Visible = false;
            ShowInTaskbar = true;
            Visible = true;
            Show();
        }
        finally
        {
            ResumeLayout(performLayout: true);
        }

        BeginInvoke(new Action(() =>
        {
            try
            {
                _isInTrayMode = false;

                if (WindowState == FormWindowState.Minimized)
                    WindowState = FormWindowState.Normal;

                NativeShowWindow(Handle, ShowWindowRestore);
                BringToFront();
                Activate();
                TopMost = true;
                TopMost = false;
                Focus();
                Invalidate(invalidateChildren: true);
                Update();
            }
            finally
            {
                _trayTransitionInProgress = false;
            }
        }));
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            return;
        }

        _watcherService.Dispose();
        _dashboardTimer.Stop();
        _dashboardTimer.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _appIcon?.Dispose();
        _trayRamIcon?.Dispose();
        _logoImage?.Dispose();
        UnregisterHotKey(Handle, HotkeyIdToggle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam == HotkeyIdToggle)
        {
            if (_optimizer.IsGameModeActive())
                _ = ExecuteRestoreAsync(manualTrigger: false);
            else
                _ = ExecuteOptimizeAsync(manualTrigger: false);
        }

        base.WndProc(ref m);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const int ShowWindowRestore = 9;

    private static void NativeShowWindow(IntPtr handle, int command)
    {
        if (handle != IntPtr.Zero)
            ShowWindow(handle, command);
    }
}

internal sealed class SciFiPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 22;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UseCutCorners { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CutCornerSize { get; set; } = 16;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CutTopLeft { get; set; } = -1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CutTopRight { get; set; } = -1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CutBottomRight { get; set; } = -1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CutBottomLeft { get; set; } = -1;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderGlowLeft { get; set; } = Color.FromArgb(40, 88, 255);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderGlowRight { get; set; } = Color.FromArgb(255, 102, 68);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCornerNotches { get; set; }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0 || ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var backgroundPath = CreateChromePath(ClientRectangle);
        var graphicsState = e.Graphics.Save();
        e.Graphics.SetClip(backgroundPath);

        using var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(4, 8, 18), Color.FromArgb(10, 8, 24), LinearGradientMode.Vertical);
        e.Graphics.FillPath(bg, backgroundPath);

        DrawGlow(e.Graphics, new Rectangle(-120, 120, 380, 380), Color.FromArgb(80, 0, 136, 255));
        DrawGlow(e.Graphics, new Rectangle(Width - 330, 50, 340, 340), Color.FromArgb(75, 255, 88, 32));
        DrawGlow(e.Graphics, new Rectangle(Width / 2 - 170, 180, 340, 180), Color.FromArgb(42, 255, 126, 0));
        e.Graphics.Restore(graphicsState);
        DrawBorder(e.Graphics);
        DrawCornerNotches(e.Graphics);
        DrawParticles(e.Graphics);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (UseCutCorners)
            UiGeometry.ApplyCutCornerRegion(this, ResolveCutCorners());
        else
            UiGeometry.ApplyRoundedRegion(this, CornerRadius);
    }

    private void DrawBorder(Graphics graphics)
    {
        if (Width <= 1 || Height <= 1)
            return;

        using var leftPen = new Pen(BorderGlowLeft, 2F);
        using var rightPen = new Pen(BorderGlowRight, 2F);
        using var borderPath = CreateChromePath(new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1)));
        graphics.DrawPath(leftPen, borderPath);

        var clipState = graphics.Save();
        graphics.SetClip(new Rectangle(Width / 2, 0, Math.Max(0, Width / 2), Height));
        graphics.DrawPath(rightPen, borderPath);
        graphics.Restore(clipState);
    }

    private void DrawCornerNotches(Graphics graphics)
    {
        if (!ShowCornerNotches || Width < 120 || Height < 120)
            return;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var blueGlow = new Pen(Color.FromArgb(150, 86, 214, 255), 3.2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var blueCore = new Pen(Color.FromArgb(180, 128, 232, 255), 1.2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var orangeGlow = new Pen(Color.FromArgb(150, 255, 122, 72), 3.2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var orangeCore = new Pen(Color.FromArgb(200, 255, 162, 110), 1.2F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        var topRight = new[]
        {
            new Point(Width - 92, 12),
            new Point(Width - 44, 12),
            new Point(Width - 24, 32)
        };
        graphics.DrawLines(blueGlow, topRight);
        graphics.DrawLines(blueCore, topRight);

        var bottomLeft = new[]
        {
            new Point(18, Height - 42),
            new Point(18, Height - 18),
            new Point(42, Height - 18)
        };
        graphics.DrawLines(orangeGlow, bottomLeft);
        graphics.DrawLines(orangeCore, bottomLeft);

        using var blueDot = new SolidBrush(Color.FromArgb(210, 118, 224, 255));
        using var orangeDot = new SolidBrush(Color.FromArgb(210, 255, 150, 102));
        graphics.FillEllipse(blueDot, Width - 98, 9, 6, 6);
        graphics.FillEllipse(orangeDot, 15, Height - 24, 6, 6);
    }

    private GraphicsPath CreateChromePath(Rectangle bounds)
    {
        return UseCutCorners
            ? UiGeometry.CreateCutCornerPath(bounds, ResolveCutCorners())
            : UiGeometry.CreateRoundedPath(bounds, CornerRadius);
    }

    private UiCutCorners ResolveCutCorners()
    {
        var fallback = CutCornerSize;
        return new UiCutCorners(
            CutTopLeft >= 0 ? CutTopLeft : fallback,
            CutTopRight >= 0 ? CutTopRight : fallback,
            CutBottomRight >= 0 ? CutBottomRight : fallback,
            CutBottomLeft >= 0 ? CutBottomLeft : fallback);
    }

    private static void DrawGlow(Graphics graphics, Rectangle bounds, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        using var path = new GraphicsPath();
        path.AddEllipse(bounds);
        using var brush = new PathGradientBrush(path) { CenterColor = color, SurroundColors = new[] { Color.Transparent } };
        graphics.FillEllipse(brush, bounds);
    }

    private void DrawParticles(Graphics graphics)
    {
        var blue = Color.FromArgb(88, 54, 194, 255);
        var orange = Color.FromArgb(88, 255, 108, 44);

        for (var i = 0; i < 18; i++)
        {
            var x = (37 * i) % Math.Max(1, Width - 8);
            var y = (61 * i) % Math.Max(1, Height - 8);
            using var brush = new SolidBrush(i % 2 == 0 ? blue : orange);
            graphics.FillEllipse(brush, x, y, 2 + (i % 3), 2 + (i % 3));
        }
    }
}

internal sealed class NeonPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 18;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(70, 180, 255);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color GlowColor { get; set; } = Color.FromArgb(70, 180, 255);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillTop { get; set; } = Color.FromArgb(12, 16, 34);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillBottom { get; set; } = Color.FromArgb(5, 8, 20);

    public NeonPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0 || ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var outer = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        if (outer.Width <= 0 || outer.Height <= 0)
            return;

        using var fill = new LinearGradientBrush(ClientRectangle, FillTop, FillBottom, LinearGradientMode.Vertical);
        using var outerPath = UiGeometry.CreateRoundedPath(outer, CornerRadius);
        using var innerPath = UiGeometry.CreateRoundedPath(new Rectangle(1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3)), Math.Max(0, CornerRadius - 1));
        e.Graphics.FillPath(fill, outerPath);

        using var glowPen = new Pen(Color.FromArgb(72, GlowColor), 3F);
        using var borderPen = new Pen(BorderColor, 1.2F);
        e.Graphics.DrawPath(glowPen, innerPath);
        e.Graphics.DrawPath(borderPen, outerPath);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        UiGeometry.ApplyRoundedRegion(this, CornerRadius);
    }
}

internal sealed class RamPulseButton : Control
{
    private readonly System.Windows.Forms.Timer _animationTimer;
    private float _phase;
    private bool _hovered;
    private double _usagePercent;
    private bool _isBusy;
    private string _detailsText = string.Empty;
    private string _subtitleText = "Forte: limpeza equilibrada";
    private string _footerText = "Clique para otimizar RAM";

    public RamPulseButton()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        MinimumSize = new Size(190, 136);
        BackColor = Color.FromArgb(9, 12, 26);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        _animationTimer = new System.Windows.Forms.Timer { Interval = 33 };
        _animationTimer.Tick += (_, _) =>
        {
            _phase += _isBusy ? 6.5F : 2.2F;
            if (_phase >= 360F)
                _phase -= 360F;

            Invalidate();
        };
        _animationTimer.Start();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        if (Width <= 0 || Height <= 0 || ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;

        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var fill = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(9, 12, 26),
            Color.FromArgb(5, 8, 20),
            LinearGradientMode.Vertical);
        using var backgroundPath = UiGeometry.CreateRoundedPath(ClientRectangle, 18);
        pevent.Graphics.FillPath(fill, backgroundPath);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor { get; set; } = Color.FromArgb(60, 235, 126);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double UsagePercent
    {
        get => _usagePercent;
        set
        {
            _usagePercent = Math.Max(0, Math.Min(100, value));
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DetailsText
    {
        get => _detailsText;
        set
        {
            _detailsText = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SubtitleText
    {
        get => _subtitleText;
        set
        {
            _subtitleText = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SubtitleColor { get; set; } = Color.FromArgb(127, 255, 186);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string FooterText
    {
        get => _footerText;
        set
        {
            _footerText = value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _animationTimer.Dispose();

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (Width <= 0 || Height <= 0 || ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var ringBounds = new Rectangle(28, 2, Math.Max(112, Width - 56), 108);
        if (ringBounds.Width != ringBounds.Height)
        {
            var size = Math.Min(ringBounds.Width, ringBounds.Height);
            ringBounds = new Rectangle((Width - size) / 2, 2, size, size);
        }

        var cardBounds = new Rectangle(6, 2, Math.Max(0, Width - 12), Math.Max(0, Height - 4));
        using var cardPath = UiGeometry.CreateRoundedPath(cardBounds, 16);
        using var cardGlow = new Pen(Color.FromArgb(36, AccentColor), 1.2F);
        using var fillBrush = new SolidBrush(Color.FromArgb(18, 24, 38));
        using var shadowBrush = new SolidBrush(Color.FromArgb(_hovered ? 58 : 34, AccentColor));
        using var ringPen = new Pen(Color.FromArgb(36, 62, 78), 9F);
        using var accentPen = new Pen(AccentColor, 4.6F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var glowPen = new Pen(Color.FromArgb(80, AccentColor), 9F) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        e.Graphics.DrawPath(cardGlow, cardPath);
        e.Graphics.FillEllipse(shadowBrush, Rectangle.Inflate(ringBounds, 4, 4));
        e.Graphics.FillEllipse(fillBrush, ringBounds);
        e.Graphics.DrawArc(ringPen, ringBounds, -90, 360);

        var sweep = _isBusy
            ? 124F
            : Math.Max(22F, (float)(_usagePercent * 3.6));
        var startAngle = _isBusy ? _phase - 90F : -90F;
        e.Graphics.DrawArc(glowPen, ringBounds, startAngle, sweep);
        e.Graphics.DrawArc(accentPen, ringBounds, startAngle, sweep);

        var dotAngle = (startAngle + sweep) * (Math.PI / 180d);
        var centerX = ringBounds.Left + (ringBounds.Width / 2d);
        var centerY = ringBounds.Top + (ringBounds.Height / 2d);
        var radius = ringBounds.Width / 2d;
        var dotX = centerX + (Math.Cos(dotAngle) * radius);
        var dotY = centerY + (Math.Sin(dotAngle) * radius);
        using var dotBrush = new SolidBrush(AccentColor);
        e.Graphics.FillEllipse(dotBrush, (float)dotX - 5F, (float)dotY - 5F, 10F, 10F);

        var titleFont = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
        var valueFont = new Font("Segoe UI", 30F, FontStyle.Regular, GraphicsUnit.Point);
        var detailsFont = new Font("Segoe UI", 8.75F, FontStyle.Regular, GraphicsUnit.Point);
        var subtitleFont = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold, GraphicsUnit.Point);
        var footerFont = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold, GraphicsUnit.Point);

        TextRenderer.DrawText(
            e.Graphics,
            "RAM",
            titleFont,
            new Rectangle(ringBounds.Left, ringBounds.Top + 18, ringBounds.Width, 22),
            Color.FromArgb(170, 184, 210),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(
            e.Graphics,
            $"{_usagePercent:0}%",
            valueFont,
            new Rectangle(ringBounds.Left, ringBounds.Top + 34, ringBounds.Width, 52),
            AccentColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(
            e.Graphics,
            _detailsText,
            detailsFont,
            new Rectangle(10, ringBounds.Bottom + 6, Math.Max(0, Width - 20), 18),
            Color.FromArgb(171, 188, 210),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(
            e.Graphics,
            _subtitleText,
            subtitleFont,
            new Rectangle(10, ringBounds.Bottom + 24, Math.Max(0, Width - 20), 18),
            SubtitleColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(
            e.Graphics,
            _footerText,
            footerFont,
            new Rectangle(10, ringBounds.Bottom + 42, Math.Max(0, Width - 20), 18),
            _isBusy ? AccentColor : Color.FromArgb(127, 255, 186),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        titleFont.Dispose();
        valueFont.Dispose();
        detailsFont.Dispose();
        subtitleFont.Dispose();
        footerFont.Dispose();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UiGeometry.ApplyRoundedRegion(this, 18);
    }
}

internal static class UiGeometry
{
    public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return path;

        radius = Math.Max(0, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
        if (radius == 0)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

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

    public static GraphicsPath CreateCutCornerPath(Rectangle bounds, int cutSize)
    {
        return CreateCutCornerPath(bounds, new UiCutCorners(cutSize, cutSize, cutSize, cutSize));
    }

    public static GraphicsPath CreateCutCornerPath(Rectangle bounds, UiCutCorners corners)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return path;

        var maxCut = Math.Min(bounds.Width, bounds.Height) / 2;
        var topLeft = Math.Max(0, Math.Min(corners.TopLeft, maxCut));
        var topRight = Math.Max(0, Math.Min(corners.TopRight, maxCut));
        var bottomRight = Math.Max(0, Math.Min(corners.BottomRight, maxCut));
        var bottomLeft = Math.Max(0, Math.Min(corners.BottomLeft, maxCut));

        if (topLeft == 0 && topRight == 0 && bottomRight == 0 && bottomLeft == 0)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        path.AddLine(bounds.Left + topLeft, bounds.Top, bounds.Right - topRight, bounds.Top);
        path.AddLine(bounds.Right - topRight, bounds.Top, bounds.Right, bounds.Top + topRight);
        path.AddLine(bounds.Right, bounds.Top + topRight, bounds.Right, bounds.Bottom - bottomRight);
        path.AddLine(bounds.Right, bounds.Bottom - bottomRight, bounds.Right - bottomRight, bounds.Bottom);
        path.AddLine(bounds.Right - bottomRight, bounds.Bottom, bounds.Left + bottomLeft, bounds.Bottom);
        path.AddLine(bounds.Left + bottomLeft, bounds.Bottom, bounds.Left, bounds.Bottom - bottomLeft);
        path.AddLine(bounds.Left, bounds.Bottom - bottomLeft, bounds.Left, bounds.Top + topLeft);
        path.AddLine(bounds.Left, bounds.Top + topLeft, bounds.Left + topLeft, bounds.Top);
        path.CloseFigure();
        return path;
    }

    public static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        using var path = CreateRoundedPath(new Rectangle(Point.Empty, control.Size), radius);
        var nextRegion = new Region(path);
        var previousRegion = control.Region;
        control.Region = nextRegion;
        previousRegion?.Dispose();
    }

    public static void ApplyCutCornerRegion(Control control, int cutSize)
    {
        ApplyCutCornerRegion(control, new UiCutCorners(cutSize, cutSize, cutSize, cutSize));
    }

    public static void ApplyCutCornerRegion(Control control, UiCutCorners corners)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        using var path = CreateCutCornerPath(new Rectangle(Point.Empty, control.Size), corners);
        var nextRegion = new Region(path);
        var previousRegion = control.Region;
        control.Region = nextRegion;
        previousRegion?.Dispose();
    }
}

internal readonly record struct UiCutCorners(int TopLeft, int TopRight, int BottomRight, int BottomLeft);

internal sealed class GamerMenuRenderer : ToolStripProfessionalRenderer
{
    public GamerMenuRenderer() : base(new GamerMenuColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected
            ? Color.FromArgb(255, 248, 238)
            : Color.FromArgb(233, 240, 255);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(58, 108, 182));
        var y = e.Item.ContentRectangle.Height / 2;
        e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
    }
}

internal sealed class GamerMenuColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Color.FromArgb(6, 10, 22);
    public override Color ImageMarginGradientBegin => Color.FromArgb(6, 10, 22);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(6, 10, 22);
    public override Color ImageMarginGradientEnd => Color.FromArgb(6, 10, 22);
    public override Color MenuBorder => Color.FromArgb(53, 122, 255);
    public override Color MenuItemBorder => Color.FromArgb(255, 122, 72);
    public override Color MenuItemSelected => Color.FromArgb(24, 36, 72);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(18, 32, 68);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(28, 20, 42);
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(22, 28, 58);
    public override Color MenuItemPressedGradientMiddle => Color.FromArgb(22, 28, 58);
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(22, 28, 58);
    public override Color SeparatorDark => Color.FromArgb(58, 108, 182);
    public override Color SeparatorLight => Color.FromArgb(10, 16, 28);
}
