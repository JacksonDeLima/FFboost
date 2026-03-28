using FFBoost.Core.Services;

namespace FFBoost.UI;

public class AllowedAppsForm : ThemedDialogForm
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

    public AllowedAppsForm() : base("Apps Permitidos", Color.FromArgb(43, 184, 255))
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        _configService = new ConfigService(_configPath);
        _scanner = new ProcessScanner();

        ClientSize = new Size(628, 548);
        MinimumSize = new Size(628, 548);
        Padding = new Padding(12);

        var titleLabel = new Label
        {
            Text = "Apps Permitidos",
            Dock = DockStyle.Top,
            Height = 52,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(244, 247, 255),
            BackColor = Color.Transparent
        };

        var signatureLabel = new Label
        {
            Text = SignatureText,
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(86, 239, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point)
        };

        _lstAllowed = CreateListBox();
        _lstRunning = CreateListBox();

        _txtProcessName = CreateTextBox();
        _txtRunningFilter = CreateTextBox();
        _txtRunningFilter.PlaceholderText = "Filtrar processos";
        _txtRunningFilter.TextChanged += (_, _) => LoadRunningProcesses();

        var btnAddManual = CreateActionButton("Adicionar manualmente", Color.FromArgb(0, 224, 255));
        btnAddManual.Click += BtnAddManual_Click;

        var btnAddSelected = CreateActionButton("Adicionar selecionado", Color.FromArgb(0, 224, 255));
        btnAddSelected.Click += BtnAddSelected_Click;

        var btnRemove = CreateActionButton("Remover selecionado", Color.FromArgb(255, 90, 95));
        btnRemove.Click += BtnRemove_Click;

        var btnRefreshRunning = CreateActionButton("Atualizar processos", Color.FromArgb(0, 224, 255));
        btnRefreshRunning.Click += BtnRefreshRunning_Click;

        _lblFeedback = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(120, 255, 180),
            BackColor = Color.Transparent,
            Text = "Pronto.",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var columns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Color.Transparent,
            Padding = new Padding(18, 12, 18, 10)
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        columns.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        columns.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        columns.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        columns.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

        columns.Controls.Add(CreateSectionLabel("Apps permitidos"), 0, 0);
        columns.Controls.Add(CreateSectionLabel("Processos em execucao"), 1, 0);
        columns.Controls.Add(WrapControl(_lstAllowed, new Padding(0, 0, 10, 10)), 0, 1);
        columns.Controls.Add(WrapControl(_lstRunning, new Padding(10, 0, 0, 10)), 1, 1);
        columns.Controls.Add(WrapControl(_txtProcessName, new Padding(0, 0, 10, 8)), 0, 2);
        columns.Controls.Add(WrapControl(_txtRunningFilter, new Padding(10, 0, 0, 8)), 1, 2);
        columns.Controls.Add(WrapControl(btnAddManual, new Padding(0, 0, 10, 8)), 0, 3);
        columns.Controls.Add(WrapControl(btnAddSelected, new Padding(10, 0, 0, 8)), 1, 3);
        columns.Controls.Add(WrapControl(btnRemove, new Padding(0, 0, 10, 0)), 0, 4);
        columns.Controls.Add(WrapControl(btnRefreshRunning, new Padding(10, 0, 0, 0)), 1, 4);

        var footerSignature = new Label
        {
            Text = SignatureText,
            Dock = DockStyle.Right,
            Width = 180,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(88, 236, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Italic, GraphicsUnit.Point)
        };

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = Color.Transparent,
            Padding = new Padding(18, 0, 18, 6)
        };
        footer.Controls.Add(footerSignature);
        footer.Controls.Add(_lblFeedback);

        var card = new NeonPanel
        {
            Dock = DockStyle.Fill,
            BorderColor = Color.FromArgb(60, 181, 255),
            GlowColor = Color.FromArgb(255, 107, 70),
            FillTop = Color.FromArgb(9, 12, 26),
            FillBottom = Color.FromArgb(5, 8, 20),
            Padding = new Padding(8)
        };

        card.Controls.Add(columns);
        card.Controls.Add(footer);
        card.Controls.Add(signatureLabel);
        card.Controls.Add(titleLabel);

        var shell = new SciFiPanel
        {
            Dock = DockStyle.Fill,
            BorderGlowLeft = Color.FromArgb(40, 88, 255),
            BorderGlowRight = Color.FromArgb(255, 102, 68),
            Padding = new Padding(14)
        };

        shell.Controls.Add(card);
        DialogContent.Controls.Add(shell);

        Shown += (_, _) => ApplyOwnerBoundedSize();
        Resize += (_, _) => UiGeometry.ApplyRoundedRegion(this, 18);

        LoadAllowedApps();
        LoadRunningProcesses();
    }

    private static ListBox CreateListBox() =>
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(7, 10, 22),
            ForeColor = Color.FromArgb(235, 241, 255),
            BorderStyle = BorderStyle.None,
            IntegralHeight = false
        };

    private static TextBox CreateTextBox() =>
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(10, 18, 36),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

    private static Button CreateActionButton(string text, Color borderColor)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 38,
            BackColor = borderColor == Color.FromArgb(255, 90, 95)
                ? Color.FromArgb(24, 10, 22)
                : Color.FromArgb(8, 16, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderColor = borderColor;
        return button;
    }

    private static Label CreateSectionLabel(string text) =>
        new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = Color.FromArgb(0, 224, 255),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };

    private static Panel WrapControl(Control control, Padding margin)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = margin,
            BackColor = Color.Transparent
        };
        panel.Controls.Add(control);
        return panel;
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

    private void ApplyOwnerBoundedSize()
    {
        var preferredSize = new Size(628, 548);
        var minimumSize = new Size(580, 500);
        var boundedSize = CalculateBoundedSize(preferredSize, minimumSize);

        ClientSize = boundedSize;
        MinimumSize = boundedSize;
        MaximumSize = boundedSize;
        CenterToOwnerContent(boundedSize);
        UiGeometry.ApplyRoundedRegion(this, 18);
    }

    private Size CalculateBoundedSize(Size preferredSize, Size minimumSize)
    {
        var referenceControl = Owner as Control ?? this;
        var workingArea = Screen.FromControl(referenceControl).WorkingArea;
        var ownerClientSize = Owner?.ClientSize ?? Size.Empty;

        var maxWidth = ownerClientSize.Width > 0
            ? Math.Min(ownerClientSize.Width - 28, workingArea.Width - 60)
            : workingArea.Width - 60;
        var maxHeight = ownerClientSize.Height > 0
            ? Math.Min(ownerClientSize.Height - 36, workingArea.Height - 60)
            : workingArea.Height - 60;

        maxWidth = Math.Max(minimumSize.Width, maxWidth);
        maxHeight = Math.Max(minimumSize.Height, maxHeight);

        return new Size(
            Math.Min(preferredSize.Width, maxWidth),
            Math.Min(preferredSize.Height, maxHeight));
    }

    private void CenterToOwnerContent(Size boundedSize)
    {
        if (Owner is null)
            return;

        var ownerCenter = Owner.PointToScreen(new Point(Owner.ClientSize.Width / 2, Owner.ClientSize.Height / 2));
        var targetLeft = ownerCenter.X - (boundedSize.Width / 2);
        var targetTop = ownerCenter.Y - (boundedSize.Height / 2) + 18;
        var workingArea = Screen.FromControl(Owner).WorkingArea;

        Location = new Point(
            Math.Max(workingArea.Left, Math.Min(targetLeft, workingArea.Right - Width)),
            Math.Max(workingArea.Top, Math.Min(targetTop, workingArea.Bottom - Height)));
    }
}
