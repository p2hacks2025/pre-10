using UnityEngine;
using System.Collections.Generic;
using OpenCvSharp;

public class EdgeDetector : MonoBehaviour
{
    [Header("輪郭抽出パラメータ")]
    [SerializeField] private double threshold1 = 50;
    [SerializeField] private double threshold2 = 150;
    [SerializeField] private double minAreaRatio = 0.005; // 0.5%以下のゴミは無視

    // ★追加：近似精度（値が大きいほどカクカクになる。0.001~0.01くらいが目安）
    [SerializeField] private double epsilonRatio = 0.005;

    public List<List<Vector2>> DetectContours(Texture2D inputTexture)
    {
        List<List<Vector2>> resultContours = new List<List<Vector2>>();

        int processW = 512;
        int processH = 512;
        Texture2D resizedTex = ResizeTexture(inputTexture, processW, processH);

        Mat srcMat = OpenCvSharp.Unity.TextureToMat(resizedTex);
        Mat grayMat = new Mat();
        Cv2.CvtColor(srcMat, grayMat, ColorConversionCodes.BGR2GRAY);

        Mat blurredMat = new Mat();
        Cv2.GaussianBlur(grayMat, blurredMat, new Size(5, 5), 1.5);
        Mat edgesMat = new Mat();
        Cv2.Canny(blurredMat, edgesMat, threshold1, threshold2);

        // 膨張処理（線を太くして、切れ切れの線をくっつける）
        Mat dilatedMat = new Mat();
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.Dilate(edgesMat, dilatedMat, kernel);

        Point[][] contours;
        HierarchyIndex[] hierarchy;
        // dilatedMatを使う
        Cv2.FindContours(dilatedMat, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        double imageArea = processW * processH;

        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area < imageArea * minAreaRatio) continue; // ゴミ捨て

            // ★ここが新機能！「ポリゴン近似」
            // 輪郭の長さを測る
            double perimeter = Cv2.ArcLength(contour, true);
            // 精度を決める（長さのx%の誤差を許容する）
            double epsilon = epsilonRatio * perimeter;

            // 近似された（カクカクした）点群を取得
            Point[] approxCurve = Cv2.ApproxPolyDP(contour, epsilon, true);

            // 頂点数が少なすぎる（ただの線）場合は星座にしない
            if (approxCurve.Length < 3) continue;

            List<Vector2> unityContour = new List<Vector2>();
            foreach (var p in approxCurve)
            {
                float ux = (float)p.X - (processW / 2f);
                float uy = (float)processH - p.Y - (processH / 2f);
                unityContour.Add(new Vector2(ux, uy));
            }
            resultContours.Add(unityContour);
        }

        srcMat.Dispose(); grayMat.Dispose(); blurredMat.Dispose(); edgesMat.Dispose(); dilatedMat.Dispose();
        Destroy(resizedTex);

        return resultContours;
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D readableTexture = new Texture2D(newWidth, newHeight);
        readableTexture.ReadPixels(new UnityEngine.Rect(0, 0, newWidth, newHeight), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return readableTexture;
    }
}