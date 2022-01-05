using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LogTrace : MonoBehaviour
{
    // Start is called before the first frame update
	private GameObject dummy;
	private GameObject obstacleFireHydrant;
	private GameObject obstacleStopSign;
	private GameObject obstacleBike;
	private GameObject leftSidewalkBound;
	private GameObject rightSidewalkBound;
    private GameObject endSidewalkBound;
    private GameManager _manager;
    private List<GameObject> obstaclesWithColliders;
    // private string filePath;

    void Awake(){
         _manager = GameObject.FindObjectOfType<GameManager>();
    }
    void Start()
    {
        obstaclesWithColliders = new List<GameObject>();
    	string filePath = string.Format("Traces/trace_{0}.csv", _manager.episodeNumber);
    	StreamWriter traceWriter = new StreamWriter(new FileStream(filePath, FileMode.Create, FileAccess.Write));
    	string header = "time,dummyX,dummyY,dummyZ,fireHydrantX,fireHydrantY,fireHydrantZ,stopSignX,stopSignY,stopSignZ,bikeX,bikeY,bikeZ,leftSidewalkBoundZ,rightSidewalkBoundZ,endSidewalkBoundX,bikeColliderXMin,bikeColliderXMax,bikeColliderClosestX,bikeColliderClosestZ,hydrantColliderXMin,hydrantColliderXMax,hydrantColliderClosestX,hydrantColliderClosestZ,stopColliderXMin,stopColliderXMax,stopColliderClosestX,stopColliderClosestZ,dummyColliderXMin,dummyColliderXMax,dummyColliderClosestX,dummyColliderClosestZ";
    	traceWriter.WriteLine(header);
    	traceWriter.Close();

        dummy = GameObject.Find("FemaleDummy");
        obstacleFireHydrant = GameObject.Find("Hydrant_01_New_Prefab");
        obstacleStopSign = GameObject.Find("road_sign_stop_1");
        obstacleBike = GameObject.Find("BMXBikeE");
        leftSidewalkBound = GameObject.Find("LeftSidewalkBound");
        rightSidewalkBound = GameObject.Find("RightSidewalkBound");
        endSidewalkBound = GameObject.Find("SidewalkEnd");
        obstaclesWithColliders.Add(obstacleBike); 
        obstaclesWithColliders.Add(obstacleFireHydrant);
        obstaclesWithColliders.Add(obstacleStopSign);
        obstaclesWithColliders.Add(dummy);      

    }

    // Update is called once per frame
    void Update()
    {
    	LogToFile();
    }

    void OnDisable()
    {
    	// LogToFile();
        // traceWriter.Close();
    }

    private string GetPositionString(Vector3 position)
    {
    	string vectorString = position.ToString();
    	string positionString = vectorString.Substring(1, vectorString.Length-2);

    	return positionString;
    }

    private void LogToFile()
    {
    	float timeElapsed = Time.time;
        Vector3 dummyPosition = dummy.transform.position;
        Vector3 fireHydrantPosition = obstacleFireHydrant.transform.position;
        Vector3 StopSignPosition = obstacleStopSign.transform.position;
        Vector3 bikePosition = obstacleBike.transform.position;
        // Hard coded values for the sidewalk bounds
        float leftSidewalkBoundZ = leftSidewalkBound.transform.position.z;//0.5f;
        float rightSidewalkBoundZ = rightSidewalkBound.transform.position.z - rightSidewalkBound.transform.lossyScale.z;//3.5f;
        float endSidewalkBoundX = endSidewalkBound.transform.position.x + 1; // adding for buffer
        // Debug.Log($"sidewalk: {leftSidewalkBoundZ}, {rightSidewalkBoundZ}, {endSidewalkBoundX}");

        string dummyPositionString = GetPositionString(dummyPosition);
        string fireHydrantPositionString = GetPositionString(fireHydrantPosition);
        string StopSignPositionString = GetPositionString(StopSignPosition);
        string bikePositionString = GetPositionString(bikePosition);

        List<string> valueStrings = new List<string>();
        valueStrings.Add(timeElapsed.ToString());
        valueStrings.Add(dummyPositionString);
        valueStrings.Add(fireHydrantPositionString);
        valueStrings.Add(StopSignPositionString);
        valueStrings.Add(bikePositionString);
        valueStrings.Add(leftSidewalkBoundZ.ToString());
        valueStrings.Add(rightSidewalkBoundZ.ToString());
        valueStrings.Add(endSidewalkBoundX.ToString());
        for(int i=0; i<obstaclesWithColliders.Count; i++){
            Collider collider = obstaclesWithColliders[i].GetComponent<Collider>();
            //Debug.Log($"{collider.name}: {collider.bounds.min.x}, {collider.bounds.max.x}");
            valueStrings.Add(collider.bounds.min.x.ToString());
            valueStrings.Add(collider.bounds.max.x.ToString());
            valueStrings.Add(collider.ClosestPoint(dummy.transform.position).x.ToString());
            valueStrings.Add(collider.ClosestPoint(dummy.transform.position).z.ToString());
        }
        

        string filePath = string.Format("Traces/trace_{0}.csv", _manager.episodeNumber);
    	StreamWriter traceWriter = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write));
        string traceLogString = string.Join(",", valueStrings);
        traceWriter.WriteLine(traceLogString);
        traceWriter.Close();       

        //Debug.Log(traceLogString);
    }
}
