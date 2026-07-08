param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-AspNetCoreSharedPath {
    $base = Join-Path ${env:ProgramFiles} "dotnet\shared\Microsoft.AspNetCore.App"
    if (-not (Test-Path $base)) {
        throw "Microsoft.AspNetCore.App shared framework not found at: $base"
    }

    $latest = Get-ChildItem -Path $base -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if ($null -eq $latest) {
        throw "No Microsoft.AspNetCore.App versions found under: $base"
    }

    return $latest.FullName
}

function Load-IdentityAssemblies {
    $sharedPath = Get-AspNetCoreSharedPath
    $assemblies = @(
        "Microsoft.Extensions.Identity.Core.dll",
        "Microsoft.Extensions.Options.dll",
        "Microsoft.Extensions.Logging.Abstractions.dll",
        "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
        "Microsoft.AspNetCore.Cryptography.KeyDerivation.dll",
        "System.Security.Cryptography.Algorithms.dll"
    )

    foreach ($assembly in $assemblies) {
        $assemblyPath = Join-Path $sharedPath $assembly
        if (Test-Path $assemblyPath) {
            Add-Type -Path $assemblyPath
        }
    }
}

function Compile-HasherType {
    $source = @"
using Microsoft.AspNetCore.Identity;

public class SeedUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public static class IdentityHashHelper
{
    public static string Hash(string password)
    {
        var hasher = new PasswordHasher<SeedUser>();
        var user = new SeedUser();
        return hasher.HashPassword(user, password);
    }
}
"@

    Add-Type -TypeDefinition $source -Language CSharp
}

function Get-PasswordHash([string]$password) {
    return [IdentityHashHelper]::Hash($password)
}

function Upsert-UserByEmail($connection, [string]$email, [string]$fullName, [int]$role, [string]$passwordHash) {
    $sql = @"
IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
BEGIN
    UPDATE Users
    SET FullName = @FullName,
        Role = @Role,
        PasswordHash = @PasswordHash,
        IsActive = 1
    WHERE Email = @Email;
END
ELSE
BEGIN
    INSERT INTO Users (Email, FullName, PasswordHash, Role, CreatedAt, IsActive)
    VALUES (@Email, @FullName, @PasswordHash, @Role, SYSUTCDATETIME(), 1);
END
"@

    $cmd = $connection.CreateCommand()
    $cmd.CommandText = $sql
    [void]$cmd.Parameters.Add("@Email", [System.Data.SqlDbType]::NVarChar, 256)
    [void]$cmd.Parameters.Add("@FullName", [System.Data.SqlDbType]::NVarChar, 256)
    [void]$cmd.Parameters.Add("@PasswordHash", [System.Data.SqlDbType]::NVarChar, -1)
    [void]$cmd.Parameters.Add("@Role", [System.Data.SqlDbType]::Int)

    $cmd.Parameters["@Email"].Value = $email
    $cmd.Parameters["@FullName"].Value = $fullName
    $cmd.Parameters["@PasswordHash"].Value = $passwordHash
    $cmd.Parameters["@Role"].Value = $role

    [void]$cmd.ExecuteNonQuery()
}

Load-IdentityAssemblies
Compile-HasherType

$users = @(
    @{ Email = "admin@greenlab.local"; FullName = "Admin User"; Role = 0; Password = "Admin123!" },
    @{ Email = "teacher@greenlab.local"; FullName = "Teacher User"; Role = 1; Password = "Teacher123!" },
    @{ Email = "assistant@greenlab.local"; FullName = "TA User"; Role = 2; Password = "Assistant123!" }
)

$connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
$connection.Open()

try {
    foreach ($user in $users) {
        $hash = Get-PasswordHash -password $user.Password
        Upsert-UserByEmail -connection $connection -email $user.Email -fullName $user.FullName -role $user.Role -passwordHash $hash
    }
}
finally {
    $connection.Close()
}

Write-Host "Users seeded/updated successfully:"
Write-Host "- admin@greenlab.local / Admin123! / Role=Admin"
Write-Host "- teacher@greenlab.local / Teacher123! / Role=Teacher"
Write-Host "- assistant@greenlab.local / Assistant123! / Role=TeacherAssistant"
