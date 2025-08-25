using System;
using Nuform.Core.Domain;

namespace Nuform.App.ViewModels
{
    public class EstimateState
    {
        public BuildingInput Input { get; set; } = new();
        public CalcEstimateResult Result { get; set; } = new();
        public event Action? Updated;
        public void RaiseUpdated() => Updated?.Invoke();
    }
}

