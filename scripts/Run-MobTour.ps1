<#
.SYNOPSIS
  Runs the SuperSoundSliderThingPlus creature tour in the game (DrakesTestLab test "mobs-tour").

.DESCRIPTION
  Moves the player 200 m from the lab start (spawn house) onto flat dry ground, removes every tree, log, rock and pickable within 180 m and every other
  creature within 150 m, then spawns every creature of the game one at a time 5 m in front of the god-mode player (creatures hostile, player on the
  ground), gives each creature 30 seconds (10 s close, then the player is moved to 20 m and to 45 m from the creature so ranged attacks fire), removes it, and logs which sounds the sound patch saw.
  161 creatures x 30 s = about 80 minutes. The result is archived by the lab under <Valheim>\_testlab\*_mobs-tour and the per-creature
  lines are in the game log (BepInEx\LogOutput.log, search "MobTour").

  Close Valheim before running. The mod must be built and deployed first (dotnet build ... -c Release). The Unity editor keeps the GPU busy,
  so -AllowBusyGpu is passed to the lab.
#>
param(
  [string]$Label = "mobs-tour",
  # restart point: the first creature prefab (alphabetical, e.g. "Dragon" = Moder) - everything before it is skipped
  [string]$From = "",
  # end point (exclusive): the first creature prefab NOT to test, e.g. -To Dragon tests everything before Dragon
  [string]$To = "",
  # the lab kills the game after this many minutes (its default is 30, far too short for 161 creatures x 30 s)
  [int]$TimeoutMinutes = 120
)
$lab = Join-Path $PSScriptRoot "..\..\Claude\shared\TestLab\Run-TestLab.ps1"
if (Get-Process valheim -ErrorAction SilentlyContinue) { Write-Error "Valheim is running - close it first."; exit 1 }
$env:SSST_TOUR_FROM = $From
$env:SSST_TOUR_TO = $To
& powershell -NoProfile -ExecutionPolicy Bypass -File $lab -Mods SuperSoundSliderThingPlus -AllowBusyGpu -TimeoutMinutes $TimeoutMinutes -Tests "SuperSoundSliderThingPlus-Audit/mobs-tour" -Label $Label
