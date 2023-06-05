using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBase
{
    public interface IWorldOccupant
    {
        public string GetWorldAreaUId();
        public void SetWorldAreaUId(string worldAreaUid);
    }
}