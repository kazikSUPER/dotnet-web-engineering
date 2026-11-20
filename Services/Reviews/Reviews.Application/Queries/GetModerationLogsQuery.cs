using AutoMapper;
using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Queries;

public sealed record GetModerationLogsQuery(string? ReviewId, int Limit = 20, string? Cursor = null) : IQuery<ModerationLogPageDto>;

public sealed class GetModerationLogsQueryHandler(IModerationLogRepository repository, IMapper mapper)
    : IRequestHandler<GetModerationLogsQuery, ModerationLogPageDto>
{
    public async Task<ModerationLogPageDto> Handle(GetModerationLogsQuery request, CancellationToken ct)
    {
        ObjectId? reviewId = string.IsNullOrWhiteSpace(request.ReviewId) ? null : ObjectId.Parse(request.ReviewId);
        var page = await repository.GetPageAsync(reviewId, request.Cursor, request.Limit, ct);

        return new ModerationLogPageDto
        {
            Items = page.Items.Select(mapper.Map<ModerationLogDto>).ToArray(),
            NextCursor = page.NextCursor,
            HasMore = page.HasMore,
        };
    }
}
