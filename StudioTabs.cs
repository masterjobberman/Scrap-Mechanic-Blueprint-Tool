namespace GlassBoxBlueprintMaker;

// Standard buttons retain keyboard navigation and accessibility without native tab chrome.
internal sealed class StudioTabs : Panel
{
    public List<Panel> TabPages { get; } = new();
    private readonly FlowLayoutPanel navigation = new() { Dock = DockStyle.Top, Height = 58, WrapContents = false, Padding = new Padding(6, 4, 0, 4) };
    private readonly Panel content = new() { Dock = DockStyle.Fill };
    private int selectedIndex;
    public event EventHandler? SelectedIndexChanged;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            selectedIndex = value;
            for (var i = 0; i < TabPages.Count; i++)
            {
                TabPages[i].Visible = i == value;
                navigation.Controls[i].BackColor = i == value ? Color.FromArgb(47, 65, 61) : Color.FromArgb(16, 23, 25);
                navigation.Controls[i].ForeColor = i == value ? Color.FromArgb(255, 164, 78) : Color.FromArgb(170, 182, 198);
            }
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public void Initialize()
    {
        BackColor = Color.FromArgb(16, 23, 25);
        Controls.Add(content);
        Controls.Add(navigation);
        for (var i = 0; i < TabPages.Count; i++)
        {
            var index = i;
            var button = new WorkshopButton { Text = TabPages[i].Text, Width = 270, Height = 44, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => SelectedIndex = index;
            navigation.Controls.Add(button);
            TabPages[i].Dock = DockStyle.Fill;
            content.Controls.Add(TabPages[i]);
        }
        SelectedIndex = 0;
    }
}

