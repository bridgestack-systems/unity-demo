using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusArena.DataVisualization
{
    public class RuntimeGraphRenderer : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private RawImage targetImage;
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 256;
        [SerializeField] private Color backgroundColor = new(0.1f, 0.1f, 0.12f, 1f);
        [SerializeField] private Color gridColor = new(0.2f, 0.2f, 0.25f, 1f);
        [SerializeField] private int lineThickness = 2;

        [Header("Data")]
        [SerializeField] private int maxDataPoints = 100;
        [SerializeField] private float updateInterval = 0.1f;

        [Header("Labels")]
        [SerializeField] private TMP_Text yMinLabel;
        [SerializeField] private TMP_Text yMaxLabel;
        [SerializeField] private TMP_Text titleLabel;

        [Header("Auto-Subscribe")]
        [SerializeField] private string[] trackedStats;

        private Texture2D texture;
        private Color[] clearPixels;
        private readonly List<DataSeries> dataSeries = new();
        private float lastUpdateTime;

        private const int MaxSeries = 4;
        private static readonly Color[] defaultColors = {
            new(0.3f, 0.8f, 1f), new(1f, 0.5f, 0.3f),
            new(0.5f, 1f, 0.5f), new(1f, 0.8f, 0.3f)
        };

        private void Awake()
        {
            texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            clearPixels = new Color[textureWidth * textureHeight];
            Array.Fill(clearPixels, backgroundColor);

            if (targetImage != null)
                targetImage.texture = texture;
        }

        private void OnEnable()
        {
            if (StatsTracker.Instance != null && trackedStats is { Length: > 0 })
            {
                StatsTracker.Instance.OnStatUpdated += HandleStatUpdated;
                for (int i = 0; i < trackedStats.Length && i < MaxSeries; i++)
                {
                    Color color = defaultColors[i % defaultColors.Length];
                    SetData(trackedStats[i], new List<float>(), color);
                }
            }
        }

        private void OnDisable()
        {
            if (StatsTracker.Instance != null)
                StatsTracker.Instance.OnStatUpdated -= HandleStatUpdated;
        }

        private void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;
            Render();
        }

        private void HandleStatUpdated(string statName, float value)
        {
            var series = dataSeries.Find(s => s.Label == statName);
            if (series == null) return;

            series.Values.Add(value);
            while (series.Values.Count > maxDataPoints)
                series.Values.RemoveAt(0);
        }

        public void SetData(string label, List<float> values, Color color)
        {
            var existing = dataSeries.Find(s => s.Label == label);
            if (existing != null)
            {
                existing.Values = values ?? new List<float>();
                existing.Color = color;
                return;
            }

            if (dataSeries.Count >= MaxSeries) return;

            dataSeries.Add(new DataSeries
            {
                Label = label,
                Values = values ?? new List<float>(),
                Color = color
            });
        }

        public void RemoveSeries(string label)
        {
            dataSeries.RemoveAll(s => s.Label == label);
        }

        public void ClearAll()
        {
            dataSeries.Clear();
        }

        private void Render()
        {
            texture.SetPixels(clearPixels);

            ComputeYRange(out float yMin, out float yMax);
            DrawGrid(yMin, yMax);

            foreach (var series in dataSeries)
            {
                if (series.Values.Count < 2) continue;
                DrawSeries(series, yMin, yMax);
            }

            texture.Apply();
            UpdateLabels(yMin, yMax);
        }

        private void ComputeYRange(out float yMin, out float yMax)
        {
            yMin = float.MaxValue;
            yMax = float.MinValue;

            foreach (var series in dataSeries)
            {
                foreach (float v in series.Values)
                {
                    if (v < yMin) yMin = v;
                    if (v > yMax) yMax = v;
                }
            }

            if (yMin >= yMax)
            {
                yMin = 0f;
                yMax = 1f;
            }

            float padding = (yMax - yMin) * 0.1f;
            yMin -= padding;
            yMax += padding;
        }

        private void DrawGrid(float yMin, float yMax)
        {
            const int horizontalLines = 4;
            const int verticalLines = 5;

            for (int i = 0; i <= horizontalLines; i++)
            {
                int y = (int)((float)i / horizontalLines * (textureHeight - 1));
                DrawHorizontalLine(y, gridColor);
            }

            for (int i = 0; i <= verticalLines; i++)
            {
                int x = (int)((float)i / verticalLines * (textureWidth - 1));
                DrawVerticalLine(x, gridColor);
            }
        }

        private void DrawSeries(DataSeries series, float yMin, float yMax)
        {
            int count = series.Values.Count;
            int startIdx = Mathf.Max(0, count - maxDataPoints);
            int pointCount = count - startIdx;
            if (pointCount < 2) return;

            float yRange = yMax - yMin;

            for (int i = 1; i < pointCount; i++)
            {
                float x0 = (float)(i - 1) / (maxDataPoints - 1) * (textureWidth - 1);
                float x1 = (float)i / (maxDataPoints - 1) * (textureWidth - 1);
                float y0 = (series.Values[startIdx + i - 1] - yMin) / yRange * (textureHeight - 1);
                float y1 = (series.Values[startIdx + i] - yMin) / yRange * (textureHeight - 1);

                DrawLineBresenham(
                    Mathf.RoundToInt(x0), Mathf.RoundToInt(y0),
                    Mathf.RoundToInt(x1), Mathf.RoundToInt(y1),
                    series.Color
                );
            }
        }

        private void DrawLineBresenham(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetThickPixel(x0, y0, color);

                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void SetThickPixel(int cx, int cy, Color color)
        {
            int half = lineThickness / 2;
            for (int dx = -half; dx <= half; dx++)
            {
                for (int dy = -half; dy <= half; dy++)
                {
                    int px = cx + dx;
                    int py = cy + dy;
                    if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                        texture.SetPixel(px, py, color);
                }
            }
        }

        private void DrawHorizontalLine(int y, Color color)
        {
            if (y < 0 || y >= textureHeight) return;
            for (int x = 0; x < textureWidth; x++)
                texture.SetPixel(x, y, color);
        }

        private void DrawVerticalLine(int x, Color color)
        {
            if (x < 0 || x >= textureWidth) return;
            for (int y = 0; y < textureHeight; y++)
                texture.SetPixel(x, y, color);
        }

        private void UpdateLabels(float yMin, float yMax)
        {
            if (yMinLabel != null) yMinLabel.text = yMin.ToString("F1");
            if (yMaxLabel != null) yMaxLabel.text = yMax.ToString("F1");
        }

        public void SetTitle(string title)
        {
            if (titleLabel != null) titleLabel.text = title;
        }

        private void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }

        private class DataSeries
        {
            public string Label;
            public List<float> Values;
            public Color Color;
        }
    }
}
