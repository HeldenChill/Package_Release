using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hung.Utilities
{
    public static class EdgeHelpers
    {
        public class Node
        {
            public VisionEdge data;
            public Node parentNodes = null;
            public List<Node> childNodes;
            public float value { get; private set; } = default;


            public Node(VisionEdge data)
            {
                this.data = data;
                childNodes = new List<Node>();
            }

            public bool AddChildNode(ref Node node)
            {
                foreach (var i in childNodes)
                {
                    if (node == i)
                    {
                        return false;
                    }
                }
                childNodes.Add(node);
                return true;
            }


            public bool IsChildOf(ref Node node)
            {
                if (parentNodes == node)
                    return true;
                return false;
            }
            public void CalculateValue(Vector2 point)
            {
                value = (point - data.midPoint).magnitude;
            }

            public bool Equals(Node node)
            {
                if (node == null)
                    return false;
                if (data == node.data)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public class Edge
        {
            public int v1;
            public int v2;
            public int supPoint;
            public int triangleIndex;

            public Edge(int aV1, int aV2, int aIndex, int supPoint)
            {
                v1 = aV1;
                v2 = aV2;
                this.supPoint = supPoint;
                triangleIndex = aIndex;

            }


        }

        public class VisionEdge
        {
            public Vector2 v1;
            public Vector2 v2;
            public Vector2 midPoint;
            public Vector2 direction;
            public float halfLength;
            public VisionEdge[] adjEdges;
            public int indexAdjEdge { get; private set; }
            public Node Node { get; private set; }
            public float equalDistance { get; private set; }
            public VisionEdge()
            {
                adjEdges = new VisionEdge[2];
                Node = new Node(this);
                equalDistance = 0.5f;
            }
            public VisionEdge(Vector2 v1, Vector2 v2, Vector2 direction) : this()
            {
                this.v1 = v1;
                this.v2 = v2;
                this.midPoint = MathHelper.GetMidPoint(v1, v2);
                halfLength = (v1 - midPoint).magnitude;
                this.direction = direction.normalized;
                SortVertices();
            }

            public VisionEdge(Edge edge, Vector3[] vertices) : this()
            {
                //Set up point 1,point 2
                v1 = vertices[edge.v1];
                v2 = vertices[edge.v2];
                midPoint = MathHelper.GetMidPoint(v1, v2);
                halfLength = (v1 - midPoint).magnitude;

                Vector2 supPoint = vertices[edge.supPoint];
                Vector2 supMidPoint;
                if ((supPoint - v1).magnitude > (supPoint - v2).magnitude)
                {
                    supMidPoint = MathHelper.GetMidPoint(supPoint, v1);
                }
                else
                {
                    supMidPoint = MathHelper.GetMidPoint(supPoint, v2);
                }

                direction = (midPoint - supMidPoint);

                //We get direction of the edge in here and sort the V1,V2
                SortVertices();
            }

            private void SortVertices()
            {
                if (Mathf.Abs(v1.x - v2.x) < 0.001)
                {
                    direction.y = 0;
                    direction.Normalize();
                    if (v1.y > v2.y)
                    {
                        Vector2 t = v1;
                        v1 = v2;
                        v2 = t;
                    }
                }
                else
                {
                    direction.x = 0;
                    direction.Normalize();
                    if (v1.x > v2.x)
                    {
                        Vector2 t = v1;
                        v1 = v2;
                        v2 = t;
                    }
                }
            }
            public void AddAdjacentEdge(ref VisionEdge adjEdge)
            {
                if (indexAdjEdge >= 2)
                    return;

                adjEdges[indexAdjEdge] = adjEdge;
                indexAdjEdge++;

                if (indexAdjEdge == 2)
                {
                    if (direction.x == 0)
                    {
                        if (adjEdges[0].midPoint.x > adjEdges[1].midPoint.x)
                        {
                            VisionEdge t;
                            t = adjEdges[0];
                            adjEdges[0] = adjEdges[1];
                            adjEdges[1] = t;
                        }
                    }
                    else if (direction.y == 0)
                    {
                        if (adjEdges[0].midPoint.y > adjEdges[1].midPoint.y)
                        {
                            VisionEdge t;
                            t = adjEdges[0];
                            adjEdges[0] = adjEdges[1];
                            adjEdges[1] = t;
                        }
                    }
                }
            }

            public bool IsContain(Vector2 point)
            {
                if (direction.x == 0)
                {
                    if ((midPoint.x - halfLength <= point.x) && (point.x <= midPoint.x + halfLength))
                    {
                        if (Mathf.Abs(midPoint.y - point.y) < equalDistance)
                        {
                            return true;
                        }
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                {
                    if ((midPoint.y - halfLength <= point.y) && (point.y <= midPoint.y + halfLength))
                    {
                        if (Mathf.Abs(midPoint.x - point.x) < equalDistance)
                        {
                            return true;
                        }
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }

            public float Distance(Vector2 point)
            {
                return (point - ClosestPointOnEdge(point)).magnitude;
            }
            public Vector2 ClosestPointOnEdge(Vector2 point)
            {
                if (direction.x == 0)
                {
                    if ((midPoint.x - halfLength <= point.x) && (point.x <= midPoint.x + halfLength))
                    {
                        return new Vector2(point.x, midPoint.y);
                    }
                    else if (point.x <= midPoint.x - halfLength)
                    {
                        return new Vector2(midPoint.x - halfLength, midPoint.y);
                    }
                    else
                    {
                        return new Vector2(midPoint.x + halfLength, midPoint.y);
                    }
                }
                else
                {
                    if ((midPoint.y - halfLength <= point.y) && (point.y <= midPoint.y + halfLength))
                    {
                        return new Vector2(midPoint.x, point.y);
                    }
                    else if (point.y <= midPoint.y - halfLength)
                    {
                        return new Vector2(midPoint.x, midPoint.y - halfLength);
                    }
                    else
                    {
                        return new Vector2(midPoint.x, midPoint.y + halfLength);
                    }
                }
            }

        }
        public static List<Edge> GetEdges(int[] aIndices)
        {
            List<Edge> result = new List<Edge>();
            for (int i = 0; i < aIndices.Length; i += 3)
            {
                int v1 = aIndices[i];
                int v2 = aIndices[i + 1];
                int v3 = aIndices[i + 2];
                result.Add(new Edge(v1, v2, i, v3));
                result.Add(new Edge(v2, v3, i, v1));
                result.Add(new Edge(v3, v1, i, v2));
            }
            return result;
        }
        public static List<Edge> FindBoundary(this List<Edge> aEdges)
        {
            List<Edge> result = new List<Edge>(aEdges);
            for (int i = result.Count - 1; i > 0; i--)
            {
                for (int n = i - 1; n >= 0; n--)
                {
                    if (result[i].v1 == result[n].v2 && result[i].v2 == result[n].v1)
                    {
                        // shared edge so remove both
                        result.RemoveAt(i);
                        result.RemoveAt(n);
                        i--;
                        break;
                    }
                }
            }
            return result;
        }
        public static List<Edge> SortEdges(this List<Edge> aEdges)
        {
            List<Edge> result = new List<Edge>(aEdges);
            for (int i = 0; i < result.Count - 2; i++)
            {
                Edge E = result[i];
                for (int n = i + 1; n < result.Count; n++)
                {
                    Edge a = result[n];
                    if (E.v2 == a.v1)
                    {
                        // in this case they are already in order so just continoue with the next one
                        if (n == i + 1)
                        {
                            break;
                        }


                        // if we found a match, swap them with the next one after "i"
                        result[n] = result[i + 1];
                        result[i + 1] = a;
                        break;
                    }
                }
            }
            return result;
        }
        public static List<VisionEdge> GetVisionEdges(this List<Edge> edges, Vector3[] vertices)
        {
            List<VisionEdge> res = new List<VisionEdge>();
            foreach (Edge edge in edges)
            {
                res.Add(new VisionEdge(edge, vertices));
            }
            return res;
        }
        public static List<List<VisionEdge>> SortVisionEdges(this List<VisionEdge> aEdges)
        {
            List<VisionEdge> horizontalEdge = new List<VisionEdge>();
            List<VisionEdge> verticalEdge = new List<VisionEdge>();

            for (int i = 0; i < aEdges.Count; i++)
            {
                if (aEdges[i].direction.x == 0)
                {
                    horizontalEdge.Add(aEdges[i]);
                    continue;
                }
                else
                {
                    verticalEdge.Add(aEdges[i]);
                }
            }

            for (int i = 0; i < horizontalEdge.Count; i++)
            {
                int minHorEdge = i;
                int minVerEdge = i;

                for (int j = i + 1; j < horizontalEdge.Count; j++)
                {
                    float valueHorizontal = horizontalEdge[minHorEdge].midPoint.y - horizontalEdge[j].midPoint.y;
                    float valueVertical = verticalEdge[minVerEdge].midPoint.x - verticalEdge[j].midPoint.x;
                    if (Mathf.Abs(valueHorizontal) >= 0.0001f)
                    {
                        if (valueHorizontal > 0)
                            minHorEdge = j;
                    }
                    else
                    {
                        if (horizontalEdge[minHorEdge].midPoint.x > horizontalEdge[j].midPoint.x)
                        {
                            minHorEdge = j;
                        }
                    }

                    if (Mathf.Abs(valueVertical) >= 0.0001f)
                    {
                        if (valueVertical > 0)
                            minVerEdge = j;
                    }
                    else
                    {
                        if (horizontalEdge[minHorEdge].midPoint.y > horizontalEdge[j].midPoint.y)
                        {
                            minHorEdge = j;
                        }
                    }
                }

                VisionEdge t = horizontalEdge[minHorEdge];
                horizontalEdge[minHorEdge] = horizontalEdge[i];
                horizontalEdge[i] = t;

                t = verticalEdge[minVerEdge];
                verticalEdge[minVerEdge] = verticalEdge[i];
                verticalEdge[i] = t;
            }
            List<List<VisionEdge>> result = new List<List<VisionEdge>>();

            result.Add(horizontalEdge);
            result.Add(verticalEdge);
            return result;
        }
        public static List<VisionEdge> GetAdjacentVisionEdges(this List<VisionEdge> aEdges)
        {
            for (int i = 0; i < aEdges.Count; i++)
            {
                if (aEdges[i].indexAdjEdge == 2)
                {
                    continue;
                }

                VisionEdge edge = aEdges[i];
                for (int j = i + 1; j < aEdges.Count; j++)
                {
                    VisionEdge checkEdge = aEdges[j];
                    if (edge.v1.Equals(checkEdge.v1) || edge.v1.Equals(checkEdge.v2)
                        || edge.v2.Equals(checkEdge.v1) || edge.v2.Equals(checkEdge.v2))
                    {
                        edge.AddAdjacentEdge(ref checkEdge);
                        checkEdge.AddAdjacentEdge(ref edge);
                    }
                }
            }
            return aEdges;
        }
        public static VisionEdge FindBinaryBelongEdge(this List<VisionEdge> aEdges, Vector2 point)
        {
            if (aEdges[0].direction.x == 0)
            {
                int index = BinarySearch(aEdges, point, true);
                if (index == -1)
                    return null;
                else
                    return aEdges[index];
            }
            else
            {
                int index = BinarySearch(aEdges, point, false);
                if (index == -1)
                    return null;
                else
                    return aEdges[index];
            }
        }
        private static int BinarySearch(List<VisionEdge> arr, Vector2 point, bool isHorizontal)
        {
            float equalDistance = arr[0].equalDistance;
            int r = arr.Count - 1; // chỉ số phần tử cuối
            int l = 0; // Chỉ số phần tử đầu tiên

            if (isHorizontal)
            {
                while (r >= l)
                {
                    int mid = l + (r - l) / 2; // Tương đương (l+r)/2 nhưng ưu điểm tránh tràn số khi l,r lớn

                    // Nếu arr[mid] = x, trả về chỉ số và kết thúc.
                    if (Mathf.Abs(arr[mid].midPoint.y - point.y) < equalDistance)
                    {
                        if (arr[mid].IsContain(point))
                        {
                            return mid;
                        }
                        else
                        {
                            if (arr[mid].midPoint.x > point.x)
                            {
                                r = mid - 1;
                            }

                            if (arr[mid].midPoint.x < point.x)
                            {
                                l = mid + 1;
                            }
                        }
                    }

                    else
                    {
                        if (arr[mid].midPoint.y > point.y)
                        {
                            r = mid - 1;
                        }

                        if (arr[mid].midPoint.y < point.y)
                        {
                            l = mid + 1;
                        }
                    }

                }
            }
            else
            {
                while (r >= l)
                {
                    int mid = l + (r - l) / 2; // Tương đương (l+r)/2 nhưng ưu điểm tránh tràn số khi l,r lớn

                    // Nếu arr[mid] = x, trả về chỉ số và kết thúc.
                    if (Mathf.Abs(arr[mid].midPoint.x - point.x) < equalDistance)
                    {
                        if (arr[mid].IsContain(point))
                        {
                            return mid;
                        }
                        else
                        {
                            if (arr[mid].midPoint.y > point.y)
                            {
                                r = mid - 1;
                            }

                            if (arr[mid].midPoint.y < point.y)
                            {
                                l = mid + 1;
                            }
                        }

                    }
                    else
                    {
                        if (arr[mid].midPoint.x > point.x)
                        {
                            r = mid - 1;
                        }

                        if (arr[mid].midPoint.x < point.x)
                        {
                            l = mid + 1;
                        }
                    }
                }
            }


            // Nếu không tìm thấy
            return -1;
        }
    }
}