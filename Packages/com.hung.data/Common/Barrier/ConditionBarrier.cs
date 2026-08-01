using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Hung.Common
{
    public class ConditionBarrier
    {
        private HashSet<Task> pendingConditions = new HashSet<Task>();
        public float MinTime = 0;
        private float startTime = 0;

        // Sự kiện để các script khác lắng nghe khi mọi thứ đã sẵn sàng
        public event Action OnAllConditionsMet;


        /// <summary>
        /// Đăng ký một điều kiện cần phải hoàn thành
        /// </summary>
        public void RegisterCondition(Task task)
        {
            if(task == null) return;
            if (!pendingConditions.Contains(task))
            {
                pendingConditions.Add(task);
            }
        }
        public IEnumerator StartBarrier()
        {
            startTime = Time.time;
            yield return Task.WhenAll(pendingConditions); // Đảm bảo rằng tất cả các task đã hoàn thành trước khi tiếp tục
            if (MinTime > 0)
            {
                float elapsedTime = Time.time - startTime;
                if (elapsedTime < MinTime)
                {
                    yield return new WaitForSeconds(MinTime - elapsedTime);
                }
            }
            OnAllConditionsMet?.Invoke();
            // Logic để bắt đầu barrier
        }
        public void ClearConditions()
        {
            pendingConditions.Clear();
        }
    }
}
