﻿using Dman.Tiling;
using Dman.Tiling.SquareCoords;
using UnityEngine;

namespace Assets.Scripts.GreenhouseLoader
{
    [CreateAssetMenu(fileName = "MemberSpawner", menuName = "Greenhouse/MemberSpawner", order = 3)]
    public class MemberSpawner : ScriptableObject
    {
        public RectCoordinateRange spawningSize;
        public TileMember thingToSpawn;
        public RangePositioner inRange;

        public void SpawnMembers(Transform parent)
        {
            foreach (var coordinate in spawningSize)
            {
                var newThing = Instantiate(thingToSpawn, parent);
                newThing.SetPosition(UniversalCoordinate.From(coordinate, (short)inRange.myId));
                foreach (var spawnable in newThing.GetComponentsInChildren<ISpawnable>())
                {
                    spawnable.SetupAfterSpawn();
                }
            }
        }
    }
}
