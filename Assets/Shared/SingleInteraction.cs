using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleInteraction : MonoBehaviour
{
    private Vector3 lastInteractableClosestPoint;
    private bool lastInteractableWasTouched;
    public IGameInteractable interactable { get; private set; }

    void FixedUpdate()
    {
        if (!lastInteractableWasTouched)
        {
            interactable = null;
        }
        lastInteractableWasTouched = false;
    }

    void OnTriggerEnter(Collider other)
    {
        IGameInteractable interactable;
        if ((interactable = other.GetComponent<IGameInteractable>()) != null)
            ProcessInteractable(other, interactable);
    }

    void OnTriggerStay(Collider other)
    {
        IGameInteractable interactable;
        if ((interactable = other.GetComponent<IGameInteractable>()) != null)
            ProcessInteractable(other, interactable);
    }

    private void ProcessInteractable(Collider other, IGameInteractable interactable)
    {
        Vector3 currentPosition = transform.position;
        Vector3 currentClosestPoint = other.ClosestPoint(currentPosition);
        if (interactable != null)
        {
            if (
                Vector3.Distance(currentPosition, currentClosestPoint) >
                Vector3.Distance(currentPosition, lastInteractableClosestPoint)
                || !interactable.IsInteractionEnabled()
                )
            {
                return;
            }
        }
        this.interactable = interactable;
        lastInteractableWasTouched = true;
        lastInteractableClosestPoint = currentClosestPoint;
    }
}

public interface IGameInteractable
{
    bool IsInteractionEnabled();
    string GetName();
    string GetHint();
    void Interact();
}

public abstract class InteractableBehaviour : MonoBehaviour, IGameInteractable
{
    [SerializeField] protected bool interactionEnabled = true;
    public bool IsInteractionEnabled()
    {
        return interactionEnabled;
    }

    public virtual string GetHint()
    {
        return "Press something to interact";
    }
    public abstract void Interact();

    public string GetName()
    {
        return gameObject.name;
    }

}