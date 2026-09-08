using System.ComponentModel;

namespace GlassBoxBlueprintMaker;

internal sealed class MainForm : Form
{
    private readonly Color Orange = Color.FromArgb(242, 126, 32);
    private readonly Color Dark = Color.FromArgb(16, 23, 25);
    private readonly Color PanelGrey = Color.FromArgb(29, 38, 39);
    private readonly WorkshopHeader workshopHeader = new();
    private bool busy;
    private int libraryLoadVersion;
    private readonly TextBox searchBox = new();
    private readonly WorkshopCategoryBox categoryBox = new();
    private readonly DataGridView itemGrid = new();
    private readonly DataGridView selectedGrid = new();
    private readonly NumericUpDown quantity = new();
    private readonly TextBox nameBox = new();
    private readonly TextBox gamePathBox = new();
    private readonly TextBox outputPathBox = new();
    private readonly WorkshopPreview preview = new();
    private readonly Label status = new();
    private readonly WorkshopButton addButton = new();
    private readonly WorkshopButton removeButton = new();
    private readonly WorkshopButton generateButton = new();
    private readonly SplitContainer split = new();
    private readonly StudioTabs mainTabs = new();
    private readonly SplitContainer blueprintSplit = new();
    private readonly TextBox blueprintSearchBox = new();
    private readonly CheckBox includeWorkshopCheck = new();
    private readonly DataGridView blueprintGrid = new();
    private readonly DataGridView materialGrid = new();
    private readonly WorkshopPreview materialPreview = new();
    private readonly Label blueprintStatus = new();
    private readonly WorkshopButton refreshBlueprintsButton = new();
    private readonly WorkshopButton createMaterialsButton = new();
    private readonly BindingList<SelectedItem> selected = new();
    private List<GameItem> allItems = new();
    private List<BlueprintEntry> allBlueprints = new();
    private BlueprintEntry? activeBlueprint;

    public MainForm(bool loadOnShow = true)
    {
        Text = "Glass Box Blueprint Maker";
        MinimumSize = new Size(1040, 760);
        Size = new Size(1320, 860);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Dark;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        BuildUi();
        ApplyModernTheme(this);
        SetBusy(false);
        Shown += (_, _) => SizeMainPanels();
        Resize += (_, _) => SizeMainPanels();
        mainTabs.SelectedIndexChanged += (_, _) => SizeMainPanels();
        if (loadOnShow) Shown += async (_, _) => await LoadGameDataAsync();
    }

    private void BuildUi()
    {
        var header = workshopHeader;

        var paths = new TableLayoutPanel { Dock = DockStyle.Top, Height = 84, ColumnCount = 4, Padding = new Padding(12, 8, 12, 4), BackColor = PanelGrey };
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104)); paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88)); paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8));
        gamePathBox.Dock = DockStyle.Fill; outputPathBox.Dock = DockStyle.Fill;
        paths.Controls.Add(MakeLabel("Game folder"), 0, 0); paths.Controls.Add(gamePathBox, 1, 0); paths.Controls.Add(MakeButton("Browse", (_, _) => BrowseGame()), 2, 0);
        paths.Controls.Add(MakeLabel("Blueprints"), 0, 1); paths.Controls.Add(outputPathBox, 1, 1); paths.Controls.Add(MakeButton("Browse", (_, _) => BrowseOutput()), 2, 1);

        split.Dock = DockStyle.Fill;
        split.BackColor = Dark;
        split.Padding = new Padding(12, 10, 12, 8);
        split.Panel1.BackColor = Dark; split.Panel2.BackColor = PanelGrey;
        mainTabs.Dock = DockStyle.Fill;
        mainTabs.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
        var itemTab = new Panel { Text = "01   /   ITEM PACK BUILDER", BackColor = Dark, ForeColor = Color.White, Padding = new Padding(0) };
        var blueprintTab = new Panel { Text = "02   /   BLUEPRINT MATERIALS", BackColor = Dark, ForeColor = Color.White, Padding = new Padding(0) };
        itemTab.Controls.Add(split);
        mainTabs.TabPages.Add(itemTab);
        mainTabs.TabPages.Add(blueprintTab);
        mainTabs.Initialize();
        BuildBlueprintTab(blueprintTab);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Dark
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(paths, 0, 1);
        root.Controls.Add(mainTabs, 0, 2);
        Controls.Add(root);

        var leftTop = new TableLayoutPanel { Dock = DockStyle.Top, Height = 64, ColumnCount = 2 };
        leftTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70)); leftTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        searchBox.PlaceholderText = "Search item name, symbol, or UUID..."; searchBox.Dock = DockStyle.Fill;
        categoryBox.Dock = DockStyle.Fill; categoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoryBox.DrawMode = DrawMode.OwnerDrawFixed;
        categoryBox.DrawItem += (_, e) =>
        {
            using var background = new SolidBrush(Color.FromArgb(39, 49, 56));
            e.Graphics.FillRectangle(background, e.Bounds);
            var text = e.Index >= 0 ? categoryBox.Items[e.Index]?.ToString() : "All categories";
            TextRenderer.DrawText(e.Graphics, text, Font, e.Bounds, Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            e.DrawFocusRectangle();
        };
        searchBox.TextChanged += (_, _) => ApplyFilter(); categoryBox.SelectedIndexChanged += (_, _) => ApplyFilter();
        leftTop.Controls.Add(SearchField("FIND AN ITEM", searchBox), 0, 0); leftTop.Controls.Add(SearchField("CATEGORY", categoryBox), 1, 0);
        split.Panel1.Controls.Add(itemGrid); split.Panel1.Controls.Add(leftTop);
        ConfigureItemGrid();

        var leftBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 7, 0, 0) };
        quantity.Minimum = 1; quantity.Maximum = 100000000; quantity.Value = 10; quantity.Width = 120; quantity.Font = new Font(Font, FontStyle.Bold);
        addButton.Text = "ADD TO PACK"; StylePrimary(addButton); addButton.Width = 145; addButton.Click += (_, _) => AddSelectedItem();
        leftBottom.Controls.Add(MakeLabel("Quantity")); leftBottom.Controls.Add(quantity); leftBottom.Controls.Add(addButton);
        split.Panel1.Controls.Add(leftBottom);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, Padding = new Padding(12), BackColor = PanelGrey };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 144)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        right.Controls.Add(MakeLabel("PACK CONFIGURATION / Name"), 0, 0); nameBox.Text = "My Glass Box Item Pack"; nameBox.TextChanged += (_, _) => RefreshPreview(); nameBox.Dock = DockStyle.Fill; right.Controls.Add(nameBox, 0, 1);
        right.Controls.Add(selectedGrid, 0, 2); ConfigureSelectedGrid();
        removeButton.Text = "REMOVE SELECTED"; removeButton.Dock = DockStyle.Fill; StyleSecondary(removeButton); removeButton.Click += (_, _) => RemoveSelected(); right.Controls.Add(removeButton, 0, 3);
        preview.SizeMode = PictureBoxSizeMode.CenterImage; preview.Dock = DockStyle.Fill; preview.BackColor = Color.FromArgb(27, 28, 30); right.Controls.Add(preview, 0, 4);
        status.Text = "Loading game items..."; status.ForeColor = Color.Gainsboro; status.AutoEllipsis = true; status.Dock = DockStyle.Fill; right.Controls.Add(status, 0, 5);
        generateButton.Text = "CREATE NEW BLUEPRINT"; generateButton.Dock = DockStyle.Fill; StylePrimary(generateButton); generateButton.Click += (_, _) => Generate(); right.Controls.Add(generateButton, 0, 6);
        split.Panel2.Controls.Add(right);
    }

    private void BuildBlueprintTab(Panel page)
    {
        blueprintSplit.Dock = DockStyle.Fill;
        blueprintSplit.BackColor = Dark;
        blueprintSplit.Panel1.BackColor = Dark;
        blueprintSplit.Panel2.BackColor = PanelGrey;
        page.Controls.Add(blueprintSplit);

        var toolbar = new TableLayoutPanel { Dock = DockStyle.Top, Height = 64, ColumnCount = 3, BackColor = Dark };
        toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        blueprintSearchBox.PlaceholderText = "Search local and subscribed blueprints...";
        blueprintSearchBox.Dock = DockStyle.Fill;
        blueprintSearchBox.TextChanged += (_, _) => RefreshBlueprintGrid();
        includeWorkshopCheck.Text = "Steam Workshop";
        includeWorkshopCheck.Checked = true;
        includeWorkshopCheck.ForeColor = Color.White;
        includeWorkshopCheck.Dock = DockStyle.Fill;
        includeWorkshopCheck.CheckedChanged += async (_, _) => await LoadBlueprintsAsync();
        refreshBlueprintsButton.Text = "REFRESH";
        refreshBlueprintsButton.Dock = DockStyle.Fill;
        StyleSecondary(refreshBlueprintsButton);
        refreshBlueprintsButton.Click += async (_, _) => await LoadBlueprintsAsync();
        toolbar.Controls.Add(SearchField("FIND A BLUEPRINT", blueprintSearchBox), 0, 0);
        toolbar.Controls.Add(includeWorkshopCheck, 1, 0);
        toolbar.Controls.Add(refreshBlueprintsButton, 2, 0);
        ConfigureBlueprintGrid();
        blueprintSplit.Panel1.Controls.Add(blueprintGrid);
        blueprintSplit.Panel1.Controls.Add(toolbar);

        var details = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Padding = new Padding(12), BackColor = PanelGrey };
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 144));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        details.Controls.Add(new Label { Text = "MATERIAL MANIFEST", ForeColor = Orange, Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        materialPreview.Dock = DockStyle.Fill;
        materialPreview.SizeMode = PictureBoxSizeMode.CenterImage;
        materialPreview.BackColor = Color.FromArgb(27, 28, 30);
        details.Controls.Add(materialPreview, 0, 1);
        ConfigureMaterialGrid();
        details.Controls.Add(materialGrid, 0, 2);
        blueprintStatus.Text = "Blueprints will load after the item catalogue.";
        blueprintStatus.ForeColor = Color.Gainsboro;
        blueprintStatus.Dock = DockStyle.Fill;
        blueprintStatus.AutoEllipsis = true;
        details.Controls.Add(blueprintStatus, 0, 3);
        createMaterialsButton.Text = "CREATE ITEM PARTS BLUEPRINT";
        createMaterialsButton.Dock = DockStyle.Fill;
        createMaterialsButton.Enabled = false;
        StylePrimary(createMaterialsButton);
        createMaterialsButton.Click += (_, _) => CreateMaterialBoxes();
        details.Controls.Add(createMaterialsButton, 0, 4);
        blueprintSplit.Panel2.Controls.Add(details);
    }

    private void ConfigureBlueprintGrid()
    {
        blueprintGrid.Dock = DockStyle.Fill;
        blueprintGrid.BackgroundColor = Dark;
        blueprintGrid.BorderStyle = BorderStyle.None;
        blueprintGrid.RowHeadersVisible = false;
        blueprintGrid.AllowUserToAddRows = false;
        blueprintGrid.AllowUserToDeleteRows = false;
        blueprintGrid.ReadOnly = true;
        blueprintGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        blueprintGrid.MultiSelect = false;
        blueprintGrid.AutoGenerateColumns = false;
        blueprintGrid.RowTemplate.Height = 68;
        blueprintGrid.Columns.Add(new DataGridViewImageColumn { HeaderText = "", Width = 70, ImageLayout = DataGridViewImageCellLayout.Zoom });
        blueprintGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BLUEPRINT", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        blueprintGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SOURCE", Width = 120 });
        blueprintGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PARTS", Width = 70 });
        blueprintGrid.SelectionChanged += (_, _) => ShowBlueprintMaterials();
        StyleGrid(blueprintGrid);
    }

    private void ConfigureMaterialGrid()
    {
        materialGrid.Dock = DockStyle.Fill;
        materialGrid.BackgroundColor = Dark;
        materialGrid.BorderStyle = BorderStyle.None;
        materialGrid.RowHeadersVisible = false;
        materialGrid.AllowUserToAddRows = false;
        materialGrid.AllowUserToDeleteRows = false;
        materialGrid.ReadOnly = true;
        materialGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        materialGrid.AutoGenerateColumns = false;
        materialGrid.RowTemplate.Height = 52;
        materialGrid.Columns.Add(new DataGridViewImageColumn { HeaderText = "", Width = 54, ImageLayout = DataGridViewImageCellLayout.Zoom });
        materialGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        materialGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY", Width = 80 });
        StyleGrid(materialGrid);
    }

    private void SizeMainPanels()
    {
        if (blueprintSplit.ClientSize.Width > 700) {
            blueprintSplit.Panel2MinSize = 350;
            blueprintSplit.SplitterDistance = Math.Max(350, (int)(blueprintSplit.ClientSize.Width * 0.60));
        }
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
        itemGrid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && !busy) AddSelectedItem(); }; StyleGrid(itemGrid);
    }

    private void ConfigureSelectedGrid()
    {
        selectedGrid.Dock = DockStyle.Fill; selectedGrid.BackgroundColor = Dark; selectedGrid.BorderStyle = BorderStyle.None; selectedGrid.RowHeadersVisible = false; selectedGrid.AllowUserToAddRows = false; selectedGrid.AllowUserToDeleteRows = false; selectedGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; selectedGrid.AutoGenerateColumns = false;
        selectedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ItemTitle", HeaderText = "PACK CONTENTS", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
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
            var gameRoot = gamePathBox.Text.Trim();
            allItems = await Task.Run(() => GameDataLoader.LoadItems(gameRoot));
            workshopHeader.Craftbot = allItems.FirstOrDefault(x => x.Symbol == "obj_craftbot_craftbot1")?.Icon;
            workshopHeader.Invalidate();
            categoryBox.Items.Clear(); categoryBox.Items.Add("All categories");
            foreach (var category in allItems.Select(x => x.Category).Distinct().OrderBy(x => x)) categoryBox.Items.Add(category);
            categoryBox.SelectedIndex = 0;
            ApplyFilter(); status.Text = $"Ready — {allItems.Count:N0} survival items loaded.";
            await LoadBlueprintsAsync();
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
        if (busy) return;
        selectedGrid.EndEdit();
        NormalizeQuantities();
        if (itemGrid.CurrentRow?.DataBoundItem is not GameItem item) return;
        var existing = selected.FirstOrDefault(x => x.Item.Uuid == item.Uuid);
        if (existing is not null && (long)existing.Quantity + (int)quantity.Value > int.MaxValue)
        {
            ShowError("A stack can contain at most 2,147,483,647 items. Reduce its quantity before adding more.");
            return;
        }
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
        SetBusy(busy);
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
        preview.Image = selected.Count == 0 ? null : BlueprintWriter.RenderIcon(selected, nameBox.Text);
        old?.Dispose();
    }

    private void Generate()
    {
        selectedGrid.EndEdit();
        NormalizeQuantities();
        try
        {
            var folder = BlueprintWriter.Create(outputPathBox.Text.Trim(), nameBox.Text, selected);
            status.Text = "Created " + Path.GetFileName(folder);
            MessageBox.Show(this, $"Blueprint created successfully.\n\n{folder}\n\nRestart Scrap Mechanic if it is already open.", "Blueprint created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private async Task LoadBlueprintsAsync()
    {
        var loadVersion = ++libraryLoadVersion;
        if (allItems.Count == 0 || !Directory.Exists(outputPathBox.Text.Trim()))
        {
            allBlueprints.Clear();
            RefreshBlueprintGrid();
            blueprintStatus.Text = "Choose an existing Blueprints folder to load the library.";
            return;
        }
        refreshBlueprintsButton.Enabled = false;
        createMaterialsButton.Enabled = false;
        blueprintStatus.Text = "Scanning local and subscribed blueprints...";
        try
        {
            var itemLookup = allItems.ToDictionary(x => x.Uuid, StringComparer.OrdinalIgnoreCase);
            var blueprintRoot = outputPathBox.Text.Trim();
            var workshopRoot = GameDataLoader.IsGameRoot(gamePathBox.Text.Trim()) ? BlueprintLibrary.FindWorkshopRoot(gamePathBox.Text.Trim()) : null;
            var includeWorkshop = includeWorkshopCheck.Checked;
            var loaded = await Task.Run(() => BlueprintLibrary.Load(blueprintRoot, workshopRoot, includeWorkshop, itemLookup));
            if (loadVersion != libraryLoadVersion) return;
            allBlueprints = loaded;
            RefreshBlueprintGrid();
            var local = allBlueprints.Count(x => x.Source == "Local");
            var workshop = allBlueprints.Count(x => x.Source == "Steam Workshop");
            if (activeBlueprint is null) blueprintStatus.Text = $"Loaded {local:N0} local and {workshop:N0} subscribed blueprints. Select a blueprint to see its parts.";
        }
        catch (Exception ex)
        {
            blueprintStatus.Text = "Could not scan the blueprint library.";
            ShowError(ex.Message);
        }
        finally { if (loadVersion == libraryLoadVersion) refreshBlueprintsButton.Enabled = !busy; }
    }

    private void RefreshBlueprintGrid()
    {
        var query = blueprintSearchBox.Text.Trim();
        var view = allBlueprints.Where(x => query.Length == 0 || x.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || x.Source.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        blueprintGrid.Rows.Clear();
        foreach (var entry in view)
        {
            var row = blueprintGrid.Rows.Add(entry.Icon, entry.Name, entry.Source, entry.TotalUnits.ToString("N0"));
            blueprintGrid.Rows[row].Tag = entry;
        }
        if (blueprintGrid.Rows.Count > 0) blueprintGrid.CurrentCell = blueprintGrid.Rows[0].Cells[1];
        ShowBlueprintMaterials();
    }

    private void ShowBlueprintMaterials()
    {
        activeBlueprint = blueprintGrid.CurrentRow?.Tag as BlueprintEntry;
        materialGrid.Rows.Clear();
        var oldPreview = materialPreview.Image;
        materialPreview.Image = null;
        oldPreview?.Dispose();
        if (activeBlueprint is null)
        {
            createMaterialsButton.Enabled = false;
            blueprintStatus.Text = "No matching blueprints. Try another search or refresh your library.";
            return;
        }

        foreach (var material in activeBlueprint.Materials)
        {
            var displayName = material.Item is null ? material.DisplayName + " ⚠" : material.DisplayName;
            materialGrid.Rows.Add(material.Icon, displayName, material.Quantity.ToString("N0"));
        }
        materialPreview.Image = BlueprintWriter.RenderPartsIcon(activeBlueprint.Icon);
        createMaterialsButton.Enabled = activeBlueprint.Materials.Count > 0;
        var warning = activeBlueprint.MissingItems == 0 ? "All item IDs are installed." : $"{activeBlueprint.MissingItems} item IDs are modded or unavailable.";
        blueprintStatus.Text = $"{activeBlueprint.Materials.Count:N0} item types • {activeBlueprint.TotalUnits:N0} total parts. {warning}";
    }

    private void CreateMaterialBoxes()
    {
        if (activeBlueprint is null || activeBlueprint.Materials.Count == 0) return;
        if (activeBlueprint.Materials.Any(x => x.Quantity > int.MaxValue))
        {
            ShowError("This blueprint requires more than 2,147,483,647 units of one item, which cannot fit in a generated stack.");
            return;
        }
        if (activeBlueprint.MissingItems > 0)
        {
            var answer = MessageBox.Show(this, $"This blueprint contains {activeBlueprint.MissingItems} modded or unavailable item IDs. Their boxes will only work while the required mods are installed.\n\nCreate the item-parts blueprint anyway?", "Modded items detected", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }
        try
        {
            var generatedItems = new List<GameItem>();
            var selections = activeBlueprint.Materials.Select(material =>
            {
                var item = material.Item;
                if (item is null)
                {
                    item = new GameItem { Symbol = material.Uuid, Uuid = material.Uuid, Title = material.DisplayName, Category = "Modded", Icon = new Bitmap(material.Icon) };
                    generatedItems.Add(item);
                }
                return new SelectedItem { Item = item, Quantity = checked((int)material.Quantity) };
            }).ToList();
            var outputName = activeBlueprint.Name + " Item Parts";
            using var icon = BlueprintWriter.RenderPartsIcon(activeBlueprint.Icon);
            var description = $"Glass boxes containing all item parts counted from {activeBlueprint.Name} ({activeBlueprint.Source}).";
            var folder = BlueprintWriter.Create(outputPathBox.Text.Trim(), outputName, selections, icon, description);
            foreach (var item in generatedItems) item.Icon?.Dispose();
            blueprintStatus.Text = "Created " + Path.GetFileName(folder);
            MessageBox.Show(this, $"Created '{outputName}'.\n\n{folder}\n\nRestart Scrap Mechanic if it is already open.", "Item parts blueprint created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _ = LoadBlueprintsAsync();
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

    private async void BrowseOutput()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select your Scrap Mechanic Blueprints folder", UseDescriptionForTitle = true, InitialDirectory = outputPathBox.Text };
        if (dialog.ShowDialog(this) == DialogResult.OK) { outputPathBox.Text = dialog.SelectedPath; await LoadBlueprintsAsync(); }
    }

    private Label MakeLabel(string text) => new() { Text = text, ForeColor = Color.Gainsboro, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(3, 5, 3, 0) };
    private Control SearchField(string title, Control field)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 6) };
        field.Dock = DockStyle.Bottom;
        panel.Controls.Add(field);
        panel.Controls.Add(new Label { Text = title, ForeColor = WorkshopTheme.Muted, Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 8, FontStyle.Bold) });
        return panel;
    }
    private Button MakeButton(string text, EventHandler action) { var button = new WorkshopButton { Text = text, Dock = DockStyle.Fill }; StyleSecondary(button); button.Click += action; return button; }
    private void StylePrimary(Button button) { button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = Orange; button.ForeColor = Color.FromArgb(25, 25, 25); button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold); button.Cursor = Cursors.Hand; }
    private void StyleSecondary(Button button) { button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = Orange; button.FlatAppearance.BorderSize = 1; button.BackColor = PanelGrey; button.ForeColor = Color.White; button.Cursor = Cursors.Hand; }
    private void StyleGrid(DataGridView grid) { grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 33, 36); grid.ColumnHeadersDefaultCellStyle.ForeColor = Orange; grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold); grid.DefaultCellStyle.BackColor = PanelGrey; grid.DefaultCellStyle.ForeColor = Color.White; grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(185, 91, 20); grid.DefaultCellStyle.SelectionForeColor = Color.White; grid.GridColor = Color.FromArgb(75, 77, 81); }
    private void SetBusy(bool value, string? message = null)
    {
        busy = value;
        UseWaitCursor = value;
        addButton.Enabled = !value && allItems.Count > 0;
        generateButton.Enabled = !value && selected.Count > 0;
        removeButton.Enabled = !value && selected.Count > 0;
        refreshBlueprintsButton.Enabled = !value;
        includeWorkshopCheck.Enabled = !value;
        gamePathBox.Enabled = outputPathBox.Enabled = !value;
        if (message is not null) status.Text = message;
    }

    private void ApplyModernTheme(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is TextBox text)
            {
                text.BackColor = Color.FromArgb(39, 49, 56);
                text.ForeColor = Color.FromArgb(235, 240, 247);
                text.BorderStyle = BorderStyle.FixedSingle;
                text.Font = new Font("Segoe UI", 10.5f);
                text.AccessibleName = text.PlaceholderText.Length > 0 ? text.PlaceholderText : text == nameBox ? "Blueprint name" : text == gamePathBox ? "Game folder" : "Blueprints folder";
            }
            else if (control is ComboBox combo) { combo.FlatStyle = FlatStyle.Flat; combo.BackColor = Color.FromArgb(39, 49, 56); combo.ForeColor = Color.White; combo.AccessibleName = "Item category"; }
            else if (control is NumericUpDown number) { number.BackColor = Color.FromArgb(39, 49, 56); number.ForeColor = Color.White; number.BorderStyle = BorderStyle.FixedSingle; number.AccessibleName = "Item quantity"; }
            else if (control is Button button)
            {
                button.Text = button.Text switch { "ADD TO PACK" => "+  Add to pack", "REMOVE SELECTED" => "Remove selected", "CREATE NEW BLUEPRINT" => "Create blueprint  →", "CREATE ITEM PARTS BLUEPRINT" => "Create item parts blueprint  →", "REFRESH" => "Refresh", _ => button.Text };
                button.MinimumSize = new Size(0, 30);
                button.FlatAppearance.BorderColor = Color.FromArgb(69, 81, 98);
                button.FlatAppearance.MouseOverBackColor = button.BackColor == Orange ? Color.FromArgb(255, 154, 65) : Color.FromArgb(47, 58, 74);
            }
            else if (control is DataGridView grid)
            {
                grid.Font = new Font("Segoe UI", 9.5f);
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = 36;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Dark;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Dark;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(157, 170, 187);
                grid.DefaultCellStyle.BackColor = PanelGrey;
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(23, 32, 33);
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(69, 66, 44);
                grid.DefaultCellStyle.Padding = new Padding(5);
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                grid.GridColor = Color.FromArgb(37, 50, 50);
                grid.RowTemplate.Height = grid == selectedGrid ? 38 : grid.RowTemplate.Height;
                grid.CellPainting += (_, e) =>
                {
                    if (e.RowIndex < 0 || e.ColumnIndex != 0 || grid == selectedGrid || e.Graphics is null) return;
                    e.PaintBackground(e.ClipBounds, true);
                    var r = Rectangle.Inflate(e.CellBounds, -5, -5);
                    using var plate = WorkshopTheme.Plate(r, 6);
                    using var blue = new SolidBrush(Color.FromArgb(31, 77, 99));
                    e.Graphics.FillPath(blue, plate);
                    var state = e.Graphics.Save();
                    e.Graphics.SetClip(plate);
                    WorkshopTheme.Grid(e.Graphics, r, 12);
                    if (e.Value is Image icon) e.Graphics.DrawImage(icon, Rectangle.Inflate(r, -3, -3));
                    e.Graphics.Restore(state);
                    using var border = new Pen(grid.Rows[e.RowIndex].Selected ? WorkshopTheme.Accent : Color.FromArgb(88, 127, 145), grid.Rows[e.RowIndex].Selected ? 2 : 1);
                    e.Graphics.DrawPath(border, plate);
                    e.Handled = true;
                };
            }
            else if (control is SplitContainer divider) { divider.SplitterWidth = 10; }
            else if (control is PictureBox picture) { picture.BackColor = Dark; }
            ApplyModernTheme(control);
        }
    }

    internal void VerifyUi(List<GameItem> items, List<BlueprintEntry> library, string screenshotRoot)
    {
        allItems = items;
        workshopHeader.Craftbot = items.FirstOrDefault(x => x.Symbol == "obj_craftbot_craftbot1")?.Icon;
        allBlueprints = library;
        gamePathBox.Text = @"C:\Games\Scrap Mechanic";
        outputPathBox.Text = @"%APPDATA%\Axolot Games\Scrap Mechanic\User\YourProfile\Blueprints";
        categoryBox.Items.Add("All categories");
        foreach (var category in items.Select(x => x.Category).Distinct().OrderBy(x => x)) categoryBox.Items.Add(category);
        categoryBox.SelectedIndex = 0;
        SetBusy(false);
        Show();
        categoryBox.SelectedItem = "Blocks";
        if (itemGrid.Rows.Count == 0 || itemGrid.Rows.Cast<DataGridViewRow>().Any(r => r.DataBoundItem is GameItem i && i.Category != "Blocks")) throw new InvalidDataException("Category filter failed.");
        categoryBox.SelectedIndex = 0;
        searchBox.Text = "obj_consumable_component";
        if (itemGrid.Rows.Count != 1) throw new InvalidDataException("Item search failed.");
        AddSelectedItem();
        AddSelectedItem();
        if (selected.Count != 1 || selected[0].Quantity != 20) throw new InvalidDataException("Adding or merging items failed.");
        selectedGrid.Rows[0].Cells[1].Value = 42;
        NormalizeQuantities();
        if (selected[0].Quantity != 42) throw new InvalidDataException("Quantity editing failed.");
        RemoveSelected();
        if (selected.Count != 0 || generateButton.Enabled) throw new InvalidDataException("Removing items or empty-pack state failed.");
        AddSelectedItem();
        searchBox.Clear();
        RefreshBlueprintGrid();
        Directory.CreateDirectory(screenshotRoot);
        foreach (var windowSize in new[] { new Size(1320, 860), MinimumSize })
        {
            Size = windowSize;
            SizeMainPanels();
            for (var tab = 0; tab < 2; tab++)
            {
                mainTabs.SelectedIndex = tab;
                PerformLayout();
                Application.DoEvents();
                using var bitmap = new Bitmap(Width, Height);
                DrawToBitmap(bitmap, new Rectangle(Point.Empty, Size));
                bitmap.Save(Path.Combine(screenshotRoot, $"tab-{tab}-{windowSize.Width}.png"));
            }
        }
        blueprintSearchBox.Text = "__no_such_blueprint__";
        if (blueprintGrid.Rows.Count != 0 || createMaterialsButton.Enabled || materialGrid.Rows.Count != 0) throw new InvalidDataException("Empty library search retained stale materials.");
        blueprintSearchBox.Clear();
        if (activeBlueprint is null || materialGrid.Rows.Count == 0) throw new InvalidDataException("Blueprint selection did not populate materials.");
        Close();
    }
    private void ShowError(string message) => MessageBox.Show(this, message, "Glass Box Blueprint Maker", MessageBoxButtons.OK, MessageBoxIcon.Error);
}








