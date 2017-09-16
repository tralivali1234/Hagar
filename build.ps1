$env:VersionDateSuffix = [System.DateTime]::Now.ToString("yyyyMMddhhmmss");
dotnet build;
dotnet pack;