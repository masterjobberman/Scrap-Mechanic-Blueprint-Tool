# Glass Box Blueprint Maker

A standalone Windows app for creating Scrap Mechanic Survival cheat blueprints. It reads the
installed game's `survival_items.lua`, English item names, and the official inventory icon
atlases, then creates one connected glass cube per selected item stack.

## Use

1. Run `GlassBoxBlueprintMaker.exe`.
2. Confirm the automatically detected Scrap Mechanic and Blueprints folders.
3. Search or filter the item list, choose a quantity, and click **Add to pack**.
4. Set a blueprint name and click **Create new blueprint**.

Each output is a new UUID folder containing only `blueprint.json`, `description.json`, and a
generated 128x128 orange-and-grey `icon.png`. The app never overwrites an existing blueprint.

The receiving PC needs Scrap Mechanic installed so the app can read its current item catalogue
and icons. If Steam is installed in a non-standard library, select the game folder once in the
app. This build targets 64-bit Windows and carries its own .NET runtime.
