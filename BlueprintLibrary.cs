using System.Text.Json;

namespace GlassBoxBlueprintMaker;

internal static class BlueprintLibrary
{
    private static readonly JsonDocumentOptions JsonOptions = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public static string? FindWorkshopRoot(string gameRoot)
    {
        var common = Directory.GetParent(gameRoot);
        var steamApps = common?.Parent;
        if (steamApps is null) return null;
        var path = Path.Combine(steamApps.FullName, "workshop", "content", "387990");
        return Directory.Exists(path) ? path : null;
    }

    public static List<BlueprintEntry> Load(string blueprintRoot, string? workshopRoot, bool includeWorkshop, IReadOnlyDictionary<string, GameItem> items)
    {
        var result = new List<BlueprintEntry>();
        if (Directory.Exists(blueprintRoot))
            foreach (var folder in Directory.EnumerateDirectories(blueprintRoot))
                TryAdd(folder, "Local", items, result);

        if (includeWorkshop && workshopRoot is not null && Directory.Exists(workshopRoot))
            foreach (var folder in Directory.EnumerateDirectories(workshopRoot))
                TryAdd(folder, "Steam Workshop", items, result);

        return result.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Source).ToList();
    }

    private static void TryAdd(string folder, string source, IReadOnlyDictionary<string, GameItem> items, List<BlueprintEntry> result)
    {
        var blueprintPath = Path.Combine(folder, "blueprint.json");
        if (!File.Exists(blueprintPath)) return;
        try
        {
            var name = ReadName(folder);
            using var doc = JsonDocument.Parse(File.ReadAllText(blueprintPath), JsonOptions);
            var counts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var placedParts = 0;

            if (doc.RootElement.TryGetProperty("bodies", out var bodies) && bodies.ValueKind == JsonValueKind.Array)
                foreach (var body in bodies.EnumerateArray())
                    if (body.TryGetProperty("childs", out var children) && children.ValueKind == JsonValueKind.Array)
                        foreach (var child in children.EnumerateArray())
                        {
                            if (!child.TryGetProperty("shapeId", out var shape)) continue;
                            var uuid = shape.GetString();
                            if (string.IsNullOrWhiteSpace(uuid)) continue;
                            var amount = BoundsVolume(child);
                            Add(counts, uuid, amount);
                            placedParts++;
                        }

            if (doc.RootElement.TryGetProperty("joints", out var joints) && joints.ValueKind == JsonValueKind.Array)
                foreach (var joint in joints.EnumerateArray())
                {
                    if (!joint.TryGetProperty("shapeId", out var shape)) continue;
                    var uuid = shape.GetString();
                    if (string.IsNullOrWhiteSpace(uuid)) continue;
                    Add(counts, uuid, 1);
                    placedParts++;
                }

            var materials = counts.Select(pair =>
            {
                items.TryGetValue(pair.Key, out var item);
                return new MaterialRequirement
                {
                    Uuid = pair.Key.ToLowerInvariant(),
                    DisplayName = item?.Title ?? $"Unknown / modded item ({pair.Key[..Math.Min(8, pair.Key.Length)]})",
                    Quantity = pair.Value,
                    Item = item,
                    Icon = item?.Icon ?? GameDataLoader.MakeFallbackIcon(pair.Key)
                };
            }).OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();

            result.Add(new BlueprintEntry
            {
                Name = name,
                Source = source,
                FolderPath = folder,
                Icon = LoadIcon(folder, name),
                Materials = materials,
                PlacedParts = placedParts
            });
        }
        catch
        {
            // A single malformed or incompatible Workshop item must not block the rest of the library.
        }
    }

    private static string ReadName(string folder)
    {
        var path = Path.Combine(folder, "description.json");
        if (File.Exists(path))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path), JsonOptions);
                if (doc.RootElement.TryGetProperty("name", out var name) && !string.IsNullOrWhiteSpace(name.GetString())) return name.GetString()!;
            }
            catch { }
        }
        return Path.GetFileName(folder);
    }

    private static Image LoadIcon(string folder, string name)
    {
        var path = Path.Combine(folder, "icon.png");
        if (File.Exists(path))
        {
            try { using var image = Image.FromFile(path); return new Bitmap(image); } catch { }
        }
        return GameDataLoader.MakeFallbackIcon(name);
    }

    private static long BoundsVolume(JsonElement child)
    {
        if (!child.TryGetProperty("bounds", out var bounds) || bounds.ValueKind != JsonValueKind.Object) return 1;
        try
        {
            var x = Math.Max(1, bounds.GetProperty("x").GetInt64());
            var y = Math.Max(1, bounds.GetProperty("y").GetInt64());
            var z = Math.Max(1, bounds.GetProperty("z").GetInt64());
            return checked(checked(x * y) * z);
        }
        catch { return 1; }
    }

    private static void Add(Dictionary<string, long> counts, string uuid, long amount)
    {
        counts[uuid] = checked(counts.GetValueOrDefault(uuid) + amount);
    }
}
