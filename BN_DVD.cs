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
    // D  V  D
    //
    // Scrape DVD information from B&N.
    // ============================================================================
    public class DVD
    {

        // ============================================================================
        // D V D  E L E M E N T
        //
        // Holds all information about the DVD.  When scraping, will fill in anything
        // that isn't already filled out.
        // ============================================================================
        public class DvdElement
        {
            string m_sScanCode;
            string m_sTitle;
            string m_sSummary;
            private string m_sNotes;
            private string m_sQueryUrl;
            private string m_sCoverSrc;
            private string m_sClassification;
            private string m_sMediaType;
            private List<string> m_plsClasses;

            public DvdElement(string sScanCode)
            {
                m_sScanCode = sScanCode;
                m_sTitle = m_sSummary = "";
            }

            // notes are internal only and not scraped from anywhere
            public string Notes
            {
                get { return m_sNotes; }
                set { m_sNotes = value; }
            }

            // internal list of classifications (subjects), used to build the classification string
            public List<string> ClassList
            {
                get { return m_plsClasses; }
                set { m_plsClasses = value; }
            }

            // UPC scan code
            public string ScanCode
            {
                get { return m_sScanCode; }
                set { m_sScanCode = value; }
            }

            public string MediaType
            {
                get { return m_sMediaType; }
                set { m_sMediaType = value; }
            }

            public string QueryUrl
            {
                get { return m_sQueryUrl; }
                set { m_sQueryUrl = value; }
            }

            public string CoverSrc
            {
                get { return m_sCoverSrc; }
                set { m_sCoverSrc = value; }
            }

            public string Classification
            {
                get { return m_sClassification; }
                set { m_sClassification = value; }
            }

            public string Summary
            {
                get { return m_sSummary; }
                set { m_sSummary = value; }
            }
            public string Title
            {
                get { return m_sTitle; }
                set { m_sTitle = value; }
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: FScrapeDvd
        	%%Qualified: TCore.Scrappy.BarnesAndNoble.DVD.FScrapeDvd
        	%%Contact: rlittle
        	
            Given a DvdElement, go to B&N and scrape all the parts of the DvdElement
            that aren't already filled out.  If anything goes wrong, fill in a
            description in sError and return false.

            (NOTE: just because we failed to scrape certain elements, like subjects,
            we won't return failure. some things won't always be there)
        ----------------------------------------------------------------------------*/
        public static bool FScrapeDvd(ref DvdElement dvd, out string sError)
        {
            string sCode;

            sCode = dvd.ScanCode;
            sError = "";

            // 0044004783521
            try
                {
                ScrapingBrowser sbr = new ScrapingBrowser();
                sbr.AllowAutoRedirect = true;
                sbr.AllowMetaRedirect = true;

                WebPage wp = sbr.NavigateToPage(new Uri("https://www.barnesandnoble.com/s/" + sCode));

                // get the product summary section
                HtmlNode nodeSummary = wp.Html.SelectSingleNode("//section[@id='prodSummary']");

                if (!UpdateTitleFromSummary(nodeSummary, dvd, ref sError))
                    return false;

                if (!FUpdateSummary(dvd, wp))
                    return false;

                if (!FUpdateMediaType(dvd, wp))
                    return false;

                // don't fail if we can't get a cover src
                FUpdateCoverSrc(dvd, wp);

                // don't fail if we can't get any subjects
                FUpdateCategories(dvd, wp);
                }
            catch (Exception exc)
                {
                sError = exc.Message;
                return false;
                }

            return true;
        }

        private static bool FUpdateCategories(DvdElement dvd, WebPage wp)
        {
            if (String.IsNullOrEmpty(dvd.Classification))
                {
                HtmlNode nodeRelated = wp.Html.SelectSingleNode("//section[@id='relatedSubjects']");

                if (nodeRelated == null)
                    return false;

                // now get the list of items
                HtmlNodeCollection nodeSubjects = nodeRelated.SelectNodes(".//li");
                if (nodeSubjects.Count == 0)
                    return false;

                List<string> subjects = new List<string>();

                foreach (HtmlNode node in nodeSubjects)
                    {
                    subjects.Add(node.InnerText);
                    }

                if (subjects.Count == 0)
                    return false;

                subjects = Sanitize.SanitizeClassList(subjects);
                if (subjects.Count == 0)
                    return false;

                dvd.Classification = string.Join(",", subjects);
                }

            return true;
        }
        private static bool FUpdateCoverSrc(DvdElement dvd, WebPage wp)
        {
            if (String.IsNullOrEmpty(dvd.CoverSrc))
                {
                HtmlNode node = wp.Html.SelectSingleNode("//img[@id='pdpMainImage']");

                if (node == null)
                    return false;

                dvd.CoverSrc = node.GetAttributeValue("src", "");
                }

            return true;
        }
        private static bool FUpdateMediaType(DvdElement dvd, WebPage wp)
        {
            if (String.IsNullOrEmpty(dvd.MediaType))
                {
                HtmlNode node = wp.Html.SelectSingleNode("//section[@id='prodPromo']");

                if (node == null)
                    return false;

                dvd.MediaType = Sanitize.SanitizeMediaType(node.InnerText);
                }
            return true;
        }

        private static bool FUpdateSummary(DvdElement dvd, WebPage wp)
        {
            if (String.IsNullOrEmpty(dvd.Summary))
                {
                HtmlNode nodeSummary = wp.Html.SelectSingleNode("//div[@id='productInfoOverview']");

                if (nodeSummary == null)
                    return false;

                dvd.Summary = Sanitize.SanitizeSummary(nodeSummary.InnerText);
                }

            return true;
        }

        private static bool UpdateTitleFromSummary(HtmlNode nodeSummary, DvdElement dvd, ref string sError)
        {
            // fill in only those parts that are empty
            if (String.IsNullOrEmpty(dvd.Title))
                {
                // title is in the <h1> element
                HtmlNode nodeTitle = nodeSummary.SelectSingleNode("h1");

                // confirm that there is @itemprop='name'
                if (nodeTitle.GetAttributeValue("itemprop", "") != "name")
                    {
                    sError = "can't find title. prodsummary[@itemprop != 'name']";
                    return false;
                    }
                dvd.Title = Sanitize.SanitizeTitle(nodeTitle.InnerText);
                }
            return true;
        }
    }
}