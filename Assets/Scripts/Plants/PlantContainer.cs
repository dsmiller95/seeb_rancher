﻿using Assets.Scripts.DataModels;
using Assets.Scripts.GreenhouseLoader;
using Assets.Scripts.UI.Manipulators.Scripts;
using Dman.ReactiveVariables;
using Dman.SceneSaveSystem;
using Dman.Utilities;
using Genetics.GeneticDrivers;
using System;
using System.Collections;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.VFX;

namespace Assets.Scripts.Plants
{

    public class PlantContainer : MonoBehaviour,
        ISpawnable,
        IManipulatorClickReciever,
        ISaveableData
    {
        public BasePlantType plantType;
        public PlantTypeRegistry plantTypes;
        public GameObjectVariable selectedPlant;

        public VisualEffect harvestEffect;
        public VisualEffect plantedEffect;

        [SerializeField]
        [HideInInspector]
        public PlantState currentState;

        private CompiledGeneticDrivers _drivers;
        public CompiledGeneticDrivers GeneticDrivers
        {
            get
            {
                if (plantType == null || pollinationState == default)
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

        public IntReference levelPhase;
        public GameObject plantsParent;

        public event Action OnHarvested;

        private void Start()
        {
            levelPhase.ValueChanges
                .TakeUntilDisable(this)
                .Pairwise()
                .Subscribe(pair =>
                {
                    AdvanceGrowPhase(pair.Current - pair.Previous);
                }).AddTo(this);
        }

        private void OnDestroy()
        {
            if(currentState == null)
            {
            }else
            {
                currentState?.Dispose();
                currentState = null;
            }
        }

        private void AdvanceGrowPhase(int phaseDiff)
        {
            if (plantType == null)
                return;
            if (currentState != null && plantType != null)
            {
                plantType.AddGrowth(phaseDiff, currentState);
            }
            GrowthUpdated();
            if (CanPollinate())
            {
                SprayMySeed();
            }
        }

        private void SprayMySeed()
        {
            var radius = PollinationRadius;
            var targetOtherPlants = Physics.OverlapCapsule(transform.position - Vector3.up * 5, transform.position + Vector3.up * 5, radius)
                .Select(collider => collider.gameObject?.GetComponentInParent<PlantContainer>())
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

        public bool CanPlantSeed => plantType == null;

        public void PlantSeed(Seed toBePlanted)
        {
            pollinationState = new PollinationState(toBePlanted);
            plantType = plantTypes.GetUniqueObjectFromID(pollinationState.SelfGenes.plantType);
            currentState?.Dispose();
            currentState = plantType.GenerateBaseStateAndHookTo();
            plantedEffect.Play();
            GrowthUpdated(true);
        }

        /// <summary>
        /// Called whenever the object is spawned through spawning code
        /// </summary>
        public void SetupAfterSpawn()
        {
            plantType = null;
            currentState?.Dispose();
            currentState = null;
            pollinationState = null;
            GeneticDrivers = null;
            pollinationState = null;
            GrowthUpdated(true);
        }

        /// <summary>
        /// called whenever growth is changed, to conditionally trigger updating the plant.
        /// </summary>
        /// <param name="forcePrefabInstantiate"></param>
        public void GrowthUpdated(bool forcePrefabInstantiate = false)
        {
            if (plantType == null || currentState == null)
            {
                if (forcePrefabInstantiate)
                {
                    UpdatePlant();
                }
                return;
            }
            UpdatePlant();
        }

        /// <summary>
        /// update the redndered plant view based on current state
        /// </summary>
        public void UpdatePlant()
        {
            if (plantType == null || currentState == null)
            {
                plantsParent.DestroyAllChildren();
                return;
            }
            plantType.BuildPlantInto(plantsParent.transform, GeneticDrivers, currentState, pollinationState);
        }

        /// <summary>
        /// Whether this plant can pollinate other plants
        /// </summary>
        /// <returns></returns>
        public bool CanPollinate()
        {
            if (plantType == null || currentState == null)
            {
                return false;
            }
            return (plantType.HasFlowers(currentState))
                && (pollinationState?.CanPollinate() ?? false);
        }

        /// <summary>
        /// Whether this plant can be pollinated from other plants
        /// </summary>
        /// <returns></returns>
        public bool CanPollinateFrom(PlantContainer other)
        {
            if (!other.CanPollinate())
            {
                return false;
            }
            if (pollinationState == null || plantType == null || currentState == null)
            {
                return false;
            }
            if (!plantType.HasFlowers(currentState))
            {
                return false;
            }
            return true;
        }

        public bool PollinateFrom(PlantContainer other)
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
            UpdatePlant();
        }

        public bool IsMatureAndHasSeeds()
        {
            return this.IsMature() && this.CanHarvest() && this.CurrentSeedCount() > 0;
        }

        public bool IsMature()
        {
            return currentState != null && plantType != null && plantType.IsMature(currentState);
        }

        public bool CanHarvest()
        {
            return currentState != null && plantType != null && plantType.CanHarvest(currentState);
        }
        public int CurrentSeedCount()
        {
            if (currentState == null && plantType == null) {
                return 0;
            }
            return plantType.TotalNumberOfSeedsInState(currentState);
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
            var harvestedSeeds = plantType.HarvestSeeds(pollinationState, currentState, GeneticDrivers);

            plantType = null;
            currentState?.Dispose();
            currentState = null;
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
            UpdatePlant();
        }

        /// <summary>
        /// Handles clicks from the Click Manipulator
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        public bool SelfHit(RaycastHit hit)
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
            PlantState plantState;
            PollinationState pollination;
            public PlantSaveObject(PlantContainer source)
            {
                plantTypeId = source.plantType?.myId ?? -1;
                plantState = source.currentState;
                pollination = source.pollinationState;
            }

            public void Apply(PlantContainer target)
            {
                target.plantType = plantTypeId == -1 ? null : target.plantTypes.GetUniqueObjectFromID(plantTypeId);
                target.pollinationState = pollination;

                target.currentState?.Dispose();
                target.currentState = plantState;
                target.currentState?.AfterDeserialized();

                target.GrowthUpdated(true);
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
