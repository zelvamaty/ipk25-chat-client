all: publish

publish:
	dotnet publish -r linux-x64 -c Release -o .

