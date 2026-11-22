using System;
using System.Collections.Generic;
using UnityEngine;

public class AIPath : MonoBehaviour
{
    [SerializeField] private Transform searchRoot;

    [SerializeField] private Transform[] points =
        new Transform[0];

    public int PointCount => points.Length;

    public Vector3 GetPoint(int index)
    {
        return points[index].position;
    }

    private void Awake()
    {
        LoadPoints();
    }

    [ContextMenu("Load Path Points")]
    public void LoadPoints()
    {
        if (searchRoot == null)
        {
            points = new Transform[0];

            Debug.LogWarning(
                "AIPath: Priraď Search Root s modelom trate.",
                this
            );

            return;
        }

        Transform[] allObjects =
            searchRoot.GetComponentsInChildren<Transform>(true);

        List<Transform> found = new List<Transform>();

        foreach (Transform item in allObjects)
        {
            if (GetPointNumber(item.name) >= 0)
            {
                found.Add(item);
            }
        }

        found.Sort((a, b) =>
        {
            return GetPointNumber(a.name)
                .CompareTo(GetPointNumber(b.name));
        });

        points = found.ToArray();

        Debug.Log(
            $"AIPath: Načítaných {points.Length} bodov.",
            this
        );
    }

    private static int GetPointNumber(string objectName)
    {
        const string prefix = "PathPoint_";

        if (!objectName.StartsWith(
            prefix,
            StringComparison.Ordinal))
        {
            return -1;
        }

        string numberText =
            objectName.Substring(prefix.Length);

        if (int.TryParse(numberText, out int number)
            && number >= 0)
        {
            return number;
        }

        return -1;
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Length < 2)
            return;

        for (int i = 0; i < points.Length; i++)
        {
            Transform current = points[i];
            Transform next =
                points[(i + 1) % points.Length];

            if (current == null || next == null)
                continue;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(current.position, 0.15f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                current.position,
                next.position
            );
        }
    }
}