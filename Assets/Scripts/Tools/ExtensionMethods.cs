using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ϊ�˵��˹�����ʱ���ж�����Ƿ��ڹ��������η�Χ�ڡ������͹�����Ч
public static class ExtensionMethods
{
    private const float dotThreshold = 0.5f;
    public static bool IsFacingTarget(this Transform transform, Transform target)
    {
        var vectorToTarget = target.position - transform.position;
        vectorToTarget.Normalize();
        float dot = Vector3.Dot(transform.forward, vectorToTarget);
        return dot >= dotThreshold;
    }
}
