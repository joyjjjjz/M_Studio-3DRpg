using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ��������ԡ�����ͨ���Ժ͹�������
public class CharacterStats : MonoBehaviour
{
    public AudioSource hurtAudio;
    public AudioSource deathAudio;

    public event Action<int, int> UpdateHealthBarOnAttack;
    public CharacterData_SO characterTemplateData;
    public CharacterData_SO characterData;
    public Attack_SO attackData;
    [HideInInspector]
    public bool isCritical;

    private void Awake()
    {
        if(characterTemplateData != null)
        {
            characterData = Instantiate(characterTemplateData);
        }
    }
    #region Character state
    public int MaxHealth
    {
        get { if (characterData != null) return characterData.maxHealth; else { return 100; } }
        set { characterData.maxHealth = value; }
    }
    public int CurrentHealth
    {
        get { if (characterData != null) return characterData.currentHealth; else { return 100; } }
        set { characterData.currentHealth = value; }
    }
    public int BaseDefence
    {
        get { if (characterData != null) return characterData.baseDefence; else { return 30; } }
        set { characterData.baseDefence = value; }
    }
    public int CurrentDefence
    {
        get { if (characterData != null) return characterData.currentDefence; else { return 30; } }
        set { characterData.currentDefence = value; }
    }
    #endregion

    #region Character Combat
    // ����
    public void TakeDamage(CharacterStats attacker, CharacterStats defener, bool isPlayerHurtAudio = false)
    {
        // TODO:���ݾ����ж��Ƿ񹥻���Ч

        int damage = Mathf.Max(attacker.CurrentDamage(attacker) - defener.CurrentDefence, 1);
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);
        // ���ݹ����߱������ŷ����ߵ����˶���
        if (attacker.isCritical)
        {
            defener.GetComponent<Animator>().SetTrigger("Hit");
            // ���������ܷ�������˭��Ҫ������Ч
            if (defener.hurtAudio != null)
            {
                defener.hurtAudio.Stop();
                defener.hurtAudio.Play();
            }
        }
        else
        {
            // ��ͨ���˻�����Ч��ÿ��enemy���У���������ʯͷ�˵�ʱ�����ǻᷢ��
            if (defener.tag != Tags.Player || isPlayerHurtAudio)
            {
                if (defener.hurtAudio != null)
                {
                    defener.hurtAudio.Stop();
                    defener.hurtAudio.Play();
                }
            }
        }
        //TODO:ui
        UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);
        //TODO:���� 
        // ��������Ϊ0�����������Ӿ���
        if (CurrentHealth <= 0)
        {
            // ֻ�й���������Ҳ����Ӿ���Ͳ���������Ч
            if (attacker.tag == Tags.Player)
            {
                attacker.characterData.UpdateExp(characterData.killPoint);
            }
            if (deathAudio != null)
            {
                deathAudio.Stop();
                deathAudio.Play();
            }
        }
    }
    // ���� - ʯͷ��ײ��ʱ��
    public void TakeDamage(int damage, CharacterStats defener)
    {
        // ʯͷ��ײ����һ���ʯͷ��ʱ����Ч
        if (defener.hurtAudio != null)
        {
            defener.hurtAudio.Stop();
            defener.hurtAudio.Play();
        }

        int currentDamage = Mathf.Max(damage - defener.CurrentDefence, 1);
        //Debug.Log("CurrentHealth"+CurrentHealth);
        CurrentHealth = Mathf.Max(CurrentHealth - currentDamage, 0);
        //Debug.Log("CurrentHealth" + CurrentHealth);
        //TODO:ui
        UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);

        // ��������Ϊ0��������Ӿ���
        if (CurrentHealth <= 0)
        {
            // ֻ�й���������Ҳ����Ӿ��� �Ͳ���������Ч
            if (this.tag == Tags.Player)
            {
                GameManager.Instance.playerStates.characterData.UpdateExp(characterData.killPoint);
            }
            if (deathAudio != null)
            {
                deathAudio.Stop();
                deathAudio.Play();
            }
        }
    }
    private int CurrentDamage(CharacterStats attacker)
    {
        float coredamage = UnityEngine.Random.Range(attacker.attackData.minDamage, attacker.attackData.maxDamage);
        if (isCritical)
        {
            coredamage *= attacker.attackData.criticalMultiplier;
            //Debug.Log("����");

        }
        return (int)coredamage;
    }
    #endregion
}
