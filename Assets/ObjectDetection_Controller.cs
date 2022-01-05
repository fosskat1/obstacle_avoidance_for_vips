using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using UnityEngine.Rendering;
//using UnityEditor.Scripting.Python;

public class ObjectDetection_Controller : MonoBehaviour
{
    // Object Detection variables
	public NNModel modelAsset;
	private Model m_RuntimeModel;
	private IWorker m_Worker;

    public const int GRID_SIZE = 52;
    public const int NUM_ANCHORS = 3; 
    public const float XY_SCALE = 1.2F;
    public const int STRIDES = 8;
    public int[,] ANCHORS = new int[,] {{12,16}, {19,36}, {40,28}}; // 12,16, 19,36, 40,28
    public const int CLASS_COUNT = 80;
    public const int IM_SIZE = 416;
    private const float MINIMUM_CONFIDENCE = 0.3f;

    private string[] labels = File.ReadLines("coco_labels.txt").ToArray();

    // Texture variables
	private Texture2D tex2d;
    private int RENDER_TEXTURE_SIZE;

    // Canny and edge detection variables
    public Camera FirstPersonCamera;
    private Material Mat;
    private int CannyFileCounter;
    private int DepthFileCounter;

    // Controller variables
    // public CharacterController controller;

    private const int NUM_LANES = 3;
    private int currentLane = NUM_LANES/2; // middle lane
    public float speed = 0.1f;   

    public int MAX_EPISODE_NUMBER = 3;
    public bool runDefaultEpisode = true;

    private GameManager _manager;

    void Awake(){
        _manager = GameObject.FindObjectOfType<GameManager>();
    }

    // Start is called before the first frame update
    void Start()
    {   
        // Initialise object detection model worker for barracuda
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        // For CPU
        //this.m_Worker = WorkerFactory.CreateWorker(
        //   WorkerFactory.Type.CSharpBurst, m_RuntimeModel
        //   );
        // For GPU
        this.m_Worker = GraphicsWorker.GetWorker(m_RuntimeModel);

        // Initialise variables for textures and file saving
        CannyFileCounter = 0;
        DepthFileCounter = 0;
        FirstPersonCamera.depthTextureMode = DepthTextureMode.Depth;
        RENDER_TEXTURE_SIZE = 416;
        RenderTexture depthRender = new RenderTexture(
            RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 24
            );

        GameObject sidewalkEnd = GameObject.Find("SidewalkEnd");

        // set up episode
        if(runDefaultEpisode){
            if(_manager.episodeNumber != 1){
                SetUpEpisode();
            }
        }
        else{
            SetUpEpisode();
        }
    }

    void SetUpEpisode(){
        // get game manager
        Debug.Log($"Episode Number: {_manager.episodeNumber}");

        // get obstacles
        GameObject hydrant = GameObject.Find("Hydrant_01_New_Prefab");
        GameObject bike = GameObject.Find("BMXBikeE");
        GameObject stop = GameObject.Find("road_sign_stop_1");
        List<GameObject> obstacles = new List<GameObject>();
        obstacles.Add(hydrant);
        obstacles.Add(bike);
        obstacles.Add(stop);

        // get sidewalk end
        GameObject sidewalkEnd = GameObject.Find("SidewalkEnd");

        // place obstacles at random X locations
        for(int i = 0; i < 3; i++){
            var obstacle = obstacles[i];
            float randX = UnityEngine.Random.Range(sidewalkEnd.transform.position.x + 1, 6.0f);
            obstacle.transform.position = new Vector3(randX, obstacle.transform.position.y, obstacle.transform.position.z);
            //Debug.Log($"{obstacle.name} moved to position {obstacle.transform.position.x}");
        }

        // enable human to move
        _manager.humanMovementEnabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mat == null)
        {
            // assign shader
            Mat = new Material(Shader.Find("Hidden/depthShader"));
        }

        if(_manager.episodeNumber > GameManager.MAX_EPISODES){
            _manager.humanMovementEnabled = false;
        }

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // shrink render texture to size needed to yolo
        RenderTexture rt = new RenderTexture( RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 24);
        Graphics.Blit(source, rt);
        Texture2D sourceTexture2D = rt.toTexture2D();
        var boxes = GetBoundingBoxesFromTexture(sourceTexture2D);

        // depth render
        Texture2D depthTexture = RenderDepth(rt);
        Mat depthMask = OpenCvSharp.Unity.TextureToMat(depthTexture);
        var temp = new List<float>(5);
        // uncomment to see depth rendered to screen
        //Graphics.Blit(source, destination, Mat);

        // draw bounding boxes
        temp = DrawBoundingBoxes(boxes, depthTexture, sourceTexture2D, temp);

        // Debug.Log(string.Join(", ", temp));

        // canny edge detection
        Texture2D cannyTexture = RenderCannyEdgeDetection(rt);
        // uncomment to see canny edge detection rendered on screen
        //Graphics.Blit(cannyTexture, destination);

        String dir = GetDirection(boxes, cannyTexture, depthTexture);

        //Debug.Log("Free direction - " + dir);

        MoveInDirection(dir);

        // render normal simulation
        Graphics.Blit(sourceTexture2D, destination);
    }

    void OnDisable()
    {
        if(this.m_Worker != null){
            this.m_Worker.Dispose();
        }
    }

    // Controller functions
    private String GetDirection(
        IList<BoundingBox> boxes, 
        Texture2D cannyTexture, 
        Texture2D depthTexture
        )
    {
        int[] edgeX = GetSidewalkEdgeX(cannyTexture);
        // Debug.Log(string.Format("Sidewalk edges are - {0}, {1}", edgeX[0], edgeX[1]));

        // Check straight
        var currentLane = GetCurrentLane(edgeX);
        //Debug.Log(string.Format("Current lane - {0}",currentLane));
        var dir = "STRAIGHT";
        if (IsLaneFree(boxes, currentLane, depthTexture, edgeX))
            return dir;

        // Function gives preference to RIGHT before LEFT
        // Check right
        int right_lane = currentLane + 1;
        dir = "RIGHT";
        while (right_lane < NUM_LANES)
        {
            if (IsLaneFree(boxes, right_lane, depthTexture, edgeX) && CanGoDirection(cannyTexture, dir))
                return dir;
            right_lane++;
        }

        // Check left
        int left_lane = currentLane - 1;
        dir = "LEFT";
        while (left_lane >= 0)
        {
            if (IsLaneFree(boxes, left_lane, depthTexture, edgeX) && CanGoDirection(cannyTexture, dir))
                return dir;
            left_lane--;
        }
        
        // No free direction
        return "STOP";
    }

    private bool CanGoDirection(Texture2D texture, String direction){
        int center = (int)(IM_SIZE / 2);
        // number of pixels to search for edge
        int searchThreshold = 40;
        int start = center;
        int end = center + searchThreshold;
        if(direction == "LEFT"){
            start = center - searchThreshold;
            end = center;
        }
        else if(direction == "RIGHT"){}
        else{
            // TODO LEFT or RIGHT string should be object
            throw new InvalidOperationException("Direction must be string LEFT or RIGHT");
        }

        for(int i = start; i<= end; i++)
            if(texture.GetPixel(i,0) == Color.black){
                // Debug.Log($"NEARING {direction} EDGE: {Math.Abs(center - i)}");
                return false;
            }

        return true;
    }

    private void MoveInDirection(String direction)
    {
        Vector3 dir = new Vector3();
        float s = 1.0f;
        if (direction == "STRAIGHT")
        {
            dir = new Vector3(-1f, 0f, 0f).normalized;
            s = 0.8f;
        }  
        else if (direction == "LEFT")
        {
            dir = new Vector3(-0.5f, 0f, -1f).normalized;
            s = 1.0f; // 1.0f/Time.deltaTime;
        }
        else if (direction == "RIGHT")
        {
            dir = new Vector3(-0.5f, 0f, 1f).normalized;
            s = 1.0f; // 1.0f/Time.deltaTime;
        }
        else if (direction == "STOP")
        {
            // TODO: Different logic for STOP
            //walkAnimation.enabled = false;
            s = 0f;

            // Logic to search for a different path by moving a
            // little backwards
            // dir = new Vector3(1f, 0f, 0f).normalized;
            // s = 0.5f;

            Debug.Log("NO PERCIEVED PATH FORWARD");
            _manager.NextEpisode();
        }
        else
        {
            _manager.humanMovementEnabled = true;
            dir = new Vector3(-1f, 0f, 0f).normalized;
            s = 0.8f;
        }

        CCFirstPerson.direction = dir;
        CCFirstPerson.speed = s;
    }

    private bool IsLaneFree(
        IList<BoundingBox> boxes, 
        int lane, 
        Texture2D depthTexture,
        int[] edgeX
        )
    {
        for (int idx_b = 0; idx_b < boxes.Count; idx_b++)
        {
            var box = boxes[idx_b];
            float max = box.GetAverageIntOfBox(depthTexture, IM_SIZE);
            if (box.isClose && IsBoxBlockingLane(boxes[idx_b], lane, edgeX))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBoxBlockingLane(BoundingBox box, int lane, int[] edgeX) // , Mat depthMask
    {   
        int buffer = 15;

        int box_x_min = (int)box.Dimensions.X;
        int box_x_max = box_x_min + (int)box.Dimensions.Width;

        int lane_size = (int)((edgeX[1] - edgeX[0])/NUM_LANES);
        int lane_x_min = edgeX[0] + lane_size*lane;
        int lane_x_max = edgeX[0] + lane_size*(lane+1);

        // Debug.Log(string.Format("Lane edges - {0}, {1}", lane_x_min, lane_x_max));

        //Debug.Log(string.Format("Box X = {0}, {1}, Lane {2} = {3}, {4}", box_x_min, box_x_max, lane, lane_x_min, lane_x_max));

        if ((box_x_min < lane_x_max - buffer) && (box_x_max > lane_x_min + buffer))
        {
            // Debug.Log(true);
            return true;
        }

        return false;
    }

    private int[] GetSidewalkEdgeX(Texture2D cannyTexture)
    {
        int center = (int)(IM_SIZE / 2);

        int leftEdge = center;
        while (cannyTexture.GetPixel(leftEdge, 0) != Color.black && leftEdge > 0)
            leftEdge--;

        int rightEdge = center;
        while (cannyTexture.GetPixel(rightEdge, 0) != Color.black && rightEdge < IM_SIZE) 
            rightEdge++;

        int[] edgeX = new int[] {leftEdge, rightEdge};

        return edgeX;
    }

    private int GetCurrentLane(int[] edgeX)
    {
        var buffer = 10;
        int center = (int)(IM_SIZE / 2);
        int lane_size = (int)((edgeX[1] - edgeX[0])/NUM_LANES);

        for (int lane = 0; lane < NUM_LANES; lane++)
        {
            int lane_x_min = edgeX[0] + lane_size*lane;
            int lane_x_max = edgeX[0] + lane_size*(lane+1);

            if (center >= lane_x_min+buffer && center <= lane_x_max-buffer)
                return lane;
        }

        return 1;
    }

    // Perception functions
    private IList<BoundingBox> GetBoundingBoxesFromTexture(Texture2D tex2d)
    {
        // Converting texture data to a tensor to feed to model
        using var input = new Tensor(1, IM_SIZE, IM_SIZE, 3);

        // Iterating through tensor to set values pixel-wise
        for (var y = 0; y < tex2d.height; y++)
        {
            for (var x = 0; x < tex2d.width; x++)
            {
                for (var c = 0; c < 3; c++)
                {   
                    input[0, tex2d.height - y - 1, x, c] = tex2d.GetPixel(x, y)[c];
                }
            }
        }

        this.m_Worker.Execute(input);
        Tensor output = this.m_Worker.PeekOutput();

        var results = ParseOutputs(output, MINIMUM_CONFIDENCE);
        var boxes = FilterBoundingBoxes(results, 5, MINIMUM_CONFIDENCE);
        
        // Debug.Log(string.Format("boxes.Count = {0}", boxes.Count));
        
        input.Dispose();
        output.Dispose();

        return boxes;
    }

    private Texture2D RenderDepth(RenderTexture source){
        // create new render texture to render depth
        RenderTexture depthRender = new RenderTexture( RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 24);
        // apply depth from material to render texture
        Graphics.Blit(source, depthRender, Mat);

        Texture2D texture = new Texture2D(depthRender.width, depthRender.height, TextureFormat.RGB24, false);
        texture = depthRender.toTexture2D();

        // Add small stochastic error to the texture
        Texture2D noisyTexture = new Texture2D(depthRender.width, depthRender.height, TextureFormat.RGB24, false);
        AddNoiseToDepthTexture(texture, noisyTexture);

        // Check if noise is added
        // Color old_pixel = texture.GetPixel(208, 104);
        // Color new_pixel = noisyTexture.GetPixel(208, 104);
        // Debug.Log(string.Format("Old Pixel: " + old_pixel.ToString() + " New Pixel: " + new_pixel.ToString()));

        WriteDepthTextureToJpeg(noisyTexture);

        return noisyTexture;
    }

    private Texture2D RenderCannyEdgeDetection(RenderTexture source){
        // shrink the size of the render texture
        RenderTexture rt = new RenderTexture( RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 24);
        Graphics.Blit(source, rt);
        Texture2D texture = rt.toTexture2D();

        Mat image = OpenCvSharp.Unity.TextureToMat(texture);
            
        // convert image to grayscale
        Mat grayImage = new Mat();
        Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

        // Clean up image using Gaussian Blur
        Mat imageGrayBlur = new Mat();
        Cv2.GaussianBlur(grayImage, imageGrayBlur, new Size(5, 5), 0);

        // Extract Edges
        Mat cannyEdges = new Mat();
        Cv2.Canny(imageGrayBlur, cannyEdges, 10.0, 70.0);

        // Do an invert binarize the image
        Mat mask = new Mat();
        Cv2.Threshold(cannyEdges, mask, 70.0, 255.0, ThresholdTypes.BinaryInv);

        Texture2D output2d = null;
        output2d = OpenCvSharp.Unity.MatToTexture(mask, output2d);
        WriteCannyTextureToJpeg(output2d);
        return output2d;
    }

    // Canny and depth output helper functions
    void WriteCannyTextureToJpeg(Texture2D texture){
        CannyFileCounter = WriteTextureToJpeg(texture, "CannyEdgeDetection", CannyFileCounter);
    }

    void WriteDepthTextureToJpeg(Texture2D texture){
        DepthFileCounter = WriteTextureToJpeg(texture, "Depth", DepthFileCounter);
    }

    int WriteTextureToJpeg(Texture2D texture, string tempDirectoryName, int fileCounter)
    {
        // don't write more than 100 images to disk, and only write an image everyother frame
        if(fileCounter % 2 == 0 && fileCounter < 100 * 2){
            var Bytes = texture.EncodeToJPG();
            string projectFolder = Path.Combine( Application.dataPath, "../" );
            File.WriteAllBytes(projectFolder + "/tempImages/" + tempDirectoryName + "/"+ fileCounter/2 + ".jpg", Bytes);
        }
        return fileCounter+1;
    }

    // Depth helper function
    private void AddNoiseToDepthTexture(Texture2D texture, Texture2D noisyTexture)
    {
        float noiseScale = 0.015f;
        System.Random rnd = new System.Random();

        for (int x = 0; x < RENDER_TEXTURE_SIZE; x++)
        {
            for (int y = 0; y < RENDER_TEXTURE_SIZE; y++)
            {
                // float sample = Mathf.PerlinNoise((float)x/RENDER_TEXTURE_SIZE, (float)y/RENDER_TEXTURE_SIZE) - 0.5f;
                float sample = (float)rnd.NextDouble() - 0.5f;

                Color pixelColor = texture.GetPixel(x, y);
                Color noisyColor = new Color(
                    pixelColor.r + noiseScale*sample,
                    pixelColor.g + noiseScale*sample,
                    pixelColor.b + noiseScale*sample
                    );
                noisyTexture.SetPixel(x, y, noisyColor);

                // if ((x == 208) && (y == 104))
                // {
                //     Debug.Log(string.Format("Noise - {0}", sample));
                //     Debug.Log("Old Pixel color " + pixelColor.ToString());
                //     Debug.Log("Noisy Pixel color " + noisyColor.ToString());
                // }
            }
        }
        noisyTexture.Apply();
    }

    // Object detection output helper functions
    private List<float> DrawBoundingBoxes(
        IList<BoundingBox> boxes, 
        Texture2D depthTexture,
        Texture2D sourceTexture2D,
        List<float> temp
        )
    {
        for (int i = 0; i < boxes.Count; i++){
            var box = boxes[i];
            float max = box.GetAverageIntOfBox(depthTexture, IM_SIZE);
            temp.Add(max);
            Color color = Color.green;
            var imagePoints = box.GetBoxPointsInImage(IM_SIZE);
            if(box.isClose){
                color= Color.red;
            }
            sourceTexture2D.DrawLine(imagePoints[0], imagePoints[1], color);
            sourceTexture2D.DrawLine(imagePoints[1], imagePoints[2], color);
            sourceTexture2D.DrawLine(imagePoints[2], imagePoints[3], color);
            sourceTexture2D.DrawLine(imagePoints[3], imagePoints[0], color);
            sourceTexture2D.Apply();
        }

        return temp;
    }

    // Object detection helper functions
    private IList<BoundingBox> ParseOutputs(Tensor yoloModelOutput, float threshold = .3F)
    {
    	var boxes = new List<BoundingBox>();

    	for (int cx = 0; cx < GRID_SIZE; cx++)
    	{
    		for (int cy = 0; cy < GRID_SIZE; cy++)
    		{
    			for (int i = 0; i < NUM_ANCHORS; i++)
    			{
					var X = (
						(Expit(yoloModelOutput[0, cx, cy, i, 0])*XY_SCALE)
						- 0.5F*(XY_SCALE - 1) + cy
						) * STRIDES;
					var Y = (
						(Expit(yoloModelOutput[0, cx, cy, i, 1])*XY_SCALE)
						- 0.5F*(XY_SCALE - 1) + cx
						) * STRIDES;

					if ((X < 0) || (Y < 0))
					{
						continue;
					}

					var w = (
						(float)Math.Exp(yoloModelOutput[0, cx, cy, i, 2]) * 
						ANCHORS[i, 0]
						);
					var h = (
						(float)Math.Exp(yoloModelOutput[0, cx, cy, i, 3]) * 
						ANCHORS[i, 1]
						);

					if ((w > IM_SIZE-1) || (h > IM_SIZE-1))
					{
						continue;
					}

					var confidence = yoloModelOutput[0, cx, cy, i, 4];
					
					if (confidence < threshold)
                    {
                        continue;
                    }

					float[] predictedClasses = new float[CLASS_COUNT];
					int predictedClassOffset = 5;
					for (int predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++)
			        {
			            predictedClasses[predictedClass] = yoloModelOutput[0, cx, cy, i, predictedClass + predictedClassOffset];
			        }
			        var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                	var topScore = topResultScore * confidence;

                	if (topScore < threshold)
                    {
                        continue;
                    }

                    boxes.Add(new BoundingBox
                    {
                        Dimensions = new BoundingBoxDimensions
                        {
                            X = (X - w / 2),
                            Y = (Y - h / 2),
                            Width = w,
                            Height = h,
                        },
                        Confidence = topScore,
                        Label = labels[topResultIndex] // topResultIndex
                    });
    			}
    			
    		}
    	}
    	return boxes;
    }

    private float Sigmoid(float value)
    {
        var k = (float)Math.Exp(value);

        return k / (1.0f + k);
    }

    private float Expit(float value)
    {
        var k = (float)Math.Exp(-value);

        return k / (1.0f + k);
    }

    private float[] Softmax(float[] values)
    {
        var maxVal = values.Max();
        var exp = values.Select(v => Math.Exp(v - maxVal));
        var sumExp = exp.Sum();

        return exp.Select(v => (float)(v / sumExp)).ToArray();
    }

    private ValueTuple<int, float> GetTopResult(float[] predictedClasses)
    {
        return predictedClasses
            .Select((predictedClass, index) => (Index: index, Value: predictedClass))
            .OrderByDescending(result => result.Value)
            .First();
    }

    private float IntersectionOverUnion(UnityEngine.Rect boundingBoxA, UnityEngine.Rect boundingBoxB)
    {
        var areaA = boundingBoxA.width * boundingBoxA.height;

        if (areaA <= 0)
            return 0;

        var areaB = boundingBoxB.width * boundingBoxB.height;

        if (areaB <= 0)
            return 0;

        var minX = Math.Max(boundingBoxA.xMin, boundingBoxB.xMin);
        var minY = Math.Max(boundingBoxA.yMin, boundingBoxB.yMin);
        var maxX = Math.Min(boundingBoxA.xMax, boundingBoxB.xMax);
        var maxY = Math.Min(boundingBoxA.yMax, boundingBoxB.yMax);

        var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

        return intersectionArea / (areaA + areaB - intersectionArea);
    }

    private IList<BoundingBox> FilterBoundingBoxes(IList<BoundingBox> boxes, int limit, float threshold)
    {
        var activeCount = boxes.Count;
        var isActiveBoxes = new bool[boxes.Count];

        for (int i = 0; i < isActiveBoxes.Length; i++)
        {
            isActiveBoxes[i] = true;
        }

        var sortedBoxes = boxes.Select((b, i) => new { Box = b, Index = i })
                .OrderByDescending(b => b.Box.Confidence)
                .ToList();

        var results = new List<BoundingBox>();

        for (int i = 0; i < boxes.Count; i++)
        {
            if (isActiveBoxes[i])
            {
                var boxA = sortedBoxes[i].Box;
                results.Add(boxA);

                if (results.Count >= limit)
                    break;

                for (var j = i + 1; j < boxes.Count; j++)
                {
                    if (isActiveBoxes[j])
                    {
                        var boxB = sortedBoxes[j].Box;

                        if (IntersectionOverUnion(boxA.Rect, boxB.Rect) > threshold)
                        {
                            isActiveBoxes[j] = false;
                            activeCount--;

                            if (activeCount <= 0)
                                break;
                        }
                    }
                }

                if (activeCount <= 0)
                    break;
            }
        }

        return results;
    }
}

public class DimensionsBase
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Height { get; set; }
    public float Width { get; set; }
}


public class BoundingBoxDimensions : DimensionsBase { }

class CellDimensions : DimensionsBase { }


public class BoundingBox
{
    public BoundingBoxDimensions Dimensions { get; set; }

    public string Label { get; set; }
    // public int Label { get; set; }

    public float Confidence { get; set; }

    public bool isClose = false;

    public List<Vector2> GetBoxPointsInImage(int image_size){
        Vector2 point1 = new Vector2(this.Dimensions.X, image_size - this.Dimensions.Y);
        Vector2 point2 = new Vector2(point1[0] + this.Dimensions.Width, point1[1]);
        Vector2 point3 = new Vector2(point2[0], point2[1] - this.Dimensions.Height);
        Vector2 point4 = new Vector2(point3[0] - this.Dimensions.Width, point3[1]);
        List<Vector2> imagePoints = new List<Vector2>(4);
        imagePoints.Add(point1);
        imagePoints.Add(point2);
        imagePoints.Add(point3);
        imagePoints.Add(point4);
        return imagePoints;
    }

    public float GetAverageIntOfBox(Texture2D texture, int imageSize){
        List<Vector2> points = GetBoxPointsInImage(imageSize);
        List<float> pixels = new List<float>(416);
        float max = 0f;
        float sum = 0f;
        int count = 0;
        for(int i = (int)points[0][0]; i < (int)points[1][0]; i++ ){
            for(int j = (int)points[3][1]; j < (int)points[0][1]; j++){
                float pixel = texture.GetPixel(i,j).grayscale;
                if(pixel > max){
                    max = pixel;
                }
                sum += pixel;
                count += 1;
            }
        }
        float average = sum/count;
        // Debug.Log($"avg: {average}, max: {max}");
        float depthThreshold = 0.08f;
        if(max > depthThreshold){
            this.isClose = true;
        }
        /*for(int i = 0; i < 416; i++ ){
            for(int j = 0; j < 416; j++){
                Color pixel = texture.GetPixel(i,j);
                pixels.Add(pixel.grayscale);
            }
        }*/
        return max;

    }

    public UnityEngine.Rect Rect
    {
        get { return new UnityEngine.Rect(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height); }
    }

    public override string ToString()
    {
        return $"{Label}:{Confidence}, {Dimensions.X}:{Dimensions.Y} - {Dimensions.Width}:{Dimensions.Height}";
    }
}