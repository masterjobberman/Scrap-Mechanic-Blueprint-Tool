using System.ComponentModel;

namespace GlassBoxBlueprintMaker;

internal sealed class MainForm : Form
{
    private readonly Color Orange = Color.FromArgb(242, 126, 32);
    private readonly Color Dark = Color.FromArgb(36, 38, 41);
    private readonly Color PanelGrey = Color.FromArgb(54, 57, 62);
    private readonly TextBox searchBox = new();
    private readonly ComboBox categoryBox = new();
    private readonly DataGridView itemGrid = new();
    private readonly DataGridView selectedGrid = new();
    private readonly NumericUpDown quantity = new();
    private readonly TextBox nameBox = new();
    private readonly TextBox gamePathBox = new();
    private readonly TextBox outputPathBox = new();
    private readonly PictureBox preview = new();
    private readonly Label status = new();
    private readonly Button addButton = new();
    private readonly Button removeButton = new();
    private readonly Button generateButton = new();
    private readonly SplitContainer split = new();
    private readonly BindingList<SelectedItem> selected = new();
    private List<GameItem> allItems = new();

    public MainForm()
    {
        Text = "Glass Box Blueprint Maker";
        MinimumSize = new Size(1040, 700);
        Size = new Size(1220, 780);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Dark;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        BuildUi();
        Shown += (_, _) => SizeMainPanels();
        Shown += async (_, _) => await LoadGameDataAsync();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(28, 29, 31), Padding = new Padding(20, 12, 20, 8) };
        var title = new Label { Text = "GLASS BOX  BLUEPRINT MAKER", Font = new Font("Segoe UI Semibold", 19, FontStyle.Bold), ForeColor = Orange, AutoSize = true, Location = new Point(18, 10) };
        var subtitle = new Label { Text = "Choose survival items • set quantities • create a new vanilla blueprint", ForeColor = Color.Gainsboro, AutoSize = true, Location = new Point(21, 45) };
        header.Controls.AddRange(new Control[] { title, subtitle });

        var paths = new TableLayoutPanel { Dock = DockStyle.Top, Height = 84, ColumnCount = 4, Padding = new Padding(12, 8, 12, 4), BackColor = PanelGrey };
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104)); paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88)); paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8));
        gamePathBox.Dock = DockStyle.Fill; outputPathBox.Dock = DockStyle.Fill;
        paths.Controls.Add(MakeLabel("Game folder"), 0, 0); paths.Controls.Add(gamePathBox, 1, 0); paths.Controls.Add(MakeButton("Browse", (_, _) => BrowseGame()), 2, 0);
        paths.Controls.Add(MakeLabel("Blueprints"), 0, 1); paths.Controls.Add(outputPathBox, 1, 1); paths.Controls.Add(MakeButton("Browse", (_, _) => BrowseOutput()), 2, 1);

        split.Dock = DockStyle.Fill;
        split.BackColor = Dark;
        split.Padding = new Padding(12, 10, 12, 8);
        split.Panel1.BackColor = Dark; split.Panel2.BackColor = PanelGrey;
        Controls.Add(split);
        Controls.Add(paths);
        Controls.Add(header);

        var leftTop = new TableLayoutPanel { Dock = DockStyle.Top, Height = 42, ColumnCount = 2 };
        leftTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70)); leftTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        searchBox.PlaceholderText = "Search item name, symbol, or UUID..."; searchBox.Dock = DockStyle.Fill;
        categoryBox.Dock = DockStyle.Fill; categoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
        searchBox.TextChanged += (_, _) => ApplyFilter(); categoryBox.SelectedIndexChanged += (_, _) => ApplyFilter();
        leftTop.Controls.Add(searchBox, 0, 0); leftTop.Controls.Add(categoryBox, 1, 0);
        split.Panel1.Controls.Add(itemGrid); split.Panel1.Controls.Add(leftTop);
        ConfigureItemGrid();

        var leftBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 7, 0, 0) };
        quantity.Minimum = 1; quantity.Maximum = 100000000; quantity.Value = 10; quantity.Width = 120; quantity.Font = new Font(Font, FontStyle.Bold);
        addButton.Text = "ADD TO PACK"; StylePrimary(addButton); addButton.Width = 145; addButton.Click += (_, _) => AddSelectedItem();
        leftBottom.Controls.Add(MakeLabel("Quantity")); leftBottom.Controls.Add(quantity); leftBottom.Controls.Add(addButton);
        split.Panel1.Controls.Add(leftBottom);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, Padding = new Padding(12), BackColor = PanelGrey };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 144)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        right.Controls.Add(MakeLabel("Blueprint name"), 0, 0); nameBox.Text = "My Glass Box Item Pack"; nameBox.Dock = DockStyle.Fill; right.Controls.Add(nameBox, 0, 1);
        right.Controls.Add(selectedGrid, 0, 2); ConfigureSelectedGrid();
        removeButton.Text = "REMOVE SELECTED"; removeButton.Dock = DockStyle.Fill; StyleSecondary(removeButton); removeButton.Click += (_, _) => RemoveSelected(); right.Controls.Add(removeButton, 0, 3);
        preview.SizeMode = PictureBoxSizeMode.CenterImage; preview.Dock = DockStyle.Fill; preview.BackColor = Color.FromArgb(27, 28, 30); right.Controls.Add(preview, 0, 4);
        status.Text = "Loading game items..."; status.ForeColor = Color.Gainsboro; status.AutoEllipsis = true; status.Dock = DockStyle.Fill; right.Controls.Add(status, 0, 5);
        generateButton.Text = "CREATE NEW BLUEPRINT"; generateButton.Dock = DockStyle.Fill; StylePrimary(generateButton); generateButton.Click += (_, _) => Generate(); right.Controls.Add(generateButton, 0, 6);
        split.Panel2.Controls.Add(right);
    }

    private void SizeMainPanels()
    {
        if (split.ClientSize.Width < 700) return;
        split.Panel2MinSize = Math.Min(350, split.ClientSize.Width / 3);
        var maximum = split.ClientSize.Width - split.Panel2MinSize - split.SplitterWidth;
        split.SplitterDistance = Math.Clamp((int)(split.ClientSize.Width * 0.66), split.Panel1MinSize, maximum);
    }

    private void ConfigureItemGrid()
    {
        itemGrid.Dock = DockStyle.Fill; itemGrid.BackgroundColor = Dark; itemGrid.BorderStyle = BorderStyle.None; itemGrid.RowHeadersVisible = false; itemGrid.AllowUserToAddRows = false; itemGrid.AllowUserToDeleteRows = false; itemGrid.ReadOnly = true; itemGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; itemGrid.MultiSelect = false; itemGrid.AutoGenerateColumns = false; itemGrid.RowTemplate.Height = 68;
        itemGrid.Columns.Add(new DataGridViewImageColumn { DataPropertyName = "Icon", HeaderText = "", Width = 70, ImageLayout = DataGridViewImageCellLayout.Zoom });
        itemGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Title", HeaderText = "ITEM", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        itemGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Category", HeaderText = "TYPE", Width = 90 });
        itemGrid.CellDoubleClick += (_, _) => AddSelectedItem(); StyleGrid(itemGrid);
    }

    private void ConfigureSelectedGrid()
    {
        selectedGrid.Dock = DockStyle.Fill; selectedGrid.BackgroundColor = Dark; selectedGrid.BorderStyle = BorderStyle.None; selectedGrid.RowHeadersVisible = false; selectedGrid.AllowUserToAddRows = false; selectedGrid.AllowUserToDeleteRows = false; selectedGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; selectedGrid.AutoGenerateColumns = false;
        selectedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ItemTitle", HeaderText = "SELECTED ITEM", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
        selectedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "QTY", Width = 75 });
        selectedGrid.DataError += (_, _) => { };
        selectedGrid.CellEndEdit += (_, _) => { NormalizeQuantities(); RefreshPreview(); };
        StyleGrid(selectedGrid);
    }

    private async Task LoadGameDataAsync(bool detectPaths = true)
    {
        if (detectPaths || string.IsNullOrWhiteSpace(gamePathBox.Text)) gamePathBox.Text = GameDataLoader.FindGameRoot() ?? "";
        if (detectPaths || string.IsNullOrWhiteSpace(outputPathBox.Text)) outputPathBox.Text = GameDataLoader.FindBlueprintRoot() ?? "";
        if (!GameDataLoader.IsGameRoot(gamePathBox.Text)) { status.Text = "Choose your Scrap Mechanic game folder."; return; }
        SetBusy(true, "Loading item names and game icons...");
        try
        {
            allItems = await Task.Run(() => GameDataLoader.LoadItems(gamePathBox.Text));
            categoryBox.Items.Clear(); categoryBox.Items.Add("All categories");
            foreach (var category in allItems.Select(x => x.Category).Distinct().OrderBy(x => x)) categoryBox.Items.Add(category);
            categoryBox.SelectedIndex = 0;
            ApplyFilter(); status.Text = $"Ready — {allItems.Count:N0} survival items loaded.";
        }
        catch (Exception ex) { ShowError(ex.Message); status.Text = "Could not load game items."; }
        finally { SetBusy(false); }
    }

    private void ApplyFilter()
    {
        var query = searchBox.Text.Trim(); var category = categoryBox.SelectedItem?.ToString();
        var view = allItems.Where(i => (category is null || category == "All categories" || i.Category == category) && (query.Length == 0 || i.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) || i.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase) || i.Uuid.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
        itemGrid.DataSource = view;
    }

    private void AddSelectedItem()
    {
        if (itemGrid.CurrentRow?.DataBoundItem is not GameItem item) return;
        var existing = selected.FirstOrDefault(x => x.Item.Uuid == item.Uuid);
        if (existing is null) selected.Add(new SelectedItem { Item = item, Quantity = (int)quantity.Value }); else existing.Quantity = checked(existing.Quantity + (int)quantity.Value);
        RefreshSelectedGrid();
    }

    private void RemoveSelected()
    {
        if (selectedGrid.CurrentRow?.Tag is SelectedItem row) selected.Remove(row);
        RefreshSelectedGrid();
    }

    private void RefreshSelectedGrid()
    {
        selectedGrid.Rows.Clear();
        foreach (var entry in selected)
        {
            var row = selectedGrid.Rows.Add(entry.Item.Title, entry.Quantity);
            selectedGrid.Rows[row].Tag = entry;
        }
        RefreshPreview();
        status.Text = selected.Count == 0 ? $"Ready — {allItems.Count:N0} survival items loaded." : $"Pack contains {selected.Count} glass box{(selected.Count == 1 ? "" : "es")}.";
    }

    private void NormalizeQuantities()
    {
        foreach (DataGridViewRow row in selectedGrid.Rows)
            if (row.Tag is SelectedItem entry)
            {
                if (!int.TryParse(row.Cells[1].Value?.ToString(), out var value) || value < 1) value = 1;
                entry.Quantity = value; row.Cells[1].Value = value;
            }
    }

    private void RefreshPreview()
    {
        var old = preview.Image;
        preview.Image = selected.Count == 0 ? null : BlueprintWriter.RenderIcon(selected);
        old?.Dispose();
    }

    private void Generate()
    {
        NormalizeQuantities();
        try
        {
            var folder = BlueprintWriter.Create(outputPathBox.Text.Trim(), nameBox.Text, selected);
            status.Text = "Created " + Path.GetFileName(folder);
            MessageBox.Show(this, $"Blueprint created successfully.\n\n{folder}\n\nRestart Scrap Mechanic if it is already open.", "Blueprint created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private async void BrowseGame()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select the Scrap Mechanic game folder", UseDescriptionForTitle = true, InitialDirectory = gamePathBox.Text };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        gamePathBox.Text = dialog.SelectedPath;
        await LoadGameDataAsync(false);
    }

    private void BrowseOutput()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select your Scrap Mechanic Blueprints folder", UseDescriptionForTitle = true, InitialDirectory = outputPathBox.Text };
        if (dialog.ShowDialog(this) == DialogResult.OK) outputPathBox.Text = dialog.SelectedPath;
    }

    private Label MakeLabel(string text) => new() { Text = text, ForeColor = Color.Gainsboro, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(3, 5, 3, 0) };
    private Button MakeButton(string text, EventHandler action) { var button = new Button { Text = text, Dock = DockStyle.Fill }; StyleSecondary(button); button.Click += action; return button; }
    private void StylePrimary(Button button) { button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = Orange; button.ForeColor = Color.FromArgb(25, 25, 25); button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold); button.Cursor = Cursors.Hand; }
    private void StyleSecondary(Button button) { button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = Orange; button.FlatAppearance.BorderSize = 1; button.BackColor = PanelGrey; button.ForeColor = Color.White; button.Cursor = Cursors.Hand; }
    private void StyleGrid(DataGridView grid) { grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 33, 36); grid.ColumnHeadersDefaultCellStyle.ForeColor = Orange; grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold); grid.DefaultCellStyle.BackColor = PanelGrey; grid.DefaultCellStyle.ForeColor = Color.White; grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(185, 91, 20); grid.DefaultCellStyle.SelectionForeColor = Color.White; grid.GridColor = Color.FromArgb(75, 77, 81); }
    private void SetBusy(bool busy, string? message = null) { UseWaitCursor = busy; addButton.Enabled = !busy; generateButton.Enabled = !busy; if (message is not null) status.Text = message; }
    private void ShowError(string message) => MessageBox.Show(this, message, "Glass Box Blueprint Maker", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
