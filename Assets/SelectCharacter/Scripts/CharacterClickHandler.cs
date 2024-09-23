using UnityEngine;

/// <summary>
/// 캐릭터 클릭 이벤트을 관리하기 위한 스크립트
/// </summary>
/// <author>정은찬</author>
/// <since>2024.09.09</since>
/// <version>1.0</version>
/// <remarks>
/// 수정일: 2024.09.09, 수정자: 정은찬, 최초 생성
/// </remarks>
public class CharacterClickHandler : MonoBehaviour
{
    public System.Action<GameObject> OnCharacterClicked;

    void OnMouseDown()
    {
        if (OnCharacterClicked != null)
        {
            OnCharacterClicked(gameObject); // 캐릭터 클릭 시 이벤트 호출
        }
    }
}
