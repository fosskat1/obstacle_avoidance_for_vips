using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.Rendering;
using System;
using System.IO;

public class canny : MonoBehaviour
{
    public RenderTexture renderTexture;
    private Texture2D texture;
    public RenderTexture outputRenderTexture;
    public Texture2D output2d;
    public int FileCounter = 0;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new UnityEngine.Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

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

        output2d = OpenCvSharp.Unity.MatToTexture(mask, output2d);
        WriteTextureToJpeg(output2d);
        WriteFile(mask.Dump());
        // create output render texture
        //outputRenderTexture = new RenderTexture(output2d.width / 2, output2d.height / 2, 0);
        FileCounter += 1;
    }

    void WriteTextureToJpeg(Texture2D texture)
    {
        var Bytes = texture.EncodeToJPG();
        File.WriteAllBytes(Application.dataPath + "/ImageOutputs/" + FileCounter + ".jpg", Bytes);
    }

    void WriteFile(string line)
    {
        // Set a variable to the Documents path.
        Debug.Log(Path.Combine(Directory.GetCurrentDirectory(), "WriteLines.txt"));

        // Write the string array to a new file named "WriteLines.txt".
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "WriteLines.txt")))
        {
            outputFile.WriteLine(line);
        }
    }
}
