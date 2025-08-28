# Nuform Estimator — Immediate Orientation Update (UI Patch)

If switching **Lengthwise/Widthwise** doesn't recalc until you leave the row, make the ComboBox commit on change.

## Option A — Binding-level (recommended)
In the `DataGridComboBoxColumn` for Orientation, set `UpdateSourceTrigger=PropertyChanged`:

```xml
<DataGridComboBoxColumn Header="Orientation"
    SelectedItemBinding="{Binding CeilingOrientation, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
    ItemsSource="{Binding Source={x:Static domain:CeilingOrientationValues.All}}" />
```

## Option B — Event handler
If A isn't possible in your setup, add a `SelectionChanged` handler that commits the edit and recalculates:

**XAML** (inside the `DataGridComboBoxColumn`):
```xml
<DataGridComboBoxColumn.Header>Orientation</DataGridComboBoxColumn.Header>
<DataGridComboBoxColumn.EditingElementStyle>
  <Style TargetType="ComboBox">
    <EventSetter Event="SelectionChanged" Handler="CeilOrientation_SelectionChanged"/>
  </Style>
</DataGridComboBoxColumn.EditingElementStyle>
```

**Code-behind**:
```csharp
private void CeilOrientation_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    RoomsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
    RoomsGrid.CommitEdit(DataGridEditingUnit.Row,  true);
    Recalculate(); // or whatever method you already call after edits
}
```