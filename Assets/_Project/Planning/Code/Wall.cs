using UnityEngine;
using System.Collections;

public class Wall : MonoBehaviour {

    public DCEL.HalfEdge Edge;

    /// <summary>
    /// Finds the nearest point on the wall to the provided point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector3 NearestPoint(Vector3 p)
    {
        if (Edge == null || Edge.Twin == null) {  return Vector3.zero; }
        Vector3 a = new Vector3(Edge.Origin.X, 0, Edge.Origin.Y);
        Vector3 b = new Vector3(Edge.Twin.Origin.X, 0, Edge.Twin.Origin.Y);
        Vector3 ap = p - a;
        Vector3 ab = b - a;

        float absDistance = ab.magnitude;
        float magnitudeAB = ab.sqrMagnitude;
        float abapProduct = Vector3.Dot(ap, ab);
        float distance = abapProduct / magnitudeAB;

        if (absDistance * distance < 0.5)
        {
            return a;
        }
        else if (absDistance * distance > (absDistance - 0.5))
        {
            return b;
        }
        else
        {
            return a + ab * distance;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        ContactPoint point = collision.contacts[0];
        Debug.Log("Collision: " + point.point);
    }
}
