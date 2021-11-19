using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;

public class canny : MonoBehaviour
{
    public Texture2D texture;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
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
        Debug.Log(mask);
        //RenderFrame(mask);
    }
}
