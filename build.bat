@ECHO OFF

SET tag=%1

IF "%1"=="" (
	echo No tag provided, aborting
	GOTO :scriptend
)

echo building %tag%

docker build -t alanbot .
docker tag alanbot registry.resource.dk/alanbot:%tag%
docker push registry.resource.dk/alanbot:%tag%

:scriptend