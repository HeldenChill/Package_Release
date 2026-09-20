using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Character.Physic
{
    public class PhysicParameter : Parameter
    {
        public const float GRAVITY = -9.81f;
        private readonly Vector2 GRAVITY_VECTOR = new Vector2(0, GRAVITY);
        public Vector2 GravityVector => GRAVITY_VECTOR * GravityParameter;
        public float GravityParameter = 1f;
        public float JumpHeight = 2f;
    }
}