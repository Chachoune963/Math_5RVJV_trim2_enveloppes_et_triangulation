using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TriangulationScript : MonoBehaviour
{
    [SerializeField] private bool useDelauney;
    [SerializeField] private bool voronoi;
    [SerializeField] private MeshFilter meshFilter;

    [SerializeField] private GameObject pointsParent;

    private Vector2 debugCenter;
    private float debugRadius;
    
    private float GetAngle(Vector2 v1, Vector2 v2)
    {
        return Mathf.Acos(Vector2.Dot(v1, v2) / (v1.magnitude * v2.magnitude));
    }

    private float GetFullAngle(Vector2 v1, Vector2 v2)
    {
        var det = v1.x * v2.y - v2.x * v1.y;
        var a = GetAngle(v1, v2);

        return det >= 0 ? a : 2 * Mathf.PI - a;
    }

    private int[] IncrementalTriangulation(List<Vector2> points)
    {
        var indexes = new List<int>();
        for (int i = 0; i < points.Count; ++i)
            indexes.Add(i);
        
        var orderedIndexes = indexes
            .OrderBy(i => points[i].x)
            .ToList();

        var triangles = new List<int>();
        
        // Add first triangle with first three values
        triangles.Add(orderedIndexes[0]);
        triangles.Add(orderedIndexes[1]);
        triangles.Add(orderedIndexes[2]);

        // Far from the best way to handle this but we are pressed by time so it will do
        var bannedSides = new List<(int, int)>();
        
        // Iterate over every point not yet in the mesh
        for (int i = 3; i < orderedIndexes.Count; ++i)
        {
            var curPointIndex = orderedIndexes[i];
            var curPoint = points[curPointIndex];

            var buffer = new List<int>();
            
            var pointsUsage = new int[points.Count];

            // Iterate over every triangle we have and test visibilities with its sides
            for (int j = 0; j < triangles.Count; j += 3)
            {
                var p1 = points[triangles[j]];
                var p2 = points[triangles[j + 1]];
                var p3 = points[triangles[j + 2]];
                
                // Side 1
                var s1 = p2 - p1;
                var n1 = new Vector2(-s1.y, s1.x);
                // If visibility is checked, add this as a new triangle
                if (Vector2.Dot(n1, curPoint - p1) > 0 
                    && !bannedSides.Contains((triangles[j], triangles[j + 1])))
                {
                    pointsUsage[triangles[j]]++;
                    pointsUsage[triangles[j + 1]]++;
                    
                    buffer.Add(triangles[j]);
                    buffer.Add(curPointIndex);
                    buffer.Add(triangles[j + 1]);
                    
                    bannedSides.Add((triangles[j], triangles[j + 1]));
                    bannedSides.Add((triangles[j + 1], triangles[j]));
                }

                // Side 2
                var s2 = p3 - p2;
                var n2 = new Vector2(-s2.y, s2.x);
                // If visibility is checked, add this as a new triangle
                if (Vector2.Dot(n2, curPoint - p2) > 0 
                    && !bannedSides.Contains((triangles[j + 1], triangles[j + 2])))
                {
                    pointsUsage[triangles[j + 1]]++;
                    pointsUsage[triangles[j + 2]]++;
                    
                    buffer.Add(triangles[j + 1]);
                    buffer.Add(curPointIndex);
                    buffer.Add(triangles[j + 2]);
                    
                    bannedSides.Add((triangles[j + 1], triangles[j + 2]));
                    bannedSides.Add((triangles[j + 2], triangles[j + 1]));
                }
                
                // Side 3
                var s3 = p1 - p3;
                var n3 = new Vector2(-s3.y, s3.x);
                // If visibility is checked, add this as a new triangle
                if (Vector2.Dot(n3, curPoint - p3) > 0 
                    && !bannedSides.Contains((triangles[j + 2], triangles[j])))
                {
                    pointsUsage[triangles[j + 2]]++;
                    pointsUsage[triangles[j]]++;
                    
                    buffer.Add(triangles[j + 2]);
                    buffer.Add(curPointIndex);
                    buffer.Add(triangles[j]);
                    
                    bannedSides.Add((triangles[j + 2], triangles[j]));
                    bannedSides.Add((triangles[j], triangles[j + 2]));
                }

                // We should considerh getting this to the level below so it doesn't impact performance as much
                // This is safer though
                for (int usageI = 0; usageI < pointsUsage.Length; ++usageI)
                {
                    if (pointsUsage[usageI] > 1)
                    {
                        bannedSides.Add((usageI, curPointIndex));
                        bannedSides.Add((curPointIndex, usageI));
                    }
                }
            }

            triangles.AddRange(buffer);
        }

        return triangles.ToArray();
    }

    private (int, int) GetNormalizedEdge(int a, int b)
    {
        // Edges as we are about to parse can be order-sensitive
        // To help with this so we can use them as keys in a Dictionary, we normalize them by applying small sorting.
        if (a <= b)
            return (a, b);
        
        return (b, a);
    }
    
    int GetOppositeVertex(int a, int b, int t0, int t1, int t2)
    {
        if (t0 != a && t0 != b) return t0;
        if (t1 != a && t1 != b) return t1;
        return t2;
    }
    
    float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private (Vector2, float) FindCircumcenter(Vector2 A, Vector2 B, Vector2 C)
    {
        float a = 2 * (B.x - A.x);
        float b = 2 * (B.y - A.y);
        float c = 2 * (C.x - A.x);
        float d = 2 * (C.y - A.y);

        float e = B.x * B.x - A.x * A.x + B.y * B.y - A.y * A.y;
        float f = C.x * C.x - A.x * A.x + C.y * C.y - A.y * A.y;
        
        // Simple formula to invert 2x2 matrix
        float inv = 1 / (a * d - b * c);
        float m00 = d * inv;
        float m01 = -b * inv;
        float m10 = -c * inv;
        float m11 = a * inv;
        
        // Multiply inverted matrix with [e, f]
        float ux = m00 * e + m01 * f;
        float uy = m10 * e + m11 * f;
        Vector2 u = new Vector2(ux, uy);
        float r = (A - u).magnitude;

        return (u, r);
    }

    private Dictionary<(int, int), List<int>> EdgesToTriangles(int[] initialTriangles)
    {
        // Get edges and associated triangles
        // This data structures uses normalized edges as keys
        // The values - which SHOULD always be 2 ints - are the indexes of the 2 faces it shares
        var edgesToTriangles = new Dictionary<(int, int), List<int>>();

        for (int i = 0; i < initialTriangles.Length; i += 3)
        {
            // For each triangle, store the 3 edges
            var a = initialTriangles[i];
            var b = initialTriangles[i + 1];
            var c = initialTriangles[i + 2];

            var f1 = GetNormalizedEdge(a, b);
            if (!edgesToTriangles.ContainsKey(f1))
                edgesToTriangles[f1] = new List<int>();
            edgesToTriangles[f1].Add(i);

            var f2 = GetNormalizedEdge(b, c);
            if (!edgesToTriangles.ContainsKey(f2))
                edgesToTriangles[f2] = new List<int>();
            edgesToTriangles[f2].Add(i);

            var f3 = GetNormalizedEdge(c, a);
            if (!edgesToTriangles.ContainsKey(f3))
                edgesToTriangles[f3] = new List<int>();
            edgesToTriangles[f3].Add(i);
        }

        return edgesToTriangles;
    }

    private int[] Delauney(int[] initialTriangles, List<Vector2> points)
    {
        // Loop to simplify the lagorithm, but it gets heavier as a result
        // A more optimal way would be to keep a stack of every edge that remains to be verified
        // and update it when a flip happens. But that's not priority, it works.
        bool flipped;
        do
        {
            flipped = false;
            
            var edgesToTriangles = EdgesToTriangles(initialTriangles);

            foreach (var edge in edgesToTriangles.Keys)
            {
                // Edges used in only 1 face don't matter
                if (edgesToTriangles[edge].Count < 2)
                    continue;

                var t1 = edgesToTriangles[edge][0];
                var t2 = edgesToTriangles[edge][1];

                int a = edge.Item1;
                int b = edge.Item2;

                var (t1a, t1b, t1c) = (initialTriangles[t1], initialTriangles[t1 + 1], initialTriangles[t1 + 2]);
                var (t2a, t2b, t2c) = (initialTriangles[t2], initialTriangles[t2 + 1], initialTriangles[t2 + 2]);

                int c = GetOppositeVertex(a, b, t1a, t1b, t1c);
                int d = GetOppositeVertex(a, b, t2a, t2b, t2c);

                // We might get a and b in the wrong order since it was normalized, we need to correct this
                if (Cross(points[a], points[b], points[c]) < 0)
                    (b, a) = (a, b);

                var (center, radius) = FindCircumcenter(points[a], points[b], points[c]);
                if ((points[d] - center).magnitude < radius)
                {
                    // Finally flip the edges (Rebuild the triangles to change the common edge

                    initialTriangles[t1] = c;
                    initialTriangles[t1 + 1] = d;
                    initialTriangles[t1 + 2] = a;

                    initialTriangles[t2] = d;
                    initialTriangles[t2 + 1] = c;
                    initialTriangles[t2 + 2] = b;

                    flipped = true;
                    break; // restart
                }
            }
        } while (flipped);

        return initialTriangles;
    }

    private void Voronoi(int[] delauneyTriangles, List<Vector2> points)
    {
        // For each triangle, determine its center
        Dictionary<int, int> triangleToCircumcenter = new Dictionary<int, int>();
        List<Vector2> voronoiPoints = new List<Vector2>();
        for (int i = 0; i < delauneyTriangles.Length; i += 3)
        {
            var a = points[delauneyTriangles[i]];
            var b = points[delauneyTriangles[i + 1]];
            var c = points[delauneyTriangles[i + 2]];

            var (center, radius) = FindCircumcenter(a, b, c);
            triangleToCircumcenter.Add(i, i / 3);
            voronoiPoints.Add(center);
        }
        
        // For each edge, determine corresponding voronoi edge
        var edgesToTriangles = EdgesToTriangles(delauneyTriangles);
        var edgesToVoronoiEdges = new Dictionary<(int, int), (int, int)>();
        foreach (var edge in edgesToTriangles.Keys)
        {
            var triangles = edgesToTriangles[edge];
            // TODO: Don't ignore this!
            if (triangles.Count < 2)
            {
                // If it's a border (Only adjacent to 1 face), go an arbitrary distance along the normal
                var edgeVec = points[edge.Item2] - points[edge.Item1];
                var normal = new Vector2(edgeVec.y, -edgeVec.x).normalized;
                
                // Make sure the normal is facing outward!
                var t = triangles[0];
                var ta = delauneyTriangles[t];
                var tb = delauneyTriangles[t + 1];
                var tc = delauneyTriangles[t + 2];
                var opposite = GetOppositeVertex(edge.Item1, edge.Item2, ta, tb, tc);
                
                var circumcenter = voronoiPoints[triangleToCircumcenter[t]];
                var oppositePoint = points[opposite];
                if (Vector2.Dot(normal, oppositePoint - circumcenter) > 0)
                    normal = -normal;
                
                Debug.DrawLine(circumcenter, circumcenter + normal * 5);
            }
            else
            {
                var neighbordCircumcenter1 = triangleToCircumcenter[triangles[0]];
                var neighbordCircumcenter2 = triangleToCircumcenter[triangles[1]];

                Debug.DrawLine(voronoiPoints[neighbordCircumcenter1], voronoiPoints[neighbordCircumcenter2]);
            }
        }
        
        // No assignment of regions unfortunately, but it could be done by storing and iterating over the edges found previously.
    }

    // Update is called once per frame
    void Update()
    {
        if (voronoi)
            useDelauney = true;
        
        var points = pointsParent.GetComponentsInChildren<Transform>().ToList();
        // Includes the parent, so remove it from the list before moving on
        points.RemoveAt(0);
        
        var meshPoints = points
            .Select(p => p.position)
            .ToArray();

        var trianglesPoints = points
            .Select(p => new Vector2(p.position.x, p.position.y))
            .ToList();

        var triangles = IncrementalTriangulation(trianglesPoints);

        if (useDelauney)
            triangles = Delauney(triangles, trianglesPoints);

        if (voronoi)
            Voronoi(triangles, trianglesPoints);

        Mesh res = new Mesh();
        res.vertices = meshPoints;
        res.triangles = triangles;
        meshFilter.mesh = res;
    }
}
