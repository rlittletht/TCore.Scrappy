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
            private string m_sAuthor;
            private string m_sSeries;
            private string m_sReleaseDate;
            private string m_sSummary;

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
                
                if (!FUpdateAuthor(book, wp, ref sError))
                {
                    return false;
                }

                if (!FUpdateReleaseDate(book, wp, ref sError))
                {
                    return false;
                }

                if (!FUpdateSeries(book, wp, ref sError))
                {
                    return false;
                }

                if (!FUpdateSummary(book, wp, ref sError))
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
                HtmlNode node = wp.Html.SelectSingleNode("//h1[@itemprop='name']");
                //HtmlNode node = wp.Html.SelectSingleNode("//section[@id='prodSummary']/h1");
                //HtmlNode node = wp.Html.SelectSingleNode("//div[@id='pdp-header-info']/h1");

                if (node == null)
                    {
                    sError = "Couldn't find title: "+wp.Html.InnerHtml;
                    return false;
                    }

                book.Title = Sanitize.SanitizeTitle(node.InnerText);
            }
            return true;
        }

        static bool FUpdateAuthor(BookElement book, WebPage wp, ref string sError)
        {
            if (String.IsNullOrEmpty(book.Author))
            {
                HtmlNode node = wp.Html.SelectSingleNode("//span[@itemprop='author']");

                if (node == null)
                {
                    sError = "Couldn't find author";
                    return false;
                }

                book.Author = Sanitize.SanitizeTitle(node.InnerText);
            }
            return true;
        }

        static bool FUpdateReleaseDate(BookElement book, WebPage wp, ref string sError)
        {
            if (String.IsNullOrEmpty(book.ReleaseDate))
            {

                HtmlNode node = wp.Html.SelectSingleNode("//div[@id='ProductDetailsTab']");

                if (node == null)
                {
                    sError = "Couldn't find release date";
                    return false;
                }

                //ToDo: write another sanitize function to just extract the release date. 
                book.ReleaseDate = Sanitize.SanitizeTitle(node.InnerText);
            }
            return true;
        }

        static bool FUpdateSeries(BookElement book, WebPage wp, ref string sError)
        {
            if (String.IsNullOrEmpty(book.Series)) {

                try
                {
                    HtmlNode node = wp.Html.SelectSingleNode("//div[@id='ProductDetailsTab']");

                    if (node == null)
                    {
                        book.Series = "N/A";
                        return true;
                    }

                    //Todo: Need to create new sanitize function for series
                    book.Series = Sanitize.SanitizeSeries(node.InnerText);
                }
                catch(Exception ex)
                {
                    book.Series = "N/A";
                    return true;
                }
            }
            return true;
        }

        static bool FUpdateSummary(BookElement book, WebPage wp, ref string sError)
        {
            if (String.IsNullOrEmpty(book.Series))
            {

                HtmlNode node = wp.Html.SelectSingleNode("//div[@id=productInfoOverview]");

                if (node == null)
                {
                    sError = "Couldn't find summary";
                    return false;
                }

                //Todo: need to create new sanitize function for summary. or check to see if the current one can work or not
                book.Summary = Sanitize.SanitizeTitle(node.InnerText);
            }
            return true;
        }
    }
}