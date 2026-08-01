namespace Hung.Base
{
    using System.Collections.Generic;
    using UnityEngine;
    public struct Cube
    {
        public const float HALF_UNIT = 0.4f;
        public Vector3 Center;
        public Vector3 Size;
        public Cube(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = size;
        }
        public static Cube Mirror(Cube root, Vector3 center, Vector3 direction)
        {
            Vector3 rootToCenter = root.Center - center;
            Vector3 reflection = rootToCenter - 2 * Vector3.Dot(rootToCenter, direction) * direction;
            Vector3 newCenter = center + reflection;
            return new Cube(newCenter, root.Size);
        }
        /// <summary>
        /// Merges two cubes into a larger cube that encompasses both.
        /// </summary>
        public static Cube Merge(Cube a, Cube b)
        {
            Vector3 min = new Vector3(
                Mathf.Min(a.Center.x - a.Size.x / 2, b.Center.x - b.Size.x / 2),
                Mathf.Min(a.Center.y - a.Size.y / 2, b.Center.y - b.Size.y / 2),
                Mathf.Min(a.Center.z - a.Size.z / 2, b.Center.z - b.Size.z / 2)
            );

            Vector3 max = new Vector3(
                Mathf.Max(a.Center.x + a.Size.x / 2, b.Center.x + b.Size.x / 2),
                Mathf.Max(a.Center.y + a.Size.y / 2, b.Center.y + b.Size.y / 2),
                Mathf.Max(a.Center.z + a.Size.z / 2, b.Center.z + b.Size.z / 2)
            );

            Vector3 size = max - min;
            Vector3 center = min + size / 2;
            return new Cube(center, size);
        }

        /// <summary>
        /// Checks if a given point is inside the cube.
        /// </summary>
        public bool IsPointInside(Vector3 point)
        {
            return (point.x >= Center.x - Size.x / 2 && point.x <= Center.x + Size.x / 2 &&
                    point.y >= Center.y - Size.y / 2 && point.y <= Center.y + Size.y / 2 &&
                    point.z >= Center.z - Size.z / 2 && point.z <= Center.z + Size.z / 2);
        }

        /// <summary>
        /// Subtracts Cube B from Cube A and returns the remaining part as an array of Cubes.
        /// </summary>
        public Cube[] Subtract(Cube other)
        {
            // Check if cubes don't intersect
            if (!Intersects(other))
            {
                return new Cube[] { this };
            }

            // Check if this cube is completely inside the other
            if (other.Center.x - other.Size.x / 2 <= Center.x - Size.x / 2 &&
                other.Center.x + other.Size.x / 2 >= Center.x + Size.x / 2 &&
                other.Center.y - other.Size.y / 2 <= Center.y - Size.y / 2 &&
                other.Center.y + other.Size.y / 2 >= Center.y + Size.y / 2 &&
                other.Center.z - other.Size.z / 2 <= Center.z - Size.z / 2 &&
                other.Center.z + other.Size.z / 2 >= Center.z + Size.z / 2)
            {
                return new Cube[0]; // This cube is completely subtracted
            }

            // TODO: Implement proper subtraction that returns multiple cubes
            // For now, just return the original cube
            return new Cube[] { this };
        }

        /// <summary>
        /// Checks if two cubes intersect.
        /// </summary>
        public bool Intersects(Cube other)
        {
            return !(other.Center.x - other.Size.x / 2 > Center.x + Size.x / 2 ||
                     other.Center.x + other.Size.x / 2 < Center.x - Size.x / 2 ||
                     other.Center.y - other.Size.y / 2 > Center.y + Size.y / 2 ||
                     other.Center.y + other.Size.y / 2 < Center.y - Size.y / 2 ||
                     other.Center.z - other.Size.z / 2 > Center.z + Size.z / 2 ||
                     other.Center.z + other.Size.z / 2 < Center.z - Size.z / 2);
        }
    }
    public class CubeOctree
    {
        private const int MAX_ELEMENTS_PER_NODE = 16;
        private const int MAX_DEPTH = 8;
        
        private Cube bounds;
        private int depth;
        private CubeOctree[] children;
        private List<Cube> elements;
        
        public CubeOctree(Cube bounds, int depth = 0)
        {
            this.bounds = bounds;
            this.depth = depth;
            this.elements = new List<Cube>();
        }
        
        public void Insert(Cube cube)
        {
            // If this cube doesn't fit in our bounds, reject it
            if (!bounds.Intersects(cube))
                return;
            
            // If we're at a leaf node with space or max depth, add the element here
            if (children == null && (elements.Count < MAX_ELEMENTS_PER_NODE || depth >= MAX_DEPTH))
            {
                elements.Add(cube);
                return;
            }
            
            // If we need to split, create child nodes
            if (children == null)
                Split();
            
            // Insert into appropriate children
            foreach (var child in children)
            {
                child.Insert(cube);
            }
            
            // If we're not a leaf, we don't store elements at this level
            if (children != null && elements.Count > 0)
            {
                foreach (var element in elements)
                {
                    foreach (var child in children)
                    {
                        child.Insert(element);
                    }
                }
                elements.Clear();
            }
        }
        
        private void Split()
        {
            Vector3 center = bounds.Center;
            Vector3 halfSize = bounds.Size / 4f; // Quarter of original size
            
            children = new CubeOctree[8];
            
            // Create 8 children for each octant
            for (int i = 0; i < 8; i++)
            {
                // Calculate new center based on octant
                Vector3 newCenter = center + new Vector3(
                    ((i & 1) == 0 ? -halfSize.x : halfSize.x),
                    ((i & 2) == 0 ? -halfSize.y : halfSize.y),
                    ((i & 4) == 0 ? -halfSize.z : halfSize.z)
                );
                
                Cube childBounds = new Cube(newCenter, halfSize * 2);
                children[i] = new CubeOctree(childBounds, depth + 1);
            }
        }
        
        public List<Cube> Query(Cube queryRegion)
        {
            List<Cube> results = new List<Cube>();
            
            // If query doesn't intersect this node, return empty list
            if (!bounds.Intersects(queryRegion))
                return results;
            
            // Add any elements at this level that intersect the query
            foreach (var element in elements)
            {
                if (element.Intersects(queryRegion))
                    results.Add(element);
            }
            
            // If we have children, query them too
            if (children != null)
            {
                foreach (var child in children)
                {
                    results.AddRange(child.Query(queryRegion));
                }
            }
            
            return results;
        }
        
        public List<Cube> Query(Vector3 point)
        {
            List<Cube> results = new List<Cube>();
            
            // If point isn't in this node, return empty list
            if (!bounds.IsPointInside(point))
                return results;
            
            // Add any elements at this level that contain the point
            foreach (var element in elements)
            {
                if (element.IsPointInside(point))
                    results.Add(element);
            }
            
            // If we have children, query them too
            if (children != null)
            {
                foreach (var child in children)
                {
                    results.AddRange(child.Query(point));
                }
            }
            
            return results;
        }
    }
    public static class CUBE_UTILITIES
    {
        public static List<Cube> SphereToCube(Vector3 position, float radius, float cubeHalfSize = Cube.HALF_UNIT)
        {
            List<Cube> result = new List<Cube>();
            
            // Calculate bounds of the cube grid
            int xCount = Mathf.CeilToInt(radius / (cubeHalfSize * 2));
            int yCount = Mathf.CeilToInt(radius / (cubeHalfSize * 2));
            int zCount = Mathf.CeilToInt(radius / (cubeHalfSize * 2));
            
            // Create cubes within the sphere
            for (int x = -xCount; x <= xCount; x++)
            {
                for (int y = -yCount; y <= yCount; y++)
                {
                    for (int z = -zCount; z <= zCount; z++)
                    {
                        Vector3 cubeCenter = position + new Vector3(
                            x * cubeHalfSize * 2,
                            y * cubeHalfSize * 2,
                            z * cubeHalfSize * 2
                        );
                        
                        // Check if cube center is within sphere
                        if (Vector3.Distance(position, cubeCenter) <= radius)
                        {
                            result.Add(new Cube(cubeCenter, new Vector3(cubeHalfSize * 2, cubeHalfSize * 2, cubeHalfSize * 2)));
                        }
                    }
                }
            }
            
            return result;
        }
        
        public static List<Cube> ConeToCube(Vector3 position, Vector3 direction, float radius, float angle, float cubeHalfSize = Cube.HALF_UNIT)
        {
            List<Cube> result = new List<Cube>();
            
            // Normalize direction
            direction.Normalize();
            
            // Calculate bounds of the cube grid
            int xCount = Mathf.CeilToInt(radius / (cubeHalfSize * 2));
            int yCount = Mathf.CeilToInt(radius / (cubeHalfSize * 2));
            int zCount = Mathf.CeilToInt(radius / (cubeHalfSize * 2));
            
            // Create cubes within the cone
            for (int x = -xCount; x <= xCount; x++)
            {
                for (int y = -yCount; y <= yCount; y++)
                {
                    for (int z = -zCount; z <= zCount; z++)
                    {
                        Vector3 cubeCenter = position + new Vector3(
                            x * cubeHalfSize * 2,
                            y * cubeHalfSize * 2,
                            z * cubeHalfSize * 2
                        );
                        
                        // Check if cube is within radius
                        float distance = Vector3.Distance(position, cubeCenter);
                        if (distance <= radius)
                        {
                            // Check if cube is within cone angle
                            Vector3 toCube = cubeCenter - position;
                            float cubeAngle = Vector3.Angle(direction, toCube);
                            
                            if (cubeAngle <= angle / 2)
                            {
                                result.Add(new Cube(cubeCenter, new Vector3(cubeHalfSize * 2, cubeHalfSize * 2, cubeHalfSize * 2)));
                            }
                        }
                    }
                }
            }
            
            return result;
        }

        public static Vector3 ProjectPointOntoPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            // Compute the signed distance from the point to the plane
            float distance = Vector3.Dot(point - planePoint, planeNormal);
            // Project the point onto the plane
            return point - distance * planeNormal;
        }

    }

    
}