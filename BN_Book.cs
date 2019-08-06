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
                sbr.AutoDetectCharsetEncoding = false;
                sbr.Encoding = Encoding.UTF8;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
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

                if (!FUpdateRawCoverUrl(book, wp, ref sError))
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
                if (exc.InnerException != null)
                    sError += " + " + exc.InnerException.Message;
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

                book.ReleaseDate = Sanitize.SanitizeDate(node.InnerText);
            }
            return true;
        }

        static bool FUpdateRawCoverUrl(BookElement book, WebPage wp, ref string sError)
        {
            if (String.IsNullOrEmpty(book.RawCoverUrl))
            {

                HtmlNode node = wp.Html.SelectSingleNode("//img[@id='pdpMainImage']");

                if (node == null)
                {
                    sError = "Couldn't find release date";
                    return false;
                }

                book.RawCoverUrl = Sanitize.SanitizeCoverUrl(node.Attributes["src"].Value);
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
                    if (node.InnerText.Contains("Series"))
                    {
                        book.Series = Sanitize.SanitizeSeries(node.InnerText);
                    }
                    else
                    {
                        book.Series = "N/A";
                    }
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
            if (String.IsNullOrEmpty(book.Summary))
            {
                HtmlNode node = wp.Html.SelectSingleNode("//div[@class='text--medium overview-content']");

                if (node == null)
                {
                    sError = "Couldn't find summary";
                    return false;
                }
                book.Summary = Sanitize.SanitizeSummary(node.InnerText);
            }
            return true;
        }
    }
}