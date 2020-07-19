// for help scraping script contents

using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using NUnit.Framework;

// ReSharper disable StringIndexOfIsCultureSpecific.1
// ReSharper disable StringIndexOfIsCultureSpecific.2

namespace TCore.Scrappy
{
    enum StateBraceParse
    {
        Scanning,
        ParsingEscape,
        SingleQuoted,
        DoubleQuoted
    }

    class ScrapeScript
    {
        /*----------------------------------------------------------------------------
        	%%Function: FProcessedParseScanning
        	%%Qualified: TCore.Scrappy.ScrapeScript.FProcessedParseScanning
        	
        ----------------------------------------------------------------------------*/
        static bool FProcessedParseScanning(Stack<StateBraceParse> parserStack, char ch)
        {
            if (parserStack.Peek() != StateBraceParse.Scanning)
                throw new Exception("top of stack is not Scanning");

            if (ch == '\'' || ch == '"')
            {
                parserStack.Push(ch == '"' ? StateBraceParse.DoubleQuoted : StateBraceParse.SingleQuoted);
            }
            else if (ch == '{')
                parserStack.Push(parserStack.Peek());   // we just have a deeper level of the same thing...
            else if (ch == '}')
                parserStack.Pop();

            return true; // we consumed this token
        }

        /*----------------------------------------------------------------------------
        	%%Function: FProcessedParseQuoted
        	%%Qualified: TCore.Scrappy.ScrapeScript.FProcessedParseQuoted
        	
        ----------------------------------------------------------------------------*/
        static bool FProcessedParseQuoted(Stack<StateBraceParse> parserStack, char ch)
        {
            if (parserStack.Peek() != StateBraceParse.DoubleQuoted && parserStack.Peek() != StateBraceParse.SingleQuoted)
                throw new Exception("top of stack is not Quoted");

            char chQuote = parserStack.Peek() == StateBraceParse.DoubleQuoted ? '"' : '\'';

            if (ch == '\\')
                parserStack.Push(StateBraceParse.ParsingEscape);
            else if (ch == chQuote)
                parserStack.Pop();

            return true; // we consumed this token
        }

        /*----------------------------------------------------------------------------
        	%%Function: FProcessedParseEscaped
        	%%Qualified: TCore.Scrappy.ScrapeScript.FProcessedParseEscaped
        	
        ----------------------------------------------------------------------------*/
        static bool FProcessedParseEscaped(Stack<StateBraceParse> parserStack, char ch)
        {
            if (parserStack.Peek() != StateBraceParse.ParsingEscape)
                throw new Exception("top of stack is not ParsingEscape");

            // for now, any character following the backslash is the end of the escape
            // (future: \x## ?)

            parserStack.Pop();
            return true; // we consumed this token
        }

        /*----------------------------------------------------------------------------
        	%%Function: ExtractJsonValue
        	%%Qualified: TCore.Scrappy.ScrapeScript.ExtractJsonValue
        	
        ----------------------------------------------------------------------------*/
        public static string ExtractJsonStringValue(string sRaw, string sVarName)
        {
            Stack<StateBraceParse> parserStack = new Stack<StateBraceParse>();

            int ichDigitalData = sRaw.IndexOf(sVarName);
            if (ichDigitalData < 0)
                return null;

            int ich = sRaw.IndexOf("{", ichDigitalData);

            if (ich < 0)
                return null;

            int ichValueStart = ich;
            ich++;
            parserStack.Push(StateBraceParse.Scanning);

            while (ich < sRaw.Length)
            {
                if (parserStack.Count == 0)
                    break;

                StateBraceParse state = parserStack.Peek();

                if (state == StateBraceParse.Scanning)
                {
                    if (FProcessedParseScanning(parserStack, sRaw[ich]))
                    {
                        ich++;
                        continue;
                    }
                    // otherwise fallthrough
                    state = parserStack.Peek();
                }

                if (state == StateBraceParse.SingleQuoted || state == StateBraceParse.DoubleQuoted)
                {
                    if (FProcessedParseQuoted(parserStack, sRaw[ich]))
                    {
                        ich++;
                        continue;
                    }
                    // otherwise fallthrough
                    state = parserStack.Peek();
                }

                if (state == StateBraceParse.ParsingEscape)
                {
                    parserStack.Pop();
                    ich++;
                    continue;
                }

                throw new Exception("cannot fallthrough to bottom of parser loop");
            }

            return sRaw.Substring(ichValueStart, ich - ichValueStart);
        }

        public static T ExtractJsonValue<T>(string sRaw, string sVarName) where T: new()
        {
            string sJson = ExtractJsonStringValue(sRaw, sVarName);

            if (sJson == null)
                return default(T);

            JavaScriptSerializer jsc = new JavaScriptSerializer();

            return jsc.Deserialize<T>(sJson);
        }

        #region Tests


        [TestCase("var foo={ \"bar\": \"baz\" }", "foo", "{ \"bar\": \"baz\" }")]
        [TestCase("var foo={ \"bar\": \"baz\", \"boo\": { \"boo\": 10 } }", "foo", "{ \"bar\": \"baz\", \"boo\": { \"boo\": 10 } }")]
        [Test]
        public static void Test_ExtractJsonStringValue(string sInput, string sVarName, string sExpected)
        {
            string sResult = ExtractJsonStringValue(sInput, sVarName);

            Assert.AreEqual(sExpected, sResult);
        }

        class ExtractTestClass
        {
            public string bar;
        }

        [Test]
        public static void Test_ExtractJsonValueSimple()
        {
            ExtractTestClass result = ExtractJsonValue<ExtractTestClass>("var foo={ \"bar\": \"baz\" }", "foo");

            Assert.AreEqual(result.bar, "baz");
        }

        [Test]
        public static void Test_ExtractJsonValueSimpleExtraValuesIgnored()
        {
            ExtractTestClass result = ExtractJsonValue<ExtractTestClass>("var foo={ \"bar\": \"baz\",  \"boo\": { \"boo\": 10 } }", "foo");

            Assert.AreEqual(result.bar, "baz");
        }

        class ExtractTestClassOuter
        {
            public class ExtractTestClassInner
            {
                public int boo;
            }

            public string bar;
            public ExtractTestClassInner boo;
        }

        [Test]
        public static void Test_ExtractJsonValueComplex()
        {
            ExtractTestClassOuter result = ExtractJsonValue<ExtractTestClassOuter>("var foo={ \"bar\": \"baz\",  \"boo\": { \"boo\": 10 } }", "foo");

            Assert.AreEqual(result.bar, "baz");
            Assert.AreEqual(result.boo.boo, 10);
        }

        class ExtractTestClass2Outer
        {
            public class ExtractTestClass2Inner
            {
                public int boo;
            }

            public List<ExtractTestClass2Inner> boo;
            public string bar;
        }
        [Test]
        public static void Test_ExtractJsonValueComplexListExtraIgnored()
        {
            ExtractTestClass2Outer result = ExtractJsonValue<ExtractTestClass2Outer>
                ("var foo={ \"bar\": \"baz\",  "
                 + "\"boo\": [{ \"boo\": 10 }] }", "foo");

            Assert.AreEqual(result.bar, "baz");
            Assert.AreEqual(result.boo[0].boo, 10);
        }

        // ***** NOTE ****** The initializer for the stack expects an array of values TO PUSH. The AreEqual()
        // comparison expects an array of values AS POPPED. This means that the initializer and expected are mirrored

        // Initial = {A, B, C}
        // with a null op
        // Expected = {C, B, A}

        [TestCase(new[] { StateBraceParse.Scanning }, 'a', new[] { StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning }, '"', new[] { StateBraceParse.DoubleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning }, '\'', new[] { StateBraceParse.SingleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning }, '{', new[] { StateBraceParse.Scanning, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.Scanning }, '}', new[] { StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning }, '}', new StateBraceParse[] { }, true)]
        [Test]
        public static void Test_FProcessedParseScanning(StateBraceParse[] parserStateInitial, char chInput, StateBraceParse[] parserStateExpected, bool fExpected)
        {
            // setup the parser stack
            Stack<StateBraceParse> parserStack = new Stack<StateBraceParse>(parserStateInitial);

            bool fActual = FProcessedParseScanning(parserStack, chInput);
            Assert.AreEqual(fExpected, fActual);
            Assert.AreEqual(parserStateExpected, parserStack);
        }

        [TestCase(new StateBraceParse[] { })]
        [TestCase(new[] { StateBraceParse.DoubleQuoted })]
        [TestCase(new[] { StateBraceParse.SingleQuoted })]
        [TestCase(new[] { StateBraceParse.ParsingEscape })]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.ParsingEscape })]
        [Test]
        public static void Test_FProcessedParseScanning_BadParserState_WillThrow(StateBraceParse[] parserStateInitial)
        {
            // setup the parser stack
            Stack<StateBraceParse> parserStack = new Stack<StateBraceParse>(parserStateInitial);
            bool fCaught = false;

            try
            {
                bool fActual = FProcessedParseScanning(parserStack, ' ');
            }
            catch
            {
                fCaught = true;
            }

            Assert.AreEqual(true, fCaught);
        }

        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.DoubleQuoted }, 'a', new[] { StateBraceParse.DoubleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.DoubleQuoted }, '\'', new[] { StateBraceParse.DoubleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.DoubleQuoted }, '\\', new[] { StateBraceParse.ParsingEscape, StateBraceParse.DoubleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.DoubleQuoted }, '"', new[] { StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.SingleQuoted }, 'a', new[] { StateBraceParse.SingleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.SingleQuoted }, '"', new[] { StateBraceParse.SingleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.SingleQuoted }, '\\', new[] { StateBraceParse.ParsingEscape, StateBraceParse.SingleQuoted, StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.SingleQuoted }, '\'', new[] { StateBraceParse.Scanning }, true)]
        [Test]
        public static void Test_FProcessedParseQuoted(StateBraceParse[] parserStateInitial, char chInput, StateBraceParse[] parserStateExpected, bool fExpected)
        {
            // setup the parser stack
            Stack<StateBraceParse> parserStack = new Stack<StateBraceParse>(parserStateInitial);

            bool fActual = FProcessedParseQuoted(parserStack, chInput);
            Assert.AreEqual(fExpected, fActual);
            Assert.AreEqual(parserStateExpected, parserStack);
        }

        [TestCase(new StateBraceParse[] { })]
        [TestCase(new[] { StateBraceParse.DoubleQuoted, StateBraceParse.Scanning })]
        [TestCase(new[] { StateBraceParse.SingleQuoted, StateBraceParse.Scanning })]
        [TestCase(new[] { StateBraceParse.ParsingEscape })]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.ParsingEscape })]
        [Test]
        public static void Test_FProcessedParseQuoted_BadParserState_WillThrow(StateBraceParse[] parserStateInitial)
        {
            // setup the parser stack
            Stack<StateBraceParse> parserStack = new Stack<StateBraceParse>(parserStateInitial);
            bool fCaught = false;

            try
            {
                bool fActual = FProcessedParseQuoted(parserStack, ' ');
            }
            catch
            {
                fCaught = true;
            }

            Assert.AreEqual(true, fCaught);
        }

        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.ParsingEscape }, 'a', new[] { StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.ParsingEscape }, '\\', new[] { StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.ParsingEscape }, 'x', new[] { StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.ParsingEscape }, '\'', new[] { StateBraceParse.Scanning }, true)]
        [TestCase(new[] { StateBraceParse.Scanning, StateBraceParse.ParsingEscape }, '"', new[] { StateBraceParse.Scanning }, true)]
        [Test]
        public static void Test_FProcessedParseEscaped(StateBraceParse[] parserStateInitial, char chInput, StateBraceParse[] parserStateExpected, bool fExpected)
        {
            // setup the parser stack
            Stack<StateBraceParse> parserStack = new Stack<StateBraceParse>(parserStateInitial);

            bool fActual = FProcessedParseEscaped(parserStack, chInput);
            Assert.AreEqual(fExpected, fActual);
            Assert.AreEqual(parserStateExpected, parserStack);
        }

        [TestCase(new StateBraceParse[] { })]
        [TestCase(new[] { StateBraceParse.DoubleQuoted })]
        [TestCase(new[] { StateBraceParse.SingleQuoted })]
        [TestCase(new[] { StateBraceParse.Scanning })]
        [Test]
        public static void Test_FProcessedParseEscaped_BadParserState_WillThrow(StateBraceParse[] parserStateInitial)
        {
            // setup the parser stack
            Stack<StateBraceParse> parserStack = new Stack<StateBraceParse>(parserStateInitial);
            bool fCaught = false;

            try
            {
                bool fActual = FProcessedParseEscaped(parserStack, ' ');
            }
            catch
            {
                fCaught = true;
            }

            Assert.AreEqual(true, fCaught);
        }

        #endregion
    }
}