using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;
using Unity.Collections;
using Unity.Jobs;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace ProceduralBase
{

    public class WorldCellManager : MonoBehaviour
    {
        #region Singleton
        private static WorldCellManager _instance;
        public static WorldCellManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldCellManager>();
                }
                return _instance;
            }
        }
        private void Awake()
        {
            // Make sure only one instance of WorldCellManager exists in the scene
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        #endregion


    }
}