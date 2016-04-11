using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class DCEL
{
    private static float POINT_THRESHOLD = 0.5f;
    
    #region DCEL_Component_Classes

    public class HalfEdge
    {
        public Vertex Origin { get; set; }
        public Face Face { get; set; }
        public HalfEdge Twin { get; set; }
        public HalfEdge Next { get; set; }
        public HalfEdge Previous { get; set; }
        public bool Rendered { get; set; }

        public HalfEdge()
        {
            Rendered = false;
        }

        public HalfEdge(Vertex origin)
        {
            Origin = origin;
            Rendered = false;
        }

        public HalfEdge(Vertex origin, HalfEdge twin)
        {
            Origin = origin;
            Twin = twin;
            Rendered = false;
        }

        public HalfEdge(Vertex origin, HalfEdge twin, Face face, HalfEdge next)
        {
            Origin = origin;
            Face = face;
            Twin = twin;
            Next = next;
            Rendered = false;
        }
    }

    public class Face
    {
        public HalfEdge Edge { get; set; }

        public Face(HalfEdge edge)
        {
            Edge = edge;
        }
    }

    public class Vertex
    {
        public float X { get; set; }
        public float Y { get; set; }

        // Any edge that has this vertex as its origin
        public HalfEdge Edge { get; set; }

        public Vertex() { }
        
        public Vertex(float x, float y)
        {
            X = x;
            Y = y;
        }

        override public string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }

    #endregion

    #region DCEL_lists

    private List<HalfEdge> edges;
    private List<Face> faces;
    private List<Vertex> vertices;

    public IList<HalfEdge> Edges
    {
        get { return edges.AsReadOnly(); }
    }

    public IList<Vertex>Vertices
    {
        get { return vertices.AsReadOnly(); }
    }

    public IList<Face> Faces
    {
        get { return faces.AsReadOnly(); }
    }

    #endregion

    public DCEL()
    {
        edges = new List<HalfEdge>();
        faces = new List<Face>();
        vertices = new List<Vertex>();
    }

    public void RunTest()
    {
        // Create points
        Vertex pStart = new Vertex(0, 0);
        Vertex pEnd = new Vertex(2, 0);
        Vertex point = new Vertex(1, 0.2f);

        // Create edges
        HalfEdge e = new HalfEdge(pStart);
        HalfEdge eTwin = new HalfEdge(pEnd, e);
        e.Twin = eTwin;
        edges.Add(e);
        edges.Add(eTwin);

        // Check if point is on any edge
        HalfEdge result = FindIntersectingEdge(point);
        if (result != null)
        {
            SplitEdge(result, point);
        }

        foreach(HalfEdge edge in edges)
        {
            Debug.Log("E(" + edge.Origin + "->" + edge.Twin.Origin);
        }
    }
    
    #region DCEL_OPERATIONS

    /// <summary>
    /// Creates a vertex at the coordinates specified unless one already exists.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>Vertex (new or existing) located at (x,y)</returns>
    public Vertex CreateVertex(float x, float y)
    {
        // This sucks, do it better
        foreach (Vertex v in vertices)
        {
            if (Math.Abs(v.X - x) < POINT_THRESHOLD && Math.Abs(v.Y - y) < POINT_THRESHOLD)
            {
                return v;
            }
        }
        // No matching vertex found, create a new one
        Vertex point = new Vertex(x, y);
        vertices.Add(point);
        Debug.Log("Creating Vertex at: " + point.ToString());

        // Determine if this new vertex falls on an edge
        HalfEdge e = FindIntersectingEdge(point);
        if (e != null) { SplitEdge(e, point); }

        return point;
    }


    /// <summary>
    /// Creates the pair of half edges between vertexes defined at (x1,y1) and (x2,y2).
    /// Creates vertexes automatically if needed at the points
    /// Updates faces and neighboring edges as needed.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <returns>The half edge with origin at (x1, y1)</returns>
    public HalfEdge CreateEdge(float x1, float y1, float x2, float y2)
    {
        if (x1 == x2 && y1 == y2) { return null; }
        Vertex v1 = CreateVertex(x1, y1);
        Vertex v2 = CreateVertex(x2, y2);
        return CreateEdge(v1, v2);
    }


    /// <summary>
    /// Creates the pair of half edges between vertex v1 and v2.  
    /// Updates faces and neighboring edges as needed.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns>The half edge with origin at v1</returns>
    public HalfEdge CreateEdge(Vertex v1, Vertex v2)
    {
        Debug.Log("Creating edge from " + v1.ToString() + " -> " + v2.ToString());

        // Create edges
        HalfEdge e = new HalfEdge(v1);
        HalfEdge eTwin = new HalfEdge(v2, e);
        e.Twin = eTwin;

        // TODO - check for edges that match up to preexisting edges
        // Update v1's edges
        if (v1.Edge == null) { v1.Edge = e; }

        // Update v2's edges
        if (v2.Edge == null) { v2.Edge = e; }

        // Create face if needed

        // Store edges
        edges.Add(e);
        edges.Add(eTwin);

        return e;
    }

    #endregion

    #region DCEL_HELPER_METHODS

    /// <summary>
    /// Calculates the counter-clockwise angle from edge to edge.next.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns>Angle from edge to edge.next, NaN if edge.next is null</returns>
    private float CalculateEdgeAngle(HalfEdge edge)
    {
        if (edge == null || edge.Next == null) { return float.NaN; }

        // Start at the origin of edge
        // Corner is the end of edge and the origin of the next edge
        // End is at the end of next edge
        return (float)CalculateAngle(edge.Origin, edge.Next.Origin, edge.Next.Twin.Origin);
    }

    /// <summary>
    /// Calculates the relative angle between two lines with a common point corner.
    /// Angle is measured on the left side of th line moving from origin to corner to endpoint.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="corner"></param>
    /// <param name="endpoint"></param>
    /// <returns>Counter Clockwise Angle</returns>
    private double CalculateAngle(Vertex start, Vertex corner, Vertex endpoint)
    {
        // Relative Angle can be calculated by taking the arc tangent of the slope of each line and subtracting line2 from line1.
        double angle = Math.Atan2(corner.Y - start.Y, corner.X - start.X) - Math.Atan2(corner.Y - endpoint.Y, corner.X - endpoint.X);
        // Convert to degrees
        angle = angle * 180 / Math.PI;
        // Cap angle to 0-360 range
        if (angle < 0) { angle += 360; }
        return angle;  // Subtract from 360 to find counter-clockwise angle
    }

    /// <summary>
    /// Finds any edges that point would bisect.
    /// </summary>
    /// <param name="point"></param>
    /// <returns>A half edge containing point.  Null if none are found</returns>
    private HalfEdge FindIntersectingEdge(Vertex point)
    {
        foreach (HalfEdge edge in edges)
        {
            if (PointOnLine(point, edge.Origin, edge.Twin.Origin) == true)
            {
                return edge;
            }
        }

        return null;
    }

    private bool PointOnLine(Vertex point, Vertex start, Vertex end)
    {
        // Point is on the line that intersects start and end if the cross product of
        // start->point and start->end is 0.
        float dxc = point.X - start.X;
        float dyc = point.Y - start.Y;

        float dxl = end.X - start.X;
        float dyl = end.Y - start.Y;

        float cross = dxc * dyl - dyc * dxl;

        if (Math.Abs(cross) > POINT_THRESHOLD)
        {
            return false;
        }

        // Make sure point is between start and end
        if (Math.Abs(dxl) >= Math.Abs(dyl))
            return dxl > 0 ?
              start.X <= point.X && point.X <= end.X :
              end.X <= point.X && point.X <= start.X;
        else
            return dyl > 0 ?
              start.Y <= point.Y && point.Y <= end.Y :
              end.Y <= point.Y && point.Y <= start.Y;
    }

    /// <summary>
    /// Splits the edge into two edges with the original edge ending at p, the new edge starting at p.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="p"></param>
    private void SplitEdge(HalfEdge e, Vertex p)
    {
        // Perform the following operation
        //         e              e     eNew  ->
        //     ==========  =>   =====P=====
        //       e.Twin       eNewTwin  eTwin <-

        HalfEdge eTwin = e.Twin;

        // Split e
        HalfEdge eNew = new HalfEdge(p, eTwin, e.Face, e.Next);
        e.Next = eNew;

        // Split e's Twin
        HalfEdge eNewTwin = new HalfEdge(p, e, eTwin.Face, eTwin.Next);
        eTwin.Next = eNewTwin;

        // Update twins
        e.Twin = eNewTwin;
        eTwin.Twin = eNew;

        // Store new edges
        edges.Add(eNew);
        edges.Add(eNewTwin);
    }

    #endregion


}
