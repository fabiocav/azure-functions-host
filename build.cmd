@ECHO Off
ECHO test
SET Config=%1
IF "%1"=="" (
  SET Config="Release"
)

msbuild WebJobs.Script.proj /p:Configuration=%Config%;SolutionDir=%~dp0
