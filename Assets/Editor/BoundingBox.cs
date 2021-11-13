using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BoundingBox))]
[CanEditMultipleObjects]
public class BoundingBox : EditorWindow
{
    //This is the value of the Slider
    float m_Value;

    void OnGUI()
    {
        //This is the Label for the Slider
        m_Value = 10.0f;
        GUI.Label(new Rect(0, 300, 100, 30), "Rectangle Width");
// GUI.Box(new Rect(50, 350, m_Value, 70));
    }
}
