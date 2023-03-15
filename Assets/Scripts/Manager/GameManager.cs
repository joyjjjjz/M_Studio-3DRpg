using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cinemachine;

// ��Ϸ�����ߣ������߷��񣬿�����Ϸ�е�ʵ��
public class GameManager : SingleTon<GameManager>
{
    // ������Ч
    public AudioSource levelUpAudio;
    // ʤ����Ч
    public AudioSource victoryAudio;
    // ����Ƿ�����
    public bool isPlayerDeath = false;


    public CharacterStats playerStates;
    // ʵ��ͳһʵ����IEndGameObserver�ӿ�
    public List<IEndGameObserver> endGameObserversList = new List<IEndGameObserver>();
    private CinemachineFreeLook followCamera;
    protected virtual void Awake()
    {
        base.Awake();
        // ����ɾ����Ҫɾ������ű�
        DontDestroyOnLoad(this);
    }
    // ע�᷽��
    public void RigisterPlayer(CharacterStats player)
    {
        playerStates = player;
        isPlayerDeath = false;

        // �������
        //followCamera = FindObjectOfType<CinemachineFreeLook>();
        //if(followCamera != null)
        //{
        //    followCamera.Follow = playerStates.transform.GetChild(2);
        //    followCamera.LookAt = playerStates.transform.GetChild(2);
        //}
    }
    // 
    public void AddEndGameObserver(IEndGameObserver ob)
    {
        endGameObserversList.Add(ob);
    }
    public void RemoveEndGameObserver(IEndGameObserver ob)
    {
        endGameObserversList.Remove(ob);
    }
    // �㲥
    public void NotifyObservers()
    {
        foreach (var observer in endGameObserversList)
        {
            observer.EndNotify();
        }
    }

    // �Ҵ�����
    public Transform GetEntrance()
    {
        foreach (var item in FindObjectsOfType<TransitionDestination>())
        {
            if (item.destinationTag == TransitionDestination.DestinationTag.ENTER)
                return item.transform;
        }
        return null;
    }

    // ����������Ч
    public void PlayLevelUpSound()
    {
        if (levelUpAudio != null)
        {
            levelUpAudio.Stop();
            levelUpAudio.Play();
        }
    }
    // ����ʤ����Ч
    public void PlayVictorySound()
    {
        if (victoryAudio != null)
        {
            victoryAudio.Stop();
            victoryAudio.Play();
        }
    }
}
