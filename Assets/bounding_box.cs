using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bounding_box : MonoBehaviour
{
    public Collider m_Collider;
    private Vector3 m_Min;
    private Vector3 m_Max;
    private Vector3 m_Size;
    private Vector3 m_Center;
    public Transform cameraTransform;
    public LineRenderer line;
    public Camera camera;
    public Transform cone;

    // Start is called before the first frame update
    void Start()
    {
        //Fetch the Collider from the GameObject
        m_Collider = GetComponent<Collider>();
        //Fetch the center of the Collider volume
        m_Center = m_Collider.bounds.center;
        //Fetch the size of the Collider volume
        m_Size = m_Collider.bounds.size;
        //Fetch the minimum and maximum bounds of the Collider volume
        m_Min = m_Collider.bounds.min;
        m_Max = m_Collider.bounds.max;
        //Output this data into the console
        OutputData();
    }

    // Update is called before every frame
    void Update()
    {
        Vector3 relativePosition = cameraTransform.InverseTransformDirection(transform.position - cameraTransform.position);
        Debug.Log("Relavive: " + relativePosition);

        Vector3 screenPos = camera.WorldToScreenPoint(cone.position);
        Debug.Log("target is " + screenPos.x + ", " + screenPos.y +  " pixels from the bottom left");
    }

    void OutputData()
    {
        //Output to the console the center and size of the Collider volume
        Debug.Log("Collider Center : " + m_Center);
        Debug.Log("Collider Size : " + m_Size);
        Debug.Log("Collider bound Minimum : " + m_Min);
        Debug.Log("Collider bound Maximum : " + m_Max);
    }



}
