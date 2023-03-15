using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

public class MainMenu : MonoBehaviour
{
    Button newGameBtn;
    Button continueBtn;
    Button quitBtn;

    PlayableDirector director;

    private void Awake()
    {
        newGameBtn = transform.GetChild(1).GetComponent<Button>();
        continueBtn = transform.GetChild(2).GetComponent<Button>();
        quitBtn = transform.GetChild(3).GetComponent<Button>();

        newGameBtn.onClick.AddListener(PlayTimeline);
        continueBtn.onClick.AddListener(ContinueGame);
        quitBtn.onClick.AddListener(QuitGame);

        director = FindObjectOfType<PlayableDirector>();
        director.stopped += NewGame;
        //Debug.Log(director.gameObject.name);
    }
    void PlayTimeline()
    {
        if (director.state != PlayState.Playing)
        {
            //Debug.Log("PlayTimeline");
            director.Play();
        }
    }
    void NewGame(PlayableDirector bo)
    {
        //Debug.Log("NewGame");
        PlayerPrefs.DeleteAll();
        // ת������
        SceneController.Instance.TransitionToFirstLevel();
    }
    void ContinueGame()
    {
        //Debug.Log("ContinueGame");
        // ת����������ȡ����
        SceneController.Instance.TransitionToLoadGame();
    }
    void QuitGame()
    {
        //Debug.Log("�˳���Ϸ");
        Application.Quit();
    }
}
