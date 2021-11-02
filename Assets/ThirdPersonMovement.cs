using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 0.1f;

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = new Vector3(0f, 0f, 1f).normalized;
        controller.Move(direction * speed * Time.deltaTime);   
    }
}
