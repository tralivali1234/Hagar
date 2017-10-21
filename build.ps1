$env:VersionDateSuffix = [System.DateTime]::Now.ToString("yyyyMMddHHmmss");
dotnet build;
dotnet pack;