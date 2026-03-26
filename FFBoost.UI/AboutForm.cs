using System.Reflection;

namespace FFBoost.UI;

public class AboutForm : Form
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";

    public AboutForm()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        Text = "Sobre FF Boost";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 300);
        BackColor = Color.FromArgb(4, 8, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(12);

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 62,
            Text = "FF Boost",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(244, 247, 255),
            BackColor = Color.Transparent
        };

        var signatureLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(86, 239, 255),
            BackColor = Color.Transparent
        };

        var infoLabel = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            TextAlign = ContentAlignment.TopCenter,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(220, 228, 245),
            Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            Text =
                $"Versao: {version}{Environment.NewLine}" +
                "Produto: FF Boost" + Environment.NewLine +
                "Studio: FF Boost Studio" + Environment.NewLine +
                $"Assinatura: {SignatureText}{Environment.NewLine}{Environment.NewLine}" +
                "Otimizador gamer para BlueStacks e Free Fire."
        };

        var btnClose = new Button
        {
            Text = "Fechar",
            Width = 140,
            Height = 40,
            BackColor = Color.FromArgb(23, 185, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 1;
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(149, 232, 255);
        btnClose.Click += (_, _) => Close();

        var buttonHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
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

        card.Controls.Add(infoLabel);
        card.Controls.Add(buttonHost);
        card.Controls.Add(signatureLabel);
        card.Controls.Add(titleLabel);
        shell.Controls.Add(card);
        Controls.Add(shell);
    }
}
