using Assets.Scripts.Utilities.Core;
using Assets.Scripts.Utilities.SaveSystem.Components;
using Genetics.GeneticDrivers;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Assets.Scripts.UI.MarketContracts
{
    /// <summary>
    /// class used during runtime to describe a contract
    /// </summary>
    public class ContractDescriptor
    {
        public BooleanGeneticTarget[] targets;
        public float reward;
    }

    public class MarketManager : MonoBehaviour
    {
        public ContractContainer contractOfferPrefab;
        public SaveablePrefabParent marketModalContractsParent;
        public ContractContainer claimedContractPrefab;
        public SaveablePrefabParent claimedContractsModalParent;

        public IntReference levelPhase;
        public float defaultReward;
        [Tooltip("For every extra genetic driver over 1, multiply the reward by this amount. 3 genetic drivers will be defaultReward * multiplierPerAdditional^2")]
        public float multiplierPerAdditional;
        public BooleanGeneticDriver[] targetBooleanDrivers;
        [Range(0, 1)]
        public float chanceForNewContractPerPhase;

        public static MarketManager Instance;

        private void Awake()
        {
            Instance = this;
            levelPhase.ValueChanges
                .TakeUntilDestroy(this)
                .Pairwise()
                .Subscribe(pair =>
                {
                    if(pair.Current - pair.Previous != 1)
                    {
                        return;
                    }
                    var hasNewContract = Random.Range(0f, 1f) < chanceForNewContractPerPhase;
                    if (hasNewContract)
                    {
                        GenerateNewContract();
                    }
                }).AddTo(this);
        }
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void GenerateNewContract()
        {
            var rangeSample = Random.Range(0f, 1f - 1e-5f);
            // varies from 1 to targetSetLength, weighted towards lower numbers
            var numberOfTargets = Mathf.FloorToInt(Mathf.Pow(rangeSample, 2) * targetBooleanDrivers.Length) + 1;

            var targetIndexes = ArrayExt.SelectIndexSources(numberOfTargets, targetBooleanDrivers.Length);
            var chosenDrivers = targetIndexes.Select(index => targetBooleanDrivers[index]);
            var newPrice = defaultReward * (Mathf.Pow(multiplierPerAdditional, numberOfTargets));

            var newTargets = chosenDrivers
                .Select(x => new BooleanGeneticTarget(x))
                .ToArray();
            this.CreateMarketContract(new ContractDescriptor
            {
                targets = newTargets,
                reward = newPrice
            });
        }

        public void CreateMarketContract(ContractDescriptor contract)
        {
            var newContract = Instantiate(contractOfferPrefab, marketModalContractsParent.transform);
            newContract.targets = contract.targets;
            newContract.rewardAmount = contract.reward;
        }
        public void CreateClaimedContract(ContractDescriptor contract)
        {
            var newContract = Instantiate(claimedContractPrefab, claimedContractsModalParent.transform);
            newContract.targets = contract.targets;
            newContract.rewardAmount = contract.reward;
        }

        public void ClaimContract(ContractContainer marketContract)
        {
            if(marketContract.transform.parent != marketModalContractsParent.transform)
            {
                throw new System.Exception("contract must be in the market");
            }
            var contractDescriptor = new ContractDescriptor
            {
                reward = marketContract.rewardAmount,
                targets = marketContract.targets
            };
            Destroy(marketContract.gameObject);
            CreateClaimedContract(contractDescriptor);
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}