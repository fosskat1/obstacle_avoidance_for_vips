using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using OpenCvSharp;

[ExecuteInEditMode]
public class depthTexture : MonoBehaviour
{
    public Camera camera;
    public Material Mat;
    private int CannyFileCounter;
    private int DepthFileCounter;
    private int RENDER_TEXTURE_SIZE;
    // Start is called before the first frame update
    void Start()
    {
        CannyFileCounter = 0;
        DepthFileCounter = 0;
        camera.depthTextureMode = DepthTextureMode.Depth;
        RENDER_TEXTURE_SIZE = 416;
        RenderTexture depthRender = new RenderTexture( 416, 416, 24);

    }

    // Update is called once per frame
    void Update()
    {
        camera.depthTextureMode = DepthTextureMode.Depth;
        if (Mat == null)
        {
            // assign shader
            Mat = new Material(Shader.Find("Hidden/depthShader"));

        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        Texture2D depthTexture = RenderDepth(source);
        Texture2D cannyEdgeTexture = RenderCannyEdgeDetection(source);

        // uncomment to see canny edge detection rendered on screen
        //Graphics.Blit(cannyEdgeTexture, destination);
        // uncomment to see depth rendered to screen
        //Graphics.Blit(source, destination, Mat);

        // render normal simulation
        Graphics.Blit(source, destination);

    }

    private Texture2D RenderDepth(RenderTexture source){
        // create new render texture to render depth
        RenderTexture depthRender = new RenderTexture( RENDER_TEXTURE_SIZE, RENDER_TEXTURE_SIZE, 24);
        // apply depth from material to render texture
        Graphics.Blit(source, depthRender, Mat);

        Texture2D texture = new Texture2D(depthRender.width, depthRender.height, TextureFormat.RGB24, false);
        texture = depthRender.toTexture2D();
        WriteDepthTextureToJpeg(texture);

        return texture;
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
        //WriteFile(mask.Dump());
        return output2d;
    }

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
}
