using System.Collections.Generic;
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

        return res;
    }
}
