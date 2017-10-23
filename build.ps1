$env:VersionDateSuffix = [System.DateTime]::Now.ToString("yyyyMMddHHmmss");
dotnet build -bl:Build.binlog;
dotnet pack -bl:Pack.binlog;