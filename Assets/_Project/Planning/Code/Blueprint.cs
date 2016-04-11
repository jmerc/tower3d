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

    private GameObject paper;
    private DCEL lines;
    private List<GameObject> walls;

    void Awake()
    {
        // Initialize privates
        currentMouseMode = Tool.None;
        lines = new DCEL();
        walls = new List<GameObject>();
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
                    Debug.Log("Mouse hit on child[" + childTransform.name + "]: " + hitInfo.point);
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
        return SnapPointToGrid(point);
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
        mouseStart = SnapPointToGrid(point);
        Debug.Log("ToolStart - " + point + " -> " + mouseStart);

        // Determine which tool we are using
        currentMouseMode = Tool.Place;
        // Create a new instance of a wall
        newObject = Instantiate<GameObject>(WallObject);
        newObject.transform.parent = gameObject.transform;
        newObject.transform.Translate(mouseStart);
        newObject.transform.localScale = new Vector3(0, 1, 1);
    }

    void ToolUpdate(Vector3 point)
    {
        // TODO: switch statement?
        if (currentMouseMode == Tool.Place && newObject != null)
        {
            // Transform wall so it matches this point
            newObject.transform.rotation = Quaternion.FromToRotation(Vector3.right, point - mouseStart);
            newObject.transform.localScale = new Vector3(Vector3.Distance(mouseStart, point), 1, 1);
        }
    }

    void ToolEnd(Vector3 point)
    {
        if (currentMouseMode == Tool.Place && newObject != null)
        {
            // Determine if we're keeping newObject
            if (point != mouseStart)
            {
                // Create a new edge in the DCEL
                lines.CreateEdge(mouseStart.x, mouseStart.z, point.x, point.z);
                // Render any DCEL changes
                RedrawWalls();
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


}
