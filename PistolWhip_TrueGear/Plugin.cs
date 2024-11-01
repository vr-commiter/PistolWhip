using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MyTrueGear;
using System.Numerics;
using System.Threading;

namespace PistolWhip_TrueGear
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        private static TrueGearMod _TrueGear = null;

        private static bool leftGunCanFire = false;
        private static bool rightGunCanFire = false;

        private static bool isDamage = false;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;

            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Plugin));
            _TrueGear = new TrueGearMod();
            _TrueGear.Play("HeartBeat");

            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static string CheckHand(string hand)
        {
            if (hand.Contains("Left") || hand.Contains("left"))
            {
                return "Left";
            }
            if (hand.Contains("Right") || hand.Contains("right"))
            {
                return "Right";
            }
            return hand;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(GunAmmoDisplay), "Update")]
        private static void GunAmmoDisplay_Update_Postfix(GunAmmoDisplay __instance)
        {

            try
            {
                var checkHand = CheckHand(__instance.gun.hand.name);
                if (checkHand == "Left")
                {
                    if (__instance.currentBulletCount > 0)
                    {
                        leftGunCanFire = true;
                    }
                    else
                    {
                        leftGunCanFire = false;
                    }
                }
                if (checkHand == "Right")
                {
                    if (__instance.currentBulletCount > 0)
                    {
                        rightGunCanFire = true;
                    }
                    else
                    {
                        rightGunCanFire = false;
                    }
                }
            }
            catch
            {

            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MeleeWeapon), "ProcessHit")]
        private static void MeleeWeapon_ProcessHit_Postfix(MeleeWeapon __instance)
        {
            var checkHand = CheckHand(__instance.hand.name);
            if (checkHand != "Left" && checkHand != "Right")
            {
                return;
            }
            //Log.LogInfo("---------------------------------");
            //Log.LogInfo($"{checkHand}Melee");
            _TrueGear.Play($"{checkHand}Melee");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Gun), "Fire")]
        private static void Gun_Fire_Postfix(Gun __instance)
        {
            var checkHand = CheckHand(__instance.hand.name);
            if (checkHand == "Left")
            {
                if (!leftGunCanFire)
                {
                    return;
                }
            }
            else if (checkHand == "Right")
            {
                if (!rightGunCanFire)
                {
                    return;
                }
            }
            else
            {
                return;
            }
            Log.LogInfo("---------------------------------");
            Log.LogInfo(__instance.BulletCount);
            Log.LogInfo(__instance.originalBulletCount);
            float percent = (float)__instance.BulletCount / (float)__instance.originalBulletCount;
            Log.LogInfo(percent);

            if (__instance.gunType == 3)
            {
                //Log.LogInfo("---------------------------------");
                //Log.LogInfo($"{checkHand}ShotgunShooting");
                _TrueGear.Play($"{checkHand}ShotgunShooting");
                return;
            }
            //Log.LogInfo("---------------------------------");
            //Log.LogInfo($"{checkHand}PistolShooting");
            _TrueGear.Play($"{checkHand}PistolShooting");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Gun), "Reload")]
        private static void Gun_Reload_Postfix(Gun __instance, bool triggeredByMelee)
        {
            try
            {
                if (triggeredByMelee)
                {
                    return;
                }
                if (!__instance.reloadTriggered)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            var checkHand = CheckHand(__instance.hand.name);
            if (__instance.reloadGestureTypeVar.Value == ESettings_ReloadType.DOWN)
            {
                //Log.LogInfo("---------------------------------");
                //Log.LogInfo($"{checkHand}DownReload");
                _TrueGear.Play($"{checkHand}DownReload");
            }
            if (__instance.reloadGestureTypeVar.Value == ESettings_ReloadType.UP)
            {
                //Log.LogInfo("---------------------------------");
                //Log.LogInfo($"{checkHand}UpReload");
                _TrueGear.Play($"{checkHand}UpReload");
            }
            if (__instance.reloadGestureTypeVar.Value == ESettings_ReloadType.BOTH)
            {
                if (__instance.player.head.position.y - __instance.hand.position.y >= 0.3f)
                {
                    //Log.LogInfo("---------------------------------");
                    //Log.LogInfo($"{checkHand}DownReload");
                    _TrueGear.Play($"{checkHand}DownReload");
                }
                else
                {
                    //Log.LogInfo("---------------------------------");
                    //Log.LogInfo($"{checkHand}UpReload");
                    _TrueGear.Play($"{checkHand}UpReload");
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "Hit")]
        private static void Player_Hit_Postfix(Player __instance, HitData hit, Vector3 force, IDamageSource source)
        {
            if (!isDamage)
            {
                isDamage = true;
                //Log.LogInfo("---------------------------------");
                //Log.LogInfo($"{hit.type}Damage");
                _TrueGear.Play($"{hit.type}Damage");
                Timer damageTimer = new Timer(DamageTimerCallBack, null, 10, Timeout.Infinite);
            }
        }
        private static void DamageTimerCallBack(System.Object o)
        {
            isDamage = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "ProcessKillerHit")]
        private static void Player_ProcessKillerHit_Postfix(Player __instance)
        {
            //Log.LogInfo("---------------------------------");
            //Log.LogInfo("WallDamage");
            _TrueGear.Play("WallDamage");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerHUD), "OnArmorLost")]
        private static void PlayerHUD_OnArmorLost_Postfix(PlayerHUD __instance)
        {
            try
            {
                if (!__instance.hasArmor)
                {
                    //Log.LogInfo("---------------------------------");
                    //Log.LogInfo("StartHeartBeat");
                    _TrueGear.StartHeartBeat();
                    return;
                }
                //Log.LogInfo("---------------------------------");
                //Log.LogInfo("StopHeartBeat");
                _TrueGear.StopHeartBeat();
            }
            catch
            {
                return;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerHUD), "playArmorGainedEffect")]
        private static void PlayerHUD_playArmorGainedEffect_Postfix(PlayerHUD __instance)
        {
            //Log.LogInfo("---------------------------------");
            //Log.LogInfo("StopHeartBeat");
            //Log.LogInfo("Healing");
            _TrueGear.StopHeartBeat();
            _TrueGear.Play("Healing");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerHUD), "OnPlayerDeath")]
        private static void PlayerHUD_OnPlayerDeath_Postfix(PlayerHUD __instance)
        {
            //Log.LogInfo("---------------------------------");
            //Log.LogInfo("StopHeartBeat");
            //Log.LogInfo("PlayerDeath");
            _TrueGear.StopHeartBeat();
            _TrueGear.Play("PlayerDeath");
        }
    }
}
