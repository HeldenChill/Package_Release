using System.Collections.Generic;
using UnityEngine;

namespace Hung.Utilities
{
    public class Line2D
    {
        Vector2 direction;
        Vector2 point;
        float slope;

        Vector2 resultPoint;

        public Vector2 Direction
        {
            get => direction;
            set
            {
                direction = value;
                CalculateSlope();
            }
        }
        public Vector2 Point { get => point; set => point = value; }
        public float Slope { get => slope; }

        public Line2D(Vector2 direction, Vector2 point)
        {
            this.Direction = direction;
            this.Point = point;
            CalculateSlope();
            resultPoint = Vector2.zero;
        }

        private void CalculateSlope()
        {
            if (Mathf.Abs(direction.x) == 0)
            {
                slope = Mathf.Infinity;
            }
            else
            {
                slope = direction.y / direction.x;
            }
        }

        private Vector2 GetPoint(float t)
        {
            resultPoint.x = Point.x + t * Direction.x;
            resultPoint.y = Point.y + t * Direction.y;
            return resultPoint;
        }

        public Vector2 FindPointOnLine(Vector2 startPoint, Vector2 endPoint, float magnitude, bool calculateByStartPoint = false)
        {
            float t1;
            float t2;
            float t;
            if (Mathf.Abs(Direction.x) < 0.01)
            {
                t1 = (startPoint.y - Point.y) / Direction.y;
                t2 = (endPoint.y - Point.y) / Direction.y;
            }
            else
            {
                t1 = (startPoint.x - Point.x) / Direction.x;
                t2 = (endPoint.x - Point.x) / Direction.x;
            }


            float multiplier = Mathf.Sign(t2 - t1);
            float value = magnitude / Direction.magnitude;

            if (calculateByStartPoint)
            {
                t = t1 + multiplier * value;
            }
            else
            {
                t = t2 - multiplier * value;
            }

            return GetPoint(t);
        }
    }
    public class Edge3D
    {
        protected Vector3 start;
        protected Vector3 end;
        protected Vector3 slope;
        public Vector3 Start => start;
        public Vector3 End => end;
        public Vector3 Slope => slope;
        public Edge3D(Vector3 start, Vector3 end)
        {
            this.start = start;
            this.end = end;
            slope = end - start;
        }
        public bool Equal(Edge3D edge)
        {
            if (Mathf.Abs((start - edge.Start).sqrMagnitude) <= 0.00001f
            && Mathf.Abs((end - edge.End).sqrMagnitude) <= 0.00001f)
            {
                return true;
            }

            if (Mathf.Abs((start - edge.End).sqrMagnitude) <= 0.00001f
            && Mathf.Abs((end - edge.Start).sqrMagnitude) <= 0.00001f)
            {
                return true;
            }
            return false;
        }

        public (bool, Vector3) IsConnected(Edge3D edge)
        {
            if (Mathf.Abs((start - edge.Start).sqrMagnitude) <= 0.00001f
            && Mathf.Abs((end - edge.End).sqrMagnitude) > 0.00001f)
            {
                return (true, start);
            }

            if (Mathf.Abs((start - edge.End).sqrMagnitude) <= 0.00001f
            && Mathf.Abs((end - edge.Start).sqrMagnitude) > 0.00001f)
            {
                return (true, start);
            }
            if (Mathf.Abs((start - edge.Start).sqrMagnitude) > 0.00001f
            && Mathf.Abs((end - edge.End).sqrMagnitude) <= 0.00001f)
            {
                return (true, end);
            }

            if (Mathf.Abs((start - edge.End).sqrMagnitude) > 0.00001f
            && Mathf.Abs((end - edge.Start).sqrMagnitude) <= 0.00001f)
            {
                return (true, end);
            }
            return (false, default);
        }

        public Vector3 Other(Vector3 point)
        {
            if (Mathf.Abs((point - Start).sqrMagnitude) <= 0.00001f)
            {
                return End;
            }
            else
            {
                return Start;
            }
        }
    }
    public static class MathHelper
    {
        private static Line2D line = new Line2D(Vector2.zero, Vector2.zero);

        public static float GetSlope(Vector2 startPoint, Vector2 endPoint)
        {
            Vector2 direction = endPoint - startPoint;
            line.Direction = direction;
            return line.Slope;
        }
        public static List<List<Vector3>> MergeEdges(List<Edge3D> edges)
        {
            List<List<Vector3>> res = new List<List<Vector3>>();

            return res;
        }
        public static List<Vector3> MergeEdgesToPath(List<Edge3D> edges)
        {
            if (edges == null || edges.Count == 0)
                return new List<Vector3>();

            List<Vector3> path = new List<Vector3>();
            List<Edge3D> remaining = new List<Edge3D>(edges);

            // Start with the first edge
            Edge3D current = remaining[0];
            path.Add(current.Start);
            path.Add(current.End);
            remaining.RemoveAt(0);

            bool extended = true;
            while (remaining.Count > 0 && extended)
            {
                extended = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    Edge3D edge = remaining[i];
                    // Check if edge connects to the start
                    if ((edge.End - path[0]).sqrMagnitude < 0.0001f)
                    {
                        path.Insert(0, edge.Start);
                        remaining.RemoveAt(i);
                        extended = true;
                        break;
                    }
                    else if ((edge.Start - path[0]).sqrMagnitude < 0.0001f)
                    {
                        path.Insert(0, edge.End);
                        remaining.RemoveAt(i);
                        extended = true;
                        break;
                    }
                    // Check if edge connects to the end
                    else if ((edge.Start - path[path.Count - 1]).sqrMagnitude < 0.0001f)
                    {
                        path.Add(edge.End);
                        remaining.RemoveAt(i);
                        extended = true;
                        break;
                    }
                    else if ((edge.End - path[path.Count - 1]).sqrMagnitude < 0.0001f)
                    {
                        path.Add(edge.Start);
                        remaining.RemoveAt(i);
                        extended = true;
                        break;
                    }
                }
            }

            return path;
        }
        public static Vector2 FindPointOnLine(Vector2 startPoint, Vector2 endPoint, float magnitude, bool calculateByStartPoint = false)
        {
            Vector2 direction = endPoint - startPoint;
            line.Direction = direction;
            line.Point = startPoint;

            return line.FindPointOnLine(startPoint, endPoint, magnitude, calculateByStartPoint);
        }
        public static Vector2 GetMouse2DPosition()
        {
            Vector3 worldPosition;
            Vector3 mousePos = Input.mousePosition;
            Camera mainCamera = Camera.main;
            //mousePos.z = Player.mainCamera.nearClipPlane;
            //worldPosition = Player.mainCamera.ScreenToWorldPoint(mousePos); //Draw a line through 2 point,center of camera and mousePos
            mousePos.z = mainCamera.nearClipPlane;
            worldPosition = mainCamera.ScreenToWorldPoint(mousePos);
            return new Vector2(worldPosition.x, worldPosition.y); //Convert the position to 2D
        }
        public static void RotateObject(Vector2 vec1, Vector2 vec2, GameObject gObject)
        {
            Quaternion quaternion = GetQuaternion2Vector(vec1, vec2);
            gObject.transform.rotation = quaternion;
        }
        public static Quaternion GetQuaternion2Vector(Vector2 vec1, Vector2 vec2)
        {
            float angle = Vector2.SignedAngle(vec1, vec2); //angle is the angle between vector face and vector vectorToPoint
            Quaternion quaternion = Quaternion.Euler(0, 0, angle); // Convert from Euler to Quaternion
            return quaternion;
        }
        public static Vector2 AngleToVector(float angle)
        {
            return new Vector2(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle));
        }
        public static Vector2 AngleToVector(float angle, bool isFaceRight)
        {
            if (isFaceRight)
            {
                return AngleToVector(angle);
            }
            else
            {
                Vector2 res = AngleToVector(angle);
                res.x *= -1;
                return res;
            }
        }
        public static void Flip(GameObject gameObject)
        {
            gameObject.transform.Rotate(0, 180f, 0);
        }

        //Random for AI Enemy
        public static Vector2 GetRandomVector2D(bool yGreater0, bool xGreater0)
        {
            float valX = GetSystemRandom(-1f, 1f);
            float valY = GetSystemRandom(1f, 1f);

            if (xGreater0)
                while (valX < 0)
                    valX = GetSystemRandom(-1f, 1f);
            else
                while (valX >= 0)
                    valX = GetSystemRandom(-1f, 1f);



            if (yGreater0)
                while (valY < 0)
                    valY = GetSystemRandom(-1f, 1f);
            else
                while (valY >= 0)
                    valY = GetSystemRandom(-1f, 1f);
            return new Vector2(valX, valY).normalized;
        }
        public static Vector2 GetRandomVector2D()
        {
            float valX = GetSystemRandom(-1f, 1f);
            float valY = GetSystemRandom(-1f, 1f);
            return new Vector2(valX, valY).normalized;
        }
        public static Vector2 GetRandomDirection(float min = 0, float max = 360)
        {
            float angle = GetSystemRandom(min, max);
            return AngleToVector(angle);
        }
        public static Vector2 GetRandomDirection(Vector2 min, Vector2 max)
        {
            float randomX = Random.Range(min.x, max.x);
            float randomY = Random.Range(min.y, max.y);

            // Create a new Vector2 with the random components
            return new Vector2(randomX, randomY);
        }

        public static void InitArrayWithValue(Collider2D[] array, Collider2D value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }
        public static float GetSystemRandom(float min, float max)
        {
            min = min * 1000;
            max = max * 1000;
            System.Random rng = new System.Random();

            float value = rng.Next((int)min, (int)max);
            value = value / 1000;
            return value;
        }
        public static Vector2 GetMidPoint(Vector2 p1, Vector2 p2)
        {
            return new Vector2((p1.x + p2.x) / 2, (p1.y + p2.y) / 2);
        }
        public static Vector2Int Sign(Vector2Int value)
        {
            if (value == Vector2Int.zero)
            {
                return Vector2Int.zero;
            }
            else if (value.x == 0)
            {
                value.Set(0, (int)Mathf.Sign(value.y)); //Error maybe raise in here
            }
            else if (value.y == 0)
            {
                value.Set((int)Mathf.Sign(value.x), 0); //Error maybe raise in here
            }
            else
            {
                value.Set((int)Mathf.Sign(value.x), (int)Mathf.Sign(value.y)); //Error maybe raise in here
            }
            return value;
        }
        public static int Sign(float value)
        {
            if (value == 0) return 0;
            else if (value > 0) return 1;
            else return -1;
        }
        public static float[] CaculateTime(float S, float v0, float a)
        {
            float[] res = null;
            float deta = Mathf.Pow(v0, 2) + 2 * a * S;
            if (deta < 0)
            {
                return null;
            }
            else
            {
                if (Mathf.Abs(deta) < 0.001)
                {
                    res = new float[1] { -v0 / a };
                }
                else
                {
                    float sDeta = Mathf.Sqrt(deta);
                    res = new float[2] { (-v0 + sDeta) / a, (-v0 - sDeta) / a };
                }

                return res;
            }
        }

        public static bool CalculateIntersection(Vector2 pointA, Vector2 directionA, Vector2 pointB, Vector2 directionB, out Vector2 intersection)
        {
            intersection = Vector2.zero; // Default value for out parameter

            // Compute the cross product of the direction vectors
            float cross = directionA.x * directionB.y - directionA.y * directionB.x;

            // If cross product is zero, the lines are parallel
            if (Mathf.Abs(cross) < Mathf.Epsilon)
            {
                return false; // No intersection
            }

            // Calculate the vector between the points
            Vector2 pointDiff = pointB - pointA;

            // Compute the parameter t for the line equations
            float t = (pointDiff.x * directionB.y - pointDiff.y * directionB.x) / cross;

            // Calculate the intersection point
            intersection = pointA + t * directionA;

            return true;
        }
    }
}