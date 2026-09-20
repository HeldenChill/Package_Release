using UnityEngine;
namespace Dynamic.WorldInterface.Sensor
{
    using Gameplay.Character.WorldInterface;
    public class DetectCorner3DSensor : BaseSensor
    {
        [SerializeField] float distance;
        [SerializeField] protected Transform cornerCheckAbove;
        [SerializeField] protected Transform cornerCheckBelow;
        public override void UpdateState()
        {
            Data.IsTouchingVaultCorner = !Physics.Raycast(new Ray(cornerCheckAbove.position, cornerCheckAbove.forward), distance, layer)
            && Physics.Raycast(new Ray(cornerCheckBelow.position, cornerCheckBelow.forward), distance, layer);
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(cornerCheckBelow.position, cornerCheckBelow.position + cornerCheckBelow.forward * distance);
            Gizmos.DrawLine(cornerCheckAbove.position, cornerCheckAbove.position + cornerCheckAbove.forward * distance);
        }
    }
}