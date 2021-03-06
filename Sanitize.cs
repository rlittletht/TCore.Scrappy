﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;

namespace TCore.Scrappy
{
    public class Sanitize
    {
        public delegate string SanitizeStringDelegate(string s);

        /*----------------------------------------------------------------------------
        	%%Function: FSanitizeStringCore
        	%%Qualified: TCore.Scrappy.Sanitize.FSanitizeStringCore
        	%%Contact: rlittle
        	
            sanitize the given string s.

            sFilter is a regular expression that will be used to match against the 
            string.

            if fBackwards is true, then we will match the *last* match in the string
            e.g.
                This DVD is the last. DVD
                                     ^^^^
            if fBackwards is set and match is " DVD"

            if fTruncBackwards is true, then return everything AFTER the matched string, otherwise
            return the string *UP TO* the match

                This DVD is the last. DVD

            returns "This DVD is the last." if the match is " DVD", and backwards is true, and truncBackwards is false
            returns " DVD" if the match is "", and backwards is true, and truncBackwards is true
        ----------------------------------------------------------------------------*/
        public static bool FSanitizeStringCore(string s, string sFilter, bool fBackwards, bool fTruncBackwards, out string sNew)
        {
            MatchCollection matches = Regex.Matches(s, sFilter);
            Match match;

            if (matches.Count == 0)
            {
                sNew = s;
                return false;
            }

            if (fBackwards)
                match = matches[matches.Count - 1];
            else
                match = matches[0];

            if (fTruncBackwards)
                sNew = s.Substring(match.Index + match.Length);
            else
                sNew = s.Substring(0, match.Index);

            return true;
        }

        [TestCase("This DVD is the last. DVD", " DVD", true, false, true, "This DVD is the last.")]
        [TestCase("This DVD is the last. DVD", " DVD", true, true, true, "")]
        [TestCase("This DVD is the last. DVD", " DVD", false, true, true, " is the last. DVD")]
        [TestCase("This DVD is the last. DVD", " DVD", false, false, true, "This")]
        [TestCase("This DVD is the last.", " LD", false, false, false, "This DVD is the last.")] // no matches
        [TestCase("DVD: This is a dvd", "DVD: ", false, true, true, "This is a dvd")] // match the start of the string
        [Test]
        public static void TestFSanitizeStringCore(
            string sIn,
            string sFilter,
            bool fBackwards,
            bool fTruncBackwards,
            bool fExpectedResult,
            string sExpectedResult)
        {
            string sResult;
            bool fResult = FSanitizeStringCore(sIn, sFilter, fBackwards, fTruncBackwards, out sResult);

            Assert.AreEqual(fExpectedResult, fResult);
            Assert.AreEqual(sExpectedResult, sResult);
        }

        public static string SanitizeStringCore(string s, string sFilter, bool fBackwards, bool fTruncBackwards)
        {
            string sNew;
            FSanitizeStringCore(s, sFilter, fBackwards, fTruncBackwards, out sNew);
            return sNew;
        }

        public static string SanitizeString(string sTitle, bool fTitle = true)
        {
            // try to get rid of the fluff...
            if (fTitle)
            {
                sTitle = SanitizeStringCore(sTitle, " \\(DVD\\)", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\[DVD", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\[Blu-ray", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\(Widescreen", false, false);
                sTitle = SanitizeStringCore(sTitle, "\\(Widescreen", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\(Special Edition\\)", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\(Blu-ray", false, false);
                sTitle = SanitizeStringCore(sTitle, " Blu-ray", false, false);
                sTitle = SanitizeStringCore(sTitle, " Bluray", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\(Bluray", false, false);
                sTitle = SanitizeStringCore(sTitle, " DVD ", false, false);
                sTitle = SanitizeStringCore(sTitle, " DVD", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\[Includes Digital", false, false);
                sTitle = SanitizeStringCore(sTitle, " \\[3 Discs", false, false);
            }

            sTitle = SanitizeStringCore(sTitle, "THE NEW YORK TIMES BESTSELLER!+", false, true);
            sTitle = SanitizeStringCore(sTitle, "THE USA TODAY BESTSELLER!+", false, true);
            sTitle = SanitizeStringCore(sTitle, "Overview[\r\n]*", false, true);
            sTitle = SanitizeStringCore(sTitle, "[\r\n]+Advertising", true, false);

            // trim leading and trailing whitespace
            sTitle = sTitle.Trim();
            // remove any qutoes around the whole string
            if ((sTitle.StartsWith("\"") || sTitle.StartsWith("'"))
                && sTitle[sTitle.Length - 1] == sTitle[0])
            {
                sTitle = sTitle.Substring(1, sTitle.Length - 2);
            }

            // and trim one last time
            sTitle = sTitle.Trim();
            return sTitle;
        }

        public static string SanitizeTitle(string sTitle)
        {
            return SanitizeString(sTitle, true);
        }

        public static string SanitizeSeries(string s)
        {
            int start = s.IndexOf("Series:\n") + "series:\n".Length;
            string substr = s.Substring(start);
            int end = substr.IndexOf("\n\n");
            return (substr.Substring(0, end)).Replace("\n", string.Empty);
        }

        public static string SanitizeDate(string s)
        {
            int start = s.IndexOf("Publication date:\n") + "Publication Date:\n".Length;
            string substr = s.Substring(start);
            int end = substr.IndexOf("\n");
            return (substr.Substring(0, end)).Replace("\n", string.Empty);
        }

        public static string SanitizeCoverUrl(string s)
        {
            if (s.StartsWith("//"))
                return s.Substring(2);

            return null;
        }


        public static string SanitizeSummary(string sSummary)
        {
            return SanitizeString(sSummary, false);
        }

        [Test]
        [TestCase("Test", true, "Test")]
        [TestCase("Test DVD ", true, "Test")]
        [TestCase("Test", true, "Test")]
        [TestCase("Test Starring Bob Smith", false, "Test Starring Bob Smith")]
        [TestCase("Overview\r\n\r\n\r\n\r\nRichard", false, "Richard")]
        [TestCase("Overview\r\n\n\r\n\r\n\r\nRichard", false, "Richard")]
        [TestCase("Festival.\r\n\r\n\r\nAdvertising\r\n$(document)", false, "Festival.")]
        [TestCase("Festival.\r\n\n\r\n\r\nAdvertising\r\n$(document)", false, "Festival.")]
        [TestCase("Overview\r\n\r\n\r\n\r\nRichard Festival.\r\n\r\n\r\nAdvertising\r\n$(document)", false, "Richard Festival.")]
        [TestCase("Overview\n\r\n\r\n\r\nRichard Festival.\r\n\r\n\r\nAdvertising\r\n$(document)", false, "Richard Festival.")]
        [TestCase("Overview\r\r\n\r\n\r\n\r\nRichard Festival.\r\n\r\n\r\nAdvertising\r\n$(document)", false, "Richard Festival.")]
        [TestCase(
            "Overview\r\n\r\n\r\n\r\nRichard worthwhile.\n\n\n\nAdvertising\n\n\n$(document).on('googleRelatedAdsEnabled Advertising', function() {",
            false,
            "Richard worthwhile.")]
        [TestCase("Summers (DVD)", true, "Summers")]
        [TestCase("Box (Special Edition)", true, "Box")]
        [TestCase("Lucy (Blu-ray +", true, "Lucy")]
        [TestCase("Lucy (Blu-ray/DVD/Digital HD)", true, "Lucy")]
        [TestCase("The Visit (Blu-ray + DIGITAL HD)", true, "The Visit")]
        [TestCase("I Am Legend DVD", true, "I Am Legend")]
        [TestCase("Boyhood (Blu-ray +", true, "Boyhood")]
        [TestCase("Stargate, Ark DVD", true, "Stargate, Ark")]
        [TestCase("Testing Blu-ray + DVD + Something else", true, "Testing")]
        [TestCase("Killer (Bluray/DVD Combo) [Blu-ray]", true, "Killer")]
        [TestCase("Session 9 [DVD] [English] [2001]", true, "Session 9")]
        [TestCase("Frontier(s) [DVD] [French] [2007]", true, "Frontier(s)")]
        [TestCase("Independence Day: Resurgence [Blu-ray/DVD]", true, "Independence Day: Resurgence")]
        [TestCase("Star Trek Beyond [Includes Digital Copy] [Blu-ray/DVD]", true, "Star Trek Beyond")]
        [TestCase("Martyrs [DVD] [2008] [Region 1] [US Import] [NTSC]", true, "Martyrs")]
        [TestCase("The Hobbit: The Desolation of Smaug [3 Discs] [Blu-ray/DVD]", true, "The Hobbit: The Desolation of Smaug")]
        [TestCase("Salt (Unrated) (Deluxe Extended Edition) (Blu-ray) (With INSTAWA", true, "Salt (Unrated) (Deluxe Extended Edition)")]
        [TestCase("\"This is the story\"", true, "This is the story")]
        [TestCase("\r\n\"This story\"\r\n", true, "This story")]
        [TestCase("\"\r\nThis story\r\n\"", true, "This story")]
        [TestCase("THE NEW YORK TIMES BESTSELLER!\nTesting", true, "Testing")]
        [TestCase("", true, "")]
        [TestCase("", true, "")]
        [TestCase("", true, "")]
        [TestCase("", true, "")]
        public static void TestSanitizeString(string sIn, bool fTitle, string sExpected)
        {
            Assert.AreEqual(sExpected, SanitizeString(sIn, fTitle));
        }

        public static string SanitizeMediaType(string sMediaType)
        {
            if (sMediaType != null)
            {
                sMediaType = sMediaType.ToLower();

                if (sMediaType.IndexOf("blu-ray", StringComparison.Ordinal) >= 0)
                    return "BLU-RAY";

                if (sMediaType.IndexOf("dvd", StringComparison.Ordinal) >= 0)
                    return "DVD";

                if (sMediaType.IndexOf("laserdisc", StringComparison.Ordinal) >= 0)
                    return "LD";
            }

            return "";
        }

        [Test]
        [TestCase("DVD", "DVD")]
        [TestCase("dvd", "DVD")]
        [TestCase("--DVD", "DVD")]
        [TestCase("DVD--", "DVD")]
        [TestCase("blu-ray", "BLU-RAY")]
        [TestCase("laserdisc", "LD")]
        [TestCase("DVD BLU-RAY", "BLU-RAY")]
        [TestCase("blu-ray DVD laserdisc", "BLU-RAY")]
        [TestCase("", "")]
        public static void TestSanitizeMediaType(string sInput, string sExpected)
        {
            Assert.AreEqual(sExpected, SanitizeMediaType(sInput));
        }

        static Dictionary<string, string> s_mpGeneric = new Dictionary<string, string>
        {
            {"comedy", "Comedy"},
            {"action", "Action"},
            {"adventure", "Adventure"},
            {"thriller", "Thriller"},
            {"fantasy", "Fantasy"},
            {"horror", "Horror"},
            {"family", "Family"},
            {"sci-fi", "Sci-Fi"},
            {"drama", "Drama"},
            {"mystery", "Mystery"},
            {"science fiction", "Sci-Fi"},
            {"historical", "Historical"}
        };

        static string[] s_rgsIgnore = new string[]
        {
            "Children", "Parody", "Period", "Prehistoric", "Superhero", "Sword", "UK", "War", "French", "Farce", "Costume",
            "Coming of Age", "Animation", "Alien", "Feature", "Future", "Gay/Lesbian", "Ireland", "Monster", "New Zealand", "Occult",
            "Military", "Supernatural", "Television", "Armed Forces", "Commandos", "Literary"
        };

        public static List<string> SanitizeClassList(List<string> pls)
        {
            HashSet<string> classes = new HashSet<string>();

            List<string> plsNew = new List<string>();

            foreach (string sCheck in pls)
            {
                string sCanon = sCheck.ToLower();
                bool fGotGeneric = false;

                foreach (string sGeneric in s_mpGeneric.Keys)
                {
                    if (sCanon.Contains(sGeneric))
                    {
                        fGotGeneric = true;
                        classes.Add(s_mpGeneric[sGeneric]);
                    }
                }

                if (fGotGeneric)
                    continue; // don't look for specifics if we got one or more generics

                bool fIgnore = false;

                foreach (string sIgnore in s_rgsIgnore)
                    if (sCanon.Contains(sIgnore.ToLower()))
                        fIgnore = true;

                if (fIgnore)
                    continue;

// fallthrough
                classes.Add(sCheck);
            }

            foreach (string s in classes)
                plsNew.Add(s);

            return plsNew;
        }

        static List<string> PlsFromString(string s)
        {
            if (s == "")
                return new List<string>();

            return new List<string>(s.Split('|'));
        }

        [Test]
        [TestCase("Comedy", "Comedy")]
        [TestCase("Anarchic Comedy", "Comedy")]
        [TestCase("Comedy Action", "Comedy|Action")]
        [TestCase("Comedy Action|Unknown", "Comedy|Action|Unknown")]
        [TestCase("Sci-Fi|Thriller|Monster|Alien", "Sci-Fi|Thriller")]
        [TestCase("Action Thriller", "Action|Thriller")]
        [TestCase("Adventure Drama", "Adventure|Drama")]
        [TestCase("Aliens", "")]
        [TestCase("Anarchic Comedy", "Comedy")]
        [TestCase("Animation", "")]
        [TestCase("Animation - Features", "")]
        [TestCase("Animation - Kids", "")]
        [TestCase("Biographical Feature", "")]
        [TestCase("Children", "")]
        [TestCase("Children - Animation", "")]
        [TestCase("Comedy - General", "Comedy")]
        [TestCase("Comedy - Teen", "Comedy")]
        [TestCase("Comedy Adventure", "Comedy|Adventure")]
        [TestCase("Coming of Age", "")]
        [TestCase("Computer Animation", "")]
        [TestCase("Costume Adventure", "Adventure")]
        [TestCase("Drama - General", "Drama")]
        [TestCase("Family", "Family")]
        [TestCase("Fantasy Adventure", "Fantasy|Adventure")]
        [TestCase("Fantasy Comedy", "Fantasy|Comedy")]
        [TestCase("Farce", "")]
        [TestCase("French", "")]
        [TestCase("Future Dystopias", "")]
        [TestCase("Gay/Lesbian", "")]
        [TestCase("Historical Film", "Historical")]
        [TestCase("Ireland", "")]
        [TestCase("Monster", "")]
        [TestCase("New Zealand", "")]
        [TestCase("Occult Horror", "Horror")]
        [TestCase("Parody (spoof)", "")]
        [TestCase("Period Drama", "Drama")]
        [TestCase("Prehistoric Fantasy", "Fantasy")]
        [TestCase("Psychological Thriller", "Thriller")]
        [TestCase("Romantic Mystery", "Mystery")]
        [TestCase("Sci-Fi Action", "Sci-Fi|Action")]
        [TestCase("Sci-Fi, Fantasy & Horror - International", "Sci-Fi|Fantasy|Horror")]
        [TestCase("Superhero", "")]
        [TestCase("Sword-and-Sorcery", "")]
        [TestCase("UK", "")]
        [TestCase("War Drama", "Drama")]
        [TestCase("", "")]
        [TestCase("", "")]
        [TestCase("", "")]
        [TestCase("", "")]
        public static void TestSanitizeClassList(string sInput, string sExpected)
        {
            List<string> plsInput = PlsFromString(sInput);
            List<string> plsExpected = PlsFromString(sExpected);

            plsInput = SanitizeClassList(plsInput);
            plsInput.Sort();
            plsExpected.Sort();

            Assert.AreEqual(plsExpected, plsInput);
        }
    }
}