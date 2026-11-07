// BaseWindow main tab strip: custom drawing, close glyph and drag/drop reorder.
// The results TabControl uses the same reorder helper (SqlResultsUiFactory).
using System.Drawing;
using JustyBaseLegacy.UI.Helpers;

namespace JustyBaseLegacy.UI
{
    /// <summary>Payload for an in-process tab reorder drag.</summary>
    internal sealed record TabDragData(TabPage Page);

    public partial class BaseWindow
    {
        private TabPage? _tabDragSource;
        private Point _tabDragStart;
        private bool _suppressMainTabRemovedCleanup;

        private void WireMainTabStrip()
        {
            _tabControlMain.DrawItem += _tabControlDrawingHandler.TabControlMain_DrawItem;
            _tabControlMain.MouseMove += tabControlMain_MouseMove;
            _tabControlMain.MouseLeave += _tabControlDrawingHandler.TabControlMain_MouseLeave;
            _tabControlMain.MouseUp += tabControlMain_MouseUp;
            _tabControlMain.DragLeave += (_, _) => _tabDragSource = null;
        }

        private int TabIndexAt(Point clientPoint)
        {
            for (int i = 0; i < _tabControlMain.TabCount; i++)
            {
                if (_tabControlMain.GetTabRect(i).Contains(clientPoint))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsMainTabCloseHit(int index, Point clientPoint)
        {
            Rectangle closeRect = TabIconLayout.HitRect(
                TabIconLayout.CloseIconRect(_tabControlMain.GetTabRect(index), _tabControlMain.DeviceDpi),
                _tabControlMain.DeviceDpi);
            return closeRect.Contains(clientPoint);
        }

        private void tabControlMain_MouseMove(object sender, MouseEventArgs e)
        {
            _tabControlDrawingHandler.TabControlMain_MouseMove(sender, e);

            if (_tabDragSource is null || e.Button != MouseButtons.Left)
            {
                return;
            }

            Size dragSize = SystemInformation.DragSize;
            if (Math.Abs(e.X - _tabDragStart.X) < dragSize.Width
                && Math.Abs(e.Y - _tabDragStart.Y) < dragSize.Height)
            {
                return;
            }

            TabPage source = _tabDragSource;
            _tabDragSource = null;
            try
            {
                _tabControlMain.DoDragDrop(new TabDragData(source), DragDropEffects.Move);
            }
            catch (InvalidOperationException)
            {
                // A drag can already be in progress (e.g. a fast double-drag).
            }
        }

        private void tabControlMain_MouseUp(object sender, MouseEventArgs e)
        {
            _tabDragSource = null;
        }

        /// <summary>Closes the given main tab, dispatching editor vs document.</summary>
        internal async Task CloseMainTabAsync(TabPage page)
        {
            if (page is null || !_tabControlMain.TabPages.Contains(page))
            {
                return;
            }

            if (IsDocumentTab(page))
            {
                CloseDocumentTab(page);
                return;
            }

            await DoClosingOfTabAsync(_tabControlMain, page);
        }

        /// <summary>
        /// Moves <paramref name="fromIndex"/> to <paramref name="toIndex"/> while
        /// preserving the selected page. Shared by the document strip and the
        /// per-document results strip.
        /// </summary>
        internal static void MoveTabPage(TabControl tabs, int fromIndex, int toIndex)
        {
            if (tabs is null
                || fromIndex < 0
                || toIndex < 0
                || fromIndex >= tabs.TabCount
                || toIndex >= tabs.TabCount
                || fromIndex == toIndex)
            {
                return;
            }

            TabPage moved = tabs.TabPages[fromIndex];
            TabPage? selected = tabs.SelectedTab;

            tabs.SuspendLayout();
            try
            {
                var pages = tabs.TabPages.Cast<TabPage>().ToList();
                pages.RemoveAt(fromIndex);
                pages.Insert(toIndex, moved);

                for (int i = 0; i < pages.Count; i++)
                {
                    if (tabs.TabPages[i] != pages[i])
                    {
                        tabs.TabPages[i] = pages[i];
                    }
                }

                if (selected is not null && tabs.TabPages.Contains(selected))
                {
                    tabs.SelectedTab = selected;
                }
            }
            finally
            {
                tabs.ResumeLayout(true);
                tabs.Invalidate();
            }
        }

        private void ApplyMainTabReorder(TabPage dragged, DragEventArgs e)
        {
            int fromIndex = _tabControlMain.TabPages.IndexOf(dragged);
            if (fromIndex < 0)
            {
                return;
            }

            Point clientPoint = _tabControlMain.PointToClient(new Point(e.X, e.Y));
            int toIndex = TabIndexAt(clientPoint);
            if (toIndex < 0)
            {
                toIndex = _tabControlMain.TabCount - 1;
            }

            WithoutMainTabRemovedCleanup(() =>
            {
                MoveTabPage(_tabControlMain, fromIndex, toIndex);
                _tabControlMain.SelectedTab = dragged;
            });
            NotifyEditorTabOrderChanged();
        }

        /// <summary>
        /// Runs a main-tab mutation without treating it as a tab close.
        /// Reordering via the TabPages indexer fires ControlRemoved
        /// synchronously; without the guard the removed-page cleanup would
        /// unregister live editor tabs and their splitters.
        /// </summary>
        private void WithoutMainTabRemovedCleanup(Action mutation)
        {
            _suppressMainTabRemovedCleanup = true;
            try
            {
                mutation();
            }
            finally
            {
                _suppressMainTabRemovedCleanup = false;
            }
        }

        private void NotifyEditorTabOrderChanged()
        {
            IReadOnlyList<JustData.Application.Editor.EditorDocumentId> order = GetEditorDocumentOrder();
            if (order.Count > 0)
            {
                _editorWorkspaceViewModel.Reorder(order);
            }
        }
    }
}
