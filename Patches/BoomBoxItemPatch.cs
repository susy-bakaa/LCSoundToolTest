using HarmonyLib;
using UnityEngine;

namespace no00ob.Mod.LethalCompany.LCSoundToolTest.Patches
{
    [HarmonyPatch(typeof(BoomboxItem))]
    internal class BoomBoxItemPatch
    {
        [HarmonyPatch(nameof(BoomboxItem.Start))]
        [HarmonyPostfix]
        public static void Start_Patch(BoomboxItem __instance)
        {
            AudioClip[] originalMusic = __instance.musicAudios;

            __instance.musicAudios = new AudioClip[originalMusic.Length + 1];

            for (int i = 0; i < originalMusic.Length; i++)
            {
                __instance.musicAudios[i] = originalMusic[i];
            }
            __instance.musicAudios[__instance.musicAudios.Length - 1] = SoundToolTestBase.music;

            SoundToolTestBase.Instance.logger.LogDebug($"Patched {__instance} with 1 new music track!");
        }
    }
}
