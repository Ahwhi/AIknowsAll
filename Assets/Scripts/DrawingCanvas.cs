using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RawImage))]
public class DrawingCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Canvas Settings")]
    public int   brushSize  = 18;
    public Color brushColor = Color.black;
    public Color bgColor    = Color.white;

    [Header("Reference")]
    public ModelInference modelInference;

    private const int TEX_SIZE = 280;

    private Texture2D     _canvasTex;
    private RawImage      _rawImage;
    private RectTransform _rt;
    private bool          _isDirty   = false;
    private bool          _isDrawing = false;
    private Vector2       _lastPos;

    // Ctrl+Z용 이전 상태 저장 (한 획 단위)
    private Color32[] _undoBuffer;

    void Awake()
    {
        _rawImage             = GetComponent<RawImage>();
        _rt                   = GetComponent<RectTransform>();
        _canvasTex            = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false);
        _canvasTex.filterMode = FilterMode.Bilinear;
        ClearCanvas();
        _rawImage.texture = _canvasTex;
    }

    void Update() {
        bool ctrl = Keyboard.current != null &&
                    (Keyboard.current.leftCtrlKey.isPressed ||
                     Keyboard.current.rightCtrlKey.isPressed);
        bool z = Keyboard.current != null &&
                     Keyboard.current.zKey.wasPressedThisFrame;

        if (ctrl && z) {
            if (GameManager.Instance != null && GameManager.Instance.TryUndo())
                ApplyUndo();
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;

        // 획 시작 전 현재 상태 저장 (언두 버퍼)
        _undoBuffer = _canvasTex.GetPixels32();

        _isDrawing = true;
        _lastPos   = ScreenToTex(e.position);
        DrawCircle(_lastPos, brushSize);
        _isDirty = true;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_isDrawing) return;
        Vector2 cur = ScreenToTex(e.position);
        DrawLine(_lastPos, cur, brushSize);
        _lastPos = cur;
        _isDirty = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;

        if (!_isDirty) return;
        _isDirty = false;

        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive) return;
        if (GameManager.Instance != null && GameManager.Instance.IsRoundAnswered) return;

        float[] input = GetInput28();
        modelInference.Predict(input);
    }

    // ModelInference에서 결과 받아서 GameManager에 전달
    public void NotifyResult(float topProb, string topClass)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDrawingSubmitted(topProb, topClass);
    }

    public void ClearCanvas()
    {
        Color32[] pixels = new Color32[TEX_SIZE * TEX_SIZE];
        Color32   bg     = bgColor;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;
        _canvasTex.SetPixels32(pixels);
        _canvasTex.Apply();
        _undoBuffer = null;
        if (modelInference != null) modelInference.ClearResult();
    }

    void ApplyUndo()
    {
        if (_undoBuffer == null) return;
        _canvasTex.SetPixels32(_undoBuffer);
        _canvasTex.Apply();
        _undoBuffer = null;
    }

    public float[] GetInput28()
    {
        float[] data = new float[28 * 28];
        for (int ty = 0; ty < 28; ty++)
        for (int tx = 0; tx < 28; tx++)
        {
            float sum = 0f;
            for (int dy = 0; dy < 10; dy++)
            for (int dx = 0; dx < 10; dx++)
            {
                int   px = tx * 10 + dx;
                int   py = (27 - ty) * 10 + dy;
                Color c  = _canvasTex.GetPixel(px, py);
                sum += 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            }
            float inv = 1.0f - (sum / 100f);
            data[ty * 28 + tx] = inv > 0.15f ? Mathf.Clamp01(inv * 2f) : 0f;
        }
        return data;
    }

    private Vector2 ScreenToTex(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rt, screenPos, null, out Vector2 local);
        float x = (local.x / _rt.rect.width  + 0.5f) * TEX_SIZE;
        float y = (local.y / _rt.rect.height + 0.5f) * TEX_SIZE;
        return new Vector2(Mathf.Clamp(x, 0, TEX_SIZE - 1),
                           Mathf.Clamp(y, 0, TEX_SIZE - 1));
    }

    private void DrawCircle(Vector2 center, int radius)
    {
        int cx = (int)center.x, cy = (int)center.y;
        int r2 = radius * radius;
        for (int y = -radius; y <= radius; y++)
        for (int x = -radius; x <= radius; x++)
        {
            if (x * x + y * y > r2) continue;
            int px = cx + x, py = cy + y;
            if (px < 0 || px >= TEX_SIZE || py < 0 || py >= TEX_SIZE) continue;
            _canvasTex.SetPixel(px, py, brushColor);
        }
        _canvasTex.Apply();
    }

    private void DrawLine(Vector2 from, Vector2 to, int radius)
    {
        float dist  = Vector2.Distance(from, to);
        int   steps = Mathf.Max(1, (int)(dist / 2));
        for (int i = 0; i <= steps; i++)
            DrawCircle(Vector2.Lerp(from, to, i / (float)steps), radius);
    }
}
