using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEngineOptimization
{
    public class SEO_Functions
    {
        public const float sGravi = 9.80665f;
        public static float DvLimit(float weight, float thrust, float isp, float wdr, float xTWR)
        {
            float a = 1 / ((thrust / (weight * sGravi * xTWR)) - 1);
            float b = isp * sGravi * UnityEngine.Mathf.Log((1 + a) / ((1 / wdr) + a));

            return b;
        }

        public static float WeightToPayload(float weight, float thrust, float isp, float wdr, float dv, float twr)
        {
            float a = UnityEngine.Mathf.Exp(dv / (sGravi * isp));
            float b = ((1 / a - 1 / wdr) / (1 - 1 / wdr));
            float c = (1 / (b - (weight * sGravi * twr) / (thrust)));

            return c;
        }

        public static float TotalWeightFromDv(float weightEng, float thrust, float isp, float wdr, float dv, float payload, int numEng)
        {
            //if (wdr == 1)
            //{
            //    return 0.0f;
            //}
            //else
            //{

            //}
            float a = (payload + (float)numEng * weightEng) * (1 + wdr / ((wdr - 1) / (Mathf.Exp(dv / (sGravi * isp)) - 1) - 1));
            return a;
        }

        public static float DvFromTwr(float weightEng, float thrust, float isp, float wdr, float totalWeight, float payload, int numEng)
        {
            float a = (totalWeight - payload - numEng * weightEng) * (1 - 1 / wdr);
            float b = isp * sGravi * Mathf.Log(1 / (1 - a / totalWeight));
            return b;
        }

        public static List<float> TickGen(float min, float max)
        {
            float expo = Mathf.Floor(Mathf.Log10(max - min));

            float sig = (max - min) / Mathf.Pow(10, expo);

            List<float> tickList = new List<float>{0};
            //tickList.Clear();

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

            //for (int i = 0; tickList[tickList.Count - 1] < max; i++)
            //{
            //    float a = tickList[tickList.Count - 1] + spaces * Mathf.Pow(10, expo);
            //    tickList.Add(a);
            //}

            //do
            //{
            //    float a = tickList[tickList.Count - 1] + spaces * Mathf.Pow(10, expo);
            //    tickList.Add(a);
            //}
            //while (tickList[tickList.Count - 1] < max);

            while (tickList[tickList.Count - 1] < max)
            {
                float a = tickList[tickList.Count - 1] + spaces * Mathf.Pow(10, expo);
                //if (a <= max)
                tickList.Add(a);
            }
            
            List<float> filteredList = tickList.Where(x => x >= min && x <= max).ToList();

            return filteredList;
        }
    }
}
