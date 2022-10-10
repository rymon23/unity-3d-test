using System;
using Hybrid.Components;
using UnityEngine;

public class TerritoryManagerEvents : MonoBehaviour
{
    public event Action onEvaluateNeighbors;

    public void EvaluateNeighbors() => onEvaluateNeighbors?.Invoke();
}
