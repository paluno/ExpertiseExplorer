using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExpertiseExplorer.Algorithms.FPS;
using System.Collections.Generic;

namespace ExpertiseExplorer.Algorithms.Test
{
    [TestClass]
    public class FPSTreeTest
    {
        [TestMethod]
        public void ComplexTest()
        {
            RootDirectory root = new RootDirectory();

            root.AddReview("franz", new string[] { "fileA.txt", "a/b/c/fileA.txt", "a/b/b/fileX.txt" });
            root.AddReview("franz", new string[] { "fileA.txt", "a/b/b/fileX.txt" });
            root.AddReview("heinz", new string[] { "fileA.txt", "a/b/c/fileA.txt", "a/b/b/fileX.txt" });
            root.AddReview("willi", new string[] { "c/b/a/fileX.txt", "c/b/a/fileY.txt" });
            root.AddReview("heinz", new string[] { "c/b/a/fileX.txt" });

            IDictionary<string, double> dictExpertises = root.CalculateDeveloperExpertisesForFile("fileA.txt");

            Assert.AreEqual(2, dictExpertises.Count);   // franz and heinz
            Assert.IsTrue(aboutEqual(1d/3d + 1d/2d, dictExpertises["franz"]));
            Assert.IsTrue(aboutEqual(1d/3d, dictExpertises["heinz"]));

            dictExpertises = root.CalculateDeveloperExpertisesForFile("c/b/a/fileX.txt");

            Assert.AreEqual(2, dictExpertises.Count);   // willi and heinz
            Assert.IsTrue(aboutEqual(1d / 2d + 1d/2d * 3d/4d, dictExpertises["willi"]));    // some indirection already
            Assert.IsTrue(aboutEqual(1d, dictExpertises["heinz"]));

            dictExpertises = root.CalculateDeveloperExpertisesForFile("a/b/c/d/e/f/g/fileX.txt");

            Assert.AreEqual(2, dictExpertises.Count);   // franz and heinz
            Assert.IsTrue(aboutEqual(5d / 24d + 1d / 8d, dictExpertises["franz"]));
            Assert.IsTrue(aboutEqual(5d / 24d, dictExpertises["heinz"]));
        }

        public static bool aboutEqual(double value1, double value2)
        {
            return value1 * 1.0000001d > value2 && value1 * 0.9999999d < value2;
        }
    }
}
