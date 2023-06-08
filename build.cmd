@ECHO OFF
SETLOCAL EnableDelayedExpansion

SET _args=%*
IF "%~1"=="-?" SET _args=-help
IF "%~1"=="/?" SET _args=-help

IF ["%_args%"] == [""] (
    :: Perform restore and build, IF no args are supplied.
    SET _args=-restore -build
)

FOR %%x IN (%*) DO (
    SET _arg=%%x

    IF /I "%%x%"=="-coverage" GOTO RUN_CODE_COVERAGE
)

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\build.ps1""" %_args%"
EXIT /b %ERRORLEVEL%


:RUN_CODE_COVERAGE
    SET DOTNET_ROOT=%~dp0.dotnet
    :: This tells .NET Core not to go looking for .NET Core in other places
    SET DOTNET_MULTILEVEL_LOOKUP=0
    SET PATH=%DOTNET_ROOT%;%PATH%

    SET TestResultPath=./artifacts/TestResults/Debug/
    dotnet dotnet-coverage collect --settings ./eng/CodeCoverage.config --output %TestResultPath%/local.cobertura.xml "build.cmd -test -bl"
    dotnet reportgenerator -reports:%TestResultPath%*.cobertura.xml -targetdir:%TestResultPath%CoverageResultsHtml -reporttypes:HtmlInline_AzurePipelines
    :: start %TestResultPath%CoverageResultsHtml/index.html
    :: powershell -ExecutionPolicy ByPass -NoProfile -command "./scripts/ValidateProjectCoverage.ps1 -CoberturaReportXml %TestResultPath%.cobertura.xml"
    EXIT /b %ErrorLevel%
