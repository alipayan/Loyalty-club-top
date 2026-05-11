using CustomerClub.BuildingBlocks.Application.Results;

namespace CustomerClub.BuildingBlocks.Application.CQRS;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}