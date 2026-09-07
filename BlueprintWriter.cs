using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;

namespace GlassBoxBlueprintMaker;

internal static class BlueprintWriter
{
    public const string GlassCubeUuid = "3d127db0-da28-4483-86fd-3eeed20fb85d";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Create(string outputRoot, string name, IReadOnlyList<SelectedItem> selections)
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
                description = BuildDescription(selections),
                localId = id,
                name = string.IsNullOrWhiteSpace(name) ? "Glass Box Item Pack" : name.Trim(),
                type = "Blueprint",
                version = 0
            };
            File.WriteAllText(Path.Combine(stagingFolder, "blueprint.json"), JsonSerializer.Serialize(blueprint, JsonOptions));
            File.WriteAllText(Path.Combine(stagingFolder, "description.json"), JsonSerializer.Serialize(description, JsonOptions));
            using (var icon = RenderIcon(selections)) icon.Save(Path.Combine(stagingFolder, "icon.png"), ImageFormat.Png);
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

    public static Bitmap RenderIcon(IReadOnlyList<SelectedItem> selections)
    {
        var bmp = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using var background = new LinearGradientBrush(new Rectangle(0, 0, 128, 128), Color.FromArgb(255, 54, 57, 62), Color.FromArgb(255, 31, 33, 36), 45f);
        g.FillRectangle(background, 0, 0, 128, 128);
        using var border = new Pen(Color.FromArgb(242, 126, 32), 5);
        g.DrawRectangle(border, 3, 3, 121, 121);

        if (selections.Count > 0 && selections[0].Item.Icon is not null)
        {
            using var glow = new SolidBrush(Color.FromArgb(90, 242, 126, 32));
            g.FillEllipse(glow, 8, 9, 70, 70);
            g.DrawImage(selections[0].Item.Icon!, new Rectangle(12, 12, 62, 62));
        }
        using var cubePen = new Pen(Color.FromArgb(220, 235, 238, 240), 2);
        g.DrawPolygon(cubePen, new[] { new Point(13, 20), new Point(47, 7), new Point(79, 23), new Point(79, 62), new Point(47, 78), new Point(13, 61), new Point(13, 20) });
        g.DrawLine(cubePen, 47, 7, 47, 45); g.DrawLine(cubePen, 13, 20, 47, 45); g.DrawLine(cubePen, 79, 23, 47, 45); g.DrawLine(cubePen, 47, 45, 47, 78);

        var lines = selections.Take(3).Select(x => $"{x.Quantity}x {x.Item.Title}").ToList();
        if (selections.Count > 3) lines.Add($"+{selections.Count - 3} more");
        var y = 82f;
        foreach (var line in lines)
        {
            var fontSize = FitFont(g, line, 114, 13, 7);
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.White);
            g.DrawString(line, font, brush, new RectangleF(7, y, 114, 12), new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
            y += 11;
        }
        return bmp;
    }

    private static float FitFont(Graphics g, string text, float width, float start, float minimum)
    {
        for (var size = start; size >= minimum; size--)
            using (var font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel))
                if (g.MeasureString(text, font).Width <= width) return size;
        return minimum;
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
