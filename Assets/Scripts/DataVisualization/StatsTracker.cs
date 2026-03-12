using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NexusArena.DataVisualization
{
    public class StatsTracker : MonoBehaviour
    {
        public static StatsTracker Instance { get; private set; }

        public event Action<string, float> OnStatUpdated;

        private readonly Dictionary<string, float> stats = new();
        private readonly Dictionary<string, List<TimestampedValue>> timeSeries = new();

        private float sessionStartTime;
        private int frameCount;
        private float fpsAccumulator;
        private float fpsUpdateInterval = 0.5f;
        private float fpsNextUpdate;

        private const string StatScore = "score";
        private const string StatKills = "kills";
        private const string StatDeaths = "deaths";
        private const string StatObjectsDestroyed = "objects_destroyed";
        private const string StatDistanceTraveled = "distance_traveled";
        private const string StatPlayTime = "play_time";
        private const string StatFPS = "fps";

        private Vector3 lastPosition;
        private Transform trackedTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetStats();
        }

        private void Update()
        {
            UpdatePlayTime();
            UpdateFPS();
            UpdateDistanceTraveled();
        }

        private void UpdatePlayTime()
        {
            float playTime = Time.time - sessionStartTime;
            stats[StatPlayTime] = playTime;
        }

        private void UpdateFPS()
        {
            frameCount++;
            fpsAccumulator += Time.unscaledDeltaTime;

            if (Time.unscaledTime >= fpsNextUpdate)
            {
                float fps = frameCount / fpsAccumulator;
                RecordEvent(StatFPS, fps);
                frameCount = 0;
                fpsAccumulator = 0f;
                fpsNextUpdate = Time.unscaledTime + fpsUpdateInterval;
            }
        }

        private void UpdateDistanceTraveled()
        {
            if (trackedTransform == null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    trackedTransform = cam.transform;
                    lastPosition = trackedTransform.position;
                }
                return;
            }

            Vector3 currentPos = trackedTransform.position;
            float delta = Vector3.Distance(currentPos, lastPosition);
            if (delta > 0.001f && delta < 10f) // ignore teleports
            {
                stats.TryGetValue(StatDistanceTraveled, out float current);
                stats[StatDistanceTraveled] = current + delta;
            }
            lastPosition = currentPos;
        }

        public void RecordEvent(string eventName, float value)
        {
            stats[eventName] = value;

            if (!timeSeries.TryGetValue(eventName, out var series))
            {
                series = new List<TimestampedValue>();
                timeSeries[eventName] = series;
            }
            series.Add(new TimestampedValue(Time.time, value));

            OnStatUpdated?.Invoke(eventName, value);
        }

        public void IncrementStat(string statName, float amount = 1f)
        {
            stats.TryGetValue(statName, out float current);
            float newValue = current + amount;
            RecordEvent(statName, newValue);
        }

        public float GetStat(string name)
        {
            stats.TryGetValue(name, out float value);
            return value;
        }

        public List<TimestampedValue> GetTimeSeries(string name)
        {
            return timeSeries.TryGetValue(name, out var series)
                ? new List<TimestampedValue>(series)
                : new List<TimestampedValue>();
        }

        public SessionSummary GetSessionSummary()
        {
            return new SessionSummary
            {
                score = GetStat(StatScore),
                kills = (int)GetStat(StatKills),
                deaths = (int)GetStat(StatDeaths),
                objectsDestroyed = (int)GetStat(StatObjectsDestroyed),
                distanceTraveled = GetStat(StatDistanceTraveled),
                playTime = GetStat(StatPlayTime),
                averageFPS = timeSeries.TryGetValue(StatFPS, out var fpsSeries) && fpsSeries.Count > 0
                    ? fpsSeries.Average(v => v.Value)
                    : 0f,
                allStats = new Dictionary<string, float>(stats)
            };
        }

        public void ResetStats()
        {
            stats.Clear();
            timeSeries.Clear();
            sessionStartTime = Time.time;
            fpsNextUpdate = Time.unscaledTime + fpsUpdateInterval;
            frameCount = 0;
            fpsAccumulator = 0f;

            stats[StatScore] = 0f;
            stats[StatKills] = 0f;
            stats[StatDeaths] = 0f;
            stats[StatObjectsDestroyed] = 0f;
            stats[StatDistanceTraveled] = 0f;
            stats[StatPlayTime] = 0f;
        }

        public void SetTrackedTransform(Transform target)
        {
            trackedTransform = target;
            if (target != null)
                lastPosition = target.position;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    [Serializable]
    public struct TimestampedValue
    {
        public float Time;
        public float Value;

        public TimestampedValue(float time, float value)
        {
            Time = time;
            Value = value;
        }
    }

    [Serializable]
    public class SessionSummary
    {
        public float score;
        public int kills;
        public int deaths;
        public int objectsDestroyed;
        public float distanceTraveled;
        public float playTime;
        public float averageFPS;
        public Dictionary<string, float> allStats;

        public float KDRatio => deaths > 0 ? (float)kills / deaths : kills;

        public override string ToString()
        {
            return $"Score: {score}, K/D: {kills}/{deaths} ({KDRatio:F1}), " +
                   $"Time: {playTime:F0}s, Distance: {distanceTraveled:F1}m, Avg FPS: {averageFPS:F0}";
        }
    }
}
