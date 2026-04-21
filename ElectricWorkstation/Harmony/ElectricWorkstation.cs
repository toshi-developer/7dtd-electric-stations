using HarmonyLib;
using System.Reflection;

/// <summary>
/// ElectricWorkstation mod entry point.
/// Registers Harmony patches on load via IModApi.
/// </summary>
public class ElectricWorkstation : IModApi
{
    public void InitMod(Mod _mod)
    {
        Log.Out("[ElectricWorkstation] Initializing...");
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Log.Out("[ElectricWorkstation] Harmony patches applied.");
    }
}

/// <summary>
/// Shared utility: checks whether any of the 6 adjacent blocks carries
/// a powered TileEntity (TileEntityPoweredBlock) with IsPowered == true.
///
/// Usage (Phase 2):
///   Place a vanilla powered block — e.g. wireTripRelay, electricSwitch —
///   directly next to the electric workstation and wire it to a generator.
///   As long as that adjacent block is receiving power the workstation runs.
/// </summary>
internal static class ElectricWorkstationUtils
{
    private static readonly Vector3i[] AdjacentOffsets = new[]
    {
        new Vector3i( 1, 0, 0),
        new Vector3i(-1, 0, 0),
        new Vector3i( 0, 1, 0),
        new Vector3i( 0,-1, 0),
        new Vector3i( 0, 0, 1),
        new Vector3i( 0, 0,-1),
    };

    /// <summary>
    /// Returns true if at least one of the 6 neighbours has a powered tile
    /// entity that is currently receiving grid power.
    /// </summary>
    internal static bool HasAdjacentPower(World world, Vector3i pos)
    {
        foreach (var offset in AdjacentOffsets)
        {
            Vector3i adjPos = pos + offset;
            TileEntityPoweredBlock te = world.GetTileEntity(0, adjPos) as TileEntityPoweredBlock;
            if (te != null && te.IsPowered)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true when the block at <paramref name="pos"/> is marked as an
    /// electric workstation via the XML property IsElectricWorkstation=true.
    /// </summary>
    internal static bool IsElectricWorkstation(World world, Vector3i pos)
    {
        BlockValue bv = world.GetBlock(0, pos);
        Block block = bv.Block;
        if (block == null) return false;
        if (!block.Properties.Contains("IsElectricWorkstation")) return false;
        return block.Properties.GetBool("IsElectricWorkstation");
    }
}

/// <summary>
/// Patch TileEntityWorkstation.UpdateTick so that electricChemistryStation blocks
/// run only when adjacent to a powered block.
///
/// Phase 2 — power grid: IsBurning is set/cleared based on whether any of the
///            6 neighbours carries a TileEntityPoweredBlock with IsPowered == true.
///            Place a wireTripRelay (or any powered block) next to the station
///            and connect it to a generator to enable operation.
/// </summary>
[HarmonyPatch(typeof(TileEntityWorkstation), "UpdateTick")]
public static class Patch_TileEntityWorkstation_UpdateTick
{
    static void Prefix(TileEntityWorkstation __instance)
    {
        try
        {
            World world = GameManager.Instance.World;
            if (world == null) return;

            Vector3i pos = __instance.ToWorldPos();

            if (!ElectricWorkstationUtils.IsElectricWorkstation(world, pos)) return;

            // Phase 2: gate IsBurning on adjacent grid power
            bool hasPower = ElectricWorkstationUtils.HasAdjacentPower(world, pos);

            if (hasPower)
            {
                if (!__instance.IsBurning)
                    __instance.IsBurning = true;
            }
            else
            {
                if (__instance.IsBurning)
                    __instance.IsBurning = false;
            }
        }
        catch (System.Exception e)
        {
            Log.Warning("[ElectricWorkstation] TileEntityWorkstation patch error: " + e.Message);
        }
    }
}

/// <summary>
/// Patch TileEntityForge.UpdateTick so that electricForge blocks smelt
/// only when adjacent to a powered block.
///
/// Phase 2 — power grid: fuelInForgeInTicks is refilled only while a
///            neighbouring TileEntityPoweredBlock.IsPowered == true.
///            When power is lost the forge drains its (virtual) fuel and stops.
/// </summary>
[HarmonyPatch(typeof(TileEntityForge), "UpdateTick")]
public static class Patch_TileEntityForge_UpdateTick
{
    // A value well above what one tick can consume (cFuelBurnPerTick is a small float).
    // Large enough to keep the forge running, small enough to avoid overflow issues.
    private const int FuelRefillTicks = 2000;

    static void Prefix(TileEntityForge __instance)
    {
        try
        {
            World world = GameManager.Instance.World;
            if (world == null) return;

            Vector3i pos = __instance.ToWorldPos();

            if (!ElectricWorkstationUtils.IsElectricWorkstation(world, pos)) return;

            // Phase 2: refill fuel only while receiving adjacent grid power
            if (ElectricWorkstationUtils.HasAdjacentPower(world, pos))
            {
                __instance.fuelInForgeInTicks = FuelRefillTicks;
            }
            // If no power: do NOT refill — the forge's own tick will drain
            // fuelInForgeInTicks to 0 and smelting will halt naturally.
        }
        catch (System.Exception e)
        {
            Log.Warning("[ElectricWorkstation] TileEntityForge patch error: " + e.Message);
        }
    }
}
