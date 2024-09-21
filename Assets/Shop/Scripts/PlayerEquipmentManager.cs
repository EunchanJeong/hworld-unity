using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

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

    private static string getCharacterItemApiUrl;
    private string fbxPath = "Items/";

    private static string authToken;
    private static string refreshToken;

    public class CharacterItemResponseDTO
    {
        public int categoryId { get; set; } // 카테고리 ID
        public int itemOptionId { get; set; } // 아이템 옵션 ID
        public string itemOption { get; set; } // 아이템 옵션 이름
        public int itemId { get; set; } // 아이템 ID
        public string itemName { get; set; } // 아이템 이름
        public int itemPrice { get; set; } // 아이템 가격
        public string imageUrl { get; set; } // 아이템 이미지 URL
    }

    void Start()
    {
        // "Player" 게임 오브젝트의 Transform을 찾아 handBone과 다른 본을 설정
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            handBone = FindBone(playerObject.transform, "hand_l");
            neckBone = FindBone(playerObject.transform, "neck_01");
            headBone = FindBone(playerObject.transform, "head");

            if (handBone == null || neckBone == null || headBone == null)
            {
                Debug.LogError("캐릭터 본을 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("'Player'라는 이름을 가진 게임 오브젝트를 찾을 수 없습니다.");
        }

        // 서버에서 장착된 아이템 데이터를 불러오고 캐릭터에 장착
        FetchCharacterItems();
    }

    // 서버에서 캐릭터의 장착 아이템을 불러오는 함수
    public void FetchCharacterItems()
    {
        PlayerEquipmentManager.GetCharacterItem(this, (List<CharacterItemResponseDTO> items) =>
        {
            foreach (var item in items)
            {
                Debug.Log($"카테고리 ID: {item.categoryId}, 아이템 옵션 ID: {item.itemOptionId}, 아이템 이름: {item.itemName}");
                // 각 아이템을 캐릭터에 장착
                EquipItemOnCharacter(item.itemOptionId, item.categoryId);
            }
        });
    }

    // 아이템을 캐릭터에 장착하는 함수
    void EquipItemOnCharacter(int itemOptionId, int categoryId)
    {
        string categoryName = GetCategoryNameById(categoryId);
        string prefabPath = $"Items/{categoryName}_{itemOptionId}"; // Resources 폴더 내 경로

        GameObject itemPrefab = Resources.Load<GameObject>(prefabPath);

        if (itemPrefab == null)
        {
            Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }

        int optionId = itemOptionId;

        // 장착 로직
        switch (categoryId)
        {
            case 1: // 모자
                RemoveExistingHat();
                if (equippedHat != null) Destroy(equippedHat);

                if(optionId == 15 || optionId == 16)
                {
                    equippedHat = Instantiate(itemPrefab, headBone); // 머리 본에 모자 장착
                    equippedHat.transform.localPosition = new Vector3(0.0f, 0.00053f, 0.0000f);
                    equippedHat.transform.localRotation = Quaternion.Euler(new Vector3(3.872f, -179.781f, -0.145f)); // z축을 90도 회전
                    equippedHat.transform.localScale = new Vector3(0.00324f, 0.00350f, 0.00389f); // 주신 로컬 스케일 값 적용
                }
                else if(optionId == 17) {
                    equippedHat = Instantiate(itemPrefab, headBone); // 머리 본에 모자 장착
                    equippedHat.transform.localPosition = new Vector3(0.0f, 0.00106f, -0.0001f);
                    equippedHat.transform.localRotation = Quaternion.Euler(new Vector3(-7.906f, -1.323f, 0.071f)); // z축을 90도 회전
                    equippedHat.transform.localScale = new Vector3(0.00077f, 0.00083f, 0.00094f); // 주신 로컬 스케일 값 적용
                }

                equippedHat.layer = 6;  // 원하는 레이어 번호로 설정
                // 하위 오브젝트들도 동일한 레이어로 설정
                foreach (Transform child in equippedHat.transform)
                {
                    child.gameObject.layer = 6;
                }
                break;

            case 2: // 목걸이
                RemoveExistingNecklace();
                if (equippedNecklace != null) Destroy(equippedNecklace);
                
                if(optionId == 11)
                {
                    equippedNecklace = Instantiate(itemPrefab, neckBone); // 몸 본에 목걸이 장착
                    equippedNecklace.transform.localPosition = new Vector3(0.0f, 0.00037f, 0.00138f);
                    equippedNecklace.transform.localRotation = Quaternion.Euler(new Vector3(-0.934f, -177.521f, 0.359f));
                    equippedNecklace.transform.localScale = new Vector3(0.00082f, 0.00067f, 0.00088f);
                }
                else if(optionId == 12)
                {
                    equippedNecklace = Instantiate(itemPrefab, neckBone); // 몸 본에 목걸이 장착
                    equippedNecklace.transform.localPosition = new Vector3(0.0f, 0.00024f, 0.00059f);
                    equippedNecklace.transform.localRotation = Quaternion.Euler(new Vector3(13.839f, -358.167f, -0.379f));
                    equippedNecklace.transform.localScale = new Vector3(0.00079f, 0.00045f, 0.00059f);
                }

                equippedNecklace.layer = 6;  // 원하는 레이어 번호로 설정
                // 하위 오브젝트들도 동일한 레이어로 설정
                foreach (Transform child in equippedNecklace.transform)
                {
                    child.gameObject.layer = 6;
                }
                break;

            case 3: // 안경
                RemoveExistingGlasses();
                if (equippedGlasses != null) Destroy(equippedGlasses);
                equippedGlasses = Instantiate(itemPrefab, headBone);
                equippedGlasses.transform.localPosition = new Vector3(0.00032f, 0.00018f, 0.00102f);
                equippedGlasses.transform.localRotation = Quaternion.Euler(new Vector3(-0.404f, 91.894f, 0.013f)); // 안경의 로테이션 설정
                equippedGlasses.transform.localScale = new Vector3(0.000258f, 0.000258f, 0.000258f); // 안경의 스케일 설정
                equippedGlasses.layer = 6;  // 원하는 레이어 번호로 설정
                // 하위 오브젝트들도 동일한 레이어로 설정
                foreach (Transform child in equippedGlasses.transform)
                {
                    child.gameObject.layer = 6;
                }
                break;

            case 4: // 가방
                RemoveExistingBag(); // 기존 가방 삭제
                
                if(optionId == 9) 
                {
                    equippedBag = Instantiate(itemPrefab, handBone); // 손 본에 가방 장착
                    equippedBag.transform.localPosition = new Vector3(-0.00087f, 0.00203f, -0.00121f);
                    equippedBag.transform.localRotation = Quaternion.Euler(new Vector3(-1.488f, 118.743f, 134.081f)); // z축을 90도 회전
                    equippedBag.transform.localScale = new Vector3(0.000964f, 0.000418f, 0.000165f); // 주신 로컬 스케일 값 적용
                }
                else if(optionId == 10)
                {
                    equippedBag = Instantiate(itemPrefab, handBone); // 손 본에 가방 장착
                    Debug.Log("FBX 아이템 옵션: " + itemOptionId);
                    equippedBag.transform.localPosition = new Vector3(-0.00099f, 0.00248f, -0.00153f);
                    equippedBag.transform.localRotation = Quaternion.Euler(new Vector3(-45.24f, 377.64f, 188.43f)); // z축을 90도 회전
                    equippedBag.transform.localScale = new Vector3(0.00145f, 0.000823f, 0.000844f); // 주신 로컬 스케일 값 적용
                }
                equippedBag.layer = 6;  // 원하는 레이어 번호로 설정
                // 하위 오브젝트들도 동일한 레이어로 설정
                foreach (Transform child in equippedBag.transform)
                {
                    child.gameObject.layer = 6;
                }
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
                Destroy(child.gameObject); // "necklace"가 포함된 오브젝트를 삭제
                Debug.Log("기존 목걸이를 삭제했습니다.");
            }
        }
    }

    void RemoveExistingGlasses()
    {
        foreach (Transform child in headBone)
        {
            if (child.name.ToLower().Contains("glasses"))
            {
                Destroy(child.gameObject); // "glasses"가 포함된 오브젝트를 삭제
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
                Destroy(child.gameObject); // "hat"이 포함된 오브젝트를 삭제
                Debug.Log("기존 모자를 삭제했습니다.");
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

    // 서버에서 캐릭터 아이템 정보를 가져오는 메서드
    public static void GetCharacterItem(MonoBehaviour caller, System.Action<List<CharacterItemResponseDTO>> callback)
    {
        caller.StartCoroutine(GetCharacterItemCoroutine(callback));
    }

    public static IEnumerator GetCharacterItemCoroutine(System.Action<List<CharacterItemResponseDTO>> callback)
    {
        if (getCharacterItemApiUrl == null)
        {
            string basicApiUrl = ServerConfig.hostUrl;
            getCharacterItemApiUrl = basicApiUrl + "/characters/item";
        }

        using (UnityWebRequest request = UnityWebRequest.Get(getCharacterItemApiUrl))
        {
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("아이템 가져오기 에러 : " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;

                // JSON 응답을 리스트 형태로 파싱
                List<CharacterItemResponseDTO> items = JsonConvert.DeserializeObject<List<CharacterItemResponseDTO>>(jsonResponse);

                // 콜백 함수 호출
                callback?.Invoke(items);
            }
        }
    }

    // 공통 헤더 설정 메서드
    private static UnityWebRequest SetHeaders(UnityWebRequest request)
    {
        authToken = PlayerPrefs.GetString("authToken", null);
        refreshToken = PlayerPrefs.GetString("refreshToken", null);

        if (!string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(refreshToken))
        {
            request.SetRequestHeader("auth", authToken);
            request.SetRequestHeader("refresh", refreshToken);
        }
        else
        {
            request.SetRequestHeader("auth", "");
            request.SetRequestHeader("refresh", "");
        }
        return request;
    }
}
