del /Q CoverageReports\*

packages\OpenCover.4.5.1403\OpenCover.Console.exe -register:user -target:packages\NUnit.Runners.2.6.2\tools\nunit-console.exe -targetargs:"Processor.UnitTests\bin\Debug\Processor.UnitTests.dll /noshadow" -filter:"+[Processor]*" -output:CoverageReports\results.xml

packages\ReportGenerator.1.8.1.0\ReportGenerator.exe -reports:CoverageReports\results.xml -targetdir:CoverageReports\

del /Q TestResult.xml