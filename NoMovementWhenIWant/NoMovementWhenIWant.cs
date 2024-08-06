using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using SkyFrost.Base;

namespace NoMovementWhenIWant
{
    public class NoMovementWhenIWant : ResoniteMod
    {
        public override string Name => "NoMovementWhenIWant";
        public override string Author => "hazre";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/hazre/NoMovementWhenIWant/";

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("Enable", "Enable NoMovementWhenIWant.", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> NO_TANK_CONTROLS_COMPATIBILITY = new ModConfigurationKey<bool>("NoTankControlsCompatibility", "Enable NoTankControls mod compatibility.", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> CLOUDVARIABLEPATH = new ModConfigurationKey<string>("CloudVariablePath", "Cloud Variable Path to to disable movement. Must be a bool.", () => "U-hazre.NoMovementWhenIWant");
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> POLLING = new("polling", "How often it should check the cloud Variable? (In Seconds, 0 for instant)", () => 5);
        public static ModConfiguration config;
        private static bool? cloudValue = null;
        private static bool enabled = true;
        private static bool noTankControlsCompatibility = true;
        private static string cloudVariablePath = null;
        private static int pollingAmount = 5;
        private static bool lastNoTankControlsState = false;
        public override void OnEngineInit()
        {
            config = GetConfiguration();
            config.OnThisConfigurationChanged += (_) => OnConfigurationChanged();
            Harmony harmony = new Harmony("dev.hazre.NoMovementWhenIWant");
            harmony.PatchAll();

            Engine.Current.OnReady += () =>
            {
                OnConfigurationChanged();
                CloudVariablePolling();
            };
        }

        private void OnConfigurationChanged()
        {
            enabled = config.GetValue(ENABLED);
            noTankControlsCompatibility = config.GetValue(NO_TANK_CONTROLS_COMPATIBILITY);
            cloudVariablePath = config.GetValue(CLOUDVARIABLEPATH);
            pollingAmount = config.GetValue(POLLING);

            updateCloudProxy();
        }

        private static void CloudVariablePolling()
        {
            var worker = Userspace.Current;

            if (enabled && CloudVariableHelper.IsValidPath(cloudVariablePath))
            {
                worker.StartTask(async delegate ()
                {
                    var proxy = worker.Cloud.Variables.RequestProxy(worker.LocalUser.UserID, cloudVariablePath);

                    await proxy.Refresh();

                    if (proxy.State == CloudVariableState.Invalid || proxy.State == CloudVariableState.Unregistered
                     || proxy.Identity.path != cloudVariablePath)
                    {
                        Error("Failed to poll cloud variable or variable path changed while polling.");
                    }
                    else
                    {
                        var value = proxy.ReadValue<bool>();

                        cloudValue = value;
                    }
                });
            }

            worker.RunInSeconds(pollingAmount, CloudVariablePolling);
        }

        private void updateCloudProxy()
        {
            if (!CloudVariableHelper.IsValidPath(cloudVariablePath))
            {
                Error("Invalid Cloud Variable Path");
                config.Set(ENABLED, false);
            }
            else
            {
                Msg("Valid Cloud Variable Path");
            }
        }

        [HarmonyPatch(typeof(InteractionHandler))]
        [HarmonyPatch("BeforeInputUpdate")]
        class DisableMovementPatch
        {
            [HarmonyAfter(new string[] { "owo.Nytra.NoTankControls" })]
            private static void Postfix(InteractionHandler __instance, ref InteractionHandlerInputs ____inputs)
            {
                if (enabled)
                {
                    bool shouldBlock = false;

                    if (cloudValue.HasValue)
                    {
                        shouldBlock = (bool)cloudValue;
                    }

                    if (noTankControlsCompatibility)
                    {
                        // Check if NoTankControls has changed the state
                        if (____inputs.Axis.RegisterBlocks != lastNoTankControlsState)
                        {
                            // NoTankControls has likely modified the state, so we shouldn't override it
                            lastNoTankControlsState = ____inputs.Axis.RegisterBlocks;
                        }
                        else
                        {
                            // NoTankControls hasn't changed the state, so we can apply our logic
                            ____inputs.Axis.RegisterBlocks = shouldBlock;
                        }
                    }
                    else
                    {
                        // No compatibility mode, just apply our logic
                        ____inputs.Axis.RegisterBlocks = shouldBlock;
                    }

                    // Update the last known state
                    lastNoTankControlsState = ____inputs.Axis.RegisterBlocks;
                }
            }
        }
    }
}