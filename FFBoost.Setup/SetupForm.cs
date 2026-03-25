using System.Reflection;

namespace FFBoost.Setup;

public class SetupForm : Form
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";

    private readonly Label _statusLabel;
    private readonly Button _installButton;
    private readonly Button _uninstallButton;

    public SetupForm()
    {
        Text = "FF Boost Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 380);
        BackColor = Color.FromArgb(10, 14, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var accentBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = Color.FromArgb(0, 224, 255)
        };

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 64,
            Text = "FF Boost",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(244, 247, 255)
        };

        var headerSignatureLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(86, 239, 255)
        };

        var subtitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Text = "INSTALADOR GAMER",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(0, 224, 255)
        };

        var infoLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 78,
            Padding = new Padding(32, 12, 32, 0),
            TextAlign = ContentAlignment.TopCenter,
            Text = "Instala ou remove o FF Boost em %LocalAppData%\\FFBoost e gerencia o atalho da area de trabalho.",
            ForeColor = Color.FromArgb(202, 213, 240)
        };

        _installButton = new Button
        {
            Text = "Instalar FF Boost",
            Width = 210,
            Height = 52,
            BackColor = Color.FromArgb(0, 198, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        _installButton.FlatAppearance.BorderSize = 0;
        _installButton.Click += InstallButton_Click;

        _uninstallButton = new Button
        {
            Text = "Desinstalar",
            Width = 170,
            Height = 44,
            BackColor = Color.FromArgb(18, 26, 46),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point),
            Cursor = Cursors.Hand
        };
        _uninstallButton.FlatAppearance.BorderColor = Color.FromArgb(255, 90, 95);
        _uninstallButton.Click += UninstallButton_Click;

        var buttonHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88
        };
        buttonHost.Controls.Add(_installButton);
        buttonHost.Controls.Add(_uninstallButton);
        buttonHost.Resize += (_, _) =>
        {
            const int spacing = 14;
            var totalWidth = _installButton.Width + _uninstallButton.Width + spacing;
            var startX = Math.Max(0, (buttonHost.Width - totalWidth) / 2);
            _installButton.Location = new Point(startX, Math.Max(0, (buttonHost.Height - _installButton.Height) / 2));
            _uninstallButton.Location = new Point(startX + _installButton.Width + spacing, Math.Max(0, (buttonHost.Height - _uninstallButton.Height) / 2));
        };

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 14, 24, 14),
            Text = "Pronto para instalar ou desinstalar.",
            TextAlign = ContentAlignment.TopLeft,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(14, 21, 38),
            ForeColor = Color.FromArgb(235, 241, 255),
            Font = new Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point)
        };

        var signatureLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(88, 236, 255),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Italic, GraphicsUnit.Point)
        };

        var watermarkLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(18, 0, 224, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point)
        };

        var statusHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 10, 24, 18)
        };
        statusHost.Controls.Add(watermarkLabel);
        statusHost.Controls.Add(_statusLabel);

        Controls.Add(statusHost);
        Controls.Add(signatureLabel);
        Controls.Add(buttonHost);
        Controls.Add(infoLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(headerSignatureLabel);
        Controls.Add(titleLabel);
        Controls.Add(accentBar);
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
            {
                File.Delete(desktopShortcut);
            }

            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

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
