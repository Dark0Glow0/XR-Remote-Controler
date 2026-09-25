using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Content.Interaction;

/// <summary>
/// Controla un objeto (por ejemplo, un cubo que representa un robot)
/// mediante los elementos UI 3D del paquete XRI:
/// XR Lever, XR Joystick, XR Knob, XR Slider y un XR Socket con bateria.
///
/// Este script NO modifica los scripts originales del paquete.
/// Solo escucha sus eventos y utiliza los valores recibidos para mover
/// y rotar el robot.
/// </summary>
public class ControlRemotoRobot : MonoBehaviour
{
    [Header("OBJETO CONTROLADO")]
    [Tooltip("Transform del cubo/robot que sera controlado.")]
    [SerializeField] private Transform robot;

    [Header("ELEMENTOS UI 3D")]
    [Tooltip("XR Lever del control remoto.")]
    [SerializeField] private XRLever lever;

    [Tooltip("XR Joystick del control remoto.")]
    [SerializeField] private XRJoystick joystick;

    [Tooltip("XR Knob del control remoto.")]
    [SerializeField] private XRKnob knob;

    [Tooltip("XR Slider del control remoto. Se utilizara para controlar la velocidad.")]
    [SerializeField] private XRSlider slider;

    [Header("BATERIA / SOCKET")]
    [Tooltip("XR Socket Interactor donde se colocara la bateria.")]
    [SerializeField] private XRSocketInteractor batterySocket;

    [Tooltip("Componente XRGrabInteractable de la bateria que debe entrar en el socket.")]
    [SerializeField] private XRBaseInteractable batteryToAccept;

    [Header("MOVIMIENTO")]
    [Tooltip("Velocidad minima del robot cuando el Slider esta en 0.")]
    [SerializeField] private float minSpeed = 0.5f;

    [Tooltip("Velocidad maxima del robot cuando el Slider esta en 1.")]
    [SerializeField] private float maxSpeed = 5f;

    [Header("ROTACION")]
    [Tooltip("Angulo maximo que puede girar el robot hacia cada lado.")]
    [SerializeField] private float maxRotationAngle = 90f;

    // Valores recibidos desde los UI 3D
    private Vector2 joystickValue = Vector2.zero;
    private float knobValue = 0.5f;
    private float sliderValue = 0.5f;

    // Estado del sistema
    private bool hasBattery = false;
    private bool leverActivated = false;

    private float currentSpeed;
    private Quaternion initialRobotRotation;

    // Offset (en espacio local del robot) entre el pivote del Transform
    // y el centro visual real del modelo. Se usa para que el robot gire
    // sobre si mismo en vez de describir una curva cuando el pivote del
    // modelo no esta centrado.
    private Vector3 visualCenterLocalOffset = Vector3.zero;

    private void Awake()
    {
        // Evita valores invalidos en el Inspector.
        minSpeed = Mathf.Max(0f, minSpeed);
        maxSpeed = Mathf.Max(minSpeed, maxSpeed);

        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, 1f - sliderValue);
    }

    private void Start()
    {
        // Guardamos la rotacion inicial del robot para que el Knob
        // controle un rango fijo de -90 a +90 grados sin acumular vueltas.
        if (robot != null)
        {
            initialRobotRotation = robot.rotation;
            visualCenterLocalOffset = GetLocalCenterOffset(robot);
        }

        // Tomamos los valores iniciales de los componentes del paquete.
        if (joystick != null)
        {
            joystickValue = joystick.value;
        }

        if (knob != null)
        {
            knobValue = knob.value;
        }

        if (slider != null)
        {
            sliderValue = slider.value;
            UpdateSpeed(sliderValue);
        }

        // El sistema comienza apagado hasta recibir la bateria.
        hasBattery = false;
        leverActivated = false;
    }

    private void OnEnable()
    {
        if (lever != null)
        {
            lever.onLeverActivate.AddListener(OnLeverActivate);
            lever.onLeverDeactivate.AddListener(OnLeverDeactivate);
        }

        if (joystick != null)
        {
            joystick.onValueChangeX.AddListener(OnJoystickValueChangeX);
            joystick.onValueChangeY.AddListener(OnJoystickValueChangeY);
        }

        if (knob != null)
        {
            knob.onValueChange.AddListener(OnKnobValueChange);
        }

        if (slider != null)
        {
            slider.onValueChange.AddListener(OnSliderValueChange);
        }

        if (batterySocket != null)
        {
            batterySocket.selectEntered.AddListener(OnBatteryInserted);
            batterySocket.selectExited.AddListener(OnBatteryRemoved);
        }
    }

    private void OnDisable()
    {
        if (lever != null)
        {
            lever.onLeverActivate.RemoveListener(OnLeverActivate);
            lever.onLeverDeactivate.RemoveListener(OnLeverDeactivate);
        }

        if (joystick != null)
        {
            joystick.onValueChangeX.RemoveListener(OnJoystickValueChangeX);
            joystick.onValueChangeY.RemoveListener(OnJoystickValueChangeY);
        }

        if (knob != null)
        {
            knob.onValueChange.RemoveListener(OnKnobValueChange);
        }

        if (slider != null)
        {
            slider.onValueChange.RemoveListener(OnSliderValueChange);
        }

        if (batterySocket != null)
        {
            batterySocket.selectEntered.RemoveListener(OnBatteryInserted);
            batterySocket.selectExited.RemoveListener(OnBatteryRemoved);
        }
    }

    private void Update()
    {
        if (!CanControlRobot() || robot == null)
            return;

        MoveRobot();
        RotateRobot();
    }

    private bool CanControlRobot()
    {
        return hasBattery && leverActivated;
    }

    // Calcula el centro visual real del robot (bounds combinados de todos
    // sus renderers) y lo devuelve como un offset en espacio LOCAL del
    // Transform. Esto nos dice donde esta el "centro" del modelo aunque
    // el pivote del Transform no este ahi.
    private Vector3 GetLocalCenterOffset(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return Vector3.zero;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return target.InverseTransformPoint(bounds.center);
    }

    // =========================================================
    // LEVER
    // =========================================================

    private void OnLeverActivate()
    {
        leverActivated = true;
    }

    private void OnLeverDeactivate()
    {
        leverActivated = false;
        joystickValue = Vector2.zero;
    }

    // =========================================================
    // JOYSTICK
    // =========================================================

    private void OnJoystickValueChangeX(float value)
    {
        joystickValue.x = value;
    }

    private void OnJoystickValueChangeY(float value)
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

    // =========================================================
    // KNOB
    // =========================================================

    private void OnKnobValueChange(float value)
    {
        // Solo guardamos el valor aqui. La rotacion se aplica en Update()
        // (ver RotateRobot), igual que el movimiento se aplica en MoveRobot().
        // Asi, si el Knob se mueve antes de tener bateria/palanca activa,
        // el giro correcto se aplica apenas el robot pueda ser controlado,
        // en vez de perderse porque el evento ya paso.
        knobValue = Mathf.Clamp01(value);
    }

    private void RotateRobot()
    {
        // El valor del Knob se convierte directamente en un angulo:
        // 0   = -90 grados
        // 0.5 =   0 grados
        // 1   = +90 grados
        float angle = Mathf.Lerp(-maxRotationAngle, maxRotationAngle, knobValue);

        Quaternion targetRotation = initialRobotRotation * Quaternion.Euler(0f, angle, 0f);

        // Si el pivote del Transform no coincide con el centro visual del
        // modelo, rotar solo con "robot.rotation = targetRotation" hace que
        // el robot describa una curva/arco en vez de girar sobre si mismo.
        // Por eso guardamos donde queda el centro visual ANTES de rotar, y
        // despues movemos el robot lo justo para que ese centro se mantenga
        // fijo en el mismo punto del mundo.
        Vector3 visualCenterBefore = robot.TransformPoint(visualCenterLocalOffset);

        robot.rotation = targetRotation;

        Vector3 visualCenterAfter = robot.TransformPoint(visualCenterLocalOffset);
        robot.position += visualCenterBefore - visualCenterAfter;
    }

    // =========================================================
    // SLIDER = VELOCIDAD
    // =========================================================

    private void OnSliderValueChange(float value)
    {
        sliderValue = Mathf.Clamp01(value);
        UpdateSpeed(sliderValue);
    }

    private void UpdateSpeed(float value)
    {
        // Se invierte el Slider para que:
        // arriba (1) = velocidad maxima
        // centro (0.5) = velocidad media
        // abajo (0) = velocidad minima
        float invertedValue = 1f - Mathf.Clamp01(value);

        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, invertedValue);
    }

    // =========================================================
    // SOCKET / BATERIA
    // =========================================================

    private void OnBatteryInserted(SelectEnterEventArgs args)
    {
        // Acepta unicamente la bateria que se haya asignado en el Inspector.
        if (batteryToAccept == null)
        {
            // Para evitar que el proyecto falle si no se asigno la referencia,
            // igualmente dejamos que el socket active el sistema.
            hasBattery = true;
            return;
        }

        if (args.interactableObject == batteryToAccept)
        {
            hasBattery = true;
        }
    }

    private void OnBatteryRemoved(SelectExitEventArgs args)
    {
        if (batteryToAccept == null)
        {
            hasBattery = false;
            leverActivated = false;
            return;
        }

        if (args.interactableObject == batteryToAccept)
        {
            hasBattery = false;
            leverActivated = false;
            joystickValue = Vector2.zero;
        }
    }

    // =========================================================
    // METODOS PUBLICOS OPCIONALES
    // =========================================================

    /// <summary>
    /// Permite activar manualmente el sistema desde otro script o evento.
    /// </summary>
    public void ActivarControl()
    {
        hasBattery = true;
    }

    /// <summary>
    /// Permite desactivar manualmente el sistema desde otro script o evento.
    /// </summary>
    public void DesactivarControl()
    {
        hasBattery = false;
        leverActivated = false;
        joystickValue = Vector2.zero;
    }
}