﻿using UnityEngine;

namespace Assets.Scripts.Utilities.SaveSystem.Components
{
    [CreateAssetMenu(fileName = "SaveablePrefabType", menuName = "Saving/SaveablePrefabType", order = 1)]
    public class SaveablePrefabType : IDableObject
    {
        public SaveablePrefab prefab;
        public int prefabID;
        public override void AssignId(int myNewID)
        {
            prefabID = myNewID;
        }
    }
}