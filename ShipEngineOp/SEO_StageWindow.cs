using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace ShipEngineOptimization
{
    public class SEO_StageWindow
    {
        public static List<StageConfig> stageList = new List<StageConfig>();
        public static Vector2 scrollPosition_fuelGlobal = Vector2.zero;
        public static Vector2 scrollPosition_eng = Vector2.zero;
        public static Vector2 scrollPosition_stages = Vector2.zero;

        public static HashSet<string> selectedFuelGlobal = new HashSet<string>();
        public static HashSet<string> selectedFuelStage = new HashSet<string>();

        //public static int selectedStageIndex;
        public static StageConfig selectedStageForEngine;
        public static StageConfig selectedStageForPlanet;
        //static int division = 7;

        public static void DrawStageWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.StageWndRect.size.x - 17, 2, 15, 15), "X", SEO_Main.smallButton))
            {
                SEO_Main.showStageWindow = false;
            }
            if (GUI.Button(new Rect(SEO_Main.StageWndRect.size.x - 32, 2, 15, 15), new GUIContent("?", Localizer.Format("#LOC_SHIPENGOP_tooltips_toggleTooltips")), SEO_Main.smallButton))
            {
                SEO_Main.showTooltip = !SEO_Main.showTooltip;
            }

            if (stageList.Count < 1)
            {
                stageList.Add(new StageConfig());
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_propellantFilter"), Localizer.Format("#LOC_SHIPENGOP_tooltips_fuelFilter")));
            scrollPosition_fuelGlobal = GUILayout.BeginScrollView(scrollPosition_fuelGlobal, GUILayout.Width(300), GUILayout.Height(100));
            selectedFuelGlobal = GetFuelFilter(selectedFuelGlobal);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            //GUILayout.Space(20);
            GUILayout.Label("", GUILayout.ExpandWidth(true));
            SEO_Main.ToggleCareer();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            SEO_Main.PayloadInput();

            float totalDv = stageList.Select(m => m.trueDv).Sum();
            float totalMass = GetTotalWeight(stageList);

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_totalDv") + totalDv.ToString("F0") + " " + Localizer.Format("#LOC_SHIPENGOP_units_metersPerSecond"));
            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass") + (totalMass == float.MaxValue ? Localizer.Format("#LOC_SHIPENGOP_infinity") : (totalMass.ToString("G3")  + " " + Localizer.Format("#LOC_SHIPENGOP_units_tons"))));

        //PSO staging
            if (GUILayout.Button(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_optimizeStaging"), Localizer.Format("#LOC_SHIPENGOP_tooltips_optimizingStages"))))
            {
                List<StageConfig> optimizingStages = stageList.Where(s => s.includeInOp).ToList();
                if (optimizingStages.Count > 1)
                {
                    totalDv = optimizingStages.Sum(s => s.targetDv);
                    foreach (StageConfig stage in optimizingStages)
                    {
                        stage.constrainOp = 0;
                        if (stage != optimizingStages.First())
                        {
                            stage.targetTwr = optimizingStages.First().targetTwr;
                            stage.SetPlanet(optimizingStages.First().planetNameStr);
                            stage.altitude = optimizingStages.First().altitude;
                        }
                    }

                    float lastTotalWeight = float.MaxValue;
                    List<float> lastDvList = new List<float>();
                    List<StageConfig> lastStageList = stageList.ToList();

                    for (int i = 1; i <= optimizingStages.Count; i++)
                    {
                        foreach (StageConfig stage in optimizingStages)
                        {
                            if (stage != optimizingStages.First())
                            { stage.targetDv = 0; }
                            else
                            { stage.targetDv = totalDv; }
                        }

                        List<StageConfig> mockStageList = stageList.ToList();
                        List<StageConfig> mockOptimizingStages = optimizingStages.ToList();

                        foreach (StageConfig stage in optimizingStages)
                        {
                            if (optimizingStages.IndexOf(stage) >= i)
                            {
                                mockOptimizingStages.Remove(stage);
                                mockStageList.Remove(stage);
                                //Debug.Log("Deleted 1 stage");
                            }
                        }
                        //Debug.Log($"1 iteration, {mockStageList.Count()} stages");
                        //mockOptimizingStages.RemoveRange(mockOptimizingStages.Count - i, i);
                        //foreach (StageConfig stage in optimizingStages)
                        //{
                        //    if (!mockOptimizingStages.Contains(stage))
                        //    { mockStageList.Remove(stage); }
                        //}

                        if (mockOptimizingStages.Count > 1)
                        { PsoSelectedStages(mockOptimizingStages, mockStageList); }

                        float newTotalWeight = GetTotalWeight(mockStageList);
                        //Debug.Log($"Total weight obtained {newTotalWeight:F6}");

                        if (newTotalWeight >= lastTotalWeight && lastTotalWeight != float.MaxValue)
                        {
                            //Debug.Log($"new total weight {newTotalWeight:G3}, last total weight {lastTotalWeight:G3}");
                            stageList = lastStageList.ToList();
                            for (i = 0; i < stageList.Count(); i++)
                            { stageList[i].targetDv = lastDvList[i]; }
                            Debug.Log(string.Join(", ", lastDvList));
                            break;
                        }
                        else
                        {
                            //Debug.Log("1 iteration");
                            lastTotalWeight = newTotalWeight;
                            lastStageList = mockStageList.ToList();
                            lastDvList = lastStageList.Select(s => s.targetDv).ToList();
                        }
                    }
                    //PsoSelectedStages(optimizingStages, stageList);
                }
            }
            GUILayout.Box( Localizer.Format("#LOC_SHIPENGOP_stageWindow_selectedStages") + " " + string.Join(Localizer.Format("#LOC_SHIPENGOP_joint"), stageList.Where(s => s.includeInOp).Select(s => stageList.IndexOf(s))) );

            //division = Mathf.RoundToInt(GUILayout.HorizontalSlider(division, 1, 10));

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.BeginVertical();

        //Stage List Starts
            scrollPosition_stages = GUILayout.BeginScrollView(scrollPosition_stages, GUILayout.Width(600), GUILayout.Height(500));

            foreach (StageConfig stage in stageList)
            {
                GUILayout.BeginHorizontal(GUILayout.Width(580)); // Begin stage title
                if (stageList.Count > 1)
                {
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        stageList.Remove(stage);
                        break;
                    }
                }
                GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_stageWindow_stage") + stageList.IndexOf(stage).ToString());
                GUILayout.EndHorizontal(); // End stage title

                stage.payloadMass = stageList.IndexOf(stage) == 0 ? SEO_Main.payload : stageList[stageList.IndexOf(stage) - 1].stageWeight;

                GUILayout.BeginHorizontal(); // Begin stage stats

                    GUILayout.BeginVertical(GUILayout.Width(235)); // Begin left side

                //stage.manualMode = GUILayout.Toggle(stage.manualMode, Localizer.Format("#LOC_SHIPENGOP_stageWindow_manualMode"));

                        GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredDv"));
                string targetDvStr = GUILayout.TextField(stage.targetDv.ToString("F0"), 6, GUILayout.Width(60));
                float.TryParse(targetDvStr, out stage.targetDv);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_desiredTwr"));
                string targetTwrStr = GUILayout.TextField(stage.targetTwr.ToString("F2"), 6, GUILayout.Width(60));
                float.TryParse(targetTwrStr, out stage.targetTwr);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_extraMass"), Localizer.Format("#LOC_SHIPENGOP_tooltips_extraMass")));
                string addiMassStr = GUILayout.TextField(stage.additionalMass.ToString(stage.additionalMass == 0 ? "F2" : ""), 7, GUILayout.Width(60));
                float.TryParse(addiMassStr, out stage.additionalMass);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                //stage.strictMode = GUILayout.Toggle(stage.strictMode, new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_strictMode"), Localizer.Format("#LOC_SHIPENGOP_tooltips_strictMode")));
                stage.constrainOp = SetConstrain(stage.constrainOp);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                stage.filterT = GUILayout.Toggle(stage.filterT, Localizer.Format("#LOC_SHIPENGOP_stageWindow_QtyFilter"));
                if (stage.filterT)
                {
                    stage.qtyFilter = Mathf.RoundToInt(GUILayout.HorizontalSlider(stage.qtyFilter, 1, 21, GUILayout.Width(80)));
                    GUILayout.Label(stage.qtyFilter.ToString());
                }
                        GUILayout.EndHorizontal();

                GUILayout.Space(3);
                SEO_Main.GetAtm(stage);

                    GUILayout.EndVertical(); // End left side

                GUILayout.Space(20);

                    GUILayout.BeginVertical(GUILayout.Width(320)); // Begin right side

                        GUILayout.BeginHorizontal();
                stage.manualMode = GUILayout.Toggle(stage.manualMode, Localizer.Format("#LOC_SHIPENGOP_stageWindow_manualMode"));
                stage.keepEngine = GUILayout.Toggle(stage.keepEngine, new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_keepEngine"), Localizer.Format("#LOC_SHIPENGOP_tooltips_asparagusStaging")));
                //stage.includeInOp = GUILayout.Toggle(stage.includeInOp, new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_includeInOp"), Localizer.Format("#LOC_SHIPENGOP_tooltips_includeOptimizing")));
                        GUILayout.EndHorizontal();

                if (stageList.IndexOf(stage) == 0)
                { stage.keepEngine = false; }
                else if (stage.keepEngine)
                {
                    //stage.manualMode = true;
                    stage.includeInOp = stageList[stageList.IndexOf(stage) - 1].includeInOp;
                }
                //GUILayout.EndVertical();

                //GUILayout.Label(stage.engineName);
                if (stage.manualMode)
                {
                    if (GUILayout.Button(new GUIContent(stage.engineName, Localizer.Format("#LOC_SHIPENGOP_tooltips_stageEngineManualMode")), SEO_Main.engineNameButton, GUILayout.Width(320)))
                    {
                        //selectedFuelStage = selectedFuelGlobal;
                        selectedFuelStage.Clear();
                        selectedFuelStage.UnionWith(selectedFuelGlobal);
                        selectedStageForEngine = stage;
                        //selectedStageIndex = stageList.IndexOf(stage);
                        //stage.manualSelEngine = stage.engineName;
                        SEO_Main.showStageEngWindow = true;
                    }
                }
                else { GUILayout.Box(new GUIContent(stage.engineName, Localizer.Format("#LOC_SHIPENGOP_tooltips_stageEngineAutoMode")), SEO_Main.engineNameBox, GUILayout.Width(320)); }

                        GUILayout.BeginHorizontal(); // Begin right side columns
                            GUILayout.BeginVertical(GUILayout.Width(150)); // Begin right side column 1

                stage.strictMode = GUILayout.Toggle(stage.strictMode, new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_strictMode"), Localizer.Format("#LOC_SHIPENGOP_tooltips_strictMode")));
                GUILayout.Space(3);
                if (stage.stageWeight != float.MaxValue)
                {
                    GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty") + stage.engineQty.ToString());
                    GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_stageMass") + stage.stageWeight.ToString("G3") + " " + Localizer.Format("#LOC_SHIPENGOP_units_tons"));
                    if (stage.trueTwr < stage.targetTwr || stage.trueDv < stage.targetDv) { GUI.color = Color.red; }
                    GUILayout.Label(stage.constrainOp == 0 ?
                        (Localizer.Format("#LOC_SHIPENGOP_shipSpecs_twr") + stage.trueTwr.ToString("F2")) :
                        (Localizer.Format("#LOC_SHIPENGOP_shipSpecs_dv") + stage.trueDv.ToString("F0") + " " + Localizer.Format("#LOC_SHIPENGOP_units_metersPerSecond")));
                    GUI.color = Color.white;
                }
                            GUILayout.EndVertical(); // End right side column 1

                            GUILayout.BeginVertical(); // Begin right side column 2
                stage.includeInOp = GUILayout.Toggle(stage.includeInOp, new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_includeInOp"), Localizer.Format("#LOC_SHIPENGOP_tooltips_includeOptimizing")));
                    
            //Split stage
                if (GUILayout.Button(new GUIContent(Localizer.Format("#LOC_SHIPENGOP_stageWindow_splitStage"), Localizer.Format("#LOC_SHIPENGOP_tooltips_automaticSplit")), GUILayout.Width(100), GUILayout.Height(60)))
                {
                    stage.keepEngine = false;
                    stage.constrainOp = 0;
                    stage.GetBestEngine();

                    float startingPayload = stage.payloadMass;
                    float stageTotalDv = stage.targetDv;

                    List<StageConfig> splitStageList = new List<StageConfig> { stage };
                    float newTotalWeight = stage.stageWeight;
                    float lastTotalWeight = float.MaxValue;
                    List<StageConfig> lastStageList = splitStageList.ToList();
                    List<float> lastDvList = lastStageList.Select(s => s.targetDv).ToList();

                    while (newTotalWeight < lastTotalWeight || newTotalWeight == float.MaxValue)
                    {
                        lastTotalWeight = newTotalWeight;
                        lastStageList = splitStageList.ToList();
                        lastDvList = lastStageList.Select(s => s.targetDv).ToList();

                        splitStageList.Insert(0, stage.CloneStageWithPayload(startingPayload));
                        PsoSelectedStages(splitStageList, splitStageList);
                        newTotalWeight = GetTotalWeight(splitStageList);
                    }

                    lastStageList.First().payloadMass = startingPayload;

                    if (lastStageList.Count > 1)
                    {
                        for (int i = 0; i < lastStageList.Count; i++)
                        { lastStageList[i].targetDv = lastDvList[i]; }
                        lastStageList.Remove(stage);
                        stageList.InsertRange(stageList.IndexOf(stage), lastStageList);
                    }
                    else
                    {
                        stage.payloadMass = startingPayload;
                        stage.targetDv = lastDvList.Last();
                    }

                    //debug
                    //Debug.Log("Split stage into " + lastStageList.Count());
                    //Debug.Log("dv distribution: " + string.Join(" ", lastDvList));
                    //stage.payloadMass = startingPayload;
                    //stage.targetDv = stageTotalDv;
                    splitStageList.Clear();
                    lastStageList.Clear();
                }
                            GUILayout.EndVertical(); // End right side column 2
                
                        GUILayout.EndHorizontal(); // End right side columns

                    GUILayout.EndVertical(); // End right side

                GUILayout.EndHorizontal(); // End stage stats

                GUILayout.Space(5);
            }

            if (GUILayout.Button("+"))
            {
                stageList.Add(new StageConfig());
            }


            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            SEO_Main.DrawToolTip();
            GUI.DragWindow();
        }

        public static void DrawStageEngineWindow(int windowID)
        {
            if (GUI.Button(new Rect(SEO_Main.StageEngWndRect.size.x - 17, 2, 15, 15), "X", SEO_Main.smallButton))
            {
                SEO_Main.showStageEngWindow = false;
            }
            if (GUI.Button(new Rect(SEO_Main.StageEngWndRect.size.x - 32, 2, 15, 15), "?", SEO_Main.smallButton))
            {
                SEO_Main.showTooltip = !SEO_Main.showTooltip;
            }

            GUILayout.BeginVertical(GUILayout.Width(250));

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_propellantFilterManualMode"));
            scrollPosition_fuelGlobal = GUILayout.BeginScrollView(scrollPosition_fuelGlobal, GUILayout.Width(300), GUILayout.Height(100));
            //SEO_StageWindow fuelGlobalRef = new SEO_StageWindow();
            //selectedFuelStage = fuelGlobalRef.GetFuelFilter(selectedFuelStage);
            selectedFuelStage = GetFuelFilter(selectedFuelStage);
            GUILayout.EndScrollView();

            GUILayout.Space(20);

            //var topEngineList = stageList[selectedStageIndex].GetTopEngineList(selectedFuelStage);
            var topEngineList = selectedStageForEngine.GetTopEngineList(selectedFuelStage);

            GUILayout.Label(Localizer.Format("#LOC_SHIPENGOP_stageWindow_engineConfig"));
            scrollPosition_eng = GUILayout.BeginScrollView(scrollPosition_eng, GUILayout.Width(300), GUILayout.Height(500));
            foreach (var engine in topEngineList)
            {
                if (GUILayout.Button(engine.Name, SEO_Main.engineNameButton))
                {
                    //stageList[selectedStageIndex].SetManualSelEngine(engine.Name);
                    selectedStageForEngine.SetManualSelEngine(engine.Name);
                    SEO_Main.showStageEngWindow = false;
                    //selectedEngineInGross = engine.Name;
                    //showAssignEngine = true;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_totalMass") + engine.weightTotal.ToString("G3") + " " + Localizer.Format("#LOC_SHIPENGOP_units_tons"));
                GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_fuelMass") + engine.fuelMass.ToString("G3") + " " + Localizer.Format("#LOC_SHIPENGOP_units_tons"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Box(Localizer.Format("#LOC_SHIPENGOP_shipSpecs_engineQty") + engine.qtyEng.ToString());

                //if (engine.twr < stageList[selectedStageIndex].targetTwr) { GUI.color = Color.red; }
                if (engine.twr < selectedStageForEngine.targetTwr) { GUI.color = Color.red; }
                GUILayout.Box(selectedStageForEngine.constrainOp == 0 ?
                    //GUILayout.Box(stageList[selectedStageIndex].constrainOp == 0 ?
                    (Localizer.Format("#LOC_SHIPENGOP_shipSpecs_twr") + engine.twr.ToString("F2")) :
                    (Localizer.Format("#LOC_SHIPENGOP_shipSpecs_dv") + engine.dv.ToString("F0") + " " + Localizer.Format("#LOC_SHIPENGOP_units_metersPerSecond")));
                GUI.color = Color.white;

                GUILayout.EndHorizontal();

                GUILayout.Space(8);
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        static int SetConstrain(int oldConstrain)
        {
            GUIContent[] constrainGrid = new GUIContent[]
            {
                new GUIContent(SEO_GraphWindow.constrainOpStr[0], Localizer.Format("#LOC_SHIPENGOP_tooltips_constrainDv")),
                new GUIContent(SEO_GraphWindow.constrainOpStr[1], Localizer.Format("#LOC_SHIPENGOP_tooltips_constrainTwr"))
            };
            //int xSelection = GUILayout.SelectionGrid(oldConstrain, SEO_GraphWindow.constrainOpStr, 2);
            int xSelection = GUILayout.SelectionGrid(oldConstrain, constrainGrid, 2);
            if (xSelection != oldConstrain)
            {
                return xSelection;
            }

            return oldConstrain;
        }

        public static HashSet<string> GetFuelFilter(HashSet<string> selectedFuelOld)
        {

            foreach (var fuel in SEO_Main.fuelTypes)
            {
                //if (fuel == "None") continue;
                if (!SEO_Main.groupedEngines.ContainsKey(fuel)) { continue; }
                else if (SEO_Main.engineData[SEO_Main.groupedEngines[fuel].FirstOrDefault()].wdr == 1f) { continue; }

                bool wasSelected = selectedFuelOld.Contains(fuel);
                //bool isSelectedNow = GUILayout.Toggle(wasSelected, fuel);
                string displayName = SEO_Main.propellantDisplayNames.ContainsKey(fuel) ? SEO_Main.propellantDisplayNames[fuel] : fuel;
                bool isSelectedNow = GUILayout.Toggle(wasSelected, displayName);

                if (isSelectedNow && !wasSelected)
                    selectedFuelOld.Add(fuel);
                else if (!isSelectedNow && wasSelected)
                    selectedFuelOld.Remove(fuel);
            }

            return selectedFuelOld;
        }

        //static void FindOptimalStaging(float dvSum, StageConfig stage0, StageConfig stage1, List<StageConfig> stageListInternal)
        //{
        //    List<float> domains = new List<float> { 0f };
        //    for (int i = 1; i < division; i++)
        //    {
        //        domains.Add(i * (dvSum / division));
        //    }
        //    domains.Add(dvSum - 0.1f);

        //    //List<StageConfig> stageListInternal = new List<StageConfig> { stage0, stage1 };
        //    List<(float dv, float totalMass)> minimunList = new List<(float, float)>();

        //    for (int iter = 0; iter < division; iter++)
        //    {
        //        var stageZero = GetOptimalStageZero(dvSum, domains[iter], domains[iter+1], stage0, stage1, stageListInternal);
        //        minimunList.Add(stageZero);
        //    }

        //    float optimalStageZeroDv = Mathf.Floor(minimunList.OrderBy(s => s.totalMass).First().dv);
        //    stage0.targetDv = optimalStageZeroDv;
        //    stage1.targetDv = dvSum - optimalStageZeroDv;

        //    Debug.Log("[ShipEngineOp] dv minimum: " + string.Join(", ", minimunList.Select(s => s.dv)));
        //    Debug.Log("[ShipEngineOp] mass minimum: " + string.Join(", ", minimunList.Select(s => s.totalMass)));
        //}

        //static (float, float) GetOptimalStageZero(float dvSum, float left, float right, StageConfig stage0, StageConfig stage1, List<StageConfig> mockStages)
        //{
        //    float tolerance = 0.1f;

        //    while (right - left > tolerance)
        //    {
        //        float stage0_dv0 = left + (right - left) / 3;
        //        float stage0_dv1 = right - (right - left) / 3;

        //        stage0.targetDv = stage0_dv0;
        //        stage1.targetDv = dvSum - stage0_dv0;
        //        float totalMass_0 = GetTotalWeight(mockStages);

        //        stage0.targetDv = stage0_dv1;
        //        stage1.targetDv = dvSum - stage0_dv1;
        //        float totalMass_1 = GetTotalWeight(mockStages);

        //        if (totalMass_0 < totalMass_1)
        //        {
        //            right = stage0_dv1; // Minimum is in the left third
        //        }
        //        else
        //        {
        //            left = stage0_dv0; // Minimum is in the right third
        //        }
        //    }

        //    float optimalDv = (left + right) / 2;
        //    stage0.targetDv = optimalDv;
        //    stage1.targetDv = dvSum - optimalDv;

        //    float minTotalMass = GetTotalWeight(mockStages);

        //    return (optimalDv,  minTotalMass);
        //}

        public static void PsoSelectedStages(List<StageConfig> optimizingStages, List<StageConfig> mockStageList)
        {
            float totalDv = optimizingStages.Sum(s => s.targetDv);       
            if (totalDv <= 0) { return; }
            //if (optimizingStages.Sum(s => s.targetDv) <= 0) { return; }

            // Define PSO parameters
            int totalVariables = optimizingStages.Count();
            int numParticles = 50;
            int maxIterations = 1000; // Max iterations still acts as a safeguard
            float inertiaWeight = 0.729f; // Common value for w
            float cognitiveWeight = 1.49445f; // Common value for c1
            float socialWeight = 1.49445f; // Common value for c2

            // Convergence parameters
            float fitnessTolerance = 1e-3f; // Stop if fitness changes by less than this
            int consecutiveConvergedIterations = 10; // For this many consecutive iterations

            Debug.Log("Starting Particle Swarm Optimization...");
            Debug.Log($"Target: Minimize f(x1) + ... + f(x{totalVariables}) where f(x) = x + 1/x");
            Debug.Log($"Constraint: x1 + ... + x{totalVariables} = {totalDv}");
            Debug.Log($"Stopping criterion: Fitness change < {fitnessTolerance:E1} for {consecutiveConvergedIterations} consecutive iterations (or max {maxIterations} iterations).");
            Debug.Log("--------------------------------------------------");

            PsoOptimizer optimizer = new PsoOptimizer(totalVariables, numParticles, maxIterations,
                                                    inertiaWeight, cognitiveWeight, socialWeight,
                                                    totalDv, fitnessTolerance, consecutiveConvergedIterations);

            optimizer.Optimize(optimizingStages, mockStageList);

            Debug.Log("\nOptimization Complete!");
            Debug.Log("--------------------------------------------------");
            Debug.Log($"Global Best Fitness Found: {optimizer.GlobalBestFitness:F6}");


            // Collect all N variables for display
            float sumOfIndependent = 0f;
            for (int i = 0; i < optimizer.GlobalBestPosition.Length; i++)
            {
                optimizingStages[i].targetDv = optimizer.GlobalBestPosition[i];
            }
            float totalWeight = GetTotalWeight(mockStageList);
            var engineList = optimizingStages.Select(s => s.engineName).ToList();
            for (int i = 0; i < optimizer.GlobalBestPosition.Length; i++)
            {
                var stage = optimizingStages[i];
                if (stage.manualMode || stage.keepEngine)
                {
                    stage.targetDv = Mathf.Round(optimizer.GlobalBestPosition[i]);
                }
                else
                {
                    stage.targetDv = Mathf.Floor(optimizer.GlobalBestPosition[i]);
                    stage.GetBestEngine();

                    if (stage.engineName != engineList[i])
                    { stage.targetDv += 1f; }
                }
                totalWeight = GetTotalWeight(mockStageList);
                sumOfIndependent += stage.targetDv;
            }
            //for (int i = 0; i < optimizer.GlobalBestPosition.Length; i++)
            //{
            //    optimizingStages[i].targetDv = Mathf.Floor(optimizer.GlobalBestPosition[i]);
            //    sumOfIndependent += optimizingStages[i].targetDv;
            //}

            // Add the derived N-th variable
            optimizingStages.Last().targetDv = totalDv - sumOfIndependent;
            //Debug.Log($"sum of indepandents {sumOfIndependent:F6}");
            //Debug.Log($"total dv {totalDv:F6}");

            for (int i = 0; i < optimizingStages.Count; i++)
            {
                Debug.Log($"Optimal x{i + 1}: {optimizingStages[i].targetDv:F6}");
            }

            //List<float> finalVariables = new List<float>();
            //float sumOfIndependent = 0f;
            //for (int i = 0; i < optimizer.GlobalBestPosition.Length; i++)
            //{
            //    finalVariables.Add(optimizer.GlobalBestPosition[i]);
            //    sumOfIndependent += optimizer.GlobalBestPosition[i];
            //}

            //finalVariables.Add(totalDv - sumOfIndependent);

            //for (int i = 0; i < finalVariables.Count; i++)
            //{
            //    optimizingStages[i].targetDv = finalVariables[i];
            //    Debug.Log($"Optimal x{i + 1}: {finalVariables[i]:F6}");
            //}
            //Debug.Log($"Sum of all variables: {finalVariables.Sum():F6} (should be close to {totalDv})");
        }

        public static float GetTotalWeight(List<StageConfig> mockStages)
        {
            for (int idx = 0; idx < mockStages.Count; idx ++)
            {
                if (mockStages[idx].keepEngine)
                {
                    mockStages[idx].nextStageEngineQty = mockStages[idx-1].engineQty;
                    mockStages[idx].engineName = mockStages[idx-1].engineName;
                }

                mockStages[idx].GetBestEngine();

                if (idx >= mockStages.Count - 1) { break; }
                mockStages[idx + 1].payloadMass = mockStages[idx].stageWeight;
            }
            //Debug.Log("stage 0 dv = " + stageList.First().targetDv.ToString());
            //Debug.Log("stage 0 mass = " + stageList.First().stageWeight.ToString());
            return mockStages.Last().stageWeight;
        }
    }
}
