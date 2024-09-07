using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShopManager : MonoBehaviour
{
    // 상점과 아이템을 가져올 API 엔드포인트
    private string ShopListapiUrl = "http://localhost:8080/shop";
    private string ShopItemListapiUrl = "http://localhost:8080/shop/item";
    private string CartApiUrl = "http://localhost:8080/carts"; // 카트 API URL

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

    // UI 요소들
    public Dropdown DropdownShop;
    public Dropdown DropdownItemOption; // 아이템 옵션 선택 드롭다운
    public Button ButtonHat, ButtonNecklace, ButtonGlasses, ButtonBag;
    public Button ButtonCart; // 카트 버튼
    public GameObject shopPrefab;
    public Transform itemParent;
    public GameObject itemPrefab;
    public GameObject noItemsTextPrefab;

    private int selectedShopId;
    private int selectedCategoryId = 4; // 기본 카테고리: 가방
    private int selectedItemOptionId; // 선택된 아이템 옵션 ID 저장
    private List<ItemOption> currentOptions; // 현재 아이템의 옵션 리스트

       // 선택된 버튼의 색상
    public Color selectedButtonColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // 진한 회색
    public Color defaultButtonColor = new Color(1f, 1f, 1f, 1.0f); // 기본 흰색

    // 카테고리 ID
    private readonly int hatCategoryId = 1;
    private readonly int necklaceCategoryId = 2;
    private readonly int glassesCategoryId = 3;
    private readonly int bagCategoryId = 4;

    // 게임이 시작될 때 실행되는 함수
    void Start()
    {
        // API에서 상점 및 모든 아이템 데이터를 초기 로드
        GetShopsAndItemsFromAPI();

        // 드롭다운 및 카테고리 버튼 클릭 시 이벤트 설정
        DropdownShop.onValueChanged.AddListener(OnShopSelected);
        ButtonHat.onClick.AddListener(() => OnCategorySelected(hatCategoryId, ButtonHat));
        ButtonNecklace.onClick.AddListener(() => OnCategorySelected(necklaceCategoryId, ButtonNecklace));
        ButtonGlasses.onClick.AddListener(() => OnCategorySelected(glassesCategoryId, ButtonGlasses));
        ButtonBag.onClick.AddListener(() => OnCategorySelected(bagCategoryId, ButtonBag));

        // 카트 버튼 클릭 시 이벤트 설정
        ButtonCart.onClick.AddListener(OnCartButtonClicked);

        // 드롭다운에서 선택 변경 시 이벤트 설정
        DropdownItemOption.onValueChanged.AddListener(OnOptionSelected);
    }

    // 상점 및 상점별 카테고리별 아이템을 모두 API로부터 가져오는 함수
    public void GetShopsAndItemsFromAPI()
    {
        StartCoroutine(GetShopsAndItems());
    }

    // API로부터 상점 및 아이템을 비동기적으로 가져오는 코루틴
    IEnumerator GetShopsAndItems()
    {
        using (UnityWebRequest shopRequest = UnityWebRequest.Get(ShopListapiUrl))
        {
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

    // 상점 UI 생성
    void CreateShopUI()
    {
        DropdownShop.ClearOptions();
        List<Dropdown.OptionData> shopOptions = new List<Dropdown.OptionData>();

        foreach (var shop in shops)
        {
            Dropdown.OptionData optionData = new Dropdown.OptionData(shop.shopName);
            shopOptions.Add(optionData);
        }

        DropdownShop.AddOptions(shopOptions);
        SetShopData(shopPrefab, shops[0]);// 첫 번째 상점 이미지 로드
    }

    // 상점 선택 시 호출되는 함수
    public void OnShopSelected(int index)
    {
        selectedShopId = shops[index].shopId;
        SetShopData(shopPrefab, shops[index]); // 상점 이미지 로드
         // 상점을 변경해도 마지막으로 선택된 카테고리 유지
        switch (selectedCategoryId)
        {
            case 1:
                OnCategorySelected(hatCategoryId, ButtonHat);
                break;
            case 2:
                OnCategorySelected(necklaceCategoryId, ButtonNecklace);
                break;
            case 3:
                OnCategorySelected(glassesCategoryId, ButtonGlasses);
                break;
            case 4:
                OnCategorySelected(bagCategoryId, ButtonBag);
                break;
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
        else
        {
            CreateItemUI(new List<Item>()); // 아이템이 없으면 빈 리스트 처리
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
        itemPriceText.text = $"{item.itemPrice} 원";

        // 아이템을 클릭하면 해당 아이템의 옵션을 드롭다운에 추가
        Button itemButton = itemObject.GetComponent<Button>();
        itemButton.onClick.AddListener(() => OnItemSelected(item)); // 아이템 선택 시 함수 호출

        StartCoroutine(LoadItemImage(item.itemImageUrl, itemObject));
    }

    // 아이템 선택 시 호출되는 함수 (아이템 옵션을 드롭다운에 추가)
    public void OnItemSelected(Item selectedItem)
    {
        // 기존 드롭다운 옵션 초기화
        DropdownItemOption.ClearOptions();

        // 아이템의 옵션 데이터를 가져와 드롭다운에 추가
        currentOptions = selectedItem.itemOptions; // 현재 아이템의 옵션 리스트 저장
        List<Dropdown.OptionData> optionDataList = new List<Dropdown.OptionData>();

        // 옵션 리스트를 순회하며 드롭다운에 추가
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
    }

    // 드롭다운에서 옵션이 선택될 때 호출되는 함수
    public void OnOptionSelected(int index)
    {
        // 옵션 리스트가 존재하고, 유효한 인덱스일 때 선택된 옵션 ID를 저장
        if (currentOptions != null && index < currentOptions.Count)
        {
            selectedItemOptionId = currentOptions[index].itemOptionId; // 선택된 옵션 ID 저장
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
