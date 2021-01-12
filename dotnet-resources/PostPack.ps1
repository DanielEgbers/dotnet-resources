$toolPath = [System.IO.Path]::Combine((Get-Location), 'bin\Debug');
dotnet tool update dotnet-resources --tool-path $toolPath