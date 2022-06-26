using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class BW_Slider : MonoBehaviour
{
    [Tooltip("Percent of slider width on which value counter will jump either in right or left direction.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    float jumpPercent = 0.5f;
    [Tooltip("Left value display, it hides automatically when slider value is greater than the jump-percent.")]
    [SerializeField]
    Text leftReceiver;
    [Tooltip("Right value display, it hides automatically when slider value is less than the jump-percent.")]
    [SerializeField]
    Text rightReceiver;

    Slider slider;
    string Value { get => slider.value.ToString(); }

    void Awake()
    {
        slider = GetComponent<Slider>();
    }
    void Update()
    {
        leftReceiver.text = slider.value / slider.maxValue < jumpPercent ? string.Empty : Value;
        rightReceiver.text = slider.value / slider.maxValue >= jumpPercent ? string.Empty : Value;
    }
}
