# ContractGenerationFixes
A small collection of fixes for contract generation in HBS BT. Does not affect procedurally generated contracts or Flashpoints.

**v1.1.0.0 and higher requires modtek v3 or higher**

MapRandomizer fixes a vanilla bug that would result in event-generated contracts always spawning on the same map. Instead, anything that calls the method `AddContract` (primarily events through `System_AddContract` or `System_StartContract`) should now select a random map appropriate to the contract type and biome. Additional values added to `ParseContractActionData` that allow you to force use of a specific map, ignore biome enforcement, and alter difficulty of event-spawned contracts.

Example use of additional values:
```
"Actions" : [
  {
    "Type" : "System_AddContract",      
    "value" : "ContractName",           
    "valueConstant" : null,             
    "additionalValues" : [
      "Target",                         
      "Employer",                       
      "TargetSystem",                   
      "TargetAlly",                     
      "MapID",                          
      "IgnoreBiomes",
      "CustomDifficulty",
      "SysAdjustDifficulty"                               
    ]                                   
}                                       
```
`ContractName`: name of contract, e.g. `AmbushConvoy_GuerrillaInterception`, required.

`Target`: OpFor factionID, e.g. `Liao`, required.

`Employer`: Employer factionID e.g. `Davion`, required.

`TargetSystem`: target star system ID for travel contract e.g. `starsystemdef_Detroit`, optional.

`TargetAlly`: Allied factionID, e.g. `Steiner`, optional.

`MapID`: Spawn contract on this specific map, e.g. `mapGeneral_frostySlopes_iTnd`, optional. <b>Caution!</b> Setting a MapID that is invalid                   for the chosen contract type <i>will</i> break the event.

`IgnoreBiomes`: e.g, `TRUE`. If set to "TRUE" will spawn maps that do not match Target (or Current) star system (vanilla behavior). If null or any other string, random maps will match biomes of star system. Ignored if MapID used above.

`CustomDifficulty`: Override star system and contract file difficulty, and set to difficulty level specified (+- normal variation).

`SysAdjustDifficulty`: Takes star system difficulty and adjusts it by the number specified. Ignored if CustomDifficulty used above.

Any <i>optional</i> unused additional values must be set `null` if later additional values are used. For example, if unused, `TargetAlly` and `MapID` must be set `null` if `IgnoreBiomes` TRUE.


