using System.Drawing;

namespace GlassBoxBlueprintMaker;

internal sealed class GameItem
{
    public required string Symbol { get; init; }
    public required string Uuid { get; init; }
    public required string Title { get; init; }
    public required string Category { get; init; }
    public Image? Icon { get; set; }
}

internal sealed class SelectedItem
{
    public required GameItem Item { get; init; }
    public int Quantity { get; set; }
}

internal sealed class MaterialRequirement
{
    public required string Uuid { get; init; }
    public required string DisplayName { get; init; }
    public required long Quantity { get; init; }
    public GameItem? Item { get; init; }
    public required Image Icon { get; init; }
    public string Availability => Item is null ? "Modded / unavailable" : "Installed survival item";
}

internal sealed class BlueprintEntry
{
    public required string Name { get; init; }
    public required string Source { get; init; }
    public required string FolderPath { get; init; }
    public required Image Icon { get; init; }
    public required List<MaterialRequirement> Materials { get; init; }
    public int PlacedParts { get; init; }
    public int MissingItems => Materials.Count(x => x.Item is null);
    public long TotalUnits => Materials.Sum(x => x.Quantity);
}
