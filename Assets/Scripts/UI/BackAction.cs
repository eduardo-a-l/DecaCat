using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BackAction : MonoBehaviour
{
    private Action action;

    public static void Attach(GameObject target, Action backAction)
    {
        if (target == null || backAction == null)
            return;

        target.AddComponent<BackAction>().action = backAction;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            action();
    }
}
