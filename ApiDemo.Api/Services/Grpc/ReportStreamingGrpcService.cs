using FunctionalProgramming.Grpc;
using Grpc.Core;

namespace FunctionalProgramming.Services.Grpc;

public sealed class ReportStreamingGrpcService : ReportStreaming.ReportStreamingBase
{
    public override async Task StreamReportProgress(
        ReportProgressRequest request,
        IServerStreamWriter<ReportProgressUpdate> responseStream,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.ReportName) || request.ReportName.Length > 80)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Report name must contain 1 to 80 characters."));
        }

        string[] stages = ["Queued", "Reading data", "Aggregating", "Writing report", "Completed"];
        for (var index = 0; index < stages.Length; index++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (index > 0)
            {
                // Simulate work; cancelling the call also stops this delay.
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
            }

            await responseStream.WriteAsync(new ReportProgressUpdate
            {
                ReportName = request.ReportName.Trim(),
                Sequence = index + 1,
                PercentComplete = index * 25,
                Status = stages[index],
                Completed = index == stages.Length - 1
            });
        }
    }
}
