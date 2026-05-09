using UnityEngine;
using UnityEngine.UI;
using Unity.InferenceEngine;
using System.Collections;

public class CNNVisualizer : MonoBehaviour
{
    [Header("Feature Grid 부모 오브젝트")]
    public RectTransform feat1Grid;
    public RectTransform feat2Grid;
    public RectTransform feat3Grid;

    [Header("Settings")]
    public int   showCount = 8;
    public int   cellSize  = 56;

    [Header("레이어 활성화 애니메이션")]
    public Image[] layerHighlights;
    public float   animDuration  = 0.15f;
    public Color   activeColor   = new Color(0.2f, 0.8f, 1f, 1f);
    public Color   inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private RawImage[]  _feat1Images, _feat2Images, _feat3Images;
    private Texture2D[] _feat1Texs,  _feat2Texs,  _feat3Texs;

    void Start()
    {
        _feat1Images = BuildGrid(feat1Grid, showCount, cellSize);
        _feat2Images = BuildGrid(feat2Grid, showCount, cellSize);
        _feat3Images = BuildGrid(feat3Grid, showCount, cellSize);

        _feat1Texs = CreateTextures(showCount, 14, 14);
        _feat2Texs = CreateTextures(showCount, 7,  7);
        _feat3Texs = CreateTextures(showCount, 3,  3);

        ClearAll();
    }

    public void UpdateFeatureMaps(
        Tensor<float> feat1,
        Tensor<float> feat2,
        Tensor<float> feat3,
        float[] probs)
    {
        FillTextures(_feat1Texs, _feat1Images, feat1, 32,  14, 14);
        FillTextures(_feat2Texs, _feat2Images, feat2, 64,  7,  7);
        FillTextures(_feat3Texs, _feat3Images, feat3, 128, 3,  3);
        StartCoroutine(AnimateLayers());
    }

    public void ClearAll()
    {
        ClearGrid(_feat1Texs, _feat1Images, 14, 14);
        ClearGrid(_feat2Texs, _feat2Images, 7,  7);
        ClearGrid(_feat3Texs, _feat3Images, 3,  3);
        if (layerHighlights != null)
            foreach (var h in layerHighlights)
                if (h != null) h.color = inactiveColor;
    }

    private RawImage[] BuildGrid(RectTransform parent, int count, int size)
    {
        if (parent == null) return new RawImage[0];
        var layout = parent.GetComponent<GridLayoutGroup>()
                  ?? parent.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize        = new Vector2(size, size);
        layout.spacing         = new Vector2(2, 2);
        layout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment  = TextAnchor.LowerCenter;

        var images = new RawImage[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"FM_{i}", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            images[i] = go.GetComponent<RawImage>();
            images[i].color = Color.white;
        }
        return images;
    }

    private Texture2D[] CreateTextures(int count, int w, int h)
    {
        var texs = new Texture2D[count];
        for (int i = 0; i < count; i++)
        {
            texs[i]            = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texs[i].filterMode = FilterMode.Point;
        }
        return texs;
    }

    private void FillTextures(Texture2D[] texs, RawImage[] images,
                               Tensor<float> feat, int channels, int h, int w)
    {
        int count = Mathf.Min(showCount, channels);
        for (int c = 0; c < count; c++)
        {
            float minV = float.MaxValue, maxV = float.MinValue;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = feat[0, c, y, x];
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;
            }
            float range = Mathf.Max(maxV - minV, 1e-6f);

            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = HeatmapColor((feat[0, c, y, x] - minV) / range);

            texs[c].SetPixels32(pixels);
            texs[c].Apply();
            images[c].texture = texs[c];
        }
    }

    private void ClearGrid(Texture2D[] texs, RawImage[] images, int w, int h)
    {
        if (texs == null) return;
        for (int i = 0; i < texs.Length; i++)
        {
            if (texs[i] == null) continue;
            Color32[] pixels = new Color32[w * h];
            for (int j = 0; j < pixels.Length; j++)
                pixels[j] = new Color32(30, 30, 30, 255);
            texs[i].SetPixels32(pixels);
            texs[i].Apply();
            if (images != null && i < images.Length && images[i] != null)
                images[i].texture = texs[i];
        }
    }

    private Color32 HeatmapColor(float t)
    {
        t = Mathf.Clamp01(t);
        Color c = t < 0.5f
            ? Color.Lerp(new Color(0.1f, 0.1f, 0.8f), new Color(0.1f, 0.8f, 0.1f), t * 2f)
            : Color.Lerp(new Color(0.1f, 0.8f, 0.1f), new Color(0.9f, 0.1f, 0.1f), (t - 0.5f) * 2f);
        return c;
    }

    private IEnumerator AnimateLayers()
    {
        if (layerHighlights == null) yield break;
        foreach (var h in layerHighlights)
            if (h != null) h.color = inactiveColor;
        for (int i = 0; i < layerHighlights.Length; i++)
        {
            if (layerHighlights[i] == null) continue;
            layerHighlights[i].color = activeColor;
            yield return new WaitForSeconds(animDuration);
            if (i < layerHighlights.Length - 1)
                layerHighlights[i].color = inactiveColor;
        }
    }
}
