using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using KSP.UI.Screens;
using RealFuels;
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
        //public static bool showGrossSort = false;
        public static bool showPlanetWindow = false;
        public static bool showStageWindow = false;
        public static bool showStageEngWindow = false;
        public static bool showPropellantSetting = false;
        public static bool showEngineSetting = false;
        public static bool showTooltip = true;

        public const int indiWndWidth = 600;
        public const int indiWndHeight = 200;
        public const int dvWndWidth = 700;

        public static Rect mainWndRect = new Rect(600, 200, 250, 200);
        public static Rect indiWndRect = new Rect(400, 200, indiWndWidth, indiWndHeight);
        public static Rect dvWndRect = new Rect(600, 200, dvWndWidth, indiWndHeight);
        public static Rect numWndRect = new Rect(600, 200, dvWndWidth, indiWndHeight);
        public static Rect engineWndRect = new Rect(400, 400, 250, 300);
        public static Rect planetWndRect = new Rect(400, 400, 100, 100);
        public static Rect StageWndRect = new Rect(400, 200, 600, 300);
        public static Rect StageEngWndRect = new Rect(400, 400, 300, 600);
        public static Rect PropellantSettingWndRect = new Rect(400, 400, 300, 400);
        public static Rect EngineSettingWndRect = new Rect(400, 400, 500, 300);

        private ApplicationLauncherButton appButton;

        //static Texture2D bgTexture = new Texture2D(1, 1);
        bool initializedStyle = false;
        public static GUIStyle smallButton;
        public static GUIStyle engineNameBox;
        public static GUIStyle engineNameButton;
        public static GUIStyle labelWithBackground;
        public static GUIStyle rightLabel;
        public static GUIStyle boldStyle;

        void Start()
        {
            Debug.Log("[ShipEngineOp] Mod Initialized in Editor!");
            //LoadWdrFromConfig();
            LoadPropWdrFromConfig();
            LoadResourceDensities();
            //Debug.Log("LiquidFuel " + densities["LiquidFuel"].ToString());
            RefreshEngineParts();
            FinalizePropellantList();
            //InitializeEngineParts();
            //fuelTypes = groupedEngines.Keys.ToArray();

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
            if (!initializedStyle)
            {
                smallButton = new GUIStyle(GUI.skin.button);
                smallButton.fontSize = 10;
                smallButton.padding = new RectOffset(-10, -10, -10, -10);

                engineNameBox = new GUIStyle(GUI.skin.box); // Use the skin's box style as a base
                engineNameBox.alignment = TextAnchor.MiddleCenter;
                engineNameBox.wordWrap = true;

                engineNameButton = new GUIStyle(GUI.skin.button); // Use the skin's box style as a base
                engineNameButton.wordWrap = true;

                labelWithBackground = new GUIStyle(GUI.skin.label);
                labelWithBackground.alignment = TextAnchor.MiddleCenter;
                Texture2D bgTexture = new Texture2D(1, 1);
                bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.75f)); // Black with 75% transparency
                bgTexture.Apply();
                labelWithBackground.normal.background = bgTexture; // Set background texture

                rightLabel = new GUIStyle(GUI.skin.label);
                rightLabel.alignment = TextAnchor.MiddleRight;

                boldStyle = new GUIStyle(GUI.skin.label);
                boldStyle.alignment = TextAnchor.MiddleCenter;
                boldStyle.fontStyle = FontStyle.Bold;

                initializedStyle = true;
            }
            //smallButton.fontSize = 10;
            //smallButton.padding = new RectOffset(-10, -10, -10, -10);

            if (showMainWindow)
            {
                mainWndRect = GUILayout.Window(8880, mainWndRect, DrawMainWindow, Localizer.Format("#LOC_SHIPENGOP_mainWindowTitle"));
            }
            if (showIndiWindow)
            {
                indiWndRect = GUILayout.Window(8881, indiWndRect, SEO_GraphWindow.DrawIndiWindow, Localizer.Format("#LOC_SHIPENGOP_indiWindowTitle"));
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
                engineWndRect = GUILayout.Window(8884, engineWndRect, SEO_GraphWindow.DrawEngineWindow, Localizer.Format("#LOC_SHIPENGOP_engSelWindowTitle"));
            }
            //if (showGrossSort)
            //{
            //    grossSortWndRect = GUILayout.Window(9101, grossSortWndRect, SEO_GraphWindow.DrawGrossSort, Localizer.Format("#LOC_SHIPENGOP_grossCompTitle"));
            //}
            if (showPlanetWindow)
            {
                planetWndRect = GUILayout.Window(9102, planetWndRect, DrawplanetWindow, Localizer.Format("#LOC_SHIPENGOP_planetTitle"));
            }
            if (showStageWindow)
            {
                StageWndRect = GUILayout.Window(9013, StageWndRect, SEO_StageWindow.DrawStageWindow, Localizer.Format("#LOC_SHIPENGOP_stageWindowTitle"));
            }
            if (showStageEngWindow)
            {
                StageEngWndRect = GUILayout.Window(9015, StageEngWndRect, SEO_StageWindow.DrawStageEngineWindow, (Localizer.Format("#LOC_SHIPENGOP_stageWindow_stage") + SEO_StageWindow.stageList.IndexOf(SEO_StageWindow.selectedStageForEngine).ToString() + " " + Localizer.Format("#LOC_SHIPENGOP_stageEngineWindowTitle")));
            }
            if (showEngineSetting)
            {
                EngineSettingWndRect = GUILayout.Window(9019, EngineSettingWndRect, DrawEngineSetting, Localizer.Format("#LOC_SHIPENGOP_mainWindow_engineSetting"));
            }
            if (showPropellantSetting)
            {
                PropellantSettingWndRect = GUILayout.Window(9016, PropellantSettingWndRect, DrawPropellantSetting, "Propellant Setting");
            }
            if (showTooltip && tooltip != null && tooltip.Trim().Length > 0)
            {
                SetupTooltip();
                GUI.Window(1234, tooltipRect, TooltipWindow, "");
            }
        }

        public static float standardGravity = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
        public static float standardAtm = (float)FlightGlobals.getStaticPressure(0, Planetarium.fetch.Home);
        //static float selectedPlanetMaxAltitude = 70000f;

        //static float selectedAltitude = 70000f;
        //public static int indexEngine = 0;
        public static string emptyStr = Localizer.Format("#LOC_SHIPENGOP_empty");
        //public static string[] selectedEngine = new string[] { emptyStr, emptyStr, emptyStr };
        //public static bool[] customWdrBool = new bool[3];
        //public static bool[] additionalWeightBool = new bool[3];
        public static bool careerToggle = false;
        public static float payload = 10f;

        public static Dictionary<string, List<string>> groupedEngines = new Dictionary<string, List<string>>();
        public static List<string> blacklistEngines = new List<string>();
        //public static Dictionary<string, (float weight, float thrust, float isp, float wdr)> engineData = new Dictionary<string, (float, float, float, float)>();
        public static Dictionary<string, (float weight, float thrust, float isp, float wdr, string internalName, int modeIdx)> engineData = new Dictionary<string, (float, float, float, float, string, int)>(); // mode index: 0 = single mode; 1 = primary mode; 2 = secondary mode
        public static Dictionary<string, string> propellantDisplayNames = new Dictionary<string, string>();
        //private Vector2 scrollPosition_eng = Vector2.zero;
        //private Vector2 scrollPosition_fuel = Vector2.zero;

        public static IHasAtmData PlanetAtmClass;
        //static string selectedPlanet = "Kerbin";
        //private string[] selectedFuelType = { "None", "None", "None" };
        public static string[] fuelTypes;
        public static string tooltip = "";
        static int selectedFuelIndex;
        static Vector2 scrollPosition_fuel = Vector2.zero;
        static Vector2 scrollPosition_eng = Vector2.zero;
        static Vector2 scrollPosition_prop = Vector2.zero;

        public static Dictionary<string, (bool toggle, float addiWeight)> additionalWeight = new Dictionary<string, (bool, float)>()
        {
            {emptyStr, (false, 0f) }
        };


        public static Dictionary<string, float> resourceDensities = new Dictionary<string, float>();
        public static Dictionary<string, float> propellantWdr = new Dictionary<string, float>();
        //public static Dictionary<string, float> propellantWdr = new Dictionary<string, float>()
        //{
        //    { "LiquidFuel", 9f },
        //    { "Oxidizer", 9f },
        //    { "XenonGas", 4f },
        //    { "MonoPropellant", 5f },
        //    { "LqdHydrogen", 6f },
        //    { "LqdMethane", 6f }
        //};

        void DrawMainWindow(int windowID)
        {
            if (GUI.Button(new Rect(mainWndRect.size.x - 17, 2, 15, 15), "X", smallButton))
            {
                showMainWindow = false;
            }
            if (GUI.Button(new Rect(mainWndRect.size.x - 32, 2, 15, 15), new GUIContent("?", Localizer.Format("#LOC_SHIPENGOP_tooltips_toggleTooltips")), smallButton))
            {
                showTooltip = !showTooltip;
            }

            GUILayout.BeginVertical();

            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_mainWindow_stageConfig"), GUILayout.Height(100)))
            {
                showStageWindow = true;
            }

            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_mainWindow_engineSepc"), GUILayout.Height(100)))
            {
                showIndiWindow = true;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_mainWindow_engineSetting")))
            {
                showEngineSetting = true;
            }
            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_mainWindow_propellantSetting")))
            {
                showPropellantSetting = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            DrawToolTip();
            GUI.DragWindow();
        }

        void DrawEngineSetting(int windowID)
        {
            if (GUI.Button(new Rect(EngineSettingWndRect.size.x - 17, 2, 15, 15), "X", smallButton))
            {
                showEngineSetting = false;
            }
            if (GUI.Button(new Rect(EngineSettingWndRect.size.x - 32, 2, 15, 15), "?", smallButton))
            {
                showTooltip = !showTooltip;
            }

            string selectedFuelType = PropellantFilter();

            scrollPosition_eng = GUILayout.BeginScrollView(scrollPosition_eng, GUILayout.Width(500), GUILayout.Height(350));
            if (groupedEngines.ContainsKey(selectedFuelType))
            {
                foreach (var engine in groupedEngines[selectedFuelType])
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(engine, GUILayout.Width(240));

                    bool toggleBlacklist = GUILayout.Toggle(blacklistEngines.Contains(engine), Localizer.Format("#LOC_SHIPENGOP_settingWindow_blacklist"));
                    if (toggleBlacklist != blacklistEngines.Contains(engine))
                    {
                        if (toggleBlacklist)
                        { blacklistEngines.Add(engine); }
                        else
                        { blacklistEngines.Remove(engine); }
                        //if (blacklistEngines.Contains(engine))
                        //{ blacklistEngines.Remove(engine); }
                        //else
                        //{ blacklistEngines.Add(engine); }
                    }


                    bool newAddiWeightToggle = GUILayout.Toggle(additionalWeight[engine].toggle, Localizer.Format("#LOC_SHIPENGOP_indiWindow_addiWeightToggle"));
                    string newAddiweightStr = GUILayout.TextField(additionalWeight[engine].addiWeight.ToString(), 6, GUILayout.Width(60));
                    float.TryParse(newAddiweightStr, out float newAddiWeight);
                    if (newAddiWeightToggle != additionalWeight[engine].toggle || newAddiWeight != additionalWeight[engine].addiWeight)
                    {
                        additionalWeight[engine] = (newAddiWeightToggle, newAddiWeight);
                    }

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        void DrawPropellantSetting(int windowID)
        {
            if (GUI.Button(new Rect(PropellantSettingWndRect.size.x - 17, 2, 15, 15), ""))
            {
                showPropellantSetting = false;
            }

            var propellantNames = propellantWdr.Keys.ToList();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_settingWindow_propellant"));
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_settingWindow_massRatio"));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            scrollPosition_prop = GUILayout.BeginScrollView(scrollPosition_prop, GUILayout.Width(300), GUILayout.Height(400));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            foreach (var prop in propellantNames)
            {
                GUILayout.Label(PartResourceLibrary.Instance.GetDefinition(prop)?.displayName ?? prop);
                //GUILayout.Label(prop);
                //GUILayout.Label(propellantDisplayNames[prop.Key]);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            foreach (var prop in propellantNames)
            {
                string wdrString = GUILayout.TextField(propellantWdr[prop].ToString(), 6, GUILayout.Width(60)); // Input box
                if (float.TryParse(wdrString, out float propWdr))
                { propellantWdr[prop] = propWdr; }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_settingWindow_saveSetting")))
            { RefreshEngineParts(); }

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public static void DrawplanetWindow(int windowID)
        {
            if (GUI.Button(new Rect(planetWndRect.size.x - 17, 2, 15, 15), "X", smallButton))
            {
                showPlanetWindow = false;
            }

            string[] planetNames = FlightGlobals.Bodies.Where(b => b.atmosphere && !b.isStar).Select(b => b.bodyName).ToArray();

            foreach (var planetStr in planetNames)
            {
                if (GUILayout.Button(planetStr))
                {
                    PlanetAtmClass.SetPlanet(planetStr);
                    showPlanetWindow = false;
                }
            }

            GUI.DragWindow();
        }

        //Vector2 tooltipSize;
        float tooltipX, tooltipY;
        Rect tooltipRect;

        void SetupTooltip()
        {
            Vector2 mousePosition;
            mousePosition.x = Input.mousePosition.x;
            mousePosition.y = Screen.height - Input.mousePosition.y;
            //  Log.Info("SetupTooltip, tooltip: " + tooltip);
            //Debug.Log("SetupTooltip, tooltip: " + tooltip);
            if (tooltip != null && tooltip.Trim().Length > 0)
            {
                GUIStyle tooltipStyle = GUI.skin.customStyles[0];
                tooltipStyle.wordWrap = true;
                tooltipStyle.normal.textColor = Color.green;
                //tooltipStyle.fontSize = 11;
                tooltipStyle.padding = new RectOffset(10, 10, 10, 10); // Add some padding
                float maxTooltipWidth = 360f;
                float tooltipWidth = tooltipStyle.CalcSize(new GUIContent(tooltip)).x < maxTooltipWidth ? tooltipStyle.CalcSize(new GUIContent(tooltip)).x : maxTooltipWidth;
                //float tooltipWidth = HighLogic.Skin.label.CalcSize(new GUIContent(tooltip)).x < maxTooltipWidth ? HighLogic.Skin.label.CalcSize(new GUIContent(tooltip)).x : maxTooltipWidth;
                float tooltipHeight = tooltipStyle.CalcHeight(new GUIContent(tooltip), tooltipWidth);
                //tooltipSize = HighLogic.Skin.label.CalcSize(new GUIContent(tooltip));
                tooltipX = (mousePosition.x + tooltipWidth > Screen.width) ? (Screen.width - tooltipWidth) : mousePosition.x;
                tooltipY = mousePosition.y;
                if (tooltipX < 0) tooltipX = 0;
                if (tooltipY < 0) tooltipY = 0;
                tooltipRect = new Rect(tooltipX - 1, tooltipY - tooltipHeight, tooltipWidth + 4, tooltipHeight);
                //tooltipRect = new Rect(tooltipX - 1, tooltipY - tooltipSize.y, tooltipSize.x + 4, tooltipSize.y);
            }
        }

        void TooltipWindow(int id)
        {
            GUI.Label(new Rect(2, 0, tooltipRect.width - 2, tooltipRect.height), tooltip, GUI.skin.customStyles[0]);
        }

        public static void DrawToolTip()
        {
            tooltip = GUI.tooltip;
        }

        static void RefreshEngineParts()
        {
            bool realFuelsInstalled = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name.Equals("RealFuels", StringComparison.OrdinalIgnoreCase));

            var availableParts = PartLoader.LoadedPartsList;
            Debug.Log("activate refresh engine");
            engineData.Clear();
            groupedEngines.Clear();

            groupedEngines.Add("None", new List<string> { emptyStr });
            engineData[emptyStr] = (0, 0, 0, 1, "", 0);

            foreach (var part in availableParts)
            {
                if (careerToggle && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    if (!ResearchAndDevelopment.PartModelPurchased(part))
                        continue;
                }

                if (part.partPrefab == null)
                { continue; }
                else if (realFuelsInstalled)
                { FindEngineDataRF(part); }
                else
                {  FindEngineData(part); }
                //else if (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX"))
                //{
                //    if (part.partPrefab.Modules.Contains("MultiModeEngine"))
                //    {
                //        string engineName;
                //        var multiModeModule = part.partPrefab.Modules.GetModule<MultiModeEngine>();
                //        foreach (var engineModule in part.partPrefab.FindModulesImplementing<ModuleEngines>())
                //        {
                //            int modeIdx;
                //            if (engineModule.engineID == multiModeModule.primaryEngineID)
                //            {                              
                //                engineName = multiModeModule.primaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.primaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.primaryEngineID})";
                //                modeIdx = 1;
                //            }
                //            else
                //            {
                //                engineName = multiModeModule.secondaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.secondaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.secondaryEngineID})";
                //                modeIdx = 2;
                //            }
                //            SetEngineData(engineName, part.partPrefab.mass, engineModule, part.name, modeIdx);
                //        }
                //    }
                //    else
                //    { 
                //        var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
                //        if (engineModule != null)
                //        {
                //            SetEngineData(part.title, part.partPrefab.mass, engineModule, part.name, 0);
                //        }
                //    }
                //}
            }
        }

        static void FindEngineData(AvailablePart part)
        {
            if (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX"))
            {
                if (part.partPrefab.Modules.Contains("MultiModeEngine"))
                {
                    string engineName;
                    var multiModeModule = part.partPrefab.Modules.GetModule<MultiModeEngine>();
                    foreach (var engineModule in part.partPrefab.FindModulesImplementing<ModuleEngines>())
                    {
                        int modeIdx;
                        if (engineModule.engineID == multiModeModule.primaryEngineID)
                        {
                            engineName = multiModeModule.primaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.primaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.primaryEngineID})";
                            modeIdx = 1;
                        }
                        else
                        {
                            engineName = multiModeModule.secondaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.secondaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.secondaryEngineID})";
                            modeIdx = 2;
                        }
                        SetEngineData(engineName, part.partPrefab.mass, engineModule, part.name, modeIdx);
                    }
                }
                else
                {
                    var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
                    if (engineModule != null)
                    {
                        SetEngineData(part.title, part.partPrefab.mass, engineModule, part.name, 0);
                    }
                }
            }
        }

        static void FindEngineDataRF(AvailablePart part)
        {
            if (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX"))
            {
                if (part.partPrefab.Modules.Contains("MultiModeEngine"))
                {
                    string engineName;
                    var multiModeModule = part.partPrefab.Modules.GetModule<MultiModeEngine>();
                    foreach (var engineModule in part.partPrefab.FindModulesImplementing<ModuleEngines>())
                    {
                        int modeIdx;
                        if (engineModule.engineID == multiModeModule.primaryEngineID)
                        {
                            engineName = multiModeModule.primaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.primaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.primaryEngineID})";
                            modeIdx = 1;
                        }
                        else
                        {
                            engineName = multiModeModule.secondaryEngineModeDisplayName != null ? part.title + $" ({multiModeModule.secondaryEngineModeDisplayName})" : part.title + $" ({multiModeModule.secondaryEngineID})";
                            modeIdx = 2;
                        }
                        SetEngineData(engineName, part.partPrefab.mass, engineModule, part.name, modeIdx);
                    }
                }
                else
                {
                    var engineModule = part.partPrefab.Modules.GetModule<ModuleEngines>();
                    if (engineModule != null)
                    {
                        SetEngineData(part.title, part.partPrefab.mass, engineModule, part.name, 0);
                    }
                }
            }
            else if (part.partPrefab.Modules.Contains("ModuleEnginesRF"))
            {
                var engineModule = part.partPrefab.Modules.GetModule<ModuleEnginesRF>();
                if (engineModule != null)
                {
                    SetEngineDataRF(part.title, part.partPrefab.mass, engineModule, part.name, 0);
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

        static void SetEngineData (string name, float weight, ModuleEngines engineModule, string internalName, int modeIdx)
        {
            var propellantNames = GetSortedFuelType(engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge"));
            if (propellantNames.Contains("SolidFuel")) return;

            if (!groupedEngines.ContainsKey(propellantNames))
            {
                groupedEngines[propellantNames] = new List<string>();
                var localizedNames = engineModule.propellants
                    .Where(p => p.name != "ElectricCharge")
                    .Select(p => PartResourceLibrary.Instance.GetDefinition(p.name)?.displayName ?? p.name);
                propellantDisplayNames[propellantNames] = string.Join(Localizer.Format("#LOC_SHIPENGOP_joint"), localizedNames);
            }
            groupedEngines[propellantNames].Add(name);

            var propRatios = engineModule.propellants.Where(p => p.name != "ElectricCharge" && p.name != "IntakeAir").ToDictionary(p => p.name, p => p.ratio);
            float wdrTotal = GetWdrTotal(propRatios);

            engineData[name] = (weight, engineModule.maxThrust, engineModule.atmosphereCurve.Evaluate(0), wdrTotal, internalName, modeIdx);
            additionalWeight[name] = (false, 0);
        }

        static void SetEngineDataRF(string name, float weight, ModuleEnginesRF engineModule, string internalName, int modeIdx)
        {
            var propellantNames = GetSortedFuelType(engineModule.propellants.Select(p => p.name).Where(p => p != "ElectricCharge"));
            if (propellantNames.Contains("SolidFuel")) return;

            if (!groupedEngines.ContainsKey(propellantNames))
            {
                groupedEngines[propellantNames] = new List<string>();
                var localizedNames = engineModule.propellants
                    .Where(p => p.name != "ElectricCharge")
                    .Select(p => PartResourceLibrary.Instance.GetDefinition(p.name)?.displayName ?? p.name);
                propellantDisplayNames[propellantNames] = string.Join(Localizer.Format("#LOC_SHIPENGOP_joint"), localizedNames);
            }
            groupedEngines[propellantNames].Add(name);

            var propRatios = engineModule.propellants.Where(p => p.name != "ElectricCharge" && p.name != "IntakeAir").ToDictionary(p => p.name, p => p.ratio);
            float wdrTotal = GetWdrTotal(propRatios);

            engineData[name] = (weight, engineModule.maxThrust, engineModule.atmosphereCurve.Evaluate(0), wdrTotal, internalName, modeIdx);
            additionalWeight[name] = (false, 0);
        }

        static float GetWdrTotal(Dictionary<string, float> propRatios)
        {
            if (propRatios.Keys.Any(key => !propellantWdr.ContainsKey(key)))
            {
                var newPropellant = propRatios.Keys.Where(key => !propellantWdr.ContainsKey(key));
                foreach (var prop in newPropellant)
                { propellantWdr[prop] = 1f; }
                return 1f;
            }
            else if (propRatios.Select(p => propellantWdr[p.Key]).Any(wdr => wdr <= 1f))
            { return 1f; }
            else if (propRatios.Count == 1)
            { return propellantWdr[propRatios.First().Key]; }
            else
            {
                var propellantStats = propRatios.ToDictionary(p => p.Key, p => (ratio: p.Value, density: resourceDensities[p.Key], propWdr: propellantWdr[p.Key]));
                float totalPropMass = propellantStats.Sum(p => (p.Value.ratio * p.Value.density));
                float totalTankMass = propellantStats.Sum(p => (p.Value.ratio * p.Value.density / totalPropMass) / (1 - 1 / p.Value.propWdr));
                return 1 / (1 - 1 / totalTankMass);
            }
        }

        public static float DvLimit(float weight, float thrust, float isp, float wdr, float xTWR, float surfaceGravity)
        {
            float a = 1 / ((thrust / (weight * surfaceGravity * xTWR)) - 1);
            float b = isp * standardGravity * UnityEngine.Mathf.Log((1 + a) / ((1 / wdr) + a));

            return b;
        }

        public static void ToggleCareer()
        {
            bool newToggle = GUILayout.Toggle(careerToggle, Localizer.Format("#LOC_SHIPENGOP_common_careerToggle"));
            if (newToggle != careerToggle)
            {
                careerToggle = newToggle;
                //LoadPropWdrFromConfig();
                RefreshEngineParts();
                //FinalizePropellantList();
            }
        }

        public static string PropellantFilter()
        {
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engList_fuelSelect"));
            scrollPosition_fuel = GUILayout.BeginScrollView(scrollPosition_fuel, GUILayout.Width(300), GUILayout.Height(100));
            string[] fuelDisplayList = fuelTypes.Select(f => propellantDisplayNames.ContainsKey(f) ? propellantDisplayNames[f] : emptyStr).ToArray();
            selectedFuelIndex = GUILayout.SelectionGrid(selectedFuelIndex, fuelDisplayList, 1);
            string selectedFuelType = fuelTypes[selectedFuelIndex];
            GUILayout.EndScrollView();

            return selectedFuelType;
        }

        public static void PayloadInput()
        {
            GUILayout.Label(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_common_payloadMass"), Localizer.Format("#LOC_SHIPENGOP_tooltips_payloadMass")));
            GUILayout.BeginHorizontal();
            string vesselMassStr = GUILayout.TextField(payload.ToString(), 7, GUILayout.Width(60)); // Input box
            float.TryParse(vesselMassStr, out payload);

            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_common_loadMass")))
            {
                if (EditorLogic.fetch != null && EditorLogic.fetch.ship != null)
                {
                    payload = EditorLogic.fetch.ship.GetTotalMass();
                }
            }
            GUILayout.EndHorizontal();
        }

        static void FinalizePropellantList()
        {
            fuelTypes = groupedEngines.Keys.ToArray();
            var propellantKeys = propellantWdr.Keys.ToList();
            foreach (var prop in propellantKeys)
            {                
                if (fuelTypes.All(p => !p.Contains(prop)))
                {
                    //Debug.Log(prop);
                    propellantWdr.Remove(prop);
                }
            }
        }

        static void LoadPropWdrFromConfig()
        {
            propellantWdr.Clear();
            propellantWdr = GameDatabase.Instance.GetConfigNodes("SECODEFAULTMR")
                .Where(node => node.HasValue("propellant") && node.HasValue("ratio"))
                .GroupBy(node => node.GetValue("propellant")) // Group nodes by their name
                .ToDictionary(
                    group => group.Key, // The key for the dictionary is the name itself
                    group => {
                        // The value is the density of the *first* node in that group
                        // You could also choose group.Last(), or apply a custom logic
                        // if a resource is defined multiple times (e.g., take the highest density)
                        if (group.Count() > 1)
                        {
                            Debug.LogWarning($"[ShipEngineOp] Duplicate Propellant Mass Raio found for '{group.Key}'. Using the highest pritoritized one.");
                        }
                        Debug.Log($"[ShipEngineOp] Define Mass Raio found of '{group.Key}'");
                        return float.TryParse(group.OrderByDescending(p => int.TryParse(p.GetValue("priority"), out int pri) ? pri : 0).First().GetValue("ratio"), out float den) ? den : 0;
                        //return float.TryParse(group.First().GetValue("ratio"), out float den) ? den : 0;
                    }
                );

        }

        static string GetSortedFuelType(IEnumerable<string> fuels)
        {
            return string.Join(", ", fuels.Select(f => f.Trim()).OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
        }

        void LoadResourceDensities()
        {
            Debug.Log("[ShipEngineOp] Attempting to load RESOURCE_DEFINITION nodes safely.");

            // Group by name, then take the first one if duplicates exist.
            resourceDensities = GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION")
                .Where(node => node.HasValue("density") && node.HasValue("name"))
                .GroupBy(node => node.GetValue("name")) // Group nodes by their name
                .ToDictionary(
                    group => group.Key, // The key for the dictionary is the name itself
                    group => {
                        // The value is the density of the *first* node in that group
                        // You could also choose group.Last(), or apply a custom logic
                        // if a resource is defined multiple times (e.g., take the highest density)
                        if (group.Count() > 1)
                        {
                            Debug.LogWarning($"[ShipEngineOp] Duplicate RESOURCE_DEFINITION found for '{group.Key}'. Using the first one encountered.");
                        }
                        return float.TryParse(group.First().GetValue("density"), out float den) ? den : 0;
                    }
                );

            Debug.Log($"[ShipEngineOp] Successfully loaded {resourceDensities.Count} resource densities (handling duplicates).");
        }

        public static void GetAtm(IHasAtmData element)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(element.planetNameStr, Localizer.Format("#LOC_SHIPENGOP_tooltips_selectPlanet")), GUILayout.Width(80)))
            {
                Vector2 mousePosition = Vector2.zero;
                mousePosition.x = Input.mousePosition.x;
                mousePosition.y = Screen.height - Input.mousePosition.y;
                PlanetAtmClass = element;
                Vector2 offset = new Vector2(0.0f, -50f);
                planetWndRect.position = mousePosition + offset;
                //planetWndRect.position = StageWndRect.position + Event.current.mousePosition + offset;
                showPlanetWindow = true;
            }

            var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == element.planetNameStr);

            float maxAltitude = (float)planet.atmosphereDepth;

            element.altitude = Mathf.Clamp(element.altitude, 0, maxAltitude);
            float processedAlt = GUILayout.HorizontalSlider(Mathf.Sqrt(element.altitude), 0f, Mathf.Sqrt(maxAltitude), GUILayout.Width(80));
            element.altitude = processedAlt * processedAlt;

            GUILayout.Label((element.altitude / 1000).ToString("G3") + " km");
            GUILayout.EndHorizontal();
        }
    }

    public interface IHasAtmData
    {
        //string engineName { get; set; }
        float altitude { get; set; }
        string planetNameStr { get; set; }
        void SetPlanet(string planetInput);
    }
}
