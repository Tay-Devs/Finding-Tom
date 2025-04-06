using UnityEngine;

[ExecuteAlways]
public class PaperFoldTimeSetter : MonoBehaviour
{
    [SerializeField] private Material material;

    private void Update()
    {
        if (material != null)
        {
            material.SetFloat("_UnscaledTime", Time.unscaledTime);
        }
    }
}