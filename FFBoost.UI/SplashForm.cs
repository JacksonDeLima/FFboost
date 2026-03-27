namespace FFBoost.UI;

public class SplashForm : Form
{
    private const string SignatureText = "\u6587\uFF29\uFF4C\uFF55\uFF53\uFF49\uFF4F\uFF4E";
    private readonly System.Windows.Forms.Timer _timer;

    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        ClientSize = new Size(420, 220);
        BackColor = Color.FromArgb(10, 14, 24);

        var accentBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = Color.FromArgb(0, 224, 255)
        };

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 96,
            Text = "FF Boost",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(244, 247, 255)
        };

        var subtitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "CARREGANDO MODO TATICO",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(0, 224, 255)
        };

        var signatureLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            Text = SignatureText,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Italic, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(86, 239, 255)
        };

        Controls.Add(signatureLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(titleLabel);
        Controls.Add(accentBar);

        _timer = new System.Windows.Forms.Timer { Interval = 320 };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Close();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _timer.Start();
    }
}
