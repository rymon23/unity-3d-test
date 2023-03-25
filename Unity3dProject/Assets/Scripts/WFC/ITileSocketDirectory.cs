using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WFCSystem
{
    public interface IHexagonTileSocketSystem
    {
        // bool[,] GetCompatibilityMatrix();
    }

    public interface ITileSocketDirectory
    {
        public enum ResetCompatibilityState { Unset = 0, None, All }
        bool[,] GetCompatibilityMatrix();
        public List<SocketCompatibility> GetCompatibilityTable();
        // (string name, Color color)[] GetSockets();
    }
}