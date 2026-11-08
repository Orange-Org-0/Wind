using UnityEngine;

public class VegetationController : MonoBehaviour
{
    [HideInInspector] public SpringValue BendValue;
    public float BendFrequency = 1.0f;
    public float BendDampingRatio = 0.5f;
    public Material mat;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BendValue = new SpringValue(BendFrequency, BendDampingRatio);
        mat = GetComponent<Material>();
    }

    // Update is called once per frame
    void Update()
    {
        if (BendValue != null)
        {
            BendValue.UpdateValue(Time.deltaTime);

            if (mat != null)
            {
                mat.SetFloat("_BendStrength", BendValue.value);
            }
        }
        
    }

    private void OnValidate()
    {
        if (BendValue != null)
        {
            BendValue.UpdateParams(BendFrequency, BendDampingRatio);
        }
    }
}
