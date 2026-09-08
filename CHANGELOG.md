# Changelog

## 1.3.3 — 2026-09-08

This release includes the workshop redesign and preview improvements developed after the previous GitHub release, 1.1.0.

### Design

- Scrap Mechanic-inspired blue drafting grids, yellow highlights, curved panels, gear emblem, and an installed Craftbot illustration.
- Custom navigation and buttons, themed category selector, persistent search labels, and clearer table selections.
- Improved panel sizing across both tabs. Default window: 1320 × 860; minimum window: 1040 × 760.
- Refreshed screenshots in the README.

### Preview icons

- Matching blue-and-yellow frames for item packs and material packs.
- Item-pack previews use the **Pack Configuration / Name** field and update while typing. Exported icons use the same name.
- Blank names fall back to `Glass Box Item Pack`; long names are shortened visually to fit the small icon.
- Glass-cube illustration, quantity badge, and additional-item count for mixed packs.
- Icons are rendered at higher resolution and reduced to the game's 128 × 128 format.
- Existing blueprint icons are not changed automatically.

### Fixes

- Prevented overflowing a stack when adding to an existing quantity.
- Committed pending quantity edits before adding items or exporting.
- Disabled empty-pack actions and guarded adding items while game data loads.
- Cleared stale material details when library searches return no results or the folder is unavailable.
- Prevented older asynchronous library scans from replacing newer results.
- Read the game-folder text before starting background catalogue loading.

### Verification

- Release build and packaged executable self-test passed on a Windows PC with Scrap Mechanic installed.
- Loaded 1,405 catalogue items and 189 local/Workshop blueprints in that environment.
- Checked category and text filtering, add/merge/remove, quantity edits, empty states, material counting, icon rendering, and temporary blueprint creation.
- Inspected both tabs at default and minimum window sizes.
- Blueprint spawning inside Scrap Mechanic was not tested.

## 1.1.0 — Previous GitHub release

- Item pack builder and blueprint material packs, with local and subscribed Workshop blueprint support.
