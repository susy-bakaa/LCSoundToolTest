using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using LCSoundTool;
using no00ob.Mod.LethalCompany.LCSoundToolTest.Patches;
using LCSoundTool.Resources;

namespace no00ob.Mod.LethalCompany.LCSoundToolTest
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, LCSoundToolTestMod.PluginInfo.PLUGIN_VERSION)]
    public class SoundToolTestBase : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "LCSoundToolTest";
        private const string PLUGIN_NAME = "LC Sound Tool Test";
        //private const string PLUGIN_VERSION = "1.2.3";

        private ConfigEntry<string> configFileType;

        public static SoundToolTestBase Instance;

        internal ManualLogSource logger;

        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);

        // Keybinds for the networked audio clip test
        public KeyboardShortcut sendNetworkAudioTest;
        public KeyboardShortcut removeNetworkAudioTest;
        public KeyboardShortcut logNetworkAudioTest;
        public KeyboardShortcut syncNetworkAudioTest;
        public KeyboardShortcut toggleSourceAudioTest1;
        public KeyboardShortcut toggleSourceAudioTest2;
        public KeyboardShortcut playSourceAudioTest1;
        public KeyboardShortcut playSourceAudioTest2;
        public KeyboardShortcut playSourceAudioTest3;
        public bool wasKeyDown;
        public bool wasKeyDown2;
        public bool wasKeyDown3;
        public bool wasKeyDown4;
        public bool wasKeyDown5;
        public bool wasKeyDown6;
        public bool wasKeyDown7;
        public bool wasKeyDown8;
        public bool wasKeyDown9;

        // Bundle
        internal static AssetBundle bundle;

        // All of the 7 different audio clips we are utilizing
        public static AudioClip music;
        public static AudioClip sound;
        public static AudioClip randomSound1;
        public static AudioClip randomSound2;
        public static AudioClip networkedSound;
        public static AudioClip sourceTestSound1;
        public static AudioClip sourceTestSound2;
        public static AudioClip sourceTestSound3;

        // The sound we are replacing with the networkedSound audio clip
        public static string networkedSoundName = "Scan";

        // Flags, one for events and another few for custom source dependant audio testing states
        bool subbed;
        bool swapped1;
        bool swapped2;

        // Custom source dependant audio testing gameobject references
        private static GameObject testingObjectInstance;
        private static AudioSource source1;
        private static AudioSource source2;
        private static AudioSource source3;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            configFileType = Config.Bind("General", "AudioFileType", "wav", "Which audio file type do you want the test mod to use? Valid values are 'wav', 'ogg' and 'mp3'");

            logger = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_GUID);

            logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

            // AssetBundle for custom source dependant audio testing gameobject
            bundle = AssetBundle.LoadFromMemory(LCSoundToolTestMod.Properties.Resources.soundtooltest);

            // Setup the keybinds, F3 -> Send networked audio clip & F4 -> Remove networked audio clip
            // F9 -> Debug log about networked audio clips, F10 -> Sync hosts networked clips to all clients
            // F1, F1 + Ctrl, F1 + Alt -> Play test sounds for audio source dependant sound feature
            // F2 -> Toggle the previous test's sounds replacement sounds on and off
            sendNetworkAudioTest = new KeyboardShortcut(KeyCode.F3, new KeyCode[0]);
            removeNetworkAudioTest = new KeyboardShortcut(KeyCode.F4, new KeyCode[0]);
            logNetworkAudioTest = new KeyboardShortcut(KeyCode.F9, new KeyCode[0]);
            syncNetworkAudioTest = new KeyboardShortcut(KeyCode.F10, new KeyCode[0]);
            playSourceAudioTest1 = new KeyboardShortcut(KeyCode.F1, new KeyCode[0]);
            playSourceAudioTest2 = new KeyboardShortcut(KeyCode.F1, new KeyCode[1] { KeyCode.LeftControl });
            playSourceAudioTest3 = new KeyboardShortcut(KeyCode.F1, new KeyCode[1] { KeyCode.LeftAlt });
            toggleSourceAudioTest1 = new KeyboardShortcut(KeyCode.F1, new KeyCode[1] { KeyCode.LeftShift });
            toggleSourceAudioTest2 = new KeyboardShortcut(KeyCode.F1, new KeyCode[2] { KeyCode.LeftShift, KeyCode.LeftControl });

            harmony.PatchAll(typeof(BoomBoxItemPatch));
        }

        private void Start()
        {
            // Here we get all of the sounds from the mods folder. They're located inside subfolder defined by the file type config value. Which can be 'wav', 'ogg' or 'mp3'
            music = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"music_test.{configFileType.Value}");
            sound = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"test.{configFileType.Value}");
            randomSound1 = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"test-33.{configFileType.Value}");
            randomSound2 = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"test-67.{configFileType.Value}");
            networkedSound = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"network_test.{configFileType.Value}");
            sourceTestSound1 = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"stt_test-_TestSource1,TestSource2-33.{configFileType.Value}");
            sourceTestSound2 = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"stt_test-_TestSource1,TestSource2-67.{configFileType.Value}");
            sourceTestSound3 = SoundTool.GetAudioClip("no00ob-LCSoundToolTest", configFileType.Value, $"stt_test-_TestSource3.{configFileType.Value}");
            // For some reason Unity doesn't always get the name of the sound clip which can cause problems.
            // This should be fixed in LCSoundTool v.1.3.0 onwards, but it's here for preservations sake and never hurts to define them manually just in case.
            music.name = "music_test";
            sound.name = "test";
            randomSound1.name = "test-33";
            randomSound2.name = "test-67";
            networkedSound.name = networkedSoundName;
            sourceTestSound1.name = "stt_test-_TestSource1,TestSource2-33";
            sourceTestSound2.name = "stt_test-_TestSource1,TestSource2-67";
            sourceTestSound3.name = "stt_test-_TestSource3";

            // For the test.wav, we just use it to replace one of the main menu button sounds nothing special here. Check LCSoundTool's page for more info.
            SoundTool.ReplaceAudioClip("Button3", sound);

            // For the test-chance.wav files, we just use them to replace one of the main menu button sounds with two random clips other with a chance of 67% and other with 33%. Check LCSoundTool's page for more info.
            SoundTool.ReplaceAudioClip("Button2", randomSound1);
            SoundTool.ReplaceAudioClip("Button2", randomSound2);

            // Absolutely before doing anything with networking we need to check if the user has LCSoundTool networking toggled on. If not we can not do any networking. Here you can inform them with a message that your mod wont work without it on!
            if (!SoundTool.networkingAvailable)
            {
                logger.LogWarning("LCSoundTool networking not enabled! This mod will not work fully and might run into problems.");
            }
        }

        private void Update()
        {
            // Same check here.
            if (!SoundTool.networkingAvailable)
                return;

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

            if (toggleSourceAudioTest1.IsDown() && !wasKeyDown5)
            {
                wasKeyDown5 = true;
            }
            if (toggleSourceAudioTest1.IsUp() && wasKeyDown5)
            {
                wasKeyDown5 = false;
                swapped1 = !swapped1;
                Instance.logger.LogDebug($"Toggled source audio test 1 & 2! {swapped1}");
                if (swapped1)
                {
                    SoundTool.ReplaceAudioClip("stt_test", sourceTestSound1);
                    SoundTool.ReplaceAudioClip("stt_test", sourceTestSound2);
                }
                else
                {
                    SoundTool.RestoreAudioClip("stt_test");
                }
                return;
            }

            if (toggleSourceAudioTest2.IsDown() && !wasKeyDown9)
            {
                wasKeyDown9 = true;
            }
            if (toggleSourceAudioTest2.IsUp() && wasKeyDown9)
            {
                wasKeyDown9 = false;
                swapped2 = !swapped2;
                Instance.logger.LogDebug($"Toggled source audio test 3! {swapped2}");
                if (swapped2)
                {
                    SoundTool.ReplaceAudioClip("stt_test", sourceTestSound3);
                }
                else
                {
                    SoundTool.RestoreAudioClip("stt_test", sourceTestSound3);
                }
                return;
            }

            if (playSourceAudioTest1.IsDown() && !wasKeyDown6)
            {
                wasKeyDown6 = true;
            }
            if (playSourceAudioTest1.IsUp() && wasKeyDown6)
            {
                wasKeyDown6 = false;
                
                if (testingObjectInstance == null)
                {
                    testingObjectInstance = GameObject.Instantiate((GameObject)bundle.LoadAsset("SoundToolTest.prefab"), GameNetworkManager.Instance.localPlayerController.gameObject.transform.position, GameNetworkManager.Instance.localPlayerController.gameObject.transform.rotation);
                    source1 = testingObjectInstance.transform.GetChild(0).GetComponent<AudioSource>();
                    source2 = testingObjectInstance.transform.GetChild(1).GetComponent<AudioSource>();
                    source3 = testingObjectInstance.transform.GetChild(2).GetComponent<AudioSource>();
                }

                source1.Play();
                return;
            }

            if (playSourceAudioTest2.IsDown() && !wasKeyDown7)
            {
                wasKeyDown7 = true;
            }
            if (playSourceAudioTest2.IsUp() && wasKeyDown7)
            {
                wasKeyDown7 = false;

                if (testingObjectInstance == null)
                {
                    testingObjectInstance = GameObject.Instantiate((GameObject)bundle.LoadAsset("SoundToolTest.prefab"), GameNetworkManager.Instance.localPlayerController.gameObject.transform.position, GameNetworkManager.Instance.localPlayerController.gameObject.transform.rotation);
                    source1 = testingObjectInstance.transform.GetChild(0).GetComponent<AudioSource>();
                    source2 = testingObjectInstance.transform.GetChild(1).GetComponent<AudioSource>();
                    source3 = testingObjectInstance.transform.GetChild(2).GetComponent<AudioSource>();
                }

                source2.Play();
                return;
            }

            if (playSourceAudioTest3.IsDown() && !wasKeyDown8)
            {
                wasKeyDown8 = true;
            }
            if (playSourceAudioTest3.IsUp() && wasKeyDown8)
            {
                wasKeyDown8 = false;

                if (testingObjectInstance == null)
                {
                    testingObjectInstance = GameObject.Instantiate((GameObject)bundle.LoadAsset("SoundToolTest.prefab"), GameNetworkManager.Instance.localPlayerController.gameObject.transform.position, GameNetworkManager.Instance.localPlayerController.gameObject.transform.rotation);
                    source1 = testingObjectInstance.transform.GetChild(0).GetComponent<AudioSource>();
                    source2 = testingObjectInstance.transform.GetChild(1).GetComponent<AudioSource>();
                    source3 = testingObjectInstance.transform.GetChild(2).GetComponent<AudioSource>();
                }

                source3.Play();
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