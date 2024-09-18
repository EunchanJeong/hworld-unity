// #if UNITY_EDITOR // 이 코드 블록은 에디터에서만 실행되도록 설정
// using UnityEditor; // AssetDatabase를 사용하기 위해 필요한 네임스페이스
// #endif

using UnityEngine;

public class PlayerEquipmentManager : MonoBehaviour
{
    // 캐릭터의 본 위치
    public Transform headBone; // 모자를 장착할 본
    public Transform handBone; // 가방을 장착할 본

    public Transform neckBone; // 목걸이를 장착할 본
    private GameObject equippedHat;
    private GameObject equippedNecklace;
    private GameObject equippedGlasses;
    private GameObject equippedBag;

    private string fbxPath = "Items/";

    void Start()
    {
        // "Player" 게임 오브젝트의 Transform을 찾아 handBone을 설정
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            handBone = FindBone(playerObject.transform, "hand_l");
            if (handBone == null)
            {
                Debug.LogError("hand_l 본을 찾을 수 없습니다.");
            }
            
            neckBone = FindBone(playerObject.transform, "head");
            if (neckBone == null)
            {
                Debug.LogError("head 본을 찾을 수 없습니다.");
            }

            headBone = FindBone(playerObject.transform, "head");
            if (headBone == null)
            {
                Debug.LogError("head 본을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("'Player'라는 이름을 가진 게임 오브젝트를 찾을 수 없습니다.");
        }

        LoadEquippedItems();
    }

    // 저장된 장비를 불러오는 함수
    void LoadEquippedItems()
    {
        if (PlayerPrefs.HasKey("EquippedHat"))
        {
            int hatOptionId = PlayerPrefs.GetInt("EquippedHat");
            EquipItemOnCharacter(hatOptionId, 1); // 카테고리 1: 모자
        }

        if (PlayerPrefs.HasKey("EquippedNecklace"))
        {
            int necklaceOptionId = PlayerPrefs.GetInt("EquippedNecklace");
            EquipItemOnCharacter(necklaceOptionId, 2); // 카테고리 2: 목걸이
        }

        if (PlayerPrefs.HasKey("EquippedGlasses"))
        {
            int glassesOptionId = PlayerPrefs.GetInt("EquippedGlasses");
            EquipItemOnCharacter(glassesOptionId, 3); // 카테고리 3: 안경
        }

        if (PlayerPrefs.HasKey("EquippedBag"))
        {
            int bagOptionId = PlayerPrefs.GetInt("EquippedBag");
            EquipItemOnCharacter(bagOptionId, 4); // 카테고리 4: 가방
        }
    }

    // 장착 아이템을 캐릭터에 부착하는 함수
    void EquipItemOnCharacter(int itemOptionId, int categoryId)
    {
        // #if UNITY_EDITOR
        // // 에디터에서만 AssetDatabase를 사용하여 프리팹을 로드
        // string categoryName = GetCategoryNameById(categoryId);
        // string fbxFileName = $"{categoryName}_{itemOptionId}.fbx";
        // string fbxFilePath = $"{fbxPath}{fbxFileName}";

        // GameObject itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFilePath);

        // if (itemPrefab == null)
        // {
        //     Debug.LogError($"FBX 파일을 찾을 수 없습니다: {fbxFilePath}");
        //     return;
        // }
        // #else
        // 런타임에서는 Resources.Load를 사용하여 프리팹을 로드
        string categoryName = GetCategoryNameById(categoryId);
        string prefabPath = $"Items/{categoryName}_{itemOptionId}"; // Resources 폴더 내 경로
        
        GameObject itemPrefab = Resources.Load<GameObject>(prefabPath);

        if (itemPrefab == null)
        {
            Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }
        // #endif

        // 장착 로직
        switch (categoryId)
        {
            case 1: // 모자
                RemoveExistingHat();
                if (equippedHat != null) Destroy(equippedHat);
                equippedHat = Instantiate(itemPrefab, headBone);
                equippedHat.transform.localPosition = new Vector3(0.0f, 0.000536f, 0.00010f);
                equippedHat.transform.localRotation = Quaternion.Euler(new Vector3(3.872f, -179.781f, -0.145f)); // z축을 90도 회전
                equippedHat.transform.localScale = new Vector3(0.00324f, 0.00350f, 0.00412f); // 주신 로컬 스케일 값 적용
                break;

            case 2: // 목걸이
                RemoveExistingNecklace();
                if (equippedNecklace != null) Destroy(equippedNecklace);
                equippedNecklace = Instantiate(itemPrefab, neckBone);
                equippedNecklace.transform.localPosition = Vector3.zero;
                equippedNecklace.transform.localRotation = Quaternion.identity;
                break;

            case 3: // 안경
                RemoveExistingGlasses();
                if (equippedGlasses != null) Destroy(equippedGlasses);
                equippedGlasses = Instantiate(itemPrefab, headBone);
                equippedGlasses.transform.localPosition = new Vector3(0.00032f, 0.00018f, 0.00102f);
                equippedGlasses.transform.localRotation = Quaternion.Euler(new Vector3(-0.404f, 91.894f, 0.013f)); // z축을 90도 회전
                equippedGlasses.transform.localScale = new Vector3(0.000258f, 0.000258f, 0.000258f); // 주신 로컬 스케일 값 적용
                break;

            case 4: // 가방
                RemoveExistingBag(); // 기존 가방 삭제
                equippedBag = Instantiate(itemPrefab, handBone); // 손 본에 가방 장착
                equippedBag.transform.localPosition = new Vector3(-0.00123f, 0.00301f, -0.00181f);
                equippedBag.transform.localRotation = Quaternion.Euler(new Vector3(-1.488f, 118.743f, 134.081f)); // z축을 90도 회전
                equippedBag.transform.localScale = new Vector3(0.000973f, 0.000688f, 0.000167f); // 주신 로컬 스케일 값 적용
                break;

            default:
                Debug.LogError("잘못된 카테고리 ID입니다.");
                break;
        }
    }

    // handBone에서 "bag"이라는 이름이 포함된 오브젝트를 찾아서 삭제
    void RemoveExistingBag()
    {
        foreach (Transform child in handBone)
        {
            if (child.name.ToLower().Contains("bag"))
            {
                Destroy(child.gameObject); // "bag"이 포함된 오브젝트를 삭제
                Debug.Log("기존 가방을 삭제했습니다.");
            }
        }
    }

    void RemoveExistingNecklace()
    {
        foreach (Transform child in neckBone)
        {
            if (child.name.ToLower().Contains("necklace"))
            {
                Destroy(child.gameObject); // "bag"이 포함된 오브젝트를 삭제
                Debug.Log("기존 목걸이을 삭제했습니다.");
            }
        }
    }

    void RemoveExistingGlasses()
    {
        foreach (Transform child in headBone)
        {
            if (child.name.ToLower().Contains("glasses"))
            {
                Destroy(child.gameObject); // "bag"이 포함된 오브젝트를 삭제
                Debug.Log("기존 안경을 삭제했습니다.");
            }
        }
    }

    void RemoveExistingHat()
    {
        foreach (Transform child in headBone)
        {
            if (child.name.ToLower().Contains("hat"))
            {
                Destroy(child.gameObject); // "bag"이 포함된 오브젝트를 삭제
                Debug.Log("기존 모자을 삭제했습니다.");
            }
        }
    }

    // 트랜스폼 내에서 이름이 대소문자 구분 없이 특정 이름을 가진 본을 찾는 함수
    Transform FindBone(Transform parent, string boneName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (string.Equals(child.name, boneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }
        return null;
    }

    // 카테고리 ID에 따라 카테고리 이름을 반환하는 함수
    string GetCategoryNameById(int categoryId)
    {
        switch (categoryId)
        {
            case 1: return "hat";      // 모자
            case 2: return "necklace"; // 목걸이
            case 3: return "glasses";  // 안경
            case 4: return "bag";      // 가방
            default: return "unknown";
        }
    }
}