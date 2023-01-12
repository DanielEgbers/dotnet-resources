Remove-Item ./tmp -Recurse -ErrorAction Ignore
New-Item -ItemType Directory ./tmp | Out-Null
Invoke-WebRequest http://www.angusj.com/resourcehacker/resource_hacker.zip -OutFile ./tmp/data.zip
Expand-Archive ./tmp/data.zip -DestinationPath ./tmp
Copy-Item ./tmp/ResourceHacker.exe .
Remove-Item ./tmp -Recurse