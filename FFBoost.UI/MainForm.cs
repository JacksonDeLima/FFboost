using FFBoost.Core.Rules;
using FFBoost.Core.Services;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FFBoost.UI;

public class MainForm : Form
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";
    private const int HotkeyIdToggle = 9001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint WmHotkey = 0x0312;

    private readonly OptimizerService _optimizer;
    private readonly AdminService _adminService;
    private readonly ConfigService _configService;
    private readonly SystemMetricsService _metricsService;
    private readonly LogFileService _logFileService;
    private readonly TelemetryService _telemetryService;
    private readonly GameWatcherService _watcherService;
    private readonly Label _lblStatus;
    private readonly Label _lblAdminStatus;
    private readonly Label _lblSignature;
    private readonly ListBox _lstLogs;
    private readonly ComboBox _cmbProfile;
    private readonly CheckBox _chkFreeFireMode;
    private readonly Label _lblCpuInfo;
    private readonly Label _lblRamInfo;
    private readonly Button _btnOptimize;
    private readonly Button _btnRestore;
    private readonly NotifyIcon _trayIcon;
    private bool _exitRequested;
    private bool _watcherTriggeredChange;

    public MainForm()
    {
        Text = "FF Boost";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(740, 670);
        MinimumSize = new Size(740, 670);
        BackColor = Color.FromArgb(10, 14, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(baseDirectory, "config.json");

        _configService = new ConfigService(configPath);
        _optimizer = new OptimizerService(
            _configService,
            new ProcessScanner(),
            new ProcessKiller(),
            new ProcessSuspendService(),
            new PerformanceManager(),
            new TimerResolutionService(),
            new OverlayService(),
            new ProcessRules());

        _adminService = new AdminService();
        _metricsService = new SystemMetricsService();
        _logFileService = new LogFileService(baseDirectory);
        _telemetryService = new TelemetryService(baseDirectory);
        _watcherService = new GameWatcherService(new ProcessScanner(), _configService);

        var titleLabel = new Label
        {
            Text = "FF Boost",
            Dock = DockStyle.Top,
            Height = 72,
            Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(244, 247, 255),
            BackColor = Color.Transparent
        };

        var subtitleLabel = new Label
        {
            Text = "OTIMIZACAO DE SISTEMA PARA BAIXA LATENCIA",
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0, 224, 255),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        var headerSignatureLabel = new Label
        {
            Text = SignatureText,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(86, 239, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point)
        };

        _btnOptimize = new Button
        {
            Text = "Otimizar Agora",
            Width = 300,
            Height = 72,
            BackColor = Color.FromArgb(0, 198, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        _btnOptimize.FlatAppearance.BorderSize = 0;
        _btnOptimize.Click += (_, _) => ExecuteOptimize(manualTrigger: true);

        var btnAllowedApps = new Button
        {
            Text = "Apps Permitidos",
            Width = 180,
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.FromArgb(235, 241, 255),
            Cursor = Cursors.Hand
        };
        btnAllowedApps.FlatAppearance.BorderColor = Color.FromArgb(0, 224, 255);
        btnAllowedApps.Click += BtnAllowedApps_Click;

        _btnRestore = new Button
        {
            Text = "Reverter Tudo",
            Width = 180,
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.FromArgb(235, 241, 255),
            Cursor = Cursors.Hand
        };
        _btnRestore.FlatAppearance.BorderColor = Color.FromArgb(255, 90, 95);
        _btnRestore.Click += (_, _) => ExecuteRestore(manualTrigger: true);

        var btnAbout = new Button
        {
            Text = "Sobre",
            Width = 100,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(12, 18, 34),
            ForeColor = Color.FromArgb(190, 235, 255),
            Cursor = Cursors.Hand
        };
        btnAbout.FlatAppearance.BorderColor = Color.FromArgb(0, 224, 255);
        btnAbout.Click += BtnAbout_Click;

        var btnClearLog = new Button
        {
            Text = "Limpar Log",
            Width = 120,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(12, 18, 34),
            ForeColor = Color.FromArgb(190, 235, 255),
            Cursor = Cursors.Hand
        };
        btnClearLog.FlatAppearance.BorderColor = Color.FromArgb(0, 224, 255);

        var lblProfile = new Label
        {
            Dock = DockStyle.Left,
            Width = 70,
            Text = "PERFIL",
            ForeColor = Color.FromArgb(0, 224, 255),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        _cmbProfile = new ComboBox
        {
            Width = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _cmbProfile.SelectedIndexChanged += CmbProfile_SelectedIndexChanged;

        _chkFreeFireMode = new CheckBox
        {
            AutoSize = true,
            Text = "Free Fire + BlueStacks",
            ForeColor = Color.FromArgb(255, 120, 150),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        _chkFreeFireMode.CheckedChanged += ChkFreeFireMode_CheckedChanged;

        _lblAdminStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(255, 215, 120),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            Text = "Verificando permissoes..."
        };

        var lblLogsTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(0, 224, 255),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            Text = "LOG VISUAL"
        };

        _lblCpuInfo = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(190, 235, 255),
            Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "CPU: --"
        };

        _lblRamInfo = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(190, 235, 255),
            Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Text = "RAM: --"
        };

        _lstLogs = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(13, 19, 34),
            ForeColor = Color.FromArgb(235, 241, 255),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point)
        };
        btnClearLog.Click += (_, _) => _lstLogs.Items.Clear();

        _lblStatus = new Label
        {
            Text = "Pronto para otimizar.",
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16),
            TextAlign = ContentAlignment.TopLeft,
            BackColor = Color.FromArgb(13, 19, 34),
            ForeColor = Color.FromArgb(235, 241, 255),
            Font = new Font("Consolas", 11F, FontStyle.Regular, GraphicsUnit.Point)
        };

        var shellPanel = new GradientPanel
        {
            Dock = DockStyle.Fill,
            ColorTop = Color.FromArgb(10, 14, 24),
            ColorBottom = Color.FromArgb(20, 31, 56)
        };

        var accentBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = Color.FromArgb(0, 224, 255)
        };

        var topActionsHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.Transparent
        };
        topActionsHost.Controls.Add(btnAbout);
        topActionsHost.Controls.Add(btnClearLog);
        topActionsHost.Resize += (_, _) =>
        {
            btnAbout.Location = new Point(Math.Max(0, topActionsHost.Width - btnAbout.Width - 10), 2);
            btnClearLog.Location = new Point(Math.Max(0, topActionsHost.Width - btnAbout.Width - btnClearLog.Width - 18), 2);
        };

        var contentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(28, 16, 28, 24)
        };
        contentPanel.BackColor = Color.Transparent;
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));

        var optimizePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        optimizePanel.Controls.Add(_btnOptimize);
        optimizePanel.Resize += (_, _) =>
        {
            _btnOptimize.Location = new Point(
                Math.Max(0, (optimizePanel.Width - _btnOptimize.Width) / 2),
                Math.Max(0, (optimizePanel.Height - _btnOptimize.Height) / 2));
        };

        var bottomButtonsHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        bottomButtonsHost.Controls.Add(btnAllowedApps);
        bottomButtonsHost.Controls.Add(_btnRestore);
        bottomButtonsHost.Resize += (_, _) =>
        {
            const int spacing = 16;
            var totalWidth = btnAllowedApps.Width + _btnRestore.Width + spacing;
            var startX = Math.Max(0, (bottomButtonsHost.Width - totalWidth) / 2);
            var y = Math.Max(0, (bottomButtonsHost.Height - btnAllowedApps.Height) / 2);
            btnAllowedApps.Location = new Point(startX, y);
            _btnRestore.Location = new Point(startX + btnAllowedApps.Width + spacing, y);
        };

        var profileHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        profileHost.Controls.Add(_chkFreeFireMode);
        profileHost.Controls.Add(_cmbProfile);
        profileHost.Controls.Add(lblProfile);
        profileHost.Resize += (_, _) =>
        {
            lblProfile.Location = new Point(Math.Max(0, (profileHost.Width - 430) / 2), 10);
            _cmbProfile.Location = new Point(lblProfile.Right + 10, 8);
            _chkFreeFireMode.Location = new Point(_cmbProfile.Right + 18, 10);
        };

        var adminHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        adminHost.Controls.Add(_lblRamInfo);
        adminHost.Controls.Add(_lblCpuInfo);
        adminHost.Controls.Add(_lblAdminStatus);

        var logsHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        logsHost.Controls.Add(_lstLogs);
        logsHost.Controls.Add(lblLogsTitle);

        var statusContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };

        var watermarkLabel = new Label
        {
            Text = SignatureText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(18, 0, 224, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point)
        };
        statusContainer.Controls.Add(watermarkLabel);
        statusContainer.Controls.Add(_lblStatus);

        _lblSignature = new Label
        {
            Text = SignatureText,
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(88, 236, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Italic, GraphicsUnit.Point)
        };
        statusContainer.Controls.Add(_lblSignature);

        contentPanel.Controls.Add(subtitleLabel, 0, 0);
        contentPanel.Controls.Add(headerSignatureLabel, 0, 1);
        contentPanel.Controls.Add(profileHost, 0, 2);
        contentPanel.Controls.Add(optimizePanel, 0, 3);
        contentPanel.Controls.Add(bottomButtonsHost, 0, 4);
        contentPanel.Controls.Add(adminHost, 0, 5);
        contentPanel.Controls.Add(logsHost, 0, 6);
        contentPanel.Controls.Add(statusContainer, 0, 7);

        shellPanel.Controls.Add(contentPanel);
        shellPanel.Controls.Add(topActionsHost);
        shellPanel.Controls.Add(titleLabel);
        shellPanel.Controls.Add(accentBar);

        Controls.Add(shellPanel);

        _trayIcon = BuildTrayIcon();

        LoadAdminStatus();
        LoadProfile();
        ConfigureWatcher();
        Shown += MainForm_Shown;
        Resize += MainForm_Resize;
        FormClosing += MainForm_FormClosing;
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
            ? Color.FromArgb(120, 255, 180)
            : Color.FromArgb(255, 215, 120);
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

        var runningBefore = _optimizer.GetRunningProcesses();
        var cpuBefore = _metricsService.GetCpuUsagePercentage();
        var ramBefore = _metricsService.GetUsedRamGb();
        var result = _optimizer.StartGameMode();
        var cpuAfter = _metricsService.GetCpuUsagePercentage();
        var ramAfter = _metricsService.GetUsedRamGb();

        var report = _optimizer.LastTechnicalReport;
        report.CpuBefore = cpuBefore;
        report.CpuAfter = cpuAfter;
        report.RamBefore = ramBefore;
        report.RamAfter = ramAfter;
        report.ProcessesBefore = runningBefore.Count;
        report.ProcessesAfter = _optimizer.GetRunningProcesses().Count;

        _lblStatus.Text = result.status;
        AddLogs(result.logs);
        _lblCpuInfo.Text = $"CPU: {cpuBefore}% -> {cpuAfter}%";
        _lblRamInfo.Text = $"RAM: {ramBefore} GB -> {ramAfter} GB";

        var suggestions = _telemetryService.GetWhitelistSuggestions(runningBefore);
        foreach (var suggestion in suggestions)
            AddLogs(new[] { $"Sugestao de whitelist: {suggestion}" });

        var fullLog = new List<string>
        {
            $"Status: {result.status}",
            $"Perfil: {_cmbProfile.SelectedItem}",
            $"Modo FF: {(_chkFreeFireMode.Checked ? "ativo" : "padrao")}",
            $"CPU: {cpuBefore}% -> {cpuAfter}%",
            $"RAM: {ramBefore} GB -> {ramAfter} GB",
            $"Processos: {report.ProcessesBefore} -> {report.ProcessesAfter}",
            $"Plano: kill {report.KillPlanCount}, suspend {report.SuspendPlanCount}",
            $"Encerrados: {report.KilledCount}",
            $"Suspensos: {report.SuspendedCount}",
            $"Overlays: {report.OverlayCount}",
            $"Tempo: {report.Elapsed.TotalMilliseconds:0} ms"
        };
        fullLog.AddRange(result.logs);

        var logPath = _logFileService.SaveLog(fullLog);
        AddLogs(new[] { $"Log salvo em: {logPath}" });

        if (_configService.Load().TelemetryEnabled)
        {
            _telemetryService.Append(new FFBoost.Core.Models.TelemetryEntry
            {
                Timestamp = DateTime.Now,
                Profile = _cmbProfile.SelectedItem?.ToString() ?? "Seguro",
                CpuBefore = cpuBefore,
                CpuAfter = cpuAfter,
                RamBefore = ramBefore,
                RamAfter = ramAfter,
                KilledCount = report.KilledCount,
                SuspendedCount = report.SuspendedCount,
                KilledProcesses = report.KilledProcesses,
                RelaunchedProcesses = suggestions
            });
        }

        if (manualTrigger)
        {
            using var form = new TechnicalReportForm(report);
            form.ShowDialog(this);
        }
    }

    private void ExecuteRestore(bool manualTrigger)
    {
        var result = _optimizer.Restore();
        _lblStatus.Text = result.status;
        AddLogs(result.logs);

        if (manualTrigger)
        {
            _lblCpuInfo.Text = "CPU: --";
            _lblRamInfo.Text = "RAM: --";
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
        AddLogs(new[]
        {
            _chkFreeFireMode.Checked
                ? "Modo Free Fire + BlueStacks ativado."
                : "Modo Free Fire + BlueStacks desativado."
        });
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

internal sealed class GradientPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorTop { get; set; } = Color.Black;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorBottom { get; set; } = Color.Black;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, ColorTop, ColorBottom, LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}
