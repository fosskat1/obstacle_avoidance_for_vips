using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bounding_box : MonoBehaviour
{
    public Collider m_Collider;
    public Camera camera;
    public Transform cone;
    public LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer.useWorldSpace = true;
    }

    // Update is called before every frame
    void Update()
    {
        // get camera coordinates
        Vector3 min = m_Collider.bounds.min;
        Vector3 max = m_Collider.bounds.max;
        Vector3 conePosMin = camera.WorldToScreenPoint(new Vector3(max.x, min.y, min.z));
        Vector3 conePosMax = camera.WorldToScreenPoint(new Vector3(max.x, max.y, max.z));
        Debug.Log("target min: " + conePosMin.x + ", " + conePosMin.y +  "; target max: " + conePosMax.x + ", " + conePosMax.y);

        // Render line to check if pixel coordinates are correct
        RenderPixelLine(conePosMin, conePosMax);
    }

    void RenderPixelLine(Vector3 conePosMin, Vector3 conePosMax)
    {
        Vector3 t = camera.ScreenToWorldPoint(conePosMin);
        Vector3 t1 = camera.ScreenToWorldPoint(conePosMax);
        Debug.Log(t + ", " + t1);
        lineRenderer.SetPosition(0, t);
        lineRenderer.SetPosition(1, t1);
    }

}
