using System;
using System.Collections.Generic;
using System.Diagnostics;
using RUI.Algorithms;
using Smooth.Algebraics;
using System.Linq;
using UnityEngine;
using VehiclePhysics;
using static VehiclePhysics.VPAudio;


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
        //static int numEngMax0 = 3;
        //static int numEngMin0 = 1;
        static float payload = 10f;

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

        static int xOption = 0; // 0 = TWR as x axis; 1 = dv as x axis
        static string[] xOptionStr = { "set a dV", "set a TWR" };

        public static void DrawDvLimitWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 17, 2, 15, 15), ""))
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

            //GUI.color = Color.green;
            //GUILayout.Box(EngDrawMain.engineParts[EngDrawMain.selectedPartIndex0], GUILayout.Width(200));
            //GUI.color = Color.blue;
            //GUILayout.Box(EngDrawMain.engineParts[EngDrawMain.selectedPartIndex1], GUILayout.Width(200));
            //GUI.color = Color.red;
            //GUILayout.Box(EngDrawMain.engineParts[EngDrawMain.selectedPartIndex2], GUILayout.Width(200));
            GUI.color = Color.white;

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            GUILayout.Space(10);

            GUILayout.Label("Thrust to Weight Limit");

            //float[] twrMaxList = new float[3];
            //twrMaxList[0] = EngDrawMain.engineThrusts[EngDrawMain.selectedPartIndex0] / (EngDrawMain.engineWeights[EngDrawMain.selectedPartIndex0] * EngineFunctions.sGravi);
            //twrMaxList[1] = EngDrawMain.engineThrusts[EngDrawMain.selectedPartIndex1] / (EngDrawMain.engineWeights[EngDrawMain.selectedPartIndex1] * EngineFunctions.sGravi);
            //twrMaxList[2] = EngDrawMain.engineThrusts[EngDrawMain.selectedPartIndex2] / (EngDrawMain.engineWeights[EngDrawMain.selectedPartIndex2] * EngineFunctions.sGravi);
            UpdateTwrRange();
            GUILayout.BeginHorizontal();
            xMax_dvGraph = GUILayout.HorizontalSlider(xMax_dvGraph, 0, globalTwrRange[1]);
            GUILayout.Label(xMax_dvGraph.ToString("F2"));
            GUILayout.EndHorizontal();

            //twrMaxInput = GUILayout.TextField(twrMaxInput, 3, GUILayout.Width(30));

            //if (float.TryParse(twrMaxInput, out float parsedtwrMax))
            //{
            //    //twrMaxValue = parsedtwrMax;
            //    xMax_dvGraph = parsedtwrMax;
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
            List<float> xTicks = SEO_Functions.TickGen(0.0f, xMax_dvGraph);
            foreach (float xTick in xTicks)
            {
                int xPos = Mathf.RoundToInt(xTick / xMax_dvGraph * (graph_width - graph_left - graph_right - 1) + graph_left);
                DrawLine(dvGraph_tex, xPos, graph_bottom, xPos, graph_bottom + 4, Color.white);
                DrawLine(dvGraph_tex, xPos, graph_height - graph_top - 1, xPos, graph_height - graph_top - 5, Color.white);
                GUI.Label(new Rect(graphCorner.x + xPos - 14, graphCorner.y + graph_height - graph_bottom - 2, 31, 20), xTick.ToString(), centeredTextStyle);
            }
            List<float> yTicks = SEO_Functions.TickGen(0.0f, globalDvRange[1]);
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

            dvGraph_tex.Apply();
        }

        static float setDv = 2400;
        static float setTWR =1f;

        public static void DrawNumEngWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.dvWndRect.size.x - 17, 2, 15, 15), ""))
            {
                SEO_Main.showNumEng = false;
            }

            GUILayout.BeginHorizontal();
            // column 1
            GUILayout.BeginVertical(GUILayout.Width(graph_width + 15));
            GUILayout.Space(5);
            GUILayout.Label(numGraph_tex);

            UpdateNumEngGraphs();
            //GUILayout.Label("Debug twr: " + string.Join(", ", twrDebug));
            //GUILayout.Label("Debug weight: " + string.Join(", ", WeightDebug));

            GUILayout.EndVertical();

            //column 2
            GUILayout.BeginVertical();

            for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            {
                GUI.color = colorSequence[idx];
                GUILayout.Box(SEO_Main.selectedEngine[idx], GUILayout.Width(200));
                GUI.color = Color.white;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Qty Min: " + numEngMin[idx].ToString());
                numEngMin[idx] = Mathf.RoundToInt(GUILayout.HorizontalSlider(numEngMin[idx], 1, numEngMax[idx], GUILayout.Width(135)));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Qty Max: " + numEngMax[idx].ToString());
                numEngMax[idx] = Mathf.RoundToInt(GUILayout.HorizontalSlider(numEngMax[idx], numEngMin[idx], 21, GUILayout.Width(135)));
                GUILayout.EndHorizontal();
            }

            //GUI.color = Color.green;
            //GUILayout.Box(EngDrawMain.engineParts[EngDrawMain.selectedPartIndex0], GUILayout.Width(200));
            //GUI.color = Color.white;
            //numEngMin0 = Mathf.RoundToInt(GUILayout.HorizontalSlider(numEngMin0, 1, numEngMax0));
            //numEngMax0 = Mathf.RoundToInt(GUILayout.HorizontalSlider(numEngMax0, numEngMin0, 12));

            //GUI.color = Color.blue;
            //GUILayout.Box(EngDrawMain.engineParts[EngDrawMain.selectedPartIndex1], GUILayout.Width(200));
            //GUI.color = Color.red;
            //GUILayout.Box(EngDrawMain.engineParts[EngDrawMain.selectedPartIndex2], GUILayout.Width(200));
            //GUI.color = Color.white;

            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            GUILayout.Space(10);

            GUILayout.Label("Payload Mass (tons):");
            string vesselMassStr = GUILayout.TextField(payload.ToString("F2"), 6); // Input box
            float.TryParse(vesselMassStr, out payload);

            if (GUILayout.Button("Set to Current Vessel"))
            {
                if (EditorLogic.fetch != null && EditorLogic.fetch.ship != null)
                {
                    payload = EditorLogic.fetch.ship.GetTotalMass();
                }
            }

            GUILayout.Label("Draw x axis by:");

            int xSelection = GUILayout.SelectionGrid(xOption, xOptionStr, 1);

            if (xSelection != xOption)
            {
                xOption = xSelection;
            }

            if (xOption == 0)
            {
                UpdateDvRange();
                GUILayout.Label("Target dV:" + setDv.ToString("F0"));
                setDv = GUILayout.HorizontalSlider(setDv, 0, globalDvRange[0]);
            }
            else if (xOption == 1)
            {
                UpdateTwrRange();
                GUILayout.Label("Target TWR: " + setTWR.ToString("F2"));
                setTWR = GUILayout.HorizontalSlider(setTWR, 0, globalTwrRange[0]);
            }

            //GUILayout.Label("Debug set dV: " + setDv.ToString("F0"));
            //GUILayout.Label("Debug set TWR: " + setTWR.ToString("F2"));

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

            //engineIndexes = new List<int> { EngDrawMain.selectedPartIndex0, EngDrawMain.selectedPartIndex1, EngDrawMain.selectedPartIndex2 };

            //int[] numEngNet0 = new int[numEngMax0 - numEngMin0 + 1];
            //float[] totalWeight0 = new float[numEngNet0.Length];
            //float[] totalWeight1 = new float[numEngNet0.Length];
            //float[] totalWeight2 = new float[numEngNet0.Length];
            //float[] xNet0 = new float[numEngNet0.Length];
            //float[] xNet1 = new float[numEngNet0.Length];
            //float[] xNet2 = new float[numEngNet0.Length];

            List<int[]> numEngNets = new List<int[]>();
            List<float[]> allPtsX = new List<float[]>();
            List<float[]> allPtsTotalWeight = new List<float[]>();
            //List<float[]> allPtsTotalWeight = new List<float[]> { totalWeight0, totalWeight1, totalWeight2 };

            for (int idx = 0; idx < SEO_Main.selectedEngine.Length; idx++)
            {
                var data = SEO_Main.engineData[SEO_Main.selectedEngine[idx]];
                numEngNets.Add(new int[numEngMax[idx] - numEngMin[idx] + 1]);
                allPtsTotalWeight.Add(new float[numEngMax[idx] - numEngMin[idx] + 1]);
                allPtsX.Add(new float[numEngMax[idx] - numEngMin[idx] + 1]);
                for (int i = 0; i < numEngNets[idx].Length; i++)
                {
                    numEngNets[idx][i] = i + numEngMin[idx];
                    //numEngNet0[i] = i + numEngMin0;
                    switch (xOption)
                    {
                        case 0: // set dv
                            if (SEO_Main.currentWdr[SEO_Main.selectedEngine[idx]] != 1.0f)
                            {
                                allPtsTotalWeight[idx][i] = SEO_Functions.TotalWeightFromDv(data.weight, data.thrust, data.isp, SEO_Main.currentWdr[SEO_Main.selectedEngine[idx]], setDv, payload, numEngNets[idx][i]);
                                allPtsX[idx][i] = numEngNets[idx][i] * data.thrust / (allPtsTotalWeight[idx][i] * SEO_Functions.sGravi);
                            }
                            break;
                        case 1: // set TWR
                            if (SEO_Main.currentWdr[SEO_Main.selectedEngine[idx]] != 1.0f && data.thrust / (SEO_Functions.sGravi * data.weight) > setTWR)
                            {
                                //if (EngDrawMain.engineThrusts[engineIndexes[eng]] / (EngineFunctions.sGravi * EngDrawMain.engineWeights[engineIndexes[eng]]) <= setTWR)
                                //    { continue; }
                                allPtsTotalWeight[idx][i] = (numEngNets[idx][i] * data.thrust) / (SEO_Functions.sGravi * setTWR);
                                allPtsX[idx][i] = SEO_Functions.DvFromTwr(data.weight, data.thrust, data.isp, SEO_Main.currentWdr[SEO_Main.selectedEngine[idx]], allPtsTotalWeight[idx][i], payload, numEngNets[idx][i]);
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

            //DrawDotGraph(allPtsX[0], allPtsTotalWeight[0], Color.green);
            //DrawDotGraph(allPtsX[1], allPtsTotalWeight[1], Color.blue);
            //DrawDotGraph(allPtsX[2], allPtsTotalWeight[2], Color.red);

            //for (int i = 0; i < numEngNet0.Length; i++)
            //{
            //    int x = Mathf.RoundToInt(graph_left + (0.1f + (xNet0[i] - xMin_numGraph) / (xMax_numGraph - xMin_numGraph) * 0.8f) * (graph_width - 1 - graph_left));
            //    //int x = Mathf.RoundToInt(vesselTWR0[i] / twrMax * graph_width);
            //    int y = Mathf.RoundToInt(graph_bottom + ( 0.1f + (totalWeight0[i] - totalWeightMin) / (totalWeightMax - totalWeightMin) * 0.8f ) * (graph_height - 1 - graph_bottom));
            //    //int y = Mathf.RoundToInt(totalWeight0[i] / totalWeightMax * graph_height);
            //    DrawDot(numGraph_tex, x, y, Color.green);
            //}

            //yNet = CalVesselWeight(numEngNet0, setDv, 0);


            //float dvMax0 = EngDrawMain.engineISPs[EngDrawMain.selectedPartIndex0] * EngineFunctions.sGravi * Mathf.Log(EngDrawMain.engineWDRs[EngDrawMain.selectedPartIndex0]);
            //float dvMax1 = EngDrawMain.engineISPs[EngDrawMain.selectedPartIndex1] * EngineFunctions.sGravi * Mathf.Log(EngDrawMain.engineWDRs[EngDrawMain.selectedPartIndex1]);
            //float dvMax2 = EngDrawMain.engineISPs[EngDrawMain.selectedPartIndex2] * EngineFunctions.sGravi * Mathf.Log(EngDrawMain.engineWDRs[EngDrawMain.selectedPartIndex2]);

            //float[] x_net = new float[num_pts + 1];
            //float[] y_net = new float[num_pts + 1];
            //float[] wtpMaxs = new float[3];

            //// build x net
            //for (int i = 0; i <= num_pts; i++)
            //{
            //    float x = i;
            //    x_net[i] = x / num_pts * xMaxTWR;
            //}

            //DrawDot(numGraph_tex, 50, 20, Color.green);

            //y_net = CalWeightToPayload(x_net, EngDrawMain.selectedPartIndex0);
            //DrawCurveGraph(x_net, y_net, numGraph_tex, Color.green);
            //wtpMaxs[0] = y_net[num_pts];

            //y_net = CalWeightToPayload(x_net, EngDrawMain.selectedPartIndex1);
            //DrawCurveGraph(x_net, y_net, numGraph_tex, Color.blue);
            //wtpMaxs[1] = y_net[num_pts];

            //y_net = CalWeightToPayload(x_net, EngDrawMain.selectedPartIndex2);
            //DrawCurveGraph(x_net, y_net, numGraph_tex, Color.red);
            //wtpMaxs[2] = y_net[num_pts];

            //float y_max = Mathf.Max(wtpMaxs);



            // Tick generation
            GUIStyle centeredTextStyle = new GUIStyle(GUI.skin.label);
            centeredTextStyle.alignment = TextAnchor.MiddleCenter;
            //Texture2D bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.75f)); // Black with 75% transparency
            bgTexture.Apply();

            centeredTextStyle.normal.background = bgTexture; // Set background texture

            List<float> xTicks = SEO_Functions.TickGen(trueXRange[0], trueXRange[1]);
            foreach (float xTick in xTicks)
            {
                int xPos = Mathf.RoundToInt((xTick - trueXRange[0]) / (trueXRange[1] - trueXRange[0]) * (graph_width - graph_left - graph_right - 1) + graph_left);
                DrawLine(numGraph_tex, xPos, graph_bottom, xPos, graph_bottom + 4, Color.white);
                DrawLine(numGraph_tex, xPos, graph_height - graph_top - 1, xPos, graph_height - graph_top - 5, Color.white);
                //GUI.Label(new Rect(graphCorner.x + xPos, graphCorner.y + graph_height - graph_bottom - 2, 40, 20), xTick.ToString());
                GUI.Label(new Rect(graphCorner.x + xPos - 14, graphCorner.y + graph_height - graph_bottom - 2, 31, 20), xTick.ToString(), centeredTextStyle);
            }
            List<float> yTicks = SEO_Functions.TickGen(trueYRange[0], trueYRange[1]);
            foreach (float yTick in yTicks)
            {
                int yPos = Mathf.RoundToInt((yTick - trueYRange[0]) / (trueYRange[1] - trueYRange[0]) * (graph_height - graph_top - graph_bottom - 1) + graph_bottom);
                DrawLine(numGraph_tex, graph_width - graph_right - 1, yPos, graph_width - graph_right - 5, yPos, Color.white);
                DrawLine(numGraph_tex, graph_left, yPos, graph_left + 4, yPos, Color.white);
                GUI.Label(new Rect(graphCorner.x + graph_width - graph_right + 3, graphCorner.y + graph_height - 13 - yPos, 40, 20), yTick.ToString());
            }

            // Labels
            string xLable = "TWR";
            float xLableWidth = 36;
            switch (xOption)
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

                
                if (mousePos.y <= graphCorner.y + (graph_height - graph_bottom - graph_top) / 2 + graph_top - 1)
                {
                    switch (xOption)
                    {
                        default:
                            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y + 15, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + "TWR: " + closestValues.Value.x.ToString("F2") + "\n" + "Total Weight: " + closestValues.Value.y.ToString("F1") + " t" + "\n" + "Qty: " + closestNum.ToString());
                            break;
                        case 1:
                            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y + 15, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + "dV: " + closestValues.Value.x.ToString("F0") + " m/s" + "\n" + "Total Weight: " + closestValues.Value.y.ToString("F1") + " t" + "\n" + "Qty: " + closestNum.ToString());
                            break;
                    }
                }
                else
                {
                    switch (xOption)
                    {
                        default:
                            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y - 80, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + "TWR: " + closestValues.Value.x.ToString("F2") + "\n" + "Total Weight: " + closestValues.Value.y.ToString("F1") + " t" + "\n" + "Qty: " + closestNum.ToString());
                            break;
                        case 1:
                            GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y - 80, 140, 70), SEO_Main.selectedEngine[closestEng] + "\n" + "dV: " + closestValues.Value.x.ToString("F0") + " m/s" + "\n" + "Total Weight: " + closestValues.Value.y.ToString("F1") + " t" + "\n" + "Qty: " + closestNum.ToString());
                            break;
                    }
                    //GUI.Box(new Rect(closestPoint.Value.x - 70, closestPoint.Value.y - 80, 140, 70), EngDrawMain.selectedEngine[closestEng] + "\n" + "TWR: " + closestValues.Value.x.ToString("F2") + "\n" + "Weight: " + closestValues.Value.y.ToString("F1") + " t" + "\n" + "Qty: " + closestNum.ToString());
                }

            }

            for (int eng = 0; eng < SEO_Main.selectedEngine.Length; eng++)
            {
                DrawDotGraph(allPtsX[eng], allPtsTotalWeight[eng], colorSequence[eng]);
            }

            numGraph_tex.Apply();
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
                if (data.thrust == 0.0f)
                    { continue; }
                twrMaxList.Add(data.thrust / (data.weight * SEO_Functions.sGravi));
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
                if (SEO_Main.currentWdr[engine] == 1.0f)
                { continue; }
                dvMax.Add(data.isp * SEO_Functions.sGravi * Mathf.Log(SEO_Main.currentWdr[engine]));
            }
            globalDvRange[0] = dvMax.Where(x => x > 0.0f).DefaultIfEmpty().Min();
            globalDvRange[1] = dvMax.Where(x => x > 0.0f).DefaultIfEmpty().Max();
        }


        static float[] CalDvLimit(float[] x, string engine)
        {
            var data = SEO_Main.engineData[engine];
            float[] y = new float[num_pts + 1];
            for (int i = 0; i <= num_pts; i++)
            {
                y[i] = SEO_Functions.DvLimit(data.weight, data.thrust, data.isp, SEO_Main.currentWdr[engine], x[i]);

            }
            return y;
        }
        //static float CalVesselWeight(int num, float setValue, string engine)
        //{
        //    var data = EngDrawMain.engineData[engine];
        //    float y;
        //    y = EngineFunctions.TotalWeightFromDv(data.weight, data.thrust, data.isp, SEO_Main.currentWdr[SEO_Main.selectedEngine[idx]], setValue, payload, num);
        //    //for (int i = 0; i < x.Length; i++)
        //    //{
        //    //    y = EngineFunctions.vesselWeight(EngDrawMain.engineWeights[index], EngDrawMain.engineThrusts[index], EngDrawMain.engineISPs[index], (float)EngDrawMain.engineWDRs[index], setValue, vesselMass, num);

        //    //}
        //    return y;
        //}

        //static float[] CalWeightToPayload(float[] x, int index)
        //{
        //    float[] y = new float[num_pts + 1];
        //    for (int i = 0; i <= num_pts; i++)
        //    {
        //        y[i] = EngineFunctions.WeightToPayload(EngDrawMain.engineWeights[index], EngDrawMain.engineThrusts[index], EngDrawMain.engineISPs[index], (float)EngDrawMain.engineWDRs[index], setDv, x[i]);
                
        //        //switch (xOption)
        //        //{
        //        //    case 0:
        //        //        Y_net[i] = EngineFunctions.WeightToPayload(EngDrawMain.engineWeights[index], EngDrawMain.engineThrusts[index], EngDrawMain.engineISPs[index], (float)EngDrawMain.engineWDRs[index], setDv, X_net[i]);
        //        //        break;
        //        //    case 1:
        //        //        Y_net[i] = EngineFunctions.WeightToPayload(EngDrawMain.engineWeights[index], EngDrawMain.engineThrusts[index], EngDrawMain.engineISPs[index], (float)EngDrawMain.engineWDRs[index], X_net[i], setTWR);
        //        //        break;
        //        //}
        //    }
        //    return y;
        //    //if (twrAsX)
        //    //{
        //    //    Y_net[i] = EngineFunctions.WeightToPayload(EngDrawMain.engineWeights[index], EngDrawMain.engineThrusts[index], EngDrawMain.engineISPs[index], (float)EngDrawMain.engineWDRs[index], setDv, X_net[i]);
        //    //}
        //    //else
        //    //{
        //    //    Y_net[i] = EngineFunctions.WeightToPayload(EngDrawMain.engineWeights[index], EngDrawMain.engineThrusts[index], EngDrawMain.engineISPs[index], (float)EngDrawMain.engineWDRs[index], X_net[i], setTWR);
        //    //}

        //}

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
    }
}
