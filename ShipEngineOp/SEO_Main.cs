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
        private Vector2 scrollPosition_eng = Vector2.zero;
        private Vector2 scrollPosition_fuel = Vector2.zero;

        private string[] selectedFuelType = { "None", "None", "None" };
        private string[] fuelTypes;
        private int[] selectedFuelIndex = new int[3];

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

                if (selectedEngine[idx] != "None" && engineData.ContainsKey(selectedEngine[idx]))
                {
                    var data = engineData[selectedEngine[idx]];
                    GUILayout.Label("Weight: " + data.weight + " t");
                    GUILayout.Label("Thrust: " + data.thrust + " kN");
                    GUILayout.Label("ISP: " + data.isp + " s");
                    GUILayout.Label("Wet/Dry: " + data.wdr);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Altitude (Kerbin): " + (selectedAltitude / 1000).ToString("F0") + " km");
            selectedAltitude = GUILayout.HorizontalSlider(selectedAltitude, 0f, 70000f, GUILayout.Width(200));
            //GUILayout.Label("Altitude (Kerbin): " + (selectedAltitude / 1000).ToString("F3") + " km");
            //logAltitude = GUILayout.HorizontalSlider(logAltitude, 0f, Mathf.Log(70000f), GUILayout.Width(200));
            //selectedAltitude = Mathf.Exp(logAltitude);
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

            //foreach (var fuelType in groupedEngines.Keys)
            //{
            //    GUILayout.Label(fuelType, GUI.skin.box);
            //    foreach (var engine in groupedEngines[fuelType])
            //    {
            //        if (GUILayout.Button(engine))
            //        {
            //            selectedEngine[indexEngine] = engine;
            //            showEngineList = false;
            //        }
            //    }
            //}

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        //void Update()
        //{
        //    if (Input.GetKeyDown(KeyCode.F2))
        //    {
        //        showMainWindow = !showMainWindow;
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
            groupedEngines.Add("None", new List<string> { "None" });
            engineData["None"] = (0, 0, 0, 1);

            foreach (var part in availableParts)
            {
                if (part.partPrefab != null && (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX")))
                {
                    //enginePartList.Add(part.title);
                    //weightList.Add(part.partPrefab.mass);

                    var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
                    if (engineModule != null)
                    {
                        var propellantNames = engineModule.propellants.Select(p => p.name).ToList();
                        if (propellantNames.Contains("SolidFuel")) continue;

                        string fuelType = string.Join(", ", propellantNames);
                        if (!groupedEngines.ContainsKey(fuelType))
                        {
                            groupedEngines[fuelType] = new List<string>();
                        }
                        groupedEngines[fuelType].Add(part.title);

                        //enginePartList.Add(part.title);
                        //weightList.Add(part.partPrefab.mass);

                        float weight = part.partPrefab.mass;
                        float pressure = (float)(FlightGlobals.getStaticPressure(selectedAltitude, Planetarium.fetch.Home) / FlightGlobals.getStaticPressure(0, Planetarium.fetch.Home));
                        float ispAtAltitude = engineModule.atmosphereCurve.Evaluate(pressure);
                        float thrustAtAltitude = (ispAtAltitude / engineModule.atmosphereCurve.Evaluate(0)) * engineModule.maxThrust;

                        //thrustList.Add(thrustAtAltitude);
                        //ispList.Add(ispAtAltitude);

                        float wdrValue = 1f;

                        if (propellantNames.Contains("LiquidFuel")) wdrValue = 9f;
                        else if (propellantNames.Contains("MonoPropellant")) wdrValue = 5f;
                        else if (propellantNames.Contains("XenonGas") || propellantNames.Contains("ArgonGas") || propellantNames.Contains("Lithium"))
                            wdrValue = 4f;
                        else if (propellantNames.Contains("LqdHydrogen") && propellantNames.Contains("Oxidizer"))
                            wdrValue = 8.24f;
                        else if (propellantNames.Contains("LqdMethane") && propellantNames.Contains("Oxidizer"))
                            wdrValue = 8.5f;
                        else if (propellantNames.Contains("LqdHydrogen") || propellantNames.Contains("LqdMethane"))
                            wdrValue = 6f;


                        //foreach (var propellant in engineModule.propellants)
                        //{
                        //    switch (propellant.name)
                        //    {
                        //        case "LiquidFuel":
                        //            wdrValue = 9f;
                        //            break;
                        //        case "MonoPropellant":
                        //            wdrValue = 5f;
                        //            break;
                        //        case "XenonGas":
                        //            wdrValue = 4f;
                        //            break;
                        //        case "LqdHydrogen":
                        //            wdrValue = 9f;
                        //            break;
                        //        case "LqdMethane":
                        //            wdrValue = 9f;
                        //            break;
                        //        case "ArgonGas":
                        //            wdrValue = 4f;
                        //            break;
                        //        case "Lithium":
                        //            wdrValue = 4f;
                        //            break;
                        //    }
                        //    if (wdrValue > 1) break;
                        //}
                        //wdrList.Add(wdrValue);
                        engineData[part.title] = (weight, thrustAtAltitude, ispAtAltitude, wdrValue);
                    }
                    //else
                    //{
                    //    thrustList.Add(0);
                    //    ispList.Add(0);
                    //    wdrList.Add(1);
                    //}
                    
                }
            }

            //engineWeights = weightList.ToArray();
            //engineThrusts = thrustList.ToArray();
            //engineISPs = ispList.ToArray();
            //engineWDRs = wdrList.ToArray();
        }
    }
}
