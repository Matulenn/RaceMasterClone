using System;
using UnityEngine;

public class RaceCatchUp : MonoBehaviour
{
    [Serializable]
    private class RacerState
    {
        public RaceProgress racer;

        public float distanceFromGroup;
        public float speedMultiplier = 1f;

        [NonSerialized] public float totalDistance;
        [NonSerialized] public float previousArc;
        [NonSerialized] public int segmentIndex = -1;
    }

    [Header("References")]
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private AIPath path;

    [Tooltip("Priraď iba AI súperov, nie hráča.")]
    [SerializeField] private RaceProgress[] aiRacers;

    [Header("Distance Along Track")]
    [SerializeField, Min(0f)]
    private float assistanceStartGap = 15f;

    [SerializeField, Min(0.1f)]
    private float fullAssistanceGap = 40f;

    [Header("Speed Limits")]
    [Tooltip("0.20 = maximálne +20 % rýchlosti.")]
    [SerializeField, Range(0f, 1f)]
    private float maximumBoost = 0.20f;

    [Tooltip("0.10 = maximálne -10 % rýchlosti.")]
    [SerializeField, Range(0f, 0.5f)]
    private float maximumSlowdown = 0.10f;

    [SerializeField, Min(0.01f)]
    private float multiplierChangeSpeed = 0.2f;

    [Header("Tracking")]
    [Tooltip("Po inicializácii hľadá blízke úseky okolo posledného.")]
    [SerializeField, Min(1)]
    private int segmentSearchRadius = 12;

    [Header("Runtime Status")]
    [SerializeField] private RacerState[] states;

    private Vector3[] trackPoints;
    private float[] segmentLengths;
    private float[] cumulativeDistances;

    private float trackLength;
    private bool trackingStarted;

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        BuildTrack();

        if (trackLength < 0.01f)
        {
            Debug.LogError(
                "RaceCatchUp: Trasa nemá platnú dĺžku.",
                this
            );

            enabled = false;
            return;
        }

        states = new RacerState[aiRacers.Length];

        for (int i = 0; i < aiRacers.Length; i++)
        {
            states[i] = new RacerState
            {
                racer = aiRacers[i]
            };
        }
    }

    private bool ValidateReferences()
    {
        if (raceManager == null ||
            path == null ||
            path.PointCount < 2 ||
            aiRacers == null ||
            aiRacers.Length < 2)
        {
            Debug.LogError(
                "RaceCatchUp: Priraď RaceManager, AIPath " +
                "a aspoň dvoch AI súperov.",
                this
            );

            return false;
        }

        for (int i = 0; i < aiRacers.Length; i++)
        {
            if (aiRacers[i] == null ||
                aiRacers[i].GetComponent<AIPathFollower>() == null)
            {
                Debug.LogError(
                    "RaceCatchUp: Každá položka musí byť " +
                    "AI súper s RaceProgress a AIPathFollower.",
                    this
                );

                return false;
            }

            for (int j = 0; j < i; j++)
            {
                if (aiRacers[i] == aiRacers[j])
                {
                    Debug.LogError(
                        "RaceCatchUp: Duplicitný AI súper.",
                        this
                    );

                    return false;
                }
            }
        }

        return true;
    }

    private void BuildTrack()
    {
        int count = path.PointCount;

        trackPoints = new Vector3[count];
        segmentLengths = new float[count];
        cumulativeDistances = new float[count];

        for (int i = 0; i < count; i++)
        {
            trackPoints[i] = path.GetPoint(i);
            trackPoints[i].y = 0f;
        }

        trackLength = 0f;

        for (int i = 0; i < count; i++)
        {
            cumulativeDistances[i] = trackLength;

            int next = (i + 1) % count;

            segmentLengths[i] = Vector3.Distance(
                trackPoints[i],
                trackPoints[next]
            );

            trackLength += segmentLengths[i];
        }
    }

    private void LateUpdate()
    {
        if (!raceManager.RaceStarted)
            return;

        if (!trackingStarted)
        {
            InitializeTracking();
            trackingStarted = true;
        }

        float groupDistance = 0f;
        int activeCount = 0;

        foreach (RacerState state in states)
        {
            if (state.racer.Finished)
                continue;

            float arc = FindArcPosition(state, false);

            state.totalDistance += WrappedDifference(
                arc - state.previousArc
            );

            state.previousArc = arc;

            groupDistance += state.totalDistance;
            activeCount++;
        }

        if (activeCount > 0)
        {
            groupDistance /= activeCount;
        }

        foreach (RacerState state in states)
        {
            float targetMultiplier = 1f;

            if (!state.racer.Finished && activeCount > 1)
            {
                // Kladné = za skupinou, záporné = pred skupinou.
                state.distanceFromGroup =
                    groupDistance - state.totalDistance;

                float strength = Mathf.InverseLerp(
                    assistanceStartGap,
                    Mathf.Max(
                        assistanceStartGap + 0.01f,
                        fullAssistanceGap
                    ),
                    Mathf.Abs(state.distanceFromGroup)
                );

                targetMultiplier =
                    state.distanceFromGroup > 0f
                        ? 1f + maximumBoost * strength
                        : 1f - maximumSlowdown * strength;
            }
            else
            {
                state.distanceFromGroup = 0f;
            }

            state.speedMultiplier = Mathf.MoveTowards(
                state.speedMultiplier,
                targetMultiplier,
                multiplierChangeSpeed * Time.deltaTime
            );
        }
    }

    private void InitializeTracking()
    {
        float referenceArc = 0f;

        for (int i = 0; i < states.Length; i++)
        {
            float arc = FindArcPosition(states[i], true);

            if (i == 0)
                referenceArc = arc;

            states[i].previousArc = arc;

            states[i].totalDistance = WrappedDifference(
                arc - referenceArc
            );
        }
    }

    private float FindArcPosition(
        RacerState state,
        bool searchWholeTrack)
    {
        Vector3 position = state.racer.transform.position;
        position.y = 0f;

        int count = trackPoints.Length;

        bool fullSearch =
            searchWholeTrack ||
            state.segmentIndex < 0 ||
            segmentSearchRadius * 2 + 1 >= count;

        int iterations = fullSearch
            ? count
            : segmentSearchRadius * 2 + 1;

        int startIndex = fullSearch
            ? 0
            : state.segmentIndex - segmentSearchRadius;

        float closestDistance = float.PositiveInfinity;
        float bestArc = state.previousArc;
        int bestSegment = state.segmentIndex;

        for (int i = 0; i < iterations; i++)
        {
            int index = ((startIndex + i) % count + count) % count;
            int next = (index + 1) % count;

            Vector3 segment =
                trackPoints[next] - trackPoints[index];

            if (segment.sqrMagnitude < 0.0001f)
                continue;

            float t = Mathf.Clamp01(
                Vector3.Dot(
                    position - trackPoints[index],
                    segment
                ) / segment.sqrMagnitude
            );

            Vector3 projected =
                trackPoints[index] + segment * t;

            float distance =
                (position - projected).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestSegment = index;

                bestArc =
                    cumulativeDistances[index]
                    + segmentLengths[index] * t;
            }
        }

        state.segmentIndex = bestSegment;
        return bestArc;
    }

    private float WrappedDifference(float difference)
    {
        return Mathf.Repeat(
            difference + trackLength * 0.5f,
            trackLength
        ) - trackLength * 0.5f;
    }

    public float GetMultiplier(RaceProgress racer)
    {
        if (!isActiveAndEnabled ||
            !trackingStarted ||
            states == null)
        {
            return 1f;
        }

        foreach (RacerState state in states)
        {
            if (state.racer == racer)
                return state.speedMultiplier;
        }

        return 1f;
    }
}