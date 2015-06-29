using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExpertiseExplorer.Algorithms.FPS;
using System.Collections.Generic;
using ExpertiseExplorer.ExpertiseDB.Extensions;
using System.Linq;

namespace ExpertiseExplorer.Algorithms.Test
{
    [TestClass]
    public class FPSTreeTest
    {
        [TestMethod]
        public void ComplexTest()
        {
            RootDirectory root = new RootDirectory();

            root.AddReview(1, new string[] { "fileA.txt", "a/b/c/fileA.txt", "a/b/b/fileX.txt" });
            root.AddReview(1, new string[] { "fileA.txt", "a/b/b/fileX.txt" });
            root.AddReview(2, new string[] { "fileA.txt", "a/b/c/fileA.txt", "a/b/b/fileX.txt" });
            root.AddReview(3, new string[] { "c/b/a/fileX.txt", "c/b/a/fileY.txt" });
            root.AddReview(2, new string[] { "c/b/a/fileX.txt" });

            IDictionary<int, double> dictExpertises = root.CalculateDeveloperExpertisesForFile("fileA.txt")
                .ToDictionary(dev => dev.DeveloperId, dev => dev.Expertise);

            Assert.AreEqual(2, dictExpertises.Count);   // 1 and 2
            Assert.IsTrue(aboutEqual(1d / 3d + 1d / 2d, dictExpertises[1]));
            Assert.IsTrue(aboutEqual(1d/3d, dictExpertises[2]));

            dictExpertises = root.CalculateDeveloperExpertisesForFile("c/b/a/fileX.txt")
                .ToDictionary(dev => dev.DeveloperId, dev => dev.Expertise);

            Assert.AreEqual(2, dictExpertises.Count);   // 3 and 2
            Assert.IsTrue(aboutEqual(1d / 2d + 1d/2d * 3d/4d, dictExpertises[3]));    // some indirection already
            Assert.IsTrue(aboutEqual(1d, dictExpertises[2]));

            dictExpertises = root.CalculateDeveloperExpertisesForFile("a/b/c/d/e/f/g/fileX.txt")
                .ToDictionary(dev => dev.DeveloperId, dev => dev.Expertise);

            Assert.AreEqual(2, dictExpertises.Count);   // 1 and 2
            Assert.IsTrue(aboutEqual(5d / 24d + 1d / 8d, dictExpertises[1]));
            Assert.IsTrue(aboutEqual(5d / 24d, dictExpertises[2]));
        }

        public static bool aboutEqual(double value1, double value2)
        {
            return value1 * 1.0000001d > value2 && value1 * 0.9999999d < value2;
        }
    }
}
