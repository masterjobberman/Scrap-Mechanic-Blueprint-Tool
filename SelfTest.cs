namespace GlassBoxBlueprintMaker;

internal static class SelfTest
{
    public static int Run()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "GlassBoxBlueprintMaker-SelfTest-" + Guid.NewGuid());
        try
        {
            using var form = new MainForm();
            if (form.Text != "Glass Box Blueprint Maker") throw new InvalidDataException("The main window could not be constructed.");
            var gameRoot = GameDataLoader.FindGameRoot() ?? throw new InvalidOperationException("Scrap Mechanic was not detected.");
            var items = GameDataLoader.LoadItems(gameRoot);
            if (items.Count < 1000) throw new InvalidDataException($"Only {items.Count} survival items were loaded.");
            var componentKit = items.Single(x => x.Symbol == "obj_consumable_component");
            if (componentKit.Icon is null) throw new InvalidDataException("The Component Kit icon was not loaded.");
            Directory.CreateDirectory(testRoot);
            var selection = new[] { new SelectedItem { Item = componentKit, Quantity = 10 } };
            var folder = BlueprintWriter.Create(testRoot, "Self Test Component Kits", selection);
            var required = new[] { "blueprint.json", "description.json", "icon.png" };
            if (!required.All(file => File.Exists(Path.Combine(folder, file)))) throw new InvalidDataException("Generated blueprint is incomplete.");
            return 0;
        }
        catch
        {
            return 1;
        }
        finally
        {
            try { if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true); } catch { }
        }
    }
}
