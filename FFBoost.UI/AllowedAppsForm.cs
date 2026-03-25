using FFBoost.Core.Services;

namespace FFBoost.UI;

public class AllowedAppsForm : Form
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";

    private readonly string _configPath;
    private readonly ConfigService _configService;
    private readonly ProcessScanner _scanner;
    private readonly ListBox _lstAllowed;
    private readonly ListBox _lstRunning;
    private readonly TextBox _txtProcessName;
    private readonly TextBox _txtRunningFilter;
    private readonly Label _lblFeedback;

    public AllowedAppsForm()
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        _configService = new ConfigService(_configPath);
        _scanner = new ProcessScanner();

        Text = "Apps Permitidos";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 470);
        MinimumSize = new Size(620, 470);
        BackColor = Color.FromArgb(10, 14, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var titleLabel = new Label
        {
            Text = "Apps Permitidos",
            Dock = DockStyle.Top,
            Height = 52,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(244, 247, 255)
        };

        var signatureLabel = new Label
        {
            Text = SignatureText,
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(86, 239, 255),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point)
        };

        var lblAllowed = new Label
        {
            Text = "Apps permitidos",
            ForeColor = Color.FromArgb(0, 224, 255),
            AutoSize = true,
            Location = new Point(20, 18)
        };

        var lblRunning = new Label
        {
            Text = "Processos em execucao",
            ForeColor = Color.FromArgb(0, 224, 255),
            AutoSize = true,
            Location = new Point(320, 18)
        };

        _lstAllowed = new ListBox
        {
            Location = new Point(20, 42),
            Size = new Size(250, 220),
            BackColor = Color.FromArgb(13, 19, 34),
            ForeColor = Color.FromArgb(235, 241, 255),
            BorderStyle = BorderStyle.FixedSingle
        };

        _lstRunning = new ListBox
        {
            Location = new Point(320, 42),
            Size = new Size(250, 220),
            BackColor = Color.FromArgb(13, 19, 34),
            ForeColor = Color.FromArgb(235, 241, 255),
            BorderStyle = BorderStyle.FixedSingle
        };

        _txtProcessName = new TextBox
        {
            Location = new Point(20, 280),
            Size = new Size(250, 23),
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        _txtRunningFilter = new TextBox
        {
            Location = new Point(320, 280),
            Size = new Size(250, 23),
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Filtrar processos"
        };
        _txtRunningFilter.TextChanged += (_, _) => LoadRunningProcesses();

        var btnAddManual = new Button
        {
            Text = "Adicionar manualmente",
            Location = new Point(20, 315),
            Size = new Size(250, 32),
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnAddManual.FlatAppearance.BorderColor = Color.FromArgb(0, 224, 255);
        btnAddManual.Click += BtnAddManual_Click;

        var btnAddSelected = new Button
        {
            Text = "Adicionar selecionado",
            Location = new Point(320, 312),
            Size = new Size(250, 32),
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnAddSelected.FlatAppearance.BorderColor = Color.FromArgb(0, 224, 255);
        btnAddSelected.Click += BtnAddSelected_Click;

        var btnRefreshRunning = new Button
        {
            Text = "Atualizar processos",
            Location = new Point(320, 351),
            Size = new Size(250, 29),
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRefreshRunning.FlatAppearance.BorderColor = Color.FromArgb(0, 224, 255);
        btnRefreshRunning.Click += BtnRefreshRunning_Click;

        var btnRemove = new Button
        {
            Text = "Remover selecionado",
            Location = new Point(20, 355),
            Size = new Size(250, 32),
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnRemove.FlatAppearance.BorderColor = Color.FromArgb(255, 90, 95);
        btnRemove.Click += BtnRemove_Click;

        _lblFeedback = new Label
        {
            Location = new Point(20, 400),
            Size = new Size(550, 24),
            ForeColor = Color.FromArgb(120, 255, 180),
            Text = "Pronto."
        };

        var footerSignature = new Label
        {
            Text = SignatureText,
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(88, 236, 255),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Italic, GraphicsUnit.Point)
        };

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0)
        };
        contentPanel.Controls.Add(lblAllowed);
        contentPanel.Controls.Add(lblRunning);
        contentPanel.Controls.Add(_lstAllowed);
        contentPanel.Controls.Add(_lstRunning);
        contentPanel.Controls.Add(_txtProcessName);
        contentPanel.Controls.Add(_txtRunningFilter);
        contentPanel.Controls.Add(btnAddManual);
        contentPanel.Controls.Add(btnAddSelected);
        contentPanel.Controls.Add(btnRefreshRunning);
        contentPanel.Controls.Add(btnRemove);
        contentPanel.Controls.Add(_lblFeedback);

        Controls.Add(contentPanel);
        Controls.Add(footerSignature);
        Controls.Add(signatureLabel);
        Controls.Add(titleLabel);

        LoadAllowedApps();
        LoadRunningProcesses();
    }

    private void LoadAllowedApps()
    {
        _lstAllowed.Items.Clear();

        var config = _configService.Load();
        foreach (var item in config.AllowedProcesses.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase))
            _lstAllowed.Items.Add(item);
    }

    private void LoadRunningProcesses()
    {
        _lstRunning.Items.Clear();

        var filter = _txtRunningFilter.Text.Trim();
        var processes = _scanner.GetRunningProcessNames();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            processes = processes
                .Where(x => x.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var processName in processes)
            _lstRunning.Items.Add(processName);
    }

    private void BtnAddManual_Click(object? sender, EventArgs e)
    {
        var processName = _txtProcessName.Text.Trim();
        if (string.IsNullOrWhiteSpace(processName))
            return;

        AddAllowedProcess(processName);
        _txtProcessName.Clear();
    }

    private void BtnAddSelected_Click(object? sender, EventArgs e)
    {
        if (_lstRunning.SelectedItem == null)
            return;

        var processName = _lstRunning.SelectedItem.ToString() ?? string.Empty;
        AddAllowedProcess(processName);
    }

    private void AddAllowedProcess(string processName)
    {
        var config = _configService.Load();

        if (!config.AllowedProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
        {
            config.AllowedProcesses.Add(processName);
            _configService.Save(config);
            _lblFeedback.ForeColor = Color.FromArgb(120, 255, 180);
            _lblFeedback.Text = $"Adicionado aos permitidos: {processName}";
        }
        else
        {
            _lblFeedback.ForeColor = Color.FromArgb(255, 215, 120);
            _lblFeedback.Text = $"Ja estava permitido: {processName}";
        }

        LoadAllowedApps();
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (_lstAllowed.SelectedItem == null)
            return;

        var selected = _lstAllowed.SelectedItem.ToString() ?? string.Empty;
        var config = _configService.Load();
        config.AllowedProcesses.RemoveAll(x => x.Equals(selected, StringComparison.OrdinalIgnoreCase));
        _configService.Save(config);
        _lblFeedback.ForeColor = Color.FromArgb(255, 215, 120);
        _lblFeedback.Text = $"Removido dos permitidos: {selected}";

        LoadAllowedApps();
    }

    private void BtnRefreshRunning_Click(object? sender, EventArgs e)
    {
        LoadRunningProcesses();
    }
}
