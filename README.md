# ContractGenerationFixes
A small collection of fixes for contract generation in HBS BT. Does not affect procedurally generated contracts or Flashpoints.

MapRandomizer fixes a vanilla bug that would result in event-generated contracts always spawning on the same map. Instead, anything that calls the method `AddContract` (primarily events through `System_AddContract` or `System_StartContract`) should now select a random map appropriate to the contract type and biome. Additional values added to `ParseContractActionData` that allow you to force use of a specific map or ignore biome enforcement.

Example use of additional values:
```
"Actions" : [
  {
    "Type" : "System_AddContract",      //  Also works for System_StartContract
    "value" : "ContractName",           //  Name of the contract you want to spawn, required
    "valueConstant" : null,             //  Always null
    "additionalValues" : [
      "Target",                         //  OpFor faction name, required
      "Employer",                       //  Employer faction name, required
      "TargetSystem",                   //  Target star system, optional. If present, spawns travel contract.
      "TargetAlly",                     //  Allied faction name, optional (if appropriate to contract).
      "MapID",                          //  MapID of desired map, optional.
      "IgnoreBiomes"                    //  Optional. If set to "TRUE" will spawn maps that do not match Target
    ]                                   //    (or Current) star system. If null or any other string, random maps 
}                                       //    will match biomes of star system. Not needed if MapID used above.
```
Any <i>optional</i> unused additional values must be set `null` if later additional values are used. For example, if unused, `TargetAlly` and `MapID` must be set `null` if `IgnoreBiomes` TRUE.

Setting a MapID that is invalid for the chosen contract type <i>will</i> break the event.
