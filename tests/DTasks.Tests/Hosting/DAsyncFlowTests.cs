using static DTasks.Hosting.HostingFixtures;

namespace DTasks.Hosting;

public partial class DAsyncFlowTests
{
    private readonly FakeDTaskStorage _storage;
    private readonly FakeDTaskConverter _converter;
    private readonly TestBinaryDTaskHost _sut;

    public DAsyncFlowTests()
    {
        _storage = new FakeDTaskStorage();
        _converter = new FakeDTaskConverter();

        _sut = Substitute.For<TestBinaryDTaskHost>(_storage, _converter);
    }

    [Fact]
    public async Task DAsyncFlow_ShouldBeSuspendableAndResumable()
    {
        // Arrange
        async DTask<FileData> ProcessFileDAsync(string url)
        {
            ReadOnlyMemory<byte> file = await DownloadBytesAsync(url);
            FileData data = await ProcessDataDAsync(file);
            data.Date = await GetDateDAsync();
            await DTask.Yield();
            return data;
        }

        static async Task<byte[]> DownloadBytesAsync(string url)
        {
            byte[] bytes = new byte[url.Length];
            await Task.Delay(500);
            Random.Shared.NextBytes(bytes);
            return bytes;
        }

        static async DTask<FileData> ProcessDataDAsync(ReadOnlyMemory<byte> file)
        {
            var data = new FileData { FeatureCount = file.Length + 10 };
            data = await SignDAsync(data, "mytoken");
            return data;
        }

        static async DTask<FileData> SignDAsync(FileData data, string token)
        {
            string signature = "signed with ";
            await DTask.Delay(TimeSpan.FromSeconds(1));
            data.Signature = signature + token;
            return data;
        }

        static DTask<DateTime> GetDateDAsync()
        {
            return DTask<DateTime>.Suspend((id, ct) => Task.CompletedTask);
        }

        var scope = Substitute.For<IDTaskScope>();
        var context = new TestFlowContext();
        DateTime date = DateTime.Now;
        DTask task = ProcessFileDAsync("http://dtasks.com");
        FlowId flowId = default;

        _sut.OnDelayAsync_Public(Arg.Any<FlowId>(), scope, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                flowId = call.Arg<FlowId>();
                return Task.CompletedTask;
            });

        _sut.OnCallbackAsync_Public(Arg.Any<FlowId>(), scope, Arg.Any<ISuspensionCallback>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                flowId = call.Arg<FlowId>();
                return Task.CompletedTask;
            });

        _sut.OnYieldAsync_Public(Arg.Any<FlowId>(), scope, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                flowId = call.Arg<FlowId>();
                return Task.CompletedTask;
            });

        // Act
        var awaiter = task.GetDAwaiter();
        await awaiter.IsCompletedAsync();
        await _sut.SuspendAsync(context, scope, awaiter);

        await _sut.ResumeAsync(flowId, scope);
        await _sut.ResumeAsync(flowId, scope, date);
        await _sut.ResumeAsync(flowId, scope);

        // Assert
        await _sut.Received().OnDelayAsync_Public(flowId, scope, TimeSpan.FromSeconds(1), Arg.Any<CancellationToken>());
        await _sut.Received().OnCallbackAsync_Public(flowId, scope, Arg.Any<ISuspensionCallback>(), Arg.Any<CancellationToken>());
        await _sut.Received().OnYieldAsync_Public(flowId, scope, Arg.Any<CancellationToken>());
        await _sut.Received().OnCompletedAsync_Public(
            flowId,
            context,
            Arg.Is<FileData>(data => data.FeatureCount == 27 && data.Signature == "signed with mytoken" && data.Date == date),
            Arg.Any<CancellationToken>());
    }

    private class FileData
    {
        public int FeatureCount { get; set; }
        public string Signature { get; set; } = "";
        public DateTime Date { get; set; }
    }
}
