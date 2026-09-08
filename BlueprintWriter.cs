using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;

namespace GlassBoxBlueprintMaker;

internal static class BlueprintWriter
{
    public const string GlassCubeUuid = "3d127db0-da28-4483-86fd-3eeed20fb85d";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Create(string outputRoot, string name, IReadOnlyList<SelectedItem> selections, Image? iconOverride = null, string? descriptionOverride = null)
    {
        if (selections.Count == 0) throw new InvalidOperationException("Add at least one item first.");
        if (!Directory.Exists(outputRoot)) throw new DirectoryNotFoundException("The Blueprints folder does not exist.");
        if (selections.Any(x => x.Quantity < 1)) throw new InvalidOperationException("Every quantity must be at least 1.");

        var id = Guid.NewGuid().ToString();
        var finalFolder = Path.Combine(outputRoot, id);
        var stagingFolder = Path.Combine(outputRoot, ".glassbox-" + id);
        Directory.CreateDirectory(stagingFolder);
        try
        {
            var children = selections.Select((selection, index) => new
            {
                color = "DF7F01",
                controller = new { controllers = (object?)null, id = 10000 + index, joints = (object?)null, stackedAmount = selection.Quantity, stackedItem = selection.Item.Uuid },
                pos = new { x = (index % 8) * 2, y = (index / 8) * 2, z = 0 },
                shapeId = GlassCubeUuid,
                xaxis = 1,
                zaxis = -2
            }).ToArray();
            var blueprint = new { bodies = new[] { new { childs = children } }, version = 4 };
            var description = new
            {
                description = descriptionOverride ?? BuildDescription(selections),
                localId = id,
                name = string.IsNullOrWhiteSpace(name) ? "Glass Box Item Pack" : name.Trim(),
                type = "Blueprint",
                version = 0
            };
            File.WriteAllText(Path.Combine(stagingFolder, "blueprint.json"), JsonSerializer.Serialize(blueprint, JsonOptions));
            File.WriteAllText(Path.Combine(stagingFolder, "description.json"), JsonSerializer.Serialize(description, JsonOptions));
            using (var icon = iconOverride is null ? RenderIcon(selections, name) : ResizeIcon(iconOverride)) icon.Save(Path.Combine(stagingFolder, "icon.png"), ImageFormat.Png);
            ValidateStaging(stagingFolder, id, selections.Count);
            Directory.Move(stagingFolder, finalFolder);
            return finalFolder;
        }
        catch
        {
            if (Directory.Exists(stagingFolder)) Directory.Delete(stagingFolder, true);
            throw;
        }
    }

    public static Bitmap RenderPartsIcon(Image source)
    {
        using var canvas = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
        {
            ConfigureIconCanvas(g);
            DrawIconPlate(g);
            g.DrawImage(source, new Rectangle(12, 8, 104, 91));
            DrawIconLabel(g, "PARTS PACK", new RectangleF(7, 103, 114, 17), 10, WorkshopTheme.Accent);
            using var rim = new Pen(WorkshopTheme.Accent, 2);
            g.DrawLine(rim, 14, 100, 114, 100);
        }
        return ResizeIcon(canvas);
    }

    private static void ConfigureIconCanvas(Graphics g)
    {
        g.ScaleTransform(4, 4);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    }

    private static void DrawIconPlate(Graphics g)
    {
        using var plate = WorkshopTheme.Plate(new Rectangle(3, 3, 122, 122), 10);
        using var background = new LinearGradientBrush(new Rectangle(0, 0, 128, 128), Color.FromArgb(39, 94, 123), Color.FromArgb(16, 43, 60), 90f);
        g.FillPath(background, plate);
        var state = g.Save();
        g.SetClip(plate);
        WorkshopTheme.Grid(g, new Rectangle(4, 4, 120, 120), 12);
        using var footer = new SolidBrush(Color.FromArgb(235, 21, 31, 37));
        g.FillRectangle(footer, 4, 101, 120, 24);
        g.Restore(state);
        using var border = new Pen(WorkshopTheme.Accent, 2.5f);
        g.DrawPath(border, plate);
        using var bolt = new SolidBrush(Color.FromArgb(176, 206, 218));
        g.FillEllipse(bolt, 8, 8, 3, 3);
        g.FillEllipse(bolt, 117, 117, 3, 3);
    }

    private static void DrawIconLabel(Graphics g, string text, RectangleF area, float size, Color color)
    {
        using var font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(text, font, brush, area, format);
    }
    private static Bitmap ResizeIcon(Image source)
    {
        var bmp = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(source, new Rectangle(0, 0, 128, 128));
        return bmp;
    }

    public static Bitmap RenderIcon(IReadOnlyList<SelectedItem> selections, string? packName = null)
    {
        using var canvas = new Bitmap(512, 512, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
        {
            ConfigureIconCanvas(g);
            DrawIconPlate(g);
            var itemName = string.IsNullOrWhiteSpace(packName) ? "Glass Box Item Pack" : packName.Trim();
            DrawIconLabel(g, itemName, new RectangleF(14, 8, 102, 12), 9, Color.FromArgb(225, 241, 245));
            var cube = new[] { new Point(29, 37), new Point(64, 23), new Point(99, 37), new Point(99, 72), new Point(64, 87), new Point(29, 72) };
            using var glass = new SolidBrush(Color.FromArgb(45, 150, 214, 241));
            g.FillPolygon(glass, cube);
            if (selections.Count > 0 && selections[0].Item.Icon is Image itemIcon)
                g.DrawImage(itemIcon, new Rectangle(37, 32, 54, 48));
            using var edge = new Pen(Color.FromArgb(214, 229, 248, 255), 1.5f);
            g.DrawPolygon(edge, cube);
            g.DrawLine(edge, 29, 37, 64, 52);
            g.DrawLine(edge, 99, 37, 64, 52);
            g.DrawLine(edge, 64, 52, 64, 87);
            using var badge = new SolidBrush(WorkshopTheme.Accent);
            g.FillRectangle(badge, 27, 84, 74, 15);
            var quantity = selections.Count == 1 ? $"x{selections[0].Quantity:N0}" : $"{selections.Count} ITEM TYPES";
            DrawIconLabel(g, quantity, new RectangleF(28, 84, 72, 15), 9, Color.FromArgb(23, 31, 34));
            var title = selections.Count > 1 ? $"+{selections.Count - 1} other item types" : "";
            DrawIconLabel(g, title, new RectangleF(10, 103, 108, 17), 9, Color.FromArgb(237, 243, 235));
        }
        return ResizeIcon(canvas);
    }
    private static string BuildDescription(IReadOnlyList<SelectedItem> selections)
    {
        var text = string.Join(", ", selections.Select(x => $"{x.Quantity}x {x.Item.Title}"));
        return text.Length <= 900 ? "Glass box item pack: " + text : $"Glass box item pack containing {selections.Count} selected item stacks.";
    }

    private static void ValidateStaging(string folder, string id, int expectedChildren)
    {
        var files = Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(x => x).ToArray();
        if (!files.SequenceEqual(new[] { "blueprint.json", "description.json", "icon.png" })) throw new InvalidDataException("Blueprint output did not contain exactly the three required files.");
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "blueprint.json")));
        var bodies = blueprint.RootElement.GetProperty("bodies");
        if (bodies.GetArrayLength() != 1 || bodies[0].GetProperty("childs").GetArrayLength() != expectedChildren) throw new InvalidDataException("Blueprint part count validation failed.");
        var seenIds = new HashSet<int>();
        foreach (var child in bodies[0].GetProperty("childs").EnumerateArray())
        {
            if (child.GetProperty("shapeId").GetString() != GlassCubeUuid) throw new InvalidDataException("A generated part is not a glass cube.");
            if (child.GetProperty("controller").GetProperty("stackedAmount").GetInt32() < 1) throw new InvalidDataException("Invalid stored quantity.");
            if (!seenIds.Add(child.GetProperty("controller").GetProperty("id").GetInt32())) throw new InvalidDataException("Duplicate controller ID.");
        }
        using var description = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "description.json")));
        if (description.RootElement.GetProperty("localId").GetString() != id) throw new InvalidDataException("Blueprint folder identity validation failed.");
        using var icon = Image.FromFile(Path.Combine(folder, "icon.png"));
        if (icon.Width != 128 || icon.Height != 128) throw new InvalidDataException("Blueprint icon is not 128x128.");
    }
}



