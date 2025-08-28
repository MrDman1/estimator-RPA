THIS FILE OVERRIDES ALL PROJECT OUTPUT PATHS.

1) Create folder: C:\dev\NuformBuild\
2) Place Directory.Build.props next to NuformEstimator.sln (solution root).
   Make sure there is NO other Directory.Build.props in any parent folder.
3) Reopen solution → Clean → Rebuild.
4) Verify in Build Output that OutputPath points to C:\dev\NuformBuild\...
5) Start Debugging from VS.

If still 'Access is denied':
 - Run VS as Administrator.
 - Event Viewer → Microsoft → Windows → AppLocker → EXE and DLL (Denied events).
 - Event Viewer → Microsoft → Windows → CodeIntegrity → Operational (WDAC denies).
 - Copy EXE from C:\dev\NuformBuild\... to Desktop and run it manually.
