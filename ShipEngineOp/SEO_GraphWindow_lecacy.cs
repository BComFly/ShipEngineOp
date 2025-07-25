using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;


namespace ShipEngineOptimization
{
    public class SEO_GraphWindow
    {
        //static string twrMaxInput = "2.0";
        static float xMax_dvGraph = 2.0f;
        //static float yMax_dvGraph;
        //static List<int> engineIndexes = new List<int>();
        static List<Color> colorSequence = new List<Color> { Color.green, Color.blue, Color.red };
        static float[] globalDvRange = new float[2];
        static float[] globalTwrRange = new float[2]; // [0]=min [1]=max
        //static float[] xRange_numGraph = new float[2];
        //static float[] yRange_numGraph = new float[2];
        static float[] trueXRange = new float[2];
        static float[] trueYRange = new float[2];
        //static float xMaxDv = 6000f;
        const int num_pts = 200;

        static int[] numEngMax = new int[] { 3, 3, 3 };
        static int[] numEngMin = new int[] { 1, 1, 1 };
        
        // Var in Gross Comparison Window
        static bool filterT = false;
        static int qtyFilter = 9;
        static bool showAssignEngine;
        static string selectedEngineInGross;

        public static bool strictMode;

        //static int numEngMax0 = 3;
        //static int numEngMin0 = 1;

        public static float payload = 10f;

        public const int graph_width = 450;
        public const int graph_height = 350;
        public const int graph_left = 20;
        public const int graph_right = 30;
        public static int graph_top = 20;
        public const int graph_bottom = 20;
        static Vector2 graphCorner = new Vector2(10, 30);
        static Texture2D dvGraph_tex = new Texture2D(graph_width, graph_height, TextureFormat.ARGB32, false);
        static Texture2D numGraph_tex = new Texture2D(graph_width, graph_height, TextureFormat.ARGB32, false);
        static Texture2D bgTexture = new Texture2D(1, 1);

        static int constrainOp = 0; // 0 = TWR as x axis; 1 = dv as x axis
        public static string[] constrainOpStr = { Localizer.Format("#LOC_SHIPENGOP_common_constrainDv"), Localizer.Format("#LOC_SHIPENGOP_common_constrainTwr") };
        //static bool constrainDv = true;

        //public static bool autoUpdate = true;

        public static void DrawDvLimitWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 17, 2, 15, 15), ""))
            {
                SEO_Main.showDvLimit = false;
            }

            if (SEO_Main.selectedEngine.Count(eng => eng == SEO_Main.emptyStr) == SEO_Main.selectedEngine.Count())
            {
                SEO_Main.showDvLimit = false;
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

            for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            {
                GUI.color = colorSequence[idx];
                GUILayout.Box(SEO_Main.selectedEngine[idx], GUILayout.Width(200));
            }

            GUI.color = Color.white;

            GUILayout.Space(10);
            //GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            //GUILayout.Space(10);

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_dvLimitWindow_twrLimit"));

            UpdateTwrRange();
            GUILayout.BeginHorizontal();
            xMax_dvGraph = GUILayout.HorizontalSlider(xMax_dvGraph, 0, globalTwrRange[1]);
            GUILayout.Label(xMax_dvGraph.ToString("F2"));
            GUILayout.EndHorizontal();

            //GUILayout.Space(30);
            //autoUpdate = GUILayout.Toggle(autoUpdate, Localizer.Format("#LOC_SHIPENGOP_common_autoUpdate"));
            //if (autoUpdate)
            //{
            //    UpdateDvGraphs();
            //}
            //else
            //{
            //    if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_common_manualUpdate")))
            //    { UpdateDvGraphs(); }
            //}
            //if (!autoUpdate)
            //{
            //    if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_common_manualUpdate")))
            //    { UpdateDvGraphs(true); }
            //}


            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

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

            for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            {
                y_net = CalDvLimit(x_net, SEO_Main.selectedEngine[idx]);
                DrawCurveGraph(x_net, y_net, dvGraph_tex, colorSequence[idx]);
            }

            //y_net = CalDvLimit(x_net, EngDrawMain.selectedPartIndex0);
            //DrawCurveGraph(x_net, y_net, dvGraph_tex, Color.green);

            //y_net = CalDvLimit(x_net, EngDrawMain.selectedPartIndex1);
            //DrawCurveGraph(x_net, y_net, dvGraph_tex, Color.blue);

            //y_net = CalDvLimit(x_net, EngDrawMain.selectedPartIndex2);
            //DrawCurveGraph(x_net, y_net, dvGraph_tex, Color.red);

            GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.label);
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;

            // Tick generation
            List<float> xTicks = SEO_CommonFunctions.TickGen(0.0f, xMax_dvGraph);
            foreach (float xTick in xTicks)
            {
                int xPos = Mathf.RoundToInt(xTick / xMax_dvGraph * (graph_width - graph_left - graph_right - 1) + graph_left);
                DrawLine(dvGraph_tex, xPos, graph_bottom, xPos, graph_bottom + 4, Color.white);
                DrawLine(dvGraph_tex, xPos, graph_height - graph_top - 1, xPos, graph_height - graph_top - 5, Color.white);
                GUI.Label(new Rect(graphCorner.x + xPos - 14, graphCorner.y + graph_height - graph_bottom - 2, 31, 20), xTick.ToString(), centeredTextStyle);
            }
            List<float> yTicks = SEO_CommonFunctions.TickGen(0.0f, globalDvRange[1]);
            foreach (float yTick in yTicks)
            {
                int yPos = Mathf.RoundToInt(yTick / globalDvRange[1] * (graph_height - graph_top - graph_bottom - 1) + graph_bottom);
                DrawLine(dvGraph_tex, graph_width - graph_right - 1, yPos, graph_width - graph_right - 5, yPos, Color.white);
                DrawLine(dvGraph_tex, graph_left, yPos, graph_left + 4, yPos, Color.white);
                GUI.Label(new Rect(graphCorner.x + graph_width - graph_right + 3, graphCorner.y + graph_height - 13 - yPos, 40, 20), (yTick / 1000).ToString());
            }

            bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.75f)); // Black with 75% transparency
            bgTexture.Apply();

            centeredTextStyle.normal.background = bgTexture; // Set background texture

            // Labels
            GUI.Label(new Rect(graphCorner.x + graph_left - 3, graphCorner.y - 1, 40, 20), "(m/s)");
            GUI.Label(new Rect(graphCorner.x + graph_width - graph_right - 31, graphCorner.y - 1, 100, 20), "dV (km/s)");
            GUI.Label(new Rect(graphCorner.x + graph_width - graph_right - 21, graphCorner.y + graph_height - graph_bottom - 2, 36, 20), "TWR", centeredTextStyle);
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
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 17, 2, 15, 15), ""))
            {
                SEO_Main.showNumEng = false;
            }

            if (SEO_Main.selectedEngine.Count(eng => eng == SEO_Main.emptyStr) == SEO_Main.selectedEngine.Count() || twrTarget <= 0)
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

            for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            {
                GUI.color = colorSequence[idx];
                GUILayout.Box(SEO_Main.selectedEngine[idx], GUILayout.Width(200));
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engQtyWindow_qtyMin") + numEngMin[idx].ToString());
                numEngMin[idx] = Mathf.RoundToInt(GUILayout.HorizontalSlider(numEngMin[idx], 1, numEngMax[idx], GUILayout.Width(135)));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engQtyWindow_qtyMax") + numEngMax[idx].ToString());
                numEngMax[idx] = Mathf.RoundToInt(GUILayout.HorizontalSlider(numEngMax[idx], numEngMin[idx], 21, GUILayout.Width(135)));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            GUILayout.Space(10);

            PayloadInput();

            SelectConstrain();

            if (constrainOp == 0)
            {
                UpdateDvRange();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_engQtyWindow_targetDv") + dvTarget.ToString("F0") + " m/s");
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

        //static float[] twrDebug;
        //static float[] WeightDebug;

        public static void UpdateNumEngGraphs()
        {
            init_textures();
            init_axes();

            List<int[]> numEngNets = new List<int[]>();
            List<float[]> allPtsX = new List<float[]>();
            List<float[]> allPtsTotalWeight = new List<float[]>();
            //List<float[]> allPtsTotalWeight = new List<float[]> { totalWeight0, totalWeight1, totalWeight2 };

            for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            {
                var data = SEO_Main.engineData[SEO_Main.selectedEngine[idx]];
                float wdr = (SEO_Main.customWdr.ContainsKey(SEO_Main.selectedEngine[idx]) ? SEO_Main.customWdr[SEO_Main.selectedEngine[idx]].toggle : false) ? SEO_Main.customWdr[SEO_Main.selectedEngine[idx]].customValue : data.wdr;
                float realWeight = (SEO_Main.additionalWeight.ContainsKey(SEO_Main.selectedEngine[idx]) ? SEO_Main.additionalWeight[SEO_Main.selectedEngine[idx]].toggle : false) ? SEO_Main.additionalWeight[SEO_Main.selectedEngine[idx]].addiWeight + data.weight : data.weight;

                numEngNets.Add(new int[numEngMax[idx] - numEngMin[idx] + 1]);
                allPtsTotalWeight.Add(new float[numEngMax[idx] - numEngMin[idx] + 1]);
                allPtsX.Add(new float[numEngMax[idx] - numEngMin[idx] + 1]);

                for (int i = 0; i < numEngNets[idx].Length; i++)
                {
                    numEngNets[idx][i] = i + numEngMin[idx];
                    //numEngNet0[i] = i + numEngMin0;
                    switch (constrainOp)
                    {
                        case 0: // set dv
                            //if (SEO_Main.customWdr[SEO_Main.selectedEngine[idx]].customValue != 1.0f)
                            if (wdr != 1.0f)
                            {
                                //allPtsTotalWeight[idx][i] = SEO_Functions.TotalWeightFromDv(data.weight, data.thrust, data.isp, SEO_Main.customWdr[SEO_Main.selectedEngine[idx]], dvTarget, payload, numEngNets[idx][i]);
                                allPtsTotalWeight[idx][i] = SEO_CommonFunctions.TotalWeightFromDv(realWeight, data.thrust, data.isp, wdr, dvTarget, payload, numEngNets[idx][i]);
                                allPtsX[idx][i] = numEngNets[idx][i] * data.thrust / (allPtsTotalWeight[idx][i] * SEO_CommonFunctions.surfaceGravity);
                            }
                            break;
                        case 1: // set TWR
                                //if (SEO_Main.customWdr[SEO_Main.selectedEngine[idx]] != 1.0f && data.thrust / (SEO_Functions.sGravi * data.weight) > twrTarget)
                            if (wdr != 1.0f && data.thrust / (SEO_CommonFunctions.surfaceGravity * realWeight) > twrTarget)
                            {
                                //if (EngDrawMain.engineThrusts[engineIndexes[eng]] / (EngineFunctions.sGravi * EngDrawMain.engineWeights[engineIndexes[eng]]) <= setTWR)
                                //    { continue; }
                                allPtsTotalWeight[idx][i] = (numEngNets[idx][i] * data.thrust) / (SEO_CommonFunctions.surfaceGravity * twrTarget);
                                allPtsX[idx][i] = SEO_CommonFunctions.DvFromTwr(realWeight, data.thrust, data.isp, wdr, allPtsTotalWeight[idx][i], payload, numEngNets[idx][i]);
                                //allPtsX[idx][i] = SEO_Functions.DvFromTwr(data.weight, data.thrust, data.isp, SEO_Main.customWdr[SEO_Main.selectedEngine[idx]], allPtsTotalWeight[idx][i], payload, numEngNets[idx][i]);
                            }
                            break;
                    }
                }
            }

            float[] xRange_numGraph = new float[2];
            float[] yRange_numGraph = new float[2];
            //float[] xMins = new float[] { allPtsX[0][0], allPtsX[1][0], allPtsX[2][0] };
            xRange_numGraph[0] = allPtsX.SelectMany(array => array)
                .Where(x => x > 0.0f)
                .DefaultIfEmpty()
                .Min();
            xRange_numGraph[1] = allPtsX.SelectMany(array => array)
                .Where(x => x > 0.0f)
                .DefaultIfEmpty()
                .Max();
            yRange_numGraph[0] = allPtsTotalWeight.SelectMany(array => array)
                .Where(x => x > 0.0f)
                .DefaultIfEmpty()
                .Min();
            yRange_numGraph[1] = allPtsTotalWeight.SelectMany(array => array)
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
            GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.label);
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;
            //Texture2D bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.75f)); // Black with 75% transparency
            bgTexture.Apply();

            centeredTextStyle.normal.background = bgTexture; // Set background texture

            List<float> xTicks = SEO_CommonFunctions.TickGen(trueXRange[0], trueXRange[1]);
            foreach (float xTick in xTicks)
            {
                int xPos = Mathf.RoundToInt((xTick - trueXRange[0]) / (trueXRange[1] - trueXRange[0]) * (graph_width - graph_left - graph_right - 1) + graph_left);
                DrawLine(numGraph_tex, xPos, graph_bottom, xPos, graph_bottom + 4, Color.white);
                DrawLine(numGraph_tex, xPos, graph_height - graph_top - 1, xPos, graph_height - graph_top - 5, Color.white);
                //GUI.Label(new Rect(graphCorner.x + xPos, graphCorner.y + graph_height - graph_bottom - 2, 40, 20), xTick.ToString());
                GUI.Label(new Rect(graphCorner.x + xPos - 14, graphCorner.y + graph_height - graph_bottom - 2, 31, 20), xTick.ToString(constrainOp == 0 ? "G2" : "F0"), centeredTextStyle);
            }
            List<float> yTicks = SEO_CommonFunctions.TickGen(trueYRange[0], trueYRange[1]);
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
            GUI.Label(new Rect(graphCorner.x + graph_width - graph_right -21, graphCorner.y + graph_height - graph_bottom - 2, xLableWidth, 20), xLable, centeredTextStyle);

            // Highlight closest
            Vector2? closestValues = null;
            Vector2? closestPoint = null;
            int closestEng = 0;
            int closestNum = 0;
            float closestDistance = float.MaxValue;
            Vector2 mousePos = Event.current.mousePosition;

            // labels text style
            //GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.label);
            //centeredTextStyle.alignment = TextAnchor.MiddleCenter;

            bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.3f)); // Black with 50% transparency
            bgTexture.Apply();

            centeredTextStyle.normal.background = bgTexture; // Set background texture

            for (int eng = 0; eng < SEO_Main.selectedEngine.Length; eng++)
            {
                for (int j = 0; j < allPtsX[eng].Length; j++)
                {
                    float xPos = graphCorner.x + Mathf.RoundToInt(graph_left + (allPtsX[eng][j] - trueXRange[0]) / (trueXRange[1] - trueXRange[0]) * (graph_width - graph_left - graph_right - 1));
                    float yPos = graphCorner.y + graph_height - Mathf.RoundToInt(graph_bottom + (allPtsTotalWeight[eng][j] - trueYRange[0]) / (trueYRange[1] - trueYRange[0]) * (graph_height - graph_bottom - graph_top - 1));
                    
                    float distance = Vector2.Distance(new Vector2(xPos, yPos), mousePos);
                    if (distance < closestDistance)
                    {
                        closestEng = eng;
                        closestDistance = distance;
                        closestValues = new Vector2(allPtsX[eng][j], allPtsTotalWeight[eng][j]);
                        closestPoint = new Vector2(xPos, yPos);
                        closestNum = numEngNets[eng][j];
                    }

                    // labeling numbers
                    //GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(xPos - 8, yPos - 11, 17, 17), numEngNets[eng][j].ToString(), centeredTextStyle);
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
                        SEO_Main.selectedEngine[closestEng]
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
                        + closestNum.ToString());

                //if (mousePos.y <= graphCorner.y + (graph_height - graph_bottom - graph_top) / 2 + graph_top - 1)
                //{
                //    switch (constrainOp)
                //    {
                //        default:
                //            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y + 15, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_twr") + closestValues.Value.x.ToString("F2") + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass") + closestValues.Value.y.ToString("G3") + " t" + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty") + closestNum.ToString());
                //            break;
                //        case 1:
                //            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y + 15, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_dv") + closestValues.Value.x.ToString("F0") + " m/s" + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass") + closestValues.Value.y.ToString("G3") + " t" + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty") + closestNum.ToString());
                //            break;
                //    }
                //}
                //else
                //{
                //    switch (constrainOp)
                //    {
                //        default:
                //            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y - 80, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_twr") + closestValues.Value.x.ToString("F2") + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass") + closestValues.Value.y.ToString("G3") + " t" + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty") + closestNum.ToString());
                //            break;
                //        case 1:
                //            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y - 80, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_dv") + closestValues.Value.x.ToString("F0") + " m/s" + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass") + closestValues.Value.y.ToString("G3") + " t" + "\n" + Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty") + closestNum.ToString());
                //            break;
                //    }
                //    //GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y - 80, 140, 70), EngDrawMain.selectedEngine[closestEng] + "\n" + "TWR: " + closestValues.Value.x.ToString("F2") + "\n" + "Weight: " + closestValues.Value.y.ToString("F1") + " t" + "\n" + "Qty: " + closestNum.ToString());
                //}

            }

            for (int eng = 0; eng < SEO_Main.selectedEngine.Length; eng++)
            {
                DrawDotGraph(allPtsX[eng], allPtsTotalWeight[eng], colorSequence[eng]);
            }

            numGraph_tex.Apply();
        }

        private static HashSet<string> selectedTopFuelTypes = new HashSet<string>();
        private static Vector2 scrollPosition_eng = Vector2.zero;
        private static Vector2 scrollPosition_fuel = Vector2.zero;

        public static void DrawGrossSort(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.grossSortWndRect.size.x - 17, 2, 15, 15), ""))
            {
                SEO_Main.showGrossSort = false;
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_fuelFilter"));

            scrollPosition_fuel = GUILayout.BeginScrollView(scrollPosition_fuel, GUILayout.Width(300), GUILayout.Height(100));
            foreach (var fuel in SEO_Main.fuelTypes)
            {
                if (fuel == "None") continue;

                bool wasSelected = selectedTopFuelTypes.Contains(fuel);
                //bool isSelectedNow = GUILayout.Toggle(wasSelected, fuel);
                string displayName = SEO_Main.propellantDisplayNames.ContainsKey(fuel) ? SEO_Main.propellantDisplayNames[fuel] : fuel;
                bool isSelectedNow = GUILayout.Toggle(wasSelected, displayName);

                if (isSelectedNow && !wasSelected)
                    selectedTopFuelTypes.Add(fuel);
                else if (!isSelectedNow && wasSelected)
                    selectedTopFuelTypes.Remove(fuel);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(30);

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_configTitle"));

            var topEngines = selectedTopFuelTypes
                .Where(fuel => SEO_Main.groupedEngines.ContainsKey(fuel))
                .SelectMany(fuel => SEO_Main.groupedEngines[fuel])
                .Distinct()
                .Where(engineName => SEO_Main.engineData.ContainsKey(engineName))
                .Select(engineName =>
                {
                    var data = SEO_Main.engineData[engineName];
                    float wdr = (SEO_Main.customWdr.ContainsKey(engineName) ? SEO_Main.customWdr[engineName].toggle : false) ? SEO_Main.customWdr[engineName].customValue : data.wdr;
                    float realWeight = (SEO_Main.additionalWeight.ContainsKey(engineName) ? SEO_Main.additionalWeight[engineName].toggle : false) ? SEO_Main.additionalWeight[engineName].addiWeight + data.weight : data.weight;
                    float weightTotal = GetWeightTotal(realWeight, data.thrust, data.isp, wdr, payload, dvTarget, twrTarget);
                    float qtyEng = SEO_CommonFunctions.EngQtyUpper(realWeight, data.thrust, data.isp, wdr, payload, dvTarget, twrTarget);
                    return new
                    {
                        Name = engineName,
                        //data.weight,
                        weight = realWeight,
                        data.thrust,
                        data.isp,
                        wdr,
                        weightTotal,
                        //weightTotal = GetWeightTotal(realWeight, data.thrust, data.isp, wdr, payload, dvTarget, twrTarget),
                        fuelMass = (weightTotal - qtyEng * realWeight - payload) * (1 - 1/wdr),
                        qtyEng
                        //qtyEng = SEO_Functions.EngQtyUpper(realWeight, data.thrust, data.isp, wdr, payload, dvTarget, twrTarget)
                        //weightTotal = GetWeightTotal(data.weight, data.thrust, data.isp, customWdr, payload, dvTarget, twrTarget),
                        //qtyEng = SEO_Functions.EngQtyUpper(data.weight, data.thrust, data.isp, customWdr, payload, dvTarget, twrTarget)
                    };
                })
                .Where(e => e.weightTotal > 0)
                .Where(e => !filterT || e.qtyEng <= qtyFilter)
                .OrderBy(e => constrainOp == 0 ? e.weightTotal : e.fuelMass)
                //.OrderBy(e => e.weightTotal)
                .Take(10);

            scrollPosition_eng = GUILayout.BeginScrollView(scrollPosition_eng, GUILayout.Width(300), GUILayout.Height(300));

            //string seletedEngineSort;

            foreach (var engine in topEngines)
            {
                //GUILayout.Box(engine.Name + "\n" + "Weight: " + engine.weightTotal.ToString("F2") + " t, Qty: " + engine.qtyEng.ToString() + ", TWR: " + ( engine.thrust * engine.qtyEng / (engine.weightTotal * SEO_Functions.sGravi)).ToString("F2"));

                //string engText = engine.Name + "\n" + "Weight: " + engine.weightTotal.ToString("F2") + " t, Qty: " + engine.qtyEng.ToString() + ", TWR: " + (engine.thrust * engine.qtyEng / (engine.weightTotal * SEO_Functions.sGravi)).ToString("F2");
                //GUILayout.Box(engText);
                if (GUILayout.Button(engine.Name))
                {
                    selectedEngineInGross = engine.Name;
                    showAssignEngine = true;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass") + engine.weightTotal.ToString("G3") + " t");
                //float fuelMass = (engine.weightTotal - engine.qtyEng * engine.weight - payload) * (1 - 1/engine.wdr);
                GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_fuelMass") + engine.fuelMass.ToString("G3") + " t");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty") + engine.qtyEng.ToString());
                switch (constrainOp)
                {
                    default:
                        GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_twr") + (engine.thrust * engine.qtyEng / (engine.weightTotal * SEO_CommonFunctions.surfaceGravity)).ToString("F2"));
                        break;
                    case 1:
                        float dv = SEO_CommonFunctions.standardGravity * engine.isp * Mathf.Log(engine.wdr / (1f + (((payload + engine.weight * engine.qtyEng) / engine.weightTotal) * (engine.wdr - 1))));
                        GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_dv") + dv.ToString("F0") + " m/s");
                        break;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            //GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(2));
            //GUILayout.Space(10);

            GUILayout.BeginVertical();

            PayloadInput();

            //GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredDv"));
            GUILayout.Label(constrainOp == 0 ? Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredDv") : Localizer.Format("#LOC_SHIPENGOP_stageWindow_minDv"));
            //if (constrainOp == 0)
            //{
            //    GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredDv"));
            //}
            //else if (constrainOp == 1)
            //{
            //    GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_minDv"));
            //}

            string dvStr = GUILayout.TextField(dvTarget.ToString("F0"), 8, GUILayout.Width(60)); // Input box
            float.TryParse(dvStr, out dvTarget);

            //GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredTwr"));
            GUILayout.Label(constrainOp == 0 ? Localizer.Format("#LOC_SHIPENGOP_stageWindow_minTwr") : Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredTwr"));
            //if (constrainOp == 0)
            //{
            //    GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_minTwr"));
            //}
            //else if (constrainOp == 1)
            //{
            //    GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredTwr"));
            //}
            string twrStr = GUILayout.TextField(twrTarget.ToString("F2"), 5, GUILayout.Width(40)); // Input box
            float.TryParse(twrStr, out twrTarget);

            strictMode = GUILayout.Toggle(strictMode, Localizer.Format("#LOC_SHIPENGOP_stageWindow_strictMode"));

            SelectConstrain();
            GUILayout.Space(10);

            SEO_Main.AtmosphereSetting();

            SEO_Main.ToggleCareer();

            filterT = GUILayout.Toggle(filterT, Localizer.Format("#LOC_SHIPENGOP_stageWindow_QtyFilter"));
            if (filterT)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(150));
                qtyFilter = Mathf.RoundToInt(GUILayout.HorizontalSlider(qtyFilter, 1, 21, GUILayout.Width(100)));
                GUILayout.Label(qtyFilter.ToString());
                GUILayout.EndHorizontal();
                //topEngines = topEngines.Where(e => e.qtyEng <= qtyFilter);
            }

            if (showAssignEngine)
            {
                GUILayout.Space(10);
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_setIndiComparison"));
                GUILayout.Box(selectedEngineInGross, GUILayout.Width(200));
                for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
                {
                    if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_indiWindow_engine") + (idx + 1).ToString(), GUILayout.Width(100)))
                    {
                        SEO_Main.selectedEngine[idx] = selectedEngineInGross;
                        showAssignEngine = false;
                    }
                }
                if (GUILayout.Button(Localizer.Format("#LOC_SHIPENGOP_stageWindow_setIndiCancel"), GUILayout.Width(100)))
                {
                    showAssignEngine = false;
                    //SEO_Main.OpenIndiWindow();
                }
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }

        static float GetWeightTotal (float weightEng, float thrust, float isp, float wdr, float payload, float dv, float twr)
        {
            if (twr > thrust / (weightEng * SEO_CommonFunctions.surfaceGravity) || dv >= SEO_CommonFunctions.DvLimit(weightEng, thrust, isp, wdr, twr))
            {  return 0; }

            int qtyEng = SEO_CommonFunctions.EngQtyUpper(weightEng, thrust, isp, wdr, payload, dv, twr);


            switch (constrainOp)
            {
                default: // constrain dv
                    float e = Mathf.Exp(dv / (SEO_CommonFunctions.standardGravity * isp));
                    float weightTotal = (payload + qtyEng * weightEng) * (1 + (1 - e) / (e / wdr - 1));
                    return weightTotal;
                case 1: // constrain TWR
                    return thrust * qtyEng / (twr * SEO_CommonFunctions.surfaceGravity);
            }
            //float e = Mathf.Exp( dv / (SEO_Functions.sGravi * isp) );
            //float weightTotal = (payload + qtyEng*weightEng) * (1 + (1 - e) / (e/wdr - 1) );

            //return weightTotal;
        }

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
                //yMax_dvGraph = Mathf.Max(y[0], y[num_pts]);
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

                //// Highlight closest dot
                //float xAbs = xPos + graphCorner.x;
                //float yAbs = graph_height - 1 - yPos + graphCorner.y;

                //float distance = Vector2.Distance(new Vector2(xAbs, yAbs), mousePos);
                //if (distance < closestDistance)
                //{
                //    closestDistance = distance;
                //    closestPoint = new Vector2(xAbs, yAbs);
                //}
            }
            //if (closestPoint.HasValue)
            //{
            //    DrawDot(numGraph_tex, (int)(closestPoint.Value.x - graphCorner.x), (int)(graphCorner.y - closestPoint.Value.y + graph_height - 1), Color.white);
            //}
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
            List<float> twrMaxList = new List<float>();
            foreach (string engine in SEO_Main.selectedEngine)
            {
                var data = SEO_Main.engineData[engine];
                float realWeight = (SEO_Main.additionalWeight.ContainsKey(engine) ? SEO_Main.additionalWeight[engine].toggle : false) ? SEO_Main.additionalWeight[engine].addiWeight + data.weight : data.weight;

                if (data.thrust == 0.0f)
                    { continue; }
                //twrMaxList.Add(data.thrust / (data.weight * SEO_Functions.sGravi));
                twrMaxList.Add(data.thrust / (realWeight * SEO_CommonFunctions.surfaceGravity));
            }
            globalTwrRange[0] = twrMaxList.Where(x => x > 0.0f).DefaultIfEmpty().Min();
            globalTwrRange[1] = twrMaxList.Where(x => x > 0.0f).DefaultIfEmpty().Max();
        }

        static void UpdateDvRange()
        {
            List<float> dvMax = new List<float>();
            foreach (string engine in SEO_Main.selectedEngine)
            {
                var data = SEO_Main.engineData[engine];
                float wdr = (SEO_Main.customWdr.ContainsKey(engine) ? SEO_Main.customWdr[engine].toggle : false) ? SEO_Main.customWdr[engine].customValue : data.wdr;

                //if (SEO_Main.customWdr[engine] == 1.0f)
                if (wdr == 1.0f)
                { continue; }
                dvMax.Add(data.isp * SEO_CommonFunctions.standardGravity * Mathf.Log(wdr));
            }
            globalDvRange[0] = dvMax.Where(x => x > 0.0f).DefaultIfEmpty().Min();
            globalDvRange[1] = dvMax.Where(x => x > 0.0f).DefaultIfEmpty().Max();
        }


        static float[] CalDvLimit(float[] x, string engine)
        {
            var data = SEO_Main.engineData[engine];
            float wdr = (SEO_Main.customWdr.ContainsKey(engine) ? SEO_Main.customWdr[engine].toggle : false) ? SEO_Main.customWdr[engine].customValue : data.wdr;
            float realWeight = (SEO_Main.additionalWeight.ContainsKey(engine) ? SEO_Main.additionalWeight[engine].toggle : false) ? SEO_Main.additionalWeight[engine].addiWeight + data.weight : data.weight;

            float[] y = new float[num_pts + 1];
            for (int i = 0; i <= num_pts; i++)
            {
                //y[i] = SEO_Functions.DvLimit(data.weight, data.thrust, data.isp, wdr, x[i]);
                y[i] = SEO_CommonFunctions.DvLimit(realWeight, data.thrust, data.isp, wdr, x[i]);
            }
            return y;
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

        static void SelectConstrain ()
        {
            int xSelection = GUILayout.SelectionGrid(constrainOp, constrainOpStr, 2);
            if (xSelection != constrainOp)
            {
                constrainOp = xSelection;
            }

            //bool dvToggle = constrainDv; 
            //bool twrToggle = !constrainDv; 

            //GUILayout.BeginHorizontal();
            //bool newDvToggle = GUILayout.Toggle(dvToggle, "Constrain dv");
            //bool newTwrToggle = GUILayout.Toggle(twrToggle, "Constrain TWR");
            //GUILayout.EndHorizontal();

            //if (newDvToggle != dvToggle)
            //{
            //    constrainDv = true;
            //}
            //else if (newTwrToggle != twrToggle)
            //{
            //    constrainDv = false;
            //}
        }

        public static void PayloadInput ()
        {
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_common_payloadMass"));
            GUILayout.BeginHorizontal();
            string vesselMassStr = GUILayout.TextField(payload.ToString("F2"), 6, GUILayout.Width(60)); // Input box
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
    }
}
