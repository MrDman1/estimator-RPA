V7 fixes duplicate 'net8.0-windows\net8.0-windows' by appending the TFM only once.

1) Create folder: C:\dev\NuformBuild\
2) Place Directory.Build.props next to NuformEstimator.sln (solution root).
3) Close + reopen VS, Clean, Rebuild.
4) Verify output: C:\dev\NuformBuild\bin\Nuform.App\Debug\net8.0-windows\

To run without launching the EXE (bypass policy), use the .NET host:
  dotnet "C:\dev\NuformBuild\bin\Nuform.App\Debug\net8.0-windows\Nuform.App.dll"

Or configure VS:
  Project > Nuform.App > Properties > Debug
   - Launch: Executable
   - Executable: C:\Program Files\dotnet\dotnet.exe
   - Application arguments: "$(TargetDir)Nuform.App.dll"
   - Working directory: $(TargetDir)
