using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using global::Utilities;

namespace Hung.Utilities.Input
{
    using Input = UnityEngine.Input;
    public class LineDrawInput : MonoBehaviour
    {
        Coroutine drawing;
        [SerializeField]
        GameObject lineRenderer;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartLine();
            }

            if(Input.GetMouseButtonUp(0))
            {
                FinishLine();
            }
        }

        void StartLine()
        {
            if(drawing != null)
            {
                StopCoroutine(drawing);
            }

            drawing = StartCoroutine(DrawLine());
        }

        void FinishLine()
        {
            StopCoroutine (drawing);
        }

        IEnumerator DrawLine()
        {
            GameObject newGameobject = Instantiate(lineRenderer, Vector3.zero, Quaternion.identity);
            LineRenderer line = newGameobject.GetComponent<LineRenderer>();
            line.positionCount = 0;

            while (true)
            {
                Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                position.z = 0;
                line.positionCount++;
                DevLog.Log(DevId.Hung, $"Draw: {position}");
                line.SetPosition(line.positionCount - 1, position);
                yield return null;
            }
        }
    }
}