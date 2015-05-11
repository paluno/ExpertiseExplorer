using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Algorithms.Statistics;
using System.Linq;
using System.Collections.Generic;

namespace AlgorithmsTest
{
    [TestClass]
    public class StatisticsTest
    {
        const string nameMappingTest =
            "u2n;email@address;12;0;Full Name=9:2002.55769230769:2003.71153846154\n" +
            "u2n;address2@test;14;0;Full Name=12:2005.71153846154:2007.32692307692\n" +
            "n2u;Full Name;5;0;address3@test=5:2007.30769230769:2007.32692307692\n" +
            "n2u;Another Name;2;0;address2@test=2:2004.78846153846:2004.78846153846\n" +
            "n2u;Not Related;2;0;unrelated@test=2:2004.36538461538:2004.3653846";

        const string authorMultiList =
            "Full Name <email@address>,Full Name <address2@test>,address3@test>\n" +
            "Not Related,Absoluteley Not Related,<unrelated@test>";

        const string authorTestset =
            "Full Name <email@address>\n" +
            "Unknown Name <address3@test>\n" +
            "Full Name <address4@test>\n" +
            "Not Related\n" +
            "unrelated@test\n" +
            "Completely different\n" +
            "Completely different van mail address [:cdma] <unmapped@address>";

        const string authorTestset2 =
            "Full Name <email@address>\n" +
            "Not Related <strangely@formatted\n" +
            "Not Related\n" +
            "Completely different [cdma]\n" +
            "Completely different van mail address [:cdma] <unmapped@address>";

        const string incompleteMailTestset =
            "Person X <email.address@that.is.long>\n" +
            "Person X <email.address>";

        const string nameMatchedTestset =
            "Glenn Randers-Pehrson <glennrp@gmail.com>\n" +
            "Glenn Randers-Pehrson <glennrp+bmo@gmail.com>";

        [TestMethod]
        public void TestSimpleAliasingWithNames()
        {
            AliasFinder af = new AliasFinder();
            af.InitializeMappingFromNames(nameMappingTest.Split('\n'));

            string[] names = af.Consolidate(authorTestset.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            Assert.AreEqual(4, names.Length);
            Assert.AreEqual("Completely different", names[0]);
            Assert.AreEqual("Completely different van mail address [:cdma] <unmapped@address>", names[1]);
        }

        [TestMethod]
        public void TestSimpleAliasingWithNames2()
        {
            AliasFinder af = new AliasFinder();
            af.InitializeMappingFromNames(nameMappingTest.Split('\n'));

            string[] names = af.Consolidate(authorTestset2.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            Assert.AreEqual(3, names.Length);
            Assert.IsTrue(names[0].Contains("Completely different [cdma]"));
            Assert.IsTrue(names[0].Contains("Completely different van mail address [:cdma] <unmapped@address>"));
            Assert.AreEqual(2, names[0].Split(',').Length);
        }

        [TestMethod]
        public void TestSimpleAliasingWithAuthors()
        {
            AliasFinder af = new AliasFinder();
            af.InitializeMappingFromAuthorList(authorMultiList.Split('\n'));

            string[] names = af.Consolidate(authorTestset.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            Assert.AreEqual(4, names.Length);
            Assert.AreEqual("Completely different", names[0]);
            Assert.AreEqual("Completely different van mail address [:cdma] <unmapped@address>", names[1]);
        }

        [TestMethod]
        public void TestMappingMatch()
        {
            AliasFinder afNames = new AliasFinder();
            afNames.InitializeMappingFromNames(nameMappingTest.Split('\n'));

            AliasFinder afAuthors = new AliasFinder();
            afAuthors.InitializeMappingFromAuthorList(authorMultiList.Split('\n'));

            string[] namesFromNames = afNames.Consolidate(authorTestset.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            string[] namesFromAuthors = afAuthors.Consolidate(authorTestset.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            CollectionAssert.AreEquivalent(namesFromAuthors, namesFromNames);
        }


        [TestMethod]
        public void TestNameDistinction()
        {
            AliasFinder afAuthors = new AliasFinder();
            afAuthors.InitializeMappingFromAuthorList(authorMultiList.Split('\n'));


            string[] namesFromDoubleList = afAuthors.Consolidate((authorTestset + "\n" + authorTestset).Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            string[] namesFromNormalList = afAuthors.Consolidate(authorTestset.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            CollectionAssert.AreEquivalent(namesFromNormalList, namesFromDoubleList);
        }
        
        [TestMethod]
        public void TestIncompleteMails()
        {
            AliasFinder af = new AliasFinder();

            string[] names = af.Consolidate(incompleteMailTestset.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            Assert.AreEqual(1, names.Length);
        }

        [TestMethod]
        public void TestNameMatching()
        {
            AliasFinder af = new AliasFinder();

            string[] names = af.Consolidate(nameMatchedTestset.Split('\n'))
                 .Select(reviewerList => string.Join(",", reviewerList))    // put each reviewer in one string
                 .OrderBy(x => x)                                           // sort the resulting reviewers
                 .ToArray();

            Assert.AreEqual(1, names.Length);
        }

        [TestMethod]
        public void TestAuthorParsing()
        {
            Author a = new Author("Chinese (Joe) Name <oneguy@address.com>");
            Assert.AreEqual("oneguy@address.com", a.MailPart);
//            Assert.IsNull(a.NamePart);      // this cannot really be parsed
            Assert.IsNull(a.LoginNamePart);

            a = new Author("Binde-Strich im Namen <mail@address.example.ende"); // note the missing > character
            Assert.AreEqual("Binde-Strich im Namen", a.NamePart);
            Assert.IsNull(a.LoginNamePart);
            Assert.AreEqual("mail@address.example.ende", a.MailPart);

            a = new Author("only.mail.address@domain.name");
            Assert.IsNull(a.NamePart);
            Assert.IsNull(a.LoginNamePart);
            Assert.AreEqual("only.mail.address@domain.name", a.MailPart);

            a = new Author("Guys Name (:LoginPartName) <mail@address>");
            Assert.AreEqual("Guys Name", a.NamePart);
            Assert.AreEqual("LoginPartName", a.LoginNamePart);
            Assert.AreEqual("mail@address", a.MailPart);

            a = new Author("Scott O'Connor <mail@address>");
            Assert.AreEqual("Scott O'Connor", a.NamePart);
            Assert.IsNull(a.LoginNamePart);
            Assert.AreEqual("mail@address", a.MailPart);
        }
    }
}
