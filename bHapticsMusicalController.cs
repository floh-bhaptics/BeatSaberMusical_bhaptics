using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;

using HarmonyLib;
using MyBhapticsTactsuit;

namespace bHapticsMusical
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class bHapticsMusical : MonoBehaviour
    {
        public static bHapticsMusical Instance { get; private set; }

        public static TactsuitVR tactsuitVr;
        public static List<string> myEffectStrings = new List<string> { };
        public static Stopwatch timerLastEffect = new Stopwatch();
        public static Stopwatch timerSameTime = new Stopwatch();
        public static int numberOfEvents = 0;
        public static int defaultTriggerNumber = 4;
        public static int currentTriggerNumber = 4;
        public static List<float> highWeights = new List<float> { };
        public static float weightFactor = 1.0f;
        public static bool reducedWeight = false;
        public static bool ringEffectOff = false;
        public static System.Random rnd = new System.Random();


        // These methods are automatically called by Unity, you should remove any you aren't using.
        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (Instance != null)
            {
                Plugin.Log?.Warn($"Instance of {GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            Instance = this;
            Plugin.Log?.Debug($"{name}: Awake()");
            var harmony = new Harmony("bhaptics.musical.patch.beatsaber");
            harmony.PatchAll();

        }
        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Plugin.Log?.Debug($"{name}: OnDestroy()");
            if (Instance == this)
                Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

        }
        #endregion

        #region Lighting effects

        [HarmonyPatch(typeof(TrackLaneRingsRotationEffect), "SpawnRingRotationEffect", new Type[] { })]
        public class bhaptics_RingRotationEffect
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (ringEffectOff) return;
                tactsuitVr.PlaySpecialEffect("RingRotation");
            }
        }

        [HarmonyPatch(typeof(EnvironmentSpawnRotation), "BeatmapEventAtNoteSpawnCallback", new Type[] { typeof(BeatmapEventData) })]
        public class bhaptics_LightChangeEffect
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapEventData beatmapEventData)
            {
                // If it's a "special" effect, just play a pattern
                if ((beatmapEventData.type == BeatmapEventType.Special0) | (beatmapEventData.type == BeatmapEventType.Special1) | (beatmapEventData.type == BeatmapEventType.Special2) | (beatmapEventData.type == BeatmapEventType.Special3))
                {
                    tactsuitVr.PlaySpecialEffect(tactsuitVr.myEffectStrings[rnd.Next(myEffectStrings.Count())]);
                    return;
                }

                // If last effects has been a while, reduce threshold
                if (!timerLastEffect.IsRunning) timerLastEffect.Start();
                if (timerLastEffect.ElapsedMilliseconds >= 2000)
                {
                    if (currentTriggerNumber > 1) currentTriggerNumber -= 1;
                    timerLastEffect.Restart();
                }

                // Count number of effects at the "same time"
                if (timerSameTime.ElapsedMilliseconds <= 100)
                {
                    numberOfEvents += 1;
                    timerSameTime.Restart();
                }
                else
                {
                    numberOfEvents = 0;
                    timerSameTime.Restart();
                }

                // If number of simultaneous events is above threshold, trigger effect
                if (numberOfEvents >= currentTriggerNumber)
                {
                    // reset trigger (if it was lowered)
                    currentTriggerNumber = defaultTriggerNumber;
                    string effectName = myEffectStrings[rnd.Next(myEffectStrings.Count())];
                    tactsuitVr.PlaySpecialEffect(effectName);

                    // check if default trigger was set way too high or too low
                    float weight = (float)numberOfEvents / (float)defaultTriggerNumber / weightFactor;
                    if (weight > 5.0f) highWeights.Add(weight);
                    if (weight < 0.24f) highWeights.Add(weight);
                    // if this happened 4 times in a row, adjust trigger (only down)
                    if (highWeights.Count >= 4)
                    {
                        weightFactor = highWeights.Average();
                        if (weightFactor < 1.0f)
                        {
                            if ((!reducedWeight) && (defaultTriggerNumber > 2))
                            {
                                defaultTriggerNumber -= 1;
                                tactsuitVr.LOG("Trigger adjusted! " + defaultTriggerNumber.ToString() + " " + weightFactor.ToString());
                            }
                        }
                        else reducedWeight = true;
                        highWeights.Clear();
                    }
                }

            }
        }

        #endregion

        #region Map analysis

        public static void resetGlobalParameters()
        {
            highWeights.Clear();
            weightFactor = 1.0f;
            reducedWeight = false;
            defaultTriggerNumber = 4;
            currentTriggerNumber = 4;
            ringEffectOff = false;
        }

        public static void analyzeMap(BeatmapData beatmapData)
        {
            // if there are too many ring effects, it gets annoying
            ringEffectOff = (beatmapData.spawnRotationEventsCount > 50);
            // count total number of events, estimate trigger number
            numberOfEvents = beatmapData.beatmapEventsData.Count();
            defaultTriggerNumber = numberOfEvents / 500;
            if (defaultTriggerNumber <= 1) defaultTriggerNumber = 2;
            currentTriggerNumber = defaultTriggerNumber;
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromBinary", new Type[] { typeof(byte[]), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetBinaryData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                analyzeMap(__result);
            }
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromJson", new Type[] { typeof(string), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetJsonData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                analyzeMap(__result);
            }
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromBeatmapSaveData", new Type[] { typeof(List<BeatmapSaveData.NoteData>), typeof(List<BeatmapSaveData.WaypointData>), typeof(List<BeatmapSaveData.ObstacleData>), typeof(List<BeatmapSaveData.EventData>), typeof(BeatmapSaveData.SpecialEventKeywordFiltersData), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetMemoryData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                analyzeMap(__result);
            }
        }


        #endregion

    }
}
