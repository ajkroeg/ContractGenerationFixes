# ContractGenerationFixes
A small collection of fixes and extensions for contracts in HBS BT.

**v1.2.0.0 and higher require the latest IRBTModUtils: https://github.com/BattletechModders/IRBTModUtils/releases **
**v1.1.0.0 and higher requires modtek v3 or higher**

### The new sexy

As of v1.2.0.0, MapRandomizer has been expanded somewhat. MapRandomizer now reenables the unused `usesExpiration` and `expirationTimeOverride` from ContractOverrides (functionality previously provided by the SearchAndRescue mod). Functionally, this means that contracts can be given a time limit; on expiration they are removed from the contract list. Contracts with a time limit are noted with a special icon in place of the "travel contract" icon. This icon is determined by the `ContractTimeoutIcon` setting.

In addition, MapRandomizer has implemented ContractOverride extensions providing additional functionality. These are additional fields within the ContractOverrides themselves.

`ForceStartOnExpiration` - if present and set true, this contract will be force-started on expiration. Player will be unable to back out of the lance config screen, and unable to negotiate contract terms (default values from the ContractOverride will be used).

`ResultsOnExpiration` - block of SimGameEventResults will be executed on contract expiration.

`UniversalContractEffects` - EffectData defined here will be applied to *all* units spawning in the contract.

### The OG Fixes

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

`MapID`: Spawn contract on this specific map, e.g. `mapGeneral_frostySlopes_iTnd`, optional. <b>Caution!</b> Setting a MapID that is invalid for the chosen contract type <i>will</i> break the event.

`IgnoreBiomes`: e.g, `TRUE`. If set to "TRUE" will spawn maps that do not match Target (or Current) star system (vanilla behavior). If null or any other string, random maps will match biomes of star system. Ignored if MapID used above.

`CustomDifficulty`: Override star system and contract file difficulty, and set to difficulty level specified (+- normal variation).

`SysAdjustDifficulty`: Takes star system difficulty and adjusts it by the number specified. Ignored if CustomDifficulty used above.

Any <i>optional</i> unused additional values must be set `null` if later additional values are used. For example, if unused, `TargetAlly` and `MapID` must be set `null` if `IgnoreBiomes` TRUE.


In addition, MapRandomizer also *mostly* fixes a vanilla issue where travel contracts would be generated using the difficulty level of players current starsystem, but then change to the destination systems difficulty on arrival. This fix can be toggled using the `enableTravelFix` setting.