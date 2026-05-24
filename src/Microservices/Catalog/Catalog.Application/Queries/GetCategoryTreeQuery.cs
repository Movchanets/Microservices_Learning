using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public record GetCategoryTreeQuery : IRequest<List<CategoryTreeDto>>;
