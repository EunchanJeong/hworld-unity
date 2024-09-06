using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml.Serialization; // Newtonsoft.Json 사용

public class ItemManager : MonoBehaviour
{
    // API 엔드포인트 URL
    private string ShopListapiUrl = "http://localhost:8080/shop";
    private string ShopItemListapiUrl = "http://localhost:8080/shop/item";

    // 상점 데이터를 담을 클래스
    public class Shop
    {
        public int shopId; // 상점ID
        public string shopName; // 상점 이름
        public string shopImageUrl; // 상점 이미지 URL
    }
    // 아이템 데이터를 담을 클래스
    public class Item

    {
        public string itemName { get; set; } // 아이템 이름
        public string itemImageUrl { get; set; } // 아이템 이미지 URL
        public int itemOptionId { get; set; }
        public string itemOption { get; set; }
        public int itemPrice { get; set; }
    }

    // 상점 리스트를 담을 변수
    public List<Shop> shops = new List<Shop>();


    // 아이템 리스트를 담을 변수
    public List<Item> items = new List<Item>();

      // UI 요소들
    public Dropdown DropdownShop; // 상점 선택 드롭다운
    public Button ButtonHat, ButtonNecklace, ButtonGlasses, ButtonBag; // 카테고리 버튼들

    // UI 요소들
    public GameObject shopPrefab; // 상점을 나타낼 UI 프리팹
    public Transform itemParent; // 아이템을 나열할 부모 패널 (LeftPanel)
    public GameObject itemPrefab; // 아이템을 나타낼 UI 프리팹 (버튼이나 이미지)

    private int selectedShopId; // 선택된 상점 ID
    private int selectedCategoryId = 4; // 기본 카테고리: 가방

    // 카테고리 ID 설정
    private readonly int hatCategoryId = 1;
    private readonly int necklaceCategoryId = 2;
    private readonly int glassesCategoryId = 3;
    private readonly int bagCategoryId = 4;

    // 게임이 시작될 때 호출됨
    void Start()
    {
        // 상점 진입 시 API에서 상점 데이터를 가져온다.
        GetShopsFromAPI();

        // 드롭다운에서 상점 선택시 이벤트 설정
        DropdownShop.onValueChanged.AddListener(OnShopSelected);
    }

    public void GetShopsFromAPI()
    {
        // 상점 데이터를 API로부터 가져오는 함수
        StartCoroutine(GetShops());
    }
    // 아이템 데이터를 API로부터 가져오는 함수
    public void GetItemsFromAPI(int shopId, int categoryId)
    {
        StartCoroutine(GetItems(shopId, categoryId));
    }

    IEnumerator GetShops()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(ShopListapiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                // JSON 데이터를 받아서 파싱 (Newtonsoft.Json 사용)
                string jsonResponse = request.downloadHandler.text;

                // Newtonsoft.Json을 사용하여 JSON을 아이템 리스트로 변환
                shops = JsonConvert.DeserializeObject<List<Shop>>(jsonResponse);

                 // 상점 이름을 드롭다운에 추가
                DropdownShop.ClearOptions(); // 기존 옵션 제거
                List<Dropdown.OptionData> shopOptions = new List<Dropdown.OptionData>();
                
                foreach (var shop in shops)
                {
                    Dropdown.OptionData optionData = new Dropdown.OptionData(shop.shopName); // 각각의 상점 이름으로 옵션을 생성
                    shopOptions.Add(optionData); // OptionData 리스트에 추가
                }

                DropdownShop.AddOptions(shopOptions); // OptionData 리스트를 드롭다운에 추가

                // 첫 번째 상점을 기본 선택하고 Label 업데이트
                DropdownShop.value = 0; // 첫 번째 옵션 선택
                OnShopSelected(0); // 첫 번째 상점에 대한 데이터 로드 및 Label 업데이트
            }
        }
    }
    // 아이템 API 요청 코루틴
    IEnumerator GetItems(int shopId, int categoryId)
    {   
        string apiUrl = $"{ShopItemListapiUrl}?shopId={shopId}&categoryId={categoryId}";
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                // JSON 데이터를 받아서 파싱 (Newtonsoft.Json 사용)
                string jsonResponse = request.downloadHandler.text;

                // Newtonsoft.Json을 사용하여 JSON을 아이템 리스트로 변환
                items = JsonConvert.DeserializeObject<List<Item>>(jsonResponse);

                // 가져온 데이터를 바탕으로 UI 생성
                CreateItemUI();
            }
        }
    }

    void CreateShopUI()
    {
        SetShopData(shopPrefab, shops[0]);
    }
    // 받아온 데이터를 바탕으로 UI에 아이템을 나열
    void CreateItemUI()
    {
        // 1. 먼저 기존에 있는 ItemPrefab 처리
        if (itemParent.childCount > 0) // 아이템이 이미 배치되어 있는 경우
        {
            // 첫 번째 아이템을 기존에 존재하는 프리팹에 적용
            Transform firstItemTransform = itemParent.GetChild(0); // 첫 번째 자식
            SetItemData(firstItemTransform.gameObject, items[0]);
        }

        // 2. 나머지 아이템들은 새로 생성하여 처리
        for (int i = 1; i < items.Count; i++)
        {
            // 나머지 아이템 프리팹 생성
            GameObject newItem = Instantiate(itemPrefab, itemParent);

            // 새로 생성된 아이템에 데이터 설정
            SetItemData(newItem, items[i]);
        }
    }

    // 상점 데이터 설정을 위한 함수
    void SetShopData(GameObject shopObject, Shop shop)
    {
        StartCoroutine(LoadShopImage(shop.shopImageUrl, shopObject));
    }

    // 아이템 데이터 설정을 위한 함수
    void SetItemData(GameObject itemObject, Item item)
    {
        // 아이템 이름 설정
        Transform itemNameTextTransform = itemObject.transform.Find("ItemNameText");
        if (itemNameTextTransform == null)
        {
            Debug.LogError("ItemNameText 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Text itemNameText = itemNameTextTransform.GetComponent<Text>();
        if (itemNameText == null)
        {
            Debug.LogError("ItemNameText 오브젝트에 Text 컴포넌트가 없습니다.");
            return;
        }

        itemNameText.text = item.itemName;

        // 이미지 URL을 사용해 아이템 이미지 로드
        StartCoroutine(LoadItemImage(item.itemImageUrl, itemObject));

        // 아이템 가격 설정
        Transform itemPriceTextTransform = itemObject.transform.Find("ItemPriceText");
        if (itemPriceTextTransform == null)
        {
            Debug.LogError("ItemPriceText 오브젝트를 찾을 수 없습니다.");
            return;
        }

        Text itemPriceText = itemPriceTextTransform.GetComponent<Text>();
        if (itemPriceText == null)
        {
            Debug.LogError("itemPriceText 오브젝트에 Text 컴포넌트가 없습니다.");
            return;
        }

        itemPriceText.text = item.itemPrice.ToString() + " 원";
    }


    // 이미지 URL로부터 아이템 이미지를 불러오는 코루틴
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
                // 텍스처를 가져와서 아이템 이미지로 설정
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                RawImage itemImage = itemObject.transform.Find("ItemImage").GetComponent<RawImage>();

                if (itemImage != null)
                {
                    itemImage.texture = texture;
                }
                else
                {
                    Debug.LogError("ItemImage 오브젝트를 찾을 수 없거나, RawImage 컴포넌트가 없습니다.");
                }
            }
        }
    }

    // 이미지 URL로부터 상점 이미지를 불러오는 코루틴
    IEnumerator LoadShopImage(string imageUrl, GameObject shopObject)
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
                // 텍스처를 가져와서 아이템 이미지로 설정
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                RawImage shopImage = shopObject.transform.Find("ShopImage").GetComponent<RawImage>();

                if (shopImage != null)
                {
                    shopImage.texture = texture;
                }
                else
                {
                    Debug.LogError("shopImage 오브젝트를 찾을 수 없거나, RawImage 컴포넌트가 없습니다.");
                }
            }
        }
    }

    // 드롭다운에서 상점 선택 시 호출되는 함수
    public void OnShopSelected(int index)
    {
        selectedShopId = shops[index].shopId;
        LoadShopImage(shops[index].shopImageUrl, shopPrefab); // 선택된 상점의 이미지 로드
        GetItemsFromAPI(selectedShopId, selectedCategoryId); // 현재 선택된 카테고리로 다시 불러옴
    }
}