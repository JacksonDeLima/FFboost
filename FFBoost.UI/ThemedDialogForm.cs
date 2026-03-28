using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace FFBoost.UI;

public abstract class ThemedDialogForm : Form
{
    private const int CsDropShadow = 0x00020000;
    private const int WmNclButtonDown = 0x00A1;
    private const int HtCaption = 0x0002;

    protected Panel DialogContent { get; }

    protected ThemedDialogForm(string titleText, Color accentColor)
    {
        Text = titleText;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(4, 8, 18);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(1);

        var titleBar = BuildTitleBar(titleText, accentColor);

        DialogContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(titleBar, 0, 0);
        root.Controls.Add(DialogContent, 0, 1);

        Controls.Add(root);

        Shown += (_, _) => UiGeometry.ApplyRoundedRegion(this, 18);
        Resize += (_, _) => UiGeometry.ApplyRoundedRegion(this, 18);
        HandleCreated += (_, _) => WindowEffects.ApplyPreferredWindowChrome(Handle, preferRoundedCorners: true);
    }

    private Panel BuildTitleBar(string titleText, Color accentColor)
    {
        var titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = titleText.ToUpperInvariant(),
            Padding = new Padding(16, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(236, 242, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point)
        };

        var accentLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 6,
            BackColor = accentColor
        };

        var closeButton = new DialogChromeButton
        {
            Dock = DockStyle.Right,
            Width = 52,
            Cursor = Cursors.Hand,
            TabStop = false,
            ButtonKind = DialogChromeButtonKind.Close
        };
        closeButton.Click += (_, _) => Close();

        var minimizeButton = new DialogChromeButton
        {
            Dock = DockStyle.Right,
            Width = 44,
            Cursor = Cursors.Hand,
            TabStop = false,
            ButtonKind = DialogChromeButtonKind.Minimize
        };
        minimizeButton.Click += (_, _) => WindowState = FormWindowState.Minimized;

        var titleBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(8, 12, 26)
        };

        var divider = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(46, 88, 154)
        };

        var glowLine = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 2,
            BackColor = Color.FromArgb(32, accentColor)
        };

        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(closeButton);
        titleBar.Controls.Add(minimizeButton);
        titleBar.Controls.Add(accentLabel);
        titleBar.Controls.Add(glowLine);
        titleBar.Controls.Add(divider);

        WireDrag(titleBar);
        WireDrag(titleLabel);
        return titleBar;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ClassStyle |= CsDropShadow;
            return createParams;
        }
    }

    private void WireDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();
            SendMessage(Handle, WmNclButtonDown, HtCaption, 0);
        };
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}

internal enum DialogChromeButtonKind
{
    Minimize,
    Close
}

internal sealed class DialogChromeButton : Control
{
    private bool _hovered;
    private bool _pressed;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DialogChromeButtonKind ButtonKind { get; set; } = DialogChromeButtonKind.Close;

    public DialogChromeButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.UserPaint,
            true);

        BackColor = Color.FromArgb(8, 12, 26);
        ForeColor = Color.FromArgb(244, 247, 255);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (Width <= 0 || Height <= 0)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(8, 6, Math.Max(0, Width - 16), Math.Max(0, Height - 12));
        if (_hovered || _pressed)
        {
            var fillColor = ResolveFillColor();
            var borderColor = ResolveBorderColor();

            using var path = UiGeometry.CreateRoundedPath(bounds, 10);
            using var fillBrush = new SolidBrush(fillColor);
            using var borderPen = new Pen(borderColor, 1.1F);
            e.Graphics.FillPath(fillBrush, path);
            e.Graphics.DrawPath(borderPen, path);
        }

        var iconBounds = Rectangle.Inflate(bounds, ButtonKind == DialogChromeButtonKind.Close ? -12 : -11, -10);
        using var iconPen = new Pen(_pressed ? Color.FromArgb(255, 222, 222) : ForeColor, 1.8F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        if (ButtonKind == DialogChromeButtonKind.Minimize)
        {
            var y = iconBounds.Bottom - 2;
            e.Graphics.DrawLine(iconPen, iconBounds.Left + 1, y, iconBounds.Right - 1, y);
        }
        else
        {
            e.Graphics.DrawLine(iconPen, iconBounds.Left, iconBounds.Top, iconBounds.Right, iconBounds.Bottom);
            e.Graphics.DrawLine(iconPen, iconBounds.Right, iconBounds.Top, iconBounds.Left, iconBounds.Bottom);
        }
    }

    private Color ResolveFillColor()
    {
        return ButtonKind == DialogChromeButtonKind.Minimize
            ? (_pressed ? Color.FromArgb(132, 26, 40, 68) : Color.FromArgb(96, 18, 30, 54))
            : (_pressed ? Color.FromArgb(148, 124, 30, 30) : Color.FromArgb(112, 88, 24, 24));
    }

    private Color ResolveBorderColor()
    {
        return ButtonKind == DialogChromeButtonKind.Minimize
            ? (_pressed ? Color.FromArgb(210, 130, 194, 255) : Color.FromArgb(168, 84, 168, 255))
            : (_pressed ? Color.FromArgb(228, 255, 118, 118) : Color.FromArgb(180, 255, 96, 96));
    }
}

internal static class WindowEffects
{
    private const int DwMWaNCRenderingPolicy = 2;
    private const int DwMWaUseImmersiveDarkMode = 20;
    private const int DwMWaWindowCornerPreference = 33;
    private const int DwmncrpEnabled = 2;
    private const int DwmwcpRound = 2;

    public static void ApplyPreferredWindowChrome(IntPtr handle, bool preferRoundedCorners)
    {
        if (!OperatingSystem.IsWindows() || handle == IntPtr.Zero)
            return;

        try
        {
            var enableNcRendering = DwmncrpEnabled;
            DwmSetWindowAttribute(handle, DwMWaNCRenderingPolicy, ref enableNcRendering, sizeof(int));

            var darkMode = 1;
            DwmSetWindowAttribute(handle, DwMWaUseImmersiveDarkMode, ref darkMode, sizeof(int));

            if (preferRoundedCorners)
            {
                var roundedCorners = DwmwcpRound;
                DwmSetWindowAttribute(handle, DwMWaWindowCornerPreference, ref roundedCorners, sizeof(int));
            }
        }
        catch
        {
            // Best-effort visual enhancement only.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
}
