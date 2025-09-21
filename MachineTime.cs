using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using ScheduleOne.ObjectScripts;
using UnityEngine;
using HarmonyLib;
using Newtonsoft.Json;
using System.IO;
using MelonLoader.TinyJSON;

namespace FasterMixing
{
    public class MachineTime : MelonMod
    {

        public class Settings
        {
            public int MixTimePerItem { get; set; }
            public bool InstantMixing { get; set; }
            public int MixTimeForAnything { get; set; }
        }

        public static Settings Modsettings;

        public override void OnInitializeMelon()
        {
            LoadSettings();
            MelonLogger.Msg("Faster Mixing loaded!");
            var harmony = new HarmonyLib.Harmony("zlatan.FasterMixing");
            harmony.PatchAll();

        }
        private void LoadSettings()
        {
            try
            {
                string settingsPath = Path.Combine(Directory.modDirectory, "configFasterMixing.json");
                if (!File.Exists(settingsPath))
                {
                    var defaultSettings = new Settings { MixTimePerItem = 1, InstantMixing = false, MixTimeForAnything = 0 };
                    File.WriteAllText(settingsPath, JsonConvert.SerializeObject(defaultSettings, Formatting.Indented));
                    MelonLogger.Msg("No settings file found. Created one with default settings.");
                }

                string json = File.ReadAllText(settingsPath);
                Modsettings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error loading the settings from json: {e}");
            }
        }

        [HarmonyPatch(typeof(MixingStation), "GetMixTimeForCurrentOperation")]
        class Patching
        {

            static void Postfix(ref int __result, MixingStation __instance)
            {
                if (__instance.CurrentMixOperation == null)
                {
                    return;
                }
                if (Modsettings.MixTimePerItem <= 0)
                {
                    MelonLogger.Msg("Tried to put mixtime per item less or equal to 0, changing the value to instant mix");
                    Modsettings.MixTimePerItem = 1;
                    Modsettings.InstantMixing = true;
                }
                //If instant mixing is set to true, than that will override MixTimePer20Item and MixTimePerItem
                if (Modsettings.InstantMixing)
                {
                    __result = 1;
                }
                //If MixTimeForAnything is set to anything higher than 0, then that will override MixTimePerItem
                if (Modsettings.MixTimeForAnything > 0 && !Modsettings.InstantMixing)
                {
                    __result = Modsettings.MixTimeForAnything;
                }
                //If user went with none of these (default setting) or just changed MixTimePerItem
                else if (!Modsettings.InstantMixing && Modsettings.MixTimeForAnything <= 0)
                {
                    __result = MachineTime.Modsettings.MixTimePerItem * __instance.CurrentMixOperation.Quantity;
                }

            }
        }

    }
}