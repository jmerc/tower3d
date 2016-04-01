using UnityEngine;
using System.Collections;

public class Blueprint : MonoBehaviour {


    enum Tool {None, Place, Pan, Zoom};
    public float scale = 1.0f;
    public float offsetX = 0.0f;
    public float offsetY = 0.0f;
    public bool snapToGrid;
    public GameObject blueprint;
    public GameObject wall;
    
    private Tool currentMouseMode;
    private Vector3 mouseStart;
    private GameObject newObject;
    
    // Use this for initialization
    void Start () {
        if (blueprint)
        {
            // Set blueprint dimension
            Vector3 blueprintSize = blueprint.transform.localScale;

            Material mat = blueprint.GetComponent<Renderer>().material;
            mat.mainTextureScale = new Vector2(blueprintSize.x / scale, blueprintSize.y / scale);
        }

        // Initialize privates
        currentMouseMode = Tool.None;
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
            if (hitInfo.collider == GetComponent<Collider>())
            {
                point = hitInfo.point;
                if (snapToGrid)
                {
                    point = SnapPointToGrid(point);
                }
                return true;
            }
        }
        point = Vector3.zero;
        return false;
    }

    private Vector3 SnapPointToGrid(Vector3 point)
    {
        if (snapToGrid == false)
        {
            return point;
        }
        // Find closest intersection
        Vector3 snappedPoint = new Vector3();

        snappedPoint.y = point.y;
        snappedPoint.x = Mathf.Round(point.x * scale) / scale;
        snappedPoint.z = Mathf.Round(point.z * scale) / scale;
        return snappedPoint;
    }

    void ToolStart(Vector3 point)
    {
        mouseStart = SnapPointToGrid(point);
        Debug.Log("ToolStart - " + point + " -> " + mouseStart);

        // Determine which tool we are using
        currentMouseMode = Tool.Place;
        // Create a new instance of a wall
        newObject = Instantiate<GameObject>(wall);
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

        }

        newObject = null;
        currentMouseMode = Tool.None;
        // Determine if we're keeping newObject
    }


}
