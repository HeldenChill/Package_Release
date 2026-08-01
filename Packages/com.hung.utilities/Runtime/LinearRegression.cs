using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace Hung.Utilities
{
    public class LinearRegression
    {
        private float slope;
        private float yIntercept;

        public void CalculateRegression(List<Vector2> data)
        {
            int n = data.Count;
            float sumX = 0;
            float sumY = 0;
            float sumXY = 0;
            float sumX2 = 0;

            foreach (Vector2 p in data)
            {
                sumX += p.x;
                sumY += p.y;
            }
          
            float meanX = sumX / n;
            float meanY = sumY / n;

            foreach (Vector2 p in data)
            {
                sumXY += (p.x - meanX) * (p.y - meanX);
                sumX2 += Mathf.Pow(p.x - meanX, 2);
            }
            if (sumX2 != 0)
                slope = sumXY / sumX2;
            else
                slope = 0;
            yIntercept = meanY - slope * meanX;
        }

        public float PredictY(float x)
        {
            return slope * x + yIntercept;
        }
    }
}