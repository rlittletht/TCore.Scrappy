using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCore.CmdLine;
using TCore.Scrappy.BarnesAndNoble;

namespace ScrapTest
{
    class Tester : ICmdLineDispatch
    {
        public enum TestMethod
        {
            Unknown,
            GenericISBN,
            GenericUPC,
            BarnesAndNoble_DVD,
            BarnesAndNoble_Book,
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
            else if (cls.Switch == "D")
                m_tm = TestMethod.BarnesAndNoble_DVD;
            else if (cls.Switch == "B")
                m_tm = TestMethod.BarnesAndNoble_Book;
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
            CmdLineConfig cfg = new CmdLineConfig(
                new CmdLineSwitch[]
                {
                    new CmdLineSwitch("I", true, false, "Generic ISBN scrape", "Generic ISBN", null),
                    new CmdLineSwitch("U", true, false, "Generic UPC scrape", "Generic UPC", null),
                    new CmdLineSwitch("i", false, false, "test input", "input", null),
                    new CmdLineSwitch("pw", false, false, "password", "password", null),
                    new CmdLineSwitch("D", true, false, "Scrape DVD Info", "DVD", null),
                    new CmdLineSwitch("B", true, false, "Scrape Book Info", "Book", null),
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

        void CallBN_DVD(string sParam)
        {
            TCore.Scrappy.BarnesAndNoble.DVD.DvdElement dvd = new DVD.DvdElement(sParam);
            DVD.ScrapeSet set;

            string sError;

            if (DVD.FScrapeDvd(dvd, out set, out sError))
            {
                Console.WriteLine("\nDVD:");
                Console.WriteLine("ScanCode: {0}", dvd.ScanCode);
                Console.WriteLine("Title: {0}", dvd.Title);
                Console.WriteLine("Summary: {0}", dvd.Summary);
                Console.WriteLine("Classification: {0}", dvd.Classification);
                Console.WriteLine("MediaType: {0}", dvd.MediaType);
                Console.WriteLine("CoverSrc: {0}", dvd.CoverSrc);

            }
            else
            {
                Console.WriteLine("DVD Scrape failed: {0}", sError);
            }
        }

        void CallBN_Book(string sParam)
        {
            TCore.Scrappy.BarnesAndNoble.Book.BookElement book = new Book.BookElement(sParam);
            string sError;

            if (Book.FScrapeBook(book, out sError))
            {
                Console.WriteLine("\nBook:");
                Console.WriteLine("ScanCode: {0}", book.ScanCode);
                Console.WriteLine("Title: {0}", book.Title);
                Console.WriteLine("Author: {0}", book.Author);
                Console.WriteLine("Series: {0}", book.Series);
                Console.WriteLine("Release Date: {0}", book.ReleaseDate);
                Console.WriteLine("Summary: {0}", book.Summary);
            }
            else
            {
                Console.WriteLine("Book Scrape failed: {0}", sError);
            }
        }

        public void Run(string[] args)
        {
            ParseCmdLine(args);

            if (m_tm == TestMethod.Unknown)
                return;

            switch (m_tm)
            {
                case TestMethod.GenericUPC:
                    Console.WriteLine("Test method returned: {0}", TCore.Scrappy.GenericUPC.FetchTitleFromUPC(m_sTestArg));
                    break;
                case TestMethod.GenericISBN:
                    Console.WriteLine("Test method returned: {0}", TCore.Scrappy.GenericISBN.FetchTitleFromISBN13(m_sTestArg, m_sPassword));
                    break;
                case TestMethod.BarnesAndNoble_DVD:
                    CallBN_DVD(m_sTestArg);
                    break;
                case TestMethod.BarnesAndNoble_Book:
                    CallBN_Book(m_sTestArg);
                    break;
            }

        }
    }
}
