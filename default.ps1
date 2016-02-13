task default -depends Clean, Build, Test

task Clean {
	exec { msbuild 6502.sln '/t:Clean' /nologo /verbosity:Minimal }
}

task Build {
	exec { msbuild 6502.sln '/t:Build' /nologo /verbosity:Minimal  }
}

task Test {
    exec { .\packages\NUnit.Runners.2.6.2\tools\nunit-console.exe 6502.nunit }
}