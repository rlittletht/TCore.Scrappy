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
using HtmlAgilityPack;
using ScrapySharp.Network;

namespace TCore.Scrappy.BarnesAndNoble
{
    // ============================================================================
    // B O O K
    // ============================================================================
    public class Book
    {
        // ============================================================================
        // B O O K  E L E M E N T
        //
        // Holds all the information about a book
        // ============================================================================
        public class BookElement
        {
            private string m_sScanCode;
            private string m_sTitle;

            public BookElement(string sScanCode)
            {
                m_sScanCode = sScanCode;
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
        public static bool FScrapeBook(BookElement book, out string sError)
        {
            string sCode;

            sCode = book.ScanCode;
            sError = "";

            try
                {
                ScrapingBrowser sbr = new ScrapingBrowser();
                sbr.AllowAutoRedirect = true;
                sbr.AllowMetaRedirect = true;

                WebPage wp = sbr.NavigateToPage(new Uri("https://www.barnesandnoble.com/s/" + sCode));

                if (!FUpdateTitle(book, wp, ref sError))
                    {
                    return false;
                    }
            }
            catch (Exception exc)
                {
                sError = exc.Message;
                return false;
                }
            return true;
        }

        static bool FUpdateTitle(BookElement book, WebPage wp, ref string sError)
        {
            if (String.IsNullOrEmpty(book.Title))
                {
                HtmlNode node = wp.Html.SelectSingleNode("//section[@id='prodSummary']/h1");

                if (node == null)
                    {
                    sError = "Couldn't find title";
                    return false;
                    }

                book.Title = Sanitize.SanitizeTitle(node.InnerText);
                }
            return true;
        }
    }
}