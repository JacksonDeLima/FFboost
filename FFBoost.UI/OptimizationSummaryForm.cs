using FFBoost.Core.Models;

namespace FFBoost.UI;

public class OptimizationSummaryForm : ThemedDialogForm
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";

    public OptimizationSummaryForm(OptimizationResult result) : base("Resumo da Otimizacao", result.Success ? Color.FromArgb(0, 224, 255) : Color.FromArgb(255, 120, 120))
    {
        ClientSize = new Size(500, 420);
        BackColor = Color.FromArgb(9, 13, 24);

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 62,
            Text = result.Success ? "Resumo Tatico" : "Falha na Otimizacao",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = result.Success ? Color.FromArgb(0, 224, 255) : Color.FromArgb(255, 120, 120)
        };

        var headerSignatureLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(86, 239, 255),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point)
        };

        var summaryLabel = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            Font = new Font("Consolas", 11F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(235, 241, 255),
            BackColor = Color.FromArgb(14, 21, 38),
            BorderStyle = BorderStyle.FixedSingle,
            Text = BuildDetailText(result)
        };

        var btnClose = new Button
        {
            Text = "Fechar",
            Width = 140,
            Height = 42,
            BackColor = Color.FromArgb(0, 198, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => Close();

        var buttonHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 78
        };
        buttonHost.Controls.Add(btnClose);
        buttonHost.Resize += (_, _) =>
        {
            btnClose.Location = new Point(
                Math.Max(0, (buttonHost.Width - btnClose.Width) / 2),
                Math.Max(0, (buttonHost.Height - btnClose.Height) / 2));
        };

        var signatureLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 22,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(88, 236, 255),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Italic, GraphicsUnit.Point)
        };

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 0, 18, 12)
        };
        contentHost.Controls.Add(summaryLabel);

        DialogContent.Controls.Add(contentHost);
        DialogContent.Controls.Add(buttonHost);
        DialogContent.Controls.Add(signatureLabel);
        DialogContent.Controls.Add(headerSignatureLabel);
        DialogContent.Controls.Add(titleLabel);
    }

    private static string BuildDetailText(OptimizationResult result)
    {
        var lines = new List<string>
        {
            result.Success ? "Modo jogo ativado com sucesso." : result.Message,
            string.Empty,
            $"Emulador detectado: {FormatList(result.DetectedEmulators)}",
            $"Discord detectado: {YesNo(result.DiscordDetected)}",
            $"Gravador detectado: {FormatList(result.DetectedRecorders)}",
            $"Processos encerrados: {FormatKilled(result.KilledProcesses)}",
            $"Processos ignorados: {result.IgnoredCount}",
            $"Prioridade elevada: {YesNo(result.EmulatorPrioritized)}",
            $"Plano de energia alterado: {YesNo(result.PowerPlanChanged)}"
        };

        if (!string.IsNullOrWhiteSpace(result.PreviousPowerPlanName))
        {
            lines.Add($"Plano anterior: {result.PreviousPowerPlanName}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatKilled(IReadOnlyCollection<string> killedProcesses)
    {
        return killedProcesses.Count == 0 ? "nenhum" : string.Join(", ", killedProcesses);
    }

    private static string FormatList(IReadOnlyCollection<string> values)
    {
        return values.Count == 0 ? "nenhum" : string.Join(", ", values);
    }

    private static string YesNo(bool value)
    {
        return value ? "sim" : "nao";
    }
}
