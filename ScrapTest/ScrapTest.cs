using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCore.CmdLine;

namespace ScrapTest
{
    class Tester : ICmdLineDispatch
    {
        public enum TestMethod
        {
            Unknown,
            GenericISBN,
            GenericUPC
        }

        private TestMethod m_tm;
        private string m_sTestArg;
        private string m_sPassword;

        public bool FDispatchCmdLineSwitch(CmdLineSwitch cls, string sParam, object oClient, out string sError)
        {
            sError = "";

            if (cls.Switch == "I")
                m_tm = TestMethod.GenericISBN;
            else if (cls.Switch == "U")
                m_tm = TestMethod.GenericUPC;
            else if (cls.Switch == "i")
                m_sTestArg = sParam;
            else if (cls.Switch == "pw")
                m_sPassword = sParam;
            
            return true;
        }

        void ConsoleWriteDelegate(string s)
        {
            Console.WriteLine(s);
        }

        public void ParseCmdLine(string[] args)
        {
            CmdLineConfig cfg = new CmdLineConfig(new CmdLineSwitch[]
                                                      {
                                                      new CmdLineSwitch("I", true, false, "Generic ISBN scrape", "Generic ISBN", null),
                                                      new CmdLineSwitch("U", true, false, "Generic UPC scrape", "Generic UPC", null),
                                                      new CmdLineSwitch("i", false, false, "test input", "input", null),
                                                      new CmdLineSwitch("pw", false, false, "password", "password", null),
                                                      });

            CmdLine cmdLine = new CmdLine(cfg);

            string sError;

            if (!cmdLine.FParse(args, this, null, out sError))
                throw new Exception(sError);

            if (m_tm == TestMethod.Unknown)
                {
                cmdLine.Usage(ConsoleWriteDelegate);
                }

        }

        public void Run(string[] args)
        {
            ParseCmdLine(args);
            string sResult = "";

            if (m_tm == TestMethod.Unknown)
                return;

            switch (m_tm)
                {
                case TestMethod.GenericUPC:
                    sResult = TCore.Scrappy.GenericUPC.FetchTitleFromUPC(m_sTestArg);
                    break;
                case TestMethod.GenericISBN:
                    sResult = TCore.Scrappy.GenericISBN.FetchTitleFromISBN13(m_sTestArg, m_sPassword);
                    break;
                }
            Console.WriteLine("Test method returned: {0}", sResult);
        }
    }
}
