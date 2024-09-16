using UnityEngine;

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