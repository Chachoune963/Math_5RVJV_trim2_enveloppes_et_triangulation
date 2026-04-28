using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Point
{
    public Vector3 position;
    public List<Edge> usedInEdges = new List<Edge>();
}

public class Edge
{
    public Point a;
    public Point b;

    public Face f1;
    public Face f2;
}

public class Face
{
    public Point a;
    public Point b;
    public Point c;
    
    public Edge s1;
    public Edge s2;
    public Edge s3;
}

public class TetraedraGraph
{
    private List<Point> points = new List<Point>();
    private List<Edge> edges = new List<Edge>();
    private List<Face> faces = new List<Face>();

    private bool CheckEdgeExists(Point a, Point b, out Edge foundEdge)
    {
        foreach (var edge in edges)
        {
            if ((a == edge.a && b == edge.b)
                || b == edge.a && a == edge.b)
            {
                foundEdge = edge;
                return true;
            }
        }

        foundEdge = null;
        return false;
    }

    // Util to recalculate edges from existing faces and points
    private void RebuildEdges()
    {
        // Clear all edge data
        // Faces should update their dead references themselves
        edges.Clear();
        foreach (var point in points)
            point.usedInEdges.Clear();
        
        foreach (var face in faces)
        {
            Edge edge1;
            if (CheckEdgeExists(face.a, face.b, out edge1))
                edge1.f2 = face;
            else
            {
                edge1 = new Edge() { a = face.a, b = face.b, f1 = face };
                edges.Add(edge1);
                face.a.usedInEdges.Add(edge1);
                face.b.usedInEdges.Add(edge1);
            }
            face.s1 = edge1;
            
            Edge edge2;
            if (CheckEdgeExists(face.b, face.c, out edge2))
                edge2.f2 = face;
            else
            {
                edge2 = new Edge() { a = face.b, b = face.c, f1 = face };
                edges.Add(edge2);
                face.b.usedInEdges.Add(edge2);
                face.c.usedInEdges.Add(edge2);
            }
            face.s2 = edge2;
            
            Edge edge3;
            if (CheckEdgeExists(face.c, face.a, out edge3))
                edge3.f2 = face;
            else
            {
                edge3 = new Edge() { a = face.c, b = face.a, f1 = face };
                edges.Add(edge3);
                face.c.usedInEdges.Add(edge3);
                face.a.usedInEdges.Add(edge3);
            }
            face.s3 = edge3;
        }
    }

    private bool IsVisibleFromPoint(Face face, Point point)
    {
        var edge1_vec = face.b.position - face.a.position;
        var edge2_vec = face.c.position - face.a.position;

        var normal = Vector3.Cross(edge1_vec, edge2_vec);
        
        return Vector3.Dot(normal, point.position - face.a.position) > 0;
    }
    
    // The "Horizon" is the set of edges standing between a visible face, and one that's not.
    // In other words, they're the edges we'll need to connect to our new point.
    // In the course's words, the horizon are the purple elements and "visited" the blue ones
    private (List<Edge>, HashSet<Face>) FindHorizon(Point point, Face startFace)
    {
        List<Edge> horizon = new List<Edge>();
        // Keeping track of the visited faces can tell us which faces need to be deleted
        HashSet<Face> visited = new HashSet<Face>();
        Stack<Face> toVisit = new Stack<Face>();

        toVisit.Push(startFace);

        while (toVisit.Count > 0)
        {
            Face current = toVisit.Pop();
            if (!visited.Add(current)) continue;

            foreach (var edge in new[] { current.s1, current.s2, current.s3 })
            {
                Face neighbor = (edge.f1 == current) ? edge.f2 : edge.f1;

                if (neighbor == null || !IsVisibleFromPoint(neighbor, point))
                    horizon.Add(edge);
                else
                    toVisit.Push(neighbor);
            }
        }

        return (horizon, visited);
    }

    public void AddPoint(Point point)
    {
        // First, look if the point is visible by any face in the first place.
        // If not, that implies the point is inside the convex hull already.
        Face visibleFace = null;
        foreach (var face in faces)
        {
            if (IsVisibleFromPoint(face, point))
            {
                visibleFace = face;
                break;
            }
        }
        
        // No visible face was found, point in inside the hull, no need to do anything.
        if (visibleFace is null)
            return;

        // Get the horizon and the faces to delete
        var (horizon, deletedFaces) = FindHorizon(point, visibleFace);
        
        // Delete the hidden faces
        foreach (var face in deletedFaces)
            faces.Remove(face);
        
        // The point now deserves its rightful place in the hull
        // All hail the point
        points.Add(point);

        var barycenter = new Point();
        foreach (var calcPoint in points)
            barycenter.position += calcPoint.position;
        barycenter.position /= points.Count;
        
        // Add faces from all horizon edges
        foreach (var edge in horizon)
        {
            // Make sure it's build in the right orientation
            var newFace = new Face() { a = edge.a, b = edge.b, c = point };
            if (IsVisibleFromPoint(newFace, barycenter))
                newFace = new Face() { a = edge.a, b = point, c = edge.b };
            faces.Add(newFace);
        }
        
        // Edges data are now broken, clear it and rebuild it entirely
        // Any issue should solve itself by this point
        RebuildEdges();
    }

    // Done by AI to accelerate things, not important for our algorithms anyway
    public Mesh AsMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Map each Point to a vertex index
        Dictionary<Point, int> indexMap = new Dictionary<Point, int>();

        int index = 0;
        foreach (var p in points)
        {
            indexMap[p] = index++;
            vertices.Add(p.position);
        }

        // Build triangle list
        foreach (var f in faces)
        {
            triangles.Add(indexMap[f.a]);
            triangles.Add(indexMap[f.b]);
            triangles.Add(indexMap[f.c]);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
    
    // This data structure is HELL to build initially so let's program a shortcut function
    public static TetraedraGraph BaseCube()
    {
        TetraedraGraph res = new TetraedraGraph();

        var size = 1.0f / 2;
        // The 8 points of a cube
        Point p1 = new Point() { position = new Vector3(-size, -size, -size) };
        Point p2 = new Point() { position = new Vector3(-size, -size, size) };
        Point p3 = new Point() { position = new Vector3(-size, size, -size) };
        Point p4 = new Point() { position = new Vector3(-size, size, size) };
        Point p5 = new Point() { position = new Vector3(size, -size, -size) };
        Point p6 = new Point() { position = new Vector3(size, -size, size) };
        Point p7 = new Point() { position = new Vector3(size, size, -size) };
        Point p8 = new Point() { position = new Vector3(size, size, size) };

        res.points = new List<Point>() { p1, p2, p3, p4, p5, p6, p7, p8 };

        // Faces
        var faces = new List<Face>();
        // Front
        faces.Add(new Face() { a = p2, b = p6, c = p8 });
        faces.Add(new Face() { a = p2, b = p8, c = p4 });
        
        // Back
        faces.Add(new Face() { a = p1, b = p7, c = p5 });
        faces.Add(new Face() { a = p1, b = p3, c = p7 });

        // Left
        faces.Add(new Face() { a = p1, b = p2, c = p4 });
        faces.Add(new Face() { a = p1, b = p4, c = p3 });

        // Right
        faces.Add(new Face() { a = p6, b = p5, c = p7 });
        faces.Add(new Face() { a = p6, b = p7, c = p8 });

        // Up
        faces.Add(new Face() { a = p3, b = p4, c = p8 });
        faces.Add(new Face() { a = p3, b = p8, c = p7 });

        // Down
        faces.Add(new Face() { a = p1, b = p5, c = p6 });
        faces.Add(new Face() { a = p1, b = p6, c = p2 });

        res.faces = faces;
        
        res.RebuildEdges();

        return res;
    }
}
