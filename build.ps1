$env:VersionDateSuffix = [System.DateTime]::Now.ToString("yyyyMMddHHmmss");
dotnet build -bl:Build.binlog -v:d;
dotnet pack -bl:Pack.binlog -v:d;