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
	// public static string filePath = "trace.csv";
 //    private StreamWriter traceWriter = new StreamWriter(filePath);

    void Awake(){
         _manager = GameObject.FindObjectOfType<GameManager>();
    }
    void Start()
    {
    	string filePath = "trace.csv";
    	StreamWriter traceWriter = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write));
    	string header = "time,dummyX,dummyY,dummyZ,fireHydrantX,fireHydrantY,fireHydrantZ,stopSignX,stopSignY,stopSignZ,bikeX,bikeY,bikeZ,leftSidewalkBoundZ,rightSidewalkBoundZ, endSidewalkBoundX";
    	traceWriter.WriteLine(header);
    	traceWriter.Close();

        dummy = GameObject.Find("FemaleDummy");
        obstacleFireHydrant = GameObject.Find("Hydrant_01_New_Prefab");
        obstacleStopSign = GameObject.Find("road_sign_stop_1");
        obstacleBike = GameObject.Find("BMXBikeE");
        leftSidewalkBound = GameObject.Find("LeftSidewalkBound");
        rightSidewalkBound = GameObject.Find("RightSidewalkBound");
        endSidewalkBound = GameObject.Find("SidewalkEnd");
    }

    // Update is called once per frame
    void Update()
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
        Debug.Log($"sidewalk: {leftSidewalkBoundZ}, {rightSidewalkBoundZ}, {endSidewalkBoundX}");

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

        string filePath = "trace.csv";
    	StreamWriter traceWriter = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write));
        string traceLogString = string.Join(",", valueStrings);
        traceWriter.WriteLine(traceLogString);
        traceWriter.Close();
    }

    void OnDisable()
    {
        // traceWriter.Close();
    }

    private string GetPositionString(Vector3 position)
    {
    	string vectorString = position.ToString();
    	string positionString = vectorString.Substring(1, vectorString.Length-2);

    	return positionString;
    }
}
