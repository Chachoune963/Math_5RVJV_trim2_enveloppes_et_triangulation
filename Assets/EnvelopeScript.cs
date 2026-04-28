using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvelopeScript : MonoBehaviour
{
    private enum EnvelopeAlgorithm
    {
        Jarvis,
        GrahamScan
    }
    
    [SerializeField] private EnvelopeAlgorithm algorithm;
    
    [SerializeField] private LineRenderer envelopeRenderer;

    [SerializeField] private Transform pointsParent;

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

    private bool IsConvex(Vector2 p1, Vector2 p2, Vector2 p3)
        => GetFullAngle(p2 - p1, p2 - p3) > Mathf.PI;

    // Returns the indexes of the envelope points, in order
    private List<int> JarvisEnvelope(List<Vector2> jarvisPoints)
    {
        // Useful for later
        var count = jarvisPoints.Count;
        
        // Find point at bottom-left of the points cloud
        int firstPoint = 0;
        float xmin = jarvisPoints[firstPoint].x;
        float ymin = jarvisPoints[firstPoint].y;
        for (int i = 1; i < count; ++i)
        {
            if (jarvisPoints[i].x < xmin || (Math.Abs(jarvisPoints[i].x - xmin) < 0.001 && jarvisPoints[i].y < ymin))
            {
                firstPoint = i;
                xmin = jarvisPoints[i].x;
                ymin = jarvisPoints[i].y;
            }
        }
        
        Vector2 v = Vector2.down;
        var res = new List<int>();
        var pivot = firstPoint;

        // Main bulk of the algorithm
        do
        {
            // Add current pivot
            res.Add(pivot);
            var pivotPoint = jarvisPoints[pivot];
            
            // Take the next point's data as first reference
            var current = (pivot + 1) % count; 
            var currentPoint = jarvisPoints[current];
            
            var pivot_candidate = current;
            var min_angle = GetFullAngle(v, currentPoint - pivotPoint);
            var max_len = (currentPoint - pivotPoint).magnitude;

            // Next pivot is the one with the smallest angle
            while (current != pivot)
            {
                current = (current + 1) % count;
                currentPoint = jarvisPoints[current];

                var pToV = currentPoint - pivotPoint;
                var cur_angle = GetFullAngle(v, pToV);
                var cur_len = pToV.magnitude;
                
                if (cur_angle < min_angle || (Math.Abs(cur_angle - min_angle) < 0.001 && cur_len > max_len))
                {
                    pivot_candidate = current;
                    min_angle = cur_angle;
                    max_len = cur_len;
                }
            }

            v = jarvisPoints[pivot_candidate] - pivotPoint;
            pivot = pivot_candidate;

        } while (pivot != firstPoint);

        return res;
    }

    private List<int> GrahamScanEnvelope(List<Vector2> scanPoints)
    {
        Vector2 baryCenter = new Vector2();
        foreach (var point in scanPoints)
            baryCenter += point;
        baryCenter /= scanPoints.Count;

        var indexes = new List<int>();
        for (int i = 0; i < scanPoints.Count; ++i)
            indexes.Add(i);
        
        var orderedIndexes = indexes
            .OrderBy(i => GetFullAngle(Vector2.right, (scanPoints[i] - baryCenter).normalized))
            .ToList();
        
        // TODO: Pas fais à temps...

        return new List<int> { 0, 1 };
    }

    void Update()
    {
        var points = pointsParent.GetComponentsInChildren<Transform>();

        var jarvisPoints = points
            .Select(p => new Vector2(p.position.x, p.position.y))
            .ToList();

        List<int> envelope;
        if (algorithm == EnvelopeAlgorithm.Jarvis)
            envelope = JarvisEnvelope(jarvisPoints);
        else
            envelope = GrahamScanEnvelope(jarvisPoints);
        
        var linePoints = new Vector3[envelope.Count + 1];
        for (int i = 0; i < envelope.Count; ++i)
            linePoints[i] = points[envelope[i]].position;
        linePoints[envelope.Count] = points[envelope[0]].position;

        envelopeRenderer.positionCount = envelope.Count + 1;
        envelopeRenderer.SetPositions(linePoints);
    }
}
