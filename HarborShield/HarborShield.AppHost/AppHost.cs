var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.HarborShield_Worker>("harborshield-worker");

builder.AddProject<Projects.HarborShield_Api>("harborshield-api");

builder.Build().Run();
