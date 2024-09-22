using UnityEngine;

/// <summary>
/// 캐릭터의 위치를 저장하고 불러오기 위한 스크립트
/// </summary>
/// <author>조영욱</author>
/// <since>2024.09.09</since>
/// <version>1.0</version>
/// <remarks>
/// 수정일: 2024.09.09, 수정자: 조영욱, 최초 생성
/// </remarks>
public class PlayerPositionManager : MonoBehaviour
{
    public static Vector3 savedPosition;
    public static bool hasSavedPosition = false;

    public static void SavePosition(Vector3 position)
    {
        savedPosition = position;
        hasSavedPosition = true;
        Debug.Log("Save Player Position");
    }

    public static Vector3 LoadPosition()
    {
        Debug.Log("Load Player Position");
        return savedPosition;
    }
}
