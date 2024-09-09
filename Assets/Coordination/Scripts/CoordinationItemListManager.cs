using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;
using System.Text;
using UnityEngine.SceneManagement;
using System.Globalization;

namespace Coordination {
    public class CoordinationItemListResponseDTO
    {
        public int categoryId {get; set;}
        public int itemId {get; set;}
        public int itemOptionId {get; set;}
        public string imageUrl {get; set;}
        public string name {get; set;}
        public int price {get; set;}
        public Boolean inCart {get; set;}
    }

    [System.Serializable]
    public class CartItemOptionRequestDTO
    {
        public int itemOptionId;

        public CartItemOptionRequestDTO(int itemOptionId)
        {
            this.itemOptionId = itemOptionId;
        }
    }

    [System.Serializable]
    public class CommonResponseDTO
    {
        public bool success;
        public string message;
    }

    public class CoordinationItemListManager : MonoBehaviour
    {
        public CoordinationListManager coordinationListManager;

        public GameObject item;
        public Transform contentParent;


        public GameObject deletePopup;
        public GameObject addPopup;
        public GameObject deletePopup2;
        public Transform popupParent;

        public Color grayColor;  // 회색

        private string basicApiUrl = "http://localhost:8080/coordinations/"; 
        
        void Start()
        {
            // #2F3744 색상을 defaultButtonColor에 설정
            ColorUtility.TryParseHtmlString("#AAAAAA", out grayColor);

            coordinationListManager = FindObjectOfType<CoordinationListManager>();
            coordinationListManager.OnCoordinationIdChanged += HandleCoordinationIdChanged;
        }

        private void HandleCoordinationIdChanged(int newCoordinationId)
        {
            Debug.Log("Received Coordination ID: " + newCoordinationId);
            StartCoroutine(GetItemsForCoordination(newCoordinationId));

            // DeleteCoordinationButton 이벤트 설정
            Button deleteCoordinationButton = GameObject.Find("DeleteCoordinationButton").GetComponent<Button>();
            deleteCoordinationButton.onClick.RemoveAllListeners();

            // coordinationId가 있을 때만 버튼에 이벤트 추가
            deleteCoordinationButton.onClick.AddListener(() => { StartCoroutine(OnCoordinationDeleteButtonClick(newCoordinationId)); });
        }

        private IEnumerator GetItemsForCoordination(int coordinationId)
        {
            ClearPreviousItems(); // 이전 아이템 제거

            string apiUrl = $"{basicApiUrl}{coordinationId}";   

            using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
            {
                yield return request.SendWebRequest(); // 요청 보내고 대기

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log("착용 제품이 없습니다.");
                }
                else
                {
                    // 서버로부터 받은 JSON 데이터를 파싱
                    string jsonResponse = request.downloadHandler.text;
                    List<CoordinationItemListResponseDTO> coordinationItemList = JsonConvert.DeserializeObject<List<CoordinationItemListResponseDTO>>(jsonResponse);

                    // 받아온 데이터 처리
                    foreach (var coordinationItem in coordinationItemList)
                    {
                        Debug.Log($"Item ID: {coordinationItem.itemId}");

                        // CoordinationTitle 프리팹을 Content 안에 생성
                        GameObject newCoordinationItem = Instantiate(item, contentParent);

                        // CoordinationTitle의 자식 오브젝트에서 Text 컴포넌트를 찾아서 이름 설정
                        Text itemTitle = newCoordinationItem.transform.Find("ItemName").GetComponent<Text>();
                        itemTitle.text = coordinationItem.name;

                        // CoordinationTitle의 자식 오브젝트에서 Text 컴포넌트를 찾아서 가격 설정
                        Text itemPrice = newCoordinationItem.transform.Find("ItemPrice").GetComponent<Text>();
                        string formattedPrice = coordinationItem.price.ToString("N0", CultureInfo.InvariantCulture);
                        itemPrice.text = formattedPrice + "원";

                        // CoordinationItem의 자식 오브젝트에서 Image 컴포넌트를 찾아서 이미지 설정
                        Image itemImage = newCoordinationItem.transform.Find("ItemImage").GetComponent<Image>();

                        if (itemImage != null)
                        {
                            yield return StartCoroutine(LoadImageFromUrl(coordinationItem.imageUrl, itemImage));
                        }

                        // 장바구니 버튼 가져오기
                        Button cartButton = newCoordinationItem.transform.Find("CartButton").GetComponent<Button>();

                        if (cartButton != null) {
                            var cartButtonImage = cartButton.GetComponent<Image>();
                            Text cartButtonText = cartButton.transform.Find("CartButtonText").GetComponent<Text>();

                            if (coordinationItem.inCart) 
                            {
                                // 이미 장바구니에 있으면
                                cartButtonImage.color = grayColor;
                                cartButtonText.text = "장바구니 삭제";

                                cartButton.onClick.AddListener(() => StartCoroutine(OnCartDeleteButtonClick(coordinationItem.itemOptionId)));
                            } 
                            else
                            {
                                // 장바구니에 없으면
                                cartButton.onClick.AddListener(() => StartCoroutine(OnCartAddButtonClick(coordinationItem.itemOptionId)));
                            }
                        }
                    }
                }
            }
        }

        // 코디 삭제 버튼 클릭 시
        private IEnumerator OnCoordinationDeleteButtonClick(int coordinationId)
        {
            GameObject popup = Instantiate(deletePopup2, popupParent);

            Button yesButton = popup.transform.Find("YesButton").GetComponent<Button>();
            Button noButton = popup.transform.Find("NoButton").GetComponent<Button>();

            // YesButton 클릭 시 삭제 요청 보내기
            yesButton.onClick.AddListener(() => StartCoroutine(RemoveCoordination(coordinationId)));

            // NoButton 클릭 시 팝업 닫기
            noButton.onClick.AddListener(() => Destroy(popup));

            yield break;
        }

        private IEnumerator RemoveCoordination(int coordinationId) 
        {
            string url = $"{basicApiUrl}{coordinationId}";

            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                Debug.Log("삭제 요청 받음 -> " + coordinationId);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("삭제 완료");
                    
                    // 현재 씬의 이름을 가져와서 다시 로드
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else
                {
                    Debug.Log("삭제 실패");
                }
            }
        }

        private IEnumerator LoadImageFromUrl(string url, Image imageComponent)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("이미지 로드 오류: " + www.error);
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    Sprite sprite = SpriteFromTexture(texture);

                    imageComponent.sprite = sprite; 
                }
            }
        }

        private Sprite SpriteFromTexture(Texture2D texture)
        {
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
        }

        private void ClearPreviousItems()
        {
            // 이전에 생성된 아이템들을 모두 삭제
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
        }

        // 장바구니 삭제 버튼 클릭 시
        private IEnumerator OnCartDeleteButtonClick(int itemOptionId)
        {
            GameObject popup = Instantiate(deletePopup, popupParent);

            Button yesButton = popup.transform.Find("YesButton").GetComponent<Button>();
            Button noButton = popup.transform.Find("NoButton").GetComponent<Button>();

            // YesButton 클릭 시 삭제 요청 보내기
            yesButton.onClick.AddListener(() => StartCoroutine(RemoveCartItem(itemOptionId)));

            // NoButton 클릭 시 팝업 닫기
            noButton.onClick.AddListener(() => Destroy(popup));

            yield break;
        }

        private IEnumerator RemoveCartItem(int itemOptionId) 
        {
            string url = $"http://localhost:8080/coordinations/cart/{itemOptionId}";

            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                Debug.Log("삭제 요청 받음 -> " + itemOptionId);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("삭제 완료");
                    
                    // 현재 씬의 이름을 가져와서 다시 로드
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else
                {
                    Debug.Log("삭제 실패");
                }
            }
        }

        // 장바구니 추가 버튼 클릭 시
        private IEnumerator OnCartAddButtonClick(int itemOptionId)
        {
            GameObject popup = Instantiate(addPopup, popupParent);

            Button yesButton = popup.transform.Find("YesButton").GetComponent<Button>();
            Button noButton = popup.transform.Find("NoButton").GetComponent<Button>();

            // YesButton 클릭 시 삭제 요청 보내기
            yesButton.onClick.AddListener(() => StartCoroutine(AddCartItem(itemOptionId)));

            // NoButton 클릭 시 팝업 닫기
            noButton.onClick.AddListener(() => Destroy(popup));

            yield break;
        }

        private IEnumerator AddCartItem(int itemOptionId) 
        {
            string url = "http://localhost:8080/carts";

            // JSON 데이터 직렬화
            CartItemOptionRequestDTO requestData = new CartItemOptionRequestDTO(itemOptionId);
            Debug.Log("requestData -> " + requestData.itemOptionId);

            string jsonData = JsonUtility.ToJson(requestData);
            Debug.Log("jsonData -> " + jsonData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                Debug.Log("추가 요청 받음 -> " + requestData.itemOptionId);

                // JSON 데이터 바디로 추가
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("추가 완료");
                    
                    // 현재 씬의 이름을 가져와서 다시 로드
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                else
                {
                    Debug.Log("추가 실패");
                }
            }
        }
    }
}

