using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bhaptics.Tact;
using bHapticsMusical;
using System.Resources;
using System.Globalization;
using System.Collections;


namespace MyBhapticsTactsuit
{
    public class TactsuitVR
    {
        /* A class that contains the basic functions for the bhaptics Tactsuit, like:
         * - A Heartbeat function that can be turned on/off
         * - A function to read in and register all .tact patterns in the bHaptics subfolder
         * - A logging hook to output to the Melonloader log
         * - 
         * */
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        // Event to start and stop the heartbeat thread
        //private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false);
        // dictionary of all feedback patterns found in the bHaptics directory
        public Dictionary<String, String> FeedbackMap = new Dictionary<String, String>();
        public Dictionary<String, String> defaultEffects = new Dictionary<String, String>();
        public List<string> myEffectStrings = new List<string> { };

#pragma warning disable CS0618 // remove warning that the C# library is deprecated
        public HapticPlayer hapticPlayer;
#pragma warning restore CS0618 


        private static RotationOption defaultRotationOption = new RotationOption(0.0f, 0.0f);

        public TactsuitVR()
        {
            LOG("Initializing suit");
            try
            {
#pragma warning disable CS0618 // remove warning that the C# library is deprecated
                hapticPlayer = new HapticPlayer("H3VR_bhaptics", "H3VR_bhaptics");
#pragma warning restore CS0618
                suitDisabled = false;
            }
            catch { LOG("Suit initialization failed!"); }
            RegisterAllTactFiles();
            LOG("Starting HeartBeat thread...");
        }

        public void LOG(string logStr)
        {
            Plugin.Log.Info(logStr);
            //Plugin.Log.LogMessage(logStr);
        }



        void RegisterAllTactFiles()
        {
            ResourceSet resourceSet = bHapticsMusical.Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);

            foreach (DictionaryEntry dict in resourceSet)
            {
                if (dict.Key.ToString().StartsWith("LightEffect"))
                {
                    defaultEffects.Add(dict.Key.ToString(), dict.Value.ToString());
                    continue;
                }
                try
                {
                    hapticPlayer.RegisterTactFileStr(dict.Key.ToString(), dict.Value.ToString());
                    LOG("Pattern registered: " + dict.Key.ToString());
                }
                catch (Exception e) { LOG(e.ToString()); }

                FeedbackMap.Add(dict.Key.ToString(), dict.Value.ToString());
            }

            // Get location of the compiled assembly and search through "bHaptics" directory and contained patterns
            // string configPath = Directory.GetCurrentDirectory() + "\\Plugins\\bHaptics";
            string configPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, "bHapticsMusical");
            //LOG("Path: " + configPath);
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
                foreach (KeyValuePair<string, string> dict in defaultEffects)
                {
                    string filePath = Path.Combine(configPath, dict.Key + ".tact");
                    File.WriteAllText(filePath, dict.Value);
                }
            }
            DirectoryInfo d = new DirectoryInfo(configPath);
            FileInfo[] Files = d.GetFiles("*.tact", SearchOption.AllDirectories);
            for (int i = 0; i < Files.Length; i++)
            {
                string filename = Files[i].Name;
                string fullName = Files[i].FullName;
                string prefix = Path.GetFileNameWithoutExtension(filename);
                // LOG("Trying to register: " + prefix + " " + fullName);
                if (filename == "." || filename == "..")
                    continue;
                string tactFileStr = File.ReadAllText(fullName);
                try
                {
                    hapticPlayer.RegisterTactFileStr(prefix, tactFileStr);
                    LOG("Light effect pattern registered: " + prefix);
                }
                catch (Exception e) { LOG(e.ToString()); }

                FeedbackMap.Add(prefix, tactFileStr);
                myEffectStrings.Add(prefix);
                
            }
            systemInitialized = true;
        }

        public void PlaybackHaptics(String key, float intensity = 1.0f, float duration = 1.0f)
        {
            //LOG("Trying to play");
            if (FeedbackMap.ContainsKey(key))
            {
                //LOG("ScaleOption");
                ScaleOption scaleOption = new ScaleOption(intensity, duration);
                //LOG("Submit");
                hapticPlayer.SubmitRegistered(key, scaleOption);
            }
            else
            {
                LOG("Feedback not registered: " + key);
            }
        }

        public bool IsPlaying(String effect)
        {
            return IsPlaying(effect);
        }

    }
}
