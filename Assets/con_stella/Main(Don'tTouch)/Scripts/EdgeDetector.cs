using UnityEngine;
using System.Collections.Generic;
using OpenCvSharp;

public class EdgeDetector : MonoBehaviour
{
    [Header("輪郭抽出パラメータ")]
    [SerializeField] private double threshold1 = 50;
    [SerializeField] private double threshold2 = 150;
    [SerializeField] private double minAreaRatio = 0.005; // 0.5%以下のゴミは無視

    // 近似精度（基本値）
    [SerializeField] private double epsilonRatio = 0.005;

    /// <summary>
    /// 画像から「主要な輪郭の点のリスト」を抽出して返す
    /// </summary>
    public List<List<Vector2>> DetectContours(Texture2D inputTexture)
    {
        List<List<Vector2>> resultContours = new List<List<Vector2>>();

        // 1. リサイズ（高速化）
        int processW = 512;
        int processH = 512;
        Texture2D resizedTex = ResizeTexture(inputTexture, processW, processH);

        Mat srcMat = OpenCvSharp.Unity.TextureToMat(resizedTex);
        Mat grayMat = new Mat();
        Cv2.CvtColor(srcMat, grayMat, ColorConversionCodes.BGR2GRAY);

        // 2. ブラーとエッジ検出
        Mat blurredMat = new Mat();
        Cv2.GaussianBlur(grayMat, blurredMat, new Size(5, 5), 1.5);
        Mat edgesMat = new Mat();
        Cv2.Canny(blurredMat, edgesMat, threshold1, threshold2);

        // 膨張処理（線を太くして、切れ切れの線をくっつける）
        Mat dilatedMat = new Mat();
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.Dilate(edgesMat, dilatedMat, kernel);

        // 3. 輪郭検出
        Point[][] contours;
        HierarchyIndex[] hierarchy;
        Cv2.FindContours(dilatedMat, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        // ★自動調整ロジック（変数の定義はここで行う）
        int totalContours = contours.Length;
        double currentEpsilonRatio = epsilonRatio; // 基本値をセット

        // 輪郭が多すぎる場合は、カクカク具合を強める
        if (totalContours > 100)
        {
            currentEpsilonRatio *= 2.0;
            Debug.Log($"輪郭多数({totalContours})のため、近似を強くします: {currentEpsilonRatio}");
        }
        else if (totalContours < 10)
        {
            currentEpsilonRatio *= 0.5;
            Debug.Log($"輪郭少数({totalContours})のため、詳細に残します: {currentEpsilonRatio}");
        }

        // 画像の総面積（ゴミ捨て基準用）
        double imageArea = processW * processH;

        // 4. 輪郭処理ループ
        foreach (var contour in contours)
        {
            double area = Cv2.ContourArea(contour);
            if (area < imageArea * minAreaRatio) continue; // 小さいゴミは無視

            // ★ここで currentEpsilonRatio を使う
            double perimeter = Cv2.ArcLength(contour, true);
            double epsilon = currentEpsilonRatio * perimeter;

            // 近似（カクカク化）
            Point[] approxCurve = Cv2.ApproxPolyDP(contour, epsilon, true);

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

        // メモリ解放
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