using Vertx.CashFlow.ReportsWorker;
using Vertx.CashFlow.BuildingBlocks;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(FileCashFlowStore.FromEnvironment(builder.Environment.EnvironmentName));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
