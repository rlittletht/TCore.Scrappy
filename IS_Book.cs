using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;
using System.Xml;
using ScrapySharp.Network;

namespace TCore.Scrappy.IsbnSearch
{
    // ============================================================================
    // B O O K
    // ============================================================================
    public class Book
    {
        [Flags]
        public enum ScrapeSet
        {
            Title = 0x0020
        }

        // ============================================================================
        // B O O K  E L E M E N T
        //
        // Holds all the information about a book
        // ============================================================================
        public class BookElement
        {
            private string m_sScanCode;
            private string m_sTitle;
            private string m_sAuthor;
            private string m_sSeries;
            private string m_sReleaseDate;
            private string m_sSummary;
            private string m_sRawCoverUrl;

            public BookElement(string sScanCode)
            {
                m_sScanCode = sScanCode;
            }

            public BookElement()
            {
                m_sScanCode = null;
            }

            public string RawCoverUrl
            {
                get => m_sRawCoverUrl;
                set => m_sRawCoverUrl = value;
            }

            public string ScanCode
            {
                get { return m_sScanCode; }
                set { m_sScanCode = value; }
            }

            public string Title
            {
                get { return m_sTitle; }
                set { m_sTitle = value; }
            }

            public string Author
            {
                get { return m_sAuthor; }
                set { m_sAuthor = value; } 
            }

            public string Series
            {
                get { return m_sSeries; }
                set { m_sSeries = value; }
            }

            public string ReleaseDate
            {
                get { return m_sReleaseDate; }
                set { m_sReleaseDate = value; }
            }

            public string Summary
            {
                get { return m_sSummary; }
                set { m_sSummary = value; }
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: FUpdateBookInfo
        	%%Qualified: TCore.Scrappy.BarnesAndNoble.Book.FUpdateBookInfo
        	%%Contact: rlittle
        	
            Given a BookElement, go to B&N and scrape all the parts of the BookElement
            that aren't already filled out.  If anything goes wrong, fill in a
            description in sError and return false.

            (NOTE: just because we failed to scrape certain elements, like subjects,
            we won't return failure. some things won't always be there)
        ----------------------------------------------------------------------------*/
        public static bool FScrapeBookSet(BookElement book, out ScrapeSet set, out string sError)
        {
            string sCode;
            
            sCode = book.ScanCode;
            sError = "";
            set = 0;

            try
            {
                WebPage wp = Core.BrowseToSearch(sCode);

                if (FUpdateTitle(book, wp, ref sError))
                    set |= ScrapeSet.Title;
            }
            catch (Exception exc)
            {
                sError = exc.Message;
                if (exc.InnerException != null)
                    sError += " + " + exc.InnerException.Message;
                return false;
            }

            if (set == 0)
                return false;

            return true;
        }

        public static bool FScrapeBook(BookElement book, out ScrapeSet set, out string sError)
        {
            bool f = FScrapeBookSet(book, out set, out sError);

            if (!f || set == 0)
                return false;

            return true;
        }

        static bool FUpdateTitle(BookElement book, WebPage wp, ref string sError)
        {
            book.Title = Core.GetSimpleStringField(book.Title, "//div[@class='bookinfo']/h1", wp, Sanitize.SanitizeTitle, ref sError, out bool fSetValue);
            return fSetValue;
        }
    }
}