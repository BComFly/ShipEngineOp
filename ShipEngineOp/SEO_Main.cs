using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Agents.Mentalities;
using KSP.UI.Screens;
using UnityEngine;


namespace ShipEngineOptimization
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class SEO_Main : MonoBehaviour
    {
        private static bool showMainWindow = false;
        private static bool showEngineList = false;
        public static bool showDvLimit = false;
        public static bool showNumEng = false;
        public const int mainWndWidth = 660;
        public const int mainWndHeight = 200;
        public const int dvWndWidth = 700;
        public static Rect mainWndRect = new Rect(400, 200, mainWndWidth, mainWndHeight);
        public static Rect dvWndRect = new Rect(600, 200, dvWndWidth, mainWndHeight);
        public static Rect numWndRect = new Rect(600, 200, dvWndWidth, mainWndHeight);
        public static Rect engineWndRect = new Rect(400, 400, 250, 300);

        private ApplicationLauncherButton appButton;

        void Start()
        {
            Debug.Log("[EngDraw] Mod Initialized in Editor!");
            RefreshEngineParts();
            //InitializeEngineParts();
            fuelTypes = groupedEngines.Keys.ToArray();

            if (ApplicationLauncher.Instance != null)
            {
                appButton = ApplicationLauncher.Instance.AddModApplication(
                    () => showMainWindow = true,
                    () => showMainWindow = false,
                    null, null, null, null,
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                    GameDatabase.Instance.GetTexture("ShipEngineOptimization/SEO_appIcon", false)
                );
            }
        }

        void OnDestroy()
        {
            if (appButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
                appButton = null;
            }
        }

        void OnGUI()
        {
            if (showMainWindow)
            {
                mainWndRect = GUILayout.Window(8881, mainWndRect, DrawMainWindow, "Ship Engine Optimization");
            }
            if (showDvLimit)
            {
                dvWndRect = GUILayout.Window(8882, dvWndRect, SEO_GraphWindow.DrawDvLimitWindow, "Delta-v Limit Graph");
            }
            if (showNumEng)
            {
                numWndRect = GUILayout.Window(8883, numWndRect, SEO_GraphWindow.DrawNumEngWindow, "Engine Performance Chart");
            }
            if (showEngineList)
            {
                engineWndRect = GUILayout.Window(8884, engineWndRect, DrawEngineWindow, "Engine Selection List");
            }
        }

        //public static int[] engineIndexes = new int[] { 0, 0, 0 };
        private float selectedAltitude = 70000f;
        //private float logAltitude = Mathf.Log(70000f);
        public static int indexEngine = 0;
        //public static string[] engineParts = new string[] { "None" };
        public static string[] selectedEngine = new string[] { "None", "None", "None" };
        //public static float[] engineWeights;
        //public static float[] engineThrusts;
        //public static float[] engineISPs;
        //public static float[] engineWDRs;
        public static Dictionary<string, List<string>> groupedEngines = new Dictionary<string, List<string>>();
        public static Dictionary<string, (float weight, float thrust, float isp, float wdr)> engineData = new Dictionary<string, (float, float, float, float)>();
        //public static Dictionary<string, float?> customWdrValues = new Dictionary<string, float?>();
        private Vector2 scrollPosition_eng = Vector2.zero;
        private Vector2 scrollPosition_fuel = Vector2.zero;

        private string[] selectedFuelType = { "None", "None", "None" };
        private string[] fuelTypes;
        private int[] selectedFuelIndex = new int[3];

        public static Dictionary<string, float> currentWdr = new Dictionary<string, float>()
        {
            {"None", 1f }
        };
        public static Dictionary<string, float> wdrValues = new Dictionary<string, float>()
        {
            { "LiquidFuel", 9f },
            { "LiquidFuel, Oxidizer", 9f },
            { "MonoPropellant", 5f },
            { "XenonGas", 4f },
            { "ArgonGas", 4f },
            { "Lithium", 4f },
            { "LqdHydrogen, Oxidizer", 8.24f },
            { "LqdMethane, Oxidizer", 8.5f },
            { "LqdHydrogen", 6f },
            { "LqdMethane", 6f }
        };

        void DrawMainWindow(int windowID)
        {
            if (GUI.Button(new Rect(mainWndRect.size.x - 17, 2, 15, 15), ""))
            {
                showMainWindow = false;
            }
            GUILayout.BeginHorizontal();

            for (int idx = 0; idx < selectedEngine.Length; idx++)
            {
                GUILayout.BeginVertical(GUILayout.Width(mainWndWidth/3 - 10));
                if (GUILayout.Button("Engine " + (idx + 1).ToString()))
                {
                    showEngineList = true;
                    indexEngine = idx;
                }

                GUILayout.Box(selectedEngine[idx], GUILayout.Width(mainWndWidth / 3 - 10));

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                //if (selectedEngine[idx] != "None" && engineData.ContainsKey(selectedEngine[idx]))
                //{
                //    var data = engineData[selectedEngine[idx]];
                //    //GUILayout.Label(data.weight.ToString() + " t");
                //    //GUILayout.Label(data.thrust.ToString("F1") + " kN");
                //    //GUILayout.Label(data.isp.ToString("F0") + " s");
                //    //GUILayout.Label(data.wdr.ToString());

                //    GUILayout.Label("Weight: " + data.weight.ToString("F0") + " t");
                //    GUILayout.Label("Thrust: " + data.thrust.ToString("F1") + " kN");
                //    GUILayout.Label("ISP: " + data.isp.ToString("F0") + " s");
                //    GUILayout.Label("Wet/Dry: " + data.wdr);
                //}
                GUILayout.Label("Weight: ");
                GUILayout.Label("Thrust: ");
                GUILayout.Label("ISP: ");
                GUILayout.Label("Wet/Dry: ");
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                if (selectedEngine[idx] != "None" && engineData.ContainsKey(selectedEngine[idx]))
                {
                    var data = engineData[selectedEngine[idx]];
                    GUILayout.Label(data.weight.ToString() + " t");
                    GUILayout.Label(data.thrust.ToString("F1") + " kN");
                    GUILayout.Label(data.isp.ToString("F0") + " s");
                    //GUILayout.Label(data.wdr.ToString());

                    float currentValue = currentWdr.ContainsKey(selectedEngine[idx]) ? currentWdr[selectedEngine[idx]] : data.wdr;
                    string wdrStr = GUILayout.TextField(currentValue.ToString("F2"), GUILayout.Width(40));
                    if (float.TryParse(wdrStr, out float newWdr))
                    {
                        currentWdr[selectedEngine[idx]] = newWdr;
                    }

                    //float currentWdr = customWdrValues.ContainsKey(selectedEngine[idx]) && customWdrValues[selectedEngine[idx]].HasValue
                    //    ? customWdrValues[selectedEngine[idx]].Value
                    //    : data.wdr;

                    //string wdrStr = GUILayout.TextField(currentWdr.ToString("F2"));
                    //if (float.TryParse(wdrStr, out float newWdr))
                    //{
                    //    customWdrValues[selectedEngine[idx]] = newWdr;
                    //}
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Altitude (Kerbin): " + (selectedAltitude / 1000).ToString("F0") + " km");
            selectedAltitude = GUILayout.HorizontalSlider(selectedAltitude, 0f, 70000f, GUILayout.Width(200));
            //GUILayout.Label("Altitude (Kerbin): " + (selectedAltitude / 1000).ToString("F3") + " km");
            //logAltitude = GUILayout.HorizontalSlider(logAltitude, 0f, Mathf.Log(70000f), GUILayout.Width(200));
            //selectedAltitude = Mathf.Exp(logAltitude);
            //UpdateEnginePerformance();
            RefreshEngineParts();

            GUILayout.Space(10); // Add spacing before the separation line
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); // Separation line
            GUILayout.Space(10); // Add spacing after the separation line

            GUILayout.Label("Select at least one engine to continue");
            if (selectedEngine.Count(eng => eng == "None") == selectedEngine.Count())
            {
                showNumEng = false;
                showDvLimit = false;
            }
            //if (GUILayout.Button("Engine Performance Chart"))
            //{
            //    showNumEng = true;
            //}
            //if (GUILayout.Button("Delta-v Limit Graph"))
            //{
            //    showDvLimit = true;
            //}
            if (GUILayout.Button("Engine Performance Chart") && selectedEngine.Count(eng => eng == "None") != selectedEngine.Count())
            {
                showNumEng = true;
            }
            if (GUILayout.Button("Delta-v Limit Graph") && selectedEngine.Count(eng => eng == "None") != selectedEngine.Count())
            {
                showDvLimit = true;
            }
            //if (GUILayout.Button("Engine Performance Chart") && selectedEngine.Count(eng => eng == "None") != selectedEngine.Count())
            //{
            //    //if (selectedEngine.Count(eng => eng == "None") == selectedEngine.Count())
            //    //{
            //    //    GUILayout.Label("Select at least one engine to continue");
            //    //}
            //    //else
            //    //{
            //    //    showNumEng = true;
            //    //}
            //    showNumEng = true;
            //}

            //if (GUILayout.Button("Delta-v Limit Graph") && selectedEngine.Count(eng => eng == "None") != selectedEngine.Count())
            //{
            //    showDvLimit = true;
            //}

            GUI.DragWindow();
        }


        void DrawEngineWindow(int windowID)
        {
            if (GUI.Button(new Rect(engineWndRect.size.x - 17, 2, 15, 15), ""))
            {
                showEngineList = false;
            }

            GUILayout.Label("Select fuel type:");
            scrollPosition_fuel = GUILayout.BeginScrollView(scrollPosition_fuel, GUILayout.Width(300), GUILayout.Height(100));

            selectedFuelIndex[indexEngine] = GUILayout.SelectionGrid(selectedFuelIndex[indexEngine], fuelTypes, 1);
            selectedFuelType[indexEngine] = fuelTypes[selectedFuelIndex[indexEngine]];

            GUILayout.EndScrollView();

            GUILayout.Space(20); // Add spacing before the separation line
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); // Separation line
            GUILayout.Space(10); // Add spacing after the separation line

            GUILayout.Label("Select Engine " + (SEO_Main.indexEngine + 1).ToString() + ":");
            scrollPosition_eng = GUILayout.BeginScrollView(scrollPosition_eng, GUILayout.Width(300), GUILayout.Height(350));

            //if (selectedFuelType[indexEngine] == "None")
            //{
            //    GUILayout.Label("Please select a fuel type to show corresponding engines");
            //}
            if (groupedEngines.ContainsKey(selectedFuelType[indexEngine]))
            {
                foreach (var engine in groupedEngines[selectedFuelType[indexEngine]])
                {
                    if (GUILayout.Button(engine))
                    {
                        selectedEngine[indexEngine] = engine;
                        showEngineList = false;
                    }
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        //void InitializeEngineParts()
        //{
        //    var availableParts = PartLoader.LoadedPartsList;
        //    groupedEngines.Clear();
        //    engineData.Clear();

        //    groupedEngines.Add("None", new List<string> { "None" });

        //    foreach (var part in availableParts)
        //    {
        //        if (part.partPrefab != null && (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX")))
        //        {
        //            var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
        //            if (engineModule != null)
        //            {
        //                var propellantNames = string.Join(", ", engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge" && p != "IntakeAir"));
        //                if (propellantNames.Contains("SolidFuel")) continue;

        //                if (!groupedEngines.ContainsKey(propellantNames))
        //                {
        //                    groupedEngines[propellantNames] = new List<string>();
        //                }
        //                groupedEngines[propellantNames].Add(part.title);

        //                float weight = part.partPrefab.mass;
        //                float wdrValue = wdrValues.ContainsKey(propellantNames) ? wdrValues[propellantNames] : 1f;

        //                engineData[part.title] = (weight, 0, 0, wdrValue);
        //            }
        //        }
        //    }
        //    fuelTypes = groupedEngines.Keys.ToArray();
        //}

        //void UpdateEnginePerformance()
        //{
        //    foreach (var key in engineData.Keys.ToList())
        //    {
        //        var part = PartLoader.LoadedPartsList.FirstOrDefault(p => p.title == key);
        //        var engineModule = part?.partPrefab?.Modules.GetModule<ModuleEngines>();
        //        if (engineModule != null)
        //        {
        //            float pressure = (float)(FlightGlobals.getStaticPressure(selectedAltitude, Planetarium.fetch.Home) / FlightGlobals.getStaticPressure(0, Planetarium.fetch.Home));
        //            float ispAtAltitude = engineModule.atmosphereCurve.Evaluate(pressure);
        //            float thrustAtAltitude = (ispAtAltitude / engineModule.atmosphereCurve.Evaluate(0)) * engineModule.maxThrust;

        //            //float pressure = (float)FlightGlobals.getStaticPressure(selectedAltitude, Planetarium.fetch.Home);
        //            //float ispAtAltitude = engineModule.atmosphereCurve.Evaluate(pressure);
        //            //float thrustAtAltitude = (ispAtAltitude / engineModule.atmosphereCurve.Evaluate(1)) * engineModule.maxThrust;

        //            engineData[key] = (engineData[key].weight, thrustAtAltitude, ispAtAltitude, engineData[key].wdr);
        //        }
        //    }
        //}

        void RefreshEngineParts()
        {
            var availableParts = PartLoader.LoadedPartsList;
            //var enginePartList = new List<string> { "None" };
            //var weightList = new List<float> { 0 };
            //var thrustList = new List<float> { 0 };
            //var ispList = new List<float> { 0 };
            //var wdrList = new List<float> { 1 };

            engineData.Clear();
            groupedEngines.Clear();
            //currentWdr.Clear();

            groupedEngines.Add("None", new List<string> { "None" });
            engineData["None"] = (0, 0, 0, 1);

            foreach (var part in availableParts)
            {
                if (part.partPrefab != null && (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX")))
                {
                    var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
                    if (engineModule != null)
                    {
                        var propellantNames = string.Join(", ", engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge" && p != "IntakeAir"));
                        if (propellantNames.Contains("SolidFuel")) continue;

                        if (!groupedEngines.ContainsKey(propellantNames))
                        {
                            groupedEngines[propellantNames] = new List<string>();
                        }
                        groupedEngines[propellantNames].Add(part.title);

                        float weight = part.partPrefab.mass;
                        float pressure = (float)(FlightGlobals.getStaticPressure(selectedAltitude, Planetarium.fetch.Home) / FlightGlobals.getStaticPressure(0, Planetarium.fetch.Home));
                        float ispAtAltitude = engineModule.atmosphereCurve.Evaluate(pressure);
                        float thrustAtAltitude = (ispAtAltitude / engineModule.atmosphereCurve.Evaluate(0)) * engineModule.maxThrust;
                        float wdrValue = wdrValues.ContainsKey(propellantNames) ? wdrValues[propellantNames] : 1f;

                        engineData[part.title] = (weight, thrustAtAltitude, ispAtAltitude, wdrValue);
                    }
                }
            }
        }
    }
}
