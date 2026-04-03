using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(Light2D))]
public class LightFX2D : MonoBehaviour
{
    public enum LightMode { normal, pulse, flicker, shortCircuit }

    [Header("settings")]
    public LightMode currentMode = LightMode.flicker;

    [Header("pulse settings")]
    [SerializeField] private float pulseMin = 0.5f;
    [SerializeField] private float pulseMax = 1.2f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("flicker settings")]
    [SerializeField] private float flickerMin = 0.2f;
    [SerializeField] private float flickerMax = 1f;
    [SerializeField] private float flickerSpeed = 10f;

    [Header("short circuit settings")]
    [SerializeField] private float offDurationMin = 0.5f;
    [SerializeField] private float offDurationMax = 3f;
    [SerializeField] private float sparkIntensity = 2f;

    private Light2D targetLight;
    private float originalIntensity;
    private float randomNoiseOffset;

    void Awake()
    {
        targetLight = GetComponent<Light2D>();
        originalIntensity = targetLight.intensity;
        randomNoiseOffset = Random.Range(0f, 100f);
    }

    void Start()
    {
        ApplyMode(currentMode);
    }

    void Update()
    {
        if (currentMode == LightMode.flicker)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, randomNoiseOffset);
            targetLight.intensity = Mathf.Lerp(flickerMin, flickerMax, noise);
        }
    }

    public void ChangeMode(LightMode newMode)
    {
        currentMode = newMode;
        ApplyMode(newMode);
    }

    private void ApplyMode(LightMode mode)
    {
        // dung DOKill(targetLight) de xoa cac tween cu tren den nay
        DOTween.Kill(targetLight);
        StopAllCoroutines();

        switch (mode)
        {
            case LightMode.normal:
                targetLight.intensity = originalIntensity;
                break;

            case LightMode.pulse:
                targetLight.intensity = pulseMin;
                // dung DOTween.To thay cho DOIntensity de ho tro Light2D
                DOTween.To(() => targetLight.intensity, x => targetLight.intensity = x, pulseMax, pulseSpeed / 2f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetTarget(targetLight); // khoa muc tieu de de quan ly
                break;

            case LightMode.shortCircuit:
                StartCoroutine(ShortCircuitRoutine());
                break;
        }
    }

    private IEnumerator ShortCircuitRoutine()
    {
        while (true)
        {
            targetLight.intensity = 0f;
            float offTime = Random.Range(offDurationMin, offDurationMax);
            yield return new WaitForSeconds(offTime);

            int sparkCount = Random.Range(2, 5);
            for (int i = 0; i < sparkCount; i++)
            {
                targetLight.intensity = sparkIntensity;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
                targetLight.intensity = 0f;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
        }
    }
}