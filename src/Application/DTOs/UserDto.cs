using System;
using System.Collections.Generic;

namespace CameraAccessAPI.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Document { get; set; }
    public bool Active { get; set; }
    
    public List<CameraSummaryDto> Cameras { get; set; } = new();
    public List<AccessRuleSummaryDto> Rules { get; set; } = new();
}

public class UserInputDto
{
    public string Name { get; set; } = default!;
    public string? Document { get; set; }
    public bool Active { get; set; } = true;
    public List<Guid> CameraIds { get; set; } = new();
    public List<UserInputRuleDto> Rules { get; set; } = new();
}

public class UserInputRuleDto
{
    public Guid? CameraId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<int> DaysOfWeek { get; set; } = new();
}

public class CameraSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}

public class AccessRuleSummaryDto
{
    public Guid Id { get; set; }
    public Guid? CameraId { get; set; }
    public string? CameraName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<int> DaysOfWeek { get; set; } = new();
}
