$reporter = @{$true="-appveyor";$false="-verbose"}[$CI -eq "True"]
$xunit64 = resolve-path ".\packages\xunit.runner.console.*\tools\xunit.console.exe"
$xunit32 = resolve-path ".\packages\xunit.runner.console.*\tools\xunit.console.x86.exe"
$testAssembly = ".\NativeInjector.test\bin\$env:CONFIGURATION\NativeInjector.Test.dll"

& $xunit64 $testAssembly $reporter
write-host
& $xunit32 $testAssembly $reporter