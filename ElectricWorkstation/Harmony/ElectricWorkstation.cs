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
/// Patch TileEntityWorkstation.UpdateTick so that electricChemistryStation blocks
/// stay "burning" without consuming any fuel.
///
/// Phase 1 — always on: IsBurning is forced true whenever the block at the
///            TileEntity's position is tagged as IsElectricWorkstation.
/// Phase 2 — power grid (TODO): query PowerManager to gate IsBurning on
///            whether the block is actually receiving grid power.
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
            BlockValue bv = world.GetBlock(0, pos);
            Block block = bv.Block;
            if (block == null) return;

            if (!block.Properties.Contains("IsElectricWorkstation")) return;
            if (!block.Properties.GetBool("IsElectricWorkstation")) return;

            // --- Phase 1: keep burning unconditionally ---
            // TODO Phase 2: replace with power grid check, e.g.:
            //   TileEntityPowered tePowered = world.GetTileEntity(0, pos) as TileEntityPowered;
            //   bool hasPower = tePowered?.IsPowered ?? false;
            //   if (!hasPower) { __instance.IsBurning = false; return; }

            if (!__instance.IsBurning)
            {
                __instance.IsBurning = true;
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
/// without consuming any fuel.
///
/// The forge tracks remaining fuel via fuelInForgeInTicks (int).
/// Phase 1 — always on: reset fuelInForgeInTicks to a large value each tick
///            so CanOperate() always returns true and no real fuel is consumed.
/// Phase 2 — power grid (TODO): gate the reset on whether the block has grid power.
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
            BlockValue bv = world.GetBlock(0, pos);
            Block block = bv.Block;
            if (block == null) return;

            if (!block.Properties.Contains("IsElectricWorkstation")) return;
            if (!block.Properties.GetBool("IsElectricWorkstation")) return;

            // --- Phase 1: keep fuel counter perpetually non-zero ---
            // fuelInForgeInTicks is consumed each tick by the forge logic.
            // By resetting it here before UpdateTick runs, no actual fuel item
            // is ever loaded from the fuel slot (fuelInStorageInTicks stays 0).
            // TODO Phase 2: gate this on power grid check.
            __instance.fuelInForgeInTicks = FuelRefillTicks;
        }
        catch (System.Exception e)
        {
            Log.Warning("[ElectricWorkstation] TileEntityForge patch error: " + e.Message);
        }
    }
}
