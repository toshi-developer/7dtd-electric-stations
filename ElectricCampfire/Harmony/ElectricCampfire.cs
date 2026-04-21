using HarmonyLib;
using System.Reflection;

/// <summary>
/// ElectricCampfire mod entry point.
/// Registers Harmony patches on load via IModApi.
/// </summary>
public class ElectricCampfire : IModApi
{
    public void InitMod(Mod _mod)
    {
        Log.Out("[ElectricCampfire] Initializing...");
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Log.Out("[ElectricCampfire] Harmony patches applied.");
    }
}

/// <summary>
/// Patch TileEntityWorkstation.UpdateTick so that electricCampfire blocks
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

            // Get this TileEntity's world position
            Vector3i pos = __instance.ToWorldPos();

            // Look up the block at that position
            // clrIdx 0 = main world layer (valid for all player-placed blocks)
            BlockValue bv = world.GetBlock(0, pos);
            Block block = bv.Block;
            if (block == null) return;

            // Only process blocks flagged as electric workstations
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
            // Catch-all so a patch error never crashes the game tick
            Log.Warning("[ElectricCampfire] UpdateTick patch error: " + e.Message);
        }
    }
}
