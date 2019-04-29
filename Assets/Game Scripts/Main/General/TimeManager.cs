using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    private static TimeManager instance;
    public static TimeManager Instance { get { return instance; } }

    public bool holdToPause;
    public float slowMoSpeed;
    public float slowMoMin;
    public float slowMoMax;
    public float timeChangeStep;
    public float enterSlowMoSmoothing;
    public float exitSlowMoSmoothing;
    public float changeSlowMoSmoothing;

    private float currentSpeed;
    private float targetSpeed;
    public bool isInSlowMo;

    private float fixedDeltaRatio;
    private static float gameSpeedFactor;
    public static bool IsInSlowMo { get { return instance.isInSlowMo; } }
    public static float GameSpeedFactor { get { return gameSpeedFactor; } }
    public static float RealDeltaTime { get { return Time.deltaTime / gameSpeedFactor; } }

    public static void ActivateSlowMo()
    {
        instance.EnterSlowMo();
    }

    public static void DeactivateSlowMo()
    {
        instance.ExitSlowMo();
    }

    private float CurrentSpeed
    {
        get { return currentSpeed; }
        set
        {
            currentSpeed = value;
            gameSpeedFactor = value;
        }
    }

    private void Awake()
    {
        CurrentSpeed = 1.0f;
        gameSpeedFactor = 1.0f;
        fixedDeltaRatio = Time.fixedDeltaTime;
        isInSlowMo = false;
        instance = this;
    }

    private void Update()
    {
        if (GInput.GetButtonDown(Cmd.Pause))
        {
            if (holdToPause)
            {
                EnterSlowMo();
            }
            else
            {
                if (isInSlowMo)
                {
                    ExitSlowMo();
                }
                else
                {
                    EnterSlowMo();
                }
            }
        }

        if (GInput.GetButtonUp(Cmd.Pause))
        {
            if (holdToPause)
            {
                ExitSlowMo();
            }
        }

        if (isInSlowMo)
        {
            if (GInput.GetButtonDown(Cmd.SpeedTime))
            {
                targetSpeed = Mathf.Clamp(targetSpeed + timeChangeStep, slowMoMin, slowMoMax);
                StopAllCoroutines();
                StartCoroutine(ChangeSpeed(changeSlowMoSmoothing));
            }

            if (GInput.GetButtonDown(Cmd.SlowTime))
            {
                targetSpeed = Mathf.Clamp(targetSpeed - timeChangeStep, slowMoMin, slowMoMax);
                StopAllCoroutines();
                StartCoroutine(ChangeSpeed(changeSlowMoSmoothing));
            }
        }
    }

    private void EnterSlowMo()
    {
        isInSlowMo = true;
        targetSpeed = slowMoSpeed;
        StopAllCoroutines();
        StartCoroutine(ChangeSpeed(enterSlowMoSmoothing));
    }

    private void ExitSlowMo()
    {
        isInSlowMo = false;
        targetSpeed = 1f;
        StopAllCoroutines();
        StartCoroutine(ChangeSpeed(exitSlowMoSmoothing));
    }

    private IEnumerator ChangeSpeed(float smoothing)
    {
        while (Mathf.Abs(targetSpeed - CurrentSpeed) > 0.005f)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, targetSpeed, smoothing);

            // change game speed
            Time.timeScale = CurrentSpeed;
            Time.fixedDeltaTime = CurrentSpeed * fixedDeltaRatio;

            yield return null;
        }

        CurrentSpeed = targetSpeed;
        Time.timeScale = CurrentSpeed;
        Time.fixedDeltaTime = targetSpeed * fixedDeltaRatio;
    }
}