using System.Collections.Generic;
using NUnit.Framework;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using Rule = BrunoMikoski.ScriptableObjectCollections.Picker.CollectionItemQuerySatisfiability.Rule;

namespace BrunoMikoski.ScriptableObjectCollections.Tests
{
    /// <summary>
    /// Brute-force agreement suite: proves the editor-side satisfiability checker (used by
    /// CollectionItemQueryPropertyDrawer) agrees with the runtime match semantics
    /// (CollectionItemQueryRule.Passes) for EVERY rule pair over a 4-item universe and EVERY rule
    /// triple over a 3-item universe — zero false positives and zero missed conflicts. Also locks
    /// the specific conflict classes the previous pairwise-only validation missed.
    /// </summary>
    public class CollectionItemQuerySatisfiabilityTests
    {
        private const int MatchTypeCount = 4;

        // Reference satisfiability: enumerate every target subset of a fixed universe and evaluate
        // rules with the exact runtime contract. Independent of the checker's item-restriction and
        // mask machinery. Rules with empty pickers are skipped, mirroring the runtime.
        private static bool ReferenceIsSatisfiable(List<(int matchType, int pickerMask)> rules, int universeSize)
        {
            for (int target = 0; target < 1 << universeSize; target++)
            {
                bool allPass = true;
                for (int i = 0; i < rules.Count; i++)
                {
                    (int matchType, int pickerMask) = rules[i];
                    int pickerCount = PopCount(pickerMask);
                    if (pickerCount == 0)
                        continue;

                    int matchCount = PopCount(target & pickerMask);
                    if (!CollectionItemQueryRule.Passes(matchType, matchCount, pickerCount))
                    {
                        allPass = false;
                        break;
                    }
                }

                if (allPass)
                    return true;
            }

            return false;
        }

        private static int PopCount(int value)
        {
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }

        private static (long, long) ItemGuid(int itemIndex)
        {
            return (itemIndex + 1, itemIndex + 100);
        }

        private static Rule ToCheckerRule(int matchType, int pickerMask)
        {
            HashSet<(long, long)> items = new HashSet<(long, long)>();
            for (int bit = 0; bit < 32; bit++)
            {
                if ((pickerMask & (1 << bit)) != 0)
                    items.Add(ItemGuid(bit));
            }

            return new Rule(matchType, items);
        }

        private static Rule MakeRule(int matchType, params int[] itemIndices)
        {
            HashSet<(long, long)> items = new HashSet<(long, long)>();
            foreach (int itemIndex in itemIndices)
                items.Add(ItemGuid(itemIndex));

            return new Rule(matchType, items);
        }

        [Test]
        public void AllRulePairs_OverFourItemUniverse_AgreeWithBruteForce()
        {
            const int universeSize = 4;
            int subsetCount = 1 << universeSize;

            int disagreements = 0;
            for (int typeA = 0; typeA < MatchTypeCount; typeA++)
            for (int maskA = 0; maskA < subsetCount; maskA++)
            for (int typeB = 0; typeB < MatchTypeCount; typeB++)
            for (int maskB = 0; maskB < subsetCount; maskB++)
            {
                List<(int, int)> referenceRules = new List<(int, int)> { (typeA, maskA), (typeB, maskB) };
                List<Rule> checkerRules = new List<Rule>
                {
                    ToCheckerRule(typeA, maskA),
                    ToCheckerRule(typeB, maskB),
                };

                bool expected = ReferenceIsSatisfiable(referenceRules, universeSize);
                bool actual = CollectionItemQuerySatisfiability.IsSatisfiable(checkerRules);
                if (expected != actual)
                    disagreements++;
            }

            Assert.AreEqual(0, disagreements, "Checker disagrees with brute-force reference on rule pairs.");
        }

        [Test]
        public void AllRuleTriples_OverThreeItemUniverse_AgreeWithBruteForce()
        {
            const int universeSize = 3;
            int subsetCount = 1 << universeSize;

            List<(int matchType, int pickerMask)> allRules = new List<(int, int)>();
            for (int type = 0; type < MatchTypeCount; type++)
            for (int mask = 0; mask < subsetCount; mask++)
                allRules.Add((type, mask));

            int disagreements = 0;
            for (int i = 0; i < allRules.Count; i++)
            for (int j = i; j < allRules.Count; j++)
            for (int k = j; k < allRules.Count; k++)
            {
                List<(int, int)> referenceRules = new List<(int, int)> { allRules[i], allRules[j], allRules[k] };
                List<Rule> checkerRules = new List<Rule>
                {
                    ToCheckerRule(allRules[i].matchType, allRules[i].pickerMask),
                    ToCheckerRule(allRules[j].matchType, allRules[j].pickerMask),
                    ToCheckerRule(allRules[k].matchType, allRules[k].pickerMask),
                };

                bool expected = ReferenceIsSatisfiable(referenceRules, universeSize);
                bool actual = CollectionItemQuerySatisfiability.IsSatisfiable(checkerRules);
                if (expected != actual)
                    disagreements++;
            }

            Assert.AreEqual(0, disagreements, "Checker disagrees with brute-force reference on rule triples.");
        }

        [Test]
        public void ConflictsMissedByPairwiseValidation_AreDetected()
        {
            // Any{a} + NotAll{a}: the singleton contradiction the old validation missed.
            Assert.IsFalse(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any, 0),
                MakeRule(CollectionItemQueryRule.NotAll, 0),
            }));

            // Any{a,b} + NotAny{a} + NotAny{b}: emergent three-way conflict.
            Assert.IsFalse(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any, 0, 1),
                MakeRule(CollectionItemQueryRule.NotAny, 0),
                MakeRule(CollectionItemQueryRule.NotAny, 1),
            }));

            // Any{a} + Any{b} + NotAll{a,b}: emergent three-way conflict.
            Assert.IsFalse(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any, 0),
                MakeRule(CollectionItemQueryRule.Any, 1),
                MakeRule(CollectionItemQueryRule.NotAll, 0, 1),
            }));

            // All{a} + All{b} + NotAll{a,b}: emergent three-way conflict.
            Assert.IsFalse(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.All, 0),
                MakeRule(CollectionItemQueryRule.All, 1),
                MakeRule(CollectionItemQueryRule.NotAll, 0, 1),
            }));
        }

        [Test]
        public void LavaFrozenScenario_IsSatisfiableAndMatchesIntent()
        {
            const int lava = 0;
            const int frozen = 1;

            // Any {OnLava} + NotAll {Frozen} — the Hot Behaviour prefab configuration.
            Assert.IsTrue(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any, lava),
                MakeRule(CollectionItemQueryRule.NotAll, frozen),
            }));

            // Runtime contract spot-checks: matches exactly "has lava AND lacks frozen".
            // target {lava}: Any(1 of 1) passes, NotAll(0 of 1) passes.
            Assert.IsTrue(CollectionItemQueryRule.Passes(CollectionItemQueryRule.Any, 1, 1));
            Assert.IsTrue(CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAll, 0, 1));
            // target {lava, frozen}: NotAll(1 of 1) fails.
            Assert.IsFalse(CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAll, 1, 1));
            // target {}: Any(0 of 1) fails.
            Assert.IsFalse(CollectionItemQueryRule.Passes(CollectionItemQueryRule.Any, 0, 1));

            // For a single item, NotAll and NotAny are equivalent.
            for (int matchCount = 0; matchCount <= 1; matchCount++)
            {
                Assert.AreEqual(
                    CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAny, matchCount, 1),
                    CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAll, matchCount, 1));
            }
        }

        [Test]
        public void EmptyRules_AreInert()
        {
            // A lone empty rule no longer poisons the query.
            Assert.IsTrue(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any),
            }));

            Assert.IsTrue(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.NotAll),
            }));

            // Empty rules do not mask real conflicts among the remaining rules.
            List<Rule> rules = new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any),
                MakeRule(CollectionItemQueryRule.All, 0),
                MakeRule(CollectionItemQueryRule.NotAny, 0),
            };
            Assert.IsFalse(CollectionItemQuerySatisfiability.IsSatisfiable(rules));

            List<int> conflictIndices = new List<int>();
            Assert.IsTrue(CollectionItemQuerySatisfiability.TryFindSmallestConflict(rules, conflictIndices));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, conflictIndices);
        }

        [Test]
        public void TryFindSmallestConflict_ReturnsMinimalSubset()
        {
            // Conflict lives in rules 0..2; rule 3 is an innocent bystander.
            List<Rule> rules = new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any, 0),
                MakeRule(CollectionItemQueryRule.All, 1),
                MakeRule(CollectionItemQueryRule.NotAll, 0, 1),
                MakeRule(CollectionItemQueryRule.NotAny, 2),
            };

            List<int> conflictIndices = new List<int>();
            Assert.IsTrue(CollectionItemQuerySatisfiability.TryFindSmallestConflict(rules, conflictIndices));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, conflictIndices);

            // Satisfiable set: no conflict reported.
            List<Rule> satisfiable = new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.Any, 0),
                MakeRule(CollectionItemQueryRule.NotAll, 1),
            };
            Assert.IsFalse(CollectionItemQuerySatisfiability.TryFindSmallestConflict(satisfiable, conflictIndices));
            Assert.AreEqual(0, conflictIndices.Count);
        }

        [Test]
        public void PairwiseFallback_AboveExactItemCeiling_StillCatchesDirectConflicts()
        {
            // 17 distinct items in one rule pushes past MaxExactItems (16) into the fallback.
            int[] manyItems = new int[CollectionItemQuerySatisfiability.MaxExactItems + 1];
            for (int i = 0; i < manyItems.Length; i++)
                manyItems[i] = i;

            // All{17 items} + NotAny{first item}: direct contradiction the fallback must catch.
            Assert.IsFalse(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.All, manyItems),
                MakeRule(CollectionItemQueryRule.NotAny, 0),
            }));

            // All{17 items} alone: satisfiable, fallback must not flag it.
            Assert.IsTrue(CollectionItemQuerySatisfiability.IsSatisfiable(new List<Rule>
            {
                MakeRule(CollectionItemQueryRule.All, manyItems),
            }));
        }

        [Test]
        public void PassesContract_BoundaryValues()
        {
            // Any: at least one present.
            Assert.IsFalse(CollectionItemQueryRule.Passes(CollectionItemQueryRule.Any, 0, 3));
            Assert.IsTrue(CollectionItemQueryRule.Passes(CollectionItemQueryRule.Any, 1, 3));

            // All: every one present.
            Assert.IsFalse(CollectionItemQueryRule.Passes(CollectionItemQueryRule.All, 2, 3));
            Assert.IsTrue(CollectionItemQueryRule.Passes(CollectionItemQueryRule.All, 3, 3));

            // NotAny: none present.
            Assert.IsTrue(CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAny, 0, 3));
            Assert.IsFalse(CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAny, 1, 3));

            // NotAll: at least one missing.
            Assert.IsTrue(CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAll, 2, 3));
            Assert.IsFalse(CollectionItemQueryRule.Passes(CollectionItemQueryRule.NotAll, 3, 3));
        }
    }
}
