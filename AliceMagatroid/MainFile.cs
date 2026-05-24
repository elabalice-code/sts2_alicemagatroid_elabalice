using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Timeline;
using AliceMagatroid_Mod.Utils;
using System.Linq;

namespace AliceMagatroid_Mod;

/**
 * Ideas
 * 
 * Self Bind
 * 
 * Bind effect - square texture based on model size, lines random generated (amount equal to bind amount)
 * shader of transparency of line based on average of point spread of the model
 * colored
 * 
 * Bind... rename? Necrobinder kinda overlaps.
 * */

[ModInitializer(nameof(Initialize))]
public class MainFile
{
    public const string ModId = "AliceMagatroid_Mod";
    public const string AssetRoot = "AliceMagatroid";

    public static Logger Logger { get; } =
        new(ModId, LogType.Generic);

    public static void Initialize()
    {
        AliceConfigHelper.LoadConfig();

        Harmony harmony = new(ModId);

        foreach (var type in AccessTools.AllTypes())
        {
            var hasPatch = type.CustomAttributes.Any(a =>
                a.AttributeType.Name.Contains("HarmonyPatch"));
            if (!hasPatch)
                continue;

            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (System.Exception ex)
            {
                Log.Error($"[AliceMagatroid_Mod] Patch failed for {type.FullName}: {ex.GetBaseException().Message}", 2);
            }
        }
    }
}
