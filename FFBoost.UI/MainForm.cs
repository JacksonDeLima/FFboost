using FFBoost.Core.Rules;
using FFBoost.Core.Services;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace FFBoost.UI;

public class MainForm : Form
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";

    private readonly OptimizerService _optimizer;
    private readonly AdminService _adminService;
    private readonly ConfigService _configService;
    private readonly SystemMetricsService _metricsService;
    private readonly LogFileService _logFileService;
    private readonly Label _lblStatus;
    private readonly Label _lblAdminStatus;
    private readonly Label _lblSignature;
    private readonly ListBox _lstLogs;
    private readonly ComboBox _cmbProfile;
    private readonly Label _lblCpuInfo;
    private readonly Label _lblRamInfo;
    private readonly Button _btnOptimize;

    public MainForm()
    {
        Text = "FF Boost";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 640);
        MinimumSize = new Size(720, 640);
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
            new PerformanceManager(),
            new ProcessRules());

        _adminService = new AdminService();
        _metricsService = new SystemMetricsService();
        _logFileService = new LogFileService(baseDirectory);

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
            Text = "BOOST DE DESEMPENHO PARA MODO JOGO",
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
        _btnOptimize.Click += BtnOptimize_Click;

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

        var btnRestore = new Button
        {
            Text = "Restaurar",
            Width = 180,
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.FromArgb(235, 241, 255),
            Cursor = Cursors.Hand
        };
        btnRestore.FlatAppearance.BorderColor = Color.FromArgb(255, 90, 95);
        btnRestore.Click += BtnRestore_Click;

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
        btnClearLog.Click += BtnClearLog_Click;

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
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
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
        bottomButtonsHost.Controls.Add(btnRestore);
        bottomButtonsHost.Resize += (_, _) =>
        {
            const int spacing = 16;
            var totalWidth = btnAllowedApps.Width + btnRestore.Width + spacing;
            var startX = Math.Max(0, (bottomButtonsHost.Width - totalWidth) / 2);
            var y = Math.Max(0, (bottomButtonsHost.Height - btnAllowedApps.Height) / 2);
            btnAllowedApps.Location = new Point(startX, y);
            btnRestore.Location = new Point(startX + btnAllowedApps.Width + spacing, y);
        };

        var profileHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        profileHost.Controls.Add(_cmbProfile);
        profileHost.Controls.Add(lblProfile);
        profileHost.Resize += (_, _) =>
        {
            lblProfile.Location = new Point(Math.Max(0, (profileHost.Width - 220) / 2), 10);
            _cmbProfile.Location = new Point(lblProfile.Right + 10, 8);
        };

        var adminHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        adminHost.Controls.Add(_lblRamInfo);
        adminHost.Controls.Add(_lblCpuInfo);
        adminHost.Controls.Add(_lblAdminStatus);
        _lblAdminStatus.Dock = DockStyle.Top;
        _lblCpuInfo.Dock = DockStyle.Top;
        _lblRamInfo.Dock = DockStyle.Top;

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

        LoadAdminStatus();
        LoadProfile();
        Shown += MainForm_Shown;
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

    private void AddLogs(IEnumerable<string> logs)
    {
        foreach (var log in logs)
            _lstLogs.Items.Add($"[{DateTime.Now:HH:mm:ss}] {log}");

        if (_lstLogs.Items.Count > 0)
            _lstLogs.TopIndex = _lstLogs.Items.Count - 1;
    }

    private void BtnOptimize_Click(object? sender, EventArgs e)
    {
        _lstLogs.Items.Clear();

        var cpuBefore = _metricsService.GetCpuUsagePercentage();
        var ramBefore = _metricsService.GetUsedRamGb();
        var result = _optimizer.StartGameMode();
        var cpuAfter = _metricsService.GetCpuUsagePercentage();
        var ramAfter = _metricsService.GetUsedRamGb();

        _lblStatus.Text = result.status;
        AddLogs(result.logs);
        _lblCpuInfo.Text = $"CPU: {cpuBefore}% -> {cpuAfter}%";
        _lblRamInfo.Text = $"RAM: {ramBefore} GB -> {ramAfter} GB";

        var fullLog = new List<string>
        {
            $"Status: {result.status}",
            $"Perfil: {_cmbProfile.SelectedItem}",
            $"CPU: {cpuBefore}% -> {cpuAfter}%",
            $"RAM: {ramBefore} GB -> {ramAfter} GB"
        };
        fullLog.AddRange(result.logs);

        var logPath = _logFileService.SaveLog(fullLog);
        AddLogs(new[] { $"Log salvo em: {logPath}" });
    }

    private void BtnAllowedApps_Click(object? sender, EventArgs e)
    {
        using var form = new AllowedAppsForm();
        form.ShowDialog(this);
    }

    private void BtnRestore_Click(object? sender, EventArgs e)
    {
        var result = _optimizer.Restore();
        _lblStatus.Text = result.status;
        AddLogs(result.logs);
    }

    private void BtnAbout_Click(object? sender, EventArgs e)
    {
        using var form = new AboutForm();
        form.ShowDialog(this);
    }

    private void BtnClearLog_Click(object? sender, EventArgs e)
    {
        _lstLogs.Items.Clear();
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
    }

    private void CmbProfile_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_cmbProfile.SelectedItem == null)
            return;

        var config = _configService.Load();
        config.SelectedProfile = _cmbProfile.SelectedItem.ToString() ?? "Seguro";
        _configService.Save(config);
    }

    private void TryAutoOptimize()
    {
        var config = _configService.Load();
        if (!config.AutoOptimizeOnStartup)
            return;

        _btnOptimize.PerformClick();
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        Shown -= MainForm_Shown;
        TryAutoOptimize();
    }
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
