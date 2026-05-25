namespace DanfossSPGroup7.Domain
{
    public class OptimizationResult
    {
        public required ProductionUnit Unit { get; init; }
        public double NetProductionCost { get; init; }
        public double HeatProduction { get; init; }
    }
}
