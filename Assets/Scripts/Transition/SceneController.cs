using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

// �����л�
public class SceneController : SingleTon<SceneController>, IEndGameObserver
{
    bool fadeOk; // �Ƿ񲥷Ź�
    public GameObject playerPrefab;
    GameObject player;
    NavMeshAgent agent;
    // �����л�����
    public SceneFader sceneFaderPrefab;
    protected override void Awake()
    {
        base.Awake();
        // ����ɾ����Ҫɾ������ű�
        DontDestroyOnLoad(this);
    }
    private void Start()
    {
        GameManager.Instance.AddEndGameObserver(this);
        fadeOk = false;
        //StartCoroutine(TestEnum());
    }

    public void TransitionToDestination(TransitionPoint transitionPoint)
    {
        switch (transitionPoint.transitionType)
        {
            case TransitionPoint.TransitionType.SameScene:
                StartCoroutine(Transition(SceneManager.GetActiveScene().name, transitionPoint.destinationTag));
                break;
            case TransitionPoint.TransitionType.DifferentScene:
                StartCoroutine(Transition(transitionPoint.sceneName, transitionPoint.destinationTag));
                break;
            case TransitionPoint.TransitionType.EndGame:
                // ������Ϸ���ص�������
                TransitionToMain();
                break;
        }
    }
    IEnumerator Transition(string sceneName, TransitionDestination.DestinationTag destination)
    {
        // TODO:�����������
        SaveManager.Instance.SavePlayerData();
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneFader fade = Instantiate(sceneFaderPrefab);
            yield return StartCoroutine(fade.FadeOut(2f));
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return Instantiate(playerPrefab, GetDestination(destination).transform.position, GetDestination(destination).transform.rotation);
            // �����������
            SaveManager.Instance.LoadPlayerData();
            yield return StartCoroutine(fade.FadeIn(2f));
            yield break;
        }
        else
        {
            // ͬ����
            player = GameManager.Instance.playerStates.gameObject;
            agent = player.GetComponent<NavMeshAgent>();
            agent.enabled = false;
            player.transform.SetPositionAndRotation(GetDestination(destination).transform.position, GetDestination(destination).transform.rotation);
            agent.enabled = true;
            yield return null;
        }
    }
    private TransitionDestination GetDestination(TransitionDestination.DestinationTag destinationTag)
    {
        var entrances = FindObjectsOfType<TransitionDestination>();
        for(int i = 0; i < entrances.Length; i++)
        {
            if(entrances[i].destinationTag == destinationTag)
            {
                return entrances[i];
            }
        }
        return null;
    }

    // ���س���
    public void TransitionToFirstLevel()// ����Ϸ
    {
        StartCoroutine(LoadLevel("Level1"));
        //StartCoroutine(LoadLevel("Game"));
    }
    public void TransitionToLoadGame()// ������Ϸ
    {
        StartCoroutine(LoadLevel(SaveManager.Instance.SceneName));// ���ݱ���Ĺؿ�������������ؿ����������ݱ������������Լ���start��ȡ
    }
    public void TransitionToMain()// ���˵�
    {
        StartCoroutine(LoadMain());
    }
    IEnumerator LoadLevel(string scene)
    {
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (scene != "")
        {
            //Debug.Log("fade ing");
            yield return StartCoroutine(fade.FadeOut(2f));
            //Debug.Log("fade ok");
            // ���س���
            //Debug.Log("LoadSceneAsync qian");
            yield return SceneManager.LoadSceneAsync(scene);
            //Debug.Log("LoadSceneAsync hou");
            // ʵ��������
            //Debug.Log("Instantiate playerPrefab qian");
            yield return player = Instantiate(playerPrefab, GameManager.Instance.GetEntrance().position, GameManager.Instance.GetEntrance().rotation);
            //Debug.Log("Instantiate playerPrefab hou");

            // �ٴα�������
            // ����Ϸʱɾ�������ݣ������ʼ��������
            SaveManager.Instance.SavePlayerData();
            yield return StartCoroutine(fade.FadeIn(2f));
            yield break;
        }
    }
    // ����������
    IEnumerator LoadMain()
    {
        SceneFader fade = Instantiate(sceneFaderPrefab);
        // ���س���
        yield return StartCoroutine(fade.FadeOut(2f));
        yield return SceneManager.LoadSceneAsync("Mains");
        yield return StartCoroutine(fade.FadeIn(2f));
        yield break;
    }

    public void EndNotify()
    {
        if (!fadeOk)
        {
            fadeOk = true;
            StartCoroutine(LoadMain());
        }
    }
    float timers = 0.0f;
    IEnumerator TestEnum()
    {
        while (timers <= 6)
        {
            timers += Time.deltaTime;
            Debug.Log(timers);
            yield return new WaitForSeconds(2);
            Debug.Log("TestEnum");
        }
        //yield break;
    }
}
