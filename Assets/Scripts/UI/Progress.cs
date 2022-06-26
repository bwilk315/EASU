using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class Progress : MonoBehaviour
{
    [Tooltip("Percent of slider width on which value counter will jump either in right or left direction.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    float jumpPercent = 0.5f;
    [Tooltip("Should the output be rounded?")]
    [SerializeField]
    bool round = false;
    [Tooltip("Left value display, it hides automatically when slider value is greater than the jump-percent.")]
    [SerializeField]
    TMP_Text leftReceiver;
    [Tooltip("Right value display, it hides automatically when slider value is less than the jump-percent.")]
    [SerializeField]
    TMP_Text rightReceiver;

    Slider slider;
    public float Percent => slider.value / slider.maxValue;
    public string PercentText => $"{Percent * 100.0f}%";

    void Awake()
    {
        slider = GetComponent<Slider>();
    }
    void Update()
    {
        leftReceiver.text = Percent < jumpPercent ? string.Empty : PercentText;
        rightReceiver.text = Percent >= jumpPercent ? string.Empty : PercentText;
    }
    public void SetValue(float value) => slider.value = value;
    public void ChangeValue(float change) => SetValue(slider.value + change);
    public void SetPercent(float fraction) => slider.value = slider.maxValue * Mathf.Clamp01(fraction);
    public void ChangePercent(float fraction) => SetPercent(Percent + fraction);
}
