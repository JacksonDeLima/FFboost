using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace FFBoost.Setup;

public class SetupForm : Form
{
    private readonly Label _statusLabel;
    private readonly Button _installButton;
    private readonly Button _uninstallButton;
    private readonly PictureBox _logoBox;
    private Image? _logoImage;

    public SetupForm()
    {
        Text = "FF Boost Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 500);
        BackColor = Color.FromArgb(4, 8, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(12);

        _logoBox = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 180,
            BackColor = Color.Transparent,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        var subtitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = "INSTALADOR FF BOOST PRO",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(244, 247, 255),
            BackColor = Color.Transparent
        };

        var infoLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 54,
            Padding = new Padding(24, 6, 24, 0),
            TextAlign = ContentAlignment.TopCenter,
            Text = "Instala ou remove o FF Boost em %LocalAppData%\\FFBoost e gerencia o atalho da area de trabalho.",
            ForeColor = Color.FromArgb(202, 213, 240),
            BackColor = Color.Transparent
        };

        _installButton = CreateButton("Instalar FF Boost", 230, 56, Color.FromArgb(23, 185, 255), Color.FromArgb(149, 232, 255));
        _installButton.Click += InstallButton_Click;

        _uninstallButton = CreateButton("Desinstalar", 190, 48, Color.FromArgb(24, 10, 22), Color.FromArgb(255, 90, 95));
        _uninstallButton.Click += UninstallButton_Click;

        var buttonHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = Color.Transparent
        };
        buttonHost.Controls.Add(_installButton);
        buttonHost.Controls.Add(_uninstallButton);
        buttonHost.Resize += (_, _) =>
        {
            const int spacing = 18;
            var totalWidth = _installButton.Width + _uninstallButton.Width + spacing;
            var startX = Math.Max(0, (buttonHost.Width - totalWidth) / 2);
            _installButton.Location = new Point(startX, Math.Max(0, (buttonHost.Height - _installButton.Height) / 2));
            _uninstallButton.Location = new Point(startX + _installButton.Width + spacing, Math.Max(0, (buttonHost.Height - _uninstallButton.Height) / 2));
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 16, 24, 16),
            Text = "Pronto para instalar ou desinstalar.",
            TextAlign = ContentAlignment.TopLeft,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(235, 241, 255),
            Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point)
        };

        var heroCard = new NeonPanel
        {
            Dock = DockStyle.Top,
            Height = 274,
            BorderColor = Color.FromArgb(60, 181, 255),
            GlowColor = Color.FromArgb(255, 107, 70),
            FillTop = Color.FromArgb(9, 12, 26),
            FillBottom = Color.FromArgb(5, 8, 20),
            Padding = new Padding(12)
        };
        heroCard.Controls.Add(infoLabel);
        heroCard.Controls.Add(subtitleLabel);
        heroCard.Controls.Add(_logoBox);

        var statusCard = new NeonPanel
        {
            Dock = DockStyle.Fill,
            BorderColor = Color.FromArgb(52, 103, 204),
            GlowColor = Color.FromArgb(52, 103, 204),
            FillTop = Color.FromArgb(9, 12, 26),
            FillBottom = Color.FromArgb(5, 8, 20),
            Padding = new Padding(10)
        };
        statusCard.Controls.Add(_statusLabel);

        var shell = new SciFiPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            BorderGlowLeft = Color.FromArgb(40, 88, 255),
            BorderGlowRight = Color.FromArgb(255, 102, 68)
        };
        shell.Controls.Add(statusCard);
        shell.Controls.Add(buttonHost);
        shell.Controls.Add(heroCard);

        Controls.Add(shell);

        LoadEmbeddedLogo();
        FormClosed += (_, _) => _logoImage?.Dispose();
    }

    private static Button CreateButton(string text, int width, int height, Color backColor, Color borderColor)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = borderColor;
        return button;
    }

    private void LoadEmbeddedLogo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Assets.ffboost-logo.png");
        if (stream == null)
            return;

        using var image = Image.FromStream(stream);
        _logoImage = new Bitmap(image);
        _logoBox.Image = _logoImage;
    }

    private void InstallButton_Click(object? sender, EventArgs e)
    {
        try
        {
            SetBusy(true);
            _statusLabel.ForeColor = Color.FromArgb(235, 241, 255);
            _statusLabel.Text = "Extraindo arquivos..." + Environment.NewLine + "Destino: %LocalAppData%\\FFBoost";
            Application.DoEvents();

            var targetDir = GetTargetDir();
            Directory.CreateDirectory(targetDir);

            ExtractResource("Payload.FFBoost.exe", Path.Combine(targetDir, "FFBoost.exe"));
            ExtractResource("Payload.config.json", Path.Combine(targetDir, "config.json"));
            CreateDesktopShortcut(targetDir);

            var exePath = Path.Combine(targetDir, "FFBoost.exe");
            _statusLabel.Text = "Instalacao concluida." + Environment.NewLine +
                                $"App: {exePath}" + Environment.NewLine +
                                "Atalho criado na area de trabalho.";

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = targetDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _statusLabel.ForeColor = Color.FromArgb(255, 120, 120);
            _statusLabel.Text = "Falha na instalacao." + Environment.NewLine + ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void UninstallButton_Click(object? sender, EventArgs e)
    {
        try
        {
            SetBusy(true);
            _statusLabel.ForeColor = Color.FromArgb(235, 241, 255);

            var targetDir = GetTargetDir();
            var desktopShortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "FF Boost.lnk");

            if (File.Exists(desktopShortcut))
                File.Delete(desktopShortcut);

            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, true);

            _statusLabel.Text = "Desinstalacao concluida." + Environment.NewLine +
                                "Arquivos removidos de %LocalAppData%\\FFBoost.";
        }
        catch (Exception ex)
        {
            _statusLabel.ForeColor = Color.FromArgb(255, 120, 120);
            _statusLabel.Text = "Falha na desinstalacao." + Environment.NewLine + ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _installButton.Enabled = !busy;
        _uninstallButton.Enabled = !busy;
    }

    private static string GetTargetDir()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FFBoost");
    }

    private static void ExtractResource(string resourceName, string outputPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Recurso nao encontrado: {resourceName}");
        using var file = File.Create(outputPath);
        stream.CopyTo(file);
    }

    private static void CreateDesktopShortcut(string targetDir)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = Path.Combine(desktop, "FF Boost.lnk");
        var targetExe = Path.Combine(targetDir, "FFBoost.exe");

        var shell = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell indisponivel.");

        dynamic shellObject = Activator.CreateInstance(shell)
            ?? throw new InvalidOperationException("Nao foi possivel criar o shell.");

        dynamic shortcut = shellObject.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetExe;
        shortcut.WorkingDirectory = targetDir;
        shortcut.IconLocation = targetExe;
        shortcut.Save();
    }
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
        DrawBorder(e.Graphics);
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

        using var fill = new LinearGradientBrush(ClientRectangle, FillTop, FillBottom, LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(fill, ClientRectangle);

        using var glowPen = new Pen(Color.FromArgb(72, GlowColor), 3F);
        using var borderPen = new Pen(BorderColor, 1.2F);
        e.Graphics.DrawRectangle(glowPen, 1, 1, Math.Max(0, Width - 3), Math.Max(0, Height - 3));
        e.Graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }
}
