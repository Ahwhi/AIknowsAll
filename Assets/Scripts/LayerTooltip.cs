using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class LayerTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("툴팁 내용")]
    [TextArea(3, 6)]
    public string tooltipText;

    [Header("툴팁 UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipLabel;

    [Header("딜레이")]
    public float showDelay = 0.4f;  // 호버 후 뜨는 딜레이

    private Coroutine _showCoroutine;
    private bool _isHovering = false;

    public void OnPointerEnter(PointerEventData e) {
        _isHovering = true;
        if (_showCoroutine != null) StopCoroutine(_showCoroutine);
        _showCoroutine = StartCoroutine(ShowAfterDelay(e.position));
    }

    public void OnPointerExit(PointerEventData e) {
        _isHovering = false;
        if (_showCoroutine != null) { StopCoroutine(_showCoroutine); _showCoroutine = null; }
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private IEnumerator ShowAfterDelay(Vector2 screenPos) {
        yield return new WaitForSeconds(showDelay);
        if (!_isHovering) yield break;

        if (tooltipPanel == null || tooltipLabel == null) yield break;
        tooltipLabel.text = tooltipText;
        tooltipPanel.SetActive(true);

        if (tooltipPanel.TryGetComponent<RectTransform>(out var rt)) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt.parent as RectTransform,
                screenPos,
                null,
                out Vector2 localPos);
            rt.anchoredPosition = localPos + new Vector2(10f, -10f);
        }
    }
}