﻿using Genetics.GeneSummarization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Dman.ReactiveVariables;
using UniRx;

namespace Assets.Scripts.UI.SeedInventory
{
    public class SeedSetSummaryPanel: MonoBehaviour
    {
        public GameObjectVariable hoveredDropSlot;

        public SeedSetSummaryRow summaryRowPrefab;
        public GameObject summaryRowParent;

        public GeneticDriverSummarySet summarization;

        private void Awake()
        {
            hoveredDropSlot.Value.TakeUntilDestroy(gameObject)
                .StartWith((GameObject)null)
                .Subscribe(x =>
                {
                    if (x == null) x = null;
                    var summary = x?.GetComponent<SeedInventoryDropSlot>()?.summarization;
                    if(summary == null)
                    {
                        summaryRowParent.SetActive(false);
                    }else
                    {
                        summaryRowParent.SetActive(true);
                        this.DisplaySummary(summary);
                    }
                }).AddTo(gameObject);
        }

        public void DisplaySummary(GeneticDriverSummarySet summary)
        {
            foreach (Transform child in summaryRowParent.transform)
            {
                if (child.GetComponent<SeedSetSummaryRow>())
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (var summaryItem in summary.summaries)
            {
                var newRow = Instantiate(summaryRowPrefab, summaryRowParent.transform);
                newRow.DisplaySummary(summaryItem);
            }
        }
    }
}
