﻿using Assets.Scripts.DataModels;
using Assets.Scripts.Utilities;
using Genetics;
using UnityEngine;

namespace Assets.Scripts.Plants
{
    [CreateAssetMenu(fileName = "PlantType", menuName = "Greenhouse/PlantType", order = 1)]
    public class PlantType : IDableObject
    {
        public GenomeEditor genome;
        public string plantName;
        [Header("Growth")]
        public PlantBuilder plantBuilder;
        public float growthPerPhase;

        [Header("Seebs")]
        public Sprite seedIcon;
        public int minSeeds;
        public int maxSeeds;

        public int plantID;

        public override void AssignId(int myNewID)
        {
            plantID = myNewID;
        }

        public float AddGrowth(int phaseDiff, float currentGrowth)
        {
            var extraGrowth = growthPerPhase * phaseDiff;
            return Mathf.Clamp(currentGrowth + extraGrowth, 0, 1);
        }

        public Seed[] HarvestSeeds(Seed sourceSeed)
        {
            var generatedSeeds = Random.Range(minSeeds, maxSeeds);
            var seedResult = new Seed[generatedSeeds];
            for (int i = 0; i < seedResult.Length; i++)
            {
                seedResult[i] = new Seed
                {
                    plantType = plantID,
                    genes = sourceSeed.genes
                };
            }
            return seedResult;
        }
    }
}
