using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public GameObject healthUIPrefab; // Ѫ��ͼƬPrefab
    Transform UIbar;    // UIbar Ѫ��ͼƬPrefab��λ��

    public Transform barPoint; // �����Ѫ��pos
    public bool alwaysVisible; // �Ƿ�һֱ�ɼ�
    public float visibleTime; // ��ʾʱ��
    Image healthSlider; // ��ɫ��ײ��ͼƬ
    Transform cam;      // �����

    private float timeLeft; // ��ʾʱ��ʣ��ʱ��

    CharacterStats currentStats;

    private void Awake()
    {
        currentStats = GetComponent<CharacterStats>();
        currentStats.UpdateHealthBarOnAttack += UpdateHealthBar;
    }
    private void OnEnable()
    {
        cam = Camera.main.transform;
        // ���е�Canvas
        foreach (Canvas canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                // canvas.transform ����prefab���õ�λ��
                UIbar = Instantiate(healthUIPrefab, canvas.transform).transform;
                healthSlider = UIbar.GetChild(0).GetComponent<Image>();
                UIbar.gameObject.SetActive(alwaysVisible);
            }
        }
    }
    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            if (UIbar != null)
            {
                Destroy(UIbar.gameObject);
                return;
            }
        }
        if (UIbar == null)
        {
            return;
        }
        UIbar.gameObject.SetActive(true);
        timeLeft = visibleTime;

        // ����Ѫ���ٷְ�
        float sliderPercent = (float)currentHealth / maxHealth;
        healthSlider.fillAmount = sliderPercent;
    }
    
    private void LateUpdate()
    {
        if (UIbar != null)
        {
            UIbar.position = barPoint.position;
            UIbar.forward = -cam.forward;

            if (timeLeft <= 0 && !alwaysVisible)
                UIbar.gameObject.SetActive(false);
            else
                timeLeft -= Time.deltaTime;
        }
    }
}
