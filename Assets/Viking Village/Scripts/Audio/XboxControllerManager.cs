using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Wymaga pakietu "Input System"

public class XboxControllerManager : MonoBehaviour
{
    public GameObject tenkokuGameObject;

    //public Tenkoku.Core.TenkokuModule tenkokuModule;

    [Header("1. Przełącznik (Right Bumper)")]
    [Tooltip("Wartość logiczna przełączana za pomocą prawego przycisku (RB)")]
    public bool isToggled = false;

    [Header("2. Zegar (Triggery)")]
    [Tooltip("Aktualny czas zegara")]
    public float currentClock = 0f;
    [Tooltip("Maksymalna prędkość przewijania (gdy trigger jest wciśnięty do końca)")]
    public float maxClockIncrement = 10f;

    public float incrementDelta;

    [Header("3. Wartości Zegara (Krzyżak Lewo/Prawo)")]
    [Tooltip("Lista wartości zegara. Możesz dodawać/usuwać elementy w Inspektorze.")]
    public List<float> clockPresets = new List<float> { 8f, 12f, 16f };
    private int currentClockPresetIndex = 0;

    [Header("4. Dowolne Wartości (Krzyżak Góra/Dół)")]
    [Tooltip("Lista 4 wartości (lub więcej). Możesz zmienić typ 'string' na 'int', 'GameObject' itp.")]
    public List<float> customValues = new List<float> { 0f, 0.3f, 0.7f, 1f };
    private int customValueIndex = 0;

    private float currentHour;

    void Update()
    {
        //currentHour = tenkokuGameObject.TenkokuModule.currentHour

        // Pobranie aktualnie podłączonego pada
        Gamepad gamepad = Gamepad.current;

        // Jeśli nie wykryto pada, przerywamy działanie w tej klatce
        if (gamepad == null) return;

        HandleBooleanToggle(gamepad);
        HandleClockRewind(gamepad);
        HandleClockPresets(gamepad);
        HandleCustomValues(gamepad);
    }

    private void HandleBooleanToggle(Gamepad gamepad)
    {
        // wasPressedThisFrame działa jak GetButtonDown (reaguje raz na wciśnięcie)
        if (gamepad.rightShoulder.wasPressedThisFrame)
        {
            isToggled = !isToggled;
            Debug.Log($"Przełącznik zmieniony: {isToggled}");
        }
    }

    private void HandleClockRewind(Gamepad gamepad)
    {
        // Odczytujemy siłę nacisku na triggery (wartości od 0.0 do 1.0)
        float leftTrigger = gamepad.leftTrigger.ReadValue();
        float rightTrigger = gamepad.rightTrigger.ReadValue();

        if (leftTrigger > 0 || rightTrigger > 0)
        {
            // Prawy trigger przesuwa do przodu (dodatni), lewy cofa (ujemny)
            // Zmiana jest proporcjonalna do siły nacisku i czasu klatki (Time.deltaTime)
            incrementDelta = (rightTrigger - leftTrigger) * maxClockIncrement * Time.deltaTime;
            currentClock += incrementDelta;

            if (currentClock > 24f) currentClock -= 24f;
            else if (currentClock < 0f) currentClock += 24f;

            // Opcjonalne: Zablokowanie zegara, aby nie spadał poniżej zera
            // currentClock = Mathf.Max(0, currentClock);
        }
    }

    private void HandleClockPresets(Gamepad gamepad)
    {
        if (clockPresets.Count == 0) return;

        if (gamepad.dpad.left.wasPressedThisFrame)
        {
            CycleClockPreset(-1);
        }
        else if (gamepad.dpad.right.wasPressedThisFrame)
        {
            CycleClockPreset(1);
        }
    }

    private void CycleClockPreset(int direction)
    {
        currentClockPresetIndex += direction;

        // Zapętlanie indeksu, jeśli wyjdzie poza zakres listy
        if (currentClockPresetIndex < 0)
            currentClockPresetIndex = clockPresets.Count - 1;
        else if (currentClockPresetIndex >= clockPresets.Count)
            currentClockPresetIndex = 0;

        currentClock = clockPresets[currentClockPresetIndex];
        Debug.Log($"Ustawiono zegar na preset: {currentClock}");
    }

    private void HandleCustomValues(Gamepad gamepad)
    {
        if (customValues.Count == 0) return;

        if (gamepad.dpad.up.wasPressedThisFrame)
        {
            CycleCustomValue(1);
        }
        else if (gamepad.dpad.down.wasPressedThisFrame)
        {
            CycleCustomValue(-1);
        }
    }

    private void CycleCustomValue(int direction)
    {
        customValueIndex += direction;

        // Zapętlanie indeksu
        if (customValueIndex < 0)
            customValueIndex = customValues.Count - 1;
        else if (customValueIndex >= customValues.Count)
            customValueIndex = 0;

        Debug.Log($"Zmieniono wartość na: {customValues[customValueIndex]}");
    }
}