using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using RealFuels;
using RealFuels.TechLevels;
using UnityEngine;



namespace ShipEngineOptimization
{
    public class StageConfig : IHasAtmData
    {
        //public int index;
        
        // input
        public float payloadMass;        
        public float targetDv;
        public float targetTwr;
        public float additionalMass;
        public int nextStageEngineQty;

        public float altitude { get; set; }
        public string planetNameStr { get; set; }
        public int constrainOp = 0; // 0 = constrain dv; 1 = constrain twr
        public int qtyFilter = 9;
        public bool filterT = false;
        public bool strictMode = false;
        public bool manualMode = false;
        public bool keepEngine = false;
        public bool includeInOp = false;

        // output
        public string engineName;
        public float stageWeight;
        public int engineQty;
        public float trueDv { get; set; }
        public float trueTwr { get; set; }

        // internal
        private float surfaceGravity = SEO_Main.standardGravity;

        public StageConfig()
        {
            targetDv = 2400;
            targetTwr = 1f;
            additionalMass = 0f;

            SetPlanet(Planetarium.fetch.Home.bodyName);
            engineName = SEO_Main.emptyStr;
        }

        public StageConfig(float payloadMassInput, float targetTwrInput, float altitudeInput, string planetNameInput, int qtyFilterInput, bool filterTInput, bool strictModeInput)
        {
            payloadMass = payloadMassInput;
            targetTwr = targetTwrInput;
            SetPlanet(planetNameInput);
            altitude = altitudeInput;
            qtyFilter = qtyFilterInput;
            filterT = filterTInput;
            strictMode = strictModeInput;

            engineName = SEO_Main.emptyStr;
        }

        public StageConfig CloneStageWithPayload(float payload)
        {
            return new StageConfig(payload, this.targetTwr, this.altitude, this.planetNameStr, this.qtyFilter, this.filterT, this.strictMode);
        }

        public void SetManualSelEngine(string engineNameInput)
        {
            engineName = engineNameInput;
            GetBestEngine();
        }

        public void SetPlanet(string planetInput)
        {
            planetNameStr = planetInput;
            var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == planetInput);
            surfaceGravity = (float)(planet.gravParameter / (planet.Radius * planet.Radius));
            altitude = (float)planet.atmosphereDepth;
        }

        void ResetShipSpecs()
        {
            engineQty = 0;
            trueDv = 0;
            trueTwr = 0;
        }


        public void GetBestEngine()
        {
            if (!SEO_Main.engineData.ContainsKey(engineName))
            {
                //Debug.Log("career avtivated");
                engineName = SEO_Main.emptyStr;
            }

            if (targetDv == 0)
            {
                if (!manualMode) { engineName = SEO_Main.emptyStr; }
                stageWeight = payloadMass;
                ResetShipSpecs();
                return;
            }
            else if (payloadMass == float.MaxValue || payloadMass == 0)
            {
                if (!manualMode) { engineName = SEO_Main.emptyStr; }
                stageWeight = payloadMass;
                ResetShipSpecs();
                return;
            }

            float truePayload = payloadMass + additionalMass;

            var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == planetNameStr);
            if (planet == null) return;

            float atm = (float)FlightGlobals.getStaticPressure(altitude, planet) / SEO_Main.standardAtm;
            (string Name, float weight, float wdr, float weightTotal, float twr, float dv, float fuelMass, int qtyEng) shipSpecs;

            if (manualMode || keepEngine)
            {
                shipSpecs = GetShipSpecs(engineName, truePayload, atm);
            }
            else
            {
                shipSpecs = GetTopEngineList(SEO_StageWindow.selectedFuelGlobal).FirstOrDefault();
            }

            if (shipSpecs.weightTotal == float.MaxValue || shipSpecs.weightTotal == 0)
            {
                engineName = shipSpecs.Name != null ? shipSpecs.Name : SEO_Main.emptyStr;
                stageWeight = float.MaxValue;
                ResetShipSpecs();
            }
            else
            {
                engineName = shipSpecs.Name;
                stageWeight = shipSpecs.weightTotal;
                engineQty = shipSpecs.qtyEng;

                trueDv = constrainOp == 0 ? targetDv : shipSpecs.dv;
                trueTwr = constrainOp == 1 ? targetTwr : shipSpecs.twr;
            }
        }

        public List<(string Name, float weight, float wdr, float weightTotal, float twr, float dv, float fuelMass, int qtyEng)> GetTopEngineList(HashSet<string> selectedFuel)
        {
            float truePayload = payloadMass + additionalMass;

            var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == planetNameStr);

            float atm = (float)FlightGlobals.getStaticPressure(altitude, planet) / SEO_Main.standardAtm;

            return selectedFuel
                .Where(fuel => SEO_Main.groupedEngines.ContainsKey(fuel))
                .SelectMany(fuel => SEO_Main.groupedEngines[fuel])
                .Distinct()
                .Where(engineName => SEO_Main.engineData.ContainsKey(engineName))
                .Where(engineName => !SEO_Main.blacklistEngines.Contains(engineName))
                .Select(engineName => GetShipSpecs(engineName, truePayload, atm))
                .Where(e => e.weightTotal < float.MaxValue)
                //.Where(e => e.weightTotal > 0)
                .Where(e => !filterT || e.qtyEng <= qtyFilter)
                .OrderBy(e => constrainOp == 0 ? e.weightTotal : e.fuelMass)
                .Take(10)
                .ToList();
        }

        public (string Name, float weight, float wdr, float weightTotal, float twr, float dv, float fuelMass, int qtyEng) GetShipSpecs(string engineName, float truePayload, float atm)
        {
            var data = SEO_Main.engineData[engineName];

            //float realIsp = GetIsp(data.internalName, atm, data.modeIdx);
            //float realThrust = (realIsp / data.isp) * data.thrust;

            (float realIsp, float realThrust) = GetIspAndThrust(engineName, data.internalName, atm, data.modeIdx);

            //Debug.Log("isp = " + realIsp.ToString());
            //Debug.Log("thrust = " + realThrust.ToString());

            //float wdr = (SEO_Main.customWdr.ContainsKey(engineName) ? SEO_Main.customWdr[engineName].toggle : false) ? SEO_Main.customWdr[engineName].customValue : data.wdr;
            float wdr = data.wdr;
            float realWeight = SEO_Main.additionalWeight[engineName].toggle ? SEO_Main.additionalWeight[engineName].addiWeight + data.weight : data.weight;
            if (keepEngine) { truePayload += -(nextStageEngineQty * realWeight); }            
            float weightTotal = GetWeightTotal(realWeight, realThrust, realIsp, wdr, truePayload, targetDv, targetTwr);
            int qtyEng = GetEngQty(realWeight, realThrust, realIsp, wdr, truePayload, targetDv, targetTwr);
            float fuelMass = (weightTotal - qtyEng * realWeight - truePayload) * (1 - 1 / wdr);
            float twr = realThrust * qtyEng / (weightTotal * surfaceGravity);
            float dv = SEO_Main.standardGravity * realIsp * Mathf.Log(wdr / (1f + (((payloadMass + realWeight * qtyEng) / weightTotal) * (wdr - 1))));

            return (engineName, realWeight, wdr, weightTotal, twr, dv, fuelMass, qtyEng);
        }


        //float GetIsp(string internalName, float atm, int modeIdx)
        //{
        //    var part = PartLoader.LoadedPartsList.FirstOrDefault(p => p.name == internalName);
        //    if (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX"))
        //    {
        //        if ( modeIdx == 0)
        //        {               
        //            var engineModule = part?.partPrefab?.Modules.GetModule<ModuleEngines>();

        //            if (part == null || engineModule == null) { return 0f; }

        //            //Debug.Log(internalName);
        //            //Debug.Log(part.title);
        //            //Debug.Log("atm = " + atm.ToString());
        //            return engineModule.atmosphereCurve.Evaluate(atm);
        //        }
        //        else
        //        {
        //            var multiModeModule = part.partPrefab.Modules.GetModule<MultiModeEngine>();

        //            string engineMode = modeIdx == 1 ? multiModeModule.primaryEngineID : multiModeModule.secondaryEngineID;

        //            //Debug.Log(part.title);
        //            var engineModule = part.partPrefab.FindModulesImplementing<ModuleEngines>().FirstOrDefault(e => e.engineID == engineMode);
        //            //Debug.Log(engineMode);
        //            //Debug.Log(engineModule.atmosphereCurve.Evaluate(atm));
        //            return engineModule.atmosphereCurve.Evaluate(atm);
        //        }
        //    }
        //    else
        //    { return GetIspRF(part, atm); }
        //}

        (float isp, float thrust) GetIspAndThrust(string name, string internalName, float atm, int modeIdx)
        {
            var part = PartLoader.LoadedPartsList.FirstOrDefault(p => p.name == internalName);
            float realIsp;
            float realThrust;
            if (part.partPrefab.Modules.Contains("ModuleEngines") || part.partPrefab.Modules.Contains("ModuleEnginesFX"))
            {
                if (modeIdx == 0)
                {
                    var engineModule = part?.partPrefab?.Modules.GetModule<ModuleEngines>();

                    if (part == null || engineModule == null) { return (0f, 0f); }

                    //Debug.Log(internalName);
                    //Debug.Log(part.title);
                    //Debug.Log("atm = " + atm.ToString());
                    realIsp = engineModule.atmosphereCurve.Evaluate(atm);
                    realThrust = (realIsp / engineModule.atmosphereCurve.Evaluate(0)) * engineModule.maxThrust;
                    return (realIsp, realThrust);
                }
                else
                {
                    var multiModeModule = part.partPrefab.Modules.GetModule<MultiModeEngine>();

                    string engineMode = modeIdx == 1 ? multiModeModule.primaryEngineID : multiModeModule.secondaryEngineID;

                    //Debug.Log(part.title);
                    var engineModule = part.partPrefab.FindModulesImplementing<ModuleEngines>().FirstOrDefault(e => e.engineID == engineMode);
                    //Debug.Log(engineMode);
                    //Debug.Log(engineModule.atmosphereCurve.Evaluate(atm));
                    realIsp = engineModule.atmosphereCurve.Evaluate(atm);
                    realThrust = (realIsp / engineModule.atmosphereCurve.Evaluate(0)) * engineModule.maxThrust;
                    return (realIsp, realThrust);
                }
            }
            else
            { return GetIspAndThrustRF(name, part, atm, modeIdx); }
        }

        (float isp, float thrust) GetIspAndThrustRF(string name, AvailablePart part, float atm, int modeIdx)
        {
            var moduleConfig = part.partPrefab.Modules.GetModule<ModuleEngineConfigs>();
            var config = moduleConfig.configs[modeIdx];
            float realIsp;
            float realThrust;
            float maxIsp;
            //var config = part.partPrefab.Modules.GetModule<ModuleEngineConfigs>().configs[modeIdx];
            ConfigNode atmosphereCurveNode = config.GetNode("atmosphereCurve");
            if (atmosphereCurveNode != null)
            {
                FloatCurve atmCurve = new FloatCurve();
                atmCurve.Load(atmosphereCurveNode);

                maxIsp = atmCurve.Evaluate(0);
                realIsp = atmCurve.Evaluate(atm);

            }
            else
            {
                TechLevel cTL = new TechLevel();
                ConfigNode techNodes = moduleConfig.techNodes;

                if (!cTL.Load(config, techNodes, moduleConfig.engineType, moduleConfig.techLevel))
                    cTL = null;

                float.TryParse(config.GetValue("IspV"), out float ispV);
                float.TryParse(config.GetValue("IspSL"), out float ispSL);

                FloatCurve newAtmoCurve = Utilities.Mod(cTL.AtmosphereCurve, ispSL, ispV);

                maxIsp = newAtmoCurve.Evaluate(0);
                realIsp = newAtmoCurve.Evaluate(atm);
            }
            realThrust = SEO_Main.engineData[name].thrust * realIsp / maxIsp;

            return (realIsp,  realThrust);
        }

        float GetIspRF (AvailablePart part, float atm)
        {
            var engineModuleRF = part?.partPrefab?.Modules.GetModule<ModuleEnginesRF>();
            return engineModuleRF.atmCurveIsp.Evaluate(atm);
        }

        float GetWeightTotal(float weightEng, float thrust, float isp, float wdr, float payload, float dv, float twr)
        {
            if (twr > thrust / (weightEng * surfaceGravity) || dv >= SEO_Main.DvLimit(weightEng, thrust, isp, wdr, twr, surfaceGravity))
            { return float.MaxValue; }

            int qtyEng = GetEngQty(weightEng, thrust, isp, wdr, payload, dv, twr);

            switch (constrainOp)
            {
                default: // constrain dv
                    float e = Mathf.Exp(dv / (SEO_Main.standardGravity * isp));
                    float weightTotal = (payload + qtyEng * weightEng) * (1 + (1 - e) / (e / wdr - 1));
                    return weightTotal;
                case 1: // constrain TWR
                    return thrust * qtyEng / (twr * surfaceGravity);
            }
        }

        int GetEngQty(float weightEng, float thrust, float isp, float wdr, float payload, float dv, float twr)
        {
            if (twr == 0) { return 1; }

            float e = Mathf.Exp(dv / (SEO_Main.standardGravity * isp));
            float a = thrust / (surfaceGravity * twr);
            float b = (1 - wdr / e) / (1 - wdr);
            float c = payload / (a * b - weightEng);
            if (!strictMode)
            { return Mathf.RoundToInt(c) > 0 ? Mathf.RoundToInt(c) : 1; }
            return Mathf.CeilToInt(c);
            //return Mathf.CeilToInt(c) > 0 ? Mathf.CeilToInt(c) : 1;
        }
    }
}
