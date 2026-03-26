using FFBoost.Core.Models;

namespace FFBoost.UI;

public class TechnicalReportForm : Form
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";

    public TechnicalReportForm(TechnicalReport report)
    {
        Text = "Relatorio Tecnico";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 440);
        BackColor = Color.FromArgb(4, 8, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(12);

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Text = "Relatorio Tecnico Final",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(0, 224, 255),
            BackColor = Color.Transparent
        };

        var signatureLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(86, 239, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point)
        };

        var contentLabel = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 16),
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(235, 241, 255),
            Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text = BuildText(report)
        };

        var btnClose = new Button
        {
            Text = "Fechar",
            Width = 140,
            Height = 40,
            BackColor = Color.FromArgb(23, 185, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnClose.FlatAppearance.BorderSize = 1;
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(149, 232, 255);
        btnClose.Click += (_, _) => Close();

        var buttonHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
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
            Padding = new Padding(14)
        };

        var card = new NeonPanel
        {
            Dock = DockStyle.Fill,
            BorderColor = Color.FromArgb(60, 181, 255),
            GlowColor = Color.FromArgb(255, 107, 70),
            FillTop = Color.FromArgb(9, 12, 26),
            FillBottom = Color.FromArgb(5, 8, 20)
        };

        card.Controls.Add(contentLabel);
        card.Controls.Add(buttonHost);
        card.Controls.Add(signatureLabel);
        card.Controls.Add(titleLabel);
        shell.Controls.Add(card);
        Controls.Add(shell);
    }

    private static string BuildText(TechnicalReport report)
    {
        return string.Join(Environment.NewLine, new[]
        {
            $"Perfil: {report.Profile}",
            $"Modo Free Fire: {YesNo(report.FreeFireModeEnabled)}",
            $"CPU: {report.CpuBefore}% -> {report.CpuAfter}%",
            $"RAM: {report.RamBefore} GB -> {report.RamAfter} GB",
            $"Score da sessao: {report.SessionScore:0.##}",
            $"Benchmark local: {report.Benchmark.SessionCount} sessao(oes), media {report.Benchmark.AvgScore:0.##}, delta {report.Benchmark.LastScoreDelta:+0.##;-0.##;0}",
            $"Processos: {report.ProcessesBefore} -> {report.ProcessesAfter}",
            $"Plano de acao: kill {report.KillPlanCount}, suspend {report.SuspendPlanCount}",
            $"Encerrados: {report.KilledCount} ({FormatList(report.KilledProcesses)})",
            $"Suspensos: {report.SuspendedCount} ({FormatList(report.SuspendedProcesses)})",
            $"Overlays: {report.OverlayCount} ({FormatList(report.OverlayProcesses)})",
            $"Afinidade aplicada: {YesNo(report.AffinityApplied)}",
            $"Timer ajustado: {YesNo(report.TimerResolutionApplied)}",
            $"Plano alto desempenho: {YesNo(report.PowerPlanActivated)}",
            $"Modo gravacao: {YesNo(report.RecordingModeDetected)}",
            $"Perfil recomendado: {report.Recommendation.RecommendedProfile} / FF {YesNo(report.Recommendation.UseFreeFirePreset)}",
            $"Motivo: {report.Recommendation.Reason}",
            $"Tempo total: {report.Elapsed.TotalMilliseconds:0} ms",
            $"Sugestoes: {FormatList(report.Suggestions)}"
        });
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
