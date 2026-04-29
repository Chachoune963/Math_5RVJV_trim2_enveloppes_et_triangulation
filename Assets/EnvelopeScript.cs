using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvelopeScript : MonoBehaviour
{
    public enum EnvelopeAlgorithm
    {
        None,
        Jarvis,
        GrahamScan
    }
    
    [SerializeField] private EnvelopeAlgorithm algorithm;
    
    [SerializeField] private LineRenderer envelopeRenderer;

    [SerializeField] private Transform pointsParent;

    public void SetAlgorithm(EnvelopeAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case EnvelopeAlgorithm.None:
                gameObject.SetActive(false);
                break;
            
            case EnvelopeAlgorithm.Jarvis:
                gameObject.SetActive(true);
                this.algorithm = EnvelopeAlgorithm.Jarvis;
                break;

            case EnvelopeAlgorithm.GrahamScan:
                gameObject.SetActive(true);
                this.algorithm = EnvelopeAlgorithm.GrahamScan;
                break;
        }
    }
    
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
        => GetFullAngle(p1 - p2, p3 - p2) > Mathf.PI;

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
        // Find point at bottom-left of the points cloud
        int firstPoint = 0;
        float xmin = scanPoints[firstPoint].x;
        float ymin = scanPoints[firstPoint].y;
        for (int i = 1; i < scanPoints.Count; ++i)
        {
            if (scanPoints[i].x < xmin || (Math.Abs(scanPoints[i].x - xmin) < 0.001 && scanPoints[i].y < ymin))
            {
                firstPoint = i;
                xmin = scanPoints[i].x;
                ymin = scanPoints[i].y;
            }
        }

        var anchorPoint = scanPoints[firstPoint];

        var indexes = new List<int>();
        for (int i = 0; i < scanPoints.Count; ++i)
            indexes.Add(i);
        indexes.Remove(firstPoint);
        
        // Anchor point should be the first point in this list
        var orderedIndexes = indexes
            .OrderBy(i => GetFullAngle(Vector2.down, scanPoints[i] - anchorPoint))
            .ThenBy(i => (scanPoints[i] - anchorPoint).sqrMagnitude)
            .ToList();
        // Force anchor point into the first position on the list
        orderedIndexes.Insert(0, firstPoint);

        List<int> envelopeStack = new List<int>();
        envelopeStack.Add(orderedIndexes[0]);
        envelopeStack.Add(orderedIndexes[1]);

        for (int i = 2; i < orderedIndexes.Count; ++i)
        {
            var current = scanPoints[orderedIndexes[i]];
            var last = scanPoints[envelopeStack[envelopeStack.Count - 1]];
            var secondLast = scanPoints[envelopeStack[envelopeStack.Count - 2]];

            while (envelopeStack.Count >= 2 && !IsConvex(secondLast, last, current))
            {
                envelopeStack.RemoveAt(envelopeStack.Count - 1);
                
                if (envelopeStack.Count < 2)
                    break;
                
                last = scanPoints[envelopeStack[envelopeStack.Count - 1]];
                secondLast = scanPoints[envelopeStack[envelopeStack.Count - 2]];
            }
            
            envelopeStack.Add(orderedIndexes[i]);
        }

        return envelopeStack;
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
