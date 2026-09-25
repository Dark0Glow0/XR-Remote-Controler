using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Content.Interaction;

public class ControlRemotoRobot : MonoBehaviour
{
    [Header("ROBOT")]
    [SerializeField] private Transform robot;

    [Header("CONTROLES UI 3D")]
    [SerializeField] private XRLever lever;
    [SerializeField] private XRJoystick joystick;
    [SerializeField] private XRKnob knob;
    [SerializeField] private XRSlider slider;

    [Header("BATERÍA")]
    [SerializeField] private XRSocketInteractor batterySocket;
    [SerializeField] private XRBaseInteractable battery;

    [Header("VELOCIDAD")]
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("ROTACIÓN")]
    [SerializeField] private float rotationSpeed = 90f;

    private Vector2 joystickValue;
    private float knobValue = 0.5f;
    private float currentSpeed;

    private bool hasBattery;
    private bool leverActive;

    private void Awake()
    {
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.5f);
    }

    private void Start()
    {
        if (joystick != null)
            joystickValue = joystick.value;

        if (knob != null)
            knobValue = knob.value;

        if (slider != null)
            UpdateSpeed(slider.value);

        hasBattery = false;
        leverActive = false;
    }

    private void OnEnable()
    {
        if (lever != null)
        {
            lever.onLeverActivate.AddListener(LeverOn);
            lever.onLeverDeactivate.AddListener(LeverOff);
        }

        if (joystick != null)
        {
            joystick.onValueChangeX.AddListener(JoystickX);
            joystick.onValueChangeY.AddListener(JoystickY);
        }

        if (knob != null)
            knob.onValueChange.AddListener(KnobChange);

        if (slider != null)
            slider.onValueChange.AddListener(SliderChange);

        if (batterySocket != null)
        {
            batterySocket.selectEntered.AddListener(BatteryInserted);
            batterySocket.selectExited.AddListener(BatteryRemoved);
        }
    }

    private void OnDisable()
    {
        if (lever != null)
        {
            lever.onLeverActivate.RemoveListener(LeverOn);
            lever.onLeverDeactivate.RemoveListener(LeverOff);
        }

        if (joystick != null)
        {
            joystick.onValueChangeX.RemoveListener(JoystickX);
            joystick.onValueChangeY.RemoveListener(JoystickY);
        }

        if (knob != null)
            knob.onValueChange.RemoveListener(KnobChange);

        if (slider != null)
            slider.onValueChange.RemoveListener(SliderChange);

        if (batterySocket != null)
        {
            batterySocket.selectEntered.RemoveListener(BatteryInserted);
            batterySocket.selectExited.RemoveListener(BatteryRemoved);
        }
    }

    private void Update()
    {
        if (!hasBattery || !leverActive || robot == null)
            return;

        MoveRobot();
        RotateRobot();
    }

    // =====================================================
    // LEVER
    // =====================================================

    private void LeverOn()
    {
        leverActive = true;
    }

    private void LeverOff()
    {
        leverActive = false;
        joystickValue = Vector2.zero;
    }

    // =====================================================
    // JOYSTICK
    // =====================================================

    private void JoystickX(float value)
    {
        joystickValue.x = value;
    }

    private void JoystickY(float value)
    {
        joystickValue.y = value;
    }

    private void MoveRobot()
    {
        Vector3 movement = new Vector3(
            joystickValue.x,
            0f,
            joystickValue.y
        );

        robot.position += movement * currentSpeed * Time.deltaTime;
    }

    // =====================================================
    // KNOB
    // =====================================================

    private void KnobChange(float value)
    {
        knobValue = value;
    }

    private void RotateRobot()
    {
        // Convierte el valor del Knob:
        // 0   = -1
        // 0.5 = 0
        // 1   = +1

        float rotationInput = (knobValue - 0.5f) * 2f;

        robot.Rotate(
            Vector3.up,
            rotationInput * rotationSpeed * Time.deltaTime,
            Space.World
        );
    }

    // =====================================================
    // SLIDER = VELOCIDAD
    // =====================================================

    private void SliderChange(float value)
    {
        UpdateSpeed(value);
    }

    private void UpdateSpeed(float value)
    {
        currentSpeed = Mathf.Lerp(
            minSpeed,
            maxSpeed,
            Mathf.Clamp01(value)
        );
    }

    // =====================================================
    // SOCKET / BATERÍA
    // =====================================================

    private void BatteryInserted(SelectEnterEventArgs args)
    {
        if (battery == null)
        {
            hasBattery = true;
            return;
        }

        if (args.interactableObject == battery)
        {
            hasBattery = true;
        }
    }

    private void BatteryRemoved(SelectExitEventArgs args)
    {
        if (battery == null)
        {
            hasBattery = false;
            leverActive = false;
            joystickValue = Vector2.zero;
            return;
        }

        if (args.interactableObject == battery)
        {
            hasBattery = false;
            leverActive = false;
            joystickValue = Vector2.zero;
        }
    }
}
