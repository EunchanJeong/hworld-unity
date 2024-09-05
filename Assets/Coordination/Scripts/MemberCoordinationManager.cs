using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

public class CoordinationListResponseDTO
{
    public int coordinationId {get; set;}
    public string title {get; set;}
    public string imageUrl {get; set;}
}

public class MemberCoordinationManager : MonoBehaviour
{
    public GameObject coordinationTitle;
    public Transform contentParent;

    private string apiUrl = "http://localhost:8080/members/my-coordinations"; 

    private void Start()
    {
        // 코디 목록을 가져오는 메서드를 호출
        GetMemberCoordination();
    }


    // 코디 목록을 가져오는 메서드
    public void GetMemberCoordination()
    {
        StartCoroutine(GetMemberCoordinationCoroutine());
    }

    // 서버에서 데이터를 가져오는 코루틴
    private IEnumerator GetMemberCoordinationCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            // 요청을 보내고 기다림
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                // 서버로부터 받은 JSON 데이터를 파싱
                string jsonResponse = request.downloadHandler.text;
                List<CoordinationListResponseDTO> coordinationList = JsonConvert.DeserializeObject<List<CoordinationListResponseDTO>>(jsonResponse);

                foreach (var coordination in coordinationList)
                {
                    // CoordinationTitle 프리팹을 Content 안에 생성
                    GameObject newCoordinationTitle = Instantiate(coordinationTitle, contentParent);

                    // CoordinationTitle의 자식 오브젝트에서 Text 컴포넌트를 찾아서 제목을 설정
                    Text titleTextComponent = newCoordinationTitle.transform.Find("CoordinationTitleText").GetComponent<Text>();
                    titleTextComponent.text = coordination.title;
                }
            }
        }
    }
}
