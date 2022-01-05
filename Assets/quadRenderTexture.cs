using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class help : MonoBehaviour
{
    private Texture2D texture;
    // Start is called before the first frame update
    void Start()
    {
        texture = GameObject.Find("FirstPersonCamera").GetComponent<canny>().output2d;
        //Texture2D texture = Resources.Load<Texture>("TextureName") as Texture;
    }

    // Update is called once per frame
    void Update()
    {
        //Texture2D myTexture = Resources.Load("my_image") as Texture2D;
        GetComponent<Renderer>().material.mainTexture = texture;
    }
}
