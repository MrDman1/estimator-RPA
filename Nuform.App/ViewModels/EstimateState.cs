using Nuform.Core.Domain;

namespace Nuform.App.ViewModels;

public class EstimateState
{
    public BuildingInput Input { get; }
    public CalcEstimateResult Result { get; set; }

    public EstimateState(BuildingInput input, CalcEstimateResult result)
    {
        Input = input;
        Result = result;
    }
}
