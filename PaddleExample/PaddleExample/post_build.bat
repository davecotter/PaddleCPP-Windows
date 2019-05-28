set SRC_PATH=%1
set TargetDir=%2
set FILE_LIST=(StructureMap RGiesecke.DllExport.Metadata Newtonsoft.Json Interop.SHDocVw CredentialManagement)

echo off
for %%i in %FILE_LIST% do (
	echo	Copying %%i...
	copy "%SRC_PATH%\%%i.dll" "%TargetDir%"
)
