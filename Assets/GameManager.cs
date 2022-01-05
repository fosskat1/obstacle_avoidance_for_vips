 using UnityEngine;
 using UnityEngine.SceneManagement;
 
     //To be attached to an object inside the first scene
     public class GameManager : MonoBehaviour
     {
         private static bool _created = false;
 
         //Accessible only trough editor or from this class
         [SerializeField]
         public const int MAX_EPISODES = 20;
 
         public int episodeNumber;
         public bool humanMovementEnabled {get; set;}
 
         private void Awake()
         {
             if (!_created)
             {
                 DontDestroyOnLoad(this.gameObject);
                 _created = true;
                 Init();
             }
         }
 
         public void Init()
         {
             episodeNumber = 1;
             humanMovementEnabled = true;
         }

         public void NextEpisode(){
            humanMovementEnabled = false;
            // start next scene
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single) ;
            episodeNumber += 1;
         }
     }