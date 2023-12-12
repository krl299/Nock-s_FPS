using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static Models;

public class PlayerController : MonoBehaviour
{
    private CharacterController characController;
    private DefaultInput defaultInput;
    [HideInInspector]
    public Vector2 moveInput;
    [HideInInspector]
    public Vector2 viewInput;

    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    [Header("References")]
    public Transform cameraHolder;
    public Transform feetTransform;

    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampXMin = -70;
    public float viewClampXMax  = 80;
    public LayerMask playerMask;
    public LayerMask groundMask;

    [Header("Gravity")]
    public float gravityAmount;
    public float gravityMin;
    private float playerGravity;

    public Vector3 jumpingForce;
    private Vector3 jumpingForceVelocity;

    [Header("Stance")]
    public PlayerStance playerStance;
    public float playerStanceSmoothing;
    public CharacterStance playerStandingStance;
    public CharacterStance playerCrouchingStance;
    public CharacterStance playerProneStance;

    public float stanceHeightErrorMargin = 0.05f;

    private float cameraHeight;
    private float cameraHeightVelocity;

    private Vector3 stanceCapsuleVelocity;
    private float stanceCapsuleHeightVelocity;

    public bool isSprinting;

    private Vector3 newMovementSpeed;
    private Vector3 newMovementSpeedVelocity;

    [Header("Weapon")]
    public WeaponController currentWeapon;
    public float weaponAnimationSpeed = 1;

    [HideInInspector]
    public bool isGrounded;
    [HideInInspector]
    public bool isFalling;

    #region - Awake -
    private void Awake()
    {
        defaultInput = new DefaultInput();

        defaultInput.Character.Movement.performed += e => moveInput = e.ReadValue<Vector2>();
        defaultInput.Character.View.performed += e => viewInput = e.ReadValue<Vector2>();
        defaultInput.Character.Jump.performed += e => Jump();
        defaultInput.Character.Crouch.performed += e => Crouch();
        defaultInput.Character.Prone.performed += e => Prone();
        defaultInput.Character.Sprint.performed += e => ToggleSprint();
        defaultInput.Character.SprintReleased.canceled += e => StopSprint();

        defaultInput.Enable();

        newCameraRotation = cameraHolder.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        characController = GetComponent<CharacterController>();

        cameraHeight = cameraHolder.localPosition.y;

        if (currentWeapon)
            currentWeapon.Initialize(this);
    }

    #endregion

    #region - Update -
    private void Update()
    {
        SetIsGrounded();
        SetIsFalling();
        CalculateView();
        CalculateMovement();
        CalculateJump();
        CalculateStance();

    }

    #endregion

    #region - isFalling / isGrounded -

    private void SetIsGrounded()
    {
        isGrounded = Physics.CheckSphere(feetTransform.position, playerSettings.isGroundedRadius, groundMask);
    }

    private void SetIsFalling()
    {
        isFalling = (!isGrounded && characController.velocity.magnitude > playerSettings.isFallingSpeed);
    }

    #endregion

    #region - Movement / View -
    private void CalculateView()
    {
        newCharacterRotation.y += playerSettings.viewYSensitivity * 
            (playerSettings.viewXInverted ? -viewInput.x : viewInput.x) * Time.deltaTime;
        transform.rotation = Quaternion.Euler(newCharacterRotation);

        newCameraRotation.x += playerSettings.viewXSensitivity * 
            (playerSettings.viewYInverted ? viewInput.y : -viewInput.y) * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, viewClampXMin, viewClampXMax);

        cameraHolder.localRotation = Quaternion.Euler(newCameraRotation);
    }

    private void CalculateMovement()
    {
        if (moveInput.y <= 0.2f)
        {
            isSprinting = false;
        }

        var verticalSpeed = playerSettings.walkingForwardSpeed;
        var horizontalSpeed = playerSettings.walkingStrafeSpeed;

        if (isSprinting)
        {
            verticalSpeed = playerSettings.runningForwardSpeed;
            horizontalSpeed = playerSettings.runningStrafeSpeed;
        }

        if (!isGrounded)
        {
            playerSettings.speedEffector = playerSettings.fallingSpeedEffector;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            playerSettings.speedEffector = playerSettings.crouchSpeedEffector;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            playerSettings.speedEffector = playerSettings.proneSpeedEffector;
        }
        else
        {
            playerSettings.speedEffector = 1;
        }

        weaponAnimationSpeed = characController.velocity.magnitude / (playerSettings.walkingForwardSpeed * playerSettings.speedEffector);

        if (weaponAnimationSpeed > 1)
        {
            weaponAnimationSpeed = 1;
        }

        verticalSpeed *= playerSettings.speedEffector;
        horizontalSpeed *= playerSettings.speedEffector;

        newMovementSpeed = Vector3.SmoothDamp(newMovementSpeed,
            new Vector3(horizontalSpeed * moveInput.x * Time.deltaTime,
                0, verticalSpeed * moveInput.y * Time.deltaTime),
            ref newMovementSpeedVelocity, isGrounded ? playerSettings.movementSmoothing :
            playerSettings.fallingSmoothing);

        var movementSpeed = transform.TransformDirection(newMovementSpeed);

        if (playerGravity > gravityMin)
            playerGravity -= gravityAmount * Time.deltaTime;
        if (playerGravity < -0.1f && isGrounded)
            playerGravity = -0.1f;

        movementSpeed.y += playerGravity;
        movementSpeed += jumpingForce * Time.deltaTime;

        characController.Move(movementSpeed);
    }

    #endregion

    #region - Jumping -

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref jumpingForceVelocity, playerSettings.jumpingFalloff);

    }

    private void Jump()
    {
        if (!isGrounded)
            return;
        if (playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
        {
            if (StanceCheck(playerStandingStance.stanceCollider.height))
                return;
            playerStance = PlayerStance.Stand;
            return;
        }
        jumpingForce = Vector3.up * playerSettings.jumpingHeight;
        playerGravity = 0;
        currentWeapon.TriggerJump();
    }

    #endregion

    #region - Stance -
    private void CalculateStance()
    {
        var currentStance = playerStandingStance;

        if (playerStance == PlayerStance.Crouch)
            currentStance = playerCrouchingStance;
        else if (playerStance == PlayerStance.Prone)
            currentStance = playerProneStance;

        cameraHeight = Mathf.SmoothDamp(cameraHolder.localPosition.y,
            currentStance.cameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x,
            cameraHeight, cameraHolder.localPosition.z);

        characController.height = Mathf.SmoothDamp(characController.height,
                        currentStance.stanceCollider.height, ref stanceCapsuleHeightVelocity,
                        playerStanceSmoothing);
        characController.center = Vector3.SmoothDamp(characController.center,
                        currentStance.stanceCollider.center, ref stanceCapsuleVelocity,
                        playerStanceSmoothing);
    }

    private void Crouch()
    {
        if (playerStance == PlayerStance.Crouch)
        {
            if (StanceCheck(playerStandingStance.stanceCollider.height))
                return;
            playerStance = PlayerStance.Stand;
            return;
        }
        if (StanceCheck(playerCrouchingStance.stanceCollider.height))
            return;
        playerStance = PlayerStance.Crouch;
    }

    private void Prone()
    {
        playerStance = PlayerStance.Prone;
    }

    private bool StanceCheck(float stanceCheckHeight)
    {
        var start = new Vector3(feetTransform.position.x,
            feetTransform.position.y + stanceHeightErrorMargin + characController.radius,
            feetTransform.position.z);
        var end = new Vector3(feetTransform.position.x,
            feetTransform.position.y - stanceHeightErrorMargin - characController.radius + stanceCheckHeight,
            feetTransform.position.z);

        return Physics.CheckCapsule(start, end, characController.radius, playerMask);
    }

    #endregion

    #region - Sprinting -
    private void ToggleSprint()
    {
        if (moveInput.y <= 0.2f)
        {
            isSprinting = false;
            return;
        }
        isSprinting = !isSprinting;
    }

    private void StopSprint()
    {
        if (playerSettings.sprintingHold)
            isSprinting = false;   
    }

    #endregion

    #region - Gizmos -

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(feetTransform.position, playerSettings.isGroundedRadius);
    }

    #endregion
}
