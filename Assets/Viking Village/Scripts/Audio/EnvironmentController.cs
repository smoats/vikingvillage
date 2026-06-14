using System;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentController : MonoBehaviour
{
    public enum DayPeriod { Morning, Noon, EveningNight }

    public Tenkoku.Core.TenkokuModule tenkokuModule;

    [Header("Główne Zmienne Środowiskowe")]
    [Range(0f, 24f)] private float timeOfDay = 12f;
    [Range(0f, 1f)] private float rainIntensity = 0f;

    [Header("Konfiguracja Okresów Dnia")]
    [Range(0f, 24f)] public float morningStart = 6f;
    [Range(0f, 24f)] public float noonStart = 14f;
    [Range(0f, 24f)] public float eveningStart = 22f;

    [Header("Zasady Środowiskowe dla Obiektów")]
    [Tooltip("Tutaj konfigurujesz zachowanie każdego obiektu w zależności od czasu i deszczu")]
    public List<EnvironmentRule> environmentRules = new List<EnvironmentRule>();

    private DayPeriod currentPeriod;

    void Update()
    {
        UpdateDayPeriod();
        EvaluateEnvironmentRules();
        UpdateTime();
    }

    private void UpdateTime()
    {
        timeOfDay = tenkokuModule.currentTimeFMOD;
    }

    private void UpdateDayPeriod()
    {
        if (timeOfDay >= morningStart && timeOfDay < noonStart)
        {
            currentPeriod = DayPeriod.Morning;
        }
        else if (timeOfDay >= noonStart && timeOfDay < eveningStart)
        {
            currentPeriod = DayPeriod.Noon;
        }
        else
        {
            currentPeriod = DayPeriod.EveningNight;
        }
    }

    private void EvaluateEnvironmentRules()
    {
        foreach (var rule in environmentRules)
        {
            if (rule.targetObject == null) continue;

            // 1. USTALENIE STANU BAZOWEGO (na podstawie pory dnia)
            bool shouldBeActive = false;
            switch (currentPeriod)
            {
                case DayPeriod.Morning: shouldBeActive = rule.activeInMorning; break;
                case DayPeriod.Noon: shouldBeActive = rule.activeInNoon; break;
                case DayPeriod.EveningNight: shouldBeActive = rule.activeInEveningNight; break;
            }

            // 2. NADPISANIE PRZEZ DESZCZ (Hierarchia wyżej)
            // Sprawdzamy, czy próg deszczu został przekroczony
            bool isRainThresholdMet = rainIntensity >= rule.minRainThreshold;

            if (isRainThresholdMet)
            {
                // Jeśli deszcz osiągnął próg, sprawdzamy co ma zrobić z obiektem
                switch (rule.rainOverride)
                {
                    case EnvironmentRule.RainOverrideType.ForceDisable:
                        // Deszcz gasi/wyłącza obiekt bez względu na to, czy to dzień czy noc
                        shouldBeActive = false;
                        break;

                    case EnvironmentRule.RainOverrideType.ForceEnable:
                        // Deszcz włącza obiekt (np. kałuże, dym z komina), ignorując porę dnia
                        shouldBeActive = true;
                        break;

                    case EnvironmentRule.RainOverrideType.DoNothing:
                        // Deszcz nie ma wpływu, zostaje stan z pory dnia
                        break;
                }
            }

            // 3. ZASTOSOWANIE STANU (Z optymalizacją)
            if (rule.targetObject.activeSelf != shouldBeActive)
            {
                rule.targetObject.SetActive(shouldBeActive);
            }
        }
    }
}

// --- JEDNA, SKONSOLIDOWANA KLASA DLA EDYTORA ---

[Serializable]
public class EnvironmentRule
{
    public enum RainOverrideType { DoNothing, ForceDisable, ForceEnable }

    [Tooltip("Nazwa identyfikacyjna w edytorze")]
    public string note = "Nazwa zasady";
    public GameObject targetObject;

    [Header("1. Konfiguracja Pór Dnia")]
    public bool activeInMorning = true;
    public bool activeInNoon = true;
    public bool activeInEveningNight = true;

    [Header("2. Konfiguracja Wpływu Deszczu (Wyższy Priorytet)")]
    [Tooltip("Od jakiej intensywności deszczu ta zasada ma zacząć obowiązywać?")]
    [Range(0f, 1f)] public float minRainThreshold = 0.1f;

    [Tooltip("Co deszcz ma zrobić z obiektem, gdy przekroczy próg?\n" +
             "• Do Nothing - deszcz ignoruje obiekt (decyduje czas)\n" +
             "• Force Disable - deszcz ZAWSZE wyłącza obiekt (np. ognisko gasi deszcz)\n" +
             "• Force Enable - deszcz ZAWSZE włącza obiekt (np. dźwięk deszczu uderzającego o dach)")]
    public RainOverrideType rainOverride = EnvironmentRule.RainOverrideType.DoNothing;
}