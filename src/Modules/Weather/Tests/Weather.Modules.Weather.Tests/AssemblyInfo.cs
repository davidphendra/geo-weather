// Telemetry tests assert exact counts on the process-global "Weather" meter. Serialize the assembly so a
// concurrent test cannot add to the same instrument between setup and assertion.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
