using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;


namespace ShipEngineOptimization
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class SEO_Main : MonoBehaviour
    {
        public static bool showMainWindow = false;
        public static bool showIndiWindow = false;
        public static bool showEngineList = false;
        public static bool showDvLimit = false;
        public static bool showNumEng = false;
        public static bool showGrossSort = false;
        public static bool showPlanetWindow = false;
        public const int indiWndWidth = 660;
        public const int indiWndHeight = 200;
        public const int dvWndWidth = 700;
        public static Rect mainWndRect = new Rect(600, 200, 250, 200);
        public static Rect indiWndRect = new Rect(400, 200, indiWndWidth, indiWndHeight);
        public static Rect dvWndRect = new Rect(600, 200, dvWndWidth, indiWndHeight);
        public static Rect numWndRect = new Rect(600, 200, dvWndWidth, indiWndHeight);
        public static Rect engineWndRect = new Rect(400, 400, 250, 300);
        public static Rect grossSortWndRect = new Rect(600, 200, 600, 300);
        public static Rect planetWndRect = new Rect(400, 400, 150, 100);

        private ApplicationLauncherButton appButton;

        void Start()
        {
            Debug.Log("[EngDraw] Mod Initialized in Editor!");
            LoadWdrFromConfig();
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
                mainWndRect = GUILayout.Window(8880, mainWndRect, DrawMainWindow, Localizer.Format("#LOC_SHIPENGOP_mainWindowTitle"));
            }
            if (showIndiWindow)
            {
                indiWndRect = GUILayout.Window(8881, indiWndRect, DrawIndiWindow, Localizer.Format("#LOC_SHIPENGOP_indiWindowTitle"));
            }
            if (showDvLimit)
            {
                dvWndRect = GUILayout.Window(8882, dvWndRect, SEO_GraphWindow.DrawDvLimitWindow, Localizer.Format("#LOC_SHIPENGOP_dvGraphTitle"));
            }
            if (showNumEng)
            {
                numWndRect = GUILayout.Window(8883, numWndRect, SEO_GraphWindow.DrawNumEngWindow, Localizer.Format("#LOC_SHIPENGOP_engPerfChartTitle"));
            }
            if (showEngineList)
            {
                engineWndRect = GUILayout.Window(8884, engineWndRect, DrawEngineWindow, Localizer.Format("#LOC_SHIPENGOP_engSelWindowTitle"));
            }
            if (showGrossSort)
            {
                grossSortWndRect = GUILayout.Window(9101, grossSortWndRect, SEO_GraphWindow.DrawGrossSort, Localizer.Format("#LOC_SHIPENGOP_grossCompTitle"));
            }
            if (showPlanetWindow)
            {
                planetWndRect = GUILayout.Window(9102, planetWndRect, DrawplanetWindow, Localizer.Format("#LOC_SHIPENGOP_planetTitle"));
            }
        }

        public static float atmPressure = (float)FlightGlobals.getStaticPressure(0, Planetarium.fetch.Home);
        static float selectedPlanetMaxAltitude = 70000f;

        static float selectedAltitude = 70000f;
        public static int indexEngine = 0;
        public static string emptyStr = Localizer.Format("#LOC_SHIPENGOP_empty");
        public static string[] selectedEngine = new string[] { emptyStr, emptyStr, emptyStr };
        public static bool[] customWdrBool = new bool[3];
        public static bool[] additionalWeightBool = new bool[3];
        public static bool careerToggle = false;

        public static Dictionary<string, List<string>> groupedEngines = new Dictionary<string, List<string>>();
        public static Dictionary<string, (float weight, float thrust, float isp, float wdr)> engineData = new Dictionary<string, (float, float, float, float)>();
        public static Dictionary<string, string> fuelDisplayNames = new Dictionary<string, string>();
        private Vector2 scrollPosition_eng = Vector2.zero;
        private Vector2 scrollPosition_fuel = Vector2.zero;

        static string selectedPlanet = "Kerbin";
        private string[] selectedFuelType = { "None", "None", "None" };
        public static string[] fuelTypes;
        private int[] selectedFuelIndex = new int[3];

        public static Dictionary<string, (bool toggle, float customValue)> customWdr = new Dictionary<string, (bool, float)>()
        {
            {emptyStr, (false, 1f) }
        };

        public static Dictionary<string, (bool toggle, float addiWeight)> additionalWeight = new Dictionary<string, (bool, float)>()
        {
            {emptyStr, (false, 0f) }
        };


        public static Dictionary<string, float> wdrValues = new Dictionary<string, float>();
        //public static Dictionary<string, float> wdrValues = new Dictionary<string, float>()
        //{
        //    { "LiquidFuel", 9f },
        //    { "LiquidFuel, Oxidizer", 9f },
        //    { "Oxidizer, LiquidFuel", 9f },
        //    { "IntakeAir, LiquidFuel", 9f },
        //    { "MonoPropellant", 5f },
        //    { "XenonGas", 4f },
        //    { "ArgonGas", 4f },
        //    { "Lithium", 4f },
        //    { "LqdHydrogen, Oxidizer", 8.24f },
        //    { "LqdMethane, Oxidizer", 8.5f },
        //    { "LqdHydrogen", 6f },
        //    { "LqdMethane", 6f }
        //};

        void DrawMainWindow(int windowID)
        {
            if (GUI.Button(new Rect(mainWndRect.size.x - 17, 2, 15, 15), ""))
            {
                showMainWindow = false;
            }

            GUILayout.BeginVertical();

            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_mainWindow_grossComp"), GUILayout.Height(100)))
            {
                showGrossSort = true;
            }
            
            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_mainWindow_indiComp"), GUILayout.Height(100)))
            {
                //OpenIndiWindow();
                showIndiWindow = true;
            }
            
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void DrawIndiWindow(int windowID)
        {
            if (GUI.Button(new Rect(indiWndRect.size.x - 17, 2, 15, 15), ""))
            {
                showIndiWindow = false;
            }
            GUILayout.BeginHorizontal();

            for (int idx = 0; idx < selectedEngine.Length; idx++)
            {
                GUILayout.BeginVertical(GUILayout.Width(indiWndWidth/3 - 10));
                if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_engine") + " " + (idx + 1).ToString()))
                {
                    showEngineList = true;
                    indexEngine = idx;
                }

                GUILayout.Box(selectedEngine[idx], GUILayout.Width(indiWndWidth / 3 - 10));

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();

                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_weight"));
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_thrust"));
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_isp"));
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_wdr"));
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                if (selectedEngine[idx] != emptyStr && engineData.ContainsKey(selectedEngine[idx]))
                {
                    var data = engineData[selectedEngine[idx]];
                    GUILayout.Label(data.weight.ToString() + " t");
                    GUILayout.Label(data.thrust.ToString("F1") + " kN");
                    GUILayout.Label(data.isp.ToString("F0") + " s");
                    GUILayout.Label(data.wdr.ToString());

                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                //customWdrBool[idx] = currentWdr.ContainsKey(selectedEngine[idx]) ? true : false;
                customWdrBool[idx] = customWdr.ContainsKey(selectedEngine[idx]) ? customWdr[selectedEngine[idx]].toggle : false;
                customWdrBool[idx] = GUILayout.Toggle(customWdrBool[idx], Localizer.Format("#LOC_SHIPENGOP_indiWindow_wdrToggle"));

                if (customWdrBool[idx])
                {
                    float currentWdr = customWdr.ContainsKey(selectedEngine[idx]) ? customWdr[selectedEngine[idx]].customValue : engineData[selectedEngine[idx]].wdr;
                    string customWdrStr = GUILayout.TextField(currentWdr.ToString("F2"), GUILayout.Width(40));
                    if (float.TryParse(customWdrStr, out float newWdr))
                    {
                        customWdr[selectedEngine[idx]] = (true, newWdr);
                    }
                }
                else if (customWdr.ContainsKey(selectedEngine[idx]))
                {
                    customWdr[selectedEngine[idx]] = (false, customWdr[selectedEngine[idx]].customValue);
                }

                //if (customWdrBool[idx])
                //{
                //    float currentValue = customWdr.ContainsKey(selectedEngine[idx]) ? customWdr[selectedEngine[idx]] : engineData[selectedEngine[idx]].wdr;
                //    string wdrStr = GUILayout.TextField(currentValue.ToString("F2"), GUILayout.Width(40));
                //    if (float.TryParse(wdrStr, out float newWdr))
                //    {
                //        customWdr[selectedEngine[idx]] = newWdr;
                //    }
                //}
                //else
                //{
                //    customWdr.Remove(selectedEngine[idx]);
                //}
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                //additionalWeightBool[idx] = additionalWeight.ContainsKey(selectedEngine[idx]) ? true : false;
                additionalWeightBool[idx] = additionalWeight.ContainsKey(selectedEngine[idx]) ? additionalWeight[selectedEngine[idx]].toggle : false;
                additionalWeightBool[idx] = GUILayout.Toggle(additionalWeightBool[idx], Localizer.Format("#LOC_SHIPENGOP_indiWindow_addiWeightToggle"));

                if (additionalWeightBool[idx])
                {
                    //float currentAddiWeight = additionalWeight.ContainsKey(selectedEngine[idx]) ? additionalWeight[selectedEngine[idx]] : 0f;
                    float currentAddiWeight = additionalWeight.ContainsKey(selectedEngine[idx]) ? additionalWeight[selectedEngine[idx]].addiWeight : 0f;
                    string addiWeightStr = GUILayout.TextField(currentAddiWeight.ToString("F2"), GUILayout.Width(50));
                    if (float.TryParse(addiWeightStr, out float newAddiWeight))
                    {
                        additionalWeight[selectedEngine[idx]] = (true, newAddiWeight);
                    }
                }
                else if (additionalWeight.ContainsKey(selectedEngine[idx]))
                {
                    additionalWeight[selectedEngine[idx]] = (false, additionalWeight[selectedEngine[idx]].addiWeight);
                    //additionalWeight.Remove(selectedEngine[idx]);
                }
                GUILayout.EndHorizontal();


                if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_applyToFuel")))
                {
                    string engineFuelType = groupedEngines.FirstOrDefault(kvp => kvp.Value.Contains(selectedEngine[idx])).Key;
                    if (!string.IsNullOrEmpty(engineFuelType) && groupedEngines.ContainsKey(engineFuelType))
                    {
                        foreach (var engine in groupedEngines[engineFuelType])
                        {
                            if (customWdr.ContainsKey(selectedEngine[idx]))
                            { customWdr[engine] = customWdr[selectedEngine[idx]]; }

                            if (additionalWeight.ContainsKey(selectedEngine[idx]))
                            { additionalWeight[engine] = additionalWeight[selectedEngine[idx]]; }
                        }
                    }
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_indiWindow_selectWarning"));
            if (selectedEngine.Count(eng => eng == emptyStr) == selectedEngine.Count())
            {
                showNumEng = false;
                showDvLimit = false;
            }
            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_enginePerfChart")) && selectedEngine.Count(eng => eng == emptyStr) != selectedEngine.Count())
            {
                showNumEng = true;
            }
            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_dvLimitCurce")) && selectedEngine.Count(eng => eng == emptyStr) != selectedEngine.Count())
            {
                showDvLimit = true;
            }
            GUILayout.EndVertical();

            GUILayout.Space(100);

            GUILayout.BeginVertical();
            ToggleCareer();
            GUILayout.Space(10);
            AtmosphereSetting();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            //GUILayout.Space(30); // Add spacing before the separation line
            //GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            //GUILayout.Space(10);

            //GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_indiWindow_selectWarning"));
            //if (selectedEngine.Count(eng => eng == emptyStr) == selectedEngine.Count())
            //{
            //    showNumEng = false;
            //    showDvLimit = false;
            //}
            //if (GUILayout.Button("Engine Performance Chart"))
            //{
            //    showNumEng = true;
            //}
            //if (GUILayout.Button("Delta-v Limit Graph"))
            //{
            //    showDvLimit = true;
            //}
            //if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_enginePerfChart")) && selectedEngine.Count(eng => eng == emptyStr) != selectedEngine.Count())
            //{
            //    showNumEng = true;
            //}
            //if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_dvLimitCurce")) && selectedEngine.Count(eng => eng == emptyStr) != selectedEngine.Count())
            //{
            //    showDvLimit = true;
            //}

            //if (GUILayout.Button("Gross Comparison"))
            //{
            //    showGrossSort = true;
            //}

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

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engList_fuelSelect"));
            scrollPosition_fuel = GUILayout.BeginScrollView(scrollPosition_fuel, GUILayout.Width(300), GUILayout.Height(100));

            //selectedFuelIndex[indexEngine] = GUILayout.SelectionGrid(selectedFuelIndex[indexEngine], fuelTypes, 1);
            string[] fuelDisplayList = fuelTypes.Select(f => fuelDisplayNames.ContainsKey(f) ? fuelDisplayNames[f] : emptyStr).ToArray();
            //string[] fuelDisplayList = fuelTypes.Select(f => fuelDisplayNames.ContainsKey(f) ? fuelDisplayNames[f] : f).ToArray();
            selectedFuelIndex[indexEngine] = GUILayout.SelectionGrid(selectedFuelIndex[indexEngine], fuelDisplayList, 1);

            selectedFuelType[indexEngine] = fuelTypes[selectedFuelIndex[indexEngine]];

            GUILayout.EndScrollView();

            GUILayout.Space(20); // Add spacing before the separation line
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); // Separation line
            GUILayout.Space(10); // Add spacing after the separation line

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engList_engineNumSelect") + (SEO_Main.indexEngine + 1).ToString());
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

        void DrawplanetWindow(int windowID)
        {
            if (GUI.Button(new Rect(planetWndRect.size.x - 17, 2, 15, 15), ""))
            {
                showPlanetWindow = false;
            }

            string[] planetNames = FlightGlobals.Bodies.Where(b => b.atmosphere && !b.isStar).Select(b => b.bodyName).ToArray();

            foreach (var planetStr in planetNames)
            {
                if (GUILayout.Button(planetStr))
                {
                    selectedPlanet = planetStr;
                    var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == planetStr);
                    SEO_Functions.surfaceGravity = (float)(planet.gravParameter / (planet.Radius * planet.Radius));

                    //Vector2 mousePos = Event.current.mousePosition;
                    //planetWndRect.position = mousePos;

                    selectedPlanetMaxAltitude = (float)planet.atmosphereDepth;
                    selectedAltitude = selectedPlanetMaxAltitude;

                    showPlanetWindow = false;
                }
            }

            GUI.DragWindow();
        }

            //private HashSet<string> selectedTopFuelTypes = new HashSet<string>();

            //void DrawGrossSort(int windowID)
            //{
            //    GUILayout.Label("Filter by Fuel Types:");

            //    foreach (var fuel in fuelTypes)
            //    {
            //        if (fuel == "None") continue;

            //        bool wasSelected = selectedTopFuelTypes.Contains(fuel);
            //        bool isSelectedNow = GUILayout.Toggle(wasSelected, fuel);

            //        if (isSelectedNow && !wasSelected)
            //            selectedTopFuelTypes.Add(fuel);
            //        else if (!isSelectedNow && wasSelected)
            //            selectedTopFuelTypes.Remove(fuel);
            //    }

            //    GUILayout.Label("Top 10 Engines by ISP:");

            //    var topEngines = selectedTopFuelTypes
            //        .Where(fuel => groupedEngines.ContainsKey(fuel))
            //        .SelectMany(fuel => groupedEngines[fuel])
            //        .Distinct()
            //        .Where(engineName => engineData.ContainsKey(engineName))
            //        .Select(engineName => new KeyValuePair<string, (float, float, float, float)>(engineName, engineData[engineName]))
            //        .OrderByDescending(e => e.Value.Item3)
            //        .Take(10);

            //    foreach (var engine in topEngines)
            //    {
            //        GUILayout.Label($"{engine.Key}: {engine.Value.Item3:F2} s");
            //    }

            //    if (GUILayout.Button("Close"))
            //    {
            //        showTopIspEngines = false;
            //    }

            //    GUI.DragWindow();

            //    //GUILayout.Label("Top 10 Engines by ISP:");
            //    //var topEngines = engineData
            //    //    .OrderByDescending(e => e.Value.isp)
            //    //    .Take(10);

            //    //foreach (var engine in topEngines)
            //    //{
            //    //    GUILayout.Label($"{engine.Key}: {engine.Value.isp:F2} s");
            //    //}

            //    //if (GUILayout.Button("Close"))
            //    //{
            //    //    showTopIspEngines = false;
            //    //}

            //    //GUI.DragWindow();
            //}


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

        static void RefreshEngineParts()
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

            groupedEngines.Add("None", new List<string> { emptyStr });
            engineData[emptyStr] = (0, 0, 0, 1);

            foreach (var part in availableParts)
            {
                if (careerToggle && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    if (!ResearchAndDevelopment.PartModelPurchased(part))
                        continue;
                }

                if (part.partPrefab != null && (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX")))
                {
                    if (part.partPrefab.Modules.Contains("MultiModeEngine"))
                    {
                        string engineName;
                        var multiModeModule = part.partPrefab.Modules.GetModule<MultiModeEngine>();
                        foreach (var engineModule in part.partPrefab.FindModulesImplementing<ModuleEngines>())
                        {

                            if (engineModule.engineID == multiModeModule.primaryEngineID)
                            {                              
                                engineName = multiModeModule.primaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.primaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.primaryEngineID})";
                            }
                            else
                            {
                                engineName = multiModeModule.secondaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.secondaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.secondaryEngineID})";
                            }
                            SetEngineData(engineName, part.partPrefab.mass, engineModule);
                        }
                    }
                    else
                    { 
                        var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
                        if (engineModule != null)
                        {
                            SetEngineData(part.title, part.partPrefab.mass, engineModule);
                        }
                    }
                }
            }
        }

        //static void RefreshEngineParts()
        //{
        //    var availableParts = PartLoader.LoadedPartsList;
        //    //var enginePartList = new List<string> { "None" };
        //    //var weightList = new List<float> { 0 };
        //    //var thrustList = new List<float> { 0 };
        //    //var ispList = new List<float> { 0 };
        //    //var wdrList = new List<float> { 1 };

        //    engineData.Clear();
        //    groupedEngines.Clear();
        //    //currentWdr.Clear();

        //    groupedEngines.Add("None", new List<string> { "None" });
        //    engineData["None"] = (0, 0, 0, 1);

        //    foreach (var part in availableParts)
        //    {
        //        if (part.partPrefab != null && (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX")))
        //        {
        //            var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
        //            if (engineModule != null)
        //            {
        //                //var propellantNames = string.Join(", ", engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge" && p != "IntakeAir"));
        //                var propellantNames = string.Join(", ", engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge"));
        //                if (propellantNames.Contains("SolidFuel")) continue;

        //                if (!groupedEngines.ContainsKey(propellantNames))
        //                {
        //                    groupedEngines[propellantNames] = new List<string>();
        //                    var localizedNames = engineModule.propellants
        //                        .Select(p => PartResourceLibrary.Instance.GetDefinition(p.name)?.displayName ?? p.name);
        //                    //fuelDisplayNames[propellantNames] = string.Join(", ", localizedNames);
        //                    fuelDisplayNames[propellantNames] = string.Join(Localizer.Format("#LOC_SHIPENGOP_joint"), localizedNames);
        //                }
        //                groupedEngines[propellantNames].Add(part.title);

        //                float weight = part.partPrefab.mass;
        //                float pressure = (float)(FlightGlobals.getStaticPressure(selectedAltitude, Planetarium.fetch.Home) / FlightGlobals.getStaticPressure(0, Planetarium.fetch.Home));
        //                float ispAtAltitude = engineModule.atmosphereCurve.Evaluate(pressure);
        //                float thrustAtAltitude = (ispAtAltitude / engineModule.atmosphereCurve.Evaluate(0)) * engineModule.maxThrust;
        //                float wdrValue = wdrValues.ContainsKey(propellantNames) ? wdrValues[propellantNames] : 1f;

        //                engineData[part.title] = (weight, thrustAtAltitude, ispAtAltitude, wdrValue);
        //            }
        //        }
        //    }
        //}

        static void SetEngineData (string name, float weight, ModuleEngines engineModule)
        {
            //var propellantNames = string.Join(", ", engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge"));
            var propellantNames = GetSortedFuelType(engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge"));
            if (propellantNames.Contains("SolidFuel")) return;

            if (!groupedEngines.ContainsKey(propellantNames))
            {
                groupedEngines[propellantNames] = new List<string>();
                var localizedNames = engineModule.propellants
                    .Where(p => p.name != "ElectricCharge")
                    .Select(p => PartResourceLibrary.Instance.GetDefinition(p.name)?.displayName ?? p.name);
                //fuelDisplayNames[propellantNames] = string.Join(", ", localizedNames);
                fuelDisplayNames[propellantNames] = string.Join(Localizer.Format("#LOC_SHIPENGOP_joint"), localizedNames);
            }
            groupedEngines[propellantNames].Add(name);

            var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == selectedPlanet);
            if (planet == null) return;

            float pressure = (float)FlightGlobals.getStaticPressure(selectedAltitude, planet) / atmPressure;

            //float pressure = (float)(FlightGlobals.getStaticPressure(selectedAltitude, Planetarium.fetch.Home) / FlightGlobals.getStaticPressure(0, Planetarium.fetch.Home));
            float ispAtAltitude = engineModule.atmosphereCurve.Evaluate(pressure);
            float thrustAtAltitude = (ispAtAltitude / engineModule.atmosphereCurve.Evaluate(0)) * engineModule.maxThrust;
            float wdrValue = wdrValues.ContainsKey(propellantNames) ? wdrValues[propellantNames] : 1f;

            engineData[name] = (weight, thrustAtAltitude, ispAtAltitude, wdrValue);
        }

        public static void AtmosphereSetting ()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_common_planetSelect")))
            {
                showPlanetWindow = true;
            }
            GUILayout.Box(selectedPlanet);

            GUILayout.EndHorizontal();


            //var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == selectedPlanet);
            //if (planet != null)
            //{
            //    selectedPlanetMaxAltitude = (float)planet.atmosphereDepth;
            //    //selectedAltitude = selectedPlanetMaxAltitude;
            //}

            ////debug
            //GUILayout.Label(SEO_Functions.surfaceGravity.ToString());
            //GUILayout.Label(SEO_Functions.standardGravity.ToString());

            //if (selectedAltitude > selectedPlanetMaxAltitude)
            //{ selectedAltitude = selectedPlanetMaxAltitude; }
            //selectedAltitude = Mathf.Clamp(selectedAltitude, 0f, selectedPlanetMaxAltitude);
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_common_altitude") + (selectedAltitude / 1000).ToString("G3") + " km");
            //selectedAltitude = GUILayout.HorizontalSlider(selectedAltitude, 0f, 70000f, GUILayout.Width(200));
            selectedAltitude = GUILayout.HorizontalSlider(selectedAltitude, 0f, selectedPlanetMaxAltitude, GUILayout.Width(200));

            RefreshEngineParts();
        }

        public static void ToggleCareer ()
        {
            careerToggle = GUILayout.Toggle(careerToggle, Localizer.Format("#LOC_SHIPENGOP_common_careerToggle"));
        }

        //public static void OpenIndiWindow ()
        //{
        //    showIndiWindow = true;
        //}

        void LoadWdrFromConfig()
        {
            wdrValues.Clear();

            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("ShipEngineOp_massRatio");
            foreach (ConfigNode node in nodes)
            {
                foreach (ConfigNode wdrNode in node.GetNodes("mrFuel"))
                {
                    //string fuel = wdrNode.GetValue("fuel");
                    var fuel = wdrNode.GetValue("propellant").Split(',');
                    var sortedFuel = GetSortedFuelType(fuel);
                    if (float.TryParse(wdrNode.GetValue("mr"), out float value))
                    {
                        wdrValues[sortedFuel] = value;
                    }
                }
            }
        }

        static string GetSortedFuelType(IEnumerable<string> fuels)
        {
            return string.Join(", ", fuels.Select(f => f.Trim()).OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
        }

    }
}
