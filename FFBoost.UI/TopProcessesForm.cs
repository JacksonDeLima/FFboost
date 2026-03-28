using System.Diagnostics;
using FFBoost.Core.Models;
using FFBoost.Core.Rules;
using FFBoost.Core.Services;

namespace FFBoost.UI;

public class TopProcessesForm : ThemedDialogForm
{
    private readonly ProcessAnalyzerService _processAnalyzerService;
    private readonly ProcessKiller _processKiller = new();
    private readonly ProcessRules _processRules = new();
    private readonly ListView _listView;
    private readonly Button _btnKill;
    private readonly Label _lblProcessName;
    private readonly Label _lblRiskBadge;
    private readonly Label _lblRiskExplanation;
    private readonly Label _lblProcessMeta;
    private readonly Label _lblProcessPath;
    private readonly Label _lblProcessDescription;

    public TopProcessesForm(ProcessAnalyzerService processAnalyzerService) : base("Top Processos Pesados", Color.FromArgb(255, 156, 72))
    {
        _processAnalyzerService = processAnalyzerService;

        ClientSize = new Size(632, 612);
        MinimumSize = new Size(632, 612);
        Padding = new Padding(10);

        var topBar = BuildTopBar();

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 46,
            Text = "TOP PROCESSOS PESADOS",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(0, 224, 255),
            BackColor = Color.Transparent
        };

        var subtitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "Ranking atual por RAM, CPU e disco com acao segura por risco.",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(126, 182, 214),
            BackColor = Color.Transparent
        };

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(8, 12, 26),
            ForeColor = Color.FromArgb(235, 241, 255),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
            FullRowSelect = true,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            HideSelection = false,
            MultiSelect = false,
            OwnerDraw = true,
            Scrollable = true,
            UseCompatibleStateImageBehavior = false,
            View = View.Details
        };
        _listView.Columns.Add("Rank", 56, HorizontalAlignment.Left);
        _listView.Columns.Add("Processo", 190, HorizontalAlignment.Left);
        _listView.Columns.Add("RAM", 96, HorizontalAlignment.Right);
        _listView.Columns.Add("CPU", 84, HorizontalAlignment.Right);
        _listView.Columns.Add("Disco", 112, HorizontalAlignment.Right);
        _listView.DrawColumnHeader += ListView_DrawColumnHeader;
        _listView.DrawItem += ListView_DrawItem;
        _listView.DrawSubItem += ListView_DrawSubItem;
        _listView.SelectedIndexChanged += (_, _) => UpdateSelectionDetails();
        _listView.Resize += (_, _) => AdjustListViewColumns();

        _lblProcessName = CreateDetailLabel("Selecione um processo para ver detalhes.");
        _lblRiskBadge = new Label
        {
            AutoSize = false,
            Width = 110,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(225, 231, 244),
            BackColor = Color.FromArgb(36, 42, 66),
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
        _lblRiskExplanation = CreateDetailLabel(string.Empty);
        _lblProcessMeta = CreateDetailLabel(string.Empty);
        _lblProcessPath = CreateDetailLabel(string.Empty);
        _lblProcessDescription = CreateDetailLabel(string.Empty);

        var detailsCard = new NeonPanel
        {
            Dock = DockStyle.Bottom,
            Height = 142,
            BorderColor = Color.FromArgb(34, 112, 204),
            GlowColor = Color.FromArgb(42, 154, 255),
            FillTop = Color.FromArgb(8, 12, 26),
            FillBottom = Color.FromArgb(6, 10, 22),
            Padding = new Padding(14, 12, 14, 12)
        };

        var detailsTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Text = "DETALHES DO PROCESSO",
            ForeColor = Color.FromArgb(108, 216, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        var headerRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 22,
            BackColor = Color.Transparent
        };
        headerRow.Controls.Add(_lblProcessName);
        headerRow.Controls.Add(_lblRiskBadge);
        headerRow.Resize += (_, _) =>
        {
            _lblRiskBadge.Location = new Point(Math.Max(0, headerRow.Width - _lblRiskBadge.Width), 0);
            _lblProcessName.Width = Math.Max(0, _lblRiskBadge.Left - 8);
        };

        var detailsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        detailsLayout.Controls.Add(_lblRiskExplanation, 0, 0);
        detailsLayout.Controls.Add(_lblProcessMeta, 0, 0);
        detailsLayout.Controls.Add(_lblProcessPath, 0, 1);
        detailsLayout.Controls.Add(_lblProcessDescription, 0, 2);

        detailsLayout.SetRow(_lblProcessMeta, 1);
        detailsLayout.SetRow(_lblProcessPath, 2);
        detailsLayout.SetRow(_lblProcessDescription, 3);

        detailsCard.Controls.Add(detailsLayout);
        detailsCard.Controls.Add(headerRow);
        detailsCard.Controls.Add(detailsTitle);

        var btnRefresh = new Button
        {
            Text = "Atualizar",
            Width = 124,
            Height = 40,
            BackColor = Color.FromArgb(23, 185, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
        btnRefresh.FlatAppearance.BorderSize = 1;
        btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(149, 232, 255);
        btnRefresh.Click += (_, _) => LoadProcesses();

        _btnKill = new Button
        {
            Text = "Encerrar Processo",
            Width = 154,
            Height = 40,
            BackColor = Color.FromArgb(160, 48, 48),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
        _btnKill.FlatAppearance.BorderSize = 1;
        _btnKill.FlatAppearance.BorderColor = Color.FromArgb(255, 120, 120);
        _btnKill.Click += (_, _) => KillSelectedProcess();

        var btnClose = new Button
        {
            Text = "Fechar",
            Width = 124,
            Height = 40,
            BackColor = Color.FromArgb(24, 30, 54),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
        btnClose.FlatAppearance.BorderSize = 1;
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(255, 107, 70);
        btnClose.Click += (_, _) => Close();

        var buttonHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            BackColor = Color.Transparent
        };
        buttonHost.Controls.Add(btnRefresh);
        buttonHost.Controls.Add(_btnKill);
        buttonHost.Controls.Add(btnClose);
        buttonHost.Resize += (_, _) =>
        {
            const int spacing = 14;
            var totalWidth = btnRefresh.Width + _btnKill.Width + btnClose.Width + (spacing * 2);
            var startX = Math.Max(0, (buttonHost.Width - totalWidth) / 2);
            btnRefresh.Location = new Point(startX, 10);
            _btnKill.Location = new Point(startX + btnRefresh.Width + spacing, 10);
            btnClose.Location = new Point(_btnKill.Right + spacing, 10);
        };

        var shell = new SciFiPanel
        {
            Dock = DockStyle.Fill,
            BorderGlowLeft = Color.FromArgb(40, 88, 255),
            BorderGlowRight = Color.FromArgb(255, 102, 68),
            Padding = new Padding(14, 12, 14, 14)
        };

        var card = new NeonPanel
        {
            Dock = DockStyle.Fill,
            BorderColor = Color.FromArgb(60, 181, 255),
            GlowColor = Color.FromArgb(255, 107, 70),
            FillTop = Color.FromArgb(9, 12, 26),
            FillBottom = Color.FromArgb(5, 8, 20),
            Padding = new Padding(18, 14, 18, 16)
        };

        card.Controls.Add(_listView);
        card.Controls.Add(detailsCard);
        card.Controls.Add(buttonHost);
        card.Controls.Add(subtitleLabel);
        card.Controls.Add(titleLabel);
        shell.Controls.Add(card);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(topBar, 0, 0);
        root.Controls.Add(shell, 0, 1);
        DialogContent.Controls.Add(root);

        Shown += (_, _) => ApplyOwnerBoundedSize();
        Resize += (_, _) => UiGeometry.ApplyRoundedRegion(this, 18);

        LoadProcesses();
    }

    private static Panel BuildTopBar()
    {
        var title = new Label
        {
            Text = "FF BOOST DIAGNOSTICS",
            AutoSize = true,
            ForeColor = Color.FromArgb(238, 243, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(0, 4)
        };

        var subtitle = new Label
        {
            Text = "Ranking e analise segura de processos",
            AutoSize = true,
            ForeColor = Color.FromArgb(126, 182, 214),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.75F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(0, 22)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        return panel;
    }

    private void LoadProcesses()
    {
        var processes = _processAnalyzerService.GetTopProcesses(12);

        _listView.BeginUpdate();
        _listView.Items.Clear();

        var rank = 1;
        foreach (var process in processes)
        {
            var item = new ListViewItem($"#{rank++}");
            item.SubItems.Add(process.Name);
            item.SubItems.Add($"{process.RamMb:0} MB");
            item.SubItems.Add($"{process.CpuPercent:0.0}%");
            item.SubItems.Add($"{process.DiskMbPerSecond:0.0} MB/s");
            item.Tag = process;
            ApplyRiskColors(item, GetRiskLevel(process));
            _listView.Items.Add(item);
        }

        if (_listView.Items.Count == 0)
        {
            var empty = new ListViewItem("-");
            empty.SubItems.Add("Nenhum processo pesado detectado agora.");
            empty.SubItems.Add("-");
            empty.SubItems.Add("-");
            empty.SubItems.Add("-");
            empty.Tag = null;
            _listView.Items.Add(empty);
        }

        _listView.EndUpdate();
        AdjustListViewColumns();
        if (_listView.Items.Count > 0)
            _listView.Items[0].Selected = true;
    }

    private void UpdateSelectionDetails()
    {
        if (_listView.SelectedItems.Count == 0 || _listView.SelectedItems[0].Tag is not ProcessResourceUsage process)
        {
            _lblProcessName.Text = "Selecione um processo para ver detalhes.";
            SetRiskBadge(ProcessRiskLevel.Optional);
            _lblRiskExplanation.Text = string.Empty;
            _lblProcessMeta.Text = string.Empty;
            _lblProcessPath.Text = string.Empty;
            _lblProcessDescription.Text = string.Empty;
            _btnKill.Enabled = false;
            return;
        }

        var riskLevel = GetRiskLevel(process);
        _lblProcessName.Text = $"{process.Name} (PID {process.ProcessId})";
        SetRiskBadge(riskLevel);
        _lblRiskExplanation.Text = BuildRiskExplanation(riskLevel, process);
        _lblProcessMeta.Text = $"RAM {process.RamMb:0} MB | CPU {process.CpuPercent:0.0}% | DISCO {process.DiskMbPerSecond:0.0} MB/s";
        _lblProcessPath.Text = string.IsNullOrWhiteSpace(process.FilePath)
            ? "Caminho: indisponivel"
            : $"Caminho: {process.FilePath}";

        var description = string.IsNullOrWhiteSpace(process.Description) ? "Descricao indisponivel" : process.Description;
        var company = string.IsNullOrWhiteSpace(process.CompanyName) ? "Empresa desconhecida" : process.CompanyName;
        _lblProcessDescription.Text = $"{description} | {company}";
        _btnKill.Enabled = riskLevel != ProcessRiskLevel.Risky && !process.Name.Equals("FFBoost", StringComparison.OrdinalIgnoreCase);
    }

    private void KillSelectedProcess()
    {
        if (_listView.SelectedItems.Count == 0 || _listView.SelectedItems[0].Tag is not ProcessResourceUsage process)
            return;

        if (_processRules.IsCritical(process.Name) || process.Name.Equals("FFBoost", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                this,
                $"O processo {process.Name} e considerado critico ou faz parte do FF Boost e nao sera encerrado por esta tela.",
                "Processo protegido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"Deseja encerrar {process.Name} (PID {process.ProcessId})?\n\nDescricao: {(string.IsNullOrWhiteSpace(process.Description) ? "indisponivel" : process.Description)}",
            "Confirmar encerramento",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
            return;

        try
        {
            using var targetProcess = Process.GetProcessById(process.ProcessId);
            var result = _processKiller.KillProcesses(new[] { targetProcess });

            if (result.KilledProcesses.Count > 0)
            {
                MessageBox.Show(
                    this,
                    $"{process.Name} foi encerrado com sucesso.",
                    "Processo encerrado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                LoadProcesses();
                return;
            }

            var failure = result.FailedProcesses.FirstOrDefault() ?? "Nao foi possivel encerrar o processo.";
            MessageBox.Show(this, failure, "Falha ao encerrar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Falha ao encerrar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Label CreateDetailLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = Color.FromArgb(225, 231, 244),
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            AutoEllipsis = true
        };
    }

    private ProcessRiskLevel GetRiskLevel(ProcessResourceUsage process)
    {
        if (_processRules.IsCritical(process.Name) || process.Name.Equals("FFBoost", StringComparison.OrdinalIgnoreCase))
            return ProcessRiskLevel.Risky;

        if (process.CpuPercent >= 10 || process.RamMb >= 300 || process.DiskMbPerSecond >= 2)
            return ProcessRiskLevel.Safe;

        return ProcessRiskLevel.Optional;
    }

    private void SetRiskBadge(ProcessRiskLevel riskLevel)
    {
        switch (riskLevel)
        {
            case ProcessRiskLevel.Safe:
                _lblRiskBadge.Text = "ZONA SEGURA";
                _lblRiskBadge.BackColor = Color.FromArgb(18, 58, 34);
                _lblRiskBadge.ForeColor = Color.FromArgb(121, 243, 171);
                break;
            case ProcessRiskLevel.Risky:
                _lblRiskBadge.Text = "NAO ENCERRAR";
                _lblRiskBadge.BackColor = Color.FromArgb(70, 18, 18);
                _lblRiskBadge.ForeColor = Color.FromArgb(255, 120, 120);
                break;
            default:
                _lblRiskBadge.Text = "USAR CUIDADO";
                _lblRiskBadge.BackColor = Color.FromArgb(64, 48, 12);
                _lblRiskBadge.ForeColor = Color.FromArgb(255, 205, 122);
                break;
        }
    }

    private static void ApplyRiskColors(ListViewItem item, ProcessRiskLevel riskLevel)
    {
        switch (riskLevel)
        {
            case ProcessRiskLevel.Safe:
                item.ForeColor = Color.FromArgb(121, 243, 171);
                break;
            case ProcessRiskLevel.Risky:
                item.ForeColor = Color.FromArgb(255, 120, 120);
                break;
            default:
                item.ForeColor = Color.FromArgb(255, 205, 122);
                break;
        }
    }

    private static string BuildRiskExplanation(ProcessRiskLevel riskLevel, ProcessResourceUsage process)
    {
        return riskLevel switch
        {
            ProcessRiskLevel.Safe => $"Risco: ZONA SEGURA. Processo com alto consumo e sem protecao critica. Pode encerrar se nao estiver usando agora.",
            ProcessRiskLevel.Risky => $"Risco: NAO ENCERRAR. Processo critico do sistema ou protegido pelo FF Boost.",
            _ => $"Risco: USAR CUIDADO. Processo nao e critico, mas pode impactar alguma ferramenta em uso. Verifique caminho e descricao antes de encerrar."
        };
    }

    private void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        if (e.Header is null)
            return;

        using var backBrush = new SolidBrush(Color.FromArgb(12, 18, 34));
        using var borderPen = new Pen(Color.FromArgb(28, 92, 170));
        using var headerFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
        TextRenderer.DrawText(
            e.Graphics,
            e.Header.Text,
            headerFont,
            e.Bounds,
            Color.FromArgb(214, 230, 255),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private void ListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        if (_listView.View != View.Details)
            e.DrawDefault = true;
    }

    private void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item is null || e.SubItem is null || e.Header is null)
            return;

        var isSelected = e.Item.Selected;
        var bounds = e.Bounds;
        var backColor = isSelected
            ? Color.FromArgb(24, 56, 102)
            : (e.ItemIndex % 2 == 0 ? Color.FromArgb(8, 12, 26) : Color.FromArgb(10, 14, 30));
        var foreColor = isSelected ? Color.FromArgb(242, 247, 255) : e.SubItem.ForeColor;
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

        if (e.Header.Text is "RAM" or "CPU" or "Disco")
            flags |= TextFormatFlags.Right;
        else
            flags |= TextFormatFlags.Left;

        using var backBrush = new SolidBrush(backColor);
        e.Graphics.FillRectangle(backBrush, bounds);

        if (isSelected)
        {
            using var linePen = new Pen(Color.FromArgb(90, 194, 255));
            e.Graphics.DrawRectangle(linePen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }

        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem.Text,
            _listView.Font,
            Rectangle.Inflate(bounds, -6, 0),
            foreColor,
            flags);
    }

    private void ApplyOwnerBoundedSize()
    {
        var preferredSize = new Size(632, 612);
        var minimumSize = new Size(600, 560);
        var boundedSize = CalculateBoundedSize(preferredSize, minimumSize);

        ClientSize = boundedSize;
        MinimumSize = boundedSize;
        MaximumSize = boundedSize;
        CenterToOwnerContent(boundedSize);
        UiGeometry.ApplyRoundedRegion(this, 18);
        AdjustListViewColumns();
    }

    private Size CalculateBoundedSize(Size preferredSize, Size minimumSize)
    {
        var referenceControl = Owner as Control ?? this;
        var workingArea = Screen.FromControl(referenceControl).WorkingArea;
        var ownerClientSize = Owner?.ClientSize ?? Size.Empty;

        var maxWidth = ownerClientSize.Width > 0
            ? Math.Min(ownerClientSize.Width - 24, workingArea.Width - 60)
            : workingArea.Width - 60;
        var maxHeight = ownerClientSize.Height > 0
            ? Math.Min(ownerClientSize.Height - 30, workingArea.Height - 60)
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
        var targetTop = ownerCenter.Y - (boundedSize.Height / 2) + 8;
        var workingArea = Screen.FromControl(Owner).WorkingArea;

        Location = new Point(
            Math.Max(workingArea.Left, Math.Min(targetLeft, workingArea.Right - Width)),
            Math.Max(workingArea.Top, Math.Min(targetTop, workingArea.Bottom - Height)));
    }

    private void AdjustListViewColumns()
    {
        if (_listView.ClientSize.Width <= 0 || _listView.Columns.Count < 5)
            return;

        var availableWidth = Math.Max(420, _listView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        var rankWidth = 58;
        var ramWidth = 94;
        var cpuWidth = 82;
        var diskWidth = 112;
        var processWidth = Math.Max(150, availableWidth - rankWidth - ramWidth - cpuWidth - diskWidth);

        _listView.Columns[0].Width = rankWidth;
        _listView.Columns[1].Width = processWidth;
        _listView.Columns[2].Width = ramWidth;
        _listView.Columns[3].Width = cpuWidth;
        _listView.Columns[4].Width = diskWidth;
    }
}
