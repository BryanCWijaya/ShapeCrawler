﻿using System;
using System.Linq;
using ShapeCrawler.Settings;
using ShapeCrawler.Texts;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ShapeCrawler.Tables
{
    /// <summary>
    /// Represents a cell in a table.
    /// </summary>
    public class CellSc
    {
        #region Fields

        private TextBoxSc _textBox;

        #endregion Fields

        #region Internal Properties

        internal int RowIndex { get; }
        internal int ColumnIndex { get; }
        internal TableSc Table { get; }
        internal A.TableCell ATableCell { get; init; }

        #endregion Internal Properties

        #region Public Properties

        /// <summary>
        /// Gets text box.
        /// </summary>
        public TextBoxSc TextBox
        {
            get
            {
                if (_textBox == null)
                {
                    TryParseTxtBody();
                }

                return _textBox;
            }
        }

        public bool IsMergedCell => DefineWhetherCellIsMerged();

        #endregion Public Properties

        #region Constructors

        internal CellSc(TableSc table, A.TableCell aTableCell, int rowIdx, int columnIdx)
        {
            Table = table;
            ATableCell = aTableCell;
            RowIndex = rowIdx;
            ColumnIndex = columnIdx;
        }

        #endregion Constructors

        private void TryParseTxtBody()
        {
            var aTxtBody = ATableCell.TextBody;
            var aTexts = aTxtBody.Descendants<A.Text>();
            if (aTexts.Any(t => t.Parent is A.Run) && aTexts.Sum(t => t.Text.Length) > 0) // at least one of <a:t> element contain text
            {
                _textBox = new TextBoxSc(Table.Shape, aTxtBody);
            }
        }

        private bool DefineWhetherCellIsMerged()
        {
            return ATableCell.GridSpan != null ||
                   ATableCell.RowSpan != null ||
                   ATableCell.HorizontalMerge != null ||
                   ATableCell.VerticalMerge != null;
        }
    }
}