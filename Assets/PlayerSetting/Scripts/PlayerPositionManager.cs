using UnityEngine;

public class PlayerPositionManager : MonoBehaviour
{
    public static Vector3 savedPosition;
    public static bool hasSavedPosition = false;

    public static void SavePosition(Vector3 position)
    {
        savedPosition = position;
        hasSavedPosition = true;
    }

    public static Vector3 LoadPosition()
    {
        return savedPosition;
    }
}
