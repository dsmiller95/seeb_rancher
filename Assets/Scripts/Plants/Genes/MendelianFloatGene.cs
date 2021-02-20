using Genetics;
using Genetics.Genes;
using Genetics.GeneticDrivers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Plants
{
    [CreateAssetMenu(fileName = "MendelianFloatGene", menuName = "Genetics/Genes/MendelianFloat", order = 2)]
    public class MendelianFloatGene : GeneEditor
    {
        public FloatGeneticDriver floatOutput;
        public override int GeneSize => 1;

        [Tooltip("When true, the highest value out of the gene copies will be used. When false, the lowest value is used.")]
        public bool higherValueDominant;
        public float rangeMin = 0;
        public float rangeMax = 1;

        public override void Evaluate(CompiledGeneticDrivers editorHandle, GeneCopies[] data)
        {
            if (editorHandle.TryGetGeneticData(floatOutput, out var _))
            {
                Debug.LogWarning($"Overwriting already set genetic driver {floatOutput} in gene {this}.");
            }
            var gene = data[0];
            double outputValue;
            if (higherValueDominant)
            {
                outputValue = gene.chromosomalCopies.Max(x => EvaluateSingleGene(x));
            }
            else
            {
                outputValue = gene.chromosomalCopies.Min(x => EvaluateSingleGene(x));
            }
            editorHandle.SetGeneticDriverData(floatOutput, (float)outputValue);
        }
        private double EvaluateSingleGene(SingleGene gene)
        {
            var weight = MendelianBooleanSwitch.HammingWeight(gene.Value);
            var adjusted = (weight / 64d) * (rangeMax - rangeMin) + rangeMin;
            return adjusted;
        }


        public override IEnumerable<GeneticDriver> GetInputs()
        {
            yield break;
        }

        public override IEnumerable<GeneticDriver> GetOutputs()
        {
            yield return floatOutput;
        }

        public override SingleGene[] GenerateGeneData()
        {
            ulong newGene = 0;
            var randomGen = new System.Random(Random.Range(int.MinValue, int.MaxValue));
            var binaryProportionalChance = randomGen.NextDouble();
            for (int i = 0; i < sizeof(ulong) * 8; i++)
            {
                var nextBit = randomGen.NextDouble() > binaryProportionalChance;
                if (nextBit)
                {
                    newGene |= ((ulong)1) << i;
                }
            }
            return new SingleGene[] { new SingleGene { Value = newGene } };
        }
    }
}