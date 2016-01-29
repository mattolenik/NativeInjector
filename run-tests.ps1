$reporter = @{$true="-appveyor";$false="-verbose"}[$CI -eq "True"]
if(!$env:CONFIGURATION) {
  write-host '$env:CONFIGURATION not set, defaulting to "Debug"'
  write-host
  $config = "Debug"
} else {
  $config = $env:CONFIGURATION
}
$xunit64 = resolve-path ".\packages\xunit.runner.console.*\tools\xunit.console.exe"
$xunit32 = resolve-path ".\packages\xunit.runner.console.*\tools\xunit.console.x86.exe"
$testAssembly = ".\NativeInjector.Test\bin\$config\NativeInjector.Test.dll"

& $xunit64 $testAssembly $reporter
write-host
& $xunit32 $testAssembly $reporter