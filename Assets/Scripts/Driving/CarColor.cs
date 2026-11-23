using System.Collections.Generic;
using UnityEngine;

public class CarColor : MonoBehaviour
{
    public enum ColorMode { Fixed, Random }

    [Tooltip("Názov materiálu karosérie.")]
    public string materialName = "Car_Color";

    [Tooltip("Fixed = konkrétna farba, Random = náhodná farba pri štarte.")]
    public ColorMode mode = ColorMode.Fixed;

    [Tooltip("Farba pri režime Fixed.")]
    public Color fixedColor = Color.red;

    [Header("Náhodná farba")]
    [Tooltip("Ak zoznam nie je prázdny, vyberie sa náhodná farba z neho. Ak je prázdny, farba je úplne náhodná.")]
    public List<Color> randomColors = new List<Color>();
    [Tooltip("Rozsah sýtosti pri úplne náhodnej farbe (0–1).")]
    public Vector2 saturationRange = new Vector2(0.6f, 1f);
    [Tooltip("Rozsah jasu pri úplne náhodnej farbe (0–1).")]
    public Vector2 brightnessRange = new Vector2(0.5f, 1f);

    // Aktuálna farba auta (napr. pre UI alebo minimapu).
    public Color CurrentColor { get; private set; }

    private readonly List<Material> bodyMaterials = new List<Material>();
    private readonly List<Material> createdInstances = new List<Material>();

    void Awake()
    {
        FindBodyMaterials();

        if (mode == ColorMode.Fixed)
            SetColor(fixedColor);
        else
            SetColor(PickRandomColor());
    }

    // Zmení farbu karosérie (dá sa volať aj z iného skriptu, napr. z menu).
    public void SetColor(Color color)
    {
        CurrentColor = color;
        foreach (Material material in bodyMaterials)
            material.color = color;   // URP: Base Color, Built-in: Color
    }

    // Nastaví novú náhodnú farbu.
    public void RandomizeColor()
    {
        SetColor(PickRandomColor());
    }

    private Color PickRandomColor()
    {
        if (randomColors.Count > 0)
            return randomColors[Random.Range(0, randomColors.Count)];

        return Random.ColorHSV(0f, 1f,
            saturationRange.x, saturationRange.y,
            brightnessRange.x, brightnessRange.y);
    }

    private void FindBodyMaterials()
    {
        bodyMaterials.Clear();

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            // Najprv overí, či renderer materiál karosérie vôbec má.
            bool hasBody = false;
            foreach (Material shared in renderer.sharedMaterials)
            {
                if (shared != null && shared.name == materialName) { hasBody = true; break; }
            }
            if (!hasBody) continue;

            // .materials vytvorí kópie materiálov len pre toto auto,
            // takže zmena farby neovplyvní ostatné autá.
            Material[] materials = renderer.materials;
            createdInstances.AddRange(materials);
            foreach (Material material in materials)
            {
                if (material.name.Replace(" (Instance)", "") == materialName)
                    bodyMaterials.Add(material);
            }
        }

        if (bodyMaterials.Count == 0)
            Debug.LogWarning($"CarColor: na aute '{gameObject.name}' sa nenašiel materiál '{materialName}'.", this);
    }

    void OnDestroy()
    {
        // Upratanie kópií materiálov
        foreach (Material material in createdInstances)
        {
            if (material != null) Destroy(material);
        }
    }
}