using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.IO;
using PdfFileWriter;
using Data.Abstract;

namespace Assmnts.Reports
{
    public class PdfOutput
    {
        public const double pageWidth = 8.5;
        public const double pageHeight = 11;
        public const double firstColumnHeaderX = .58;
        public const double secondColumnHeaderX = 4.1;
        public const double rightMargin = .25;
        public const double itemSpacing = .08;
        public const double defaultFontSize = 8;
        public const double defaultPartHeaderFontSize = 18;
        public const double defaultSectionHeaderFontSize = 12;

        //if non-null, dictates the content+position line at the top of new pages
        //if null, no page header
        public string[] pageHeaderText = null;
        public double[] pageHeaderXPos = null;

        //fxed position of page footer (x and y distance from bottom-right corner)
        private static readonly double footerMarginBottom = .3;
        private static readonly double footerMarginRight = .45;
        private static readonly double footerMarginLeft = .45;

        //x-diff between each item and it's subheader
        private const double itemIndent = .15;

        //x-diff between item identifiers and values 
        private const double itemValueTab = 1.65;

        protected PdfPage currentPage;
        protected PdfDocument doc;
        protected PdfContents cont;

        public readonly PdfFont logoFont, contentFont, boldFont;
        public readonly Color sisred;
        public double drawY;
        protected bool secondColumn = false;
        public readonly bool grayscale;

        public readonly string outputPath;

        private double bottomMargin = .5;
        public double fontSize = defaultFontSize;
        public double partHeaderFontSize = defaultPartHeaderFontSize;
        public double sectionHeaderFontSize = defaultSectionHeaderFontSize;

        public PdfOutput( bool grayscale, string outputPath )
        {
            this.grayscale = grayscale;
            this.outputPath = outputPath;
            sisred = grayscale ? Color.DarkGray : Color.FromArgb(0, 60, 123);
            doc = new PdfDocument(pageWidth, pageHeight, UnitOfMeasure.Inch, outputPath);
            currentPage = new PdfPage(doc);
            cont = new PdfContents(currentPage);
            logoFont = PdfFont.CreatePdfFont(doc, "Times", FontStyle.Regular, false);
            contentFont = PdfFont.CreatePdfFont(doc, "Arial", FontStyle.Regular, false);
            boldFont = PdfFont.CreatePdfFont(doc, "Arial", FontStyle.Bold, false);
            drawY = pageHeight;
        }

        public void setBottomMargin(double margin)
        {
            this.bottomMargin = margin;
        }

        public PdfOutput getSecondColumnBranch()
        {
            PdfOutput result = new PdfOutput(grayscale, outputPath.Replace(".pdf", "_dummy.pdf")); //dumy path should not be used
            result.doc = doc;
            result.cont = cont;
            result.secondColumn = true;
            result.drawY = drawY;
            result.bottomMargin = bottomMargin;

            return result;
        }

        //public void buildTestPdf()
        //{
        //    appendTopHeader();
        //    appendSectionBreak();
        //    appendSubHeader("Person Being Assessed");
        //    appendItem("Last Name", "Chan");
        //    appendItem("First Name", "Sophie");
        //    outputToFile("C:\\Users\\rbogard\\Downloads\\test.pdf");
        //}

        public void outputToFile()
        {
            doc.CreateFile();
            //Process Proc = new Process();
            //Proc.StartInfo = new ProcessStartInfo(path);
            //Proc.Start();
        }

        public void appendTopHeader( string title, string logoPath, bool includeSecondLine=true, string assessmentType = "")
        {
            drawY -= .7;
            string logoDir = Path.GetDirectoryName(logoPath);

            PdfImage logo = new PdfImage( doc, logoPath );
            double logoHeight = .6;
            double logoWidth = logoHeight * logo.WidthPix / logo.HeightPix;
            
            // standard (left) header logo
            cont.DrawImage(logo, firstColumnHeaderX, drawY-.2, logoWidth, logoHeight );
            
            // right header logo
            string logoRight = Path.Combine(logoDir, "logo-right.png");
            if (File.Exists(logoRight))
            {
                PdfImage logoR = new PdfImage(doc, logoRight);
                cont.DrawImage(logoR, secondColumnHeaderX + 1.7, drawY + .05, logoWidth, logoHeight);
            }

            //cont.DrawText(logoFont, 50.0, firstColumnHeaderX, drawY, sisred, "AAIDD");
            double titleSize = includeSecondLine ? 10.0 : 15.0;
            double titleY = includeSecondLine ? drawY + .15 : drawY + .07;
            cont.DrawText(boldFont, titleSize, 3.6, titleY, title );
            if (includeSecondLine)
            {
                // customize header text for SIS-A/C
                if (!String.IsNullOrEmpty(assessmentType))
                {
                    if (assessmentType == "SIS-A")
                    {
                        cont.DrawText(contentFont, 7.5, 3.6, drawY, "Confidential Interview and Profile Results for the Supports Intensity Scale Adult Version     : SIS-A");
                        cont.DrawText(contentFont, 5, 7.63, drawY + .03, "TM");
                        cont.DrawText(contentFont, 5, 8.09, drawY + .03, "TM");
                    }
                    if (assessmentType == "SIS-C")
                    {
                        cont.DrawText(contentFont, 7.5, 3.6, drawY, "Confidential Interview and Profile Results for the Supports Intensity Scale Children's Version     : SIS-C");
                        cont.DrawText(contentFont, 5, 7.85, drawY + .03, "TM");
                        cont.DrawText(contentFont, 5, 8.33, drawY + .03, "TM");
                    }
                }
                else
                {
                    cont.DrawText(contentFont, 7.5, 3.6, drawY, "Confidential Interview and Profile Results for the Supports Intensity Scale");
                }
            }
        }

        public void appendSubHeaderOnNewPageIfNecessary(string title, string subtitle = null)
        {
            title = RemoveHTMLTagsAndIllegalChars(title);
            subtitle = subtitle == null ? null : RemoveHTMLTagsAndIllegalChars(subtitle);
            double x = secondColumn ? secondColumnHeaderX : firstColumnHeaderX;
            title += String.IsNullOrEmpty(subtitle) ? ":" : " - ";
            TextBox box = new TextBox(pageWidth - x - rightMargin, 0);
            box.AddText(boldFont, fontSize, sisred, title);
            box.AddText(contentFont, fontSize, subtitle);
            Double PosY = drawY;

            if (box.BoxHeight >= drawY)
            {
                appendPageBreak();
                PosY = drawY-.3;
            }

            cont.DrawText(x, ref PosY, 0.0, 0, box);
            drawY -= .1;
            drawY -= itemSpacing;
        }

        public void appendSubHeader(string title, string subtitle = null)
        {
            title = RemoveHTMLTagsAndIllegalChars(title);
            double x = secondColumn ? secondColumnHeaderX : firstColumnHeaderX;
            title += String.IsNullOrEmpty(subtitle) ? ":" : " - ";
            TextBox box = new TextBox(pageWidth - x - rightMargin, 0);
            box.AddText(boldFont, fontSize, sisred, title);
            box.AddText(boldFont, fontSize, subtitle);
            Double PosY = drawY;
            cont.DrawText(x, ref PosY, 0.0, 0, box);
            drawY -= .1;
            drawY -= itemSpacing;
        }

        public void appendWebLink(string text, string href, double xPos)
        {
            cont.DrawWebLink(currentPage, contentFont, fontSize, xPos, drawY-.1 , text, href);
        }

        public double appendWrappedText(string text, double xPos, double wrapWidth)
        {
            return appendWrappedText(text, xPos, wrapWidth, contentFont, fontSize);
        }

        public double appendWrappedText(string text, double xPos, double wrapWidth, PdfFont font)
        {
            return appendWrappedText(text, xPos, wrapWidth, font, fontSize);
        }

        public double appendWrappedText(string text, double xPos, double wrapWidth, double fontSize)
        {
            return appendWrappedText(text, xPos, wrapWidth, contentFont, fontSize);
        }

        public double appendWrappedText(
            string text, 
            double xPos, 
            double wrapWidth, 
            PdfFont font, 
            double fontSize)
        {

            if ( String.IsNullOrEmpty(text) )
                return 0;
            bool indent = text.StartsWith("\t");
            if (indent) 
                text = text.Substring(1);
            text = RemoveHTMLTagsAndIllegalChars(text);

            TextBox box = new TextBox(wrapWidth, indent ? .25 : 0 );
            try
            {
                bool bold = false;
                while (true)
                {
                    string search = bold ? "</b>" : "<b>";
                    if (text.Contains(search))
                    {
                        int i = text.IndexOf(search);
                        box.AddText(bold ? boldFont : font, fontSize, text.Substring(0, i) );
                        text = text.Substring(i + search.Length);
                        bold = !bold;
                    }
                    else
                    {
                        box.AddText(bold ? boldFont : font, fontSize, text );
                        break;
                    }
                }
            }
            catch (ApplicationException e)
            {
                throw new Exception("could not add text \"" + text + "\"", e);
            }
            if (drawY-bottomMargin < box.BoxHeight)
                appendPageBreak();
            Double PosY = drawY;

            cont.DrawText(xPos, ref PosY, 0.0, 0, box);
            drawY -= box.BoxHeight;

            return box.BoxHeight;
        }

        public string RemoveHTMLTagsAndIllegalChars(string HTML_Text)
        {
            if (HTML_Text == null)
                return "";

            string result = string.Empty;
            bool insideTag = false;

            // Pre-allocate enough space to hold the filtered string
            StringBuilder sb = new StringBuilder(HTML_Text.Length);

            char lt = '<'; char.TryParse("<", out lt);
            char gt = '>'; char.TryParse(">", out gt);
            //char space; char.TryParse(" ", out space);

            for (int i = 0; i < HTML_Text.Length; i++)
            {
                char c = HTML_Text[i];
                if (c == lt)
                    insideTag = true;
                else if (c == gt)
                    insideTag = false;
                else if (!insideTag)
                {
                    if ( (int)c > 255 || (int)c < 32 )
                        c = ' ';
                    sb.Append(c);
                }
            }

            result = sb.ToString();
            return result;
        }

        public void appendImage( string imagePath)
        {
            drawY -= 4;
            cont.DrawImage(new PdfImage(doc,imagePath), 1, drawY, 6, 4);
        }

        public void appendSimpleTable(string[] headers, params string[][] values)
        {
            double[] valueXOffsets = new double[headers.Length];
            appendSimpleTable(headers, values, valueXOffsets);
        }

        public void appendSimpleTable(string[] headers, string[][] values, double[] valueXOffsets, double[] headerXOffsets = null )
        {
            int nCols = headers.Length;
            double d = 7.0 / nCols;
            double[] indents = new double[nCols];
            for (int i = 0; i < nCols; i++)
                indents[i] = .84 + d * i;

            //headers
            drawY -= .1;
            for (int i = 0; i < nCols; i++)
            {
                double x = indents[i];
                if (headerXOffsets != null)
                    x += headerXOffsets[i];
                cont.DrawText(boldFont, fontSize, x, drawY, headers[i]);
            }
            drawY -= itemSpacing;

            //values
            for (int i = 0; i < values.Length; i++)
            {
                drawY -= .1;
                for (int j = 0; j < nCols; j++)
                {
                    cont.DrawText(contentFont, fontSize, indents[j]+valueXOffsets[j], drawY, values[i][j]);
                }
                drawY -= itemSpacing;
            }
        }

        public void appendItem(string identifier, string value, double xDiff = itemValueTab)
        {
            identifier = RemoveHTMLTagsAndIllegalChars(identifier);
            value = RemoveHTMLTagsAndIllegalChars(value);

            double x = (secondColumn ? secondColumnHeaderX : firstColumnHeaderX) + itemIndent;
            drawY -= .1;
            cont.DrawText(boldFont, fontSize, x, drawY, identifier + ":");
            cont.DrawText(contentFont, fontSize, x + xDiff, drawY, value);
            drawY -= itemSpacing;
        }

        public void appendSectionBreak()
        {
            drawY -= .15;
            cont.SetColorStroking(Color.LightGray);
            cont.DrawLine(firstColumnHeaderX, drawY, pageWidth - rightMargin, drawY, .015);
        }

        public void appendPageBreak()
        {
            currentPage = new PdfPage(doc);
            cont = new PdfContents(currentPage);
            drawY = pageHeight;
            appendPageHeader();
            appendPageFooter();
        }

        private int pageNumber = 1;
        public void appendPageFooter()
        {
            cont.DrawText(contentFont, fontSize, pageWidth - footerMarginRight, footerMarginBottom, 
                (pageNumber++).ToString());

            cont.DrawText(contentFont, fontSize, footerMarginLeft, footerMarginBottom, 
                "Date printed: " + DateTime.Now.ToString("MM/dd/yyyy") );
        }

        private void appendPageHeader()
        {
            drawY -= .25;
            if ((pageHeaderText != null) && (pageHeaderXPos != null))
            {
                for (int i = 0; i < pageHeaderText.Length && i < pageHeaderXPos.Length; i++)
                {
                    cont.DrawText(contentFont, fontSize, pageHeaderXPos[i], drawY, pageHeaderText[i]);
                }
            }
            drawY -= itemSpacing;
        }

        #region methods that pass directly to PDFContents member

        public void DrawLine(double X1, double Y1, double X2, double Y2, double LineWidth)
        {
            cont.DrawLine(X1, Y1, X2, Y2, LineWidth);
        }

        public double DrawText(PdfFont font, double FontSize, double PosX, double PosY, string Text)
        {
            Text = RemoveHTMLTagsAndIllegalChars(Text);
            return cont.DrawText(font, FontSize, PosX, PosY, Text);
        }

        public double DrawText(PdfFont font, double FontSize, double PosX, double PosY, TextJustify just, string Text)
        {
            Text = RemoveHTMLTagsAndIllegalChars(Text);
            return cont.DrawText(font, FontSize, PosX, PosY, just, Text);
        }

        public double DrawWrappedText(PdfFont font, double fontSize, double PosX, double PosY, double wrapWidth, string text, double? newPageY = null)
        {
            return DrawWrappedText(font, fontSize, PosX, PosY, wrapWidth, TextJustify.Left, text, newPageY);
        }


        public double GetWrappedTextHeight(PdfFont font, double fontSize, double PosX, double PosY, double wrapWidth, TextJustify just, string text, double? newPageY = null )
        {
            text = RemoveHTMLTagsAndIllegalChars(text);
            TextBox box = new TextBox(wrapWidth);
            box.AddText(font, fontSize, text);

            return box.BoxHeight 
                + .15; //necessary adjustment found through guess-and-check
        }
        
        public double DrawWrappedText(PdfFont font, double fontSize, double PosX, double PosY, double wrapWidth, TextJustify just, string text, double? newPageY = null )
        {
            text = RemoveHTMLTagsAndIllegalChars(text);
            TextBox box = new TextBox(wrapWidth);
            try
            {
                box.AddText(font, fontSize, text);
            }
            catch(Exception xcptn)
            {
                Debug.WriteLine("DrawWrappedText exception: " + xcptn.Message);
                Debug.WriteLine("    text: " + text);
            }
            if (drawY < box.BoxHeight)
            {
                appendPageBreak();
                PosY = newPageY.GetValueOrDefault( drawY );
            }
            Double y = PosY;
            if (just == TextJustify.Center)
            {
                PosX += (wrapWidth - box.BoxWidth) / 2;
            }
            cont.DrawText(PosX, ref y, 0.0, 0, box);

            return box.BoxHeight;
        }

        public void SetColor(Color fillCol)
        {
            cont.SetColorNonStroking(fillCol);
            cont.SetColorStroking(fillCol);
        }

        public void FillRectangle(double x, double y, double w, double h)
        {
            cont.DrawRectangle(x, y, w, h, PaintOp.Fill);
        }

        public void OutlineRectangle(double x, double y, double w, double h)
        {
            cont.DrawRectangle(x, y, w, h, PaintOp.Stroke);
        }

        #endregion
    }
}