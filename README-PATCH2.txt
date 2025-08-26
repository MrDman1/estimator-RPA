
Patch 2:
- Replaced Nuform.Core/Domain/TrimPolicy.cs with a complete implementation (H = 5/pk; 12' vs 16' 60% rule).
- Added a global UI exception handler in Nuform.App/App.xaml.cs to prevent input lockups when a calculation throws.
- CeilingCalc.cs from Patch 1 remains; integrate by calling CeilingCalc.ComputeWidthwise/ComputeLengthwise inside your calc flow.

If SOF export needs to include new ceiling/H items, point the exporter at the same quantities you show on the Results page.
