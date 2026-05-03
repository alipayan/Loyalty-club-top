namespace CustomerClub.PointGenerator.Domain.Enums;

public enum DataSourceType : byte
{
    Elastic = 1,
    SqlServer = 2,
    Api = 3,
    Excel = 4,
    Oracle = 5
}

public enum DataSourceStatus : byte
{
    Active = 1,
    Disabled = 2
}

public enum DatasetKind : byte
{
    Table = 1,
    View = 2,
    Index = 3,
    Endpoint = 4,
    Sheet = 5
}

public enum CheckpointMode : byte
{
    None = 0,
    Time = 1,
    Id = 2,
    RowVersion = 3,
    Cursor = 4,
    Hash = 5
}

public enum PointRuleRunTriggerType : byte
{
    Manual = 1,
    Schedule = 2,
    OnUpload = 3
}

public enum PointRuleRunStatus : byte
{
    Running = 1,
    Completed = 2,
    Failed = 3,
    Partial = 4
}

public enum ProducedEventStatus : byte
{
    Pending = 1,
    Sent = 2,
    Acked = 3,
    Failed = 4
}