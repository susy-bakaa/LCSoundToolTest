using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using LCSoundTool;
using HarmonyLib;
using no00ob.Mod.LethalCompany.LCSoundToolTestMod.Patches;
using BepInEx.Configuration;

namespace no00ob.Mod.LethalCompany.LCSoundToolTest
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class SoundToolTestBase : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "LCSoundToolTest";
        private const string PLUGIN_NAME = "LC Sound Tool Test";
        private const string PLUGIN_VERSION = "1.2.0";

        public static SoundToolTestBase Instance;

        internal ManualLogSource logger;

        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        // Keybinds for the networked audio clip test
        public KeyboardShortcut sendNetworkAudioTest;
        public KeyboardShortcut removeNetworkAudioTest;
        public KeyboardShortcut logNetworkAudioTest;
        public KeyboardShortcut syncNetworkAudioTest;
        public bool wasKeyDown;
        public bool wasKeyDown2;
        public bool wasKeyDown3;
        public bool wasKeyDown4;

        // All of the 3 different audio clips we are utilizing
        public static AudioClip music;
        public static AudioClip sound;
        public static AudioClip randomSound1;
        public static AudioClip randomSound2;
        public static AudioClip networkedSound;

        // The sound we are replacing with the networkedSound audio clip
        public static string networkedSoundName = "Scan";

        // Bool for events
        bool subbed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            logger = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_GUID);

            logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

            // Setup the keybinds, F3 -> Send networked audio clip & F4 -> Remove networked audio clip
            sendNetworkAudioTest = new KeyboardShortcut(KeyCode.F3, new KeyCode[0]);
            removeNetworkAudioTest = new KeyboardShortcut(KeyCode.F4, new KeyCode[0]);
            logNetworkAudioTest = new KeyboardShortcut(KeyCode.F9, new KeyCode[0]);
            syncNetworkAudioTest = new KeyboardShortcut(KeyCode.F10, new KeyCode[0]);

            harmony.PatchAll(typeof(BoomBoxItemPatch));
        }

        private void Start()
        {
            // Here we get all of the sounds from the mods folder
            music = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", "music_test.wav");
            sound = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", "test.wav");
            randomSound1 = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", "test_33.wav");
            randomSound2 = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", "test_67.wav");
            networkedSound = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", "network_test.wav");
            // For some reason Unity doesn't always get the name of the sound clip which can cause problems
            music.name = "music_test";
            sound.name = "test";
            randomSound1.name = "test_33";
            randomSound2.name = "test_67";
            networkedSound.name = networkedSoundName;

            // For the test.wav, we just use it to replace one of the main menu button sounds nothing special here. Check LCSoundTool's page for more info.
            SoundTool.ReplaceAudioClip("Button3", sound);

            // For the test%Number.wav files, we just use them to replace one of the main menu button sounds with two random clips other with a chance of 67% and other with 33%. Check LCSoundTool's page for more info.
            SoundTool.ReplaceAudioClip("Button2", randomSound1);
            SoundTool.ReplaceAudioClip("Button2", randomSound2);
        }

        private void Update()
        {
            // For networked audio clips we wanna first make sure the network handler has been created. This happens after joining a lobby so networked sounds can not be sent or received before that.
            if (!SoundTool.networkingInitialized)
                return;

            // We check if we are subscribed to the SoundTool client audio changed event and if not we subscribe to it.
            // This event fires everytime the SoundTool.networkedClips dictionary is updated.
            if (!subbed)
            {
                subbed = true;
                SoundTool.ClientNetworkedAudioChanged += UpdateReplacedAudioList;
            }

            // We check if we managed to succesfully load the networked sound earlier, this is because we can test if it works by deleting the file from everyone besides one person and then seeing if gets sent to everyone and used regarless if it exist locally.
            if (networkedSound == null)
                return;

            if (syncNetworkAudioTest.IsDown() && !wasKeyDown4)
            {
                wasKeyDown4 = true;
            }
            if (syncNetworkAudioTest.IsUp() && wasKeyDown4)
            {
                wasKeyDown4 = false;
                Instance.logger.LogDebug($"Syncing all currently networked clips...");
                SoundTool.SyncNetworkedAudioClips();
                return;
            }

            if (logNetworkAudioTest.IsDown() && !wasKeyDown3)
            {
                wasKeyDown3 = true;
            }
            if (logNetworkAudioTest.IsUp() && wasKeyDown3)
            {
                wasKeyDown3 = false;
                Instance.logger.LogDebug($"All current networked clips count is {SoundTool.networkedClips.Count}.");
                Instance.logger.LogDebug($"All current replaced clips count is {SoundTool.replacedClips.Count}.");
                Instance.logger.LogDebug($"Current networked clips includes {networkedSoundName} {SoundTool.networkedClips.ContainsKey(networkedSoundName)}.");
                Instance.logger.LogDebug($"Current replaced clips includes {networkedSoundName} {SoundTool.replacedClips.ContainsKey(networkedSoundName)}.");
                return;
            }

            // Check for input and send the networked clip to all clients if we receive the input.
            if (sendNetworkAudioTest.IsDown() && !wasKeyDown2)
            {
                wasKeyDown2 = true;
                wasKeyDown = false;
            }
            if (sendNetworkAudioTest.IsUp() && wasKeyDown2)
            {
                wasKeyDown2 = false;
                wasKeyDown = false;
                SoundTool.SendNetworkedAudioClip(networkedSound);
                Instance.logger.LogDebug($"Sending addition of networked clip {networkedSound} over the network!");
                return;
            }

            // Check for input and remove the networked clip from all clients if we receive the input.
            if (!wasKeyDown2 && !sendNetworkAudioTest.IsDown() && removeNetworkAudioTest.IsDown() && !wasKeyDown)
            {
                wasKeyDown = true;
                wasKeyDown2 = false;
            }
            if (removeNetworkAudioTest.IsUp() && wasKeyDown)
            {
                wasKeyDown = false;
                wasKeyDown2 = false;
                SoundTool.RemoveNetworkedAudioClip(networkedSound);
                Instance.logger.LogDebug($"Sending removal of networked clip {networkedSound} over the network!");
                return;
            }
        }

        private void OnDestroy()
        {
            // If we destroy the plugin we want to unsubscribe from all events.
            if (subbed)
            {
                subbed = false;
                SoundTool.ClientNetworkedAudioChanged -= UpdateReplacedAudioList;
            }
        }

        // This method here checks everytime the networkedClips dictionary is changed if
        public void UpdateReplacedAudioList()
        {
            logger.LogDebug("Updating replaced audio clips...");

            // it contains the networked clip we have with this mod
            if (SoundTool.networkedClips.ContainsKey(networkedSoundName))
            {
                logger.LogDebug($"networkedClips includes {networkedSoundName}");
                // if it does we check if that clip has already been added to the replacement audio in SoundTool
                if (!SoundTool.replacedClips.ContainsKey(networkedSoundName))
                {
                    logger.LogDebug($"replacedClips does not include {networkedSoundName}");
                    // lastly we add the specified clip if it has not been added
                    SoundTool.ReplaceAudioClip(networkedSoundName, SoundTool.networkedClips[networkedSoundName]);
                    logger.LogDebug($"Replaced {networkedSoundName} with {SoundTool.networkedClips[networkedSoundName]}");
                }
            }
            else // if it does not contain the networked clip
            {
                logger.LogDebug($"networkedClips does not include {networkedSoundName}");
                // we check if that clip has already been added to the replacement audio in SoundTool
                if (SoundTool.replacedClips.ContainsKey(networkedSoundName))
                {
                    logger.LogDebug($"replacedClips includes {networkedSoundName}");
                    // lastly we remove the specified clip if it has been added but is no longer present in the networkedClips
                    SoundTool.RestoreAudioClip(networkedSoundName);
                    logger.LogDebug($"Restoring {networkedSoundName} to vanilla sound");
                }
            }
        }
    }
}