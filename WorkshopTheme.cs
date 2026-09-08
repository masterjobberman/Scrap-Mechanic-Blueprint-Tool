using System.Drawing.Drawing2D;

namespace GlassBoxBlueprintMaker;

internal static class WorkshopTheme
{
    public static readonly Color Accent = Color.FromArgb(255, 207, 53);
    public static readonly Color Steel = Color.FromArgb(27, 34, 36);
    public static readonly Color Muted = Color.FromArgb(146, 165, 165);
    public static readonly Color Mint = Color.FromArgb(149, 193, 215);
    public static void Grid(Graphics g, Rectangle area, int step = 24)
    {
        using var pen = new Pen(Color.FromArgb(26, 111, 175, 165));
        for (var x = area.Left; x < area.Right; x += step) g.DrawLine(pen, x, area.Top, x, area.Bottom);
        for (var y = area.Top; y < area.Bottom; y += step) g.DrawLine(pen, area.Left, y, area.Right, y);
    }
    public static GraphicsPath Plate(Rectangle r, int cut = 9)
    {
        var path = new GraphicsPath();
        path.AddLine(r.Left, r.Top, r.Right-cut*2, r.Top);
        path.AddArc(r.Right-cut*2,r.Top,cut*2,cut*2,270,90);
        path.AddLine(r.Right,r.Top+cut,r.Right,r.Bottom);
        path.AddLine(r.Right,r.Bottom,r.Left+cut,r.Bottom);
        path.AddArc(r.Left,r.Bottom-cut*2,cut*2,cut*2,90,90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class WorkshopHeader : Control
{
    internal Image? Craftbot;
    public WorkshopHeader() { DoubleBuffered = true; Dock = DockStyle.Fill; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var gradient = new LinearGradientBrush(ClientRectangle, Color.FromArgb(33, 40, 43), Color.FromArgb(22, 66, 88), 0f);
        g.FillRectangle(gradient, ClientRectangle);
        WorkshopTheme.Grid(g, new Rectangle(Width / 2, 0, Width / 2, Height));
        var center = new PointF(52, 49);
        using var orange = new SolidBrush(WorkshopTheme.Accent);
        using var dark = new SolidBrush(Color.FromArgb(20, 29, 30));
        // Workshop gear, with a glass-box silhouette at its hub.
        for (var i = 0; i < 8; i++)
        {
            var state = g.Save(); g.TranslateTransform(center.X, center.Y); g.RotateTransform(i * 45);
            g.FillRectangle(orange, -7, -35, 14, 14); g.Restore(state);
        }
        g.FillEllipse(orange, 23, 20, 58, 58); g.FillEllipse(dark, 33, 30, 38, 38);
        using var line = new Pen(Color.FromArgb(219, 241, 229), 2);
        var top = new Point(52, 36); var left = new Point(40, 43); var right = new Point(64, 43); var mid = new Point(52, 50);
        g.DrawPolygon(line, new[] { top, right, new Point(64, 57), new Point(52, 64), new Point(40, 57), left });
        g.DrawLine(line, left, mid); g.DrawLine(line, right, mid); g.DrawLine(line, mid, new Point(52, 64));
        using var eyebrow = new Font("Segoe UI", 8, FontStyle.Bold);
        using var title = new Font("Bahnschrift", 25, FontStyle.Bold);
        using var body = new Font("Segoe UI", 9);
        TextRenderer.DrawText(g, "S C R A P  M E C H A N I C   /   C O M M U N I T Y  T O O L", eyebrow, new Point(102, 12), WorkshopTheme.Accent);
        TextRenderer.DrawText(g, "GLASS BOX", title, new Point(98, 29), Color.FromArgb(242, 246, 236));
        TextRenderer.DrawText(g, "BLUEPRINT WORKSHOP", eyebrow, new Point(103, 70), WorkshopTheme.Muted);
        if (Width > 900)
        {
            var x = Width - 360;
            using var slogan = new Font("Bahnschrift", 15, FontStyle.Bold);
            TextRenderer.DrawText(g, "BUILD.  BOX.  CREATE.", slogan, new Point(x, 28), Color.FromArgb(228, 237, 229));
            TextRenderer.DrawText(g, "Survival items / Local + Workshop", body, new Point(x, 56), WorkshopTheme.Muted);
            if (Craftbot is not null) g.DrawImage(Craftbot, new Rectangle(Width-106, 1, 96, 96));
        }
        using var stripe = new Pen(WorkshopTheme.Accent, 3);
        g.DrawLine(stripe, 0, Height-3, Width, Height-3);
        for (var x = Width-180; x < Width; x += 16) g.DrawLine(stripe, x, Height-12, x+9, Height-3);
    }
}

internal sealed class WorkshopButton : Button
{
    private bool hover;
    public WorkshopButton() { DoubleBuffered = true; FlatStyle = FlatStyle.Flat; Cursor = Cursors.Hand; }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? WorkshopTheme.Steel);
        var primary = BackColor.R > 180 && BackColor.G > 70 && BackColor.B < 100;
        var fill = !Enabled ? Color.FromArgb(42, 49, 49) : primary ? (hover ? Color.FromArgb(255, 178, 66) : WorkshopTheme.Accent) : hover ? Color.FromArgb(49, 65, 65) : BackColor;
        using var path = WorkshopTheme.Plate(new Rectangle(1, 1, Width-3, Height-3));
        using var brush = new LinearGradientBrush(ClientRectangle, ControlPaint.Light(fill, .12f), fill, 90f); g.FillPath(brush, path);
        using var border = new Pen(primary && Enabled ? Color.FromArgb(255, 197, 112) : Color.FromArgb(69, 88, 87)); g.DrawPath(border, path);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, !Enabled ? WorkshopTheme.Muted : primary ? Color.FromArgb(26, 31, 26) : ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(6, 6, Width-12, Height-12));
    }
}

internal sealed class WorkshopPreview : PictureBox
{
    public WorkshopPreview() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        using var background = new LinearGradientBrush(ClientRectangle, Color.FromArgb(34, 83, 110), Color.FromArgb(18, 52, 70), 90f);
        g.FillRectangle(background, ClientRectangle);
        WorkshopTheme.Grid(g, ClientRectangle, 18);
        base.OnPaint(e);
        using var pen = new Pen(WorkshopTheme.Mint, 2);
        foreach (var (x, y, dx, dy) in new[] { (5,5,1,1), (Width-6,5,-1,1), (5,Height-6,1,-1), (Width-6,Height-6,-1,-1) })
        { g.DrawLine(pen,x,y,x+12*dx,y); g.DrawLine(pen,x,y,x,y+12*dy); }
        using var font = new Font("Consolas", 7.5f);
        TextRenderer.DrawText(g, "PREVIEW", font, new Point(14, 10), WorkshopTheme.Mint);
        if (Image is null) TextRenderer.DrawText(g, "YOUR NEXT CREATION\nSTARTS HERE", Font, ClientRectangle, WorkshopTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

internal sealed class WorkshopCategoryBox : ComboBox
{
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != 0x000F && m.Msg != 0x0318 && m.Msg != 0x0317) return;
        using var g = m.Msg == 0x000F ? Graphics.FromHwnd(Handle) : Graphics.FromHdc(m.WParam);
        using var background = new SolidBrush(BackColor);
        g.FillRectangle(background, ClientRectangle);
        using var border = new Pen(Focused ? WorkshopTheme.Accent : Color.FromArgb(76, 98, 109));
        g.DrawRectangle(border, 0, 0, Width-1, Height-1);
        TextRenderer.DrawText(g, SelectedItem?.ToString() ?? "All categories", Font, new Rectangle(8, 0, Width-30, Height), ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        using var arrow = new SolidBrush(WorkshopTheme.Accent);
        g.FillPolygon(arrow, new[] { new Point(Width-18,Height/2-2), new Point(Width-8,Height/2-2), new Point(Width-13,Height/2+3) });
    }
}

