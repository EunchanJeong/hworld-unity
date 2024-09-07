using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using System;

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
    public int point {get; set;}
}

public class QuestManager : MonoBehaviour
{
    public GameObject questTitlePanel;
    public GameObject contentParent;
    public GameObject contentBox;
    private string apiUrl = "http://localhost:8080/quests";
    private string startQuestApiUrl = "http://localhost:8080/quests/start/";
    private string finishQuestApiUrl = "http://localhost:8080/quests/finish/";

    private void Start()
    {
        // 퀘스트 목록을 가져오는 메서드 호출
        GetQuestList(0);
    }

    public void GetQuestList(int currentQuestId)
    {
        StartCoroutine(GetQuestListCoroutine(currentQuestId));
    }

    private IEnumerator GetQuestListCoroutine(int currentQuestId)
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
                
                // 기존의 패널 안에 있는 모든 자식 요소 제거 (중복 방지)
                foreach (Transform child in contentParent.transform)
                {
                    Destroy(child.gameObject);
                }

                // 퀘스트 리스트의 각 항목을 패널로 생성하여 추가
                foreach (var quest in questList)
                {
                    // 파라미터가 0일 시, 첫 퀘스트를 currentQuest로 설정
                    if (currentQuestId == 0)
                    {
                        currentQuestId = quest.questId;
                    }
                    // 퀘스트 내용 패널 세팅
                    if (currentQuestId == quest.questId)
                    {
                        setQuestContent(quest);
                    }

                    Debug.Log(quest.title);

                    GameObject newQuest = Instantiate(questTitlePanel, contentParent.transform);
                    newQuest.transform.Find("QuestTitleText").GetComponent<Text>().text = quest.title;
                    newQuest.transform.Find("SeqText").GetComponent<Text>().text = quest.questId.ToString();
                    GameObject statusBtn = newQuest.transform.Find("StatusBtn").gameObject;
                    statusBtn.transform.Find("StatusText").GetComponent<Text>().text = quest.progress;
                    switch (quest.progress)
                    {
                        case "시작가능":
                            statusBtn.GetComponent<Image>().color = new Color(221f / 255f, 207f / 255f, 9f / 255f);
                            break;
                        case "진행중":
                            statusBtn.GetComponent<Image>().color = new Color(25f / 255f, 62f / 255f, 203f / 255f);
                            break;
                        case "완료가능":
                            statusBtn.GetComponent<Image>().color = new Color(255f / 255f, 144f / 255f, 0f / 255f);
                            break;
                        case "완료":
                            statusBtn.GetComponent<Image>().color = new Color(80f / 255f, 187f / 255f, 71f / 255f);
                            break;
                        default:
                            break;
                    }

                    // 퀘스트 목록 버튼에 클릭 이벤트 추가
                    newQuest.GetComponent<Button>().onClick.AddListener(() => setQuestContent(quest));
                }
            }
        }
    }

    // 퀘스트 내용을 세팅하는 함수
    private void setQuestContent(QuestListResponseDTO quest)
    {
        Debug.Log(quest.questId);
        contentBox.transform.Find("SeqText").GetComponent<Text>().text = $"No.{quest.questId}";
        contentBox.transform.Find("TitleText").GetComponent<Text>().text = quest.title;
        contentBox.transform.Find("ContentText").GetComponent<Text>().text = quest.content;
        contentBox.transform.Find("RewardText").transform.GetComponent<Text>().text = $"보상: {quest.point}P";
        contentBox.transform.Find("StatusBtn").transform.Find("StatusText").GetComponent<Text>().text = quest.progress;
        switch (quest.progress)
            {
                case "시작가능":
                    contentBox.transform.Find("StatusBtn").GetComponent<Image>().color = new Color(221f / 255f, 207f / 255f, 9f / 255f);
                    contentBox.transform.Find("ProgressBtn").gameObject.SetActive(true);
                    contentBox.transform.Find("ProgressBtn").GetComponent<Image>().color = new Color(152f / 255f, 152f / 255f, 152f / 255f);
                    contentBox.transform.Find("ProgressBtn").transform.Find("ProgressText").GetComponent<Text>().text = "시작하기";
                    contentBox.transform.Find("ProgressBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                    contentBox.transform.Find("ProgressBtn").GetComponent<Button>().onClick.AddListener(() => StartQuest(quest.questId));
                    break;
                case "진행중":
                    contentBox.transform.Find("StatusBtn").GetComponent<Image>().color = new Color(25f / 255f, 62f / 255f, 203f / 255f);
                    contentBox.transform.Find("ProgressBtn").gameObject.SetActive(false);
                    break;
                case "완료가능":
                    contentBox.transform.Find("StatusBtn").GetComponent<Image>().color = new Color(255f / 255f, 144f / 255f, 0f / 255f);
                    contentBox.transform.Find("ProgressBtn").gameObject.SetActive(true);
                    contentBox.transform.Find("ProgressBtn").GetComponent<Image>().color = new Color(255f / 255f, 144f / 255f, 0f / 255f);
                    contentBox.transform.Find("ProgressBtn").transform.Find("ProgressText").GetComponent<Text>().text = "보상받기";
                    contentBox.transform.Find("ProgressBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                    contentBox.transform.Find("ProgressBtn").GetComponent<Button>().onClick.AddListener(() => FinishQuest(quest.questId));
                    break;
                case "완료":
                    contentBox.transform.Find("StatusBtn").GetComponent<Image>().color = new Color(80f / 255f, 187f / 255f, 71f / 255f);
                    contentBox.transform.Find("ProgressBtn").gameObject.SetActive(false);
                    break;
                default:
                    break;
            }
    }


    private void StartQuest(int questId)
    {
        Debug.Log("Start Quest: " + questId);
        StartCoroutine(StartQuestCoroutine(questId));
    }

    private IEnumerator StartQuestCoroutine(int questId)
    {
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(startQuestApiUrl + questId, ""))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("퀘스트 시작하기 에러 : " + request.error);
            }
            else
            {
                StartCoroutine(GetQuestListCoroutine(questId));
            }
        }
    }

    private void FinishQuest(int questId)
    {
        Debug.Log("Finish Quest: " + questId);
        StartCoroutine(FinishQuestCoroutine(questId));
    }

    private IEnumerator FinishQuestCoroutine(int questId)
    {
        using (UnityWebRequest request = UnityWebRequest.Put(finishQuestApiUrl + questId, ""))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("퀘스트 끝내기 에러 : " + request.error);
            }
            else
            {
                GetQuestList(questId);
            }
        }
    }

}