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
