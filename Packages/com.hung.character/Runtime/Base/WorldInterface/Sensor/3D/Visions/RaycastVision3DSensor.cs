using System.Collections.Generic;
using Gameplay.Character.WorldInterface;
using UnityEngine;
using Utilities;

namespace Dynamic.WorldInterface.Sensor
{
    public class RaycastVision3DSensor : Vision3DSensor
    {
        [SerializeField]
        protected Transform eyeTransform;
        [SerializeField]
        protected float rayEyeDistance = 1;
        [SerializeField]
        protected Transform forwardTf;
        [SerializeField]
        protected float angleDeg;
        [SerializeField]
        protected int rayCount;
        protected List<RaycastHit> raycastHits;
        protected List<RaycastHit> wallHits;
        protected List<RaycastHit> characterHits;
        protected List<DATA_TYPE> hitTypes;
        public override void Initialize(WorldInterfaceData Data, WorldInterfaceParameter Parameter)
        {
            base.Initialize(Data, Parameter);
            raycastHits = new List<RaycastHit>();
            wallHits = new List<RaycastHit>();
            characterHits = new List<RaycastHit>();
            hitTypes = new List<DATA_TYPE>();
            Data.SeenColliders = new List<Collider>();
        }
        public override void UpdateState()
        {
            RaycastCone();
            UpdateData();
        }
        protected override void UpdateData()
        {
            characterHits.Clear();
            wallHits.Clear();
            hitTypes.Clear();
            Data.SeenColliders.Clear();
            for (int i = 0; i < rayCount; i++)
            {
                if (raycastHits[i].collider != null)
                {
                    if (raycastHits[i].collider.gameObject.layer == LayerMask.NameToLayer("Character"))
                    {
                        characterHits.Add(raycastHits[i]);
                        hitTypes.Add(DATA_TYPE.CHARACTER);
                        Data.SeenColliders.Add(raycastHits[i].collider);
                        if (!Data.SeenColliders.Contains(raycastHits[i].collider))
                        {
                            DevLog.Log(DevId.Gameplay, "SeeCharacter: " + raycastHits[i].collider.transform.position);
                        }
                    }
                    else if (raycastHits[i].collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                    {
                        wallHits.Add(raycastHits[i]);
                        hitTypes.Add(DATA_TYPE.GROUND);
                    }
                    else
                    {
                        hitTypes.Add(DATA_TYPE.UNKNOWN);
                    }
                }
                else
                {
                    hitTypes.Add(DATA_TYPE.NONE);
                }
            }
            Data.CharacterHit3D = characterHits.AsReadOnly();
            Data.WallHit3D = wallHits.AsReadOnly();
        }
        public bool RaycastCone()
        {
            raycastHits.Clear();
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = ConeDirectionFibonacci(forwardTf.forward, angleDeg, i, rayCount);
                if (Physics.Raycast(new Ray(eyeTransform.position + dir * rayEyeDistance - eyeTransform.forward * rayEyeDistance, dir),
                out RaycastHit tempHit, visionDistance + rayEyeDistance, layer))
                {
                    raycastHits.Add(tempHit);
                }
                else
                {
                    raycastHits.Add(default);
                }
            }
            return false;
        }
        protected Vector3 ConeDirectionFibonacci(Vector3 forward, float angleDeg, int index, int count)
        {
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float cosMax = Mathf.Cos(angleRad);

            // Evenly distribute z in [cosMax, 1]
            float t = (index + 0.5f) / count;
            float z = Mathf.Lerp(cosMax, 1f, t);

            float phi = index * 2.399963229728653f; // golden angle (rad)
            float r = Mathf.Sqrt(1f - z * z);

            Vector3 localDir = new Vector3(
                r * Mathf.Cos(phi),
                r * Mathf.Sin(phi),
                z
            );

            return Quaternion.FromToRotation(Vector3.forward, forward) * localDir;
        }
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Vector3 origin = eyeTransform.position;
            Vector3 forward = forwardTf.forward;

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 dir = ConeDirectionFibonacci(
                    forward,
                    angleDeg,
                    i,
                    rayCount
                );
                if (hitTypes != null && i < hitTypes.Count && hitTypes[i] != DATA_TYPE.NONE)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(origin + dir * rayEyeDistance
                    - eyeTransform.forward * rayEyeDistance, raycastHits[i].point);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(origin + dir * rayEyeDistance
                    - eyeTransform.forward * rayEyeDistance, dir * (visionDistance + rayEyeDistance));
                }

            }
        }
    }
}