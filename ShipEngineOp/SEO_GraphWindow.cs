using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;


namespace ShipEngineOptimization
{
    public class SEO_GraphWindow
    {
        static float xMax_dvGraph = 2.0f;
        static List<Color> colorSequence = new List<Color> { Color.green, Color.blue, Color.red, Color.yellow, Color.magenta, Color.cyan };
        static float[] globalDvRange = new float[2];
        static float[] globalTwrRange = new float[2]; // [0]=min [1]=max
        static float[] trueXRange = new float[2];
        static float[] trueYRange = new float[2];
        const int num_pts = 200;

        //public static float payload = 10f;

        public const int graph_width = 450;
        public const int graph_height = 350;
        public const int graph_left = 20;
        public const int graph_right = 30;
        public static int graph_top = 20;
        public const int graph_bottom = 20;
        static Vector2 graphCorner = new Vector2(10, 30);
        static Texture2D dvGraph_tex = new Texture2D(graph_width, graph_height, TextureFormat.ARGB32, false);
        static Texture2D numGraph_tex = new Texture2D(graph_width, graph_height, TextureFormat.ARGB32, false);
        //static Texture2D bgTexture = new Texture2D(1, 1);

        //static int selectedFuelIndex;
        static List<EngineStats> EngineLists = new List<EngineStats>();
        static EngineStats selectedEngine;

        static int constrainOp = 0; // 0 = TWR as x axis; 1 = dv as x axis
        static int graphOption = 0; // 0 = dv critical; 1 = dv limit
        public static string[] constrainOpStr = { Localizer.Format("#LOC_SHIPENGOP_common_constrainDv"), Localizer.Format("#LOC_SHIPENGOP_common_constrainTwr") };
        public static GUIContent[] graphOptionStr = new GUIContent[]
            {
                new GUIContent(Localizer.Format("#LOC_SHIPENGOP_dvLimitWindow_dvCritical"), Localizer.Format("#LOC_SHIPENGOP_tooltips_dvCritical")),
                new GUIContent(Localizer.Format("#LOC_SHIPENGOP_dvLimitWindow_dvLimit"), Localizer.Format("#LOC_SHIPENGOP_tooltips_dvLimit"))
            };
        //public static string[] graphOptionStr = { Localizer.Format("#LOC_SHIPENGOP_dvLimitWindow_dvCritical"), Localizer.Format("#LOC_SHIPENGOP_dvLimitWindow_dvLimit") };
        //static bool constrainDv = true;
        public static bool linkAtm = false;
        static Vector2 scrollPosEngineView = Vector2.zero;

        //public static bool autoUpdate = true;

        public static void DrawIndiWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.indiWndRect.size.x - 17, 2, 15, 15), "X", SEO_Main.smallButton))
            {
                SEO_Main.showIndiWindow = false;
            }
            if (GUI.Button(new Rect(SEO_Main.indiWndRect.size.x - 32, 2, 15, 15), "?", SEO_Main.smallButton))
            {
                SEO_Main.showTooltip = !SEO_Main.showTooltip;
            }

            scrollPosEngineView = GUILayout.BeginScrollView(scrollPosEngineView, GUILayout.Width(600), GUILayout.Height(300));
            GUILayout.BeginHorizontal();

            //if (EngineLists.Count == 0)
            //{
            //    EngineLists.Add(new EngineStats());
            //}

            //GUIStyle boldStyle = new GUIStyle(GUI.skin.label);
            //boldStyle.alignment = TextAnchor.MiddleCenter;
            //boldStyle.fontStyle = FontStyle.Bold;

            if (EngineLists.Count != 0)
            {
                foreach (EngineStats engine in EngineLists)
                {
                    if (SEO_Main.careerToggle)
                    {
                        if (!SEO_Main.engineData.Keys.Contains(engine.engineName))
                        {
                            EngineLists.Remove(engine);
                            break;
                        }
                    }
                    else if (engine.engineName == SEO_Main.emptyStr && EngineLists.Count() > 1)
                    {
                        EngineLists.Remove(engine);
                        break;
                    }

                    if (linkAtm)
                    {
                        if (EngineLists.IndexOf(engine) > 0)
                        {
                            engine.SetPlanet(EngineLists.FirstOrDefault().planetNameStr);
                            engine.altitude = EngineLists.FirstOrDefault().altitude;
                        }
                    }

                    GUILayout.BeginVertical(GUILayout.Width(240));
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        EngineLists.Remove(engine);
                        break;
                    }
                    if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_engine") + " " + EngineLists.IndexOf(engine).ToString()))
                    {
                        SEO_Main.showEngineList = true;
                        selectedEngine = engine;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Box(engine.engineName, SEO_Main.engineNameBox, GUILayout.Width(240));

                    //GUILayout.BeginHorizontal();
                    if (engine.engineName != SEO_Main.emptyStr)
                    {
                        GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_basicStats"), SEO_Main.boldStyle);
                        GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_weight") + ": " + engine.effctiveWeight.ToString() + " " + Localizer.Format("#LOC_SHIPENGOP_units_tons"));
                        GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_thrust") + ": " + engine.realThrust.ToString("F1") + " " + Localizer.Format("#LOC_SHIPENGOP_units_kiloNewton"));
                        GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_isp") + ": " + engine.realIsp.ToString("F0") + " " + Localizer.Format("#LOC_SHIPENGOP_units_seconds"));
                        GUILayout.Label(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_engSpecs_wdr") + ": " + engine.wdr.ToString("G3"), Localizer.Format("#LOC_SHIPENGOP_tooltips_massRatio")));

                        GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engSpecs_optimalOperatingStats"), SEO_Main.boldStyle);
                        GUILayout.Label(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_engSpecs_optimalDv") + ": " + engine.GetOptimalDv().ToString("F0") + " " + Localizer.Format("#LOC_SHIPENGOP_units_metersPerSecond"), Localizer.Format("#LOC_SHIPENGOP_tooltips_optimalDv")));
                        GUILayout.Label(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_engSpecs_optimalTwr") + ": " + engine.GetOptimalTwr().ToString("F2"), Localizer.Format("#LOC_SHIPENGOP_tooltips_optimalTwr")));
                    }

                    GUILayout.Space(5);
                    SEO_Main.GetAtm(engine);

                    //if (GUILayout.Button("-", GUILayout.Width(240)))
                    //{
                    //    EngineLists.Remove(engine);
                    //    break;
                    //}

                    if (engine.engineName != SEO_Main.emptyStr)
                    { engine.UpdateEngineData(); }

                    GUILayout.EndVertical();
                }
            }
            
            if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_engine") + " " + EngineLists.Count(), GUILayout.Width(240)))
            {
                EngineStats newEngine = new EngineStats();
                selectedEngine = newEngine;
                SEO_Main.showEngineList = true;
                //EngineLists.Add(new EngineStats());
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
            linkAtm = GUILayout.Toggle(linkAtm, Localizer.Format("#LOC_SHIPENGOP_indiWindow_linkAtm"));
            SEO_Main.ToggleCareer();
                    GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_indiWindow_dvLimitCurce"), Localizer.Format("#LOC_SHIPENGOP_tooltips_twr-dvGraph"))))
            {
                SEO_Main.showDvLimit = true;
            }
            if (GUILayout.Button(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_indiWindow_enginePerfChart"), Localizer.Format("#LOC_SHIPENGOP_tooltips_engineConfigChart"))))
            {
                SEO_Main.showNumEng = true;
            }
            GUILayout.EndHorizontal();

                GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            SEO_Main.DrawToolTip();

            GUI.DragWindow();
        }

        public static void DrawEngineWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.engineWndRect.size.x - 17, 2, 15, 15), "X", SEO_Main.smallButton))
            {
                SEO_Main.showEngineList = false;
            }
            if (GUI.Button(new Rect(SEO_Main.engineWndRect.size.x - 32, 2, 15, 15), new GUIContent("?", Localizer.Format("#LOC_SHIPENGOP_tooltips_toggleTooltips")), SEO_Main.smallButton))
            {
                SEO_Main.showTooltip = !SEO_Main.showTooltip;
            }

            string selectedFuelType = SEO_Main.PropellantFilter();
            //GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engList_fuelSelect"));
            //scrollPosition_fuel = GUILayout.BeginScrollView(scrollPosition_fuel, GUILayout.Width(300), GUILayout.Height(100));
            //string[] fuelDisplayList = SEO_Main.fuelTypes.Select(f => SEO_Main.propellantDisplayNames.ContainsKey(f) ? SEO_Main.propellantDisplayNames[f] : SEO_Main.emptyStr).ToArray();
            //selectedFuelIndex = GUILayout.SelectionGrid(selectedFuelIndex, fuelDisplayList, 1);
            //string selectedFuelType = SEO_Main.fuelTypes[selectedFuelIndex];
            //GUILayout.EndScrollView();

            GUILayout.Space(20); // Add spacing before the separation line
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2)); // Separation line
            GUILayout.Space(10); // Add spacing after the separation line

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engList_engineNumSelect") + (EngineLists.Contains(selectedEngine) ? EngineLists.IndexOf(selectedEngine).ToString() : EngineLists.Count().ToString()));
            scrollPosition_eng = GUILayout.BeginScrollView(scrollPosition_eng, GUILayout.Width(300), GUILayout.Height(350));

            if (SEO_Main.groupedEngines.ContainsKey(selectedFuelType))
            {
                foreach (var engine in SEO_Main.groupedEngines[selectedFuelType])
                {
                    if (GUILayout.Button(engine))
                    {
                        selectedEngine.SetEngineData(engine);
                        if (engine != SEO_Main.emptyStr)
                        { selectedEngine.UpdateEngineData();}
                        if (!EngineLists.Contains(selectedEngine))
                        { EngineLists.Add(selectedEngine); }
                        SEO_Main.showEngineList = false;
                    }
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        public static void DrawDvLimitWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 17, 2, 15, 15), "X", SEO_Main.smallButton))
            {
                SEO_Main.showDvLimit = false;
            }
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 32, 2, 15, 15), "?", SEO_Main.smallButton))
            {
                SEO_Main.showTooltip = !SEO_Main.showTooltip;
            }

            if (EngineLists.All(e => e.engineName == SEO_Main.emptyStr))
            {
                SEO_Main.showDvLimit = false;
                return;
            }

            GUILayout.BeginHorizontal();
            // column 1
            GUILayout.BeginVertical(GUILayout.Width(graph_width + 15));
            GUILayout.Space(5);
            GUILayout.Label(dvGraph_tex);

            UpdateDvGraphs();

            GUILayout.EndVertical();

            //column 2
            GUILayout.BeginVertical();

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_dvLimitWindow_graphOption"));
            graphOption = GUILayout.SelectionGrid(graphOption, graphOptionStr, 2);

            //GUIStyle engineNameBox = new GUIStyle(GUI.skin.box); // Use the skin's box style as a base
            //engineNameBox.alignment = TextAnchor.MiddleCenter;
            //engineNameBox.wordWrap = true;

            foreach (var engine in EngineLists)
            {
                GUI.color = colorSequence[EngineLists.IndexOf(engine) % colorSequence.Count()];
                GUILayout.Box(engine.engineName, SEO_Main.engineNameBox, GUILayout.Width(200));
            }

            //for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            //{
            //    GUI.color = colorSequence[idx];
            //    GUILayout.Box(SEO_Main.selectedEngine[idx], GUILayout.Width(200));
            //}

            GUI.color = Color.white;

            GUILayout.Space(10);

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_dvLimitWindow_twrLimit"));

            UpdateTwrRange();
            GUILayout.BeginHorizontal();
            xMax_dvGraph = GUILayout.HorizontalSlider(xMax_dvGraph, 0, globalTwrRange[1]);
            GUILayout.Label(xMax_dvGraph.ToString("F2"));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            SEO_Main.DrawToolTip();
            GUI.DragWindow();
        }


        public static void UpdateDvGraphs()
        {
            init_textures();
            init_axes();

            //engineIndexes = new List<int> { EngDrawMain.selectedPartIndex0, EngDrawMain.selectedPartIndex1, EngDrawMain.selectedPartIndex2 };
            UpdateDvRange();

            float[] x_net = new float[num_pts + 1];
            float[] y_net = new float[num_pts + 1];

            // build x net
            for (int i = 0; i <= num_pts; i++)
            {
                float x = i;
                x_net[i] = x / num_pts * xMax_dvGraph;
            }

            foreach (var engine in EngineLists)
            {
                if (graphOption == 0)
                { y_net = engine.CalcCriticalDv(x_net); }
                else
                { y_net = engine.CalcDvLimit(x_net); }
                //y_net = engine.CalcDvLimit(x_net);
                DrawCurveGraph(x_net, y_net, dvGraph_tex, colorSequence[EngineLists.IndexOf(engine) % colorSequence.Count()]);
            }

            //for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            //{
            //    y_net = CalDvLimit(x_net, SEO_Main.selectedEngine[idx]);
            //    DrawCurveGraph(x_net, y_net, dvGraph_tex, colorSequence[idx]);
            //}

            //GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.label);
            //centeredTextStyle.alignment = TextAnchor.MiddleCenter;

            // Tick generation
            List<float> xTicks = TickGen(0.0f, xMax_dvGraph);
            foreach (float xTick in xTicks)
            {
                int xPos = Mathf.RoundToInt(xTick / xMax_dvGraph * (graph_width - graph_left - graph_right - 1) + graph_left);
                DrawLine(dvGraph_tex, xPos, graph_bottom, xPos, graph_bottom + 4, Color.white);
                DrawLine(dvGraph_tex, xPos, graph_height - graph_top - 1, xPos, graph_height - graph_top - 5, Color.white);
                GUI.Label(new Rect(graphCorner.x + xPos - 14, graphCorner.y + graph_height - graph_bottom - 2, 31, 20), xTick.ToString(), SEO_Main.labelWithBackground);
            }
            List<float> yTicks = TickGen(0.0f, globalDvRange[1]);
            foreach (float yTick in yTicks)
            {
                int yPos = Mathf.RoundToInt(yTick / globalDvRange[1] * (graph_height - graph_top - graph_bottom - 1) + graph_bottom);
                DrawLine(dvGraph_tex, graph_width - graph_right - 1, yPos, graph_width - graph_right - 5, yPos, Color.white);
                DrawLine(dvGraph_tex, graph_left, yPos, graph_left + 4, yPos, Color.white);
                GUI.Label(new Rect(graphCorner.x + graph_width - graph_right + 3, graphCorner.y + graph_height - 13 - yPos, 40, 20), (yTick / 1000).ToString());
            }

            //bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.75f)); // Black with 75% transparency
            //bgTexture.Apply();

            //SEO_Main.centeredTextStyle.normal.background = bgTexture; // Set background texture

            // Labels
            GUI.Label(new Rect(graphCorner.x + graph_left - 3, graphCorner.y - 1, 40, 20), "(m/s)");
            GUI.Label(new Rect(graphCorner.x + graph_width - graph_right - 31, graphCorner.y - 1, 100, 20), "dV (km/s)");
            GUI.Label(new Rect(graphCorner.x + graph_width - graph_right - 21, graphCorner.y + graph_height - graph_bottom - 2, 36, 20), "TWR", SEO_Main.labelWithBackground);
            //GUI.Label(new Rect(graphCorner.x + graph_width - graph_right + 1, graphCorner.y + graph_height - graph_bottom - 2, 40, 20), "TWR");

            //mouse crossing
            Vector2 mousePos = Event.current.mousePosition;
            int mouseLineX = (int)Mathf.Round(mousePos.x - graphCorner.x);
            int mouseLineY = (int)Mathf.Round(graph_height - 1 - (mousePos.y - graphCorner.y));

            if ((mouseLineX >= graph_left && mouseLineX <= (graph_width - graph_right - 1)) && (mouseLineY >= (graph_bottom) && mouseLineY <= (graph_height - graph_top - 1)))
            {

                DrawLine(dvGraph_tex, mouseLineX, graph_bottom, mouseLineX, graph_height - graph_top - 1, Color.gray);
                DrawLine(dvGraph_tex, graph_left, mouseLineY, graph_width - graph_right - 1, mouseLineY, Color.gray);

                float xValue = ((float)(mouseLineX - graph_left) / (float)(graph_width - graph_left - graph_right - 1)) * xMax_dvGraph;
                float yValue = ((float)(mouseLineY - graph_bottom) / (float)(graph_height - graph_bottom - graph_top - 1)) * globalDvRange[1];
                
                float xLabel;
                if (mousePos.x <= graphCorner.x + graph_width - 30 - graph_right - 5)
                {
                    xLabel = mousePos.x + 5;
                }
                else
                {
                    xLabel = graphCorner.x + graph_width - 30 - graph_right;
                }
                float yLabel;
                if (mousePos.y >= graphCorner.y + 1 + graph_top + 24)
                {
                    yLabel = mousePos.y - 24;
                }
                else
                {
                    yLabel = graphCorner.y + 1 + graph_top;
                }

                GUI.Label(new Rect(xLabel, graphCorner.y + graph_height - graph_bottom - 24, 100, 20), xValue.ToString("F2"));
                GUI.Label(new Rect(graphCorner.x + graph_left + 5, yLabel, 100, 20), yValue.ToString("F0"));
            }

            //if (updateGraph) dvGraph_tex.Apply();

            dvGraph_tex.Apply();
        }

        static float dvTarget = 2400;
        static float twrTarget =1f;

        public static void DrawNumEngWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 17, 2, 15, 15), "X", SEO_Main.smallButton))
            {
                SEO_Main.showNumEng = false;
            }
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 32, 2, 15, 15), "?", SEO_Main.smallButton))
            {
                SEO_Main.showTooltip = !SEO_Main.showTooltip;
            }

            if (EngineLists.All(e => e.engineName == SEO_Main.emptyStr) || twrTarget <= 0)
            {
                SEO_Main.showNumEng = false;
                return;
            }

            GUILayout.BeginHorizontal();
            // column 1
            GUILayout.BeginVertical(GUILayout.Width(graph_width + 15));
            GUILayout.Space(5);
            GUILayout.Label(numGraph_tex);

            UpdateNumEngGraphs();

            GUILayout.EndVertical();

            //column 2
            GUILayout.BeginVertical();

            foreach (var engine in EngineLists)
            {
                GUI.color = colorSequence[EngineLists.IndexOf(engine) % colorSequence.Count()];
                GUILayout.Box(engine.engineName, GUILayout.Width(200));
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engQtyWindow_qtyMin") + engine.engQtyMin.ToString());
                engine.engQtyMin = Mathf.RoundToInt(GUILayout.HorizontalSlider(engine.engQtyMin, 1, engine.engQtyMax, GUILayout.Width(135)));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engQtyWindow_qtyMax") + engine.engQtyMax.ToString());
                engine.engQtyMax = Mathf.RoundToInt(GUILayout.HorizontalSlider(engine.engQtyMax, engine.engQtyMin, 21, GUILayout.Width(135)));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            GUILayout.Space(10);

            SEO_Main.PayloadInput();

            SelectConstrain();

            if (constrainOp == 0)
            {
                UpdateDvRange();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engQtyWindow_targetDv") + dvTarget.ToString("F0") + " " + Localizer.Format("#LOC_SHIPENGOP_units_metersPerSecond"));
                dvTarget = GUILayout.HorizontalSlider(dvTarget, 0, globalDvRange[0]);
            }
            else if (constrainOp == 1)
            {
                UpdateTwrRange();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engQtyWindow_targetTwr") + twrTarget.ToString("F2"));
                twrTarget = GUILayout.HorizontalSlider(twrTarget, 0, globalTwrRange[0]);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        public static void UpdateNumEngGraphs()
        {
            init_textures();
            init_axes();

            foreach (var engine in EngineLists)
            {
                engine.engQtyNet = new int[engine.engQtyMax - engine.engQtyMin + 1];
                engine.allPtsTotalWeight = new float[engine.engQtyMax - engine.engQtyMin + 1];
                engine.allPtsX = new float[engine.engQtyMax - engine.engQtyMin + 1];

                for (int i = 0; i < engine.engQtyNet.Length; i++)
                {
                    engine.engQtyNet[i] = i + engine.engQtyMin;
                }
                switch (constrainOp)
                {
                    case 0: // set dv
                        if (engine.wdr > 1.0f)
                        {
                            engine.CalcTotalWeightAndTwrFromDv(dvTarget, SEO_Main.payload); 
                        }
                        break;
                    case 1: // set TWR
                        if (engine.wdr > 1.0f && engine.realThrust / (engine.surfaceGravity * engine.effctiveWeight) > twrTarget)
                        {
                            engine.CalcTotalWeightAndTwrFromDv(twrTarget, SEO_Main.payload);
                        }
                        break;
                }
            }

            float[] xRange_numGraph = new float[2];
            float[] yRange_numGraph = new float[2];

            xRange_numGraph[0] = EngineLists
                .Select(e => e.allPtsX)
                .SelectMany(array => array)
                .Where(x => x > 0.0f)
                .DefaultIfEmpty()
                .Min();
            xRange_numGraph[1] = EngineLists
                .Select(e => e.allPtsX)
                .SelectMany(array => array)
                .Where(x => x > 0.0f)
                .DefaultIfEmpty()
                .Max();
            yRange_numGraph[0] = EngineLists
                .Select(e => e.allPtsTotalWeight)
                .SelectMany(array => array)
                .Where(x => x > 0.0f)
                .DefaultIfEmpty()
                .Min();
            yRange_numGraph[1] = EngineLists
                .Select(e => e.allPtsTotalWeight)
                .SelectMany(array => array)
                .Where(x => x > 0.0f)
                .DefaultIfEmpty()
                .Max();

            if (xRange_numGraph[0] == xRange_numGraph[1])
                xRange_numGraph[0] = 0;
            if (yRange_numGraph[0] == yRange_numGraph[1])
                yRange_numGraph[0] = 0;

            //twrDebug = allPtsX[1];
            //WeightDebug = allPtsTotalWeight[1];

            trueXRange[0] = xRange_numGraph[0] - (xRange_numGraph[1] - xRange_numGraph[0]) * 0.125f;
            trueXRange[1] = xRange_numGraph[1] + (xRange_numGraph[1] - xRange_numGraph[0]) * 0.125f;
            trueYRange[0] = yRange_numGraph[0] - (yRange_numGraph[1] - yRange_numGraph[0]) * 0.125f;
            trueYRange[1] = yRange_numGraph[1] + (yRange_numGraph[1] - yRange_numGraph[0]) * 0.125f;


            // Tick generation
            List<float> xTicks = TickGen(trueXRange[0], trueXRange[1]);
            foreach (float xTick in xTicks)
            {
                int xPos = Mathf.RoundToInt((xTick - trueXRange[0]) / (trueXRange[1] - trueXRange[0]) * (graph_width - graph_left - graph_right - 1) + graph_left);
                DrawLine(numGraph_tex, xPos, graph_bottom, xPos, graph_bottom + 4, Color.white);
                DrawLine(numGraph_tex, xPos, graph_height - graph_top - 1, xPos, graph_height - graph_top - 5, Color.white);
                //GUI.Label(new Rect(graphCorner.x + xPos, graphCorner.y + graph_height - graph_bottom - 2, 40, 20), xTick.ToString());
                GUI.Label(new Rect(graphCorner.x + xPos - 14, graphCorner.y + graph_height - graph_bottom - 2, 31, 20), xTick.ToString(constrainOp == 0 ? "G2" : "F0"), SEO_Main.labelWithBackground);
            }
            List<float> yTicks = TickGen(trueYRange[0], trueYRange[1]);
            foreach (float yTick in yTicks)
            {
                int yPos = Mathf.RoundToInt((yTick - trueYRange[0]) / (trueYRange[1] - trueYRange[0]) * (graph_height - graph_top - graph_bottom - 1) + graph_bottom);
                DrawLine(numGraph_tex, graph_width - graph_right - 1, yPos, graph_width - graph_right - 5, yPos, Color.white);
                DrawLine(numGraph_tex, graph_left, yPos, graph_left + 4, yPos, Color.white);
                GUI.Label(new Rect(graphCorner.x + graph_width - graph_right + 3, graphCorner.y + graph_height - 13 - yPos, 40, 20), yTick.ToString("G3"));
            }

            // Labels
            string xLable = "TWR";
            float xLableWidth = 36;
            switch (constrainOp)
            {
                case 1:
                    xLable = "dv (m/s)";
                    xLableWidth = 60;
                    break;
                default:
                    xLable = "TWR";
                    xLableWidth = 36;
                    break;
            }
            GUI.Label(new Rect(graphCorner.x + graph_width - graph_right - 61, graphCorner.y - 1, 100, 20), "Total Weight (t)");
            GUI.Label(new Rect(graphCorner.x + graph_width - graph_right -21, graphCorner.y + graph_height - graph_bottom - 2, xLableWidth, 20), xLable, SEO_Main.labelWithBackground);

            // Highlight closest
            Vector2? closestValues = null;
            Vector2? closestPoint = null;
            string closestEngineName = SEO_Main.emptyStr;
            int closestEngQty = 0;

            float closestDistance = float.MaxValue;
            Vector2 mousePos = Event.current.mousePosition;

            // labels text style
            //GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.label);
            //centeredTextStyle.alignment = TextAnchor.MiddleCenter;

            //bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.3f)); // Black with 50% transparency
            //bgTexture.Apply();

            //centeredTextStyle.normal.background = bgTexture; // Set background texture

            foreach (var engine in EngineLists)
            {
                for (int i = 0; i < engine.allPtsX.Length; i++)
                {
                    float xPos = graphCorner.x + Mathf.RoundToInt(graph_left + (engine.allPtsX[i] - trueXRange[0]) / (trueXRange[1] - trueXRange[0]) * (graph_width - graph_left - graph_right - 1));
                    float yPos = graphCorner.y + graph_height - Mathf.RoundToInt(graph_bottom + (engine.allPtsTotalWeight[i] - trueYRange[0]) / (trueYRange[1] - trueYRange[0]) * (graph_height - graph_bottom - graph_top - 1));

                    float distance = Vector2.Distance(new Vector2(xPos, yPos), mousePos);
                    if (distance < closestDistance)
                    {
                        closestEngineName = engine.engineName;
                        closestDistance = distance;
                        closestValues = new Vector2(engine.allPtsX[i], engine.allPtsTotalWeight[i]);
                        closestPoint = new Vector2(xPos, yPos);
                        closestEngQty = engine.engQtyNet[i];
                    }

                    // labeling numbers
                    GUI.Label(new Rect(xPos - 8, yPos - 11, 17, 17), engine.engQtyNet[i].ToString(), SEO_Main.labelWithBackground);
                }
            }

            if (closestValues.HasValue && (mousePos.x >= (graphCorner.x + graph_left) && mousePos.x <= ( graphCorner.x + graph_width - graph_right - 1)) && (mousePos.y >= (graphCorner.y + graph_top) && mousePos.y <= (graphCorner.y + graph_height - graph_bottom - 1)))
            {
                int xPos = Mathf.RoundToInt(graph_left + (closestValues.Value.x - trueXRange[0]) / (trueXRange[1] - trueXRange[0]) * (graph_width - graph_left - graph_right - 1));
                int yPos = Mathf.RoundToInt(graph_bottom + (closestValues.Value.y - trueYRange[0]) / (trueYRange[1] - trueYRange[0]) * (graph_height - graph_bottom - graph_top - 1));
                DrawDot(numGraph_tex, xPos, yPos, 20, Color.white);

                float midPointCheck = graphCorner.y + (graph_height - graph_bottom - graph_top) / 2 + graph_top - 1;
                GUI.Box(new Rect(closestPoint.Value.x - 70,
                        mousePos.y <= midPointCheck ? (closestPoint.Value.y + 15) : (closestPoint.Value.y - 80), 140, 70),
                        closestEngineName
                        + "\n"
                        + (constrainOp == 0
                        ? (Localizer.Format("#LOC_SHIPENGOP_shipSpecs_twr") + closestValues.Value.x.ToString("F2"))
                        : (Localizer.Format("#LOC_SHIPENGOP_shipSpecs_dv") + closestValues.Value.x.ToString("F0") + " m/s"))
                        + "\n"
                        + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass")
                        + closestValues.Value.y.ToString("G3")
                        + " t"
                        + "\n"
                        + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty")
                        + closestEngQty.ToString());
            }

            foreach (var engine in EngineLists)
            {
                DrawDotGraph(engine.allPtsX, engine.allPtsTotalWeight, colorSequence[EngineLists.IndexOf(engine) % colorSequence.Count()]);
            }

            numGraph_tex.Apply();
        }

        private static HashSet<string> selectedTopFuelTypes = new HashSet<string>();
        private static Vector2 scrollPosition_eng = Vector2.zero;

        static void init_textures(bool apply = false)
        {
            var fillcolor = Color.black;
            var arr = dvGraph_tex.GetPixels();
            for (int i = 0; i < arr.Length; i++)
                arr[i] = fillcolor;
            dvGraph_tex.SetPixels(arr);
            numGraph_tex.SetPixels(arr);
            if (apply)
            {
                dvGraph_tex.Apply();
                numGraph_tex.Apply();
            }
        }

        static void init_axes()
        {
            // bounding box
            int x0 = graph_left;
            int y0 = graph_bottom;
            int x1 = graph_width - graph_right - 1;
            int y1 = y0;
            DrawLine(dvGraph_tex, x0, y0, x1, y1, Color.white);
            DrawLine(numGraph_tex, x0, y0, x1, y1, Color.white);

            x0 = graph_left;
            y0 = graph_bottom;
            x1 = x0;
            y1 = graph_height - graph_top - 1;
            DrawLine(dvGraph_tex, x0, y0, x1, y1, Color.white);
            DrawLine(numGraph_tex, x0, y0, x1, y1, Color.white);

            x0 = graph_width - graph_right - 1;
            y0 = graph_bottom;
            x1 = x0;
            y1 = graph_height - graph_top - 1;
            DrawLine(dvGraph_tex, x0, y0, x1, y1, Color.white);
            DrawLine(numGraph_tex, x0, y0, x1, y1, Color.white);

            x0 = graph_left;
            y0 = graph_height - graph_top - 1;
            x1 = graph_width - graph_right - 1;
            y1 = y0;
            DrawLine(dvGraph_tex, x0, y0, x1, y1, Color.white);
            DrawLine(numGraph_tex, x0, y0, x1, y1, Color.white);

        }

        static void DrawCurveGraph(float[] x, float[] y, Texture2D tex, Color color)
        {
            for (int i = 0; i < num_pts; i++)
            {
                if (y[i + 1] >= 0)
                {
                    int x0 = Mathf.RoundToInt(graph_left + (graph_width - graph_left - graph_right - 1 ) * (x[i] / x[num_pts]));
                    int x1 = Mathf.RoundToInt(graph_left + (graph_width - graph_left - graph_right - 1) * (x[i + 1] / x[num_pts]));
                    int y0 = Mathf.RoundToInt(graph_bottom + (graph_height - graph_bottom - graph_top - 1 ) * (y[i] / globalDvRange[1]));
                    int y1 = Mathf.RoundToInt(graph_bottom + (graph_height - graph_bottom - graph_top - 1 ) * (y[i + 1] / globalDvRange[1]));
                    DrawLine(tex, x0, y0, x1, y1, color);
                }
            }
        }

        static void DrawDotGraph(float[] x, float[] y, Color col)
        {

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] <= 0 || y[i] <= 0)
                    { continue; }
                int xPos = Mathf.RoundToInt(graph_left + (x[i] - trueXRange[0]) / (trueXRange[1] - trueXRange[0]) * (graph_width - graph_left - graph_right - 1));
                int yPos = Mathf.RoundToInt(graph_bottom + (y[i] - trueYRange[0]) / (trueYRange[1] - trueYRange[0]) * (graph_height - graph_bottom - graph_top - 1));
                DrawDot(numGraph_tex, xPos, yPos, 15, col);
            }
        }

        static void DrawDot(Texture2D tex, int centerX, int centerY, int radius, Color color)
        {
            //int radius = 12;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    int px = centerX + x;
                    int py = centerY + y;

                    // Check if the pixel is within the circular radius
                    if (x * x + y * y < radius * radius)
                    {
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            float alpha = (float)(x * x + y * y) / (float)(radius * radius);
                            //float alpha = 0;
                            tex.SetPixel(px, py, Color.Lerp(color, tex.GetPixel(px, py), alpha));
                        }
                    }
                }
            }
            tex.Apply(); // Apply changes to update the texture
        }

        static void UpdateTwrRange()
        {
            List<float> twrMaxList = EngineLists
                .Where(e => e.effctiveWeight > 0 && e.realThrust > 0)
                .Select(e => e.realThrust / (e.effctiveWeight * e.surfaceGravity))
                .ToList();

            globalTwrRange[0] = twrMaxList.Where(x => x > 0.0f).DefaultIfEmpty().Min();
            globalTwrRange[1] = twrMaxList.Where(x => x > 0.0f).DefaultIfEmpty().Max();
        }

        static void UpdateDvRange()
        {
            List<float> dvMax;
            if (graphOption == 0)
            { dvMax = EngineLists.Select(e => e.GetOptimalDv()).ToList(); }
            else
            { dvMax = EngineLists.Select(e => e.realIsp * SEO_Main.standardGravity * Mathf.Log(e.wdr)).ToList(); }

            globalDvRange[0] = dvMax.Where(x => x > 0.0f).DefaultIfEmpty().Min();
            globalDvRange[1] = dvMax.Where(x => x > 0.0f).DefaultIfEmpty().Max();
        }

        static void DrawPixel(Texture2D tex, int x, int y, Color col, float alpha) // adapted from Correct CoL
        {
            //sets the color of a single pixel on screen, blended with the contents behind it
            tex.SetPixel(x, y, Color.Lerp(col, tex.GetPixel(x, y), alpha));
        }

        static void DrawLine(Texture2D tex, int x1, int y1, int x2, int y2, Color col) //adapted from Correct CoL
        {          
            int dy = y2 - y1;
            int dx = x2 - x1;
            int xdir = 1;
            int ydir = 1;

            if (dx < 0)
            {
                dx = -dx;
                xdir = -1;
            }

            if (dy < 0)
            {
                dy = -dy;
                ydir = -1;
            }

            float fraction = 0;
            float slope = 0;

            if (dx >= dy)
            {
                if (dx == 0)
                {
                    slope = 0;
                }
                else
                {
                    slope = (float)dy / dx;
                }
                if (x1 == x2)
                {
                    xdir = 0;
                }
                do
                {
                    DrawPixel(tex, (int)x1, (int)y1, col, fraction);
                    DrawPixel(tex, (int)x1, (int)y1 + ydir, col, 1 - fraction);
                    x1 += xdir;
                    fraction += slope;
                    if (fraction > 1)
                    {
                        fraction--;
                        y1 += ydir;
                    }
                } while (x1 != x2);
                return;
            }

            if (dy >= dx)
            {
                if (dy == 0)
                {
                    slope = 0;
                }
                else
                {
                    slope = (float)dx / dy;
                }
                if (y1 == y2)
                {
                    ydir = 0;
                }
                do
                {
                    DrawPixel(tex, (int)x1, (int)y1, col, fraction);
                    DrawPixel(tex, (int)x1 + xdir, (int)y1, col, 1 - fraction);
                    y1 += ydir;
                    fraction += slope;
                    if (fraction > 1)
                    {
                        fraction--;
                        x1 += xdir;
                    }
                } while (y1 != y2);
                return;
            }
        }

        static List<float> TickGen(float min, float max)
        {
            int expo = Mathf.FloorToInt(Mathf.Log10(max - min));
            float sig = (max - min) / Mathf.Pow(10, expo);

            List<float> tickList = new List<float> { 0 };

            float spaces;
            switch (sig)
            {
                case float x when x < 1.5 || x == 1:
                    spaces = 0.25f;
                    break;
                case float x when x >= 1.5 && x <= 3:
                    spaces = 0.5f;
                    break;
                case float x when x > 2 && x <= 6:
                    spaces = 1.0f;
                    break;
                default:
                    spaces = 2.0f;
                    break;
            }

            while (tickList[tickList.Count - 1] < max)
            {
                float a = tickList[tickList.Count - 1] + spaces * Mathf.Pow(10, expo);
                tickList.Add(a);
            }

            List<float> filteredList = tickList.Where(x => x >= min && x <= max).ToList();

            return filteredList;
        }

        static void SelectConstrain ()
        {
            int xSelection = GUILayout.SelectionGrid(constrainOp, constrainOpStr, 2);
            if (xSelection != constrainOp)
            {
                constrainOp = xSelection;
            }
        }
    }
}
