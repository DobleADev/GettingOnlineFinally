using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableStandalone : InteractableBehaviour
{
    [SerializeField] string _hint = "Press something to interact";
    public string hint { get { return _hint; } set { _hint = value; } }
    [SerializeField] UnityEvent _onInteract;

    public override string GetHint()
    {
        return _hint;
    }

    public override void Interact()
    {
        if (_onInteract != null) _onInteract.Invoke();
    }
}
