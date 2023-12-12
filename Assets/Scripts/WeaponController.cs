using Palmmedia.ReportGenerator.Core;
using UnityEngine;
using static Models;

public class WeaponController : MonoBehaviour
{

    private PlayerController player;

    [Header("References")]
    public Animator weaponAnimator;

    [Header("Settings")]
    public WeaponSettingsModel weaponSettings;

    bool isInitialized;
    
    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;

    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    private bool isGroundedTrigger;
    public float fallingDelay;

    [Header("Weapon Sway")]
    public Transform weaponSwayObject;

    public float swayAmountA = 1;
    public float swayAmountB = 2;
    public float swayScale = 600;
    public float swayLerpSped = 14;

    public float swayTime;
    public Vector3 swayPosition;


    [Header("Sights")]
    public Transform sightTarget;
    public float sightOffset;
    public float aimingInTime;
    private Vector3 weaponSwayPosition;
    private Vector3 weaponSwayPositionVelocity;
    [HideInInspector]
    public bool isAimingIn;


    private void Start()
    {
        newWeaponRotation = transform.localRotation.eulerAngles;
    }

    public void Initialize(PlayerController playerController)
    {
        player = playerController;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        CalculateWeaponRotation();
        SetWeaponAnimations();
        CalculateWeaponSway();
        CalculateAimingIn();
    }

    private void CalculateAimingIn()
    {
        var targetPosition = transform.position;

        if (isAimingIn)
        {
            targetPosition = player.camera.transform.position + (weaponSwayObject.transform.position - sightTarget.position) + (player.camera.transform.forward * sightOffset);
        }

        weaponSwayPosition = weaponSwayObject.transform.position;
        weaponSwayPosition = Vector3.SmoothDamp(weaponSwayPosition, targetPosition, ref weaponSwayPositionVelocity, aimingInTime);
        weaponSwayObject.transform.position = weaponSwayPosition + swayPosition;
    }

    public void TriggerJump()
    {
        isGroundedTrigger = false;
        weaponAnimator.SetTrigger("Jump");
    }

    private void CalculateWeaponRotation()
    {
        weaponAnimator.speed = player.weaponAnimationSpeed;

        targetWeaponRotation.y += (isAimingIn ? weaponSettings.swayAmount / 2 : weaponSettings.swayAmount) *
            (weaponSettings.swayXInverted ? -player.viewInput.x : player.viewInput.x) * Time.deltaTime;
        targetWeaponRotation.x += (isAimingIn ? weaponSettings.swayAmount / 2 : weaponSettings.swayAmount) *
            (weaponSettings.swayYInverted ? player.viewInput.y : -player.viewInput.y) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -weaponSettings.swayClampX,
            weaponSettings.swayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -weaponSettings.swayClampY,
            weaponSettings.swayClampY);
        targetWeaponRotation.z = isAimingIn ? 0 : targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero,
                        ref targetWeaponRotationVelocity, weaponSettings.swayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation,
                        ref newWeaponRotationVelocity, weaponSettings.swaySmoothing);

        targetWeaponMovementRotation.z = (isAimingIn ? weaponSettings.movementSwayX / 4 : weaponSettings.movementSwayX) * (weaponSettings.swayXInverted ? -player.moveInput.x : player.moveInput.x);
        targetWeaponMovementRotation.x = (isAimingIn ? weaponSettings.movementSwayY / 4 : weaponSettings.movementSwayY) * (weaponSettings.swayYInverted ? -player.moveInput.y : player.moveInput.y);

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero,
                                   ref targetWeaponMovementRotationVelocity, weaponSettings.swayResetSmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation,
                                   ref newWeaponMovementRotationVelocity, weaponSettings.swaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
    }

    private void SetWeaponAnimations()
    {
        if (isGroundedTrigger)
            fallingDelay = 0;
        else
            fallingDelay += Time.deltaTime;

        if (player.isGrounded && !isGroundedTrigger && fallingDelay > 0.1)
        {
            weaponAnimator.SetTrigger("Landing");
            isGroundedTrigger = true;
        }
        else if (!player.isGrounded && isGroundedTrigger)
        {
            weaponAnimator.SetTrigger("Falling");
            isGroundedTrigger = false;
        }

        weaponAnimator.SetBool("isSprinting", player.isSprinting);
    }

    private void CalculateWeaponSway()
    {
        var targetposition = LissajousCurve(swayTime, swayAmountA, swayAmountB) / (isAimingIn ? swayScale * 4 : swayScale);

        swayPosition = Vector3.Lerp(swayPosition, targetposition, Time.smoothDeltaTime * swayLerpSped);
        swayTime += Time.deltaTime;

    }

    private Vector3 LissajousCurve(float Time, float A, float B)
    {
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
    }
}