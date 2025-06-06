using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEngineOptimization
{
    public class SEO_Functions
    {
        public static float standardGravity = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
        public static float surfaceGravity = standardGravity;
        public static float DvLimit(float weight, float thrust, float isp, float wdr, float xTWR)
        {
            float a = 1 / ((thrust / (weight * surfaceGravity * xTWR)) - 1);
            float b = isp * standardGravity * UnityEngine.Mathf.Log((1 + a) / ((1 / wdr) + a));

            return b;
        }

        public static float WeightToPayload(float weight, float thrust, float isp, float wdr, float dv, float twr)
        {
            float a = UnityEngine.Mathf.Exp(dv / (standardGravity * isp));
            float b = ((1 / a - 1 / wdr) / (1 - 1 / wdr));
            float c = (1 / (b - (weight * surfaceGravity * twr) / (thrust)));

            return c;
        }

        public static float TotalWeightFromDv(float weightEng, float thrust, float isp, float wdr, float dv, float payload, int qtyEng)
        {
            //if (wdr == 1)
            //{
            //    return 0.0f;
            //}
            //else
            //{

            //}
            float a = (payload + (float)qtyEng * weightEng) * (1 + wdr / ((wdr - 1) / (Mathf.Exp(dv / (standardGravity * isp)) - 1) - 1));
            return a;
        }

        public static float DvFromTwr(float weightEng, float thrust, float isp, float wdr, float totalWeight, float payload, int numEng)
        {
            float a = (totalWeight - payload - numEng * weightEng) * (1 - 1 / wdr);
            float b = isp * standardGravity * Mathf.Log(1 / (1 - a / totalWeight));
            return b;
        }

        public static int EngQtyUpper(float weightEng, float thrust, float isp, float wdr, float payload, float dv, float twr)
        {
            if (twr == 0) { return 1; }

            float e = Mathf.Exp( dv / (standardGravity *isp) );
            float a = thrust / (surfaceGravity * twr);
            float b = (1 - wdr / e) / (1 - wdr);
            float c = payload / (a * b - weightEng);
            return Mathf.CeilToInt(c);
        }

        public static List<float> TickGen(float min, float max)
        {
            int expo = Mathf.FloorToInt(Mathf.Log10(max - min));
            float sig = (max - min) / Mathf.Pow(10, expo);

            List<float> tickList = new List<float>{0};

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
    }
}
