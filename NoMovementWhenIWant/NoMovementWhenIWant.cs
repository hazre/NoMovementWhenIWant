using HarmonyLib;
using ResoniteModLoader;
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
        private static ModConfigurationKey<string> CLOUDVARIABLEPATH = new ModConfigurationKey<string>("CloudVariablePath", "Cloud Variable Path to to disable movement. Must be a bool.", () => "U-hazre.NoMovementWhenIWant");
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> POLLING = new("polling", "How often it should check the cloud Variable? (In Seconds, 0 for instant)", () => 5);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<Chirality> CHIRALITY = new("chirality", "Which hand should be affected?", () => Chirality.Both);
        public static ModConfiguration config;
        private static bool? cloudValue = null;
        private static bool enabled = true;
        private static string cloudVariablePath = null;
        private static int pollingAmount = 5;
        private static Chirality affectedHand = Chirality.Both;
        public enum Chirality
        {
            Left,
            Right,
            Both
        }
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
            cloudVariablePath = config.GetValue(CLOUDVARIABLEPATH);
            pollingAmount = config.GetValue(POLLING);
            affectedHand = config.GetValue(CHIRALITY);

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
                if (!enabled || !cloudValue.GetValueOrDefault(false))
                    return;

                bool applyBlock = affectedHand == Chirality.Both ||
                                  (affectedHand == Chirality.Left && __instance.Side == FrooxEngine.Chirality.Left) ||
                                  (affectedHand == Chirality.Right && __instance.Side == FrooxEngine.Chirality.Right);

                if (applyBlock)
                {
                    ____inputs.Axis.RegisterBlocks = true;
                }
            }
        }
    }
}