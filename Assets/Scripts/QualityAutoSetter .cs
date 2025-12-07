using UnityEngine;

public class QualityAutoSetter : MonoBehaviour
{
    public int qualityIndex = 0; // 0 = самый низкий из списка Quality Settings

    void Start()
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);
    }
}
