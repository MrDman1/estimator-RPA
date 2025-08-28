using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Nuform.App
{
    /// <summary>
    /// Auto-commit that does NOT touch ComboBoxes (drop-downs work as normal).
    /// TextBoxes commit on Enter or when focus leaves, so number edits apply without tabbing away.
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            // Numeric/Text cells: commit on Enter
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.PreviewKeyDownEvent,
                new KeyEventHandler(OnTextBoxPreviewKeyDown),
                /* handledEventsToo: */ true);

            // and also when user leaves the editor
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.LostKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(OnTextBoxLostFocus),
                /* handledEventsToo: */ true);
        }

        private static void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CommitIfInsideDataGrid(sender as DependencyObject);
        }

        private static void OnTextBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
            => CommitIfInsideDataGrid(sender as DependencyObject);

        private static void CommitIfInsideDataGrid(DependencyObject? origin)
        {
            if (origin == null) return;

            var cell = FindAncestor<DataGridCell>(origin);
            if (cell == null) return;

            var grid = FindAncestor<DataGrid>(cell);
            if (grid == null) return;

            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row,  true);
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}