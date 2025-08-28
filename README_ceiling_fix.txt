Nuform Estimator — Ceiling Orientation + Manual Length Honor

What this patch fixes:
1) Widthwise orientation now mirrors Lengthwise correctly.
   - panelsPerRow = ceil(<along panel direction span> / shipLen)
   - rows         = ceil(<perpendicular span> / panelWidthFt)
   - H-Trim LF    = max(0, rows-1) × <span parallel to H-Trim seams>

   Concretely:
   • Lengthwise: panelsPerRow = ceil(L / shipLen); rows = ceil(W / (1 or 1.5)); H-Trim LF = (rows-1)×W
   • Widthwise : panelsPerRow = ceil(W / shipLen); rows = ceil(L / (1 or 1.5)); H-Trim LF = (rows-1)×L

2) Manual length override is honored if your model exposes any of:
   • CeilingLen
   • CeilingPanelLengthFt
   • CeilingLengthFt
   (It must be 10/12/14/16/18/20 to be valid.) If not present or invalid, we fall back to min-waste auto-pick.

3) No changes to wall logic or downstream trim packaging.

Notes:
- If flipping the Orientation combobox still doesn’t recalc immediately, set the binding
  to UpdateSourceTrigger=PropertyChanged or commit the edit in SelectionChanged.