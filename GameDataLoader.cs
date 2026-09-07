using Microsoft.Win32;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GlassBoxBlueprintMaker;

internal static class GameDataLoader
{
    private static readonly Regex ItemRegex = new(@"(?m)^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*sm\.uuid\.new\(\s*""(?<uuid>[0-9a-fA-F-]{36})""\s*\)", RegexOptions.Compiled);

    public static string? FindGameRoot()
    {
        var candidates = new List<string>();
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = root.OpenSubKey(@"SOFTWARE\Valve\Steam");
                var steam = key?.GetValue("InstallPath") as string;
                if (!string.IsNullOrWhiteSpace(steam)) candidates.Add(Path.Combine(steam, "steamapps", "common", "Scrap Mechanic"));
            }
            catch { }
        }
        candidates.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Scrap Mechanic");
        candidates.Add(@"C:\Program Files\Steam\steamapps\common\Scrap Mechanic");
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).FirstOrDefault(IsGameRoot);
    }

    public static bool IsGameRoot(string path) =>
        File.Exists(Path.Combine(path, "Survival", "Scripts", "game", "survival_items.lua")) &&
        File.Exists(Path.Combine(path, "Survival", "Gui", "IconMapSurvival.xml"));

    public static string? FindBlueprintRoot()
    {
        var userRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Axolot Games", "Scrap Mechanic", "User");
        if (!Directory.Exists(userRoot)) return null;
        return Directory.GetDirectories(userRoot, "User_*").OrderByDescending(Directory.GetLastWriteTimeUtc)
            .Select(p => Path.Combine(p, "Blueprints")).FirstOrDefault(Directory.Exists);
    }

    public static List<GameItem> LoadItems(string gameRoot, Action<int, int>? progress = null)
    {
        var luaPath = Path.Combine(gameRoot, "Survival", "Scripts", "game", "survival_items.lua");
        var descriptionsPath = Path.Combine(gameRoot, "Survival", "Gui", "Language", "English", "inventoryDescriptions.json");
        var descriptions = LoadTitles(descriptionsPath);
        var matches = ItemRegex.Matches(File.ReadAllText(luaPath));
        var unique = new Dictionary<string, GameItem>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in matches)
        {
            var symbol = match.Groups["name"].Value;
            var uuid = match.Groups["uuid"].Value.ToLowerInvariant();
            if (unique.ContainsKey(uuid)) continue;
            var title = descriptions.TryGetValue(uuid, out var named) ? named : FriendlyName(symbol);
            unique[uuid] = new GameItem { Symbol = symbol, Uuid = uuid, Title = title, Category = CategoryOf(symbol) };
        }

        var icons = LoadIconMaps(gameRoot);
        var values = unique.Values.OrderBy(i => i.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
        for (var i = 0; i < values.Count; i++)
        {
            values[i].Icon = icons.TryGetValue(values[i].Uuid, out var icon) ? icon : MakeFallbackIcon(values[i].Title);
            progress?.Invoke(i + 1, values.Count);
        }
        return values;
    }

    private static Dictionary<string, string> LoadTitles(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return result;
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        foreach (var entry in doc.RootElement.EnumerateObject())
            if (entry.Value.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString())) result[entry.Name] = title.GetString()!;
        return result;
    }

    private static Dictionary<string, Image> LoadIconMaps(string gameRoot)
    {
        var result = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        LoadIconMap(Path.Combine(gameRoot, "Data", "Gui", "IconMap.xml"), result);
        LoadIconMap(Path.Combine(gameRoot, "Survival", "Gui", "IconMapSurvival.xml"), result);
        return result;
    }

    private static void LoadIconMap(string xmlPath, Dictionary<string, Image> target)
    {
        if (!File.Exists(xmlPath)) return;
        var doc = XDocument.Load(xmlPath);
        foreach (var group in doc.Descendants("Group").Where(x => (string?)x.Attribute("name") == "ItemIcons"))
        {
            var texture = (string?)group.Attribute("texture");
            var size = ((string?)group.Attribute("size"))?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (texture is null || size?.Length != 2 || !int.TryParse(size[0], out var width) || !int.TryParse(size[1], out var height)) continue;
            var atlasPath = Path.Combine(Path.GetDirectoryName(xmlPath)!, texture);
            if (!File.Exists(atlasPath)) continue;
            using var atlas = new Bitmap(atlasPath);
            foreach (var index in group.Elements("Index"))
            {
                var uuid = ((string?)index.Attribute("name"))?.ToLowerInvariant();
                var point = ((string?)index.Element("Frame")?.Attribute("point"))?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (uuid is null || uuid == "empty" || point?.Length != 2 || !int.TryParse(point[0], out var x) || !int.TryParse(point[1], out var y)) continue;
                if (x < 0 || y < 0 || x + width > atlas.Width || y + height > atlas.Height) continue;
                var icon = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(icon);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(atlas, new Rectangle(0, 0, 64, 64), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
                if (target.Remove(uuid, out var old)) old.Dispose();
                target[uuid] = icon;
            }
        }
    }

    public static Image MakeFallbackIcon(string title)
    {
        var bmp = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(54, 57, 62));
        using var pen = new Pen(Color.FromArgb(242, 126, 32), 3);
        g.DrawRectangle(pen, 9, 9, 45, 45);
        using var font = new Font("Segoe UI", 17, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        var text = title.Length == 0 ? "?" : title[..1].ToUpperInvariant();
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, 32 - size.Width / 2, 31 - size.Height / 2);
        return bmp;
    }

    private static string FriendlyName(string symbol) => string.Join(' ', symbol.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
    private static string CategoryOf(string symbol)
    {
        var first = symbol.Split('_', 2)[0].ToLowerInvariant();
        return first switch { "blk" => "Blocks", "obj" => "Objects", "jnt" => "Joints", "tool" => "Tools", "char" => "Characters", _ => "Other" };
    }
}
