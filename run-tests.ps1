$reporter = @{$true="-appveyor";$false="-verbose"}[$CI -eq "True"]
.\packages\xunit.runner.console.*\tools\xunit.console.exe .\NativeInjector.test\bin\$env:CONFIGURATION\NativeInjector.Test.dll $reporter
write-host
.\packages\xunit.runner.console.*\tools\xunit.console.x86.exe .\NativeInjector.test\bin\$env:CONFIGURATION\NativeInjector.Test.dll $reporter