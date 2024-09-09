using UnityEngine;

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
