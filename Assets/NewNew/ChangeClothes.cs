using UnityEngine;
using UnityEngine.UI;

public class ChangeClothes : MonoBehaviour
{
    // 여러 개의 Material을 배열로 관리
    public Material[] clothesMaterials;

    // Renderer를 참조하는 변수
    private Renderer characterRenderer;

    void Start()
    {
        // 캐릭터의 Renderer 컴포넌트를 찾습니다.
        characterRenderer = GetComponent<Renderer>();

        // 시작 시 첫 번째 옷을 설정합니다 (옵션)
        if (clothesMaterials.Length > 0)
        {
            ApplyClothes(0); // 첫 번째 옷 입히기
        }
    }

    // 옷을 선택하는 메서드
    public void ApplyClothes(int index)
    {
        if (index >= 0 && index < clothesMaterials.Length)
        {
            characterRenderer.material = clothesMaterials[index];
        }
        else
        {
            Debug.LogWarning("Invalid clothes index!");
        }
    }
}
