using System.Collections.Generic;
using System;
using UnityEngine;

namespace Hung.Base
{
    public abstract class BaseTargetDetection<T>
    {

        /// <summary>
        /// Checks Target In Circle Shape
        /// </summary>
        /// <param name="position">
        /// Circle Position
        /// </param>
        /// <param name="radius">
        /// Circle Radious
        /// </param>
        /// <param name="targets">
        /// Returned Targets
        /// </param>
        public abstract void CheckTargets(Vector3 position, float radius, ref List<T> targets);

        /// <summary>
        /// Check Targets In Box Shape
        /// </summary>
        /// <param name="position">
        /// Box Position
        /// </param>
        /// <param name="size">
        /// Box Size
        /// </param>
        /// <param name="angle">
        /// Box Angle
        /// </param>
        /// <param name="targets">
        /// Returned Targets
        /// </param>
        public abstract void CheckTargets(Vector3 position, Vector3 size, Quaternion angle, ref List<T> targets);
        public abstract void CheckTargets(ref List<T> targets);

    }
}
