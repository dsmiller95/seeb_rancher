﻿using Assets.Scripts.DataModels;
using Assets.Scripts.GreenhouseLoader;
using Assets.Scripts.UI.Manipulators.Scripts;
using Dman.LSystem.SystemRuntime.DOTSRenderer;
using Dman.LSystem.UnityObjects;
using Dman.ObjectSets;
using Dman.ReactiveVariables;
using Dman.SceneSaveSystem;
using Dman.Utilities;
using Genetics.GeneticDrivers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.VFX;

namespace Assets.Scripts.Plants
{
    /// <summary>
    /// handles the breeding and planting of an l-system
    /// does not own the l-system's state itself. that is managed entirely by a l system behavior component
    /// </summary>
    public class PlantedLSystem : MonoBehaviour,
        ISaveableData,
        ILSystemCompileTimeParameterGenerator,
        IManipulatorClickReciever
    {
        public LSystemPlantType plantType;
        public LSystemBehavior lSystemManager;

        public GameObjectVariable selectedPlant;

        public VisualEffect harvestEffect;
        public VisualEffect plantedEffect;

        private CompiledGeneticDrivers _drivers;
        public CompiledGeneticDrivers GeneticDrivers
        {
            get
            {
                if (pollinationState == default)
                {
                    return null;
                }
                if (_drivers == null)
                {
                    _drivers = plantType.genome.CompileGenome(pollinationState.SelfGenes.genes);
                }
                return _drivers;
            }
            private set
            {
                if (value != null)
                {
                    throw new System.Exception("can only clear");
                }
                _drivers = null;
            }
        }
        public PollinationState pollinationState;
        [Tooltip("expected to range from 0 to 1")]
        public FloatGeneticDriver pollenSpreadDriver;
        public float minPollenSpreadRadius = 1;
        public float maxPollenSpreadRadius = 4;

        public GameObject plantsParent;

        public event Action OnHarvested;

        public EventGroup beginPhaseTransition;
        public float maxTimeBetweenSteps;
        public float minTimeBetweenSteps;
        private int stepsLeftInPhaseTransition = 0;
        private float timeTillNextStep;

        public float PollinationRadius
        {
            get
            {
                if (!GeneticDrivers.TryGetGeneticData(pollenSpreadDriver, out var spreadFactor))
                {
                    throw new System.Exception($"{pollenSpreadDriver.DriverName} does not exist in the plant's genome");
                }
                return spreadFactor * (maxPollenSpreadRadius - minPollenSpreadRadius) + minPollenSpreadRadius;
            }
        }

        private void Awake()
        {
            beginPhaseTransition.OnEvent += PhaseTransitionBegin;
        }

        private void Start()
        {
        }

        private void Update()
        {
            if(stepsLeftInPhaseTransition <= 0)
            {
                return;
            }

            timeTillNextStep -= Time.deltaTime;

            if (!lSystemManager.steppingHandle.CanStep())
            {
                return;
            }

            if(timeTillNextStep <= 0)
            {
                this.StepOnce();
                timeTillNextStep = UnityEngine.Random.Range(minTimeBetweenSteps, maxTimeBetweenSteps);
                stepsLeftInPhaseTransition--;
                PhaseAdvancingCoordinator.instance.DelayPhaseComplete(maxTimeBetweenSteps + 0.1f);
            }
        }

        private void OnDestroy()
        {
            beginPhaseTransition.OnEvent -= PhaseTransitionBegin;
        }

        private void PhaseTransitionBegin()
        {
            this.stepsLeftInPhaseTransition = plantType.stepsPerPhase;
            timeTillNextStep = UnityEngine.Random.Range(minTimeBetweenSteps, maxTimeBetweenSteps);
        }

        private void StepOnce()
        {
            this.lSystemManager.StepSystem();
        }

        private void SprayMySeed()
        {
            var radius = PollinationRadius;
            var targetOtherPlants = Physics.OverlapCapsule(transform.position - Vector3.up * 5, transform.position + Vector3.up * 5, radius)
                .Select(collider => collider.gameObject?.GetComponentInParent<PlantedLSystem>())
                .Where(x => x != null)
                .ToList();
            targetOtherPlants = targetOtherPlants
                .Where(x => x.CanPollinateFrom(this))
                .ToList();
            foreach (var pollinationTarget in targetOtherPlants)
            {
                pollinationTarget.PollinateFrom(this);
            }
        }


        public void InitializeWithSeed(Seed toBePlanted)
        {
            pollinationState = new PollinationState(toBePlanted);
            var plantTypeRegistry = RegistryRegistry.GetObjectRegistry<BasePlantType>();
            plantType = (LSystemPlantType)plantTypeRegistry.GetUniqueObjectFromID(pollinationState.SelfGenes.plantType);
            plantedEffect.Play();

            // sprout the seedling
            plantType.ConfigureLSystemWithSeedling(lSystemManager, GeneticDrivers, pollinationState);
        }
        public Dictionary<string, string> GenerateCompileTimeParameters()
        {
            var drivers = this.GeneticDrivers;
            return plantType.geneticModifiers
                .Select(x =>
                {
                    if (drivers.TryGetGeneticData(x.geneticDriver, out var driverValue))
                    {
                        return new { x.lSystemDefineDirectiveName, driverValue };
                    }
                    return null;
                })
                .Where(x => x != null)
                .ToDictionary(x => x.lSystemDefineDirectiveName, x => x.driverValue.ToString());
        }

        /// <summary>
        /// Whether this plant can pollinate other plants
        /// </summary>
        /// <returns></returns>
        public bool CanPollinate()
        {
            if (plantType == null)
            {
                return false;
            }
            return (plantType.HasFlowers(this.lSystemManager))
                && (pollinationState?.CanPollinate() ?? false);
        }

        /// <summary>
        /// Whether this plant can be pollinated from other plants
        /// </summary>
        /// <returns></returns>
        public bool CanPollinateFrom(PlantedLSystem other)
        {
            if (!other.CanPollinate())
            {
                return false;
            }
            if (pollinationState == null || plantType == null)
            {
                return false;
            }
            if (!plantType.HasFlowers(this.lSystemManager))
            {
                return false;
            }
            return true;
        }

        public bool PollinateFrom(PlantedLSystem other)
        {
            if (!CanPollinateFrom(other))
            {
                return false;
            }
            if (pollinationState.RecieveGenes(other.pollinationState))
            {
                return true;
            }
            return false;
        }
        public void ClipAnthers()
        {
            pollinationState.ClipAnthers();

            // TODO: write code to programatically update the state, removing only anthers
            //  OR, just update the plant once. if built for very frequent updates, won't make much difference
        }

        public bool IsMatureAndHasSeeds()
        {
            return this.IsMature() && this.CanHarvest() && this.CurrentSeedCount() > 0;
        }

        public bool IsMature()
        {
            return plantType != null && plantType.IsMature(this.lSystemManager);
        }

        public bool CanHarvest()
        {
            return plantType != null && plantType.CanHarvest(this.lSystemManager);
        }
        public int CurrentSeedCount()
        {
            if (plantType == null) {
                return 0;
            }
            return plantType.TotalNumberOfSeedsInState(this.lSystemManager);
        }
        public Seed[] TryHarvest()
        {
            if (CanHarvest())
            {
                return HarvestPlant();
            }
            return null;
        }

        private Seed[] HarvestPlant()
        {
            var harvestedSeeds = plantType.HarvestSeeds(pollinationState, this.lSystemManager, GeneticDrivers);

            plantType = null;
            pollinationState = null;
            GeneticDrivers = null;

            OnHarvested?.Invoke();
            StartCoroutine(HarvestEffect());

            return harvestedSeeds;
        }

        private IEnumerator HarvestEffect()
        {
            // assuming the mesh has been rotated 90 degrees around z axis.
            harvestEffect.SetFloat("height", 10 * plantsParent.transform.localScale.x);
            harvestEffect.Play();
            yield return new WaitForSeconds(0.3f);
            Destroy(this.gameObject);
        }

        /// <summary>
        /// Handles clicks from the Click Manipulator
        ///     TODO: dispatch events to this method based on the unique organ identification system
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        public bool SelfClicked(uint clickedObjectID)
        {
            if (plantType == null)
            {
                return false;
            }
            selectedPlant.SetValue(gameObject);
            return true;
        }
        public GameObject GetOutlineObject()
        {
            return gameObject;
        }
        public bool IsSelectable()
        {
            return plantType != null;
        }

        #region Saveable
        [System.Serializable]
        class PlantSaveObject
        {
            int plantTypeId;
            PollinationState pollination;
            public PlantSaveObject(PlantedLSystem source)
            {
                plantTypeId = source.plantType?.myId ?? -1;
                pollination = source.pollinationState;
            }

            public void Apply(PlantedLSystem target)
            {
                var plantTypeRegistry = RegistryRegistry.GetObjectRegistry<BasePlantType>();
                target.plantType = (LSystemPlantType)(plantTypeId == -1 ? null : plantTypeRegistry.GetUniqueObjectFromID(plantTypeId));
                target.pollinationState = pollination;
            }
        }

        public string UniqueSaveIdentifier => "PlantContainer";
        public object GetSaveObject()
        {
            return new PlantSaveObject(this);
        }

        public void SetupFromSaveObject(object save)
        {
            if (save is PlantSaveObject saveObj)
            {
                saveObj.Apply(this);
            }
        }

        public ISaveableData[] GetDependencies()
        {
            return new ISaveableData[0];
        }

        #endregion
    }
}