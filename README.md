# Inventory Tools

RimWorld 1.6 Harmony mod that lets pawns receive passive `equippedStatOffsets` from carried hand-equippable inventory tools and weapons.

Source: https://github.com/Argotoss/InventoryTools

## Behavior

- Only non-apparel primary-equippable inventory items are considered.
- The currently equipped primary weapon/tool is ignored because RimWorld already applies it.
- If an inventory tool affects a stat already boosted by the equipped primary item, only the improvement over the equipped item is added.
- For each stat, only the highest positive inventory tool offset applies.
- Duplicate positive offsets do not stack.
- If an item wins at least one positive stat, all of that item's negative equipped stat offsets also apply.
- Negative offsets never apply from items that did not win a positive stat.
- Results are cached per pawn for 1000 game ticks.

## Build

Run:

```powershell
.\Source\Build.ps1
```

The compiled assembly is written to `1.6/Assemblies/InventoryTools.dll`.

## License

GPL-3.0-only. See `LICENSE`.
