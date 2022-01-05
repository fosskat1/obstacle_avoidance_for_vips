using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;
using System.IO;

public class ObjectDetection_TinyYoloV2 : MonoBehaviour
{

	public NNModel modelAsset;
	private Model m_RuntimeModel;
	private IWorker m_Worker;

	public RenderTexture renderTexture;
	private Texture2D tex2d;

	private const float MINIMUM_CONFIDENCE = 0.3f;

	public const int ROW_COUNT = 13;
    public const int COL_COUNT = 13;
    public const int BOXES_PER_CELL = 5;
    public const int BOX_INFO_FEATURE_COUNT = 5;
    public const int CLASS_COUNT = 20;
    public const float CELL_WIDTH = 32;
    public const float CELL_HEIGHT = 32;

    private float[] anchors = new float[]
    {
        1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F
    };

    //For saving image
    public int FileCounter = 0;
    //For saving image - end

    private string[] labels = File.ReadLines("coco_labels.txt").ToArray();

    // Start is called before the first frame update
    void Start()
    {
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        this.m_Worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, m_RuntimeModel);
    }

    // Update is called once per frame
    void Update()
    {

    	// assumes you have your RenderTexture renderTexture
		tex2d = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

		RenderTexture.active = renderTexture;
		tex2d.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		tex2d.Apply();

    	using var input = new Tensor(1, 416, 416, 3);
    	var test_im = 0f;

    	for (var y = 0; y < tex2d.height; y++)
        {
            for (var x = 0; x < tex2d.width; x++)
            {
            	for (var c = 0; c < 3; c++)
            	{
            		if (tex2d.GetPixel(x, y)[c] > test_im)
            		{
            			test_im = tex2d.GetPixel(x, y)[c];	
            		}
            		
            		input[0, tex2d.height - y - 1, x, c] = tex2d.GetPixel(x, y)[c];
            	}
            }
        }

        // Debug.Log(string.Format("test_im = {0}", test_im));


        /*
        //For saving image
        var Bytes = tex2d.EncodeToJPG();
        
        File.WriteAllBytes(Application.dataPath + "/ImageOutputs/" + FileCounter + ".jpg", Bytes);
        FileCounter = FileCounter + 1;
        //For saving image - end
        */

		// print(input);
		this.m_Worker.Execute(input);
		Tensor output = this.m_Worker.PeekOutput();
		var results = ParseOutputs(output, 0.1F);
        var boxes = FilterBoundingBoxes(results, 5, MINIMUM_CONFIDENCE);

       	Debug.Log(string.Format("boxes.Count = {0}", boxes.Count));
        for (int i = 0; i < boxes.Count; i++)
        {
            Debug.Log(string.Format("Label = {0}", boxes[i].Label));
        }

		input.Dispose();
		output.Dispose();
    }

    void OnDisable()
    {
        this.m_Worker.Dispose();
    }

    private IList<BoundingBox> ParseOutputs(Tensor yoloModelOutput, float threshold = .3F)
    {
        var boxes = new List<BoundingBox>();

        for (int cy = 0; cy < COL_COUNT; cy++)
        {
            for (int cx = 0; cx < ROW_COUNT; cx++)
            {
                for (int box = 0; box < BOXES_PER_CELL; box++)
                {
                    var channel = (box * (CLASS_COUNT + BOX_INFO_FEATURE_COUNT));
                    var bbd = ExtractBoundingBoxDimensions(yoloModelOutput, cx, cy, channel);
                    float confidence = GetConfidence(yoloModelOutput, cx, cy, channel);

                    if (confidence < threshold)
                    {
                        continue;
                    }

                    float[] predictedClasses = ExtractClasses(yoloModelOutput, cx, cy, channel);
                    var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                    var topScore = topResultScore * confidence;

                    if (topScore < threshold)
                    {
                        continue;
                    }

                    var mappedBoundingBox = MapBoundingBoxToCell(cx, cy, box, bbd);
                    boxes.Add(new BoundingBox
                    {
                        Dimensions = new BoundingBoxDimensions
                        {
                            X = (mappedBoundingBox.X - mappedBoundingBox.Width / 2),
                            Y = (mappedBoundingBox.Y - mappedBoundingBox.Height / 2),
                            Width = mappedBoundingBox.Width,
                            Height = mappedBoundingBox.Height,
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


    private float[] Softmax(float[] values)
    {
        var maxVal = values.Max();
        var exp = values.Select(v => Math.Exp(v - maxVal));
        var sumExp = exp.Sum();

        return exp.Select(v => (float)(v / sumExp)).ToArray();
    }


    private BoundingBoxDimensions ExtractBoundingBoxDimensions(Tensor modelOutput, int x, int y, int channel)
    {
        return new BoundingBoxDimensions
        {
            X = modelOutput[0, x, y, channel],
            Y = modelOutput[0, x, y, channel + 1],
            Width = modelOutput[0, x, y, channel + 2],
            Height = modelOutput[0, x, y, channel + 3]
        };
    }


    private float GetConfidence(Tensor modelOutput, int x, int y, int channel)
    {
        return Sigmoid(modelOutput[0, x, y, channel + 4]);
    }


    private CellDimensions MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions)
    {
        return new CellDimensions
        {
            X = ((float)y + Sigmoid(boxDimensions.X)) * CELL_WIDTH,
            Y = ((float)x + Sigmoid(boxDimensions.Y)) * CELL_HEIGHT,
            Width = (float)Math.Exp(boxDimensions.Width) * CELL_WIDTH * anchors[box * 2],
            Height = (float)Math.Exp(boxDimensions.Height) * CELL_HEIGHT * anchors[box * 2 + 1],
        };
    }


    public float[] ExtractClasses(Tensor modelOutput, int x, int y, int channel)
    {
        float[] predictedClasses = new float[CLASS_COUNT];
        int predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;

        for (int predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++)
        {
            predictedClasses[predictedClass] = modelOutput[0, x, y, predictedClass + predictedClassOffset];
        }

        return Softmax(predictedClasses);
    }


    private ValueTuple<int, float> GetTopResult(float[] predictedClasses)
    {
        return predictedClasses
            .Select((predictedClass, index) => (Index: index, Value: predictedClass))
            .OrderByDescending(result => result.Value)
            .First();
    }


    private float IntersectionOverUnion(Rect boundingBoxA, Rect boundingBoxB)
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

/*
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

    // public string Label { get; set; }
    public int Label { get; set; }

    public float Confidence { get; set; }

    public Rect Rect
    {
        get { return new Rect(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height); }
    }

    public override string ToString()
    {
        return $"{Label}:{Confidence}, {Dimensions.X}:{Dimensions.Y} - {Dimensions.Width}:{Dimensions.Height}";
    }
}
*/
