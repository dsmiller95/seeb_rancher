﻿using Assets.Scripts.DataModels;
using Assets.Scripts.Utilities.Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.SeedInventory
{
    [RequireComponent(typeof(SeedBucketDisplay))]
    public class SeedInventoryDropSlot : MonoBehaviour
    {
        public GameObjectVariable draggingSeedSet;

        public Button DropSlotButton;
        public TMP_InputField labelInputField;

        private SeedBucketUI dataModel;

        private SeedBucketDisplay Displayer => GetComponent<SeedBucketDisplay>();


        public void SeedSlotClicked()
        {
            var dragginSeeds = draggingSeedSet.CurrentValue?.GetComponent<DraggingSeeds>();
            if (dragginSeeds == null)
            {
                PopOutNewDragging();
                return;
            }
            TryAddHoveringSeedToSelf(dragginSeeds);
        }

        public void PopOutNewDragging()
        {
            var draggingProvider = GameObject.FindObjectOfType<DraggingSeedSingletonProvider>();
            var currentDragger = draggingProvider.SpawnNewDraggingSeedsOrGetCurrent();
            if (currentDragger.myBucket.TryCombineSeedsInto(dataModel.bucket))
            {
                currentDragger.SeedBucketUpdated();
                Displayer.DisplaySeedBucket(dataModel.bucket);
            }
        }

        public void TryAddHoveringSeedToSelf(DraggingSeeds dragginSeeds)
        {
            dataModel.bucket.TryCombineSeedsInto(dragginSeeds.myBucket);
            Displayer.DisplaySeedBucket(dataModel.bucket);
            dragginSeeds.SeedBucketUpdated();
        }

        public void SetDataModelLink(SeedBucketUI dataModel)
        {
            this.dataModel = dataModel;
            Displayer.DisplaySeedBucket(dataModel.bucket);
            labelInputField.text = dataModel.description;
            labelInputField.onDeselect.AddListener(newValue =>
            {
                if (isResetting)
                {
                    return;
                }
                Debug.Log(newValue);
                dataModel.description = newValue;
                StartCoroutine(ResetTextField());
                //resetText = true;
            });
        }
        private bool isResetting = false;
        IEnumerator ResetTextField()
        {
            isResetting = true;
            labelInputField.onFocusSelectAll = false;
            labelInputField.ActivateInputField();
            labelInputField.MoveToStartOfLine(false, false);
            yield return new WaitForEndOfFrame();
            labelInputField.DeactivateInputField();
            labelInputField.onFocusSelectAll = true;
            yield return new WaitForEndOfFrame();
            isResetting = false;
        }
    }
}