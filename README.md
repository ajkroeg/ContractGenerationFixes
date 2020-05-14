# ContractGenerationFixes
A small collection of fixes for contract generation in HBS BT. Does not affect procedurally generated contracts or Flashpoints.

MapRandomizer fixes a vanilla bug that would result in event-generated contracts always spawning on the same map. Instead, anything that calls the method `AddContract` (primarily events through `System_AddContract` or `System_StartContract`) should now select a random map appropriate to the contract type and biome.
