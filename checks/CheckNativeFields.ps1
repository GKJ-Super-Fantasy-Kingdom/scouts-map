param(
    [string]$GameDir = 'D:/SteamLibrary/steamapps/common/Super Fantasy Kingdom',
    [string]$ProfileDir = "$env:APPDATA/Thunderstore Mod Manager/DataFolder/SuperFantasyKingdom/profiles/Daybook Dev"
)

$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $ProfileDir 'BepInEx/core/Mono.Cecil.dll')
$nativePath = Join-Path $GameDir 'Super Fantasy Kingdom_Data/Managed/Assembly-CSharp.dll'
$modPath = Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/ScoutsMap.dll'

$native = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($nativePath)
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $modPath).Path)
try {
    $count = 0
    foreach ($owner in @('Plugin', 'MarkerTarget', 'EncounterTargets')) {
        $type = $mod.MainModule.GetType("ScoutsMap.$owner")
        $initializer = $type.Methods | Where-Object Name -CEQ '.cctor'
        foreach ($instruction in $initializer.Body.Instructions) {
            $method = $instruction.Operand
            if ($method -isnot [Mono.Cecil.GenericInstanceMethod] -or
                $method.Name -cnotin @('Field', 'FieldRefAccess')) {
                continue
            }
            if ($instruction.Previous.OpCode.Name -cne 'ldstr') {
                throw "Missing literal field name in $owner"
            }
            $fieldName = $instruction.Previous.Operand
            $gameType = $method.GenericArguments[0].FullName
            $fieldType = $method.GenericArguments[1].FullName
            $matches = @($native.MainModule.GetType($gameType).Fields | Where-Object {
                $_.Name -ceq $fieldName -and $_.FieldType.FullName -ceq $fieldType -and !$_.IsStatic
            })
            if ($matches.Count -ne 1) {
                throw "Missing or mismatched game field $gameType.$fieldName ($fieldType)"
            }
            $count++
        }
    }
    if ($count -lt 30) {
        throw "Only $count field bindings were found in the built DLL."
    }
    foreach ($signature in @(
        @('SuperFantasyKingdom.Clickable.WorldUnit', 'GetCoinCost'),
        @('SuperFantasyKingdom.Clickable.BuildingHospital', 'GetCosts')
    )) {
        $methods = @($native.MainModule.GetType($signature[0]).Methods | Where-Object {
            $_.Name -ceq $signature[1] -and $_.ReturnType.FullName -ceq 'System.Int32' -and
            !$_.IsStatic -and $_.Parameters.Count -eq 0
        })
        if ($methods.Count -ne 1) {
            throw "Missing or mismatched native price method $($signature -join '.')"
        }
    }
    "PASS $count private fields and 2 native price methods against the installed Assembly-CSharp.dll."
} finally {
    $mod.Dispose()
    $native.Dispose()
}
