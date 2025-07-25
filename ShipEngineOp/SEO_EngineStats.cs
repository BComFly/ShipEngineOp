using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;


namespace ShipEngineOptimization
{
    public class EngineStats : IHasAtmData
    {
        public string engineName { get; set; }
        public float realThrust { get; set; }
        public float effctiveWeight { get; set; }
        public float realIsp { get; set; }
        public float wdr { get; set; }

        public float altitude { get; set; }
        public string planetNameStr { get; set; }
        public int engQtyMin { get; set; }
        public int engQtyMax { get; set; }
        public int[] engQtyNet { get; set; }
        public float[] allPtsX { get; set; }
        public float[] allPtsTotalWeight { get; set; }
        public float surfaceGravity { get; set; }

        private (float weight, float thrust, float isp, float wdr, string internalName, int modeIdx) engineData;

        public EngineStats()
        {
            engineName = SEO_Main.emptyStr;
            SetEngineData(engineName);
            SetPlanet(Planetarium.fetch.Home.bodyName);
            engQtyMin = 1;
            engQtyMax = 3;
        }

        public void SetPlanet(string planetInput)
        {
            planetNameStr = planetInput;
            var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == planetInput);
            surfaceGravity = (float)(planet.gravParameter / (planet.Radius * planet.Radius));
            altitude = (float)planet.atmosphereDepth;
        }

        public void SetEngineData(string nameInput)
        {
            engineName = nameInput;
            engineData = SEO_Main.engineData[engineName];
            effctiveWeight = SEO_Main.additionalWeight[engineName].toggle ? (engineData.weight + SEO_Main.additionalWeight[engineName].addiWeight) : engineData.weight;
            realThrust = engineData.thrust;
            realIsp = engineData.isp;
            wdr = engineData.wdr;
        }

        public void UpdateEngineData()
        {
            effctiveWeight = SEO_Main.additionalWeight[engineName].toggle ? (engineData.weight + SEO_Main.additionalWeight[engineName].addiWeight) : engineData.weight;
            SetIspAndThrust();
        }

        public void CalcTotalWeightAndTwrFromDv(float dv, float payload)
        {
            allPtsX = new float[engQtyNet.Length];
            allPtsTotalWeight = new float[engQtyNet.Length];

            allPtsTotalWeight = engQtyNet
                .Select(qty => (payload + (float)qty * effctiveWeight) * (1 + wdr / ((wdr - 1) / (Mathf.Exp(dv / (SEO_Main.standardGravity * realIsp)) - 1) - 1)))
                .ToArray();
            //float a = (payload + (float)engQty * effctiveWeight) * (1 + wdr / ((wdr - 1) / (Mathf.Exp(dv / (SEO_CommonFunctions.standardGravity * realIsp)) - 1) - 1));

            //return totalWeightNet;

            for (int i = 0; i < engQtyNet.Length; i++)
            {
                allPtsX[i] = engQtyNet[i] * realThrust / (allPtsTotalWeight[i] * surfaceGravity);
            }
        }

        public void CalcTotalWeightAndDvFromTwr(float twr, float payload)
        {
            allPtsX = new float[engQtyNet.Length];
            allPtsTotalWeight = new float[engQtyNet.Length];

            allPtsTotalWeight = engQtyNet
                .Select(qty => ((qty * realThrust) / (surfaceGravity * twr)))
                .ToArray();
            for (int i = 0; i < engQtyNet.Length; i++)
            {
                float a = (allPtsTotalWeight[i] - payload - engQtyNet[i] * effctiveWeight) * (1 - 1 / wdr);
                allPtsX[i] = realIsp * SEO_Main.standardGravity * Mathf.Log(1 / (1 - a / allPtsTotalWeight[i]));
            }
        }

        public float GetOptimalTwr()
        {
            float c = 1 / (2 * (Mathf.Sqrt(wdr) + 1));
            float engineTwr = realThrust / (effctiveWeight * surfaceGravity);
            float opTwr = c * engineTwr;
            return opTwr;
        }

        public float GetOptimalDv()
        {
            float vesselWdr = 1 / (0.5f + 1 / (2 * Mathf.Sqrt(wdr)));
            float opDv = SEO_Main.standardGravity * realIsp * Mathf.Log(vesselWdr) * 2;
            return opDv;
        }

        float GetCriticalDv(float twr)
        {
            float standarizedTwr = twr / (realThrust / (effctiveWeight * surfaceGravity));
            float inverseVesselWdr = 1 + standarizedTwr * (wdr - 1) - (wdr - 1) * Mathf.Sqrt(standarizedTwr * (1/wdr + standarizedTwr * (1 - 1/wdr)));
            return 2 * SEO_Main.standardGravity * realIsp * Mathf.Log(1 / inverseVesselWdr);
        }

        void SetIspAndThrust()
        {
            var planet = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == planetNameStr);
            if (planet == null) return;

            float atm = (float)FlightGlobals.getStaticPressure(altitude, planet) / SEO_Main.standardAtm;

            var part = PartLoader.LoadedPartsList.FirstOrDefault(p => p.name == engineData.internalName);
            if (engineData.modeIdx == 0)
            {
                var engineModule = part?.partPrefab?.Modules.GetModule<ModuleEngines>();
                if (part == null || engineModule == null) { realIsp = 0f; }
                realIsp = engineModule.atmosphereCurve.Evaluate(atm);
            }
            else
            {
                var multiModeModule = part.partPrefab.Modules.GetModule<MultiModeEngine>();
                string engineMode = engineData.modeIdx == 1 ? multiModeModule.primaryEngineID : multiModeModule.secondaryEngineID;
                var engineModule = part.partPrefab.FindModulesImplementing<ModuleEngines>().FirstOrDefault(e => e.engineID == engineMode);
                realIsp = engineModule.atmosphereCurve.Evaluate(atm);
            }
            realThrust = engineData.thrust * (realIsp / engineData.isp);
        }

        public float[] CalcDvLimit(float[] x)
        {
            float[] y = x.Select(e => SEO_Main.DvLimit(effctiveWeight, realThrust, realIsp, wdr, e, surfaceGravity)).ToArray();
            return y;
        }

        public float[] CalcCriticalDv(float[] x)
        {
            return x.Select(twr => GetCriticalDv(twr)).ToArray();
        }
    }
}
