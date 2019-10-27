﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kermalis.MapEditor.Core;
using Kermalis.MapEditor.Util;
using System;
using System.ComponentModel;

namespace Kermalis.MapEditor.UI
{
    public sealed class BlocksetImage : Control, INotifyPropertyChanged
    {
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        public new event PropertyChangedEventHandler PropertyChanged;

        private readonly bool _allowSelectingMultiple;
        private readonly double _scale;

        private bool _isSelecting;
        private readonly Selection _selection;
        internal event EventHandler<Blockset.Block[][]> SelectionCompleted;

        private Blockset _blockset;
        internal Blockset Blockset
        {
            get => _blockset;
            set
            {
                if (_blockset != value)
                {
                    if (_blockset != null)
                    {
                        _blockset.OnAdded -= Blockset_OnAddedRemoved;
                        _blockset.OnRemoved -= Blockset_OnAddedRemoved;
                        _blockset.OnDrew -= Blockset_OnDrew;
                    }
                    _blockset = value;
                    _blockset.OnAdded += Blockset_OnAddedRemoved;
                    _blockset.OnRemoved += Blockset_OnAddedRemoved;
                    _blockset.OnDrew += Blockset_OnDrew;
                    ResetSelection();
                    InvalidateMeasure();
                    InvalidateVisual();
                    OnPropertyChanged(nameof(Blockset));
                }
            }
        }

        public BlocksetImage(bool allowSelectingMultiple, double scale)
        {
            _allowSelectingMultiple = allowSelectingMultiple;
            _scale = scale;
            _selection = new Selection();
            _selection.Changed += OnSelectionChanged;
        }

        public override void Render(DrawingContext context)
        {
            if (_blockset != null)
            {
                IBitmap source = _blockset.Bitmap;
                var viewPort = new Rect(Bounds.Size);
                Size sourceSize = source.Size;
                Rect destRect = viewPort.CenterRect(new Rect(sourceSize * _scale)).Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize).CenterRect(new Rect(destRect.Size / _scale));

                context.DrawImage(source, 1, sourceRect, destRect);
                var r = new Rect(_selection.X * 16 * _scale, _selection.Y * 16 * _scale, _selection.Width * 16 * _scale, _selection.Height * 16 * _scale);
                context.FillRectangle(_isSelecting ? Selection.SelectingBrush : Selection.SelectionBrush, r);
                context.DrawRectangle(_isSelecting ? Selection.SelectingPen : Selection.SelectionPen, r);
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_blockset != null)
            {
                return _blockset.Bitmap.Size * _scale;
            }
            return new Size();
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_blockset != null)
            {
                return _blockset.Bitmap.Size * _scale;
            }
            return new Size();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_blockset != null)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _isSelecting = true;
                        _selection.Start((int)(pos.X / _scale) / 16, (int)(pos.Y / _scale) / 16, 1, 1);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_blockset != null && _isSelecting && _allowSelectingMultiple)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.Other)
                {
                    Point pos = pp.Position;
                    if (Bounds.TemporaryFix_RectContains(pos))
                    {
                        _selection.Move((int)(pos.X / _scale) / 16, (int)(pos.Y / _scale) / 16);
                        e.Handled = true;
                    }
                }
            }
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_blockset != null && _isSelecting)
            {
                PointerPoint pp = e.GetPointerPoint(this);
                if (pp.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                {
                    _isSelecting = false;
                    FireSelectionCompleted();
                    InvalidateVisual();
                    e.Handled = true;
                }
            }
        }
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
        private void Blockset_OnAddedRemoved(Blockset blockset, Blockset.Block block)
        {
            ResetSelection();
        }
        private void Blockset_OnDrew(object sender, EventArgs e)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
        private void ResetSelection()
        {
            _isSelecting = false;
            _selection.Start(0, 0, 1, 1);
            FireSelectionCompleted();
        }
        private void FireSelectionCompleted()
        {
            if (SelectionCompleted != null)
            {
                var blocks = new Blockset.Block[_selection.Height][];
                for (int y = 0; y < _selection.Height; y++)
                {
                    var arrY = new Blockset.Block[_selection.Width];
                    for (int x = 0; x < _selection.Width; x++)
                    {
                        int index = x + _selection.X + ((y + _selection.Y) * Blockset.BitmapNumBlocksX);
                        arrY[x] = (index >= _blockset.Blocks.Count) ? null : _blockset.Blocks[index];
                    }
                    blocks[y] = arrY;
                }
                SelectionCompleted.Invoke(this, blocks);
            }
        }
    }
}
