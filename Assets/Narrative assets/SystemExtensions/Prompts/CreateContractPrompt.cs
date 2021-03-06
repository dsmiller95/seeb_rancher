﻿using Assets.Scripts.UI.MarketContracts;
using Dman.ReactiveVariables;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Dman.NarrativeSystem
{
    [CreateAssetMenu(fileName = "CreateContractPrompt", menuName = "Narrative/Prompts/CreateContractPrompt", order = 1)]
    public class CreateContractPrompt : Prompt
    {
        public UnityEvent onOpened;
        public UnityEvent onCompleted;

        public TargetContractDescriptor contractToCreate;
        public GameObjectVariable variableToPutContractIn;

        public override void OpenPrompt(Conversation conversation)
        {
            onOpened?.Invoke();
            //TODO: clone contract here would be best practice. the object pointer is preserved, which could lead to weirdness
            //  should be fine for now. No system is set up to run a prompt more than once without a save between.
            var createdContract = MarketManager.Instance.CreateClaimedContract(contractToCreate);
            MarketManager.Instance.ShowClaimedContractsModal();
            createdContract.StartCoroutine(HighlightContract(createdContract.gameObject));
            //variableToPutContractIn.SetValue(createdContract.gameObject);

            OpenPromptWithSetup(() =>
            {
                variableToPutContractIn.SetValue(null);
                onCompleted?.Invoke();
                conversation.PromptClosed();
                Destroy(currentPrompt.gameObject);
            });
        }

        IEnumerator HighlightContract(GameObject toBeHighlighted)
        {
            yield return new WaitForEndOfFrame();
            variableToPutContractIn.SetValue(toBeHighlighted);
        }
    }
}
