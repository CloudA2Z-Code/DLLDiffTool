[CmdletBinding()]
param(
    [parameter(position=0)]
    [string]$param1
)
[Reflection.Assembly]::ReflectionOnlyLoadFrom($param1).ImageRuntimeVersion
