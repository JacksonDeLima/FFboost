using FFBoost.Core.Models;

namespace FFBoost.UI;

public class TechnicalReportForm : Form
{
    public TechnicalReportForm(TechnicalReport report)
    {
        Text = "Relatorio Tecnico";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(820, 760);
        BackColor = Color.FromArgb(4, 8, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(10);

        var topBar = BuildTopBar();

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 48,
            Text = "RELATORIO TECNICO",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(0, 224, 255),
            BackColor = Color.Transparent
        };

        var subtitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "Diagnostico consolidado da sessao e benchmark local",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(126, 182, 214),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point)
        };

        var contentBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            BackColor = Color.FromArgb(8, 12, 26),
            ForeColor = Color.FromArgb(235, 241, 255),
            Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
            Margin = new Padding(0),
            Text = BuildText(report)
        };

        var contentCard = new NeonPanel
        {
            Dock = DockStyle.Fill,
            BorderColor = Color.FromArgb(34, 112, 204),
            GlowColor = Color.FromArgb(42, 154, 255),
            FillTop = Color.FromArgb(8, 12, 26),
            FillBottom = Color.FromArgb(6, 10, 22),
            Padding = new Padding(14, 12, 14, 12)
        };
        contentCard.Controls.Add(contentBox);

        var btnClose = new Button
        {
            Text = "Fechar",
            Width = 140,
            Height = 40,
            BackColor = Color.FromArgb(23, 185, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point)
        };
        btnClose.FlatAppearance.BorderSize = 1;
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(149, 232, 255);
        btnClose.Click += (_, _) => Close();

        var buttonHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            BackColor = Color.Transparent
        };
        buttonHost.Controls.Add(btnClose);
        buttonHost.Resize += (_, _) =>
        {
            btnClose.Location = new Point(
                Math.Max(0, (buttonHost.Width - btnClose.Width) / 2),
                Math.Max(0, (buttonHost.Height - btnClose.Height) / 2));
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
            FillBottom = Color.FromArgb(5, 8, 20)
        };

        card.Padding = new Padding(18, 14, 18, 16);
        card.Controls.Add(contentCard);
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
        Controls.Add(root);
    }

    private static Panel BuildTopBar()
    {
        var title = new Label
        {
            Text = "FF BOOST REPORTS",
            AutoSize = true,
            ForeColor = Color.FromArgb(238, 243, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(0, 4)
        };

        var subtitle = new Label
        {
            Text = "Analise tecnica da sessao atual",
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

    private static string BuildText(TechnicalReport report)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "RESUMO",
            $"Perfil efetivo: {report.Profile}",
            $"Modo Free Fire: {YesNo(report.FreeFireModeEnabled)}",
            $"Tempo total: {report.Elapsed.TotalMilliseconds:0} ms",
            $"Score da sessao: {report.SessionScore:0.##}",
            string.Empty,
            "BENCHMARK",
            $"CPU: {report.CpuBefore}% -> {report.CpuAfter}%",
            $"RAM: {report.RamBefore} GB -> {report.RamAfter} GB",
            $"Carga RAM: {report.RamUsageBeforePercent:0.#}% -> {report.RamUsageAfterPercent:0.#}%",
            $"Processos: {report.ProcessesBefore} -> {report.ProcessesAfter}",
            $"Historico local: {report.Benchmark.SessionCount} sessoes, media {report.Benchmark.AvgScore:0.##}, delta {report.Benchmark.LastScoreDelta:+0.##;-0.##;0}",
            string.Empty,
            "ACOES APLICADAS",
            $"Plano de acao: kill {report.KillPlanCount}, suspend {report.SuspendPlanCount}",
            $"Encerrados: {report.KilledCount} ({FormatList(report.KilledProcesses)})",
            $"Suspensos: {report.SuspendedCount} ({FormatList(report.SuspendedProcesses)})",
            $"Memoria otimizada: {YesNo(report.MemoryOptimizationApplied)} | {report.MemoryOptimizedProcessCount} processo(s) | ~{report.MemoryRecoveredMb:0.#} MB",
            $"Processos compactados: {FormatList(report.MemoryOptimizedProcesses)}",
            $"Afinidade aplicada: {YesNo(report.AffinityApplied)}",
            $"Timer ajustado: {YesNo(report.TimerResolutionApplied)}",
            $"Plano alto desempenho: {YesNo(report.PowerPlanActivated)}",
            $"Turbo FPS: {YesNo(report.TurboModeApplied)}",
            $"Windows modo basico (Ultra): {YesNo(report.UltraVisualTweaksApplied)}",
            $"Modo gravacao: {YesNo(report.RecordingModeDetected)}",
            $"Overlays: {report.OverlayCount} ({FormatList(report.OverlayProcesses)})",
            string.Empty,
            "RANKING DE PROCESSOS",
            $"Antes: {FormatProcesses(report.TopProcessesBefore)}",
            $"Depois: {FormatProcesses(report.TopProcessesAfter)}",
            string.Empty,
            "RECOMENDACAO",
            $"Perfil recomendado: {report.Recommendation.RecommendedProfile} / Free Fire {YesNo(report.Recommendation.UseFreeFirePreset)}",
            $"Motivo: {report.Recommendation.Reason}",
            $"Sugestoes: {FormatList(report.Suggestions)}",
            string.Empty,
            "LOGICA DE PERFORMANCE",
            $"{FormatList(report.PerformanceReport)}"
        });
    }

    private static string FormatProcesses(IReadOnlyCollection<ProcessResourceUsage> items)
    {
        return items.Count == 0
            ? "nenhum"
            : string.Join(", ", items.Select(static x => $"{x.Name} {x.RamMb:0.#}MB/{x.CpuPercent:0.#}%"));
    }

    private static string FormatList(IReadOnlyCollection<string> items)
    {
        return items.Count == 0 ? "nenhum" : string.Join(", ", items);
    }

    private static string YesNo(bool value)
    {
        return value ? "sim" : "nao";
    }
}
