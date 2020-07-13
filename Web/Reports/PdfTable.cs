using PdfFileWriter;

using System;
using System.Collections.Generic;
using System.Drawing;

namespace Assmnts.Reports
{
    public class PdfTable
    {
        //constants
        private readonly double cellContentsDy      = .07; //distance from top of cell to first-line text baseline
        private readonly double cellPaddingBottom   = .07; //distance from bottom of cell to text
        private readonly double cellPaddingLeft     = .08; //distance from left of cell to text
        private readonly double cellPaddingRight    = .16; //distance from right of cell to text
        private readonly double cellBorderWidth     = .02; //width of lines between cells

        private readonly int columnHeaderFontSize = 12;

        //vars set in constructor
        public readonly int columnCount;
        private readonly double[] bordersX;

        //vars manipulated between construction and output
        private readonly List<Row> rows = new List<Row>();
        private double minimumRowThickness = .45;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnCount">number of columns in the table</param>
        /// <param name="bordersX">x-positions of vertical borders. Must be of length columnCount+1</param>
        public PdfTable(int columnCount, double[] bordersX)
        {

            if (bordersX.Length != columnCount + 1)
                throw new Exception("bordersX has length of " + bordersX.Length + ", expecting "
                    + (columnCount + 1) + " because columnCount is " + columnCount);

            this.columnCount = columnCount;
            this.bordersX = (double[])bordersX.Clone();
        }

        public void setMinimumRowThickness( double thickness ){
            if( thickness <= 0 )
                throw new Exception( "invalid row thickness " + thickness + ", must be positive" );
            this.minimumRowThickness = thickness;
        }

        public void addRow(string[] contents, PdfFont font, double fontSize,
            Color background, double fixedThickness = -1)
        {
            if (contents == null)
                throw new Exception("Row contents parameter is null");
            if (contents.Length != columnCount)
                throw new Exception("Row contents array contains " + contents.Length + 
                    " elements, but this table was initialized with a column count of " + columnCount);

            rows.Add(new RegularRow(contents, font, fontSize, background, fixedThickness));
        }

        public void addMergedRow(string contents, PdfFont font, double fontSize, 
            Color background, double fixedThickness = -1 )
        {
            if (contents == null)
                throw new Exception("Row contents parameter is null");

            rows.Add(new MergedRow(contents, font, fontSize, background, fixedThickness));
        }

        private double getRowThickness(PdfOutput output, Row r)
        {
            double rowThickness;

            if (r is RegularRow)
            {
                string[] rowVals = ((RegularRow)r).contents;
                rowThickness = minimumRowThickness;
                if (r.fixedThickness > 0)
                {
                    rowThickness = r.fixedThickness;
                }
                else
                {
                    for (int col = 0; col < columnCount; col++)
                    {
                        double height = output.GetWrappedTextHeight(
                            r.font, r.fontSize,
                            bordersX[col] + cellPaddingLeft,
                            output.drawY - cellContentsDy,
                            bordersX[col + 1] - bordersX[col] - cellPaddingRight,
                            TextJustify.Left,
                            rowVals[col] == null ? "N/A" : rowVals[col]);
                        height += cellContentsDy + cellPaddingBottom;
                        if (height > rowThickness)
                            rowThickness = height;
                    }
                }
            }
            else
            {
                string content = ((MergedRow)r).contents;
                if (r.fixedThickness > 0)
                {
                    rowThickness = r.fixedThickness;
                }
                else
                {
                    double height = output.GetWrappedTextHeight(
                        r.font, r.fontSize,
                        bordersX[0] + cellPaddingLeft,
                        output.drawY - cellContentsDy,
                        bordersX[columnCount] - bordersX[0] - cellPaddingRight,
                        TextJustify.Left,
                        content);
                    height += cellContentsDy + cellPaddingBottom;
                    rowThickness = Math.Max(height, minimumRowThickness);
                }
            }

            return rowThickness;
        }

        public void printTable(PdfOutput output)
        {
            if( rows.Count == 0 )
                throw new Exception( "Cannot print a table with no rows" );

            //draw top table border
            output.SetColor(Color.Black);
            output.DrawLine(bordersX[0], output.drawY,
                bordersX[columnCount], output.drawY, cellBorderWidth);

            foreach (Row r in rows)
            {
                //compute row thickness before drawing, add page break if necessary
                double rowThickness = getRowThickness(output, r);
                if (output.drawY - rowThickness < 0 )
                    output.appendPageBreak();

                if (r is RegularRow)
                {
                    string[] rowVals = ((RegularRow)r).contents;

                    //draw row background, if applicable
                    double y = output.drawY;
                    if (!r.background.Equals(Color.White))
                    {
                        output.SetColor(r.background);
                        output.FillRectangle(bordersX[0], output.drawY - rowThickness,
                            bordersX[columnCount] - bordersX[0], rowThickness);
                        output.drawY = y;
                        output.SetColor(Color.Black);
                    }

                    //draw cell contents
                    for (int col = 0; col < columnCount; col++)
                    {
                        output.DrawWrappedText(
                            r.font, r.fontSize,
                            bordersX[col] + cellPaddingLeft,
                            output.drawY - cellContentsDy,
                            bordersX[col + 1] - bordersX[col] - cellPaddingRight,
                            TextJustify.Left,
                            rowVals[col] == null ? "N/A" : rowVals[col]);
                        output.drawY = y;
                    }

                    //draw cell borders
                    for (int col = 0; col <= columnCount; col++)
                    {
                        output.DrawLine(bordersX[col], output.drawY, 
                            bordersX[col], output.drawY - rowThickness, cellBorderWidth);
                    }
                    output.DrawLine(bordersX[0], output.drawY - rowThickness, 
                        bordersX[columnCount], output.drawY - rowThickness, cellBorderWidth);

                    //move down the page for the next row
                    output.drawY -= rowThickness;
                }
                else if (r is MergedRow)
                {
                    string content = ((MergedRow)r).contents;

                    //draw row background, if applicable
                    double y = output.drawY;
                    if (!r.background.Equals(Color.White))
                    {
                        output.SetColor(r.background);
                        output.FillRectangle(bordersX[0], output.drawY - rowThickness,
                            bordersX[columnCount] - bordersX[0], rowThickness);
                        output.drawY = y;
                        output.SetColor(Color.Black);
                    }

                    //draw cell content
                    output.DrawWrappedText(
                        r.font, r.fontSize,
                        bordersX[0] + cellPaddingLeft,
                        output.drawY - cellContentsDy,
                        bordersX[columnCount] - bordersX[0] - cellPaddingRight,
                        TextJustify.Left,
                        content);
                    output.drawY = y;

                    //draw cell borders
                    output.DrawLine(bordersX[0], output.drawY,
                        bordersX[0], output.drawY - rowThickness, cellBorderWidth);
                    output.DrawLine(bordersX[columnCount], output.drawY,
                        bordersX[columnCount], output.drawY - rowThickness, cellBorderWidth);
                    output.DrawLine(bordersX[0], output.drawY - rowThickness,
                        bordersX[columnCount], output.drawY - rowThickness, cellBorderWidth);

                    //move down the page for the next row
                    output.drawY -= rowThickness;
                }
                else
                {
                    throw new Exception("Unrecognized row subclass \"" + r.GetType() + "\"");
                }
            }
        }

        #region helper class "Row" and descendants
        private abstract class Row {
            public readonly PdfFont font;
            public readonly double fontSize;
            public readonly Color background;
            public readonly double fixedThickness;

            public Row(PdfFont font, double fontSize,
                Color background, double fixedThickness)
            {
                this.font = font;
                this.fontSize = fontSize;
                this.background = background;
                this.fixedThickness = fixedThickness;
            }
        };

        //a regular row has [columnCount] separate cells
        private class RegularRow : Row {
            public readonly string[] contents;

            public RegularRow(string[] contents, PdfFont font, double fontSize,
                Color background, double fixedThickness)
                : base(font, fontSize, background, fixedThickness)
            {
                this.contents = contents;
            }
        };

        //a merged row has just one cell that takes up the entire row
        private class MergedRow : Row {
            public readonly string contents;

            public MergedRow(string contents, PdfFont font, double fontSize,
                Color background, double fixedThickness)
                : base(font, fontSize, background, fixedThickness)
            {
                this.contents = contents;
            }
        };
        #endregion
    }
}