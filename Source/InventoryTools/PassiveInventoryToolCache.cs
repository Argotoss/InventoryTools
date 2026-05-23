using System.Collections.Generic;
using RimWorld;
using Verse;

namespace InventoryTools
{
    internal static class PassiveInventoryToolCache
    {
        private const int CacheDurationTicks = 1000;
        private static readonly Dictionary<Pawn, CacheEntry> Cache = new Dictionary<Pawn, CacheEntry>();

        public static float GetOffsetForStat(Pawn pawn, StatDef stat)
        {
            StatOffsetEntry entry;
            if (pawn == null || stat == null || !GetCacheEntry(pawn).Offsets.TryGetValue(stat, out entry))
            {
                return 0f;
            }

            return entry.Total;
        }

        public static List<StatOffsetSource> GetSourcesForStat(Pawn pawn, StatDef stat)
        {
            StatOffsetEntry entry;
            if (pawn == null || stat == null || !GetCacheEntry(pawn).Offsets.TryGetValue(stat, out entry))
            {
                return null;
            }

            return entry.Sources;
        }

        private static CacheEntry GetCacheEntry(Pawn pawn)
        {
            int currentTick = CurrentTick();
            CacheEntry entry;
            if (!Cache.TryGetValue(pawn, out entry) || entry.ExpiresAtTick <= currentTick)
            {
                entry = BuildCacheEntry(pawn, currentTick);
                Cache[pawn] = entry;
            }

            return entry;
        }

        private static CacheEntry BuildCacheEntry(Pawn pawn, int currentTick)
        {
            Dictionary<StatDef, WinningPositiveOffset> positiveWinners = new Dictionary<StatDef, WinningPositiveOffset>();
            Dictionary<StatDef, float> primaryPositiveOffsets = new Dictionary<StatDef, float>();
            List<Thing> winningItems = new List<Thing>();
            ThingWithComps primary = pawn.equipment == null ? null : pawn.equipment.Primary;
            RecordPrimaryPositiveOffsets(primary, primaryPositiveOffsets);

            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    Thing thing = pawn.inventory.innerContainer[i];
                    if (!IsEligibleInventoryTool(thing) || object.ReferenceEquals(thing, primary))
                    {
                        continue;
                    }

                    List<StatModifier> offsets = thing.def.equippedStatOffsets;
                    if (offsets == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < offsets.Count; j++)
                    {
                        StatModifier modifier = offsets[j];
                        if (modifier.stat == null || modifier.value <= 0f)
                        {
                            continue;
                        }

                        WinningPositiveOffset currentWinner;
                        if (!positiveWinners.TryGetValue(modifier.stat, out currentWinner) || modifier.value > currentWinner.Value)
                        {
                            positiveWinners[modifier.stat] = new WinningPositiveOffset(thing, modifier.value);
                        }
                    }
                }
            }

            Dictionary<StatDef, StatOffsetEntry> offsetsByStat = new Dictionary<StatDef, StatOffsetEntry>();
            foreach (KeyValuePair<StatDef, WinningPositiveOffset> winner in positiveWinners)
            {
                float primaryOffset;
                primaryPositiveOffsets.TryGetValue(winner.Key, out primaryOffset);

                float valueToAdd = winner.Value.Value - primaryOffset;
                if (valueToAdd > 0f)
                {
                    AddSource(offsetsByStat, winner.Key, valueToAdd, winner.Value.Item);
                    AddWinningItem(winningItems, winner.Value.Item);
                }
            }

            for (int i = 0; i < winningItems.Count; i++)
            {
                Thing winningItem = winningItems[i];
                List<StatModifier> offsets = winningItem.def.equippedStatOffsets;
                if (offsets == null)
                {
                    continue;
                }

                for (int j = 0; j < offsets.Count; j++)
                {
                    StatModifier modifier = offsets[j];
                    if (modifier.stat != null && modifier.value < 0f)
                    {
                        AddSource(offsetsByStat, modifier.stat, modifier.value, winningItem);
                    }
                }
            }

            return new CacheEntry(currentTick + CacheDurationTicks, offsetsByStat);
        }

        private static void RecordPrimaryPositiveOffsets(ThingWithComps primary, Dictionary<StatDef, float> primaryPositiveOffsets)
        {
            if (primary == null || primary.def == null || primary.def.equippedStatOffsets == null)
            {
                return;
            }

            List<StatModifier> offsets = primary.def.equippedStatOffsets;
            for (int i = 0; i < offsets.Count; i++)
            {
                StatModifier modifier = offsets[i];
                if (modifier.stat == null || modifier.value <= 0f)
                {
                    continue;
                }

                float current;
                if (!primaryPositiveOffsets.TryGetValue(modifier.stat, out current) || modifier.value > current)
                {
                    primaryPositiveOffsets[modifier.stat] = modifier.value;
                }
            }
        }

        private static bool IsEligibleInventoryTool(Thing thing)
        {
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps == null || thing.def == null)
            {
                return false;
            }

            if (thing.def.IsApparel || thing.def.equipmentType != EquipmentType.Primary)
            {
                return false;
            }

            return thingWithComps.GetComp<CompEquippable>() != null;
        }

        private static void AddSource(Dictionary<StatDef, StatOffsetEntry> offsetsByStat, StatDef stat, float value, Thing item)
        {
            StatOffsetEntry entry;
            if (!offsetsByStat.TryGetValue(stat, out entry))
            {
                entry = new StatOffsetEntry();
                offsetsByStat.Add(stat, entry);
            }

            entry.Total += value;
            entry.Sources.Add(new StatOffsetSource(item.LabelCapNoCount, value));
        }

        private static void AddWinningItem(List<Thing> winningItems, Thing item)
        {
            for (int i = 0; i < winningItems.Count; i++)
            {
                if (object.ReferenceEquals(winningItems[i], item))
                {
                    return;
                }
            }

            winningItems.Add(item);
        }

        private static int CurrentTick()
        {
            try
            {
                return Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            }
            catch
            {
                return 0;
            }
        }

        private sealed class CacheEntry
        {
            public readonly int ExpiresAtTick;
            public readonly Dictionary<StatDef, StatOffsetEntry> Offsets;

            public CacheEntry(int expiresAtTick, Dictionary<StatDef, StatOffsetEntry> offsets)
            {
                ExpiresAtTick = expiresAtTick;
                Offsets = offsets;
            }
        }

        private sealed class StatOffsetEntry
        {
            public float Total;
            public readonly List<StatOffsetSource> Sources = new List<StatOffsetSource>();
        }

        private sealed class WinningPositiveOffset
        {
            public readonly Thing Item;
            public readonly float Value;

            public WinningPositiveOffset(Thing item, float value)
            {
                Item = item;
                Value = value;
            }
        }
    }

    internal sealed class StatOffsetSource
    {
        public readonly string ItemLabel;
        public readonly float Value;

        public StatOffsetSource(string itemLabel, float value)
        {
            ItemLabel = itemLabel;
            Value = value;
        }
    }
}
