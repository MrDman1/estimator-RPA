This version fixes the previous shared obj/bin issue by using per-project, per-configuration, per-TFM paths.

OutputPath:
  %LOCALAPPDATA%\NuformBuild\bin\<Project>\<Configuration>\<TargetFramework>\
IntermediateOutputPath:
  %LOCALAPPDATA%\NuformBuild\obj\<Project>\<Configuration>\<TargetFramework>\

Place Directory.Build.props next to NuformEstimator.sln, restart VS, then Rebuild.
