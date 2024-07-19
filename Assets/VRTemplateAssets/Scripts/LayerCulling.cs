using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceBasedTransparency : MonoBehaviour
{
    private Transform playerCamera; // Player's camera transform
    public float maxDistance = 10.0f; // Distance at which the object is fully transparent
    public float minDistance = 2.0f; // Distance at which the object is fully opaque
    private Renderer objectRenderer;
    private Material objectMaterial;
    private Color originalColor;

    void Start()
    {
        // Find the main camera
        playerCamera = Camera.main.transform;
        if (playerCamera == null)
        {
            Debug.LogError("Main camera not found.");
            return;
        }

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError("Renderer not found on the object.");
            return;
        }

        objectMaterial = objectRenderer.material;
        if (objectMaterial == null)
        {
            Debug.LogError("Material not found on the object's renderer.");
            return;
        }

        if (objectMaterial.shader.name != "Standard")
        {
            Debug.LogWarning("Material shader is not Standard. Ensure it supports transparency.");
        }

        originalColor = objectMaterial.color;
        if (originalColor.a == 0)
        {
            Debug.LogWarning("Material's original alpha is zero, object will be invisible initially.");
        }

        objectMaterial.SetOverrideTag("RenderType", "Transparent");
        objectMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        objectMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        objectMaterial.SetInt("_ZWrite", 0);
        objectMaterial.DisableKeyword("_ALPHATEST_ON");
        objectMaterial.EnableKeyword("_ALPHABLEND_ON");
        objectMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        objectMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void Update()
    {
        if (playerCamera == null) return;

        float distance = Vector3.Distance(playerCamera.position, transform.position);
        float alpha = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
        Color newColor = originalColor;
        newColor.a = alpha;
        objectMaterial.color = newColor;
    }
}
