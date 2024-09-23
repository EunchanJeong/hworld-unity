using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그할 수 있는 Bar 스크립트
/// </summary>
/// <author>조영욱</author>
/// <since>2024.09.09</since>
/// <version>1.0</version>
/// <remarks>
/// 수정일: 2024.09.09, 수정자: 조영욱, 최초 생성
/// </remarks>
public class DraggableBar : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private RectTransform _horizonLineRectTransform;
    public Text _percentText;

    public float minValue = 0f;
    public float maxValue = 100f;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _horizonLineRectTransform = _rectTransform.parent.GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_horizonLineRectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            // 위치를 수평 범위 내로 클램프
            float clampedX = Mathf.Clamp(localPoint.x, -_horizonLineRectTransform.rect.width / 2, _horizonLineRectTransform.rect.width / 2);
            _rectTransform.anchoredPosition = new Vector2(clampedX, _rectTransform.anchoredPosition.y);

            // 퍼센트 텍스트 업데이트
            float normalizedValue = (clampedX + _horizonLineRectTransform.rect.width / 2) / _horizonLineRectTransform.rect.width;
            float value = Mathf.Round(normalizedValue * (maxValue - minValue));
            _percentText.text = $"{value}%";
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }
}