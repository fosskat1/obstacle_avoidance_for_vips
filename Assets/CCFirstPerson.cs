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

    private bool obstacleHit = false;

    void Awake(){
        _manager = GameObject.FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (obstacleHit)
        {
            // start next episode
            _manager.NextEpisode();
        }
        else
        {
            WalkStep();
        }
        
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.transform.tag == "Obstacle") 
        {
            if(hit.transform.name != "SidewalkEnd"){
                walkAnimation.enabled = false;
                Debug.Log($"WE HIT AN OBSTACLE: {hit.transform.name}");
            }
            else{
                Debug.Log("HIT END OF EPISODE");
            }
            obstacleHit = true; 

            
        }
    }

    private void WalkStep()
    {
        //Debug.Log(string.Format("Time {0},  Direction {1}, speed {2}", Time.time, direction, speed));
        if(_manager.humanMovementEnabled){
            if (direction.z == 0f)
            {
                controller.Move(direction * speed * Time.deltaTime);    
            }
            else
            {
                controller.Move(direction * speed);
            }   
            // controller.Move(direction * speed * Time.deltaTime);
        }
        else{
            walkAnimation.enabled = false;
        }
    }
}
