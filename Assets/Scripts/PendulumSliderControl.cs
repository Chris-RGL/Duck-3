using UnityEngine;
using UnityEngine.UI;

public class PendulumSliderControl : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The UI Slider that drives cart position")]
    public Slider slider;
    [Tooltip("Kinematic Rigidbody on the Cart GameObject")]
    public Rigidbody cartRigidbody;

    [Header("Range")]
    [Tooltip("World-unit range mapped to slider min/max (symmetric around 0)")]
    public float positionLimit = 10f;

    void Start()
    {
        if (slider == null)
        {
            Debug.LogError("PendulumSliderControl: no Slider assigned.", this);
            return;
        }

        slider.minValue = -positionLimit;
        slider.maxValue =  positionLimit;
        slider.value    =  0f;
    }

    void FixedUpdate()
    {
        if (cartRigidbody == null || slider == null) return;

        float targetX = Mathf.Clamp(slider.value, -positionLimit, positionLimit);
        Vector3 pos = cartRigidbody.position;
        pos.x = targetX;
        cartRigidbody.MovePosition(pos);
    }
}
