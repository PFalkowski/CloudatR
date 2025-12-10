using CloudatR.Lib.Abstractions;
using CloudatR.Lib.CloudEvents;

namespace CloudatR.Lib.Tests.TestHelpers;

public record SimpleCommand(string Name) : IRequest<string>;

public record SimpleQuery(int Id) : IRequest<QueryResult>;

public record QueryResult(int Id, string Data);

public record SimpleEvent(string Data) : INotification;

[CloudEvent(Type = "custom.type.test", Source = "custom-source", Subject = "test-subject")]
public record CustomCloudEventRequest(string Value) : IRequest<string>;

public record GenericMessage(string Content) : IRequest<string>;
