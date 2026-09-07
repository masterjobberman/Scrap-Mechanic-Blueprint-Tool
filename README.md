# Glass Box Blueprint Maker

A standalone Windows app for creating Scrap Mechanic Survival cheat blueprints. It reads the
installed game's `survival_items.lua`, English item names, and the official inventory icon
atlases, then creates one connected glass cube per selected item stack. It can also inspect
existing blueprints and build a boxed item pack containing all of their construction parts.

## Use

1. Run `GlassBoxBlueprintMaker.exe`.
2. Confirm the automatically detected Scrap Mechanic and Blueprints folders.
3. In **Item Pack Builder**, search or filter the item list, choose a quantity, and click
   **Add to pack**.
4. Set a blueprint name and click **Create new blueprint**.
5. Or open **Blueprint Materials**, select a local or subscribed Workshop blueprint, review
   its material list, and click **Create item parts blueprint**.

Each output is a new UUID folder containing only `blueprint.json`, `description.json`, and a
generated 128x128 orange-and-grey `icon.png`. The app never overwrites an existing blueprint.

Material packs keep the selected blueprint's name with ` Item Parts` added to the end. They
also keep its original icon and add a semi-transparent orange-and-grey parts badge. Scalable
blocks are counted from their full bounds; individual parts and joints are counted separately.

Subscribed Steam Workshop creations are supported when the Workshop item contains a normal
`blueprint.json` at its top level. Maps, challenges, and other uploads without that file are
ignored. Unknown modded UUIDs remain listed and can be boxed, but the receiving game must have
the required mods installed.

The receiving PC needs Scrap Mechanic installed so the app can read its current item catalogue
and icons. If Steam is installed in a non-standard library, select the game folder once in the
app. This build targets 64-bit Windows and carries its own .NET runtime.
