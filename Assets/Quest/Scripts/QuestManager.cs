using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

public class QuestListResponseDTO
{
    public int questId {get; set;}
    public string title {get; set;}
    public string content {get; set;}
    public string startDate {get; set;}
    public string endDate {get; set;}
    public int status {get; set;}
    public string progress {get; set;}
    public string finishedAt {get; set;}
}

public class QuestManager : MonoBehaviour
{
    private string apiUrl = "http://localhost:8080/quests";

    private void Start()
    {
        // 퀘스트 목록을 가져오는 메서드 호출
        GetQuestList();
    }

    public void GetQuestList()
    {
        StartCoroutine(GetQuestListCoroutine());
    }

    private IEnumerator GetQuestListCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("퀘스트 가져오기 에러 : " + request.error);
            }
            else 
            {
                string jsonResponse = request.downloadHandler.text;
                List<QuestListResponseDTO> questList = JsonConvert.DeserializeObject<List<QuestListResponseDTO>>(jsonResponse);
                Debug.Log(questList);
                foreach (var quest in questList)
                {
                    Debug.Log(quest.title);
                }
            }
        }
    }


}