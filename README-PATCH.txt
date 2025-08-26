
Nuform Estimator — Patched Ceiling + H-Trim (Aug 26, 2025)

WHAT'S INCLUDED
- New Core file: Nuform.Core/Domain/CeilingCalc.cs
- Updated Core file: Nuform.Core/Domain/TrimPolicy.cs
  * H is 5 pieces per pack
  * 12' vs 16' selection uses 60% waste rule

HOW TO BUILD (no coding required)
Option A — Visual Studio 2022 (recommended)
1) Install Visual Studio 2022 Community with ".NET desktop development" workload.
2) Open estimator-RPA-main/estimator-RPA-main/Nuform.App/Nuform.App.csproj
3) Set Configuration = Release, Platform = Any CPU.
4) Build → Build Solution. The EXE will be in:
   estimator-RPA-main/estimator-RPA-main/Nuform.App/bin/Release/net8.0-windows/

Option B — .NET 8 SDK (no IDE)
1) Install .NET 8 SDK from Microsoft.
2) Double-click Build.bat (or run it). The published app will be in the 'publish' folder.

NOTES
- Ceiling transitions use the **wall** colour.
- When colours match, linear footage for trims is combined BEFORE pack sizing.
- H screws follow the same rules as J.
- Widthwise: panelsPerRow = Length (12") or Length/1.5 (18"); rows = ceil(Width/20); one ship length for all rows.
- Lengthwise: panelsPerRow = Width (12") or Width/1.5 (18"); rows/ship length chosen to minimize waste.

If you want me to assemble your exact .sof mapping to COP, export a sample estimate and I’ll generate the matching SOF.
