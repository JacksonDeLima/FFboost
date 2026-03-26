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
    private readonly OptimizationCoordinatorService _optimizationCoordinator;
    private readonly SystemMetricsService _metricsService;
    private readonly LogFileService _logFileService;
    private readonly TelemetryService _telemetryService;
    private readonly GameWatcherService _watcherService;
    private readonly Label _lblStatus;
    private readonly Label _lblAdminStatus;
    private readonly Label _lblSubtitle;
    private readonly Label _lblTagline;
    private readonly Label _lblCpuInfo;
    private readonly Label _lblRamInfo;
    private readonly Label _lblBenchmarkInfo;
    private readonly Label _lblRecommendation;
    private readonly ListBox _lstLogs;
    private readonly ComboBox _cmbProfile;
    private readonly CheckBox _chkFreeFireMode;
    private readonly Button _btnOptimize;
    private readonly Button _btnRestore;
    private readonly NotifyIcon _trayIcon;
    private readonly PictureBox _logoBox;
    private Image? _logoImage;
    private bool _exitRequested;
    private bool _watcherTriggeredChange;

    public MainForm(
        OptimizerService optimizer,
        AdminService adminService,
        ConfigService configService,
        SystemMetricsService metricsService,
        LogFileService logFileService,
        TelemetryService telemetryService,
        OptimizationCoordinatorService optimizationCoordinator,
        GameWatcherService watcherService)
    {
        Text = "FF Boost";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 820);
        MinimumSize = new Size(760, 820);
        BackColor = Color.FromArgb(4, 8, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;

        _optimizer = optimizer;
        _adminService = adminService;
        _configService = configService;
        _metricsService = metricsService;
        _logFileService = logFileService;
        _telemetryService = telemetryService;
        _optimizationCoordinator = optimizationCoordinator;
        _watcherService = watcherService;

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

        var btnAbout = CreateGhostButton("Sobre", 96, Color.FromArgb(65, 167, 255));
        btnAbout.Click += BtnAbout_Click;

        var btnAutoProfile = CreateGhostButton("Perfil Auto", 118, Color.FromArgb(255, 146, 72));
        btnAutoProfile.Click += (_, _) => ApplyRecommendedProfile();

        var btnClearLog = CreateGhostButton("Limpar Log", 114, Color.FromArgb(65, 167, 255));

        var btnAllowedApps = CreateFrameButton("Apps Permitidos", 280, 48, Color.FromArgb(43, 184, 255), Color.FromArgb(8, 16, 34));
        btnAllowedApps.Click += BtnAllowedApps_Click;

        _btnRestore = CreateFrameButton("Restaurar", 280, 48, Color.FromArgb(255, 92, 92), Color.FromArgb(24, 10, 22));
        _btnRestore.Click += (_, _) => ExecuteRestore(manualTrigger: true);

        _btnOptimize = CreatePrimaryButton();
        _btnOptimize.Click += (_, _) => ExecuteOptimize(manualTrigger: true);

        _cmbProfile = new ComboBox
        {
            Width = 186,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(10, 18, 36),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point)
        };
        _cmbProfile.DropDownWidth = 186;
        _cmbProfile.SelectedIndexChanged += CmbProfile_SelectedIndexChanged;

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
            Text = "Benchmark: aguardando sessao."
        };

        _lblRecommendation = new Label
        {
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = Color.FromArgb(255, 179, 111),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "Recomendacao: coletando historico local."
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 14),
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(234, 241, 255),
            Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "Pronto para otimizar.",
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
            Padding = new Padding(18, 14, 18, 18),
            BorderGlowLeft = Color.FromArgb(40, 88, 255),
            BorderGlowRight = Color.FromArgb(255, 102, 68)
        };

        var heroCard = BuildHeroCard();
        var profileCard = BuildProfileCard();
        var optimizeCard = BuildOptimizeCard();
        var actionCard = BuildActionCard(btnAllowedApps);
        var metricsCard = BuildMetricsCard();
        var logsCard = BuildLogsCard();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 252F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(heroCard, 0, 0);
        layout.Controls.Add(profileCard, 0, 1);
        layout.Controls.Add(optimizeCard, 0, 2);
        layout.Controls.Add(actionCard, 0, 3);
        layout.Controls.Add(metricsCard, 0, 4);

        var mainStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        mainStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 604F));
        mainStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainStack.Controls.Add(layout, 0, 0);
        mainStack.Controls.Add(logsCard, 0, 1);

        root.Controls.Add(mainStack);
        Controls.Add(root);

        _trayIcon = BuildTrayIcon();
        LoadEmbeddedLogo();
        LoadAdminStatus();
        LoadProfile();
        RefreshRecommendation();
        ApplyPresetVisual();
        ConfigureWatcher();
        Shown += MainForm_Shown;
        Resize += MainForm_Resize;
        FormClosing += MainForm_FormClosing;
    }

    private Panel BuildTopBar(Button btnAbout, Button btnAutoProfile, Button btnClearLog)
    {
        var title = new Label
        {
            Text = "FF Boost",
            AutoSize = true,
            ForeColor = Color.FromArgb(238, 243, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(0, 7)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
        panel.Controls.Add(title);
        panel.Controls.Add(btnAbout);
        panel.Controls.Add(btnAutoProfile);
        panel.Controls.Add(btnClearLog);
        panel.Resize += (_, _) =>
        {
            btnAbout.Location = new Point(Math.Max(0, panel.Width - btnAbout.Width), 4);
            btnAutoProfile.Location = new Point(Math.Max(0, btnAbout.Left - btnAutoProfile.Width - 8), 4);
            btnClearLog.Location = new Point(Math.Max(0, btnAutoProfile.Left - btnClearLog.Width - 8), 4);
        };
        return panel;
    }

    private Control BuildHeroCard()
    {
        var card = CreateCard(Color.FromArgb(20, 43, 98), Color.FromArgb(255, 114, 68), new Padding(0));
        card.Margin = new Padding(0, 0, 0, 8);

        var inner = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        inner.Controls.Add(_logoBox);
        card.Controls.Add(inner);
        return card;
    }

    private Control BuildProfileCard()
    {
        var valueFrame = new NeonPanel
        {
            Size = new Size(530, 42),
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
            Width = 112,
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
            var desiredWidth = Math.Min(186, Math.Max(156, comboHost.Width - 120));
            _cmbProfile.Width = desiredWidth;
            _cmbProfile.DropDownWidth = desiredWidth;
            _cmbProfile.Location = new Point(
                Math.Max(0, comboHost.Width - desiredWidth - 20),
                Math.Max(0, (comboHost.Height - _cmbProfile.Height) / 2));
        };

        valueFrame.Controls.Add(comboHost);
        valueFrame.Controls.Add(label);

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        host.Controls.Add(_chkFreeFireMode);
        host.Controls.Add(valueFrame);
        host.Resize += (_, _) =>
        {
            valueFrame.Location = new Point(Math.Max(0, (host.Width - valueFrame.Width) / 2), 10);
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
                14);
        };
        return host;
    }

    private Control BuildActionCard(Button btnAllowedApps)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        host.Controls.Add(btnAllowedApps);
        host.Controls.Add(_btnRestore);
        host.Resize += (_, _) =>
        {
            const int spacing = 22;
            var totalWidth = btnAllowedApps.Width + _btnRestore.Width + spacing;
            var startX = Math.Max(0, (host.Width - totalWidth) / 2);
            btnAllowedApps.Location = new Point(startX, 12);
            _btnRestore.Location = new Point(startX + btnAllowedApps.Width + spacing, 12);
        };
        return host;
    }

    private Control BuildMetricsCard()
    {
        var metricsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 8),
            ColumnStyles =
            {
                new ColumnStyle(SizeType.Percent, 50F),
                new ColumnStyle(SizeType.Percent, 50F)
            }
        };

        metricsLayout.Controls.Add(CreateMetricCard("CPU", _lblCpuInfo, Color.FromArgb(54, 196, 255)), 0, 0);
        metricsLayout.Controls.Add(CreateMetricCard("RAM", _lblRamInfo, Color.FromArgb(90, 194, 255)), 1, 0);
        return metricsLayout;
    }

    private Control BuildInfoCard()
    {
        var card = CreateCard(Color.FromArgb(38, 84, 178), Color.FromArgb(64, 142, 255), new Padding(12));
        card.Controls.Add(_lblStatus);

        var infoHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = Color.Transparent
        };
        infoHost.Controls.Add(_lblRecommendation);
        infoHost.Controls.Add(_lblBenchmarkInfo);
        infoHost.Controls.Add(_lblAdminStatus);
        card.Controls.Add(infoHost);
        return card;
    }

    private Control BuildLogsCard()
    {
        var card = CreateCard(Color.FromArgb(20, 44, 102), Color.FromArgb(19, 41, 94), new Padding(8, 6, 8, 8));
        card.Margin = new Padding(0, 0, 0, 0);
        card.Controls.Add(_lstLogs);
        return card;
    }

    private Control CreateMetricCard(string title, Label valueLabel, Color accent)
    {
        var card = CreateCard(accent, accent, new Padding(14, 8, 14, 6));
        card.Margin = new Padding(0, 0, 0, 0);

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = $"{title}:",
            ForeColor = accent,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point)
        };

        card.Controls.Add(valueLabel);
        card.Controls.Add(titleLabel);
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
            Width = 446,
            Height = 54,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(23, 185, 255),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 17F, FontStyle.Regular, GraphicsUnit.Point)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(149, 232, 255);
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
            Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = borderColor;
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
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = borderColor;
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

    private NotifyIcon BuildTrayIcon()
    {
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Otimizar Agora", null, (_, _) => ExecuteOptimize(manualTrigger: false));
        trayMenu.Items.Add("Reverter Tudo", null, (_, _) => ExecuteRestore(manualTrigger: false));
        trayMenu.Items.Add("Mostrar", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add("Sair", null, (_, _) =>
        {
            _exitRequested = true;
            Close();
        });

        var trayIcon = new NotifyIcon
        {
            Text = "FF Boost",
            Visible = false,
            ContextMenuStrip = trayMenu,
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application
        };
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        return trayIcon;
    }

    private void LoadAdminStatus()
    {
        var isAdmin = _adminService.IsRunningAsAdministrator();
        _lblAdminStatus.Text = isAdmin
            ? "Executando como administrador."
            : "Nao esta como administrador. Alguns processos podem nao fechar.";
        _lblAdminStatus.ForeColor = isAdmin
            ? Color.FromArgb(121, 243, 171)
            : Color.FromArgb(255, 209, 110);
    }

    private void LoadProfile()
    {
        _cmbProfile.Items.Clear();
        _cmbProfile.Items.Add("Seguro");
        _cmbProfile.Items.Add("Forte");
        _cmbProfile.Items.Add("Ultra");

        var config = _configService.Load();
        _cmbProfile.SelectedItem = config.SelectedProfile;
        if (_cmbProfile.SelectedItem == null)
            _cmbProfile.SelectedItem = "Seguro";

        _chkFreeFireMode.Checked = config.EnableFreeFireMode;
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
        _lblCpuInfo.ForeColor = freeFireEnabled ? Color.FromArgb(255, 198, 124) : Color.FromArgb(82, 211, 255);
        _lblRamInfo.ForeColor = freeFireEnabled ? Color.FromArgb(255, 198, 124) : Color.FromArgb(82, 211, 255);
    }

    private void RefreshRecommendation()
    {
        var recommendation = _telemetryService.GetRecommendedProfile(_chkFreeFireMode.Checked);
        _lblRecommendation.Text = $"Recomendacao: {recommendation.RecommendedProfile} | score {recommendation.Score:0.##} | {recommendation.Reason}";
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
            AddLogs(new[] { "Watcher: BlueStacks detectado, aplicando otimizacao automatica." });
            ExecuteOptimize(manualTrigger: false);
            _watcherTriggeredChange = false;
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
            AddLogs(new[] { "Watcher: BlueStacks fechado, restaurando sistema." });
            ExecuteRestore(manualTrigger: false);
            _watcherTriggeredChange = false;
        }));
    }

    private void ExecuteOptimize(bool manualTrigger)
    {
        _lstLogs.Items.Clear();
        var profile = _cmbProfile.SelectedItem?.ToString() ?? "Seguro";
        var result = _optimizationCoordinator.ExecuteOptimize(profile, _chkFreeFireMode.Checked);
        var report = result.Report;

        _lblStatus.Text = result.Status;
        AddLogs(result.Logs);
        _lblCpuInfo.Text = $"CPU: {report.CpuBefore}% -> {report.CpuAfter}%";
        _lblRamInfo.Text = $"RAM: {report.RamBefore} GB -> {report.RamAfter} GB";
        _lblBenchmarkInfo.Text = $"Benchmark: score {report.SessionScore:0.##} | media {report.Benchmark.AvgScore:0.##} | delta {report.Benchmark.LastScoreDelta:+0.##;-0.##;0}";
        _lblRecommendation.Text = $"Recomendacao: {report.Recommendation.RecommendedProfile} | score {report.Recommendation.Score:0.##} | {report.Recommendation.Reason}";

        if (manualTrigger)
        {
            using var form = new TechnicalReportForm(report);
            form.ShowDialog(this);
        }
    }

    private void ExecuteRestore(bool manualTrigger)
    {
        var result = _optimizationCoordinator.ExecuteRestore();
        _lblStatus.Text = result.Status;
        AddLogs(result.Logs);

        if (manualTrigger)
        {
            _lblCpuInfo.Text = "CPU: -- -> --";
            _lblRamInfo.Text = "RAM: -- -> --";
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
        AddLogs(new[] { $"Perfil alterado para: {config.SelectedProfile}" });
    }

    private void ChkFreeFireMode_CheckedChanged(object? sender, EventArgs e)
    {
        var config = _configService.Load();
        if (config.EnableFreeFireMode == _chkFreeFireMode.Checked)
            return;

        config.EnableFreeFireMode = _chkFreeFireMode.Checked;
        _configService.Save(config);
        ApplyPresetVisual();
        RefreshRecommendation();
        AddLogs(new[]
        {
            _chkFreeFireMode.Checked ? "Modo Free Fire + BlueStacks ativado." : "Modo Free Fire + BlueStacks desativado."
        });
    }

    private void ApplyRecommendedProfile()
    {
        var recommendation = _telemetryService.GetRecommendedProfile(_chkFreeFireMode.Checked);
        _cmbProfile.SelectedItem = recommendation.RecommendedProfile;
        _lblRecommendation.Text = $"Recomendacao aplicada: {recommendation.RecommendedProfile} | score {recommendation.Score:0.##} | {recommendation.Reason}";
        AddLogs(new[] { $"Perfil automatico aplicado: {recommendation.RecommendedProfile}" });
    }

    private void TryAutoOptimize()
    {
        var config = _configService.Load();
        if (!config.AutoOptimizeOnStartup)
            return;

        if (_optimizer.IsEmulatorRunning())
            ExecuteOptimize(manualTrigger: false);
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        Shown -= MainForm_Shown;
        RegisterHotKey(Handle, HotkeyIdToggle, ModControl | ModShift, (uint)Keys.F);
        TryAutoOptimize();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Minimized)
            return;

        Hide();
        _trayIcon.Visible = true;
        _trayIcon.ShowBalloonTip(1500, "FF Boost", "Executando em segundo plano. Ctrl+Shift+F alterna otimizar/restaurar.", ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        _trayIcon.Visible = false;
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
        _trayIcon.Visible = false;
        _logoImage?.Dispose();
        UnregisterHotKey(Handle, HotkeyIdToggle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam == HotkeyIdToggle)
        {
            if (_optimizer.IsGameModeActive())
                ExecuteRestore(manualTrigger: false);
            else
                ExecuteOptimize(manualTrigger: false);
        }

        base.WndProc(ref m);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

internal sealed class SciFiPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderGlowLeft { get; set; } = Color.FromArgb(40, 88, 255);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderGlowRight { get; set; } = Color.FromArgb(255, 102, 68);

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(4, 8, 18), Color.FromArgb(10, 8, 24), LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(bg, ClientRectangle);

        DrawGlow(e.Graphics, new Rectangle(-120, 120, 380, 380), Color.FromArgb(80, 0, 136, 255));
        DrawGlow(e.Graphics, new Rectangle(Width - 330, 50, 340, 340), Color.FromArgb(75, 255, 88, 32));
        DrawGlow(e.Graphics, new Rectangle(Width / 2 - 170, 180, 340, 180), Color.FromArgb(42, 255, 126, 0));
        DrawBorder(e.Graphics);
        DrawParticles(e.Graphics);
    }

    private void DrawBorder(Graphics graphics)
    {
        using var leftPen = new Pen(BorderGlowLeft, 2F);
        using var rightPen = new Pen(BorderGlowRight, 2F);
        graphics.DrawLine(leftPen, 0, 0, 0, Height - 1);
        graphics.DrawLine(leftPen, 0, Height - 2, Width / 2, Height - 2);
        graphics.DrawLine(rightPen, Width - 1, 0, Width - 1, Height - 1);
        graphics.DrawLine(rightPen, Width / 2, Height - 2, Width - 1, Height - 2);
    }

    private static void DrawGlow(Graphics graphics, Rectangle bounds, Color color)
    {
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
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var outer = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        using var fill = new LinearGradientBrush(ClientRectangle, FillTop, FillBottom, LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(fill, outer);

        using var glowPen = new Pen(Color.FromArgb(72, GlowColor), 3F);
        using var borderPen = new Pen(BorderColor, 1.2F);
        e.Graphics.DrawRectangle(glowPen, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
        e.Graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }
}
