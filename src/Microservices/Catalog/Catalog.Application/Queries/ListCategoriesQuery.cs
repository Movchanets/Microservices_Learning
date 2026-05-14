using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Queries;

public sealed record ListCategoriesQuery : IRequest<List<CategoryDto>>;
