using KmValidator.Application.DTOs;
using KmValidator.Application.Mappers;
using KmValidator.Application.Ports;
using KmValidator.Domain.Ports;
using KmValidator.Domain.Services.Validation;

namespace KmValidator.Application.UseCases;

public class ValidateActivityUseCase : IValidateActivityUseCase
{
    private readonly IPhysicalActivityRepository _activityRepository;
    private readonly IWearableDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ActivityKmValidationService _validationService;

    public ValidateActivityUseCase(
        IPhysicalActivityRepository activityRepository,
        IWearableDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork,
        ActivityKmValidationService validationService)
    {
        _activityRepository = activityRepository;
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
    }

    public async Task<ActivitySummaryDto> ExecuteAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        var activity = await _activityRepository.GetByIdAsync(activityId, cancellationToken)
            ?? throw new KeyNotFoundException($"Atividade física com ID {activityId} não foi encontrada.");

        var device = await _deviceRepository.GetByIdAsync(activity.DeviceId, cancellationToken);

        // Re-executa as regras de validação
        await _validationService.ValidateActivityAsync(activity, cancellationToken);

        await _activityRepository.UpdateAsync(activity, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ActivityMapper.ToDto(activity, device?.DeviceName);
    }
}
