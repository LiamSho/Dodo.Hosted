Param(
  [Parameter(Position = 0, Mandatory = $true)][String]$ProjectDir,
  [Parameter(Position = 1, Mandatory = $true)][String]$BundleId
)

Write-Output "ProjectDir: $ProjectDir"

dotnet publish -c Release -o $ProjectDir/bin/publish

Compress-Archive -Path $ProjectDir/bin/publish/* -Destination ./$BundleId.zip
