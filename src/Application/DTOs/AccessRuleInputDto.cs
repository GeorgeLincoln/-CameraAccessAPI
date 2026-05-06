using System.ComponentModel.DataAnnotations;

namespace CameraAccessAPI.Application.DTOs;

public sealed class AccessRuleInputDto
{
    [Required(ErrorMessage = "O ID do usuário é obrigatório.")]
    [MaxLength(50)]
    public string UserId { get; set; } = default!;
    public Guid CameraId { get; set; }
    public bool Allowed { get; set; } = true;
    public ICollection<int> Days { get; set; } = new List<int>();
    public ICollection<AccessScheduleInputDto> Schedules { get; set; } = new List<AccessScheduleInputDto>();
}

public sealed class AccessScheduleInputDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
