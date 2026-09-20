using System.Collections.Generic;
using UnityEngine;

namespace _Game.Character
{
    public class ObjectRotation : MonoBehaviour
    {
        [SerializeField]
        protected Transform tf;
        [SerializeField]
        protected Transform headTf;
        [SerializeField]
        protected Transform anchor;
        [SerializeField]
        protected List<Transform> neckBones;   // Các bone cổ (theo thứ tự từ gốc -> đầu)
        [SerializeField]
        protected float smoothSpeed = 5f;  // Tốc độ mượt
        protected Vector3 targetPosition;
        protected Vector3 targetDirection;
        protected Quaternion targetQuaternion;
        [SerializeField]
        protected float maxYaw;
        [SerializeField]
        protected float maxPitch;
        void LateUpdate()
        {
            Vector3 euler;
            Quaternion headRotation = Quaternion.Slerp(
                    headTf.localRotation,
                    targetQuaternion,
                    Time.deltaTime * smoothSpeed
                );
            euler = LimitAngle(headRotation.eulerAngles);
            headTf.localRotation = Quaternion.Euler(euler);
            // Phân bổ xoay cho từng bone
            for (int i = 0; i < neckBones.Count; i++)
            {
                float factor = (i + 1f) / neckBones.Count; // ví dụ: 0.25, 0.5, 0.75, 1
                Quaternion desiredRotation = Quaternion.Slerp(
                    neckBones[i].rotation,
                    targetQuaternion,
                    Time.deltaTime * smoothSpeed * factor
                );
                // Chuyển sang Euler để clamp
                euler = desiredRotation.eulerAngles;


                euler = LimitAngle(euler);
                // Áp dụng rotation đã giới hạn
                neckBones[i].localRotation = Quaternion.Euler(euler);

            }
        }
        public void SetMaxYaw(float maxYaw)
        {
            this.maxYaw = maxYaw;
        }
        public void SetMaxPitch(float maxPitch)
        {
            this.maxPitch = maxPitch;
        }
        public void SetTowardPosition(Vector3 targetPosition)
        {
            this.targetPosition = tf.InverseTransformPoint(targetPosition);
            targetDirection = tf.InverseTransformDirection(targetPosition - headTf.position);
            targetQuaternion = Quaternion.LookRotation(targetDirection);
        }
        protected float NormalizeAngle(float angle)
        {
            if (angle > 180f) angle -= 360f;
            return angle;
        }
        protected Vector3 LimitAngle(Vector3 euler)
        {
            // Chuyển góc về [-180,180] để clamp dễ hơn
            euler.x = NormalizeAngle(euler.x);
            euler.y = NormalizeAngle(euler.y);

            // Giới hạn yaw (trục Y) và pitch (trục X)
            euler.y = Mathf.Clamp(euler.y, -maxYaw, maxYaw);
            euler.x = Mathf.Clamp(euler.x, -maxPitch, maxPitch);
            return euler;
        }
    }
}