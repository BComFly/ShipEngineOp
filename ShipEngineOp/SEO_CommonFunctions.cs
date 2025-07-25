using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEngineOptimization
{
    public class SEO_CommonFunctions
    {
        //public static float standardGravity = (float)(Planetarium.fetch.Home.gravParameter / (Planetarium.fetch.Home.Radius * Planetarium.fetch.Home.Radius));
        //public static float surfaceGravity = standardGravity;
        //public static float DvLimit(float weight, float thrust, float isp, float wdr, float xTWR, float surfaceGravity)
        //{
        //    float a = 1 / ((thrust / (weight * surfaceGravity * xTWR)) - 1);
        //    float b = isp * standardGravity * UnityEngine.Mathf.Log((1 + a) / ((1 / wdr) + a));

        //    return b;
        //}

        //public static List<float> TickGen(float min, float max)
        //{
        //    int expo = Mathf.FloorToInt(Mathf.Log10(max - min));
        //    float sig = (max - min) / Mathf.Pow(10, expo);

        //    List<float> tickList = new List<float>{0};

        //    float spaces;
        //    switch (sig)
        //    {
        //        case float x when x < 1.5 || x == 1:
        //            spaces = 0.25f;
        //            break;
        //        case float x when x >= 1.5 && x <= 3:
        //            spaces = 0.5f;
        //            break;
        //        case float x when x > 2 && x <= 6:
        //            spaces = 1.0f;
        //            break;
        //        default:
        //            spaces = 2.0f;
        //            break;
        //    }

        //    while (tickList[tickList.Count - 1] < max)
        //    {
        //        float a = tickList[tickList.Count - 1] + spaces * Mathf.Pow(10, expo);
        //        tickList.Add(a);
        //    }
            
        //    List<float> filteredList = tickList.Where(x => x >= min && x <= max).ToList();

        //    return filteredList;
        //}
    }
}
