using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using System.Globalization;
using dotenv.net;
using System;

public class ShopManager : MonoBehaviour
{
    // 상점과 아이템을 가져올 API 엔드포인트
    private string ShopListapiUrl;
    private string ShopItemListapiUrl;
    private string CartApiUrl; // 카트 API 
    
    private string EquipItemUrl; // 아이템 장착 API
    private string UnequipItemUrl; // 아이템 장착 해제 API

    // 상점 정보를 담는 클래스
    public class Shop
    {
        public int shopId; // 상점 ID
        public string shopName; // 상점 이름
        public string shopImageUrl; // 상점 이미지 URL
    }

    // 아이템 정보를 담는 클래스
    public class Item
    {
        public string itemName { get; set; }
        public string itemImageUrl { get; set; }
        public int itemPrice { get; set; }
        public List<ItemOption> itemOptions { get; set; } // 아이템 옵션 리스트
    }

    public class ItemOption
    {
        public int itemOptionId { get; set; }
        public string itemOption { get; set; }
    }

    // 상점 리스트와 상점별 카테고리별 아이템 데이터 저장소
    public List<Shop> shops = new List<Shop>();
    public Dictionary<int, Dictionary<int, List<Item>>> shopItems = new Dictionary<int, Dictionary<int, List<Item>>>(); // 상점 ID별, 카테고리 ID별 아이템 저장

    public Dictionary<int, Item> equippedItems = new Dictionary<int, Item>(); // 장착 아이템 저장

    // UI 요소들
    public Dropdown DropdownShop;
    public Dropdown DropdownItemOption; // 아이템 옵션 선택 드롭다운
    public Button ButtonHat, ButtonNecklace, ButtonGlasses, ButtonBag;
    public Button ButtonCart; // 카트 버튼
    public GameObject shopPrefab;
    public Transform itemParent;
    public GameObject itemPrefab;
    public GameObject noItemsTextPrefab;
    private GameObject selectedItemObject = null; // 선택된 아이템을 저장하는 변수
    public ScrollRect itemScrollRect; // ScrollRect 컴포넌트를 참조하는 변수

    private int selectedShopId;
    private int selectedCategoryId = 4; // 기본 카테고리: 가방
    private int selectedItemOptionId; // 선택된 아이템 옵션 ID 저장
    private List<ItemOption> currentOptions; // 현재 아이템의 옵션 리스트

    // 선택된 버튼의 색상
    public Color selectedButtonColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // 진한 회색
    public Color defaultButtonColor = new Color(1f, 1f, 1f, 1.0f); // 기본 흰색
    public Color BolderColor = new Color(1f, 0.549f, 0f, 1f);

    // 카테고리 ID
    private readonly int hatCategoryId = 1;
    private readonly int necklaceCategoryId = 2;
    private readonly int glassesCategoryId = 3;
    private readonly int bagCategoryId = 4;


    // 캐릭터 3D 모델을 참조할 변수
    public GameObject characterModel; // 캐릭터 3D 모델
    public Transform headBone; // 모자나 안경을 장착할 위치 (머리)
    public Transform handBone; // 가방을 장착할 위치 (몸)
    public Transform neckBone; // 목걸이를 장착할 위치

    // 아이템 프리팹을 관리할 Dictionary
    public Dictionary<string, GameObject> itemPrefabs = new Dictionary<string, GameObject>(); // 아이템 프리팹들을 저장하는 딕셔너리

    private GameObject equippedHat;     // 장착된 모자
    private GameObject equippedNecklace; // 장착된 목걸이
    private GameObject equippedGlasses; // 장착된 안경
    private GameObject equippedBag;     // 장착된 가방

     // FBX 파일이 저장된 경로 (Assets/Shop/Items)
    // private string fbxPath = "Assets/Shop/Items/";
    private string fbxPath = "Items/";

    

    private GameObject characterInstance; // 캐릭터 인스턴스

    private static string authToken;
    private static string refreshToken;

    // 게임이 시작될 때 실행되는 함수
    void Start()
    {
        // // .env 파일 로드
        // DotEnv.Load();
        
        // // 환경 변수 불러오기
        // string basicApiUrl = Environment.GetEnvironmentVariable("UNITY_APP_API_URL");
        string basicApiUrl = ServerConfig.hostUrl;
        ShopListapiUrl = basicApiUrl + "/shop";
        ShopItemListapiUrl = basicApiUrl + "/shop/item";
        CartApiUrl = basicApiUrl + "/carts";
        EquipItemUrl = basicApiUrl + "/characters/item";
        UnequipItemUrl = basicApiUrl + "/characters/item/";

        // API에서 상점 및 모든 아이템 데이터를 초기 로드
        GetShopsAndItemsFromAPI();
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        characterInstance = ChangeShopSceneWithCharacter.Instance.characterInstance;
        Debug.Log("전달받은 characterInstance -> " + characterInstance);
        if (characterInstance != null)
        {
            Debug.Log("캐릭터 인스턴스를 찾았습니다.");
            
            // 캐릭터를 2D 환경에 맞게 설정
            SetupCharacterFor2D(characterInstance);

            GameObject canvasObject = GameObject.Find("Canvas");
            characterInstance.transform.SetParent(canvasObject.transform);
            characterInstance.transform.localPosition = new Vector3(399, -331, -84);
            characterInstance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            characterInstance.transform.localScale = new Vector3(380, 380, 380);

            // handBone을 characterInstance 내 hand_l 본으로 설정
            handBone = FindBone(characterInstance.transform, "hand_l");
            if (handBone == null)
            {
                Debug.LogError("hand_l 본을 찾을 수 없습니다.");
            }

            neckBone = FindBone(characterInstance.transform, "head");
            if (neckBone == null)
            {
                Debug.LogError("head 본을 찾을 수 없습니다.");
            }

            headBone = FindBone(characterInstance.transform, "head");
            if (headBone == null)
            {
                Debug.LogError("head 본을 찾을 수 없습니다.");
            }
        }

        // 드롭다운에서 선택 변경 시 이벤트 설정
        DropdownItemOption.onValueChanged.AddListener(OnOptionSelected);

        // 드롭다운 및 카테고리 버튼 클릭 시 이벤트 설정
        DropdownShop.onValueChanged.AddListener(OnShopSelected);

        ButtonHat.onClick.AddListener(() => OnCategorySelected(hatCategoryId, ButtonHat));
        ButtonNecklace.onClick.AddListener(() => OnCategorySelected(necklaceCategoryId, ButtonNecklace));
        ButtonGlasses.onClick.AddListener(() => OnCategorySelected(glassesCategoryId, ButtonGlasses));
        ButtonBag.onClick.AddListener(() => OnCategorySelected(bagCategoryId, ButtonBag));

        // 카트 버튼 클릭 시 이벤트 설정
        ButtonCart.onClick.AddListener(OnCartButtonClicked);

        // 나가기 버튼에 MainScene으로 이동 리스너 추가
        GameObject iconExit = GameObject.Find("IconExit");
        if (iconExit != null)
        {
            Button exitButton = iconExit.GetComponent<Button>();
            exitButton.onClick.AddListener(() =>
            {
                SaveEquippedItems(); // 장착 아이템 상태 저장
                SceneManager.LoadScene("MainScene"); // MainScene으로 이동
            });
        }
        else
        {
            Debug.LogError("IconExit 버튼을 찾을 수 없습니다.");
        }
    }

    public void Update()
    {
        // Esc 버튼이나 P 클릭 시 나가기
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            SaveEquippedItems();
            SceneManager.LoadScene("MainScene");
        }
    }

    // 공통 헤더 설정 메서드
    private static UnityWebRequest SetHeaders(UnityWebRequest request)
    {
        // PlayerPrefs에서 저장된 토큰 불러오기
        authToken = PlayerPrefs.GetString("authToken", null);
        refreshToken = PlayerPrefs.GetString("refreshToken", null);

        if (!string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(refreshToken))
        {
            request.SetRequestHeader("auth", authToken);
            request.SetRequestHeader("refresh", refreshToken);
        }
        return request;
    }

    // 상점 및 상점별 카테고리별 아이템을 모두 API로부터 가져오는 함수
    public void GetShopsAndItemsFromAPI()
    {
        SaveEquippedItems(); // MainScene으로 이동하기 전 장착 아이템 상태를 저장
        StartCoroutine(GetShopsAndItems());
    }

    // API로부터 상점 및 아이템을 비동기적으로 가져오는 코루틴
    IEnumerator GetShopsAndItems()
    {
        using (UnityWebRequest shopRequest = UnityWebRequest.Get(ShopListapiUrl))
        {
            SetHeaders(shopRequest);

            yield return shopRequest.SendWebRequest();

            if (shopRequest.result == UnityWebRequest.Result.ConnectionError || shopRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(shopRequest.error);
            }
            else
            {
                // 상점 목록을 가져와서 파싱
                string shopResponse = shopRequest.downloadHandler.text;
                shops = JsonConvert.DeserializeObject<List<Shop>>(shopResponse);

                // 각 상점별로 카테고리별 아이템 목록을 불러오기
                foreach (var shop in shops)
                {
                    shopItems[shop.shopId] = new Dictionary<int, List<Item>>(); // 상점 ID에 따른 딕셔너리 초기화

                    // 각 카테고리별로 아이템을 가져와 저장
                    foreach (var categoryId in new int[] { hatCategoryId, necklaceCategoryId, glassesCategoryId, bagCategoryId })
                    {
                        string apiUrl = $"{ShopItemListapiUrl}?shopId={shop.shopId}&categoryId={categoryId}";
                        using (UnityWebRequest itemRequest = UnityWebRequest.Get(apiUrl))
                        {
                            SetHeaders(itemRequest);

                            yield return itemRequest.SendWebRequest();

                            if (itemRequest.result == UnityWebRequest.Result.ConnectionError || itemRequest.result == UnityWebRequest.Result.ProtocolError)
                            {
                                Debug.LogError(itemRequest.error);
                            }
                            else
                            {
                                string itemResponse = itemRequest.downloadHandler.text;
                                List<Item> categoryItems = JsonConvert.DeserializeObject<List<Item>>(itemResponse);
                                shopItems[shop.shopId][categoryId] = categoryItems; // 상점별 카테고리별 아이템 저장
                            }
                        }
                    }
                }

                // 상점 UI와 첫 번째 상점 및 카테고리 아이템 설정
                CreateShopUI();
                OnShopSelected(0); // 첫 번째 상점 선택
                OnCategorySelected(bagCategoryId, ButtonBag); // 기본 카테고리 선택
            }
        }
    }

        // 트랜스폼 내에서 이름이 대소문자와 관계없이 특정 이름을 가진 본을 찾는 함수
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

    // 상점 UI 생성
    void CreateShopUI()
    {
        DropdownShop.ClearOptions();
        List<Dropdown.OptionData> shopOptions = new List<Dropdown.OptionData>();

        foreach (var shop in shops)
        {
            Dropdown.OptionData optionData = new Dropdown.OptionData(shop.shopName);

             // 상점 이미지 로딩 후 옵션에 이미지 설정
            StartCoroutine(LoadShopImageForDropdown(shop.shopImageUrl, optionData)); // 비동기 로드
            shopOptions.Add(optionData);
        }

        DropdownShop.AddOptions(shopOptions);
        SetShopData(shopPrefab, shops[0]);// 첫 번째 상점 이미지 로드
    }

    // 상점 이미지를 로드하고 Dropdown의 OptionData에 설정하는 함수
    IEnumerator LoadShopImageForDropdown(string imageUrl, Dropdown.OptionData optionData)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            SetHeaders(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("상점 이미지 로딩 실패: " + request.error);
            }
            else
            {
                // 로드된 이미지를 텍스처로 변환
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite shopSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                // Dropdown의 옵션 데이터에 이미지 추가
                optionData.image = shopSprite;
            }
        }
    }
    // 상점 선택 시 호출되는 함수
    public void OnShopSelected(int index)
    {
        selectedShopId = shops[index].shopId;
        SetShopData(shopPrefab, shops[index]); // 상점 이미지 로드

        // 버튼 활성화/비활성화 로직
        UpdateCategoryButtons(selectedShopId);

         DropdownItemOption.ClearOptions(); // 상점을 변경하면 옵션 드롭다운을 초기화

         // 선택된 아이템 해제
        if (selectedItemObject != null)
        {
            SetItemBorderColor(selectedItemObject, false); // 선택된 아이템 테두리 해제
            selectedItemObject = null; // 선택 상태 해제
        }

        // 아이템이 있는 첫 번째 카테고리를 찾음
        int firstAvailableCategoryId = FindFirstAvailableCategory(selectedShopId);

        Debug.Log(firstAvailableCategoryId);

        if (firstAvailableCategoryId != -1)
        {
            // 아이템이 있는 첫 번째 카테고리로 이동
            OnCategorySelected(firstAvailableCategoryId, GetCategoryButton(firstAvailableCategoryId));
        }
        else
        {
            // 아이템이 없으면 기본 카테고리로 이동
            OnCategorySelected(bagCategoryId, ButtonBag);
        }

        // 스크롤을 맨 위로 이동시킴
        if (itemScrollRect != null)
        {
            itemScrollRect.normalizedPosition = new Vector2(0, 1); // 스크롤을 맨 위로 설정
        }
    }

    // 캐릭터 조정
    private void SetupCharacterFor2D(GameObject character)
    {
        var rb = character.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 물리 기반 이동 비활성화
        }

        var colliders = character.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false; // 충돌체 비활성화
        }
    }

    // 카테고리에 따라 버튼 활성화/비활성화하는 함수
    void UpdateCategoryButtons(int shopId)
    {
        ButtonHat.interactable = shopItems.ContainsKey(shopId) && shopItems[shopId].ContainsKey(hatCategoryId) && shopItems[shopId][hatCategoryId].Count > 0;
        ButtonNecklace.interactable = shopItems.ContainsKey(shopId) && shopItems[shopId].ContainsKey(necklaceCategoryId) && shopItems[shopId][necklaceCategoryId].Count > 0;
        ButtonGlasses.interactable = shopItems.ContainsKey(shopId) && shopItems[shopId].ContainsKey(glassesCategoryId) && shopItems[shopId][glassesCategoryId].Count > 0;
        ButtonBag.interactable = shopItems.ContainsKey(shopId) && shopItems[shopId].ContainsKey(bagCategoryId) && shopItems[shopId][bagCategoryId].Count > 0;
    }

    // 상점의 카테고리들 중 첫 번째로 아이템이 존재하는 카테고리를 찾는 함수
    int FindFirstAvailableCategory(int shopId)
    {
        int[] categoryIds = { hatCategoryId, necklaceCategoryId, glassesCategoryId, bagCategoryId };

        foreach (int categoryId in categoryIds)
        {
            // 해당 카테고리에 아이템이 있는지 확인
            if (shopItems.ContainsKey(shopId) && shopItems[shopId].ContainsKey(categoryId) && shopItems[shopId][categoryId].Count > 0)
            {
                return categoryId; // 아이템이 있는 첫 번째 카테고리 반환
            }
        }

        return -1; // 아이템이 없는 경우 -1 반환
    }

    // 카테고리 ID에 맞는 UI 버튼을 반환해줌
    Button GetCategoryButton(int categoryId)
    {
        switch (categoryId)
        {
            case 1: return ButtonHat;
            case 2: return ButtonNecklace;
            case 3: return ButtonGlasses;
            case 4: return ButtonBag;
            default: return null;
        }
    }

    // 카테고리 선택 시 호출되는 함수
    void OnCategorySelected(int categoryId, Button selectedButton)
    {
        selectedCategoryId = categoryId;

        // 선택된 카테고리 버튼의 배경색을 진하게 변경
        SetButtonBackgroundColor(selectedButton);

        // 상점별 카테고리별로 저장된 아이템 가져와 UI 갱신
        if (shopItems.ContainsKey(selectedShopId) && shopItems[selectedShopId].ContainsKey(selectedCategoryId))
        {
            List<Item> itemsToShow = shopItems[selectedShopId][selectedCategoryId];
            CreateItemUI(itemsToShow);
        }
    
    }

    // 버튼 배경색 변경 함수
    void SetButtonBackgroundColor(Button selectedButton)
    {
        // 모든 버튼의 배경색을 기본값으로 변경
        ButtonHat.GetComponent<Image>().color = defaultButtonColor;
        ButtonNecklace.GetComponent<Image>().color = defaultButtonColor;
        ButtonGlasses.GetComponent<Image>().color = defaultButtonColor;
        ButtonBag.GetComponent<Image>().color = defaultButtonColor;

        // 선택된 버튼의 배경색을 진하게 변경
        selectedButton.GetComponent<Image>().color = selectedButtonColor;
    }

    // 아이템 목록을 UI에 생성하는 함수
    void CreateItemUI(List<Item> itemsToShow)
    {
        // 기존 아이템 제거
        foreach (Transform child in itemParent)
        {
            Destroy(child.gameObject);
        }

        // 아이템이 없는 경우
        if (itemsToShow.Count == 0)
        {
            if (noItemsTextPrefab != null)
            {
                GameObject noItemsText = Instantiate(noItemsTextPrefab, itemParent);
                noItemsText.GetComponent<Text>().text = "아이템이 없습니다.";
            }
        }
        else
        {
            // 아이템이 있을 때 처리
            foreach (var item in itemsToShow)
            {
                GameObject newItem = Instantiate(itemPrefab, itemParent);
                SetItemData(newItem, item);
            }
        }
    }

    // 아이템 데이터를 UI에 적용하는 함수
    void SetItemData(GameObject itemObject, Item item)
    {
        Text itemNameText = itemObject.transform.Find("ItemNameText").GetComponent<Text>();
        itemNameText.text = item.itemName;

        Text itemPriceText = itemObject.transform.Find("ItemPriceText").GetComponent<Text>();
        string formattedPrice = item.itemPrice.ToString("N0", CultureInfo.InvariantCulture);
        itemPriceText.text = $"{formattedPrice}원";

        // 아이템을 클릭하면 해당 아이템의 옵션을 드롭다운에 추가
        Button itemButton = itemObject.GetComponent<Button>();
        itemButton.onClick.AddListener(() => OnItemSelected(itemObject, item)); // 아이템 선택 시 함수 호출

        StartCoroutine(LoadItemImage(item.itemImageUrl, itemObject));
    }

    // 선택된 아이템 프리팹에 테두리를 설정하고 색상을 변경하는 함수
    void SetItemBorderColor(GameObject itemObject, bool enableBorder, Color borderColor = default)
    {
        // 프리팹의 루트에 Outline 컴포넌트를 찾음
        var outline = itemObject.GetComponent<Outline>();

        if (!enableBorder) // 테두리를 제거할 경우
        {
            if (outline != null) 
            {
                Destroy(outline); // Outline 컴포넌트 제거
            }
        }
        else // 테두리를 추가하거나 색상을 변경할 경우
        {
            if (outline == null) 
            {
                outline = itemObject.AddComponent<Outline>(); // Outline 컴포넌트가 없으면 추가
            }

            outline.effectColor = borderColor; // 테두리 색상 설정
            outline.effectDistance = new Vector2(5, 5); // 테두리 두께 설정
        }
    }

    // 아이템 선택 시 호출되는 함수 (아이템 옵션을 드롭다운에 추가)
    public void OnItemSelected(GameObject clickedItemObject, Item selectedItem)
    {
        // 이미 선택된 아이템을 클릭했을 때 선택 해제
        if (selectedItemObject == clickedItemObject)
        {
            SetItemBorderColor(clickedItemObject, false); // 테두리 제거
            selectedItemObject = null; // 선택 해제
            DropdownItemOption.ClearOptions(); // 옵션 드롭다운 초기화

            // 선택된 카테고리에 해당하는 장착된 아이템을 제거
            StartCoroutine(DeleteSelectedOptionToCharacterItem(selectedCategoryId));
            RemoveEquippedItem(selectedCategoryId); // !!! 아이템 선택 취소 시 장착된 아이템도 해제
            return;
        }

        // 다른 아이템을 클릭했을 때 처리
        // 기존에 선택된 아이템이 있으면 테두리 해제
        if (selectedItemObject != null)
        {
            SetItemBorderColor(selectedItemObject, false); // 이전 아이템 테두리 해제
        }

        // 새로운 아이템 선택
        selectedItemObject = clickedItemObject; // 새로 선택된 아이템 저장
        SetItemBorderColor(selectedItemObject, true, BolderColor); // 선택된 아이템에 주황색 테두리 적용

        // 기존 드롭다운 옵션 초기화
        DropdownItemOption.ClearOptions();

        // 아이템의 옵션 데이터를 가져와 드롭다운에 추가
        currentOptions = selectedItem.itemOptions; // 현재 아이템의 옵션 리스트 저장
        List<Dropdown.OptionData> optionDataList = new List<Dropdown.OptionData>();

        foreach (var option in selectedItem.itemOptions)
        {
            Dropdown.OptionData optionData = new Dropdown.OptionData(option.itemOption); // 옵션 이름으로 드롭다운 데이터 생성
            optionDataList.Add(optionData); // 드롭다운 리스트에 추가
        }

        // 드롭다운에 옵션 데이터 추가
        DropdownItemOption.AddOptions(optionDataList);

        // 첫 번째 옵션을 기본 선택 (옵션이 있으면)
        if (selectedItem.itemOptions.Count > 0)
        {
            selectedItemOptionId = selectedItem.itemOptions[0].itemOptionId; // 첫 번째 옵션 ID 저장
        }

        // 선택된 아이템 옵션에 맞는 FBX 파일을 장착
        string category = DetermineItemCategory(selectedItem);
        EquipItemOnCharacter(selectedItemOptionId); // 카테고리 ID로 장착 처리
    }

      // 선택된 아이템의 카테고리 결정 (모자, 목걸이, 안경, 가방 등)
    string DetermineItemCategory(Item item)
    {
        if (item.itemName.Contains("모자"))
            return "Hat";
        else if (item.itemName.Contains("목걸이"))
            return "Necklace";
        else if (item.itemName.Contains("안경"))
            return "Glasses";
        else if (item.itemName.Contains("가방"))
            return "Bag";
        return null;
    }

// 장착된 아이템을 제거하는 함수
void RemoveEquippedItem(int categoryId) // !!! 장착된 아이템을 제거하는 함수 추가
{
    switch (categoryId)
    {
        case 1: // 모자
            if (equippedHat != null)
            {
                Destroy(equippedHat);
                equippedHat = null;
            }
            break;
        case 2: // 목걸이
            if (equippedNecklace != null)
            {
                Destroy(equippedNecklace);
                equippedNecklace = null;
            }
            break;
        case 3: // 안경
            if (equippedGlasses != null)
            {
                Destroy(equippedGlasses);
                equippedGlasses = null;
            }
            break;
        case 4: // 가방
            if (equippedBag != null)
            {
                Destroy(equippedBag);
                equippedBag = null;
            }
            break;
        default:
            Debug.LogError("잘못된 카테고리 ID입니다.");
            break;
    }
}

    // 트랜스폼 내에서 이름에 특정 단어가 포함된 자식을 찾는 함수 (대소문자 구분 없음)
    Transform FindObjectContainingName(Transform parent, string partialName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name.ToLower().Contains(partialName.ToLower()))
            {
                return child;
            }
        }
        return null;
    }

    // 카테고리 ID에 따른 카테고리 이름을 반환하는 함수
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

    // FBX 파일을 로드하고 캐릭터에 장착하는 함수
    void EquipItemOnCharacter(int itemOptionId)
    {
    
        // 카테고리 이름 결정
        string categoryName = GetCategoryNameById(selectedCategoryId);

        // itemOptionId에 해당하는 FBX 파일을 경로에서 로드 (형식: "categoryName_itemOptionId.fbx")
        // string fbxFileName = $"{categoryName}_{itemOptionId}.fbx";
        // string fbxFilePath = $"{fbxPath}{fbxFileName}";

          // itemOptionId에 해당하는 FBX 파일을 경로에서 로드 (형식: "categoryName_itemOptionId")
        string fbxFileName = $"{categoryName}_{itemOptionId}";
        string fbxFilePath = $"{fbxPath}{fbxFileName}";
        
        GameObject itemPrefab = Resources.Load<GameObject>(fbxFilePath);

        if (itemPrefab == null)
        {
            Debug.LogError($"FBX 파일을 찾을 수 없습니다: {fbxFilePath}");
            return;
        }

        Debug.Log("FBX 가져옴");

        // 선택된 카테고리 ID에 따라 본에 장착
        switch (selectedCategoryId)
        {
            case 1: // 모자

                // handBone에서 "hat"이라는 단어가 포함된 오브젝트 제거
                Transform existingHat = FindObjectContainingName(headBone, "hat");
                if (existingHat != null)
                {
                    Debug.Log("기존 가방 삭제");
                    Destroy(existingHat.gameObject);
                    StartCoroutine(DeleteSelectedOptionToCharacterItem(selectedCategoryId));
                }

                if (equippedHat != null) Destroy(equippedHat);

                StartCoroutine(PostSelectedOptionToCharacterItem(selectedItemOptionId));
                equippedHat = Instantiate(itemPrefab, headBone); // 머리 본에 모자 장착
                equippedHat.transform.localPosition = new Vector3(0.0f, 0.00053f, 0.0000f);
                equippedHat.transform.localRotation = Quaternion.Euler(new Vector3(3.872f, -179.781f, -0.145f)); // z축을 90도 회전
                equippedHat.transform.localScale = new Vector3(0.00324f, 0.00350f, 0.00389f); // 주신 로컬 스케일 값 적용
                break;

            case 2: // 목걸이

                // handBone에서 "glasses"이라는 단어가 포함된 오브젝트 제거
                Transform existingNecklace = FindObjectContainingName(headBone, "necklace");
                if (existingNecklace != null)
                {
                    Debug.Log("기존 가방 삭제");
                    Destroy(existingNecklace.gameObject);
                    StartCoroutine(DeleteSelectedOptionToCharacterItem(selectedCategoryId));
                }

                if (equippedNecklace != null) Destroy(equippedNecklace);

                StartCoroutine(PostSelectedOptionToCharacterItem(selectedItemOptionId));
                equippedNecklace = Instantiate(itemPrefab, handBone); // 몸 본에 목걸이 장착
                equippedNecklace.transform.localPosition = Vector3.zero;
                equippedNecklace.transform.localRotation = Quaternion.identity;
                break;

            case 3: // 안경

                // handBone에서 "glasses"이라는 단어가 포함된 오브젝트 제거
                Transform existingGlasses = FindObjectContainingName(headBone, "glasses");
                if (existingGlasses != null)
                {
                    Debug.Log("기존 가방 삭제");
                    Destroy(existingGlasses.gameObject);
                    StartCoroutine(DeleteSelectedOptionToCharacterItem(selectedCategoryId));
                }

                if (equippedGlasses != null) Destroy(equippedGlasses);
                equippedGlasses = Instantiate(itemPrefab, neckBone); // 머리 본에 안경 장착
                
                StartCoroutine(PostSelectedOptionToCharacterItem(selectedItemOptionId));
                equippedGlasses.transform.localPosition = new Vector3(0.00032f, 0.00018f, 0.00102f);
                equippedGlasses.transform.localRotation = Quaternion.Euler(new Vector3(-0.404f, 91.894f, 0.013f)); // z축을 90도 회전
                equippedGlasses.transform.localScale = new Vector3(0.000258f, 0.000258f, 0.000258f); // 주신 로컬 스케일 값 적용
                break;

            case 4: // 가방
                Debug.Log("가방 장착");

                // handBone에서 "bag"이라는 단어가 포함된 오브젝트 제거
                Transform existingBag = FindObjectContainingName(handBone, "bag");
                if (existingBag != null)
                {
                    Debug.Log("기존 가방 삭제");
                    Destroy(existingBag.gameObject);
                    StartCoroutine(DeleteSelectedOptionToCharacterItem(selectedCategoryId));
                }

                // 새로운 가방 장착
                StartCoroutine(PostSelectedOptionToCharacterItem(selectedItemOptionId));
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

    // 장착한 아이템을 저장하는 함수
    void SaveEquippedItems()
    {
        if (equippedHat != null)
            PlayerPrefs.SetInt("EquippedHat", selectedItemOptionId); // 모자 정보 저장
        
        if (equippedNecklace != null)
            PlayerPrefs.SetInt("EquippedNecklace", selectedItemOptionId); // 목걸이 정보 저장
        
        if (equippedGlasses != null)
            PlayerPrefs.SetInt("EquippedGlasses", selectedItemOptionId); // 안경 정보 저장
        
        if (equippedBag != null)
            PlayerPrefs.SetInt("EquippedBag", selectedItemOptionId); // 가방 정보 저장

        PlayerPrefs.Save(); // PlayerPrefs에 저장
    }

    // 드롭다운에서 옵션이 선택될 때 호출되는 함수
    public void OnOptionSelected(int index)
    {
        // 옵션 리스트가 존재하고, 유효한 인덱스일 때 선택된 옵션 ID를 저장
        if (currentOptions != null && index < currentOptions.Count)
        {
            selectedItemOptionId = currentOptions[index].itemOptionId; // 선택된 옵션 ID 저장

            selectedItemOptionId = currentOptions[index].itemOptionId;  // !!! 수정: itemOptionId 사용
            EquipItemOnCharacter(selectedItemOptionId); // !!! 카테고리 ID로 장착 처리
        }
    }

    // 카트 버튼 클릭 시 호출되는 함수 (POST 요청)
    public void OnCartButtonClicked()
    {
        // 선택된 옵션이 있는지 확인
        if (selectedItemOptionId > 0)
        {
            StartCoroutine(PostSelectedOptionToCart(selectedItemOptionId)); // 선택된 옵션 ID를 POST로 전송
        }
        else
        {
            Debug.LogError("옵션이 선택되지 않았습니다."); // 선택된 옵션이 없을 때 에러 출력
        }
    }

    // 선택된 옵션을 POST로 전송하는 코루틴
    IEnumerator PostSelectedOptionToCharacterItem(int itemOptionId)
    {

        Debug.Log("아이템 장착 POST 호출");
        Dictionary <string, int> postData = new Dictionary<string, int>
        {
            {"itemOptionId", itemOptionId }
        };

        string jsonData = JsonConvert.SerializeObject(postData);

        // 바디에 JSON 데이터를 포함한 POST 요청 생성
        using (UnityWebRequest request = new UnityWebRequest(EquipItemUrl, "POST"))
        {
            SetHeaders(request);

            // 요청에 헤더 설정 (JSON 전송을 위한 Content-Type)
            request.SetRequestHeader("Content-Type", "application/json");

            // JSON 데이터를 바디에 포함
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            yield return request.SendWebRequest();

            // 응답 확인
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("POST 요청 실패: " + request.error);
            }
            else
            {
                Debug.Log("POST 요청 성공: " + request.downloadHandler.text);
            }
        }
    }

    IEnumerator DeleteSelectedOptionToCharacterItem(int categoryId)
    {
        // DELETE 요청을 위한 URL 생성
        string deleteUrl = UnequipItemUrl + categoryId.ToString();
        
        // DELETE 요청 생성
        using (UnityWebRequest request = UnityWebRequest.Delete(deleteUrl))
        {
            SetHeaders(request);

            // 응답을 받기 위한 downloadHandler 설정
            request.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            yield return request.SendWebRequest();

            // 응답 확인
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                // 오류 발생 시
                Debug.LogError("DELETE 요청 실패: " + request.error);
            }
            else
            {
                // 성공 시
                Debug.Log("DELETE 요청 성공: " + request.downloadHandler.text);
            }
        }
    }

     // 선택된 옵션을 POST로 전송하는 코루틴
    IEnumerator PostSelectedOptionToCart(int itemOptionId)
    {
        // 전송할 데이터 생성 (JSON 형식)
        Dictionary<string, int> postData = new Dictionary<string, int>
        {
            { "itemOptionId", itemOptionId }
        };

        // JSON 데이터 직렬화
        string jsonData = JsonConvert.SerializeObject(postData);

        // 바디에 JSON 데이터를 포함한 POST 요청 생성
        using (UnityWebRequest request = new UnityWebRequest(CartApiUrl, "POST"))
        {
            SetHeaders(request);

            // 요청에 헤더 설정 (JSON 전송을 위한 Content-Type)
            request.SetRequestHeader("Content-Type", "application/json");

            // JSON 데이터를 바디에 포함
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            yield return request.SendWebRequest();

            // 응답 확인
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("POST 요청 실패: " + request.error);
            }
            else
            {
                Debug.Log("POST 요청 성공: " + request.downloadHandler.text);
            }
        }
    }

    // 아이템 이미지를 로드하는 코루틴
    IEnumerator LoadItemImage(string imageUrl, GameObject itemObject)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("이미지 로딩 실패: " + request.error);
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                RawImage itemImage = itemObject.transform.Find("ItemImage").GetComponent<RawImage>();
                if (itemImage != null)
                {
                    itemImage.texture = texture;
                }
            }
        }
    }

        // 상점 데이터 설정을 위한 함수
    void SetShopData(GameObject shopObject, Shop shop)
    {
        StartCoroutine(LoadShopImage(shop.shopImageUrl, shopObject));
    }

    // 상점 이미지를 로드하는 코루틴
    IEnumerator LoadShopImage(string imageUrl, GameObject shopObject)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            SetHeaders(request);
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("상점 이미지 로딩 실패: " + request.error);
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                RawImage shopImage = shopObject.transform.Find("ShopImage").GetComponent<RawImage>();
                if (shopImage != null)
                {
                    shopImage.texture = texture;
                }
            }
        }
    }
}