using HarmonyLib;
using Verse;

namespace InventoryTools
{
    [StaticConstructorOnStartup]
    public static class InventoryToolsMod
    {
        static InventoryToolsMod()
        {
            Harmony harmony = new Harmony("argotoss.inventorytools");
            harmony.PatchAll();
        }
    }
}
