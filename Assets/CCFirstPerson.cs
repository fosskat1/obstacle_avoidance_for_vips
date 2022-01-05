using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCFirstPerson : MonoBehaviour
{
    public CharacterController controller;
    public Animator walkAnimation;
    public static float speed = 0.1f;

    public static Vector3 direction = new Vector3(-1f, 0f, 0f).normalized;

    private GameManager _manager;

    void Awake(){
        _manager = GameObject.FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(_manager.humanMovementEnabled){
            controller.Move(direction * speed * Time.deltaTime);
        }
        else{
            walkAnimation.enabled = false;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.transform.tag == "Obstacle") 
        {
            walkAnimation.enabled = false;
            Debug.Log($"WE HIT AN OBSTACLE: {hit.transform.name}");

            // start next episode
            _manager.NextEpisode();
            
        }
    }
}
