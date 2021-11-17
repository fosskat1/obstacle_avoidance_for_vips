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
        Vector3 m_Min;
        Vector3 m_Max;
        Vector3 m_Size;
        Vector3 m_Center;
        //Fetch the Collider from the GameObject
        m_Collider = GetComponent<Collider>();
        //Fetch the center of the Collider volume
        m_Center = m_Collider.bounds.center;
        //Fetch the size of the Collider volume
        m_Size = m_Collider.bounds.size;
        //Fetch the minimum and maximum bounds of the Collider volume
        m_Min = m_Collider.bounds.min;
        m_Max = m_Collider.bounds.max;
        lineRenderer.useWorldSpace = true;


    }

    // Update is called before every frame
    void Update()
    {
        Vector3 min = m_Collider.bounds.min;
        Vector3 max = m_Collider.bounds.max;

        Vector3 screenPos = camera.WorldToScreenPoint(new Vector3(max.x, min.y, min.z));
        Vector3 screenPosMax = camera.WorldToScreenPoint(new Vector3(max.x, max.y, max.z));
        Debug.Log("target min: " + screenPos.x + ", " + screenPos.y +  "; target max: " + screenPosMax.x + ", " + screenPosMax.y);

        Vector3 t = camera.ScreenToWorldPoint(screenPos);
        Vector3 t1 = camera.ScreenToWorldPoint(screenPosMax);
        Debug.Log(t + ", " + t1);
        lineRenderer.SetPosition(0, t);
        lineRenderer.SetPosition(1, t1);



    }


}
