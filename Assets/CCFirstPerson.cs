using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCFirstPerson : MonoBehaviour
{
    public CharacterController controller;
    private bool movementEnabled = true;
    public Animator walkAnimation;
    public float speed = 0.1f;


    // Update is called once per frame
    void Update()
    {
        if(movementEnabled){
            Vector3 direction = new Vector3(-1f, 0f, 0.5f).normalized;
            controller.Move(direction * speed * Time.deltaTime);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        
        if (hit.transform.tag == "Obstacle") 
        {
            Debug.Log($"WE HIT AN OBSTACLE: {hit.transform.name}");
            walkAnimation.enabled = false;
            movementEnabled = false;
        }
    }
}
