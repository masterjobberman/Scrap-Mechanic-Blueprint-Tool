namespace GlassBoxBlueprintMaker;

internal static class SelfTest
{
    public static int Run()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "GlassBoxBlueprintMaker-SelfTest-" + Guid.NewGuid());
        try
        {
            using var form = new MainForm(false);
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

            var itemLookup = items.ToDictionary(x => x.Uuid, StringComparer.OrdinalIgnoreCase);
            var parsedTest = BlueprintLibrary.Load(testRoot, null, false, itemLookup).Single();
            if (parsedTest.Materials.Count != 1 || parsedTest.Materials[0].Uuid != BlueprintWriter.GlassCubeUuid || parsedTest.Materials[0].Quantity != 1)
                throw new InvalidDataException("Blueprint material counting failed.");
            using var partsIcon = BlueprintWriter.RenderPartsIcon(parsedTest.Icon);
            if (partsIcon.Width != 128 || partsIcon.Height != 128) throw new InvalidDataException("The parts overlay icon is invalid.");

            var blueprintRoot = GameDataLoader.FindBlueprintRoot() ?? throw new InvalidOperationException("The local Blueprints folder was not detected.");
            var workshopRoot = BlueprintLibrary.FindWorkshopRoot(gameRoot);
            var library = BlueprintLibrary.Load(blueprintRoot, workshopRoot, true, itemLookup);
            if (!library.Any(x => x.Source == "Local")) throw new InvalidDataException("No local blueprints were found.");
            if (workshopRoot is not null && !library.Any(x => x.Source == "Steam Workshop")) throw new InvalidDataException("No subscribed Workshop blueprints were found.");
            form.VerifyUi(items, new List<BlueprintEntry> { parsedTest }, Path.Combine(AppContext.BaseDirectory, "ui-verification"));
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test-result.txt"), $"PASS: {items.Count} items; {library.Count} blueprints; creation, material counting, icons, search, add, merge, quantity edit, remove, empty states, both tabs at default and minimum sizes.");
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test-result.txt"), ex.ToString());
            return 1;
        }
        finally
        {
            try { if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true); } catch { }
        }
    }
}

