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
    public GameObject shopPrefab;
    public Transform itemParent;
    public GameObject itemPrefab;
    public GameObject noItemsTextPrefab;

    private int selectedShopId;
    private int selectedCategoryId = 4; // 기본 카테고리: 가방

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
        ButtonHat.onClick.AddListener(() => OnCategorySelected(hatCategoryId));
        ButtonNecklace.onClick.AddListener(() => OnCategorySelected(necklaceCategoryId));
        ButtonGlasses.onClick.AddListener(() => OnCategorySelected(glassesCategoryId));
        ButtonBag.onClick.AddListener(() => OnCategorySelected(bagCategoryId));
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
                OnCategorySelected(bagCategoryId); // 기본 카테고리 선택
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
        OnCategorySelected(selectedCategoryId); // 현재 선택된 카테고리로 아이템 갱신
    }

    // 카테고리 선택 시 호출되는 함수
    void OnCategorySelected(int categoryId)
    {
        selectedCategoryId = categoryId;

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
        // 기존 드롭다운 옵션을 초기화
        DropdownItemOption.ClearOptions();

        // 아이템의 옵션 데이터를 가져와 드롭다운에 추가
        List<Dropdown.OptionData> optionDataList = new List<Dropdown.OptionData>();

        // 옵션 리스트를 순회하며 드롭다운에 추가
        foreach (var option in selectedItem.itemOptions)
        {
            Dropdown.OptionData optionData = new Dropdown.OptionData(option.itemOption);
            optionDataList.Add(optionData);
        }

        // 드롭다운에 옵션 데이터 추가
        DropdownItemOption.AddOptions(optionDataList);
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
