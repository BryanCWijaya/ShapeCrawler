﻿using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using ShapeCrawler.Drawing;
using ShapeCrawler.Shapes;
using ShapeCrawler.Shared;
using ShapeCrawler.Texts;
using A = DocumentFormat.OpenXml.Drawing;

// ReSharper disable CheckNamespace
namespace ShapeCrawler;

/// <summary>
///     Represents a table cell.
/// </summary>
public interface ICell
{
    /// <summary>
    ///     Gets text box.
    /// </summary>
    ITextFrame TextFrame { get; }

    /// <summary>
    ///     Gets a value indicating whether cell belongs to merged cell.
    /// </summary>
    bool IsMergedCell { get; }

    /// <summary>
    ///     Gets Shape Fill of the cell.
    /// </summary>
    IShapeFill Fill { get; }
}

internal sealed class SCCell : ICell, ITextFrameContainer
{
    private readonly ResetableLazy<SCTextFrame> textFrame;
    private readonly ResetableLazy<SCShapeFill> fill;
    private readonly ISlideStructure slideStructure;

    internal SCCell(
        SCRow tableRow, 
        A.TableCell aTableCell, 
        int rowIndex, 
        int columnIndex, 
        TypedOpenXmlPart slideTypedOpenXmlPart,
        List<ImagePart> imageParts)
    {
        this.ParentTableRow = tableRow;
        this.ATableCell = aTableCell;
        this.RowIndex = rowIndex;
        this.ColumnIndex = columnIndex;
        this.textFrame = new ResetableLazy<SCTextFrame>(this.CreateTextFrame);
        this.slideStructure = tableRow.ParentTable.SlideStructure;
        var framePr = aTableCell.TableCellProperties!;
        this.fill = new ResetableLazy<SCShapeFill>(() => new CellFill(this.slideStructure, framePr, slideTypedOpenXmlPart, imageParts));
    }

    public bool IsMergedCell => this.DefineWhetherCellIsMerged();

    public IShapeFill Fill => this.fill.Value;

    public SCShape SCShape => this.ParentTableRow.ParentTable;

    public ITextFrame TextFrame => this.textFrame.Value;

    internal A.TableCell ATableCell { get; init; }

    internal int RowIndex { get; }

    internal int ColumnIndex { get; }

    private SCRow ParentTableRow { get; }

    private SCTextFrame CreateTextFrame()
    {
        return new SCTextFrame(this, this.ATableCell.TextBody!, this.slideStructure, this);
    }

    private bool DefineWhetherCellIsMerged()
    {
        return this.ATableCell.GridSpan is not null ||
               this.ATableCell.RowSpan is not null ||
               this.ATableCell.HorizontalMerge is not null ||
               this.ATableCell.VerticalMerge is not null;
    }
}