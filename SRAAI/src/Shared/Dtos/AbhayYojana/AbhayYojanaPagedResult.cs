﻿namespace SRAAI.Shared.Dtos.AbhayYojana;
public class AbhayYojanaPagedResult
{
    public List<AbhayYojanaApplicationDto> Data { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
