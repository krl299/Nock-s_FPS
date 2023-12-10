using Palmmedia.ReportGenerator.Core;
using UnityEngine;
using static Models;

public class WeaponController : MonoBehaviour
{

    private PlayerController player;

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

        targetWeaponRotation.y += weaponSettings.swayAmount * 
            (weaponSettings.swayXInverted ? -player.viewInput.x : player.viewInput.x) * Time.deltaTime;
        targetWeaponRotation.x += weaponSettings.swayAmount * 
            (weaponSettings.swayYInverted ? player.viewInput.y : -player.viewInput.y) * Time.deltaTime;
        
        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -weaponSettings.swayClampX, 
            weaponSettings.swayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -weaponSettings.swayClampY, 
            weaponSettings.swayClampY);
        targetWeaponRotation.z = targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, 
                        ref targetWeaponRotationVelocity, weaponSettings.swayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, 
                        ref newWeaponRotationVelocity, weaponSettings.swaySmoothing);

        /*targetWeaponMovementRotation.z = weaponSettings.movementSwayX * (weaponSettings.swayXInverted ? 
                       -player.moveInput.x : player.moveInput.x) * Time.deltaTime;
        targetWeaponMovementRotation.x = weaponSettings.movementSwayY * (weaponSettings.swayYInverted ?
                       player.moveInput.y : -player.moveInput.y) * Time.deltaTime;

        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation,
            Vector3.zero, ref targetWeaponMovementRotationVelocity, weaponSettings.movementSwaySmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation,
            targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, weaponSettings.movementSwaySmoothing);
        */
        transform.localRotation = Quaternion.Euler(newWeaponRotation);
    }
}