set SRC_PATH=%1
set TargetDir=%2
set FILE_LIST=(StructureMap RGiesecke.DllExport.Metadata Newtonsoft.Json Interop.SHDocVw CredentialManagement)

for %%i in %FILE_LIST% do (
	echo	Copying %%i...
	copy "%SRC_PATH%\%%i.dll" "%TargetDir%"
)

set i=PaddleWrapper.pdb
echo	Copying %i%...
copy "%SRC_PATH%\%i%" "%TargetDir%"

set i=PaddleCLR.pdb
echo	Copying %i%...
copy "%SRC_PATH%\..\..\..\..\..\PaddleCLR\PaddleCLR\bin\x86\Debug\%i%" "%TargetDir%"
