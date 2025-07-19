using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerCharacterController
{
    private CharacterController _cc;
    private float _fallSpeed = 0;
    public float _maxfallSpeed = 20;
    public float gravityScale = 1f;
    public bool snapToGround = true;
    public bool isOnStableGround { get; private set; }
    public LayerMask walkables = ~0;
    // public float downSlopeAdjustment = 0.02f;

    public PlatformerCharacterController(
        CharacterController characterController
        )
    {
        _cc = characterController;
        isOnStableGround = true;
    }

    public void Move(Vector3 moveVelocity)
    {
        Vector3 gravity = gravityScale * Physics.gravity;
        if (moveVelocity.y > 0)
        {
            _fallSpeed = 2 * Mathf.Sqrt(moveVelocity.y * -gravity.y);
            // _fallSpeed = moveVelocity.y * -Physics.gravity.y; 
            isOnStableGround = false;
        }
        moveVelocity.y = 0;

        _fallSpeed += Time.deltaTime * gravity.y;
        _fallSpeed = Mathf.Max(_fallSpeed, -_maxfallSpeed);
        if (isOnStableGround)
        {
            _fallSpeed = Mathf.Max(0, _fallSpeed);
        }

        Vector3 fallVelocity = Time.deltaTime * _fallSpeed * Vector3.up;

        // if (_cc.isGrounded)
        // {
        //     isOnStableGround = true;
        // }

        Vector3 deltaVelocity = new Vector3(moveVelocity.x, 0, moveVelocity.z);

        float ccDetection = 2 * _cc.skinWidth;
        RaycastHit ground;

        if (Physics.SphereCast(
        // _cc.transform.position + _cc.center + (((_cc.height * 0.5f) - _cc.radius) * Vector3.down),
        _cc.transform.position + _cc.center + deltaVelocity + (((_cc.height * 0.5f) - _cc.radius - _cc.skinWidth) * Vector3.down),
        _cc.radius,
        Vector3.down,
        out ground,
        ccDetection + _cc.stepOffset + deltaVelocity.magnitude,
        ~0,
        QueryTriggerInteraction.Ignore
        ) &&
            _fallSpeed <= 0)
        {
            isOnStableGround = true;
            //Not Stable Ground Rejection
            if (Vector3.Angle(ground.normal, Vector3.up) > _cc.slopeLimit
            || !Physics.Raycast(_cc.transform.position + _cc.center + deltaVelocity + ((_cc.height * 0.5f) * Vector3.down), Vector3.down, ccDetection + _cc.stepOffset + deltaVelocity.magnitude, walkables, QueryTriggerInteraction.Ignore))
            {
                isOnStableGround = false;
                // if (moveVelocity.y <= 0)
                // {
                //     Vector3 groundFall = Vector3.ProjectOnPlane(moveVelocity.y * Vector3.up, ground.normal);
                //     moveVelocity = new Vector3(moveVelocity.x, 0, moveVelocity.z) + groundFall;
                // }

            }
            // else
            // {
            //     isOnStableGround = true;
            // }

            //Ground Slope support
            if (
            // !_cc.isGrounded &&
            isOnStableGround &&
            ground.distance > ccDetection &&
            snapToGround
            // ground.distance <= ccDetection + _cc.stepOffset + deltaVelocity.magnitude - Mathf.Min(0, moveVelocity.y) &&
            )
            {
                // Debug.DrawRay(transform.position, ground.normal * 2, Color.blue);
                //                 float mag = moveVelocity.magnitude;
                //                 moveVelocity += (ground.distance - (ccDetection - 0.001f)) * Vector3.down;
                //                 moveVelocity = mag * moveVelocity.normalized;
                moveVelocity = Vector3.ProjectOnPlane(moveVelocity, ground.normal);
                moveVelocity.y -= ground.distance - ccDetection;
            }
            else
            {
                fallVelocity = Vector3.ProjectOnPlane(fallVelocity, ground.normal);
                // fallVelocity.y = 0;
                // moveVelocity.y -= (ground.distance - ccDetection + 0.01f);
            }
        }
        else
        {
            isOnStableGround = false;
        }

        _cc.Move(moveVelocity + fallVelocity);
    }

    public void ResetGravity()
    {
        _fallSpeed = 0;
    }

    public void DetachFromGround()
    {
        isOnStableGround = false;
    }
}
