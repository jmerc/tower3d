using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Blueprint : MonoBehaviour {


    enum Tool {None, Place, Pan, Zoom};
    public float Scale = 1.0f;
    public float OffsetX = 0.0f;
    public float OffsetY = 0.0f;
    public bool SnapToGrid;
    public GameObject PaperObject;
    public GameObject WallObject;
    
    private Tool currentMouseMode;
    private Vector3 mouseStart;
    private GameObject newObject;
    private bool newObjectValid;

    private GameObject paper;
    private DCEL lines;
    private List<GameObject> walls;
    private List<GameObject> floors;

    void Awake()
    {
        // Initialize privates
        currentMouseMode = Tool.None;
        lines = new DCEL();
        walls = new List<GameObject>();
        floors = new List<GameObject>();
    }

    // Called after all objects are Awake
    void Start () {
        // Create an instance of paper and add it as a child
        paper = Instantiate<GameObject>(PaperObject);
        paper.transform.parent = gameObject.transform;

        // Set blueprint dimension
        Vector3 blueprintSize = paper.transform.localScale;

        Material mat = paper.GetComponent<Renderer>().material;
        mat.mainTextureScale = new Vector2(blueprintSize.x / Scale, blueprintSize.y / Scale);
	}
	
	// Update is called once per frame
	void Update () {

        Vector3 mousePoint;
        // Detect mouse interaction
        if (Input.GetMouseButtonDown(0))
        {
           if (ClickLocation(out mousePoint))
            {
                ToolStart(mousePoint);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (ClickLocation(out mousePoint))
            {
                ToolEnd(mousePoint);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (ClickLocation(out mousePoint))
            {
                ToolUpdate(mousePoint);
            }
        }
    }

    bool ClickLocation(out Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hitInfo = new RaycastHit();
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
        {
            // Check for a hit on one of our children
            foreach (Transform childTransform in transform)
            {
                if (hitInfo.collider == childTransform.gameObject.GetComponent<Collider>())
                {
                    point = hitInfo.point;
                    point.y = 0;  // Flatten point in case it's hitting on a wall
                    if (SnapToGrid)
                    {
                        point = SnapPoint(point, childTransform.gameObject);
                    }
                    return true;
                }
            }
        }

        // No hit found
        point = Vector3.zero;
        return false;
    }

    private Vector3 SnapPoint(Vector3 point, GameObject gameObject)
    {
        Wall wall = gameObject.GetComponent<Wall>();
        if (wall == null || wall.Edge == null)
        {
            return SnapPointToGrid(point);
        }
        else
        {
            return wall.NearestPoint(point);
        }
    }

    private Vector3 SnapPointToGrid(Vector3 point)
    {
        if (SnapToGrid == false)
        {
            return point;
        }
        // Find closest intersection
        Vector3 snappedPoint = new Vector3();

        snappedPoint.y = point.y;
        snappedPoint.x = Mathf.Round(point.x * Scale) / Scale;
        snappedPoint.z = Mathf.Round(point.z * Scale) / Scale;
        return snappedPoint;
    }

    void ToolStart(Vector3 point)
    {
        mouseStart = point;
        //Debug.Log("ToolStart - " + point);

        // Determine which tool we are using
        currentMouseMode = Tool.Place;
        // Create a new instance of a wall
        newObject = Instantiate<GameObject>(WallObject);
       //newObject.transform.parent = gameObject.transform;  // Not sure what I get for doing this
        newObject.transform.Translate(mouseStart);
        newObject.transform.localScale = new Vector3(0, 1, 1);
        newObject.GetComponent<Collider>().enabled = false;
        newObjectValid = true;
    }

    void ToolUpdate(Vector3 point)
    {
        // TODO - only update if previous point was different
        // TODO: switch statement?
        if (currentMouseMode == Tool.Place && newObject != null)
        {
            // Transform wall so it matches this point
            float length = Vector3.Distance(mouseStart, point);
            newObject.transform.rotation = Quaternion.FromToRotation(Vector3.right, point - mouseStart);
            newObject.transform.localScale = new Vector3(length, 1, 1);
            // Determine if this wall is valid (TODO: This should be checked in the DCEL itself)
            Collider[] colliders = Physics.OverlapBox((point + mouseStart) / 2 + new Vector3(0, 0.2f, 0), new Vector3(length / 2 - 0.4f, 0.01f, 0.01f), newObject.transform.rotation);
            if (colliders.Length > 1)
            {
                newObjectValid = false;
                // Tint red
                GameObject wall = newObject.transform.GetChild(0).gameObject;
                wall.GetComponent<Renderer>().material.color = Color.red;
            }
            else
            {
                newObjectValid = true;
                // Remove tint
                GameObject wall = newObject.transform.GetChild(0).gameObject;
                wall.GetComponent<Renderer>().material.color = Color.white;

            }
        }
    }

    void ToolEnd(Vector3 point)
    {
        //Debug.Log("ToolEnd - " + mouseStart + " -> " + point);

        if (currentMouseMode == Tool.Place && newObject != null)
        {
            // Determine if we're keeping newObject
            if (point != mouseStart && newObjectValid == true)
            {
                // Create a new edge in the DCEL
                lines.CreateEdge(mouseStart.x, mouseStart.z, point.x, point.z);
                // Render any DCEL changes
                RedrawWalls();
                RedrawFloors();
            }        
        }

        // Remove the temporary new object
        if (newObject != null)
        {
            Destroy(newObject);
            newObject = null;
        }
        currentMouseMode = Tool.None;
    }

    private void RedrawWalls()
    {
        IEnumerator<GameObject> wallEnumerator = walls.GetEnumerator();
       // wallEnumerator.Reset();
        GameObject wall;
        List<GameObject> newWalls = new List<GameObject>();

        foreach (DCEL.HalfEdge edge in lines.Edges)
        {
            if (wallEnumerator.MoveNext() == true)
            {
                wall = wallEnumerator.Current;
            }
            else
            {
                wall = Instantiate<GameObject>(WallObject);
                wall.transform.parent = gameObject.transform;
                newWalls.Add(wall);
            }
            DrawWall(wall, edge);
        }
        wallEnumerator.Dispose();
        walls.AddRange(newWalls);
    }

    private void DrawWall(GameObject wall, DCEL.HalfEdge edge)
    {
        Vector3 start = new Vector3(edge.Origin.X, 0, edge.Origin.Y);
        Vector3 end = new Vector3(edge.Twin.Origin.X, 0, edge.Twin.Origin.Y);

        wall.transform.position = start;
        wall.transform.rotation = Quaternion.FromToRotation(Vector3.right, end - start);
        wall.transform.localScale = new Vector3(Vector3.Distance(start, end), 1, 1);
        wall.GetComponent<Wall>().Edge = edge;
    }

    private void RedrawFloors()
    {
        IEnumerator<GameObject> floorEnumerator = floors.GetEnumerator();
        // wallEnumerator.Reset();
        GameObject floor;
        List<GameObject> newFloors = new List<GameObject>();

        foreach (DCEL.Face face in lines.Faces)
        {
            if (floorEnumerator.MoveNext() == true)
            {
                floor = floorEnumerator.Current;
            }
            else
            {
                floor = new GameObject("Floor");
                floor.transform.parent = gameObject.transform;
                newFloors.Add(floor);
            }
            DrawFloors(floor, face);
        }
        floorEnumerator.Dispose();
        floors.AddRange(newFloors);
    }

    private void DrawFloors(GameObject floor, DCEL.Face face)
    {
        // Create an array of Vectors based on the points in face
        List<DCEL.HalfEdge> edges = face.Edges;
        Vector2[] faceVertices = new Vector2[edges.Count];

        for (int i = 0; i < edges.Count; i++)
        {
            faceVertices[i] = new Vector2(edges[i].Origin.X, edges[i].Origin.Y);
        }
        Triangulator tr = new Triangulator(faceVertices);
        int[] indices = tr.Triangulate();
       
        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[faceVertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(faceVertices[i].x, 0.1f, faceVertices[i].y);
        }

        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices;
        msh.triangles = indices;
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        // Set up game object with mesh;
        MeshRenderer renderer = floor.GetComponent<MeshRenderer>();
        if (renderer == null) { renderer = floor.AddComponent<MeshRenderer>(); }

        MeshFilter filter = floor.GetComponent<MeshFilter>();
        if (filter == null) { filter = floor.AddComponent<MeshFilter>(); }
        //filter.mesh.Clear(); - proably not needed
        filter.mesh = msh;

        renderer.material.color = Color.white;
    }

}
