properties {
    $nunitConsolePath = '.\packages\NUnit.Console.3.0.1\tools\nunit3-console.exe'
}


task default -depends Clean, Build, Test

task Clean {
	exec { msbuild 6502.sln '/t:Clean' /nologo /verbosity:Minimal }
}

task Build {
	exec { msbuild 6502.sln '/t:Build' /nologo /verbosity:Minimal  }
}

task Test {
    $env:TestDataDirectory = ".\Processor.UnitTests\Functional Tests"
    & $nunitConsolePath '6502.nunit'
}